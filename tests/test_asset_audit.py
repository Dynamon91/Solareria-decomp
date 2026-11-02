"""Static diagnostics fixtures; no Unity installation or game assets required."""
import argparse
from collections import Counter
import hashlib
import io
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import warnings
import zipfile

import asset_audit as a
import manage


def guid(number): return f'{number:032x}'
def meta(number): return 'fileFormatVersion: 2\nguid: '+guid(number)+'\n'
def yaml(refs=(), script=False):
    text = '%YAML 1.1\n--- !u!114 &11400000\nMonoBehaviour:\n'
    if script: text += '  m_Script: {fileID: 0}\n'
    for item in refs: text += '  m_Ref: {fileID: 1, guid: '+item+', type: 2}\n'
    return text


class AuditTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='audit fixture ')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
    def archive(self, files):
        path = self.root/'Recovery.zip'
        with zipfile.ZipFile(path, 'w', zipfile.ZIP_DEFLATED) as z:
            for name, content in files.items(): z.writestr(name, content)
        return path
    def basic(self, text=None):
        return {'One/ExportedProject/Assets/Test.asset': yaml() if text is None else text,
                'One/ExportedProject/Assets/Test.asset.meta': meta(1)}

    def test_local_external_and_missing_guids_and_null_scripts(self):
        files = self.basic(yaml([guid(1), guid(2), guid(3), guid(4)], True))
        files.update({'One/ExportedProject/Assets/Local.asset': yaml(), 'One/ExportedProject/Assets/Local.asset.meta': meta(2),
                      'Two/ExportedProject/Assets/External.asset': yaml(), 'Two/ExportedProject/Assets/External.asset.meta': meta(3)})
        report = a.audit(self.archive(files)); first = report['projects'][0]
        self.assertEqual(report['summary']['projects_scanned'], 2)
        self.assertEqual(first['missing_local_guid_count'], 2)
        self.assertEqual(first['external_guid_candidates_count'], 1)
        self.assertEqual(report['summary']['unique_globally_missing_guids'], 1)
        self.assertEqual(first['null_script_count'], 1)
        self.assertEqual(first['null_script_assets'][0]['incoming_local_guid_references'], 1)
        self.assertFalse(report['unity_editor_verified'])

    def test_reserved_unity_builtins_do_not_become_missing_assets(self):
        reserved = ['0'*32, '0'*16+'e'+'0'*15, '0'*16+'f'+'0'*15]
        report = a.audit(self.archive(self.basic(yaml(reserved))))
        self.assertEqual(report['summary']['unique_globally_missing_guids'], 0)
        self.assertEqual(report['summary']['missing_local_guid_count'], 0)

    def test_chunk_boundaries_do_not_lose_or_double_count_references(self):
        header = b'%YAML 1.1\n'
        padding = b'#'+b'x'*(480-len(header)-2)+b'\n'
        body = ('m_Target: {fileID: 1, guid: '+guid(7)+', type: 2}\n'
                'm_Script: {fileID: 0}\n'+
                'm_Target: {fileID: 1, guid: '+guid(7)+', type: 2}\n').encode()
        with patch.object(a, 'CHUNK_BYTES', 512):
            references, nulls = a.scan_yaml(io.BytesIO(header+padding+body+b'#'+b'x'*1000))
        self.assertEqual(references, Counter({guid(7): 2}))
        self.assertEqual(nulls, 1)

    def test_duplicate_guids_are_reported_with_both_assets(self):
        files = self.basic(); files.update({'One/ExportedProject/Assets/Other.asset': yaml(),
                                           'One/ExportedProject/Assets/Other.asset.meta': meta(1)})
        report = a.audit(self.archive(files))
        self.assertEqual(report['summary']['duplicate_guid_count'], 1)
        self.assertEqual(report['projects'][0]['duplicate_guids'][0]['asset_count'], 2)

    def test_case_conflicts_are_detected_inside_zip_on_every_platform(self):
        files = self.basic(); files.update({'One/ExportedProject/Assets/test.asset': yaml(),
                                           'One/ExportedProject/Assets/test.asset.meta': meta(2)})
        report = a.audit(self.archive(files))
        self.assertEqual(report['summary']['case_conflict_count'], 2)
        self.assertEqual(report['summary']['case_asset_conflict_count'], 1)

    def test_orphan_metadata_does_not_resolve_a_missing_asset(self):
        files = self.basic(yaml([guid(8)])); files['One/ExportedProject/Assets/Gone.asset.meta'] = meta(8)
        report = a.audit(self.archive(files))
        self.assertEqual(report['summary']['orphan_metadata_count'], 1)
        self.assertEqual(report['summary']['unique_globally_missing_guids'], 1)

    def test_binary_assets_are_skipped_and_texture_payload_is_not_read(self):
        files = self.basic(b'\x00binary guid: '+guid(5).encode())
        files.update({'One/ExportedProject/Assets/Tex.png': b'not a real PNG',
                      'One/ExportedProject/Assets/Tex.png.meta': meta(2)})
        opened = []; original = a.Inputs.open
        def open_input(owner, name): opened.append(name); return original(owner, name)
        with patch.object(a.Inputs, 'open', open_input): report = a.audit(self.archive(files))
        self.assertEqual(report['summary']['non_yaml_files_skipped'], 1)
        self.assertEqual(report['summary']['unique_globally_missing_guids'], 0)
        self.assertFalse(any(name.endswith('.png') for name in opened))

    def test_metadata_without_guid_and_assets_without_meta(self):
        files = self.basic(); files['One/ExportedProject/Assets/Test.asset.meta'] = 'fileFormatVersion: 2\n'
        files['One/ExportedProject/Assets/NoMeta.asset'] = yaml()
        report = a.audit(self.archive(files))
        self.assertEqual(report['summary']['metadata_without_guid_count'], 1)
        self.assertEqual(report['summary']['assets_without_meta_count'], 1)

    def test_ignored_export_artifacts_and_raw_streaming_files_are_not_meta_errors(self):
        files = self.basic()
        for name in ['.partial.asset', 'Temp.asset~', 'cvs/test.asset', 'partial.tmp']:
            files['One/ExportedProject/Assets/'+name] = yaml([guid(9)])
        files['One/ExportedProject/Assets/StreamingAssets/content.bundle'] = b'raw payload'
        report = a.audit(self.archive(files))
        self.assertEqual(report['summary']['ignored_export_file_count'], 4)
        self.assertEqual(report['summary']['assets_without_meta_count'], 0)
        self.assertEqual(report['summary']['unique_globally_missing_guids'], 0)

    def test_source_namespace_named_assets_does_not_create_a_phantom_project(self):
        files = self.basic(); files['CodeRecovery/Code/Core/Assets/Type.cs'] = 'class Type {}'
        report = a.audit(self.archive(files))
        self.assertEqual(report['summary']['projects_scanned'], 1)

    def test_directory_and_zip_summaries_agree_and_input_is_not_modified(self):
        files = self.basic(yaml([guid(3)], True)); archive = self.archive(files)
        original_hash = hashlib.sha256(archive.read_bytes()).hexdigest()
        folder = self.root/'Exported'
        with zipfile.ZipFile(archive) as z: z.extractall(folder)
        self.assertEqual(a.audit(archive)['summary'], a.audit(folder)['summary'])
        self.assertEqual(hashlib.sha256(archive.read_bytes()).hexdigest(), original_hash)

    def test_duplicate_zip_entries_are_reported(self):
        path = self.archive(self.basic())
        with warnings.catch_warnings():
            warnings.simplefilter('ignore', UserWarning)
            with zipfile.ZipFile(path, 'a') as z: z.writestr('One/ExportedProject/Assets/Test.asset', yaml())
        self.assertEqual(a.audit(path)['summary']['duplicate_archive_entries'], 1)

    def test_invalid_input_and_empty_project_are_clear_errors(self):
        bad = self.root/'fragment.zip'; bad.write_bytes(b'not a complete ZIP')
        with self.assertRaisesRegex(ValueError, 'ZIP completo'): a.audit(bad)
        with self.assertRaisesRegex(ValueError, 'Nessun progetto'): a.audit(self.archive({'README.md': 'text'}))
        with self.assertRaisesRegex(ValueError, 'limite'): a.audit(bad, samples=0)

    def test_strict_cli_reports_findings_without_modifying_the_assets(self):
        archive = self.archive(self.basic(yaml([guid(99)])))
        args = argparse.Namespace(input=str(archive), samples=5, strict=True, report=str(self.root/'report.json'))
        self.assertEqual(manage.audit_assets(args), 2)
        self.assertTrue(Path(args.report).is_file())
        args.strict = False
        self.assertEqual(manage.audit_assets(args), 0)

    def test_optional_null_texture_is_not_misclassified_as_missing_script(self):
        report = a.audit(self.archive(self.basic(yaml()+'  m_Texture: {fileID: 0}\n')))
        self.assertEqual(report['summary']['null_script_count'], 0)

    def test_directory_metadata_is_a_valid_guid_provider(self):
        files = self.basic(yaml([guid(8)])); files.update({'One/ExportedProject/Assets/Folder.meta': meta(8),
            'One/ExportedProject/Assets/Folder/Child.asset': yaml(),
            'One/ExportedProject/Assets/Folder/Child.asset.meta': meta(9)})
        report = a.audit(self.archive(files))
        self.assertEqual(report['summary']['orphan_metadata_count'], 0)
        self.assertEqual(report['summary']['missing_local_guid_count'], 0)


if __name__ == '__main__': unittest.main()
