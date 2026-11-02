"""Assemble exported assets without rewriting serialized references.

This produces an integration candidate, not a validated playable Unity project.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re

from asset_audit import Inputs, META_GUID
from unity_copy import _make_plan, _target, _digest_file, _atomic_json, _platform_path


def assemble(source, destination):
    destination = _platform_path(Path(destination).resolve())
    staging = destination.with_name(destination.name + '.partial')
    if destination.exists() or staging.exists():
        raise ValueError('Destinazione presente: scegliere una nuova cartella.')
    inputs = Inputs(source)
    try:
        original = _make_plan(inputs)
        if 'Base' not in original['project_labels'].values():
            raise ValueError('Progetto Base assente: impostazioni non selezionabili.')
        entries = []; skipped_settings = []; guid_owner = {}; duplicates = []; paths = {}
        def digest(name):
            h = hashlib.sha256()
            with inputs.open(name) as stream:
                for chunk in iter(lambda: stream.read(1024*1024), b''): h.update(chunk)
            return h.hexdigest()
        def importer(name):
            with inputs.open(name) as stream: data = stream.read()
            return re.sub(rb'(?m)^timeCreated: [0-9]+\r?\n', b'', data)
        # Ignore only export timestamps; every importer setting must match.
        omitted = set()
        for item in original['entries']:
            name = item['source']
            if '/Assets/' not in name or not name.endswith('.meta'): continue
            with inputs.open(name) as stream: match = META_GUID.search(stream.read(65536))
            if not match: continue
            guid = match[1].decode(); asset = name[:-5]
            if asset not in inputs.files: continue  # folder metadata stays scoped
            if guid in guid_owner:
                first = guid_owner[guid]
                if digest(first) != digest(asset) or importer(first+'.meta') != importer(name):
                    raise ValueError('GUID condiviso con contenuti o importazione diversi: '+guid)
                omitted.update((asset, name)); duplicates.append({'source':asset,'canonical':first,'guid':guid})
            else: guid_owner[guid] = asset
        for item in original['entries']:
            source_name = item['source']
            if source_name in omitted: continue
            label, _, relative = item['target'].split('/', 2)
            if relative.startswith(('ProjectSettings/', 'Packages/')):
                if label != 'Base': skipped_settings.append(source_name); continue
                target = relative
            elif relative.startswith('Assets/StreamingAssets/'):
                target = relative
            else:
                target = 'Assets/Recovered/'+label+'/'+relative[len('Assets/'):]
            entries.append({'source':source_name,'target':target})
        for name in inputs.files:
            marker = '/OriginalStreamingAssets/'
            if marker in name:
                entries.append({'source':name,'target':'Assets/StreamingAssets/'+name.split(marker,1)[1]})
        unique = []; assembly_names = {}
        for item in entries:
            key = item['target'].casefold()
            if key.endswith('.dll'):
                assembly = Path(key).name
                if assembly in assembly_names:
                    raise ValueError('DLL omonime con identita GUID distinte: '+assembly)
                assembly_names[assembly] = key
            if key in paths:
                first = paths[key]
                if first['target'] != item['target'] or digest(first['source']) != digest(item['source']):
                    raise ValueError('Percorso condiviso con contenuti diversi: '+item['target'])
                duplicates.append({'source':item['source'],'canonical':first['source'],'target':item['target']})
            else: paths[key] = item; unique.append(item)
        # Detect file/directory collisions before any writes.
        keys = set(paths)
        for item in unique:
            if any(str(p).casefold() in keys for p in Path(item['target']).parents):
                raise ValueError('Conflitto file/cartella: '+item['target'])
        staging.mkdir(parents=True)
        records = []
        for item in unique:
            target = _target(staging, item['target']); target.parent.mkdir(parents=True, exist_ok=True)
            h = hashlib.sha256(); size = 0
            with inputs.open(item['source']) as src, target.open('xb') as out:
                for chunk in iter(lambda: src.read(1024*1024), b''):
                    out.write(chunk); h.update(chunk); size += len(chunk)
            actual, _, actual_size = _digest_file(target)
            if actual != h.hexdigest() or size != actual_size: raise OSError('Copia alterata: '+item['target'])
            records.append(dict(item, bytes=size, sha256=actual))
        report = {'schema_version':1, 'state':'complete', 'kind':'integration_candidate',
                  'files_count':len(records), 'total_bytes':sum(x['bytes'] for x in records),
                  'files':records, 'duplicates':duplicates, 'selected_settings_project':'Base',
                  'skipped_settings':skipped_settings, 'renames':original['renames'],
                  'serialized_contents_changed':False, 'unity_editor_verified':False,
                  'runtime_verified':False, 'native_plugins_verified':False}
        _atomic_json(staging/'copy-state.json', report)
        staging.rename(destination)
        return {k:v for k,v in report.items() if k not in {'files','duplicates','skipped_settings','renames'}} | {
            'deduplicated_files':len(omitted)+sum('target' in x for x in duplicates), 'skipped_settings_count':len(skipped_settings),
            'renamed_asset_count':len(original['renames'])}
    finally: inputs.close()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source'); parser.add_argument('destination')
    args = parser.parse_args()
    print(json.dumps(assemble(args.source, args.destination), indent=2))
