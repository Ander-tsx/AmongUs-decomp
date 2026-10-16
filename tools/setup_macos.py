"""Download pinned official analysis tools for this Mac; Python 3.11+ required."""
import json
import platform
import subprocess
import sys
import urllib.request
from pathlib import Path


def download(url, path):
    print(f'Downloading {path.name}', flush=True)
    with urllib.request.urlopen(url, timeout=120) as response, path.open('wb') as output:
        while chunk := response.read(1024 * 1024):
            output.write(chunk)


def main():
    if sys.platform != 'darwin':
        raise SystemExit('Run this script on the destination MacBook')
    root = Path(__file__).resolve().parents[1] / '.local/tools'
    root.mkdir(parents=True, exist_ok=True)
    arm = platform.machine().lower() in ('arm64', 'aarch64')
    arch = 'arm64' if arm else 'x64'
    archive = root / f'AssetRipper_mac_{arch}.tar.xz'
    download(f'https://github.com/AssetRipper/AssetRipper/releases/download/2.0.0/{archive.name}', archive)
    destination = root / 'AssetRipper'
    destination.mkdir(exist_ok=True)
    subprocess.run(['tar', '-xJf', str(archive), '-C', str(destination)], check=True)
    name = 'Cpp2IL-2022.1.0-pre-release.21-OSX' + ('-ARM64' if arm else '')
    cpp = root / 'Cpp2IL'
    download(f'https://github.com/SamboyCoding/Cpp2IL/releases/download/2022.1.0-pre-release.21/{name}', cpp)
    cpp.chmod(cpp.stat().st_mode | 0o111)
    print('Tools downloaded. No recovered game functionality is implied.')
    for path in destination.rglob('*'):
        if path.is_file() and path.name.startswith('AssetRipper') and not path.suffix:
            path.chmod(path.stat().st_mode | 0o111)
            print('AssetRipper executable:', path)


if __name__ == '__main__':
    main()
