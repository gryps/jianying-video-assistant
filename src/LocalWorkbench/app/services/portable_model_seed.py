from __future__ import annotations

import base64
import json
import os
from pathlib import Path
from typing import Any

from cryptography.hazmat.primitives.ciphers.aead import AESGCM

from app.ai import PROFILE_STAGES, load_model_profiles, save_model_profiles
from app.models import ModelProfile


PORTABLE_MODEL_SEED_ENV = "PVA_MODEL_PROFILE_SEED_PATH"
PORTABLE_MODEL_SEED_FILENAME = "model-profiles.seed"
_SEED_VERSION = 1
_SEED_AAD = b"JianyingVideoAssistant:model-profiles:portable:v1"
# This application-held key provides encrypted-at-rest packaging, not copy protection.
# Anyone who receives the complete application package can use the bundled API key.
_SEED_KEY = base64.b64decode("1jU6RTUJhYIKlkcg0GIkmjqJ95B6qJESQ0M5SwnUG2o=")


def _profile_payload(profile: ModelProfile) -> dict[str, Any]:
    return {
        "stage": profile.stage,
        "label": profile.label,
        "provider_type": profile.provider_type,
        "protocol": profile.protocol,
        "capabilities": list(profile.capabilities),
        "base_url": profile.base_url,
        "model": profile.model,
        "temperature": profile.temperature,
        "proxy_url": profile.proxy_url,
        "api_key": profile.api_key,
    }


def export_portable_model_seed(path: Path) -> None:
    profiles = load_model_profiles(include_api_key=True)
    if not any(profile.api_key.strip() for profile in profiles):
        raise ValueError("当前模型配置没有可导出的 API Key")
    plaintext = json.dumps(
        {"version": _SEED_VERSION, "profiles": [_profile_payload(profile) for profile in profiles]},
        ensure_ascii=False,
        separators=(",", ":"),
    ).encode("utf-8")
    nonce = os.urandom(12)
    ciphertext = AESGCM(_SEED_KEY).encrypt(nonce, plaintext, _SEED_AAD)
    document = json.dumps(
        {
            "version": _SEED_VERSION,
            "algorithm": "AES-256-GCM",
            "nonce": base64.b64encode(nonce).decode("ascii"),
            "ciphertext": base64.b64encode(ciphertext).decode("ascii"),
        },
        separators=(",", ":"),
    ).encode("utf-8")
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_bytes(document)
    temporary.replace(path)


def _read_portable_model_seed(path: Path) -> list[ModelProfile]:
    document = json.loads(path.read_text(encoding="utf-8"))
    if document.get("version") != _SEED_VERSION or document.get("algorithm") != "AES-256-GCM":
        raise ValueError("模型配置种子版本不受支持")
    nonce = base64.b64decode(str(document["nonce"]), validate=True)
    ciphertext = base64.b64decode(str(document["ciphertext"]), validate=True)
    plaintext = AESGCM(_SEED_KEY).decrypt(nonce, ciphertext, _SEED_AAD)
    payload = json.loads(plaintext.decode("utf-8"))
    expected_stages = {stage for stage, _label in PROFILE_STAGES}
    profiles = [ModelProfile.model_validate(item) for item in payload.get("profiles", [])]
    if {profile.stage for profile in profiles} != expected_stages:
        raise ValueError("模型配置种子内容不完整")
    if not any(profile.api_key.strip() for profile in profiles):
        raise ValueError("模型配置种子不包含 API Key")
    return profiles


def import_packaged_model_seed(path: Path | None = None) -> bool:
    seed_path_value = os.environ.pop(PORTABLE_MODEL_SEED_ENV, "")
    seed_path = path or (Path(seed_path_value) if seed_path_value else None)
    if seed_path is None or not seed_path.is_file():
        return False
    current = load_model_profiles(include_api_key=True)
    if any(profile.api_key.strip() or profile.base_url.strip() or profile.model.strip() for profile in current):
        seed_path.unlink(missing_ok=True)
        return False
    profiles = _read_portable_model_seed(seed_path)
    save_model_profiles(profiles)
    seed_path.unlink(missing_ok=True)
    return True
