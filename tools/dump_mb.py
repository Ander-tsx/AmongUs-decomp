"""Print the non-engine MonoBehaviours of a prefab with their serialized fields (refs resolved to GameObject names).

Usage: python3 tools/dump_mb.py <prefab> [ClassName ...]
"""
import os
import re
import sys

sys.path.insert(0, os.path.dirname(__file__))
from prefab_data import load, build_nodes
from dump_tree import build_indexes

SKIP = {'PassiveButton', 'ButtonBehavior', 'ActionMapGlyphDisplay', 'ControllerButtonBehavior', 'TextTranslatorTMP',
        'TextMeshPro', 'TouchpadBehavior', 'ScrollingText', 'AspectPosition', 'SpriteAnimNodes'}

path = sys.argv[1]
only = set(sys.argv[2:])
cls_idx, _ = build_indexes()
import json
ALL = '.local/guid_all.json'
if os.path.exists(ALL):
    guid_all = json.load(open(ALL))
else:
    guid_all = {}
    A = 'RecoveredMac/ExportedProject/Assets/'
    for dp, _, fs in os.walk(A):
        for f in fs:
            if f.endswith('.meta') and not f.endswith('.cs.meta'):
                try:
                    m = re.search(r'^guid: (\w+)', open(os.path.join(dp, f)).read(400), re.M)
                except Exception:
                    continue
                if m:
                    guid_all[m.group(1)] = f[:-5]
    json.dump(guid_all, open(ALL, 'w'))
objs = load(path)
nodes = build_nodes(objs)
go_of = {}
for n in nodes.values():
    for c, cid in n.components:
        go_of[cid] = n
for fid, (c, b) in objs.items():
    if c != 114:
        continue
    g = re.search(r'm_Script: .*guid: (\w+)', b)
    cls = cls_idx.get(g.group(1), '?') if g else '?'
    if cls in SKIP or cls == '?' or (only and cls not in only):
        continue
    print(f'== {cls} on {go_of[fid].name if fid in go_of else "?"}')
    lines = b.split('\n')
    i = 0
    for line in lines[9:]:
        m = re.match(r'(\s*)([A-Za-z_0-9]+):\s?(.*)$', line)
        if not m:
            if line.strip().startswith('- {fileID'):
                r = re.search(r'fileID: (\d+)', line)
                g2 = re.search(r'guid: (\w+)', line)
                if r and r.group(1) in go_of:
                    line = line.split('{')[0] + '-> ' + go_of[r.group(1)].name
                elif g2:
                    line = line.split('{')[0] + '@' + guid_all.get(g2.group(1), g2.group(1))
                print(line[:140])
            continue
        val = m.group(3)
        r = re.match(r'\{fileID: (\d+)', val)
        if r and r.group(1) in go_of:
            val = '-> ' + go_of[r.group(1)].name
        elif 'guid:' in val:
            g2 = re.search(r'guid: (\w+)', val)
            val = '@' + guid_all.get(g2.group(1), g2.group(1)) if g2 else val[:60]
        if m.group(2) in ('m_Name', 'm_EditorClassIdentifier'):
            continue
        print(f'{m.group(1)}{m.group(2)}: {val}'[:140])
