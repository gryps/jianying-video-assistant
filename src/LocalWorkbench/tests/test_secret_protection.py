from __future__ import annotations

import os

import pytest

from app.core.secret_protection import (
    DPAPI_PREFIX,
    SecretProtectionError,
    protect_secret,
    unprotect_secret,
)


@pytest.mark.skipif(os.name != "nt", reason="Windows DPAPI is only available on Windows")
def test_desktop_dpapi_encrypts_and_decrypts_for_current_user(monkeypatch):
    monkeypatch.setenv("PVA_DESKTOP_MODE", "1")
    plaintext = "sk-test-secret-that-must-not-appear-in-storage"
    protected = protect_secret(plaintext)

    assert protected.startswith(DPAPI_PREFIX)
    assert plaintext not in protected
    assert unprotect_secret(protected) == plaintext


@pytest.mark.skipif(os.name != "nt", reason="Windows DPAPI is only available on Windows")
def test_desktop_dpapi_rejects_tampered_ciphertext(monkeypatch):
    monkeypatch.setenv("PVA_DESKTOP_MODE", "1")
    protected = protect_secret("sk-test-secret")
    replacement = "A" if protected[-1] != "A" else "B"

    with pytest.raises(SecretProtectionError):
        unprotect_secret(protected[:-1] + replacement)
