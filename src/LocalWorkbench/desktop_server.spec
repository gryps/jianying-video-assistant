from pathlib import Path

root = Path(SPEC).resolve().parent

a = Analysis(
    [str(root / "desktop_server.py")],
    pathex=[str(root)],
    binaries=[],
    datas=[
        (str(root / "app"), "app"),
        (str(root / "migrations"), "migrations"),
        (str(root / "alembic.ini"), "."),
    ],
    hiddenimports=["uvicorn.logging", "uvicorn.loops.auto", "uvicorn.protocols.http.auto", "uvicorn.protocols.websockets.auto", "uvicorn.lifespan.on"],
    hookspath=[],
    runtime_hooks=[],
    excludes=["pytest", "psycopg"],
    noarchive=False,
)
pyz = PYZ(a.pure)
exe = EXE(pyz, a.scripts, [], exclude_binaries=True, name="JianyingVideoAssistant.Server", console=False)
coll = COLLECT(exe, a.binaries, a.datas, strip=False, upx=False, name="JianyingVideoAssistant.Server")
