"""Create a game-only ZIP and verify its contents against the tracked manifest."""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile


def digest(stream):
    return hashlib.file_digest(stream, 'sha256').hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--out', type=Path, default=Path('.local/transfer/AmongUs-original-Windows.zip'))
    args = parser.parse_args()
    manifest = json.loads(Path('reports/original-manifest.json').read_text(encoding='utf-8'))
    args.out.parent.mkdir(parents=True, exist_ok=True)
    temporary = args.out.with_suffix('.zip.part')
    with zipfile.ZipFile(temporary, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=3, allowZip64=True) as archive:
        for entry in manifest['files']:
            path = Path('Original') / entry['path']
            archive.write(path, 'Original/' + entry['path'])
    print('Compression finished; verifying all original file hashes...', flush=True)
    with zipfile.ZipFile(temporary) as archive:
        expected = {'Original/' + entry['path'] for entry in manifest['files']}
        if len(archive.infolist()) != len(expected) or set(archive.namelist()) != expected:
            raise RuntimeError('Archive entries differ from original game manifest')
        for entry in manifest['files']:
            with archive.open('Original/' + entry['path']) as stream:
                if digest(stream) != entry['sha256']:
                    raise RuntimeError('SHA-256 mismatch: ' + entry['path'])
    temporary.replace(args.out)
    with args.out.open('rb') as stream:
        checksum = digest(stream)
    args.out.with_suffix('.zip.sha256').write_text(checksum + '  ' + args.out.name + '\n', encoding='ascii')
    print(f'ZIP verified: {len(manifest["files"])} game files, {args.out.stat().st_size} bytes', flush=True)
    print(args.out.resolve(), flush=True)
    print('SHA-256: ' + checksum, flush=True)


if __name__ == '__main__':
    main()
