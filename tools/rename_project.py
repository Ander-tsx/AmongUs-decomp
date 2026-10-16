"""Change Unity product metadata while preserving recovered game assets."""
import argparse
from pathlib import Path
import re


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, default=Path('Recovered/ExportedProject'))
    args = parser.parse_args()
    settings = args.project / 'ProjectSettings/ProjectSettings.asset'
    text = settings.read_text(encoding='utf-8-sig')
    replacement = '  productName: Entre nosotros hay un impostor'
    updated, count = re.subn(r'^  productName:.*$', replacement, text, flags=re.MULTILINE)
    if count != 1:
        raise SystemExit(f'Expected one productName entry, found {count}; no changes written')
    settings.write_text(updated, encoding='utf-8')
    print(replacement.strip())
    print('Only Unity product metadata changed. In-game artwork/title require separate editing.')


if __name__ == '__main__':
    main()
