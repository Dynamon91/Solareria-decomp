"""Restore the verified empty TMP atlas and its two serialized reference types."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import uuid

from unity_copy import _atomic_json, _target

PATCH=Path(__file__).resolve().parent/'repairs/font-atlas'
GUID='bd7bad007f2987846bfcb8b3ad2c8f7b'
OLD='{fileID: 2800000, guid: '+GUID+', type: 3}'
NEW='{fileID: 2800000, guid: '+GUID+', type: 2}'
NAMES=['LiberationSans SDF Material.mat','LiberationSans SDF - Fallback.asset']
ATLAS='LiberationSans SDF Atlas.texture2D'

def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def repair(project):
    project=Path(project).resolve()
    if not (project/'Assets').is_dir():raise ValueError('Selezionare il progetto Unity, con la cartella Assets.')
    if (project/'Temp/UnityLockfile').exists():raise ValueError('Chiudere Unity prima della riparazione.')
    proof=json.loads((PATCH/'provenance.json').read_text());expected=[proof['old_material_sha256'],proof['old_fallback_sha256']]
    files=[]
    for name,digest in zip(NAMES,expected):
        matches=list((project/'Assets').rglob(name))
        if len(matches)!=1:raise ValueError('Asset assente o ambiguo: '+name)
        path=_target(project,matches[0].relative_to(project).as_posix())
        if sha(path)!=digest or path.read_text().count(OLD)!=1:raise ValueError('Asset diverso dalla baseline verificata: '+name)
        meta=_target(project,path.relative_to(project).as_posix()+'.meta')
        if not meta.is_file() or sha(meta)!=proof['original_meta_sha256'][name]:raise ValueError('Meta diversa dalla baseline: '+name)
        files.append(path)
    if files[0].parent!=files[1].parent:raise ValueError('Materiale e fallback non appartengono alla stessa cartella.')
    for meta in (project/'Assets').rglob('*.meta'):
        if GUID in meta.read_text(errors='replace'):raise ValueError('GUID del nuovo atlante gia presente: conservare il progetto prima di riprovare.')
    targets=[files[0].parent/ATLAS,files[0].parent/(ATLAS+'.meta')]
    if any(p.exists() for p in targets):raise ValueError('Destinazione atlante gia presente.')
    for name in [ATLAS,ATLAS+'.meta']:
        if sha(PATCH/name)!=proof['patch_files_sha256'][name]:raise ValueError('Patch modificata.')
    backup=_target(project,'RecoveryRepairs/font-atlas-'+uuid.uuid4().hex);backup.mkdir(parents=True)
    for path in files:shutil.copyfile(path,backup/path.name)
    installed=[];changed=[]
    try:
        for target in targets:
            temporary=backup/(target.name+'.new');shutil.copyfile(PATCH/target.name,temporary)
            os.replace(temporary,target);installed.append(target)
        for path in files:
            temporary=backup/(path.name+'.new');temporary.write_text(path.read_text().replace(OLD,NEW),encoding='utf-8')
            os.replace(temporary,path);changed.append(path)
        report={'guid':GUID,'patched_references':2,'original_texture_path_id':'234',
                'native_atlas_sha256':sha(targets[0]),'backup':str(backup),
                'unity_editor_verified':False,'runtime_verified':False}
        _atomic_json(backup/'repair.json',report)
        return report
    except BaseException:
        for path in changed:shutil.copyfile(backup/path.name,path)
        for path in installed:path.unlink()
        raise

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('project');args=parser.parse_args()
    try:print(json.dumps(repair(args.project),indent=2))
    except (OSError,ValueError,KeyError) as error:parser.exit(2,str(error)+'\n')
