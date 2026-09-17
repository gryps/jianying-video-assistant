from __future__ import annotations

import re
import shutil
from dataclasses import dataclass
from pathlib import Path

from sqlalchemy import select
from sqlalchemy.orm import Session

from app.media import VIDEO_EXTENSIONS, probe_video
from app.domain.models import (
    MediaAsset,
    MediaAssetTag,
    Product,
    ProductCategory,
    ShotTag,
    TagCategory,
)
from app.core.security import utc_now
from app.services.product_library import create_product, duplicate_product_name
from app.text_normalization import normalize_tag_name


INVALID_FILENAME = re.compile(r'[<>:"/\\|?*\x00-\x1f]')
FREE_TAG_CATEGORY_NAME = "自由标签"


@dataclass(frozen=True)
class ClassificationItem:
    source_path: str
    tag_ids: list[str]


def _normalized_history_name(value: str) -> str:
    return re.sub(r"\s+", "", value).casefold()


def resolve_classification_product(session: Session, *, category_name: str, product_name: str) -> Product:
    clean_category = " ".join(category_name.strip().split())
    clean_product = " ".join(product_name.strip().split())
    if not clean_category:
        raise ValueError("请输入产品分类")
    if not clean_product:
        raise ValueError("请输入产品名称")
    if len(clean_category) > 80 or len(clean_product) > 160:
        raise ValueError("产品分类或产品名称过长")

    category_key = _normalized_history_name(clean_category)
    category = next(
        (item for item in session.scalars(select(ProductCategory)).all() if _normalized_history_name(item.name) == category_key),
        None,
    )
    if category is None:
        category = ProductCategory(name=clean_category)
        session.add(category)
        session.flush()

    product = duplicate_product_name(session, clean_product)
    if product is not None and product.category_id not in (None, category.id):
        existing_category = session.get(ProductCategory, product.category_id)
        raise ValueError(f"产品名称“{product.name}”已属于“{existing_category.name if existing_category else '其他'}”分类")
    if product is None:
        product = create_product(session, name=clean_product)
    product.category_id = category.id
    product.status = "active"
    now = utc_now()
    product.updated_at = now
    category.updated_at = now
    session.flush()
    return product


def resolve_free_tag_ids(session: Session, tag_names: list[str]) -> list[str]:
    cleaned: list[tuple[str, str]] = []
    seen: set[str] = set()
    for value in tag_names:
        name = " ".join(value.strip().split())
        normalized = normalize_tag_name(name)
        if not normalized or normalized in seen:
            continue
        if len(name) > 80:
            raise ValueError(f"标签“{name[:16]}…”超过 80 个字符")
        seen.add(normalized)
        cleaned.append((name, normalized))
    if not cleaned:
        raise ValueError("每条视频至少输入一个标签")
    if len(cleaned) > 30:
        raise ValueError("每条视频最多输入 30 个标签")

    category_key = normalize_tag_name(FREE_TAG_CATEGORY_NAME)
    category = session.scalar(select(TagCategory).where(TagCategory.normalized_name == category_key))
    if category is None:
        category = TagCategory(name=FREE_TAG_CATEGORY_NAME, normalized_name=category_key)
        session.add(category)
        session.flush()
    existing = session.scalars(
        select(ShotTag).where(ShotTag.category_id == category.id, ShotTag.normalized_name.in_([value[1] for value in cleaned]))
    ).all()
    by_normalized = {item.normalized_name: item for item in existing}
    result: list[str] = []
    for name, normalized in cleaned:
        tag = by_normalized.get(normalized)
        if tag is None:
            tag = ShotTag(name=name, normalized_name=normalized, category_id=category.id)
            session.add(tag)
            session.flush()
            by_normalized[normalized] = tag
        result.append(tag.id)
    return result


def safe_filename_part(value: str, *, fallback: str) -> str:
    clean = INVALID_FILENAME.sub("-", value).strip(" .-")
    clean = re.sub(r"\s+", " ", clean)
    return (clean or fallback)[:80]


def _available_destination(folder: Path, stem: str, suffix: str, reserved: set[Path]) -> Path:
    candidate = folder / f"{stem}{suffix}"
    sequence = 2
    while candidate.exists() or candidate in reserved:
        candidate = folder / f"{stem}-{sequence}{suffix}"
        sequence += 1
    return candidate


def classify_and_move_originals(
    session: Session,
    *,
    product_id: int,
    source_dir: str,
    items: list[ClassificationItem],
) -> list[MediaAsset]:
    product = session.get(Product, product_id)
    if product is None or product.status != "active":
        raise ValueError("产品不存在或已停用")
    if not items:
        raise ValueError("至少选择一个待归类视频")

    root = Path(source_dir).expanduser().resolve()
    if not root.is_dir():
        raise ValueError("素材目录不存在或不可访问")
    product_part = safe_filename_part(product.name, fallback=f"产品-{product.id}")
    product_folder = root if root.name.casefold() == product_part.casefold() else root / product_part

    requested_tag_ids = {tag_id for item in items for tag_id in item.tag_ids}
    if not requested_tag_ids:
        raise ValueError("每条视频至少选择一个标签")
    tags = session.scalars(
        select(ShotTag).where(ShotTag.id.in_(requested_tag_ids))
    ).all()
    tags_by_id = {tag.id: tag for tag in tags}
    if set(tags_by_id) != requested_tag_ids:
        raise ValueError("部分标签不存在或已停用")

    planned: list[tuple[Path, Path, list[ShotTag], dict]] = []
    reserved: set[Path] = set()
    seen_sources: set[str] = set()
    for item in items:
        source = Path(item.source_path).expanduser().resolve()
        normalized = str(source).casefold()
        if normalized in seen_sources:
            raise ValueError(f"视频重复选择：{source.name}")
        seen_sources.add(normalized)
        if source.parent != root and root not in source.parents:
            raise ValueError(f"视频不在当前素材目录内：{source.name}")
        if not source.is_file() or source.suffix.casefold() not in VIDEO_EXTENSIONS:
            raise ValueError(f"视频不存在或格式不受支持：{source.name}")
        item_tags = [tags_by_id[tag_id] for tag_id in dict.fromkeys(item.tag_ids)]
        if not item_tags:
            raise ValueError(f"视频未选择标签：{source.name}")
        seen_categories: set[str] = set()
        duplicate_category = None
        for tag in item_tags:
            if tag.category_id in seen_categories:
                duplicate_category = tag.category_id
                break
            seen_categories.add(tag.category_id)
        duplicate_category_item = session.get(TagCategory, duplicate_category) if duplicate_category else None
        if duplicate_category is not None and normalize_tag_name(duplicate_category_item.name if duplicate_category_item else "") != normalize_tag_name(FREE_TAG_CATEGORY_NAME):
            raise ValueError(
                f"视频在同一标签分类下只能选择一个标签名称：{source.name}（{duplicate_category}）"
            )
        metadata = probe_video(source)
        tag_part = "-".join(safe_filename_part(tag.name, fallback="标签") for tag in item_tags)
        destination = _available_destination(
            product_folder, f"{product_part}-{tag_part}", source.suffix.casefold(), reserved
        )
        reserved.add(destination)
        planned.append((source, destination, item_tags, metadata))

    product_folder.mkdir(parents=True, exist_ok=True)
    moved: list[tuple[Path, Path]] = []
    try:
        for source, destination, _tags, _metadata in planned:
            shutil.move(str(source), str(destination))
            moved.append((source, destination))
    except Exception as exc:
        for original, destination in reversed(moved):
            if destination.exists() and not original.exists():
                shutil.move(str(destination), str(original))
        raise RuntimeError(f"移动原视频失败，已回退已移动文件：{exc}") from exc

    assets: list[MediaAsset] = []
    try:
        for source, destination, item_tags, metadata in planned:
            asset = MediaAsset(
                product_id=product.id,
                filename=destination.name,
                source_path=str(destination),
                original_source_path=str(source),
                duration_seconds=float(metadata.get("duration_seconds") or 0),
                width=int(metadata.get("width") or 0),
                height=int(metadata.get("height") or 0),
                status="classified",
            )
            session.add(asset)
            session.flush()
            session.add_all(
                [MediaAssetTag(asset_id=asset.id, tag_id=tag.id) for tag in item_tags]
            )
            assets.append(asset)
        session.flush()
    except Exception:
        for original, destination in reversed(moved):
            if destination.exists() and not original.exists():
                shutil.move(str(destination), str(original))
        raise
    return assets
