import os
import sys
import zipfile
from pathlib import Path


EXCLUDED_PARTS = {
    "Logs",
    "ProcessVideoAnalyzer.exe.WebView2",
    ".offload",
    "__pycache__",
}


def should_include(path: Path) -> bool:
    if any(part in EXCLUDED_PARTS for part in path.parts):
        return False
    return path.suffix.lower() != ".pyc"


def main() -> int:
    if len(sys.argv) != 3:
        print("usage: create_offline_zip.py <source-folder> <zip-path>")
        return 2

    source = Path(sys.argv[1]).resolve()
    zip_path = Path(sys.argv[2]).resolve()
    root_parent = source.parent

    if not source.is_dir():
        print(f"source folder not found: {source}")
        return 2

    files = [path for path in source.rglob("*") if path.is_file() and should_include(path)]
    total = len(files)
    bytes_total = sum(path.stat().st_size for path in files)
    print(f"creating {zip_path}")
    print(f"files={total} bytes={bytes_total}")

    with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_STORED, allowZip64=True) as archive:
        for index, path in enumerate(files, start=1):
            arcname = path.relative_to(root_parent).as_posix()
            archive.write(path, arcname)
            if index % 1000 == 0:
                print(f"zipping {index}/{total}", flush=True)

    print(f"done bytes={zip_path.stat().st_size}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
