from __future__ import annotations

import argparse
import logging
import os
import traceback
from pathlib import Path
import uvicorn
from app.main import app


def main() -> None:
    parser = argparse.ArgumentParser(description="剪映视频助手本地服务")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, required=True)
    args = parser.parse_args()
    runtime = Path(os.environ.get("PVA_RUNTIME_DIR", Path.home() / ".jianying-video-assistant"))
    log_dir = runtime / "logs"
    log_dir.mkdir(parents=True, exist_ok=True)
    log_path = log_dir / "server.log"
    logging.basicConfig(
        filename=log_path,
        level=logging.WARNING,
        encoding="utf-8",
        force=True,
        format="%(asctime)s %(levelname)s %(name)s %(message)s",
    )
    # Uvicorn may create its loggers before main() configures logging. Attach
    # the same file explicitly so request tracebacks are never lost.
    for logger_name in ("uvicorn", "uvicorn.error"):
        logger = logging.getLogger(logger_name)
        if not any(isinstance(handler, logging.FileHandler) and handler.baseFilename == str(log_path) for handler in logger.handlers):
            handler = logging.FileHandler(log_path, encoding="utf-8")
            handler.setFormatter(logging.Formatter("%(asctime)s %(levelname)s %(name)s %(message)s"))
            logger.addHandler(handler)
    uvicorn.run(app, host=args.host, port=args.port, log_level="warning", access_log=False, log_config=None)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        runtime = Path(os.environ.get("PVA_RUNTIME_DIR", Path.home() / ".jianying-video-assistant"))
        log_dir = runtime / "logs"
        log_dir.mkdir(parents=True, exist_ok=True)
        (log_dir / "server-startup-error.log").write_text(traceback.format_exc(), encoding="utf-8")
        raise
