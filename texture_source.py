"""Read-only provenance for original LiberationSans font atlas objects."""
import argparse
import hashlib
import json
from pathlib import Path

from source_scripts import UNITYPY_VERSION, UNITY_VERSION


def inspect(source):
    import UnityPy
    from UnityPy.helpers import TypeTreeHelper
    if UnityPy.__version__!=UNITYPY_VERSION: raise ValueError('Versione UnityPy inattesa; usare requirements-source.txt.')
    TypeTreeHelper.read_typetree_boost=False
    source=Path(source).resolve();env=UnityPy.load(str(source));textures=[];materials=[]
    for obj in env.objects:
        if obj.type.name not in {'Texture2D','Material'}:continue
        if str(obj.assets_file.unity_version)!=UNITY_VERSION:raise ValueError('Versione Unity originale inattesa.')
        data=obj.read()
        if 'LiberationSans' not in data.m_Name:continue
        record={'collection':str(obj.assets_file.name),'path_id':str(obj.path_id),'name':data.m_Name,
                'source_object_bytes':obj.byte_size,'source_object_sha256':hashlib.sha256(obj.get_raw_data()).hexdigest()}
        if obj.type.name=='Texture2D':
            stream=data.m_StreamData
            record.update({'width':data.m_Width,'height':data.m_Height,'texture_format':data.m_TextureFormat,
                           'inline_image_bytes':len(data.image_data),
                           'stream':{'path':stream.path,'offset':str(stream.offset),'size':stream.size},
                           'empty_in_original':data.m_Width==0 and data.m_Height==0 and len(data.image_data)==0 and stream.size==0})
            textures.append(record)
        else:
            record['textures']=[{'property':name,'file_id':value.m_Texture.m_FileID,'path_id':str(value.m_Texture.m_PathID)} for name,value in data.m_SavedProperties.m_TexEnvs]
            materials.append(record)
    if not textures:raise ValueError('Nessun atlante LiberationSans individuato nel file originale.')
    sha=hashlib.sha256()
    with source.open('rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''):sha.update(block)
    return {'source_name':source.name,'source_sha256':sha.hexdigest(),'unity_version':UNITY_VERSION,'unitypy_version':UNITYPY_VERSION,
            'read_only':True,'textures':textures,'materials':materials,
            'exported_guid_mapping_verified':False,'external_texture_streams_read':False,
            'assets_repaired':False,'unity_editor_verified':False,'runtime_verified':False}


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('source');parser.add_argument('--report',required=True)
    args=parser.parse_args()
    try:
        result=inspect(args.source);report=Path(args.report);report.parent.mkdir(parents=True,exist_ok=True)
        report.write_text(json.dumps(result,indent=2)+'\n');print('Texture esaminate:',len(result['textures']))
    except (OSError,ValueError,ImportError) as error:parser.exit(2,str(error)+'\n')
