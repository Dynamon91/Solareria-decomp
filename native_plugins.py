"""Copy only verified Windows x64 audio/input plugins into a new Unity asset folder."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import struct
import uuid

from unity_copy import _atomic_json, _target

ROOT=Path(__file__).resolve().parent
TARGET='Assets/RecoveryNativePlugins/x86_64'


def inspect(path):
    data=Path(path).read_bytes()
    if len(data)<64 or data[:2]!=b'MZ': raise ValueError('DLL PE non valida.')
    pe=struct.unpack_from('<I',data,60)[0]
    if pe+264>len(data) or data[pe:pe+4]!=b'PE\0\0': raise ValueError('Header PE non valido.')
    machine=struct.unpack_from('<H',data,pe+4)[0];magic=struct.unpack_from('<H',data,pe+24)[0]
    if machine!=0x8664 or magic!=0x20b: raise ValueError('Plugin Windows x64 richiesto.')
    clr=struct.unpack_from('<II',data,pe+24+112+14*8)
    if clr!=(0,0): raise ValueError('DLL managed inattesa al posto del plugin nativo.')
    return {'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest(),'machine':'AMD64','managed':False}


def metadata(name):
    guid=uuid.uuid5(uuid.NAMESPACE_URL,'solateria-recovery/native-windows-x64/'+name).hex
    return '''fileFormatVersion: 2
guid: %s
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      Any:
    second:
      enabled: 0
      settings: {}
  - first:
      Editor: Editor
    second:
      enabled: 1
      settings:
        CPU: x86_64
        DefaultValueInitialized: true
        OS: Windows
  - first:
      Standalone: Win64
    second:
      enabled: 1
      settings:
        CPU: x86_64
  userData:
  assetBundleName:
  assetBundleVariant:
''' % guid


def install(project, source):
    project=Path(project).resolve();source=Path(source).resolve()
    if not (project/'Assets').is_dir(): raise ValueError('Progetto Unity assente.')
    if (project/'Temp/UnityLockfile').exists(): raise ValueError('Chiudere Unity prima della copia.')
    target=_target(project,TARGET);staging=_target(project,TARGET+'.partial')
    if target.exists() or staging.exists(): raise ValueError('Cartella plugin gia presente.')
    expected=json.loads((ROOT/'native_plugins_manifest.json').read_text())
    existing={p.name.casefold() for p in (project/'Assets').rglob('*.dll')}
    records=[]
    for item in expected:
        if item['name'].casefold() in existing: raise ValueError('Plugin omonimo gia presente: '+item['name'])
        src=_target(source,item['name']);info=inspect(src)
        if info['sha256']!=item['sha256'] or info['bytes']!=item['bytes']: raise ValueError('Plugin diverso dalla baseline: '+item['name'])
        records.append(dict(info,name=item['name']))
    staging.mkdir(parents=True)
    for item in records:
        dst=staging/item['name'];shutil.copyfile(source/item['name'],dst)
        if inspect(dst)['sha256']!=item['sha256']: raise OSError('Copia plugin alterata.')
        dst.with_name(dst.name+'.meta').write_text(metadata(item['name']),encoding='utf-8')
    # A single directory rename commits all four DLL/meta pairs together.
    staging.rename(target)
    report={'installed_plugins':records,'target':TARGET,'source_bytes_unchanged':True,
            'platform':'Windows x64','native_runtime_verified':False,'unity_editor_verified':False}
    _atomic_json(project/'native-plugins-installation.json',report)
    return report


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('project');parser.add_argument('source')
    args=parser.parse_args()
    try:print(json.dumps(install(args.project,args.source),indent=2))
    except (OSError,ValueError,struct.error) as error:parser.exit(2,str(error)+'\n')
