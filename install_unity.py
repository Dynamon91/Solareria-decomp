"""Pinned Linux Unity editor + Windows Mono module installer (Python 3.12+).

Installs vendor binaries only. License activation remains in Unity Hub.
"""
import argparse
import gzip
import hashlib
import json
from pathlib import Path
import platform
import shutil
import stat
import struct
import sys
import tarfile
import urllib.request
import xml.etree.ElementTree as ET
import zlib

VERSION = '2022.3.62f2'
BASE = 'https://download.unity3d.com/download_unity/7670c08855a9/'
ARTIFACTS = {
 'editor': ('LinuxEditorInstaller/Unity-2022.3.62f2.tar.xz', 4144360836,
            '01e7f6588f98a94e5d48bf6ea1ce9574eae21d6a6261d26b60960cc4d723541f'),
 'windows': ('MacEditorTargetInstaller/UnitySetup-Windows-Mono-Support-for-Editor-2022.3.62f2.pkg',182450165,
             'd55cda08689d03e55cc11e12cdfebf25ff1e05af2c16e01336535fd79ec40a74')}


def checked_archive(path, size, expected):
    sha = hashlib.sha256(); count = 0
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024*1024), b''): sha.update(chunk); count += len(chunk)
    if count != size or sha.hexdigest() != expected: raise ValueError('Archivio incompleto o hash errato: '+str(path))


def exact(stream, size):
    data = stream.read(size)
    if len(data) != size: raise ValueError('Archivio troncato.')
    return data


def unpack_odc(stream, destination):
    """Only regular files/directories from the pinned Unity module's old ASCII cpio."""
    destination = Path(destination).resolve(); count = 0
    while True:
        header = exact(stream,76)
        if header[:6] != b'070707': raise ValueError('Formato cpio inatteso.')
        mode = int(header[18:24],8); name_size = int(header[59:65],8); size = int(header[65:76],8)
        if name_size < 1 or name_size > 16384: raise ValueError('Nome cpio non valido.')
        encoded = exact(stream,name_size)
        if encoded[-1:] != b'\0': raise ValueError('Nome cpio non terminato.')
        name = encoded[:-1].decode('utf-8')
        if name == 'TRAILER!!!': return count
        relative = Path(name)
        if relative.is_absolute() or '..' in relative.parts: raise ValueError('Percorso cpio non sicuro.')
        target = destination/relative
        if not target.resolve().is_relative_to(destination): raise ValueError('Percorso cpio esterno.')
        if stat.S_ISDIR(mode):
            if size: raise ValueError('Directory cpio con payload.')
            target.mkdir(parents=True,exist_ok=True)
        elif stat.S_ISREG(mode):
            target.parent.mkdir(parents=True,exist_ok=True)
            with target.open('xb') as output:
                remaining = size
                while remaining:
                    chunk = exact(stream,min(remaining,1024*1024)); output.write(chunk); remaining -= len(chunk)
            target.chmod(mode & 0o777); count += 1
        else: raise ValueError('Tipo cpio non supportato.')


def unpack_windows(package, destination):
    with Path(package).open('rb') as stream:
        magic, size, version, compressed, raw, algorithm = struct.unpack('>4sHHQQI',exact(stream,28))
        if magic != b'xar!' or version != 1 or compressed > 2*1024*1024 or raw > 4*1024*1024:
            raise ValueError('Header xar inatteso.')
        toc = ET.fromstring(zlib.decompress(exact(stream,compressed)))
        payloads = [node for node in toc.iter('file') if node.findtext('name') == 'Payload']
        if len(payloads) != 1: raise ValueError('Payload xar assente o ambiguo.')
        offset = int(payloads[0].findtext('data/offset'))
        if offset < 0: raise ValueError('Offset xar non valido.')
        stream.seek(size+compressed+offset)
        with gzip.GzipFile(fileobj=stream) as payload: return unpack_odc(payload,destination)


def install(destination, cache):
    if sys.version_info < (3,12): raise ValueError('Installazione: Python 3.12 o successivo richiesto.')
    if platform.system() != 'Linux' or platform.machine() != 'x86_64': raise ValueError('Installer per Linux x86_64.')
    destination = Path(destination).resolve(); cache = Path(cache).resolve()
    staging = destination.with_name(destination.name+'.partial')
    if destination.exists() or staging.exists(): raise ValueError('Destinazione gia presente.')
    if cache.is_relative_to(destination) or cache.is_relative_to(staging): raise ValueError('Cache separata richiesta.')
    cache.mkdir(parents=True,exist_ok=True); archives = {}
    for key,(url,size,sha) in ARTIFACTS.items():
        archive = cache/Path(url).name
        if not archive.exists():
            temporary = archive.with_suffix(archive.suffix+'.downloading')
            with urllib.request.urlopen(BASE+url,timeout=60) as src, temporary.open('wb') as out:
                shutil.copyfileobj(src,out,1024*1024)
            checked_archive(temporary,size,sha); temporary.rename(archive)
        checked_archive(archive,size,sha); archives[key] = archive
    staging.mkdir(parents=True)
    with tarfile.open(archives['editor'],'r:xz') as archive: archive.extractall(staging,filter='data')
    editor = staging/'Editor/Unity'
    if not editor.is_file(): raise ValueError('Editor assente nell’archivio.')
    module = staging/'Editor/Data/PlaybackEngines/WindowsStandaloneSupport'
    count = unpack_windows(archives['windows'],module)
    result = {'editor_version':VERSION, 'editor':str(destination/'Editor/Unity'),
              'windows_module_files':count, 'archives_sha256_verified':True,
              'license_verified':False, 'unity_editor_verified':False, 'runtime_verified':False}
    (staging/'installation.json').write_text(json.dumps(result,indent=2)+'\n')
    staging.rename(destination)
    return result


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('destination'); parser.add_argument('--cache',required=True)
    args=parser.parse_args()
    try: print(json.dumps(install(args.destination,args.cache),indent=2))
    except (OSError,ValueError,tarfile.TarError) as error: parser.exit(2,str(error)+'\n')
