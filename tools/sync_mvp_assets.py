"""Copy the few original assets the Unity 6 MVP reuses from a local AssetRipper export.

Usage: python3 tools/sync_mvp_assets.py [--export RecoveredMac/ExportedProject]
Assets stay out of Git; they are copied from your own legitimately obtained install.
"""
import argparse
import shutil
from pathlib import Path

AUDIO = ['impostor_kill', 'Report_BodyFound', 'alarm_emergencymeeting', 'EjectSplash', 'RoleReveal_Crewmate',
         'RoleReveal_Impostor', 'FootstepMetal01', 'FootstepMetal02', 'FootstepMetal03', 'FootstepMetal04',
         'Maiden Voyage - Theme of Skeld', 'Panel_TempButton', 'Alarm_sabotage', 'door_close', 'eject_text']


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--export', type=Path, default=Path('RecoveredMac/ExportedProject'))
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    src = (root / args.export / 'Assets') if not args.export.is_absolute() else args.export / 'Assets'
    dst = root / 'AmongUsUnity/Assets/Resources'
    (dst / 'Audio').mkdir(parents=True, exist_ok=True)
    (dst / 'Art').mkdir(parents=True, exist_ok=True)
    shutil.copy(src / 'Texture2D/ClassicPlayerAnimations.png', dst / 'Art/ClassicPlayerAnimations.bytes')
    for name in AUDIO:
        shutil.copy(src / 'AudioClip' / f'{name}.ogg', dst / 'Audio' / (name.replace(' ', '_').replace('-', '') + '.ogg'))
    print('Assets copied to', dst)


if __name__ == '__main__':
    main()
