"""Give a hand-written MonoBehaviour the GUID of the exported stub so migrated prefabs bind to it.

Usage: python3 tools/bind_script_guid.py AmongUsUnity/Assets/Scripts/Original/Console.cs ...
"""
import shutil
import sys
from pathlib import Path

STUBS = Path('RecoveredMac/ExportedProject/Assets/Scripts/Assembly-CSharp')

for arg in sys.argv[1:]:
    cs = Path(arg)
    matches = list(STUBS.rglob(cs.stem + '.cs.meta'))
    if not matches:
        print('no stub meta for', cs.stem)
        continue
    shutil.copy(matches[0], cs.with_name(cs.name + '.meta'))
    print('bound', cs.stem)
