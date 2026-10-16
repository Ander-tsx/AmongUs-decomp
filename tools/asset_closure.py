"""Dependency closure of Unity YAML assets in an AssetRipper export, keyed by GUID.

Used to migrate a prefab (e.g. the Skeld ship) with its sprites, textures, animations and materials while
keeping GUIDs stable. Scripts (.cs) are not followed: the exported ones are empty stubs.
"""
import os
import re
import sys
from pathlib import Path

GUID_RE = re.compile(r'guid: ([0-9a-f]{32})')
META_GUID_RE = re.compile(r'^guid: ([0-9a-f]{32})', re.M)
SKIP_EXT = {'.cs', '.dll'}


def index_guids(assets: Path):
    index = {}
    for dirpath, _, files in os.walk(assets):
        for f in files:
            if f.endswith('.meta'):
                p = Path(dirpath) / f
                try:
                    m = META_GUID_RE.search(p.read_text(errors='ignore')[:400])
                except OSError:
                    continue
                if m:
                    index[m.group(1)] = Path(dirpath) / f[:-5]
    return index


def closure(roots, index, follow_binary=False, skip_ext=(), no_follow_ext=('.prefab',)):
    seen, order, queue = set(), [], list(roots)
    roots = set(roots)
    while queue:
        path = queue.pop()
        if path in seen:
            continue
        seen.add(path)
        if path.suffix in skip_ext:
            continue
        order.append(path)
        if path.suffix in SKIP_EXT or path.is_dir():
            continue
        if path.suffix in no_follow_ext and path not in roots:
            continue
        if path.suffix in ('.png', '.ogg', '.wav', '.ttf', '.jpg', '.psd', '.tga') and not follow_binary:
            continue
        try:
            head = path.read_bytes()[:20]
            if not head.startswith(b'%YAML'):
                continue
            text = path.read_text(errors='ignore')
        except OSError:
            continue
        for g in set(GUID_RE.findall(text)):
            dep = index.get(g)
            if dep is not None and dep not in seen and dep.suffix not in SKIP_EXT:
                queue.append(dep)
    return order


if __name__ == '__main__':
    assets = Path(sys.argv[1])
    root = Path(sys.argv[2])
    idx = index_guids(assets)
    print('guids indexed:', len(idx))
    files = closure([root], idx)
    by_ext = {}
    for f in files:
        by_ext.setdefault(f.suffix, [0, 0])
        by_ext[f.suffix][0] += 1
        by_ext[f.suffix][1] += f.stat().st_size if f.exists() else 0
    for k, v in sorted(by_ext.items(), key=lambda kv: -kv[1][1]):
        print(f'{k or "(none)":10} {v[0]:6} {v[1]/1e6:9.1f} MB')
