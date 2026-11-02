"""Join recovery volumes after checking every SHA-256 and the complete output hash."""
import argparse
import hashlib
import json
import os
from pathlib import Path


def basename(name):
    if not isinstance(name,str) or not name or name in {'.','..'} or any(c in name for c in '/\\<>:"|?*') or any(ord(c)<32 for c in name) or name.endswith((' ','.')):
        raise ValueError('Nome file non sicuro nel manifesto.')
    reserved={'con','prn','aux','nul'}|{p+str(i) for p in ['com','lpt'] for i in range(1,10)}
    if name.split('.',1)[0].lower() in reserved:raise ValueError('Nome riservato nel manifesto.')
    return name


def join(manifest):
    manifest=Path(manifest).resolve();data=json.loads(manifest.read_text());name=basename(data['file_name'])
    output=manifest.parent/name;temporary=manifest.parent/(name+'.joining')
    if output.exists() or output.is_symlink() or temporary.exists() or temporary.is_symlink():raise ValueError('Output o copia parziale gia presenti.')
    volumes=data['volumes']
    if not volumes:raise ValueError('Manifesto senza volumi.')
    for i,item in enumerate(volumes,1):
        if basename(item['file_name'])!=name+f'.{i:03d}':raise ValueError('Volumi mancanti o fuori ordine.')
    total=0;full=hashlib.sha256()
    with temporary.open('xb') as out:
        for item in volumes:
            digest=hashlib.sha256();size=0
            with (manifest.parent/item['file_name']).open('rb') as src:
                for block in iter(lambda:src.read(1024*1024),b''):
                    out.write(block);digest.update(block);full.update(block);size+=len(block);total+=len(block)
            if size!=item['size_bytes'] or digest.hexdigest()!=item['sha256']:raise ValueError('Volume corrotto: '+item['file_name'])
        if total!=data['file_size_bytes'] or full.hexdigest()!=data['sha256']:raise ValueError('Hash finale non corrispondente.')
        out.flush();os.fsync(out.fileno())
    if output.exists() or output.is_symlink():raise ValueError('Output creato durante il lavoro: conservo la copia parziale.')
    temporary.rename(output);return output

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('manifest');args=parser.parse_args()
    try:print(join(args.manifest))
    except (OSError,ValueError,KeyError) as error:parser.exit(2,str(error)+'\n')
