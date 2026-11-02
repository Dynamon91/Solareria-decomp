"""Copy exported Unity projects without losing case-colliding asset/meta pairs.

Original ZIP and serialized contents remain unchanged. File renames still need
runtime checks for any code that loads an asset by its original pathname.
"""
from collections import defaultdict
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import stat
import tempfile
import zipfile
import zlib

from asset_audit import Inputs, META_GUID

BUFFER_BYTES = 1024 * 1024
RESERVED = {'con', 'prn', 'aux', 'nul'} | {x+str(n) for x in ['com', 'lpt'] for n in range(1, 10)}


def _safe_name(name):
    parts = PurePosixPath(name).parts
    if not parts or name.startswith('/') or '\\' in name or '..' in parts:
        raise ValueError('Percorso archivio non sicuro: '+name)
    return parts


def _windows_name(relative):
    for part in _safe_name(relative):
        if any(ord(c) < 32 or c in '<>:"|?*' for c in part) or part.endswith((' ', '.')):
            raise ValueError('Nome non compatibile con Windows: '+relative)
        if part.split('.', 1)[0].rstrip(' .').casefold() in RESERVED:
            raise ValueError('Nome riservato su Windows: '+relative)


def _copy_location(name):
    parts = PurePosixPath(name).parts
    if 'ExportedProject' in parts:
        index = parts.index('ExportedProject')
        if index+1 < len(parts): return '/'.join(parts[:index+1]), '/'.join(parts[index+1:])
    if parts and parts[0] in {'Assets', 'ProjectSettings', 'Packages'}: return '.', '/'.join(parts)
    return None

def _make_plan(inputs, project=None):
    if inputs.archive is None: raise ValueError('Per la copia usare lo ZIP completo: conserva entrambi i nomi in conflitto.')
    if inputs.duplicates: raise ValueError('ZIP con nomi duplicati: controllare il rapporto audit prima di copiare.')
    projects = defaultdict(dict)
    for name, info in inputs.files.items():
        _safe_name(name)
        location = _copy_location(name)
        if location:
            root, relative = location
            mode = info.external_attr >> 16
            if stat.S_ISLNK(mode): raise ValueError('Link simbolico non supportato nello ZIP: '+name)
            if info.flag_bits & 1: raise ValueError('ZIP cifrato non supportato.')
            projects[root][relative] = name
    if project:
        matches = {p: files for p, files in projects.items()
                   if p == project or PurePosixPath(p).parent.name == project}
        if len(matches) != 1: raise ValueError('Selezione del progetto mancante o ambigua: '+project)
        projects = matches
    if not projects: raise ValueError('Nessun ExportedProject o cartella Assets nello ZIP.')
    labels = {}; used_labels = set(); entries = []; renames = []
    for root, files in sorted(projects.items()):
        base_label = PurePosixPath(root).parent.name or 'Project'
        label = base_label; attempt = 0
        while label.casefold() in used_labels:
            attempt += 1
            label = base_label+'__'+hashlib.sha256(root.encode()).hexdigest()[:10]+(('_'+str(attempt)) if attempt>1 else '')
        _windows_name(label); used_labels.add(label.casefold()); labels[root] = label
        directories = {p.as_posix() for name in files for p in PurePosixPath(name).parents if str(p) != '.'}
        folded_dirs = defaultdict(set)
        for directory in directories: folded_dirs[directory.casefold()].add(directory)
        if any(len(v) > 1 for v in folded_dirs.values()):
            raise ValueError('Cartelle con nomi in conflitto: serve un piano di rinomina dedicato per '+root)
        if any(name.casefold() in folded_dirs for name in files):
            raise ValueError('Un file e una cartella hanno lo stesso nome su Windows: '+root)
        groups = defaultdict(list)
        for name in files:
            _windows_name(name)
            if not name.endswith('.meta'): groups[name.casefold()].append(name)
        used = {name.casefold() for name in files}
        mapping = {}
        for names in groups.values():
            for original in sorted(names)[1:]:
                if not original.startswith('Assets/'):
                    raise ValueError('Conflitto fuori da Assets: richiede verifica manuale: '+original)
                metadata = original+'.meta'
                asset_guid = None
                if metadata in files:
                    with inputs.open(files[metadata]) as stream: match = META_GUID.search(stream.read(64*1024))
                    if match: asset_guid = match.group(1).decode('ascii').lower()
                token = (asset_guid or hashlib.sha256(original.encode()).hexdigest())[:12]
                old = PurePosixPath(original); index = 0
                while True:
                    suffix = '__'+token+(('_'+str(index)) if index else '')
                    proposed = (old.parent/(old.stem+suffix+old.suffix)).as_posix()
                    if proposed.casefold() not in used and (proposed+'.meta').casefold() not in used: break
                    index += 1
                mapping[original] = proposed; used.add(proposed.casefold()); used.add((proposed+'.meta').casefold())
                if metadata in files: mapping[metadata] = proposed+'.meta'
                renames.append({'project': root, 'asset': original, 'new_asset': proposed,
                                'meta': metadata if metadata in files else None,
                                'new_meta': proposed+'.meta' if metadata in files else None, 'unchanged_guid': asset_guid})
        targets = set()
        for relative, source in sorted(files.items()):
            target = label+'/ExportedProject/'+mapping.get(relative, relative)
            if target.casefold() in targets: raise ValueError('Conflitto residuo nei metadati: '+relative)
            targets.add(target.casefold()); _windows_name(target)
            info = inputs.files[source]
            entries.append({'source': source, 'target': target, 'bytes': info.file_size, 'crc32': info.CRC})
    version = hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
    signature = hashlib.sha256(json.dumps({'version': version, 'entries': entries}, sort_keys=True).encode()).hexdigest()
    return {'schema_version': 1, 'source_name': inputs.path.name, 'fingerprint': signature,
            'project_labels': labels, 'files_count': len(entries), 'total_bytes': sum(x['bytes'] for x in entries),
            'renamed_asset_count': len(renames), 'renames': renames, 'entries': entries}


def plan(source, project=None):
    inputs = Inputs(source)
    try: return _make_plan(inputs, project)
    finally: inputs.close()


def _atomic_json(path, value):
    fd, name = tempfile.mkstemp(prefix='copy-state-', suffix='.tmp', dir=path.parent)
    try:
        with os.fdopen(fd, 'w', encoding='utf-8') as stream:
            json.dump(value, stream, indent=2); stream.flush(); os.fsync(stream.fileno())
        os.replace(name, path)
    finally:
        if os.path.exists(name): os.unlink(name)


def _digest_file(path):
    sha = hashlib.sha256(); crc = 0; size = 0
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(BUFFER_BYTES), b''):
            sha.update(chunk); crc = zlib.crc32(chunk, crc); size += len(chunk)
    return sha.hexdigest(), crc & 0xffffffff, size


def _target(staging, relative):
    _windows_name(relative)
    path = staging/relative
    if not path.resolve().is_relative_to(staging.resolve()): raise ValueError('Percorso di destinazione esterno alla copia.')
    current = path
    while current != staging:
        if current.is_symlink(): raise ValueError('Link simbolico nella copia parziale: '+str(current))
        current = current.parent
    return path


def _platform_path(path):
    if os.name != 'nt': return path
    prefix = '\\\\?\\'
    text = str(path.absolute())
    if text.startswith(prefix): return path
    if text.startswith('\\\\'): return Path(prefix+'UNC'+chr(92)+text[2:])
    return Path(prefix+text)

def materialize(source, destination, project=None, resume=False):
    destination = _platform_path(Path(destination).expanduser().resolve())
    if destination.exists(): raise ValueError('La destinazione esiste: scegliere una nuova cartella; non sovrascrivo progetti.')
    staging = destination.with_name(destination.name+'.partial')
    if staging.is_symlink(): raise ValueError('La cartella parziale non puo essere un link simbolico.')
    inputs = Inputs(source)
    try:
        copy_plan = _make_plan(inputs, project)
        state_file = staging/'copy-state.json'
        if staging.exists():
            if not resume: raise ValueError('Copia parziale presente: riprendere con materialize --resume.')
            try: previous = json.loads(state_file.read_text(encoding='utf-8'))
            except (OSError, ValueError) as error: raise ValueError('Checkpoint della copia mancante o corrotto. Conservare la cartella parziale prima di riprovare.') from error
            if previous.get('fingerprint') != copy_plan['fingerprint']:
                raise ValueError('Archivio o piano diversi: non mescolo le copie parziali.')
        else:
            staging.mkdir(parents=True)
            _atomic_json(state_file, {'fingerprint': copy_plan['fingerprint'], 'state': 'in_progress'})
        records = []; copied = skipped = 0
        for item in copy_plan['entries']:
            target = _target(staging, item['target'])
            if target.exists():
                sha, crc, size = _digest_file(target)
                if size != item['bytes'] or crc != item['crc32']:
                    raise ValueError('File parziale modificato: '+str(target)+'. Conservare la cartella prima di un nuovo tentativo.')
                skipped += 1
            else:
                target.parent.mkdir(parents=True, exist_ok=True)
                fd, temporary = tempfile.mkstemp(prefix='copying-', suffix='.tmp', dir=target.parent)
                sha_source = hashlib.sha256(); size = 0; crc = 0
                try:
                    with os.fdopen(fd, 'wb') as output, inputs.open(item['source']) as stream:
                        for chunk in iter(lambda: stream.read(BUFFER_BYTES), b''):
                            output.write(chunk); sha_source.update(chunk)
                            crc = zlib.crc32(chunk, crc); size += len(chunk)
                    if size != item['bytes'] or crc & 0xffffffff != item['crc32']:
                        raise ValueError('Contenuto ZIP non integro: '+item['source'])
                    sha, _, copied_size = _digest_file(Path(temporary))
                    if sha != sha_source.hexdigest() or copied_size != size: raise OSError('Verifica della copia fallita.')
                    os.replace(temporary, target); copied += 1
                finally:
                    if os.path.exists(temporary): os.unlink(temporary)
            records.append({'source': item['source'], 'target': item['target'], 'bytes': size, 'sha256': sha})
        manifest = {key: value for key, value in copy_plan.items() if key != 'entries'}
        manifest.update({'state': 'complete', 'files': records, 'copied_files': copied, 'resumed_files': skipped,
                         'serialized_contents_changed': False, 'unity_editor_verified': False})
        _atomic_json(state_file, manifest)
        if destination.exists(): raise ValueError('La destinazione e stata creata durante il lavoro: conservo la copia parziale.')
        staging.rename(destination)
        return {key: value for key, value in manifest.items() if key != 'files'}
    except zipfile.BadZipFile as error:
        raise ValueError('ZIP danneggiato: la copia parziale e conservata e nessun progetto viene dichiarato completo.') from error
    finally: inputs.close()


def verify(destination):
    """Check every copied file, including binary textures/audio, against its manifest."""
    destination = _platform_path(Path(destination).expanduser().resolve())
    manifest = json.loads((destination/'copy-state.json').read_text(encoding='utf-8'))
    if manifest.get('state') != 'complete': raise ValueError('La copia non e stata completata.')
    files = manifest.get('files')
    if not isinstance(files, list) or len(files) != manifest.get('files_count'):
        raise ValueError('Manifesto della copia incompleto.')
    missing = []; altered = []; count = 0
    for item in files:
        path = _target(destination, item['target'])
        if not path.is_file(): missing.append(item['target']); continue
        sha, _, size = _digest_file(path)
        if sha != item['sha256'] or size != item['bytes']: altered.append(item['target'])
        else: count += 1
    return {'verified_files': count, 'expected_files': len(files), 'missing_files': missing,
            'altered_files': altered, 'ok': not missing and not altered, 'unity_editor_verified': False}
