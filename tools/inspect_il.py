"""Measure recovered managed method bodies (requires dnfile and dncil)."""
import argparse
from collections import Counter
import json
from pathlib import Path

import dnfile
from dncil.cil.body.reader import read_method_body_from_bytes


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('assembly', type=Path)
    parser.add_argument('--out', type=Path, default=Path('.local/il-analysis.json'))
    args = parser.parse_args()
    pe = dnfile.dnPE(str(args.assembly))
    patterns = Counter()
    errors = []
    for method in pe.net.mdtables.MethodDef:
        if not method.Rva:
            continue
        try:
            body = read_method_body_from_bytes(pe.get_data(method.Rva, 65536))
            patterns[tuple(str(instruction.opcode) for instruction in body.instructions)] += 1
        except Exception as error:
            errors.append({'method': str(method.Name), 'error': str(error)})
    report = {'assembly': str(args.assembly), 'method_bodies': sum(patterns.values()),
              'throw_null_bodies': patterns[('ldnull', 'throw')],
              'most_common_opcode_patterns': [{'opcodes': list(k), 'count': v} for k, v in patterns.most_common(20)],
              'errors': errors}
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
