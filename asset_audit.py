"""Read-only Unity GUID diagnostics for exported projects or a recovery ZIP.

This does not open Unity, rewrite references, or prove that a game is playable.
"""
from collections import Counter, defaultdict
from pathlib import Path, PurePosixPath
import re
import zipfile

BUILTIN_GUIDS = {'0'*32, '0'*16+'e'+'0'*15, '0'*16+'f'+'0'*15}
# AssetRipper.Primitives 3.2.0, UnityGuidTests.MissingReferenceSerializedCorrectly.
ASSET_RIPPER_MISSING_REFERENCE = '0000000deadbeef15deadf00d0000000'
META_GUID = re.compile(rb'(?m)^guid:[ \t]*([0-9a-fA-F]{32})[ \t]*\r?$')
REFERENCE = re.compile(rb'\bguid:[ \t]{0,32}([0-9a-fA-F]{32})(?=[\s,}])')
NULL_SCRIPT = re.compile(rb'\bm_Script:[ \t]{0,32}\{[^}\r\n]{0,128}\bfileID:[ \t]{0,32}0[ \t]{0,32}[,}]')
YAML_SUFFIXES = {'.unity', '.prefab', '.asset', '.mat', '.anim', '.controller',
                 '.overridecontroller', '.playable', '.rendertexture', '.cubemap', '.guiskin', '.terrainlayer',
                 '.texture2d', '.shadervariants', '.sprite', '.lighting', '.mask', '.mixer',
                 '.giparams', '.statemachine', '.state', '.blendtree'}
CHUNK_BYTES = 128 * 1024
OVERLAP_BYTES = 256


def project_location(name):
    parts = PurePosixPath(name).parts
    if '..' in parts or name.startswith('/'): return None
    if 'ExportedProject' in parts:
        i = parts.index('ExportedProject')
        if i+1 < len(parts) and parts[i+1] in {'Assets', 'ProjectSettings'}:
            return '/'.join(parts[:i+1]), '/'.join(parts[i+1:])
    if parts and parts[0] in {'Assets', 'ProjectSettings'}:
        return '.', '/'.join(parts)
    return None


def scan_yaml(stream):
    """Bounded buffers, including a reference split across two read calls."""
    first = stream.read(CHUNK_BYTES)
    prefix = first.lstrip(b'\xef\xbb\xbf \t\r\n')
    if not prefix.startswith((b'%YAML', b'--- !u!')): return None
    references = Counter(); null_scripts = 0
    pending = first
    while True:
        chunk = stream.read(CHUNK_BYTES)
        cutoff = max(0, len(pending)-OVERLAP_BYTES) if chunk else len(pending)
        for match in REFERENCE.finditer(pending):
            if match.start() < cutoff: references[match.group(1).decode('ascii').lower()] += 1
        for match in NULL_SCRIPT.finditer(pending):
            if match.start() < cutoff: null_scripts += 1
        if not chunk: break
        pending = pending[cutoff:] + chunk
    return references, null_scripts


class Inputs:
    def __init__(self, path):
        self.path = Path(path).expanduser().resolve()
        self.archive = None
        self.files = {}
        self.duplicates = []
        if self.path.is_file():
            if not zipfile.is_zipfile(self.path): raise ValueError('Specificare uno ZIP completo, non una singola parte.')
            try: self.archive = zipfile.ZipFile(self.path)
            except zipfile.BadZipFile as error:
                raise ValueError('ZIP incompleto o corrotto: riunire tutte le parti e verificare lo SHA-256.') from error
            for info in self.archive.infolist():
                if info.is_dir(): continue
                if info.filename in self.files: self.duplicates.append(info.filename)
                else: self.files[info.filename] = info
        elif self.path.is_dir():
            for p in self.path.rglob('*'):
                if p.is_file() and not p.is_symlink(): self.files[p.relative_to(self.path).as_posix()] = p
        else: raise ValueError('Input del controllo non trovato: '+str(self.path))
    def open(self, name):
        return self.archive.open(self.files[name]) if self.archive else self.files[name].open('rb')
    def close(self):
        if self.archive: self.archive.close()


def audit(path, samples=5):
    if samples < 1: raise ValueError('Il limite di esempi deve essere almeno 1.')
    inputs = Inputs(path)
    try:
        projects = defaultdict(dict); ignored = Counter()
        for name in inputs.files:
            location = project_location(name)
            if location:
                project, relative = location
                if any(part.startswith('.') or part.endswith('~') or part.lower() == 'cvs' for part in PurePosixPath(relative).parts) or relative.lower().endswith('.tmp'):
                    ignored[project] += 1; continue
                projects[project][relative] = name
        if not projects: raise ValueError('Nessun progetto esportato: scegliere una cartella con Assets/ o lo ZIP del recupero.')
        providers = defaultdict(list); guid_by_project = defaultdict(lambda: defaultdict(list))
        missing_guid = defaultdict(list); orphan_meta = defaultdict(list)
        for project, files in projects.items():
            directories = {p.as_posix() for name in files for p in PurePosixPath(name).parents if p.as_posix() != '.'}
            for relative, full in files.items():
                if not relative.startswith('Assets/') or not relative.endswith('.meta'): continue
                with inputs.open(full) as stream: header = stream.read(64*1024)
                match = META_GUID.search(header)
                if not match:
                    missing_guid[project].append(relative); continue
                guid = match.group(1).decode('ascii').lower()
                asset = relative[:-5]
                if asset not in files and asset not in directories:
                    orphan_meta[project].append(relative); continue
                guid_by_project[project][guid].append(asset)
                providers[guid].append({'project': project, 'asset': asset})
        reports = []; global_missing = set()
        for project, files in sorted(projects.items()):
            available = guid_by_project[project]
            missing = {}; nulls = []; null_count = 0; scanned = skipped = refs_total = 0; incoming = Counter()
            asset_guids = {asset: guid for guid, names in available.items() for asset in names}
            for relative, full in files.items():
                if PurePosixPath(relative).suffix.lower() not in YAML_SUFFIXES: continue
                with inputs.open(full) as stream: result = scan_yaml(stream)
                if result is None: skipped += 1; continue
                scanned += 1; references, count = result; refs_total += sum(references.values()); incoming.update(references)
                if count:
                    null_count += count
                    nulls.append({'asset': relative, 'count': count, 'guid': asset_guids.get(relative)})
                for guid, count in references.items():
                    if guid in BUILTIN_GUIDS or (guid in available and guid != ASSET_RIPPER_MISSING_REFERENCE): continue
                    if guid not in missing:
                        placeholder = guid == ASSET_RIPPER_MISSING_REFERENCE
                        candidates = [] if placeholder else providers.get(guid, [])
                        missing[guid] = {'guid': guid, 'reference_kind': 'assetripper_missing_reference' if placeholder else 'asset_guid', 'occurrences': 0, 'sample_assets': [],
                                         'candidate_provider_count': len(candidates), 'candidate_providers': candidates[:samples]}
                        if not candidates: global_missing.add(guid)
                    detail = missing[guid]; detail['occurrences'] += count
                    if len(detail['sample_assets']) < samples: detail['sample_assets'].append(relative)
            duplicate_guids = [{'guid': guid, 'asset_count': len(paths), 'sample_assets': paths[:samples]}
                               for guid, paths in sorted(available.items()) if len(paths) > 1]
            without_meta = [p for p in files if p.startswith('Assets/') and not p.startswith('Assets/StreamingAssets/') and not p.endswith('.meta') and p+'.meta' not in files]
            folded = defaultdict(list)
            for p in files: folded[p.casefold()].append(p)
            conflicts = [names[:samples] for names in folded.values() if len(names) > 1]
            case_assets = sum(len(names)>1 and not names[0].endswith('.meta') for names in folded.values())
            for item in nulls: item['incoming_local_guid_references'] = incoming.get(item['guid'], 0)
            reports.append({'project': project, 'ignored_export_file_count': ignored[project], 'case_asset_conflict_count': case_assets, 'yaml_files_scanned': scanned, 'non_yaml_files_skipped': skipped,
                            'reference_occurrences': refs_total, 'missing_local_guid_count': len(missing),
                            'missing_global_guid_count': sum(not x['candidate_provider_count'] for x in missing.values()),
                            'external_guid_candidates_count': sum(bool(x['candidate_provider_count']) for x in missing.values()),
                            'missing_local_guids': sorted(missing.values(), key=lambda x: x['guid']),
                            'duplicate_guid_count': len(duplicate_guids), 'duplicate_guids': duplicate_guids,
                            'null_script_count': null_count, 'null_script_samples': nulls[:samples], 'null_script_assets': nulls,
                            'referenced_null_script_asset_count': sum(bool(x['incoming_local_guid_references']) for x in nulls),
                            'assets_without_meta_count': len(without_meta), 'assets_without_meta_samples': without_meta[:samples],
                            'metadata_without_guid_count': len(missing_guid[project]), 'metadata_without_guid_samples': missing_guid[project][:samples],
                            'orphan_metadata_count': len(orphan_meta[project]), 'orphan_metadata_samples': orphan_meta[project][:samples],
                            'case_conflict_count': len(conflicts), 'case_conflict_samples': conflicts[:samples]})
        summary = {key: sum(r[key] for r in reports) for key in
                   ['yaml_files_scanned', 'non_yaml_files_skipped', 'missing_local_guid_count',
                    'external_guid_candidates_count', 'duplicate_guid_count', 'null_script_count', 'referenced_null_script_asset_count',
                    'assets_without_meta_count', 'metadata_without_guid_count', 'orphan_metadata_count', 'case_conflict_count', 'case_asset_conflict_count', 'ignored_export_file_count']}
        summary.update({'projects_scanned': len(reports), 'unique_globally_missing_guids': len(global_missing),
                        'duplicate_archive_entries': len(inputs.duplicates)})
        return {'schema_version': 1, 'input_name': inputs.path.name, 'read_only': True,
                'unity_editor_verified': False, 'summary': summary, 'projects': reports,
                'duplicate_archive_entry_samples': inputs.duplicates[:samples],
                'limitations': ['GUID fields are scanned heuristically in YAML text; confirm suspicious references in Unity.',
                                'Static GUID matching does not validate fileID/sub-asset IDs, script layouts, importer settings or runtime behaviour.',
                                'Providers in other export projects are candidates only; no references or files are modified.',
                                'Zero references can be intentional; only null m_Script fields are separately flagged.',
                                'Non-YAML assets are counted as skipped, not treated as validated. No Unity build is performed.']}
    except zipfile.BadZipFile as error:
        raise ValueError('Archivio ZIP danneggiato: verificare il manifesto SHA-256 prima del controllo.') from error
    finally: inputs.close()
