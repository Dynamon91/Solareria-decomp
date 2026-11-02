"""Install and invoke the Unity integration check; never infer success from exit code alone."""
import argparse
import json
from pathlib import Path
import subprocess
import uuid

ROOT = Path(__file__).resolve().parent
EDITOR = '2022.3.62f2'


def run(project, unity, mode='validate', scenes=(), output=None, timeout=3600):
    project = Path(project).resolve(); unity = Path(unity).expanduser().resolve()
    if timeout <= 0: raise ValueError('Timeout positivo richiesto.')
    if mode not in {'validate', 'build'}: raise ValueError('Modalita non valida.')
    version = project/'ProjectSettings/ProjectVersion.txt'
    if not (project/'Assets').is_dir() or not version.is_file(): raise ValueError('Progetto Unity assente.')
    if 'm_EditorVersion: '+EDITOR not in version.read_text(): raise ValueError('Versione progetto incompatibile.')
    if (project/'Temp/UnityLockfile').exists(): raise ValueError('Chiudere Unity prima del controllo batch.')
    if not unity.is_file(): raise ValueError('Editor Unity non trovato: '+str(unity))
    for scene in scenes:
        if not scene.startswith('Assets/') or not scene.endswith('.unity') or '..' in Path(scene).parts:
            raise ValueError('Percorso scena non valido: '+scene)
        if not (project/scene).is_file(): raise ValueError('Scena assente: '+scene)
    output_path = None
    if mode == 'build':
        if not output: raise ValueError('Specificare --output in una cartella nuova.')
        output_path = Path(output).resolve()
        if output_path.suffix.lower() != '.exe': raise ValueError('Output Windows richiesto: .exe')
        if output_path.is_relative_to(project): raise ValueError('Salvare la build fuori dal progetto.')
        if output_path.parent.exists(): raise ValueError('La cartella di build deve essere nuova.')
    target = project/'Assets/SolateriaRecoveryTools/Editor/SolateriaRecoveryBuild.cs'
    content = (ROOT/'unity/Editor/SolateriaRecoveryBuild.cs').read_bytes()
    # Reserve the folder so existing user scripts/asmdefs cannot be silently overwritten.
    folder = project/'Assets/SolateriaRecoveryTools'
    if any(p.is_symlink() for p in [folder, folder/'Editor', target]):
        raise ValueError('Cartella strumenti non sicura.')
    if folder.exists():
        existing = {p.relative_to(folder).as_posix() for p in folder.rglob('*') if p.is_file()}
        allowed = {'Editor/SolateriaRecoveryBuild.cs', 'Editor/SolateriaRecoveryBuild.cs.meta', 'Editor.meta'}
        if not existing <= allowed or not target.is_file() or target.read_bytes() != content:
            raise ValueError('Cartella strumenti gia usata o modificata; conservarla prima di aggiornare.')
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(content)
    logs = project/'RecoveryLogs'/uuid.uuid4().hex; logs.mkdir(parents=True)
    request = {'mode':mode, 'scenes':list(scenes), 'output':str(output_path or ''),
               'report':str(logs/'editor-result.json'), 'token':logs.name}
    request_path = logs/'request.json'; request_path.write_text(json.dumps(request), encoding='utf-8')
    if output_path: output_path.parent.mkdir(parents=True)
    command = [str(unity), '-batchmode', '-nographics', '-projectPath', str(project),
               '-buildTarget', 'Win64', '-executeMethod', 'SolateriaRecoveryBuild.Run',
               '-recoveryRequest', str(request_path), '-logFile', str(logs/'Editor.log')]
    result = {'ok':False, 'unity_editor_verified':False, 'build_verified':False,
              'runtime_verified':False, 'logs':str(logs), 'mode':mode}
    try:
        with (logs/'launcher.log').open('w',encoding='utf-8') as stream:
            process = subprocess.run(command, stdout=stream, stderr=subprocess.STDOUT, timeout=timeout, check=False)
        result['returncode'] = process.returncode
        report = json.loads((logs/'editor-result.json').read_text(encoding='utf-8'))
        if not isinstance(report, dict) or report.get('token') != logs.name: raise ValueError('Rapporto Unity non valido.')
        result['editor_report'] = report
        result['ok'] = process.returncode == 0 and report.get('ok') is True and report.get('unity_editor_verified') is True
        result['unity_editor_verified'] = result['ok']
        result['build_verified'] = result['ok'] and mode == 'build' and report.get('build_verified') is True and output_path.is_file()
        if mode == 'build' and not result['build_verified']: result['ok'] = False
    except (OSError, ValueError, subprocess.TimeoutExpired) as error:
        result['error'] = str(error)
    (logs/'runner-result.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('project'); parser.add_argument('--unity', required=True)
    parser.add_argument('--mode', choices=['validate','build'],default='validate')
    parser.add_argument('--scene', action='append',default=[]); parser.add_argument('--output')
    parser.add_argument('--timeout',type=int,default=3600)
    args=parser.parse_args()
    try:
        result=run(args.project,args.unity,args.mode,args.scene,args.output,args.timeout)
        print(json.dumps(result,indent=2)); raise SystemExit(0 if result['ok'] else 2)
    except ValueError as error: parser.exit(2,str(error)+'\n')
