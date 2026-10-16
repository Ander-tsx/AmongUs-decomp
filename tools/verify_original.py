"""Create or verify a portable SHA-256 manifest of the original game files."""
import argparse
import hashlib
import json
from pathlib import Path


def sha256(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path('Original'))
    parser.add_argument('--manifest', type=Path, default=Path('reports/original-manifest.json'))
    parser.add_argument('--create', action='store_true')
    args = parser.parse_args()
    if not args.root.is_dir():
        parser.error('Original installation directory is missing')
    if args.create:
        files = [{'path': p.relative_to(args.root).as_posix(), 'bytes': p.stat().st_size,
                  'sha256': sha256(p)} for p in sorted(args.root.rglob('*')) if p.is_file()]
        document = {'game': 'Among Us', 'steam_app_id': 945360, 'build_id': '25134893',
                    'platform': 'Windows x86_64', 'unity_version': '2022.3.44f1',
                    'file_count': len(files), 'total_bytes': sum(f['bytes'] for f in files), 'files': files}
        args.manifest.parent.mkdir(parents=True, exist_ok=True)
        args.manifest.write_text(json.dumps(document, indent=2), encoding='utf-8')
        print(f'Manifest created: {len(files)} files, {document["total_bytes"]} bytes')
        return
    document = json.loads(args.manifest.read_text(encoding='utf-8'))
    errors = []
    for entry in document['files']:
        path = args.root / entry['path']
        if not path.is_file():
            errors.append('Missing: ' + entry['path'])
        elif path.stat().st_size != entry['bytes'] or sha256(path) != entry['sha256']:
            errors.append('Mismatch (or LFS pointer): ' + entry['path'])
    if errors:
        raise SystemExit('\n'.join(errors))
    print(f'OK: all {len(document["files"])} original files match SHA-256 manifest')


if __name__ == '__main__':
    main()
