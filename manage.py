#!/usr/bin/env python3
"""Solateria: local dependencies, sequential decompilation and bounded asset jobs."""
import argparse, hashlib, json, os, shutil, socket, subprocess, sys, time, tempfile, uuid, stat
from contextlib import contextmanager
import urllib.parse, urllib.request
from html.parser import HTMLParser
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent
ASSEMBLIES = ['Assembly-CSharp-firstpass', 'StaticData', 'Core', 'Assembly-CSharp']

def digest(p):
    h = hashlib.sha256()
    with p.open('rb') as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b''): h.update(chunk)
    return h.hexdigest()

def write_json(p, value):
    p.parent.mkdir(parents=True, exist_ok=True)
    fd, temporary = tempfile.mkstemp(prefix=p.name+'.', suffix='.tmp', dir=p.parent)
    try:
        with os.fdopen(fd, 'w', encoding='utf-8') as stream:
            json.dump(value, stream, indent=2)
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, p)
    finally:
        if os.path.exists(temporary): os.unlink(temporary)

@contextmanager
def operation_lock():
    """OS lock: automatically released even if the process crashes."""
    path = ROOT/'.local/pipeline.lock'
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('a+b') as stream:
        if stream.tell() == 0: stream.write(b'0'); stream.flush()
        stream.seek(0)
        try:
            if os.name == 'nt':
                import msvcrt
                msvcrt.locking(stream.fileno(), msvcrt.LK_NBLCK, 1)
            else:
                import fcntl
                fcntl.flock(stream, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except OSError as error:
            raise ValueError('Un altro lavoro e in corso in questa cartella. Attendere che termini.') from error
        try: yield
        finally:
            stream.seek(0)
            if os.name == 'nt':
                import msvcrt
                msvcrt.locking(stream.fileno(), msvcrt.LK_UNLCK, 1)
            else:
                import fcntl
                fcntl.flock(stream, fcntl.LOCK_UN)

def resolve_tool(value):
    candidate = Path(value).expanduser()
    if candidate.is_file(): return candidate.resolve()
    found = shutil.which(str(value))
    if found: return Path(found).resolve()
    raise ValueError('Strumento non trovato: '+str(value))

def validate_references(folder):
    expected = json.loads((ROOT/'references_manifest.json').read_text(encoding='utf-8'))
    for item in expected:
        path = folder/item['name']
        if not path.is_file() or path.stat().st_size != item['bytes'] or digest(path) != item['sha256']:
            raise ValueError('Dipendenza mancante o modificata: '+item['name']+'. Eseguire prepare.')
    return hashlib.sha256(json.dumps(expected, sort_keys=True).encode()).hexdigest()

def file_manifest(folder, source_only=False):
    result = []
    for path in sorted(folder.rglob('*')):
        relative = path.relative_to(folder)
        if not path.is_file() or relative.as_posix() == 'complete.json': continue
        if source_only and {'bin', 'obj'}.intersection(relative.parts): continue
        if path.is_symlink(): raise ValueError('Link simbolico inatteso: '+str(path))
        result.append({'path': relative.as_posix(), 'bytes': path.stat().st_size, 'sha256': digest(path)})
    return result

def verify_manifest(folder, items, source_only=False):
    try:
        if not isinstance(items, list) or not items: return False
        for item in items:
            relative = Path(item['path'])
            if relative.is_absolute() or '..' in relative.parts: return False
        return file_manifest(folder, source_only) == items
    except (OSError, ValueError, KeyError, TypeError): return False

def read_checkpoint(path):
    try: return json.loads(path.read_text(encoding='utf-8'))
    except (OSError, ValueError): return None

def preserve_partial(path):
    """Rename rather than remove the failed output, on the same filesystem."""
    if path.exists():
        archive = ROOT/'.local/interrupted'; archive.mkdir(parents=True, exist_ok=True)
        previous = archive/(path.name+'.interrupted-'+uuid.uuid4().hex[:10])
        path.rename(previous)
        print('Lavoro precedente conservato:', previous, flush=True)

def doctor(args):
    checks = []
    def check(label, action):
        try: action(); checks.append({'check': label, 'ok': True})
        except (OSError, ValueError, RuntimeError, subprocess.SubprocessError) as error:
            checks.append({'check': label, 'ok': False, 'detail': str(error)})
    def sdk():
        command = resolve_tool(args.dotnet)
        result = subprocess.run([str(command), '--list-sdks'], capture_output=True, text=True,
                                timeout=20, check=True, env=environment(str(command)))
        if not any(line.split('.', 1)[0].isdigit() and int(line.split('.', 1)[0]) >= 8
                   for line in result.stdout.splitlines()):
            raise ValueError('Installare .NET SDK 8 o successivo; il solo runtime non basta.')
    check('dotnet_sdk', sdk)
    check('references', lambda: validate_references(ROOT/'References'))
    if args.ilspy: check('ilspy', lambda: resolve_tool(args.ilspy))
    if args.asset_ripper:
        check('asset_ripper', lambda: resolve_tool(args.asset_ripper))
        def assets():
            config = read_checkpoint(ROOT/'.local/config.json')
            if not isinstance(config, dict) or 'data' not in config:
                raise ValueError('Configurazione del gioco mancante: eseguire prepare.')
            data = game_data(config['data'])
            validate_references(data/'Managed')
            if not any((data/'StreamingAssets/aa/StandaloneWindows64').glob('*.bundle')):
                raise ValueError('Bundle mancanti nella copia locale del gioco.')
        check('game_assets', assets)
    report = {'ok': all(item['ok'] for item in checks), 'checks': checks}
    write_json(ROOT/'.local/doctor.json', report)
    print(json.dumps(report, indent=2))
    return 0 if report['ok'] else 1

def game_data(game):
    game = Path(game).expanduser().resolve()
    for p in [game, game/'Solateria_Data', game/'Solateria'/'Solateria_Data']:
        if (p/'Managed'/'Core.dll').is_file(): return p
    raise ValueError('Cartella Solateria_Data/Managed non trovata nella copia locale del gioco.')

def link_or_copy(src, dst):
    dst.parent.mkdir(parents=True, exist_ok=True)
    if not src.stat().st_mode & stat.S_IWUSR:
        shutil.copyfile(src, dst)
        return
    try: os.link(src, dst)
    except OSError: shutil.copyfile(src, dst)

def remove_reference_snapshot(folder):
    # Reference snapshots are copies, so writable flags never affect the game.
    def retry_read_only(operation, path, error):
        if not isinstance(error[1], PermissionError): raise error[1]
        os.chmod(path, os.stat(path).st_mode | stat.S_IWRITE)
        operation(path)
    shutil.rmtree(folder, onerror=retry_read_only)

def prepare(game):
    data = game_data(game)
    expected = json.loads((ROOT/'references_manifest.json').read_text())
    refs = ROOT/'References'
    if refs.exists() and (not refs.is_dir() or refs.is_symlink()):
        raise ValueError('References deve essere una cartella locale normale.')
    # Validate all inputs before changing local dependencies.
    for item in expected:
        p = data/'Managed'/item['name']
        if not p.is_file() or p.stat().st_size != item['bytes'] or digest(p) != item['sha256']:
            raise ValueError('DLL mancante o build diversa: '+item['name'])
    local = ROOT/'.local'; local.mkdir(exist_ok=True)
    staging = Path(tempfile.mkdtemp(prefix='references-new-', dir=local))
    backup = local/('references-old-'+uuid.uuid4().hex)
    installed = False
    try:
        for item in expected: shutil.copyfile(data/'Managed'/item['name'], staging/item['name'])
        validate_references(staging)
        if refs.exists(): refs.rename(backup)
        try:
            staging.rename(refs); installed = True
            write_json(local/'config.json', {'data': str(data)})
        except BaseException:
            if installed: remove_reference_snapshot(refs)
            if backup.exists(): backup.rename(refs)
            raise
        if backup.exists(): remove_reference_snapshot(backup)
    finally:
        if staging.exists(): remove_reference_snapshot(staging)
    print('Dipendenze locali verificate:', len(expected))

def environment(dotnet, heap_mib=None):
    env = os.environ.copy()
    if Path(dotnet).is_file(): env['DOTNET_ROOT'] = str(Path(dotnet).resolve().parent)
    env['DOTNET_PROCESSOR_COUNT'] = '2'
    if heap_mib: env['DOTNET_GCHeapHardLimit'] = format(heap_mib * 1024**2, 'X')
    return env

def build(dotnet, project_root, msbuild=None):
    project_root = Path(project_root).resolve()
    validate_references(project_root/'References')
    dotnet = str(resolve_tool(dotnet))
    if msbuild: msbuild = str(resolve_tool(msbuild))
    command = [dotnet, msbuild] if msbuild else [dotnet, 'msbuild']
    command += [str(ROOT/'Build.proj'), '-t:Build', '-m:1', '-nr:false', '-v:minimal',
                '-p:RecoveryRoot='+str(project_root)]
    subprocess.run(command, check=True, env=environment(dotnet))

def decompile(args):
    refs = ROOT/'References'
    reference_id = validate_references(refs)
    ilspy = resolve_tool(args.ilspy)
    args.dotnet = str(resolve_tool(args.dotnet))
    prefix = [args.dotnet, str(ilspy)] if ilspy.suffix == '.dll' else [str(ilspy)]
    target = ROOT/'.local/fresh'
    identity = {'references': reference_id, 'ilspy': digest(ilspy), 'pipeline': digest(Path(__file__))}
    state_path = target/'decompile-state.json'
    if target.exists():
        if not args.resume: raise ValueError('Esiste gia .local/fresh. Riprendere con decompile --resume.')
        if not state_path.exists(): raise ValueError('Recupero precedente senza checkpoint: rinominare .local/fresh.')
        if json.loads(state_path.read_text(encoding='utf-8')) != identity:
            raise ValueError('Dipendenze o strumenti diversi: rinominare .local/fresh prima di ricominciare.')
        validate_references(target/'References')
    else:
        (target/'References').mkdir(parents=True)
        for f in refs.glob('*.dll'): link_or_copy(f, target/'References'/f.name)
        shutil.copy2(ROOT/'NuGet.Config', target/'NuGet.Config')
        write_json(state_path, identity)
    logs = target/'Logs'; logs.mkdir(exist_ok=True)
    env = environment(args.dotnet, args.heap_mib)
    def type_source(assembly, typename, dest):
        result = subprocess.run(prefix+['-t', typename, '-r', str(refs), str(refs/(assembly+'.dll'))],
                                capture_output=True, text=True, encoding='utf-8', env=env, check=True, timeout=180)
        source = result.stdout.split('You are not using the latest version of the tool')[0]
        if not source.strip(): raise ValueError('Tipo esportato vuoto: '+typename)
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_text(source, encoding='utf-8')
    retries = json.loads((ROOT/'type_recovery.json').read_text())
    for assembly in ASSEMBLIES:
        output = target/'Code'/assembly
        checkpoint = logs/(assembly+'-complete.json')
        if checkpoint.exists() and verify_manifest(output, read_checkpoint(checkpoint), True):
            print('Assembly gia verificato:', assembly, flush=True); continue
        if output.exists(): preserve_partial(output)
        output.mkdir(parents=True)
        with (logs/(assembly+'.log')).open('w') as log:
            subprocess.run(prefix+['-p', '--nested-directories', '-r', str(refs), '-o', str(output),
                                  str(refs/(assembly+'.dll'))], stdout=log, stderr=subprocess.STDOUT,
                           check=True, env=env, timeout=600)
        for item in retries:
            if item['file'].startswith('Code/'+assembly+'/') and item['exit'] == 0:
                type_source(assembly, item['type'], target/item['file'])
        if assembly == 'Core':
            type_source(assembly, 'UnitySourceGeneratedAssemblyMonoScriptTypes_v1',
                        output/'UnitySourceGeneratedAssemblyMonoScriptTypes_v1.cs')
            for name, arity in [('GenericPriorityQueue', 2), ('SimplePriorityQueue', 1), ('SimplePriorityQueue', 2)]:
                type_source(assembly, 'Priority_Queue.'+name+'`'+str(arity),
                            output/'Priority_Queue'/(name+'_'+str(arity)+'.cs'))
            for name in ['GenericPriorityQueue', 'SimplePriorityQueue']:
                f = output/'Priority_Queue'/(name+'.cs')
                # Both generic arities have their own recovered source files above.
                if f.exists(): f.unlink()
        for f in output.rglob('*.cs'):
            text = f.read_text(encoding='utf-8')
            text = text.replace('GameObject IEntrance.get_gameObject()\n\t{\n\t\treturn base.gameObject;\n\t}',
                                'GameObject IEntrance.gameObject => base.gameObject;')
            if f.name == 'DoTweenExtension.cs' and 'using Spine.Unity;' not in text:
                text = 'using Spine.Unity;\n'+text
            if not text.strip(): raise ValueError('Sorgente vuoto da recuperare: '+str(f))
            f.write_text(text, encoding='utf-8')
        project = output/(assembly+'.csproj'); tree = ET.parse(project)
        for hint in tree.findall('.//HintPath'): hint.text = '../../References/'+Path(hint.text).name
        existing = {r.get('Include') for r in tree.findall('.//Reference')}
        group = ET.SubElement(tree.getroot(), 'ItemGroup')
        for dll in sorted(refs.glob('Unity*.dll')):
            if dll.stem not in existing:
                node = ET.SubElement(group, 'Reference', Include=dll.stem)
                ET.SubElement(node, 'HintPath').text = '../../References/'+dll.name
        ET.indent(tree); tree.write(project, encoding='unicode')
        write_json(checkpoint, file_manifest(output, True))
        print('Recuperato:', assembly, flush=True)
    if args.verify: build(args.dotnet, target, args.msbuild)
    print('Nuovi sorgenti:', target/'Code')

def compact_dlls(folder):
    known = {item['sha256']: ROOT/'References'/item['name']
             for item in json.loads((ROOT/'references_manifest.json').read_text())}
    cache = ROOT/'.local/dll-cache'
    cache.mkdir(parents=True, exist_ok=True)
    for entry in cache.glob('*.dll'):
        if digest(entry) != entry.stem:
            print('Cache DLL modificata: ricreo il collegamento '+entry.name, file=sys.stderr)
            entry.unlink()
        else: known[entry.stem] = entry
    saved = 0
    for p in folder.rglob('*.dll'):
        key = digest(p)
        reference = known.get(key)
        if reference is None:
            cached = cache/(key+'.dll')
            try:
                os.link(p, cached)
                known[key] = cached
            except OSError: pass
            continue
        if not reference.exists(): continue
        if digest(reference) != key: raise ValueError('DLL condivisa modificata: '+str(reference))
        if p.stat().st_ino == reference.stat().st_ino and p.stat().st_dev == reference.stat().st_dev: continue
        temporary = p.with_name(p.name+'.linktmp')
        try:
            os.link(reference, temporary)
            size = p.stat().st_size
            temporary.replace(p)
            saved += size
        except OSError:
            if temporary.exists(): temporary.unlink()
    return saved

class SettingsForm(HTMLParser):
    def __init__(self): super().__init__(); self.fields = {}; self.select = None
    def handle_starttag(self, tag, attrs):
        a = dict(attrs)
        if tag == 'input' and a.get('name') and a.get('type') != 'submit' and (a.get('type') != 'checkbox' or 'checked' in a):
            self.fields[a['name']] = a.get('value', '')
        if tag == 'select': self.select = a.get('name')
        if tag == 'option' and 'selected' in a: self.fields[self.select] = a['value']

@contextmanager
def staged_game(data, bundle, label):
    parent = ROOT/'.local/staging'; parent.mkdir(parents=True, exist_ok=True)
    directory = Path(tempfile.mkdtemp(prefix=label+'-', dir=parent))
    stage = directory/'Solateria'
    try:
        stage.mkdir()
        inputs = list((data/'Managed').glob('*.dll')) + [data/x for x in ['globalgamemanagers', 'app.info', 'boot.config']]
        if bundle: inputs += [bundle]
        else:
            inputs += [p for p in data.iterdir() if p.is_file() and p not in inputs]
            inputs += [p for p in (data/'Resources').rglob('*') if p.is_file()]
        for source in inputs:
            link_or_copy(source, stage/'Solateria_Data'/source.relative_to(data))
        executable = data.parent/'Solateria.exe'
        if executable.exists(): link_or_copy(executable, stage/executable.name)
        yield stage
    finally: shutil.rmtree(directory)

def export(args):
    config = ROOT/'.local/config.json'
    if not config.exists(): raise ValueError('Eseguire prima prepare.')
    data = game_data(json.loads(config.read_text(encoding='utf-8'))['data'])
    reference_id = validate_references(ROOT/'References')
    validate_references(data/'Managed')
    executable = resolve_tool(args.asset_ripper)
    tool_id = digest(executable)
    bundles = sorted((data/'StreamingAssets/aa/StandaloneWindows64').glob('*.bundle'))
    if args.bundle:
        bundles = [p for p in bundles if p.name == args.bundle]
        if not bundles: raise ValueError('Bundle non trovato: '+args.bundle)
    if not bundles: raise ValueError('Nessun bundle presente nella copia locale del gioco.')
    jobs = ([] if args.bundle else [('Base', None)]) + [(p.stem, p) for p in bundles]
    for label, bundle in jobs:
        out = ROOT/'.local/assets'/label; done = out/'complete.json'
        if bundle:
            identity = digest(bundle)
        else:
            base_inputs = [p for p in data.iterdir() if p.is_file()]
            base_inputs += [p for p in (data/'Resources').rglob('*') if p.is_file()]
            identity = hashlib.sha256(''.join(str(p.relative_to(data))+digest(p) for p in sorted(base_inputs)).encode()).hexdigest()
        job_id = {'input': identity, 'references': reference_id, 'tool': tool_id, 'pipeline': digest(Path(__file__))}
        if done.exists():
            previous = read_checkpoint(done) or {}
            if not isinstance(previous, dict): previous = {}
            if previous.get('job_id') == job_id and (out/'ExportedProject').is_dir() and verify_manifest(out, previous.get('files')):
                saved = compact_dlls(out)
                print('Gia completato:', label, '; DLL condivise:', saved, 'byte', flush=True); continue
        if out.exists() and any(out.iterdir()):
            if not args.retry: raise ValueError('Output incompleto, modificato o con checkpoint precedente: '+str(out)+'. Riprovare con export --retry.')
            preserve_partial(out)
        with staged_game(data, bundle, label) as stage:
            with socket.socket() as sock: sock.bind(('127.0.0.1', 0)); port = sock.getsockname()[1]
            base = 'http://127.0.0.1:'+str(port)+'/'
            logs = ROOT/'.local/logs'; logs.mkdir(exist_ok=True)
            log_path = logs/(label+'-'+uuid.uuid4().hex[:10]+'.log')
            with (logs/(log_path.stem+'-server.log')).open('w') as log:
                proc = subprocess.Popen([str(executable), '--headless', '--port', str(port),
                                         '--log-path', str(log_path)], stdout=log, stderr=subprocess.STDOUT,
                                        env=environment(args.dotnet, args.heap_mib))
                try:
                    for attempt in range(150):
                        try: urllib.request.urlopen(base, timeout=2).read(); break
                        except OSError:
                            if proc.poll() is not None: raise RuntimeError('AssetRipper non avviato; controllare i log.')
                            time.sleep(.1)
                    else: raise TimeoutError('AssetRipper non risponde.')
                    def post(route, values):
                        request = urllib.request.Request(base+route, data=urllib.parse.urlencode(values).encode())
                        return urllib.request.urlopen(request, timeout=1800).read()
                    form = SettingsForm(); form.feed(urllib.request.urlopen(base+'Settings/Edit', timeout=20).read().decode('utf-8'))
                    form.fields.update({'ScriptExportMode': 'DllExportWithoutRenaming', 'ExportUnreadableAssets': 'true',
                                        'ShaderExportMode': 'Decompile'})
                    post('Settings/Update', form.fields)
                    print('Importazione:', label, flush=True)
                    post('LoadFolder', {'path': str(stage)})
                    if '[Error]' in log_path.read_text(errors='replace'): raise RuntimeError('Errore di importazione: '+str(log_path))
                    post('Export/UnityProject', {'path': str(out)})
                    text = log_path.read_text(errors='replace')
                    if '[Error]' in text or 'Finished post-export' not in text:
                        raise RuntimeError('Esportazione non completata; controllare '+str(log_path))
                    count = sum(p.is_file() for p in out.rglob('*'))
                    if not count or not (out/'ExportedProject').is_dir() or not any(p.is_file() for p in (out/'ExportedProject').rglob('*')):
                        raise RuntimeError('Progetto esportato vuoto o mancante.')
                    saved = compact_dlls(out)
                    write_json(done, {'job_id': job_id, 'files': file_manifest(out), 'exported_files': count, 'deduplicated_dll_bytes': saved,
                                      'dependency_warnings': text.count("wasn't found"), 'heap_limit_mib': args.heap_mib})
                    print('Completato:', label, count, 'file', flush=True)
                finally:
                    if proc.poll() is None:
                        proc.terminate()
                        try: proc.wait(timeout=10)
                        except subprocess.TimeoutExpired: proc.kill(); proc.wait()
    

def audit_assets(args):
    from asset_audit import audit
    result = audit(args.input, args.samples)
    destination = Path(args.report).expanduser().resolve()
    write_json(destination, result)
    print(json.dumps(result['summary'], indent=2))
    print('Rapporto statico:', destination)
    print('I riferimenti segnalati richiedono verifica in Unity; nessun asset e stato modificato.')
    keys = ['missing_local_guid_count', 'duplicate_guid_count', 'null_script_count', 'assets_without_meta_count',
            'metadata_without_guid_count', 'orphan_metadata_count', 'case_conflict_count', 'duplicate_archive_entries']
    return 2 if args.strict and any(result['summary'][key] for key in keys) else 0

def materialize_assets(args):
    from unity_copy import materialize, plan
    if args.plan_only:
        result = plan(args.input, args.project)
        destination = ROOT/'.local/reports/materialize-plan.json'
        write_json(destination, result)
        print('Piano:', destination)
    else:
        result = materialize(args.input, args.output, args.project, args.resume)
    print(json.dumps({k: v for k, v in result.items() if k not in {'entries', 'renames'}}, indent=2))
    return 0

def verify_materialized(args):
    from unity_copy import verify
    result = verify(args.input)
    write_json(Path(args.report).expanduser().resolve(), result)
    print(json.dumps({k: v for k, v in result.items() if k not in {'missing_files', 'altered_files'}}, indent=2))
    print('File mancanti:', len(result['missing_files']), '; modificati:', len(result['altered_files']))
    return 0 if result['ok'] else 2

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    p = sub.add_parser('verify-copy'); p.add_argument('input'); p.add_argument('--report', default=str(ROOT/'.local/reports/copy-verification.json'))
    p = sub.add_parser('materialize'); p.add_argument('input'); p.add_argument('--output', default=str(ROOT/'.local/working')); p.add_argument('--project'); p.add_argument('--resume', action='store_true'); p.add_argument('--plan-only', action='store_true')
    p = sub.add_parser('audit'); p.add_argument('input'); p.add_argument('--report', default=str(ROOT/'.local/reports/unity-audit.json')); p.add_argument('--samples', type=int, default=5); p.add_argument('--strict', action='store_true')
    p = sub.add_parser('doctor'); p.add_argument('--dotnet', default='dotnet'); p.add_argument('--ilspy'); p.add_argument('--asset-ripper')
    p = sub.add_parser('prepare'); p.add_argument('game')
    p = sub.add_parser('build'); p.add_argument('--project-root', default=str(ROOT))
    p.add_argument('--dotnet', default='dotnet'); p.add_argument('--msbuild')
    p = sub.add_parser('decompile'); p.add_argument('--ilspy', default='ilspycmd')
    p.add_argument('--dotnet', default='dotnet'); p.add_argument('--heap-mib', type=int, default=1024)
    p.add_argument('--verify', action='store_true'); p.add_argument('--msbuild'); p.add_argument('--resume', action='store_true')
    p = sub.add_parser('export'); p.add_argument('--asset-ripper', required=True); p.add_argument('--bundle'); p.add_argument('--retry', action='store_true')
    p.add_argument('--dotnet', default='dotnet'); p.add_argument('--heap-mib', type=int, default=2048)
    args = parser.parse_args()
    if hasattr(args, 'heap_mib') and args.heap_mib < 256: parser.error('--heap-mib deve essere almeno 256.')
    try:
        if args.command == 'doctor': return doctor(args)
        with operation_lock():
            if args.command == 'verify-copy': return verify_materialized(args)
            elif args.command == 'materialize': return materialize_assets(args)
            elif args.command == 'audit': return audit_assets(args)
            elif args.command == 'prepare': prepare(args.game)
            elif args.command == 'build': build(args.dotnet, args.project_root, args.msbuild)
            elif args.command == 'decompile': decompile(args)
            elif args.command == 'export': export(args)
    except (OSError, ValueError, RuntimeError, KeyError, TypeError, ET.ParseError, subprocess.SubprocessError) as error:
        print('ERRORE:', error, file=sys.stderr); return 1
    except KeyboardInterrupt:
        print('Interrotto. I lavori completi restano verificabili; usare --resume o --retry.', file=sys.stderr); return 130
    return 0

if __name__ == '__main__': sys.exit(main())

