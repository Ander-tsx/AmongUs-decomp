"""Drive the official AssetRipper local HTTP API. Refuses to overwrite an export."""
import argparse
import urllib.parse
import urllib.request
from pathlib import Path


def post(base, route, **fields):
    request = urllib.request.Request(base + route, data=urllib.parse.urlencode(fields).encode())
    with urllib.request.urlopen(request, timeout=21600) as response:
        return response.status


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--out', type=Path, required=True)
    parser.add_argument('--api', default='http://127.0.0.1:57431')
    args = parser.parse_args()
    if not (args.source / 'GameAssembly.dll').is_file():
        parser.error('Source must be an IL2CPP game installation containing GameAssembly.dll')
    if args.out.exists() and any(args.out.iterdir()):
        parser.error('Output must be absent or empty; keep previous recovery intact')
    print('Loading installation...', flush=True)
    post(args.api, '/LoadFolder', Path=str(args.source.resolve()))
    print('Exporting Unity project; progress is recorded by AssetRipper...', flush=True)
    post(args.api, '/Export/UnityProject', Path=str(args.out.resolve()))
    project = args.out / 'ExportedProject'
    if not (project / 'ProjectSettings/ProjectVersion.txt').is_file():
        raise SystemExit('Export did not produce ProjectVersion.txt. Inspect the AssetRipper log.')
    print(f'Export finished: {project.resolve()}', flush=True)


if __name__ == '__main__':
    main()
