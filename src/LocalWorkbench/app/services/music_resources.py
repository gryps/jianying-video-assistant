from __future__ import annotations

import json
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path
from typing import BinaryIO
from urllib.parse import urlparse
from urllib.request import Request, urlopen

from sqlalchemy import func, select
from sqlalchemy.orm import Session

from app.config import settings
from app.domain.models import JianyingDraft, MusicResource
from app.services.music_share_parser import (
    DOUYIN_AUDIO_HOST_SUFFIXES,
    DOUYIN_HOSTS,
    DOUYIN_MEDIA_HOST_SUFFIXES,
    _douyin_media_url,
    _douyin_media_urls,
    _douyin_video_id,
    _extract_shared_url,
    _is_douyin_url,
)


CHROMIUM_USER_AGENT = (
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
    "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36"
)
AUDIO_EXTENSIONS = {".mp3", ".wav", ".m4a", ".aac", ".flac", ".ogg", ".wma"}
VIDEO_EXTENSIONS = {".mp4", ".mov", ".mkv", ".avi", ".webm", ".m4v"}


def _music_root(resource_id: str) -> Path:
    root = Path(settings.workspace_dir) / "music" / resource_id
    root.mkdir(parents=True, exist_ok=True)
    return root


def _probe_duration(path: Path) -> float:
    result = subprocess.run(
        [
            settings.ffprobe_binary,
            "-v",
            "error",
            "-show_entries",
            "format=duration",
            "-of",
            "json",
            str(path),
        ],
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        timeout=120,
    )
    if result.returncode != 0:
        detail = result.stderr.decode("utf-8", errors="replace")[-500:]
        raise RuntimeError(detail or "FFprobe 无法读取音频时长")
    data = json.loads(result.stdout.decode("utf-8"))
    duration = float((data.get("format") or {}).get("duration") or 0)
    if duration <= 0:
        raise RuntimeError("音频时长无效")
    return round(duration, 3)


def _ensure_audible_audio(path: Path) -> None:
    result = subprocess.run(
        [
            settings.ffmpeg_binary,
            "-nostats",
            "-i",
            str(path),
            "-af",
            "volumedetect",
            "-f",
            "null",
            "-",
        ],
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        timeout=600,
    )
    detail = result.stderr.decode("utf-8", errors="replace")
    match = re.search(r"max_volume:\s*(-?\d+(?:\.\d+)?)\s*dB", detail)
    if result.returncode != 0 or match is None:
        raise RuntimeError("无法确认音频是否包含有效声音")
    if float(match.group(1)) <= -80:
        raise RuntimeError("提取结果为静音，未作为可用背景音乐入库")


def _extract_audio(source: Path, target: Path) -> Path:
    result = subprocess.run(
        [
            settings.ffmpeg_binary,
            "-v",
            "error",
            "-y",
            "-i",
            str(source),
            "-vn",
            "-acodec",
            "pcm_s16le",
            "-ar",
            "44100",
            "-ac",
            "2",
            str(target),
        ],
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        timeout=600,
    )
    if result.returncode != 0 or not target.is_file():
        detail = result.stderr.decode("utf-8", errors="replace")[-500:]
        raise RuntimeError(detail or "FFmpeg 无法分离音频")
    return target


def _finish_resource(resource: MusicResource, path: Path) -> None:
    _ensure_audible_audio(path)
    resource.file_path = str(path.resolve())
    resource.duration_seconds = _probe_duration(path)
    resource.status = "ready"
    resource.error = ""


def delete_music_resource(session: Session, resource_id: str) -> dict[str, object]:
    resource = session.get(MusicResource, resource_id)
    if resource is None:
        raise LookupError("音乐资源不存在")
    references = session.scalar(
        select(func.count(JianyingDraft.id)).where(
            JianyingDraft.music_resource_id == resource.id
        )
    ) or 0
    if references:
        raise ValueError(f"音乐仍被 {references} 个剪映草稿引用，不能删除")
    result = {
        "resource_id": resource.id,
        "name": resource.name,
        "storage_root": str(Path(settings.workspace_dir) / "music" / resource.id),
    }
    session.delete(resource)
    session.flush()
    return result


def _installed_browser_candidates() -> tuple[Path, ...]:
    environment_candidates = (
        ("PROGRAMFILES(X86)", "Microsoft/Edge/Application/msedge.exe"),
        ("PROGRAMFILES", "Microsoft/Edge/Application/msedge.exe"),
        ("LOCALAPPDATA", "Microsoft/Edge/Application/msedge.exe"),
        ("PROGRAMFILES", "Google/Chrome/Application/chrome.exe"),
        ("PROGRAMFILES(X86)", "Google/Chrome/Application/chrome.exe"),
        ("LOCALAPPDATA", "Google/Chrome/Application/chrome.exe"),
    )
    native = tuple(
        Path(root) / relative
        for environment_name, relative in environment_candidates
        if (root := os.environ.get(environment_name))
    )
    return native + (
        Path("/mnt/c/Program Files (x86)/Microsoft/Edge/Application/msedge.exe"),
        Path("/mnt/c/Program Files/Microsoft/Edge/Application/msedge.exe"),
        Path("/mnt/c/Program Files/Google/Chrome/Application/chrome.exe"),
    )


def _find_anonymous_chromium() -> Path | None:
    for candidate in _installed_browser_candidates():
        if candidate.is_file():
            return candidate
    for command in (
        "msedge",
        "msedge.exe",
        "microsoft-edge",
        "microsoft-edge-stable",
        "google-chrome",
        "chrome",
        "chrome.exe",
        "chromium",
        "chromium-browser",
    ):
        resolved = shutil.which(command)
        if resolved:
            return Path(resolved)
    return None


def _running_on_windows() -> bool:
    return os.name == "nt"


def _chromium_profile_argument(browser: Path, profile: Path) -> str:
    resolved = str(profile.resolve())
    if browser.suffix.lower() != ".exe" or _running_on_windows():
        return resolved
    try:
        result = subprocess.run(
            ["wslpath", "-w", resolved],
            check=False,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=10,
        )
    except (OSError, subprocess.SubprocessError):
        return resolved
    translated = result.stdout.decode("utf-8", errors="replace").strip()
    return translated or resolved


def _dump_anonymous_browser_dom(
    browser: Path,
    *,
    profile: Path,
    url: str,
    virtual_time_ms: int,
    netlog_path: Path | None = None,
) -> str:
    command = [
        str(browser),
        "--headless",
        "--disable-gpu",
        "--disable-software-rasterizer",
        "--disable-gpu-compositing",
        "--use-angle=swiftshader",
        "--no-sandbox",
        "--disable-dev-shm-usage",
        "--disable-extensions",
        "--disable-background-networking",
        "--no-first-run",
        "--mute-audio",
        f"--user-data-dir={_chromium_profile_argument(browser, profile)}",
    ]
    if netlog_path is not None:
        netlog_path.unlink(missing_ok=True)
        command.extend(
            [
                f"--log-net-log={netlog_path.resolve()}",
                "--net-log-capture-mode=IncludeSensitive",
            ]
        )
    command.extend(
        [
            f"--virtual-time-budget={virtual_time_ms}",
            "--dump-dom",
            url,
        ]
    )
    result = subprocess.run(
        command,
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        timeout=max(60, virtual_time_ms // 1000 + 45),
    )
    document = result.stdout.decode("utf-8", errors="replace")
    if not document:
        detail = result.stderr.decode("utf-8", errors="replace")[-500:]
        raise RuntimeError(detail or "Edge/Chrome 临时浏览器未返回页面内容")
    return document


def _douyin_netlog_media_urls(netlog_path: Path) -> list[str]:
    if not netlog_path.is_file():
        return []
    try:
        payload = json.loads(netlog_path.read_text(encoding="utf-8"))
    except (OSError, ValueError, TypeError):
        return []
    urls: list[str] = []
    seen: set[str] = set()
    for event in payload.get("events", []):
        if not isinstance(event, dict):
            continue
        params = event.get("params")
        value = params.get("url") if isinstance(params, dict) else None
        if not isinstance(value, str) or value in seen:
            continue
        parsed = urlparse(value)
        hostname = (parsed.hostname or "").lower()
        if not any(hostname.endswith(suffix) for suffix in DOUYIN_MEDIA_HOST_SUFFIXES):
            continue
        if len(parsed.path) < 20 or not any(
            marker in parsed.path.lower() for marker in ("media-audio", "media-video", "/video/")
        ):
            continue
        seen.add(value)
        urls.append(value)
    return sorted(urls, key=lambda value: ("media-audio" not in urlparse(value).path.lower(), value))


def _combined_douyin_media_urls(document: str, netlog_path: Path) -> list[str]:
    document_urls = _douyin_media_urls(document)
    return document_urls + [
        value for value in _douyin_netlog_media_urls(netlog_path) if value not in document_urls
    ]


def _download_douyin_media(url: str, target: Path) -> None:
    hostname = (urlparse(url).hostname or "").lower()
    path = urlparse(url).path.lower()
    trusted_video = any(hostname.endswith(suffix) for suffix in DOUYIN_MEDIA_HOST_SUFFIXES)
    trusted_audio = any(hostname.endswith(suffix) for suffix in DOUYIN_AUDIO_HOST_SUFFIXES) and any(
        marker in path for marker in ("ies-music", "music-play", "/music/", "musically-maliva-obj")
    )
    if not trusted_video and not trusted_audio:
        raise RuntimeError("抖音页面返回了不受信任的媒体地址")
    request = Request(
        url,
        headers={
            "User-Agent": CHROMIUM_USER_AGENT,
            "Referer": "https://www.douyin.com/",
            "Range": "bytes=0-",
            "Accept": "*/*",
        },
    )
    with urlopen(request, timeout=300) as response, target.open("wb") as output:
        shutil.copyfileobj(response, output)
    if target.stat().st_size < 1024:
        raise RuntimeError("抖音媒体响应为空或被平台拒绝")


def _extract_douyin_audio(share_url: str, root: Path) -> Path:
    browser = _find_anonymous_chromium()
    if browser is None:
        raise RuntimeError("抖音链接需要 Microsoft Edge、Chrome 或 Chromium 临时浏览器，但本机未找到可用浏览器")
    profile = root / "anonymous-browser-profile"
    profile.mkdir(parents=True, exist_ok=True)
    netlog_path = root / "anonymous-browser-netlog.json"
    source_video = root / "source-video.mp4"
    target_audio = root / "source.wav"
    try:
        document = _dump_anonymous_browser_dom(
            browser,
            profile=profile,
            url=share_url,
            virtual_time_ms=20_000,
            netlog_path=netlog_path,
        )
        media_urls = _combined_douyin_media_urls(document, netlog_path)
        if not media_urls:
            video_id = _douyin_video_id(document)
            if not video_id:
                raise RuntimeError("Edge/Chrome 临时浏览器无法识别抖音作品 ID")
            document = _dump_anonymous_browser_dom(
                browser,
                profile=profile,
                url=f"https://www.douyin.com/video/{video_id}",
                virtual_time_ms=20_000,
                netlog_path=netlog_path,
            )
            media_urls = _combined_douyin_media_urls(document, netlog_path)
        if not media_urls:
            raise RuntimeError("Edge/Chrome 临时浏览器未发现可下载的抖音视频流")
        errors: list[str] = []
        for index, media_url in enumerate(media_urls[:12], start=1):
            source_video.unlink(missing_ok=True)
            target_audio.unlink(missing_ok=True)
            try:
                _download_douyin_media(media_url, source_video)
                _extract_audio(source_video, target_audio)
                _ensure_audible_audio(target_audio)
                return target_audio
            except Exception as exc:
                errors.append(f"候选{index}：{exc}")
        detail = "；".join(errors[-3:])
        raise RuntimeError(f"抖音页面返回的媒体流均无有效声音{f'（{detail}）' if detail else ''}")
    finally:
        source_video.unlink(missing_ok=True)
        netlog_path.unlink(missing_ok=True)
        shutil.rmtree(profile, ignore_errors=True)


def create_uploaded_music(
    session: Session,
    *,
    name: str,
    filename: str,
    stream: BinaryIO,
    rights_confirmed: bool,
) -> MusicResource:
    if not rights_confirmed:
        raise ValueError("必须确认拥有该音频的使用权限")
    suffix = Path(filename).suffix.lower()
    if suffix not in AUDIO_EXTENSIONS | VIDEO_EXTENSIONS:
        raise ValueError("不支持的音频或视频文件格式")
    resource = MusicResource(
        name=name.strip() or Path(filename).stem[:200],
        source_type="upload",
        rights_confirmed=True,
    )
    session.add(resource)
    session.flush()
    root = _music_root(resource.id)
    try:
        target = prepare_uploaded_audio(filename=filename, stream=stream, root=root)
        _finish_resource(resource, target)
    except Exception as exc:
        resource.status = "failed"
        resource.error = f"音频准备失败：{exc}"
    session.flush()
    return resource


def prepare_uploaded_audio(*, filename: str, stream: BinaryIO, root: Path) -> Path:
    suffix = Path(filename).suffix.lower()
    if suffix not in AUDIO_EXTENSIONS | VIDEO_EXTENSIONS:
        raise ValueError("不支持的音频或视频文件格式")
    root.mkdir(parents=True, exist_ok=True)
    source = root / f"upload{suffix}"
    with source.open("wb") as output:
        shutil.copyfileobj(stream, output)
    if source.stat().st_size <= 0:
        raise ValueError("上传的音频或视频为空")
    return _extract_audio(source, root / "source.wav") if suffix in VIDEO_EXTENSIONS else source


def prepare_shared_audio(share_url: str, root: Path) -> Path:
    normalized_url = _extract_shared_url(share_url)
    parsed = urlparse(normalized_url)
    if parsed.scheme not in {"http", "https"} or not parsed.netloc:
        raise ValueError("请填写有效的 HTTP(S) 分享链接")
    root.mkdir(parents=True, exist_ok=True)
    result = subprocess.run(
        [
            sys.executable, "-m", "yt_dlp", "--no-playlist", "--no-progress",
            "-x", "--audio-format", "wav", "-o", str(root / "source.%(ext)s"),
            normalized_url,
        ],
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        timeout=900,
    )
    target = root / "source.wav"
    if result.returncode != 0 or not target.is_file():
        detail = result.stderr.decode("utf-8", errors="replace")[-500:]
        if _is_douyin_url(normalized_url):
            target = _extract_douyin_audio(normalized_url, root)
        else:
            raise RuntimeError(detail or "平台未返回可提取音频")
    return target


def create_link_music(
    session: Session, *, name: str, share_url: str, rights_confirmed: bool
) -> MusicResource:
    if not rights_confirmed:
        raise ValueError("必须确认拥有该音频的使用权限")
    normalized_url = _extract_shared_url(share_url)
    parsed = urlparse(normalized_url)
    if parsed.scheme not in {"http", "https"} or not parsed.netloc:
        raise ValueError("请填写有效的 HTTP(S) 分享链接")
    resource = MusicResource(
        name=name.strip() or parsed.netloc[:200],
        source_type="share_link",
        source_url=normalized_url,
        rights_confirmed=True,
    )
    session.add(resource)
    session.flush()
    root = _music_root(resource.id)
    try:
        target = prepare_shared_audio(resource.source_url, root)
        _finish_resource(resource, target)
    except Exception as exc:
        resource.status = "failed"
        resource.error = f"分享链接提取失败：{exc}。请改为上传本地音频文件。"
    session.flush()
    return resource
