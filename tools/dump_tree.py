"""Print a GameObject subtree of a Unity YAML scene/prefab: names, positions, components, sprites.

Usage: python3 tools/dump_tree.py <file.unity|prefab> <root GameObject name> [max depth]
"""
import os
import re
import sys

sys.path.insert(0, os.path.dirname(__file__))
from prefab_data import load, build_nodes, field, vec

A = 'RecoveredMac/ExportedProject/Assets/'


def build_indexes():
    import json
    cache = '.local/index_cache.json'
    if os.path.exists(cache):
        d = json.load(open(cache))
        return d['cls'], d['spr']
    cls = {}
    for dp, _, fs in os.walk(A + 'Scripts'):
        for f in fs:
            if f.endswith('.cs.meta'):
                cls[re.search(r'^guid: (\w+)', open(os.path.join(dp, f)).read(), re.M).group(1)] = f[:-8]
    spr = {}
    for f in os.listdir(A + 'Sprite'):
        if f.endswith('.asset.meta'):
            spr[re.search(r'^guid: (\w+)', open(A + 'Sprite/' + f).read(), re.M).group(1)] = f[:-11]
    os.makedirs('.local', exist_ok=True)
    json.dump({'cls': cls, 'spr': spr}, open(cache, 'w'))
    return cls, spr


def main():
    path, root = sys.argv[1], sys.argv[2]
    maxd = int(sys.argv[3]) if len(sys.argv) > 3 else 3
    guid2cls, guid2spr = build_indexes()
    objs = load(path)
    nodes = build_nodes(objs)
    kids = {}
    for n in nodes.values():
        kids.setdefault(n.parent.fid if n.parent else None, []).append(n)

    def comps(n):
        out = []
        for c, cid in n.components:
            b = objs[cid][1]
            if c == 114:
                g = re.search(r'm_Script: .*guid: (\w+)', b)
                out.append(guid2cls.get(g.group(1), '?') if g else '?')
            elif c == 212:
                g = re.search(r'm_Sprite: .*guid: (\w+)', b)
                dm = re.search(r'm_DrawMode: (\d)', b); sz = re.search(r'm_Size: \{x: ([^,]+), y: ([^}]+)\}', b)
                extra = f'[draw={dm.group(1)} size=({float(sz.group(1)):.2f},{float(sz.group(2)):.2f})]' if dm and dm.group(1) != '0' and sz else ''
                out.append('SR:' + (guid2spr.get(g.group(1), '-') if g else '-') + extra)
            elif c == 4:
                pass
            else:
                out.append(str(c))
        return out

    def show(n, d):
        sc = '' if all(abs(v-1)<1e-3 for v in n.scale[:2]) else f' scale={[round(v,2) for v in n.scale[:2]]}'
        print('  ' * d + f'{n.name} {"" if n.active else "(off) "}pos={[round(v, 2) for v in n.pos[:3]]}{sc} {comps(n)}')
        if d < maxd:
            for k in kids.get(n.fid, []):
                show(k, d + 1)

    for n in nodes.values():
        if n.name == root:
            show(n, 0)
            break


main()
