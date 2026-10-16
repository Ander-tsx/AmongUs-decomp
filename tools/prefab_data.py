"""Parse a Unity YAML prefab: hierarchy, world transforms and MonoBehaviour fields (stdlib only)."""
import math
import re
from pathlib import Path


def load(path):
    text = Path(path).read_text()
    parts = re.split(r'(?m)^--- !u!(\d+) &(\d+)\n', text)
    objs = {}
    for i in range(1, len(parts), 3):
        objs[parts[i + 1]] = (int(parts[i]), parts[i + 2])
    return objs


def field(body, name, default=None):
    m = re.search(r'(?m)^\s*%s: (.*)$' % re.escape(name), body)
    return m.group(1).strip() if m else default


def vec(body, name):
    m = re.search(r'(?m)^\s*%s: \{x: ([^,]+), y: ([^,}]+)(?:, z: ([^,}]+))?' % re.escape(name), body)
    if not m:
        return None
    return [float(m.group(1)), float(m.group(2)), float(m.group(3) or 0)]


def quat_z(body):
    m = re.search(r'm_LocalRotation: \{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}', body)
    if not m:
        return 0.0
    x, y, z, w = map(float, m.groups())
    return math.atan2(2 * (w * z + x * y), 1 - 2 * (y * y + z * z))


class Node:
    def __init__(self, fid, go_body, tr_id, tr_body):
        self.fid = fid
        self.name = field(go_body, 'm_Name')
        self.active = field(go_body, 'm_IsActive', '1') == '1'
        self.tr_id = tr_id
        self.pos = vec(tr_body, 'm_LocalPosition') or [0, 0, 0]
        self.scale = vec(tr_body, 'm_LocalScale') or [1, 1, 1]
        self.rot = quat_z(tr_body)
        m = re.search(r'm_Father: \{fileID: (\d+)\}', tr_body)
        self.father_tr = m.group(1) if m else '0'
        self.components = []
        self.parent = None

    def world(self):
        """World position, accumulating parent scale and Z rotation."""
        chain = []
        n = self
        while n:
            chain.append(n)
            n = n.parent
        x = y = 0.0
        # transform local point through the chain from the root down
        px, py = 0.0, 0.0
        pos = [0.0, 0.0]
        sx, sy, rot = 1.0, 1.0, 0.0
        for n in reversed(chain):
            lx, ly = n.pos[0] * sx, n.pos[1] * sy
            c, s = math.cos(rot), math.sin(rot)
            pos[0] += lx * c - ly * s
            pos[1] += lx * s + ly * c
            sx *= n.scale[0]
            sy *= n.scale[1]
            rot += n.rot
        return pos


def build_nodes(objs):
    go_to_tr = {}
    nodes_by_tr = {}
    for fid, (cls, body) in objs.items():
        if cls == 4:
            m = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
            go_to_tr[m.group(1)] = fid
    by_go = {}
    for fid, (cls, body) in objs.items():
        if cls == 1:
            tr = go_to_tr.get(fid)
            if tr:
                n = Node(fid, body, tr, objs[tr][1])
                by_go[fid] = n
                nodes_by_tr[tr] = n
                comps = re.findall(r'component: \{fileID: (\d+)\}', body)
                n.components = [(objs[c][0], c) for c in comps if c in objs]
    for n in by_go.values():
        n.parent = nodes_by_tr.get(n.father_tr)
    return by_go
