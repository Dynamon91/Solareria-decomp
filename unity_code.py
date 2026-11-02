"""Stage freshly built game DLLs and apply them with unchanged Unity metadata."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import uuid

import manage
from unity_copy import _atomic_json, _target

ROOT = Path(__file__).resolve().parent
ASSEMBLIES = ('Assembly-CSharp-firstpass','StaticData','Core','Assembly-CSharp')


def sha(path):
    h=hashlib.sha256()
    with Path(path).open('rb') as stream:
        for block in iter(lambda: stream.read(1024*1024), b''): h.update(block)
    return h.hexdigest()


def stage(destination, dotnet='dotnet'):
    destination=Path(destination).resolve(); partial=destination.with_name(destination.name+'.partial')
    if destination.exists() or partial.exists(): raise ValueError('Destinazione stage gia presente.')
    manage.build(dotnet,ROOT)
    executable=str(manage.resolve_tool(dotnet));env=manage.environment(executable)
    references={x['name']:x for x in json.loads((ROOT/'references_manifest.json').read_text())}
    outputs={name:ROOT/'Code'/name/'bin/Debug/netstandard2.1'/(name+'.dll') for name in ASSEMBLIES}
    if not all(p.is_file() for p in outputs.values()): raise ValueError('Output delle quattro compilazioni incompleto.')
    # Gate installation on a metadata check against the original managed assemblies.
    checker=ROOT/'tools/AssemblyCheck/bin/Debug/net8.0/AssemblyCheck.dll'
    checker_sources=[ROOT/'tools/AssemblyCheck/Program.cs',ROOT/'tools/AssemblyCheck/AssemblyCheck.csproj']
    if not checker.is_file() or any(p.stat().st_mtime_ns > checker.stat().st_mtime_ns for p in checker_sources):
        subprocess.run([executable,'build',str(ROOT/'tools/AssemblyCheck'),
                        '--configfile',str(ROOT/'NuGet.Config'),'-v','quiet'],check=True,env=env)
    command=[executable,str(ROOT/'tools/AssemblyCheck/bin/Debug/net8.0/AssemblyCheck.dll')]
    for name,path in outputs.items(): command.extend([str(ROOT/'References'/(name+'.dll')),str(path)])
    checked=subprocess.run(command,check=True,env=env,capture_output=True,text=True)
    names=json.loads(checked.stdout)
    if names.get('ok') is not True: raise ValueError('Identita o nomi dei tipi non conservati.')
    partial.mkdir(parents=True); files=[]
    for name,path in outputs.items():
        target=partial/path.name; shutil.copyfile(path,target)
        if sha(target)!=sha(path): raise OSError('Stage alterato.')
        files.append({'name':path.name,'sha256':sha(target),'bytes':target.stat().st_size,
                      'original_sha256':references[path.name]['sha256']})
    sources={p.relative_to(ROOT).as_posix():sha(p) for p in sorted((ROOT/'Code').rglob('*'))
             if p.is_file() and p.suffix in {'.cs','.csproj'} and not {'bin','obj'} & set(p.relative_to(ROOT/'Code').parts)}
    manifest={'schema_version':1,'compiled':True,'files':files,'source_hashes':sources,'metadata_name_check':names,
              'unity_editor_verified':False,'runtime_verified':False}
    _atomic_json(partial/'code-manifest.json',manifest);partial.rename(destination)
    return {'staged_assemblies':len(files),'compiled':True,'unity_editor_verified':False,'runtime_verified':False}


def load_stage(path):
    path=Path(path).resolve();manifest=json.loads((path/'code-manifest.json').read_text())
    files=manifest['files']
    if manifest.get('compiled') is not True or manifest.get('metadata_name_check',{}).get('ok') is not True:
        raise ValueError('Stage privo di verifica della compilazione e dei nomi.')
    if sorted(x['name'] for x in files)!=sorted(n+'.dll' for n in ASSEMBLIES): raise ValueError('Stage incompleto o duplicato.')
    expected={x['name']:x['sha256'] for x in json.loads((ROOT/'references_manifest.json').read_text())}
    for item in files:
        if item['original_sha256']!=expected[item['name']]: raise ValueError('Baseline diversa da quella verificata.')
        p=_target(path,item['name'])
        if not p.is_file() or sha(p)!=item['sha256'] or p.stat().st_size!=item['bytes']: raise ValueError('Stage modificato: '+item['name'])
    return path,files


def closed_project(project):
    if not (project/'Assets').is_dir(): raise ValueError('Progetto Unity assente.')
    if (project/'Temp/UnityLockfile').exists(): raise ValueError('Chiudere Unity prima della sostituzione.')


def apply(project, staged):
    project=Path(project).resolve();closed_project(project);staged,files=load_stage(staged)
    plan=[]
    for item in files:
        matches=[p for p in (project/'Assets').rglob('*.dll') if p.name.casefold()==item['name'].casefold()]
        if len(matches)!=1: raise ValueError('DLL assente o duplicata: '+item['name'])
        target=_target(project,matches[0].relative_to(project).as_posix());meta=_target(project,str(target.relative_to(project))+'.meta')
        if sha(target)!=item['original_sha256']: raise ValueError('DLL originale diversa o gia sostituita: '+item['name'])
        if not meta.is_file(): raise ValueError('Meta assente: '+item['name'])
        plan.append(dict(item,target=target.relative_to(project).as_posix(),meta_sha256=sha(meta)))
    backup=project/'RecoveryCodeBackups'/uuid.uuid4().hex
    if (project/'RecoveryCodeBackups').is_symlink(): raise ValueError('Backup non sicuro.')
    backup.mkdir(parents=True);state={'state':'prepared','files':plan,'unity_editor_verified':False,'runtime_verified':False}
    for item in plan:
        original=_target(project,item['target']);shutil.copyfile(original,backup/item['name'])
        if sha(backup/item['name'])!=item['original_sha256']: raise OSError('Backup alterato.')
        shutil.copyfile(staged/item['name'],backup/(item['name']+'.new'))
        if sha(backup/(item['name']+'.new'))!=item['sha256']: raise OSError('Copia stage alterata.')
    checkpoint=backup/'transaction.json';_atomic_json(checkpoint,state)
    changed=[]
    try:
        state['state']='applying';_atomic_json(checkpoint,state)
        for item in plan:
            target=_target(project,item['target'])
            if sha(target)!=item['original_sha256'] or sha(Path(str(target)+'.meta'))!=item['meta_sha256']:
                raise ValueError('Progetto modificato durante la sostituzione.')
            os.replace(backup/(item['name']+'.new'),target);changed.append(item)
            if sha(target)!=item['sha256']: raise OSError('DLL installata alterata.')
        state['state']='applied';_atomic_json(checkpoint,state)
    except BaseException:
        for item in reversed(changed):
            target=_target(project,item['target']);tmp=backup/(item['name']+'.restore')
            shutil.copyfile(backup/item['name'],tmp);os.replace(tmp,target)
        state['state']='rolled_back';_atomic_json(checkpoint,state);raise
    return {'applied_assemblies':len(plan),'transaction':str(checkpoint),'meta_unchanged':True,'runtime_verified':False}


def verify(project, transaction):
    project=Path(project).resolve();transaction=Path(transaction).resolve()
    if transaction.parent.parent!=project/'RecoveryCodeBackups': raise ValueError('Transazione esterna al progetto.')
    state=json.loads(transaction.read_text());errors=[]
    if state['state']!='applied': raise ValueError('Transazione non applicata; controllare il checkpoint.')
    if sorted(x['name'] for x in state['files'])!=sorted(n+'.dll' for n in ASSEMBLIES): raise ValueError('Transazione incompleta.')
    for item in state['files']:
        target=_target(project,item['target'])
        if not target.is_file() or sha(target)!=item['sha256']: errors.append(item['name'])
        meta=_target(project,item['target']+'.meta')
        if not meta.is_file() or sha(meta)!=item['meta_sha256']: errors.append(item['name']+'.meta')
        original=_target(transaction.parent,item['name'])
        if not original.is_file() or sha(original)!=item['original_sha256']: errors.append('backup/'+item['name'])
    return {'ok':not errors,'errors':errors,'runtime_verified':False}


def rollback(project, transaction):
    project=Path(project).resolve();closed_project(project);transaction=Path(transaction).resolve()
    if transaction.parent.parent!=project/'RecoveryCodeBackups': raise ValueError('Transazione esterna al progetto.')
    state=json.loads(transaction.read_text())
    if sorted(x['name'] for x in state['files'])!=sorted(n+'.dll' for n in ASSEMBLIES): raise ValueError('Transazione incompleta.')
    for item in state['files']:
        target=_target(project,item['target']);original=_target(transaction.parent,item['name'])
        if sha(original)!=item['original_sha256']: raise ValueError('Backup modificato.')
        if sha(target) not in {item['original_sha256'],item['sha256']}: raise ValueError('DLL modificata: ripristino interrotto per conservarla.')
        if sha(_target(project,item['target']+'.meta'))!=item['meta_sha256']: raise ValueError('Meta modificata: ripristino interrotto.')
    state['state']='rolling_back';_atomic_json(transaction,state)
    for item in state['files']:
        original=_target(transaction.parent,item['name']);temporary=_target(transaction.parent,item['name']+'.restore')
        shutil.copyfile(original,temporary);os.replace(temporary,_target(project,item['target']))
    state['state']='rolled_back';_atomic_json(transaction,state)
    return {'restored_assemblies':len(state['files']),'meta_unchanged':True,'runtime_verified':False}


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);sub=parser.add_subparsers(dest='command',required=True)
    p=sub.add_parser('stage');p.add_argument('destination');p.add_argument('--dotnet',default='dotnet')
    p=sub.add_parser('apply');p.add_argument('project');p.add_argument('stage')
    for command in ['verify','rollback']:
        p=sub.add_parser(command);p.add_argument('project');p.add_argument('transaction')
    args=parser.parse_args()
    try:
        if args.command=='stage':result=stage(args.destination,args.dotnet)
        elif args.command=='apply':result=apply(args.project,args.stage)
        elif args.command=='verify':result=verify(args.project,args.transaction)
        else:result=rollback(args.project,args.transaction)
        print(json.dumps(result,indent=2));raise SystemExit(0 if result.get('ok',True) else 2)
    except (OSError,ValueError,KeyError,subprocess.CalledProcessError) as error:parser.exit(2,str(error)+'\n')
