"""Copy a prefab and its asset dependencies from the AssetRipper export into the Unity 6 project.

GUIDs are preserved (files are copied with their .meta) so prefab/sprite references keep resolving.
Usage: python3 tools/migrate_map.py Ship/SkeldShip.prefab AnimationClip/Walk.anim ...
"""
import argparse
import shutil
from pathlib import Path

from asset_closure import closure, index_guids


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('roots', nargs='*', help='asset paths relative to the export Assets folder (prefabs, clips, ...)')
    ap.add_argument('--sprites', type=Path, help='text file with sprite names (one per line) to migrate from Sprite/')
    ap.add_argument('--export', type=Path, default=Path('RecoveredMac/ExportedProject/Assets'))
    ap.add_argument('--dest', type=Path, default=Path('AmongUsUnity/Assets/Original'))
    args = ap.parse_args()
    idx = index_guids(args.export)
    roots = [args.export / r for r in args.roots]
    if args.sprites:
        roots += [args.export / 'Sprite' / (n.strip() + '.asset') for n in args.sprites.read_text().split() if n.strip()]
    roots = [r for r in roots if r.exists() or print('missing', r)]
    files = closure(roots, idx, skip_ext=('.otf',))
    n = 0
    for f in files:
        rel = f.relative_to(args.export)
        for src in (f, f.with_name(f.name + '.meta')):
            if src.exists() and src.is_file():
                dst = args.dest / src.relative_to(args.export)
                dst.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(src, dst)
                n += 1
    # Replace the exported dummy shaders with working ones (same file names, so GUIDs stay valid).
    overrides = args.dest.parent / 'ShaderOverrides'
    for shader in overrides.glob('*.shader'):
        target = args.dest / 'Shader' / shader.name
        if target.exists():
            shutil.copy2(shader, target)
            n += 1
    print(f'copied {n} files ({len(files)} assets) to {args.dest}')


if __name__ == '__main__':
    main()
