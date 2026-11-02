"""Read-only provenance checks for null MonoBehaviour script pointers.

UnityPy is an optional, pinned dependency. Source files are inspected in separate
processes; no references or serialized data are assigned or rewritten.
"""
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import tempfile

UNITYPY_VERSION = '1.25.4'
UNITY_VERSION = '2022.3.62f2'
MAX_HASHED_OBJECT_BYTES = 1024 * 1024


def _sha_file(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024*1024), b''): digest.update(chunk)
    return digest.hexdigest()


def inspect_environment(env):
    records = []; errors = []; total = 0
    for obj in env.objects:
        if obj.type.name != 'MonoBehaviour': continue
        total += 1
        identity = {'collection': str(obj.assets_file.name), 'path_id': str(obj.path_id)}
        try:
            version = str(obj.assets_file.unity_version)
            if version != UNITY_VERSION: raise ValueError('Versione Unity non supportata per questo progetto: '+version)
            head = obj.parse_monobehaviour_head()
            consumed = obj.Position - obj.byte_start
            if consumed < 0 or consumed > obj.byte_size: raise ValueError('Intestazione oltre i limiti dell\'oggetto.')
            if int(head.m_Script.m_PathID) != 0: continue
            game_object = getattr(head, 'm_GameObject', None)
            raw_hash = None
            if obj.byte_size <= MAX_HASHED_OBJECT_BYTES:
                raw = obj.get_raw_data()
                if len(raw) != obj.byte_size: raise ValueError('Dimensione del dato originale incoerente.')
                raw_hash = hashlib.sha256(raw).hexdigest()
            records.append({**identity, 'name': head.m_Name, 'script_file_id': int(head.m_Script.m_FileID),
                            'script_path_id': str(head.m_Script.m_PathID),
                            'game_object_path_id': str(game_object.m_PathID) if game_object is not None else None,
                            'source_bytes': obj.byte_size, 'header_bytes': consumed,
                            'opaque_payload_bytes': obj.byte_size-consumed, 'raw_sha256': raw_hash})
        except Exception as error:
            errors.append({**identity, 'error': type(error).__name__+': '+str(error)})
    return {'mono_behaviours_read': total, 'original_null_scripts': records, 'errors': errors}


def inspect_file(source):
    try:
        import UnityPy
        from UnityPy.helpers import TypeTreeHelper
    except ImportError as error:
        raise ValueError('Installare le dipendenze opzionali: python -m pip install -r requirements-source.txt') from error
    if UnityPy.__version__ != UNITYPY_VERSION:
        raise ValueError('UnityPy non compatibile: attesa la versione '+UNITYPY_VERSION)
    TypeTreeHelper.read_typetree_boost = False
    source = Path(source).resolve()
    env = UnityPy.load(str(source))
    if not env.objects: raise ValueError('Il file non contiene oggetti Unity riconosciuti.')
    result = inspect_environment(env)
    result.update({'source_name': source.name, 'source_bytes': source.stat().st_size,
                   'source_sha256': _sha_file(source), 'unitypy_version': UNITYPY_VERSION})
    return result


def merge_sources(results):
    objects = {}; errors = []; total = 0
    for result in results:
        total += result.get('mono_behaviours_read', 0)
        errors.extend(result.get('errors', []))
        for item in result.get('original_null_scripts', []):
            key = (item['collection'].casefold(), item['path_id'])
            previous = objects.get(key)
            equivalent = previous is not None and {**previous, 'collection': previous['collection'].casefold()} == {**item, 'collection': item['collection'].casefold()}
            if previous is not None and (not equivalent or not item.get('raw_sha256')):
                errors.append({'collection': item['collection'], 'path_id': item['path_id'],
                               'error': 'Identita originale duplicata con dati diversi.'})
            else: objects[key] = item
    return {'mono_behaviours_read_including_duplicates': total,
            'original_null_scripts': sorted(objects.values(), key=lambda x: (x['collection'], x['path_id'])),
            'errors': errors}


def _scalar(value):
    value = value.strip()
    if value.startswith('"'):
        try: return json.loads(value)
        except ValueError: raise ValueError('Nome YAML con escape non riconosciuti.')
    if value.startswith("'"):
        if not value.endswith("'"): raise ValueError('Nome YAML non terminato.')
        return value[1:-1].replace("''", "'")
    return value


def exported_nulls(project):
    from asset_audit import audit
    project = Path(project).resolve()
    report = audit(project)
    if len(report['projects']) != 1:
        raise ValueError('Confrontare un singolo ExportedProject per volta.')
    row = report['projects'][0]; items = []; errors = []
    prefix = Path() if row['project'] == '.' else Path(row['project'])
    for item in row['null_script_assets']:
        path = project/prefix/item['asset']
        try:
            # Bound this extra evidence read; large/multi-object documents need a richer mapping.
            if path.stat().st_size > 1024*1024: raise ValueError('Documento troppo grande per il confronto dei nomi.')
            text = path.read_text(encoding='utf-8-sig')
            if item['count'] != 1 or len(re.findall(r'^--- !u!', text, re.MULTILINE)) != 1:
                raise ValueError('Piu oggetti nel documento: confronto per nome ambiguo.')
            names = re.findall(r'^  m_Name:[ \t]*(.*)$', text, re.MULTILINE)
            if len(names) != 1: raise ValueError('Nome non univoco nel documento.')
            items.append({'asset': item['asset'], 'name': _scalar(names[0])})
        except (OSError, ValueError) as error: errors.append({'asset': item['asset'], 'error': str(error)})
    return {'exported_null_scripts': items, 'export_errors': errors}


def compare(merged, exported):
    original = Counter(x['name'] for x in merged['original_null_scripts'])
    recovered = Counter(x['name'] for x in exported['exported_null_scripts'])
    clean = not merged['errors'] and not exported['export_errors']
    names_match = original == recovered
    opaque = sum(x['opaque_payload_bytes'] for x in merged['original_null_scripts'])
    if not clean: status = 'incomplete_evidence'
    elif not names_match: status = 'different_counts_or_names'
    elif opaque: status = 'original_nulls_match_but_opaque_payload_requires_review'
    else: status = 'nulls_already_present_in_source_headers'
    return {'status': status, 'source_null_count': sum(original.values()), 'export_null_count': sum(recovered.values()),
            'name_multisets_equal': clean and names_match, 'source_opaque_payload_bytes': opaque,
            'header_only_baseline_confirmed': clean and names_match and opaque == 0,
            'individual_export_identity_mapping_verified': False,
            'source_null_names': dict(sorted(original.items())), 'export_null_names': dict(sorted(recovered.items())),
            'unity_editor_verified': False}


def inspect_sources(source, project=None, timeout=180):
    source = Path(source).expanduser().resolve()
    if not source.exists(): raise ValueError('Sorgente originale non trovata.')
    if source.is_file() and source.suffix.lower() not in {'.assets', '.bundle'}:
        raise ValueError('Usare file originali .assets o .bundle, non YAML esportati o archivi compressi.')
    candidates = [source] if source.is_file() else sorted(p for p in source.rglob('*')
                            if p.is_file() and not p.is_symlink() and p.suffix.lower() in {'.assets', '.bundle'})
    if not candidates: raise ValueError('Nessun bundle o file serialized .assets originale trovato.')
    results = []
    with tempfile.TemporaryDirectory(prefix='solateria-script-source-') as temporary:
        for index, path in enumerate(candidates):
            output = Path(temporary)/(str(index)+'.json')
            try:
                process = subprocess.run([sys.executable, str(Path(__file__).resolve()), '--worker', str(path), str(output)],
                                         capture_output=True, text=True, timeout=timeout)
                if process.returncode or not output.is_file():
                    raise ValueError('Lettore terminato: '+(process.stderr.strip()[-1000:] or str(process.returncode)))
                results.append(json.loads(output.read_text(encoding='utf-8')))
            except (OSError, ValueError, subprocess.SubprocessError) as error:
                results.append({'source_name': path.name, 'errors': [{'error': str(error)}]})
    merged = merge_sources(results)
    result = {'schema_version': 1, 'read_only': True, 'source_files': results, 'merged': merged,
              'comparison': None, 'unity_editor_verified': False,
              'limitations': ['Null-pointer names/counts are compared in aggregate, not mapped to individual exported GUIDs.',
                             'No missing script is assigned. Original null pointers must not be replaced by guessing class names.',
                             'This checks the common MonoBehaviour header, not other fields, valid script links or gameplay.']}
    if project:
        exported = exported_nulls(project); result['exported'] = exported
        result['comparison'] = compare(merged, exported)
    return result


if __name__ == '__main__':
    if len(sys.argv) != 4 or sys.argv[1] != '--worker':
        raise SystemExit('Usare manage.py inspect-scripts; --worker e un comando interno.')
    try:
        result = inspect_file(sys.argv[2])
        Path(sys.argv[3]).write_text(json.dumps(result, indent=2), encoding='utf-8')
    except Exception as error:
        print(type(error).__name__+': '+str(error), file=sys.stderr); raise SystemExit(1)
