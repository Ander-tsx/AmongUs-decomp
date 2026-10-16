"""Inventory local recovery without treating generated method stubs as source recovery."""
import argparse
import collections
import hashlib
import json
from pathlib import Path


def inventory(root):
    counts = collections.Counter()
    sizes = collections.Counter()
    files = []
    for path in sorted(root.rglob('*')):
        if not path.is_file():
            continue
        size = path.stat().st_size
        ext = path.suffix.lower() or '<none>'
        counts[ext] += 1
        sizes[ext] += size
        files.append({'path': path.relative_to(root).as_posix(), 'bytes': size})
    return {'root': str(root.resolve()), 'file_count': len(files),
            'bytes': sum(sizes.values()), 'extensions': dict(counts),
            'bytes_by_extension': dict(sizes), 'files': files}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path, default=Path('Original'))
    parser.add_argument('--project', type=Path, default=Path('Recovered/ExportedProject'))
    parser.add_argument('--out', type=Path, default=Path('.local/recovery-audit.json'))
    args = parser.parse_args()
    if not args.source.is_dir() or not args.project.is_dir():
        parser.error('Both source and exported project must exist')
    report = {'source': inventory(args.source), 'project': inventory(args.project)}
    evidence = []
    for name in ('GameAssembly.dll', 'Among Us_Data/il2cpp_data/Metadata/global-metadata.dat'):
        path = args.source / name
        if path.exists():
            with path.open('rb') as stream:
                digest = hashlib.file_digest(stream, 'sha256').hexdigest()
            evidence.append({'path': name, 'sha256': digest})
    report['binary_evidence'] = evidence
    scripts = list(args.project.rglob('*.cs'))
    report['script_count'] = len(scripts)
    report['stub_markers'] = {}
    markers = ('throw new NotImplementedException', 'throw null', 'Cpp2ILInjected', 'return default')
    report['stub_markers'] = dict.fromkeys(markers, 0)
    for path in scripts:
        content = path.read_text(encoding='utf-8-sig', errors='replace')
        for marker in markers:
            report['stub_markers'][marker] += content.count(marker)
    report['validation'] = {'unity_compilation': 'not_verified', 'gameplay_parity': 'not_verified', 'lan_multiplayer': 'not_verified'}
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding='utf-8')
    print(json.dumps({k: {a: b for a, b in v.items() if a != 'files'} if k in ('source', 'project') else v for k, v in report.items()}, indent=2))


if __name__ == '__main__':
    main()
