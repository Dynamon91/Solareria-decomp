#!/usr/bin/env python3
"""Solateria: local dependencies, sequential decompilation and bounded asset jobs."""
import argparse, hashlib, json, os, shutil, socket, subprocess, sys, time
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
    tmp = p.with_suffix(p.suffix + '.tmp')
    tmp.write_text(json.dumps(value, indent=2), encoding='utf-8')
    tmp.replace(p)

def game_data(game):
    game = Path(game).expanduser().resolve()
    for p in [game, game/'Solateria_Data', game/'Solateria'/'Solateria_Data']:
        if (p/'Managed'/'Core.dll').is_file(): return p
    raise ValueError('Cartella Solateria_Data/Managed non trovata nella copia locale del gioco.')

def link_or_copy(src, dst):
    dst.parent.mkdir(parents=True, exist_ok=True)
    try: os.link(src, dst)
    except OSError: shutil.copy2(src, dst)

def prepare(game):
    data = game_data(game)
    expected = json.loads((ROOT/'references_manifest.json').read_text())
    refs = ROOT/'References'
    # Validate all inputs before changing local dependencies.
    for item in expected:
        p = data/'Managed'/item['name']
        if not p.is_file() or p.stat().st_size != item['bytes'] or digest(p) != item['sha256']:
            raise ValueError('DLL mancante o build diversa: '+item['name'])
    refs.mkdir(exist_ok=True)
    for item in expected: shutil.copy2(data/'Managed'/item['name'], refs/item['name'])
    write_json(ROOT/'.local/config.json', {'data': str(data)})
    print('Dipendenze locali verificate:', len(expected))

def environment(dotnet, heap_mib=None):
    env = os.environ.copy()
    if Path(dotnet).is_file(): env['DOTNET_ROOT'] = str(Path(dotnet).resolve().parent)
    env['DOTNET_PROCESSOR_COUNT'] = '2'
    if heap_mib: env['DOTNET_GCHeapHardLimit'] = format(heap_mib * 1024**2, 'X')
    return env

def build(dotnet, project_root, msbuild=None):
    project_root = Path(project_root).resolve()
    if not (project_root/'References/Core.dll').exists():
        raise ValueError('Prima eseguire prepare con la cartella del gioco locale.')
    command = [dotnet, msbuild] if msbuild else [dotnet, 'msbuild']
    command += [str(ROOT/'Build.proj'), '-t:Build', '-m:1', '-nr:false', '-v:minimal',
                '-p:RecoveryRoot='+str(project_root)]
    sys.stdout.flush()
    os.execvpe(command[0], command, environment(dotnet))

def decompile(args):
    refs = ROOT/'References'
    if not (refs/'Core.dll').exists(): raise ValueError('Eseguire prima prepare.')
    ilspy = Path(args.ilspy).expanduser()
    if not ilspy.exists() and not shutil.which(args.ilspy):
        raise ValueError('ILSpy non trovato. Specificare --ilspy oppure installare ilspycmd (README).')
    prefix = [args.dotnet, str(ilspy)] if ilspy.suffix == '.dll' else [str(ilspy)]
    target = ROOT/'.local/fresh'
    if target.exists(): raise ValueError('Esiste gia .local/fresh. Rinominarla prima di una nuova decompilazione.')
    (target/'References').mkdir(parents=True)
    for f in refs.glob('*.dll'): link_or_copy(f, target/'References'/f.name)
    shutil.copy2(ROOT/'NuGet.Config', target/'NuGet.Config')
    logs = target/'Logs'; logs.mkdir()
    env = environment(args.dotnet, args.heap_mib)
    def type_source(assembly, typename, dest):
        result = subprocess.run(prefix+['-t', typename, '-r', str(refs), str(refs/(assembly+'.dll'))],
                                capture_output=True, text=True, env=env, check=True)
        source = result.stdout.split('You are not using the latest version of the tool')[0]
        if not source.strip(): raise ValueError('Tipo esportato vuoto: '+typename)
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_text(source, encoding='utf-8')
    retries = json.loads((ROOT/'type_recovery.json').read_text())
    for assembly in ASSEMBLIES:
        output = target/'Code'/assembly; output.mkdir(parents=True)
        with (logs/(assembly+'.log')).open('w') as log:
            subprocess.run(prefix+['-p', '--nested-directories', '-r', str(refs), '-o', str(output),
                                  str(refs/(assembly+'.dll'))], stdout=log, stderr=subprocess.STDOUT,
                           check=True, env=env)
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
        print('Recuperato:', assembly, flush=True)
    if args.verify: build(args.dotnet, target, args.msbuild)
    print('Nuovi sorgenti:', target/'Code')

def compact_dlls(folder):
    known = {item['sha256']: ROOT/'References'/item['name']
             for item in json.loads((ROOT/'references_manifest.json').read_text())}
    cache = ROOT/'.local/dll-cache'
    cache.mkdir(parents=True, exist_ok=True)
    known.update({p.stem: p for p in cache.glob('*.dll')})
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

def export(args):
    config = ROOT/'.local/config.json'
    if not config.exists(): raise ValueError('Eseguire prima prepare.')
    data = Path(json.loads(config.read_text())['data'])
    bundles = sorted((data/'StreamingAssets/aa/StandaloneWindows64').glob('*.bundle'))
    if args.bundle:
        bundles = [p for p in bundles if p.name == args.bundle]
        if not bundles: raise ValueError('Bundle non trovato: '+args.bundle)
    jobs = ([] if args.bundle else [('Base', None)]) + [(p.stem, p) for p in bundles]
    for label, bundle in jobs:
        out = ROOT/'.local/assets'/label; done = out/'complete.json'
        if bundle:
            identity = digest(bundle)
        else:
            base_inputs = [p for p in data.iterdir() if p.is_file()]
            base_inputs += [p for p in (data/'Resources').rglob('*') if p.is_file()]
            identity = hashlib.sha256(''.join(str(p.relative_to(data))+digest(p) for p in sorted(base_inputs)).encode()).hexdigest()
        if done.exists():
            previous = json.loads(done.read_text())
            if previous['input_sha256'] == identity and (out/'ExportedProject').is_dir():
                saved = compact_dlls(out)
                print('Gia completato:', label, '; DLL condivise:', saved, 'byte', flush=True); continue
            raise ValueError('Esportazione precedente diversa: '+label)
        if out.exists() and any(out.iterdir()):
            raise ValueError('Esportazione incompleta: rinominare la cartella prima del nuovo tentativo: '+str(out))
        stage = ROOT/'.local/staging'/label/'Solateria'; stage.mkdir(parents=True, exist_ok=True)
        inputs = list((data/'Managed').glob('*.dll')) + [data/x for x in ['globalgamemanagers', 'app.info', 'boot.config']]
        if bundle: inputs += [bundle]
        else:
            inputs += [p for p in data.iterdir() if p.is_file() and p not in inputs]
            inputs += [p for p in (data/'Resources').rglob('*') if p.is_file()]
        for src in inputs:
            dst = stage/'Solateria_Data'/src.relative_to(data)
            if not dst.exists(): link_or_copy(src, dst)
        exe = data.parent/'Solateria.exe'
        if exe.exists() and not (stage/exe.name).exists(): link_or_copy(exe, stage/exe.name)
        with socket.socket() as sock: sock.bind(('127.0.0.1', 0)); port = sock.getsockname()[1]
        base = 'http://127.0.0.1:'+str(port)+'/'
        logs = ROOT/'.local/logs'; logs.mkdir(exist_ok=True)
        log_path = logs/(label+'.log')
        with (logs/(label+'-server.log')).open('w') as log:
            proc = subprocess.Popen([str(Path(args.asset_ripper).resolve()), '--headless', '--port', str(port),
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
                form = SettingsForm(); form.feed(urllib.request.urlopen(base+'Settings/Edit').read().decode())
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
                if not count: raise RuntimeError('Esportazione vuota.')
                saved = compact_dlls(out)
                write_json(done, {'input_sha256': identity, 'exported_files': count, 'deduplicated_dll_bytes': saved,
                                  'dependency_warnings': text.count("wasn't found"), 'heap_limit_mib': args.heap_mib})
                print('Completato:', label, count, 'file', flush=True)
            finally:
                proc.terminate()
                try: proc.wait(timeout=10)
                except subprocess.TimeoutExpired: proc.kill(); proc.wait()
                shutil.rmtree(stage.parent)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    p = sub.add_parser('prepare'); p.add_argument('game')
    p = sub.add_parser('build'); p.add_argument('--project-root', default=str(ROOT))
    p.add_argument('--dotnet', default='dotnet'); p.add_argument('--msbuild')
    p = sub.add_parser('decompile'); p.add_argument('--ilspy', default='ilspycmd')
    p.add_argument('--dotnet', default='dotnet'); p.add_argument('--heap-mib', type=int, default=1024)
    p.add_argument('--verify', action='store_true'); p.add_argument('--msbuild')
    p = sub.add_parser('export'); p.add_argument('--asset-ripper', required=True); p.add_argument('--bundle')
    p.add_argument('--dotnet', default='dotnet'); p.add_argument('--heap-mib', type=int, default=2048)
    args = parser.parse_args()
    if hasattr(args, 'heap_mib') and args.heap_mib < 256: parser.error('--heap-mib deve essere almeno 256.')
    try:
        if args.command == 'prepare': prepare(args.game)
        elif args.command == 'build': build(args.dotnet, args.project_root, args.msbuild)
        elif args.command == 'decompile': decompile(args)
        elif args.command == 'export': export(args)
    except (OSError, ValueError, RuntimeError, subprocess.SubprocessError) as error:
        print('ERRORE:', error, file=sys.stderr); return 1
    return 0

if __name__ == '__main__': sys.exit(main())
