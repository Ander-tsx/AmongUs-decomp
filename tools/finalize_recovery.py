"""Finish long-running local extraction, then record editor validation honestly."""
import argparse
import ctypes
import datetime
import json
import subprocess
import sys
import time
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
STATUS = ROOT / '.local/finalization-status.json'
state = {'stage': 'starting', 'gameplay_parity': 'not_verified', 'lan_multiplayer': 'not_verified'}


def status(stage, **updates):
    state.update(stage=stage, updated_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(), **updates)
    STATUS.write_text(json.dumps(state, indent=2), encoding='utf-8')
    print(json.dumps(state), flush=True)


def tail(path):
    if not path.exists():
        return ''
    with path.open('rb') as stream:
        stream.seek(max(0, path.stat().st_size - 131072))
        return stream.read().decode('utf-8', errors='replace')


def alive(pid):
    kernel = ctypes.windll.kernel32
    kernel.OpenProcess.restype = ctypes.c_void_p
    kernel.CloseHandle.argtypes = [ctypes.c_void_p]
    kernel.GetExitCodeProcess.argtypes = [ctypes.c_void_p, ctypes.POINTER(ctypes.c_ulong)]
    handle = kernel.OpenProcess(0x1000, False, pid)
    if not handle:
        return False
    code = ctypes.c_ulong()
    ok = kernel.GetExitCodeProcess(handle, ctypes.byref(code))
    kernel.CloseHandle(handle)
    return bool(ok and code.value == 259)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--exporter-pid', type=int, required=True)
    parser.add_argument('--download-pid', type=int)
    args = parser.parse_args()
    project = ROOT / 'Recovered/ExportedProject'
    installer = ROOT / '.local/tools/UnitySetup64-2022.3.44f1.exe'
    editor_root = ROOT / '.local/Unity/2022.3.44f1'
    editor = editor_root / 'Editor/Unity.exe'
    status('waiting_for_editor_download')
    deadline = time.monotonic() + 7200
    while not installer.exists() or installer.stat().st_size < 3454659584:
        if time.monotonic() > deadline or (args.download_pid and not alive(args.download_pid)):
            state['editor_installation'] = 'download_incomplete'
            break
        time.sleep(10)
    else:
        if not editor.exists():
            status('installing_editor')
            try:
                result = subprocess.run([str(installer), '/S', '/D=' + str(editor_root)], timeout=1800)
                state['installer_exit_code'] = result.returncode
            except (OSError, subprocess.TimeoutExpired) as error:
                state['editor_installation'] = str(error)
        if editor.exists():
            state['editor_installation'] = 'installed'
    status('waiting_for_export')
    deadline = time.monotonic() + 43200
    while 'Export : Finished post-export' not in tail(ROOT / '.local/assetripper.log'):
        if not alive(args.exporter_pid):
            raise RuntimeError('AssetRipper exited before completing post-export')
        if time.monotonic() > deadline:
            raise TimeoutError('Export exceeded twelve hours; inspect log and process')
        time.sleep(10)
    if not (project / 'ProjectSettings/ProjectVersion.txt').exists():
        raise RuntimeError('Exporter reported completion but Unity project metadata is missing')
    status('export_complete')
    subprocess.run([sys.executable, str(ROOT / 'tools/rename_project.py')], cwd=ROOT, check=True)
    with (ROOT / '.local/audit-summary.log').open('w', encoding='utf-8') as output:
        subprocess.run([sys.executable, str(ROOT / 'tools/audit_recovery.py')], cwd=ROOT, stdout=output, check=True)
    status('inventory_complete', product_name='Entre nosotros hay un impostor')
    if editor.exists():
        status('validating_unity_compilation')
        unity_log = ROOT / '.local/unity-validation.log'
        try:
            result = subprocess.run([str(editor), '-batchmode', '-nographics', '-quit',
                                     '-projectPath', str(project), '-logFile', str(unity_log)], timeout=3600)
            text = unity_log.read_text(encoding='utf-8', errors='replace') if unity_log.exists() else ''
            errors = [line for line in text.splitlines() if 'error CS' in line]
            state['unity_exit_code'] = result.returncode
            state['compiler_error_count'] = len(errors)
            state['compiler_error_examples'] = errors[:20]
            state['unity_compilation'] = 'failed' if result.returncode or errors else 'editor_exited_without_reported_compiler_errors'
            audit_path = ROOT / '.local/recovery-audit.json'
            audit = json.loads(audit_path.read_text(encoding='utf-8'))
            audit['validation']['unity_compilation'] = state['unity_compilation']
            audit_path.write_text(json.dumps(audit, indent=2, ensure_ascii=False), encoding='utf-8')
        except (OSError, subprocess.TimeoutExpired) as error:
            state['unity_compilation'] = str(error)
    status('recovery_finished_game_reconstruction_incomplete')


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        status('failed', error=str(error))
        raise
