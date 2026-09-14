from __future__ import annotations

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy import func, select

from app.api.v1.schemas import (
    ProductCategoryCreateRequest,
    ProductCategoryResponse,
    ProductCategoryUpdateRequest,
    ProductCreateRequest,
    ProductPageResponse,
    ProductResponse,
    ProductUpdateRequest,
)
from app.core.database import session_scope
from app.core.security import utc_now
from app.domain.models import AdminUser, MediaAsset, Product, ProductCategory
from app.services.audit import record_audit
from app.services.auth import require_admin
from app.services.product_library import create_product, duplicate_product_name, product_code

router = APIRouter(prefix="/products", tags=["products"])


def product_response(session, product: Product) -> ProductResponse:
    asset_count = session.scalar(select(func.count(MediaAsset.id)).where(MediaAsset.product_id == product.id)) or 0
    code = product_code(product.id)
    category = session.get(ProductCategory, product.category_id) if product.category_id else None
    return ProductResponse(
        id=product.id,
        system_code=code,
        name=product.name,
        status=product.status,
        asset_count=asset_count,
        category_id=product.category_id,
        category_name=category.name if category else "未分类",
        created_at=product.created_at,
        updated_at=product.updated_at,
    )


def _category_or_404(session, category_id: str | None) -> ProductCategory | None:
    if not category_id:
        return None
    category = session.get(ProductCategory, category_id)
    if category is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="产品分类不存在")
    return category


@router.get("/page", response_model=ProductPageResponse)
def page_products(
    page: int = 1,
    page_size: int = 20,
    query: str = "",
    category_id: str = "",
    _admin: AdminUser = Depends(require_admin),
) -> ProductPageResponse:
    page = max(1, page)
    page_size = min(100, max(10, page_size))
    with session_scope() as session:
        filters = [Product.status == "active"]
        if query.strip():
            filters.append(Product.name.contains(query.strip()))
        if category_id:
            filters.append(Product.category_id == category_id)
        total = session.scalar(select(func.count(Product.id)).where(*filters)) or 0
        statement = (
            select(Product)
            .where(*filters)
            .order_by(Product.updated_at.desc(), Product.id.desc())
            .offset((page - 1) * page_size)
            .limit(page_size)
        )
        items = [product_response(session, item) for item in session.scalars(statement).all()]
        pages = max(1, (total + page_size - 1) // page_size)
        return ProductPageResponse(items=items, page=min(page, pages), page_size=page_size, total=total, pages=pages)


@router.get("/categories", response_model=list[ProductCategoryResponse])
def list_product_categories(_admin: AdminUser = Depends(require_admin)) -> list[ProductCategoryResponse]:
    with session_scope() as session:
        rows = session.scalars(select(ProductCategory).order_by(ProductCategory.name)).all()
        return [ProductCategoryResponse(
            id=item.id,
            name=item.name,
            product_count=session.scalar(select(func.count(Product.id)).where(Product.category_id == item.id, Product.status == "active")) or 0,
        ) for item in rows]


@router.post("/categories", response_model=ProductCategoryResponse, status_code=status.HTTP_201_CREATED)
def add_product_category(payload: ProductCategoryCreateRequest, _admin: AdminUser = Depends(require_admin)) -> ProductCategoryResponse:
    name = payload.name.strip()
    with session_scope() as session:
        duplicate = session.scalar(select(ProductCategory).where(func.lower(ProductCategory.name) == name.casefold()))
        if duplicate:
            raise HTTPException(status_code=409, detail=f"产品分类“{duplicate.name}”已存在")
        item = ProductCategory(name=name)
        session.add(item)
        session.flush()
        return ProductCategoryResponse(id=item.id, name=item.name, product_count=0)


@router.patch("/categories/{category_id}", response_model=ProductCategoryResponse)
def update_product_category(category_id: str, payload: ProductCategoryUpdateRequest, _admin: AdminUser = Depends(require_admin)) -> ProductCategoryResponse:
    name = payload.name.strip()
    with session_scope() as session:
        item = _category_or_404(session, category_id)
        duplicate = session.scalar(select(ProductCategory).where(func.lower(ProductCategory.name) == name.casefold(), ProductCategory.id != category_id))
        if duplicate:
            raise HTTPException(status_code=409, detail=f"产品分类“{duplicate.name}”已存在")
        assert item is not None
        item.name = name
        item.updated_at = utc_now()
        count = session.scalar(select(func.count(Product.id)).where(Product.category_id == item.id, Product.status == "active")) or 0
        return ProductCategoryResponse(id=item.id, name=item.name, product_count=count)


@router.delete("/categories/{category_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_product_category(category_id: str, _admin: AdminUser = Depends(require_admin)) -> None:
    with session_scope() as session:
        item = _category_or_404(session, category_id)
        count = session.scalar(select(func.count(Product.id)).where(Product.category_id == category_id, Product.status == "active")) or 0
        if count:
            raise HTTPException(status_code=409, detail=f"该分类下还有 {count} 个产品，请先调整产品分类")
        assert item is not None
        session.delete(item)


@router.get("", response_model=list[ProductResponse])
def list_products(include_inactive: bool = True, _admin: AdminUser = Depends(require_admin)) -> list[ProductResponse]:
    with session_scope() as session:
        statement = select(Product).where(Product.status != "deleted").order_by(Product.id)
        if not include_inactive:
            statement = statement.where(Product.status == "active")
        products = session.scalars(statement).all()
        return [product_response(session, product) for product in products]


@router.post("", response_model=ProductResponse, status_code=status.HTTP_201_CREATED)
def add_product(payload: ProductCreateRequest, admin: AdminUser = Depends(require_admin)) -> ProductResponse:
    with session_scope() as session:
        _category_or_404(session, payload.category_id)
        duplicate = duplicate_product_name(session, payload.name)
        if duplicate is not None:
            raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail=f"产品名称“{duplicate.name}”已存在，请直接选择已有产品")
        product = create_product(session, name=payload.name)
        product.category_id = payload.category_id
        record_audit(
            session,
            actor_id=admin.id,
            action="product.create",
            object_type="product",
            object_id=str(product.id),
            after={"system_code": product_code(product.id), "name": product.name},
        )
        return product_response(session, product)


@router.patch("/{product_id}", response_model=ProductResponse)
def update_product(product_id: int, payload: ProductUpdateRequest, admin: AdminUser = Depends(require_admin)) -> ProductResponse:
    with session_scope() as session:
        product = session.get(Product, product_id)
        if product is None:
            raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="产品不存在")
        if product.status == "merged":
            raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="已合并产品不能修改")
        name = payload.name.strip()
        if "category_id" in payload.model_fields_set:
            _category_or_404(session, payload.category_id)
        duplicate = duplicate_product_name(session, name, exclude_product_id=product.id)
        if duplicate is not None:
            raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail=f"产品名称“{duplicate.name}”已存在")
        before = {"name": product.name}
        product.name = name
        if "category_id" in payload.model_fields_set:
            product.category_id = payload.category_id
        product.updated_at = utc_now()
        record_audit(
            session,
            actor_id=admin.id,
            action="product.update",
            object_type="product",
            object_id=str(product.id),
            before=before,
            after={"name": name},
        )
        session.flush()
        return product_response(session, product)
