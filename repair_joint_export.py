"""Quarantine only provably unused duplicate lighting artifacts; preserve asset GUIDs on case renames."""
import argparse
from collections import defaultdict,Counter
import json
from pathlib import Path
import re
import uuid

from asset_audit import audit, META_GUID, scan_yaml, YAML_SUFFIXES
from unity_copy import _target, _atomic_json, _digest_file, _windows_name

SCENE=re.compile(r'^  m_Scene: \{fileID: 102900000, guid: ([0-9a-f]{32}), type: 3\}$',re.M)
LIGHTING=re.compile(r'^  m_LightingDataAsset: \{fileID: 112000000, guid: ([0-9a-f]{32}), type: 2\}$',re.M)

def guid(path):
    match=META_GUID.search(Path(str(path)+'.meta').read_bytes())
    if not match:raise ValueError('Meta priva di GUID: '+str(path))
    return match[1].decode().lower()

def repair(project):
    root=Path(project).resolve()
    if (root/'Temp/UnityLockfile').exists():raise ValueError('Chiudere Unity prima della riparazione.')
    before=audit(root,samples=20)
    if len(before['projects'])!=1 or not (root/'Assets').is_dir():raise ValueError('Selezionare un singolo progetto Unity.')
    if before['summary']['duplicate_guid_count']:raise ValueError('GUID duplicati: riparazione ambigua.')
    providers={};incoming=Counter();assets=list((root/'Assets').rglob('*'))
    for path in assets:
        if path.is_file() and path.suffix=='.meta':
            match=META_GUID.search(path.read_bytes())
            if match:providers[match[1].decode().lower()]=Path(str(path)[:-5])
        if path.is_file() and path.suffix.lower() in YAML_SUFFIXES:
            with path.open('rb') as f:result=scan_yaml(f)
            if result:incoming.update(result[0])
    quarantine=[]
    for missing in before['projects'][0]['missing_local_guids']:
        if missing['occurrences']!=1 or len(missing['sample_assets'])!=1:raise ValueError('Riferimento non riparabile automaticamente: '+missing['guid'])
        asset=_target(root,missing['sample_assets'][0]);text=asset.read_text()
        match=SCENE.search(text)
        if asset.name!='LightingData.asset' or not match or match[1]!=missing['guid'] or incoming[guid(asset)]!=0:
            raise ValueError('Il riferimento non appartiene a dati di illuminazione inutilizzati.')
        scene=asset.parent.parent/(asset.parent.name+'.unity')
        if not scene.is_file():raise ValueError('Scena corrente assente.')
        active=LIGHTING.search(scene.read_text())
        if not active or active[1] not in providers:raise ValueError('Dati di illuminazione della scena non risolti.')
        replacement=providers[active[1]]
        reverse=SCENE.search(replacement.read_text())
        if replacement==asset or not reverse or reverse[1]!=guid(scene):raise ValueError('Il collegamento reciproco della scena non e verificato.')
        quarantine.extend([asset,Path(str(asset)+'.meta')])
    omitted=set(quarantine);groups=defaultdict(list);used=set();dirs=defaultdict(set)
    for path in assets:
        relative=path.relative_to(root).as_posix();_windows_name(relative)
        if path.is_dir():dirs[relative.casefold()].add(relative)
        if path.is_file() and path not in omitted:
            used.add(relative.casefold())
            if path.suffix!='.meta':groups[relative.casefold()].append(path)
    if any(len(names)>1 for names in dirs.values()):raise ValueError('Collisione di cartelle: serve un piano separato.')
    renames=[]
    for names in groups.values():
        for path in sorted(names)[1:]:
            token=guid(path)[:12];target=path.with_name(path.stem+'__'+token+path.suffix)
            for item in [target,Path(str(target)+'.meta')]:
                if item.relative_to(root).as_posix().casefold() in used:raise ValueError('Destinazione di rinomina gia usata.')
                used.add(item.relative_to(root).as_posix().casefold())
            renames.extend([(path,target),(Path(str(path)+'.meta'),Path(str(target)+'.meta'))])
    backup=_target(root,'RecoveryRepairs/joint-'+uuid.uuid4().hex);backup.mkdir(parents=True)
    operations=[(path,backup/'quarantine'/path.relative_to(root)) for path in quarantine]+renames
    plan=[{'source':a.relative_to(root).as_posix(),'target':b.relative_to(root).as_posix(),'sha256':_digest_file(a)[0]} for a,b in operations]
    checkpoint=backup/'repair.json';_atomic_json(checkpoint,{'state':'prepared','operations':plan})
    moved=[]
    try:
        for item in plan:
            a=_target(root,item['source']);b=_target(root,item['target'])
            if _digest_file(a)[0]!=item['sha256'] or b.exists():raise ValueError('Progetto modificato durante la riparazione.')
            b.parent.mkdir(parents=True,exist_ok=True);a.rename(b);moved.append((a,b))
        after=audit(root,samples=20)
        if after['summary']['missing_local_guid_count'] or after['summary']['case_conflict_count']:raise ValueError('La verifica finale non e superata.')
        result={'state':'complete','before':before['summary'],'after':after['summary'],'operations':plan,
                'quarantined_generated_lighting_assets':len(quarantine)//2,'renamed_asset_meta_pairs':len(renames)//2,
                'serialized_references_reassigned':False,'payload_bytes_unchanged':True,
                'unity_editor_verified':False,'runtime_verified':False}
        _atomic_json(checkpoint,result);return result
    except BaseException:
        for a,b in reversed(moved):a.parent.mkdir(parents=True,exist_ok=True);b.rename(a)
        _atomic_json(checkpoint,{'state':'rolled_back','operations':plan});raise

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('project');args=parser.parse_args()
    try:print(json.dumps(repair(args.project),indent=2))
    except (OSError,ValueError,KeyError) as error:parser.exit(2,str(error)+'\n')
