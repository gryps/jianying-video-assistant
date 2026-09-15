from __future__ import annotations

import base64
import ctypes
import os
from ctypes import wintypes


DPAPI_PREFIX = "dpapi:v1:"
CRYPTPROTECT_UI_FORBIDDEN = 0x01
_ENTROPY = b"JianyingVideoAssistant:model-api-key:v1"


class SecretProtectionError(RuntimeError):
    """Raised when a locally protected secret cannot be encrypted or decrypted."""


class _DataBlob(ctypes.Structure):
    _fields_ = [("cbData", wintypes.DWORD), ("pbData", ctypes.POINTER(ctypes.c_byte))]


def _desktop_dpapi_enabled() -> bool:
    return os.name == "nt" and os.environ.get("PVA_DESKTOP_MODE") == "1"


def is_protected_secret(value: str) -> bool:
    return value.startswith(DPAPI_PREFIX)


def _blob(value: bytes) -> tuple[_DataBlob, ctypes.Array]:
    buffer = ctypes.create_string_buffer(value)
    return _DataBlob(len(value), ctypes.cast(buffer, ctypes.POINTER(ctypes.c_byte))), buffer


def _crypt_protect(value: bytes) -> bytes:
    data, data_buffer = _blob(value)
    entropy, entropy_buffer = _blob(_ENTROPY)
    output = _DataBlob()
    crypt32 = ctypes.WinDLL("crypt32", use_last_error=True)
    kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel32.LocalFree.argtypes = [wintypes.HLOCAL]
    kernel32.LocalFree.restype = wintypes.HLOCAL
    crypt32.CryptProtectData.argtypes = [
        ctypes.POINTER(_DataBlob),
        wintypes.LPCWSTR,
        ctypes.POINTER(_DataBlob),
        ctypes.c_void_p,
        ctypes.c_void_p,
        wintypes.DWORD,
        ctypes.POINTER(_DataBlob),
    ]
    crypt32.CryptProtectData.restype = wintypes.BOOL
    if not crypt32.CryptProtectData(
        ctypes.byref(data),
        "剪映视频助手模型密钥",
        ctypes.byref(entropy),
        None,
        None,
        CRYPTPROTECT_UI_FORBIDDEN,
        ctypes.byref(output),
    ):
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        return ctypes.string_at(output.pbData, output.cbData)
    finally:
        kernel32.LocalFree(ctypes.cast(output.pbData, wintypes.HLOCAL))


def _crypt_unprotect(value: bytes) -> bytes:
    data, data_buffer = _blob(value)
    entropy, entropy_buffer = _blob(_ENTROPY)
    output = _DataBlob()
    crypt32 = ctypes.WinDLL("crypt32", use_last_error=True)
    kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel32.LocalFree.argtypes = [wintypes.HLOCAL]
    kernel32.LocalFree.restype = wintypes.HLOCAL
    crypt32.CryptUnprotectData.argtypes = [
        ctypes.POINTER(_DataBlob),
        ctypes.POINTER(wintypes.LPWSTR),
        ctypes.POINTER(_DataBlob),
        ctypes.c_void_p,
        ctypes.c_void_p,
        wintypes.DWORD,
        ctypes.POINTER(_DataBlob),
    ]
    crypt32.CryptUnprotectData.restype = wintypes.BOOL
    if not crypt32.CryptUnprotectData(
        ctypes.byref(data),
        None,
        ctypes.byref(entropy),
        None,
        None,
        CRYPTPROTECT_UI_FORBIDDEN,
        ctypes.byref(output),
    ):
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        return ctypes.string_at(output.pbData, output.cbData)
    finally:
        kernel32.LocalFree(ctypes.cast(output.pbData, wintypes.HLOCAL))


def protect_secret(value: str) -> str:
    clean = value.strip()
    if not clean or is_protected_secret(clean) or not _desktop_dpapi_enabled():
        return clean
    try:
        encrypted = _crypt_protect(clean.encode("utf-8"))
    except OSError as exc:
        raise SecretProtectionError("Windows 无法加密 API Key，请检查当前用户配置后重试") from exc
    return DPAPI_PREFIX + base64.b64encode(encrypted).decode("ascii")


def unprotect_secret(value: str) -> str:
    clean = value.strip()
    if not clean or not is_protected_secret(clean):
        return clean
    if not _desktop_dpapi_enabled():
        raise SecretProtectionError("此 API Key 只能由保存它的 Windows 用户在原电脑上解密")
    try:
        encrypted = base64.b64decode(clean[len(DPAPI_PREFIX) :], validate=True)
        return _crypt_unprotect(encrypted).decode("utf-8")
    except (OSError, ValueError, UnicodeDecodeError) as exc:
        raise SecretProtectionError("此 API Key 无法由当前 Windows 用户解密，请重新填写") from exc
