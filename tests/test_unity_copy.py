"""Portable-copy tests: independent assets, unchanged GUIDs and recoverable writes."""
import hashlib
import json
from pathlib import Path
import stat
import tempfile
import unittest
from unittest.mock import patch
import warnings
import zipfile

import asset_audit
import unity_copy as c


def guid(n): return f'{n:032x}'
def meta(n): return 'fileFormatVersion: 2\nguid: '+guid(n)+'\n'


class CopyTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='unity copy test ')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.source = self.root/'Recovery.zip'
        self.output = self.root/'working copy'
    def files(self):
        p = 'One/ExportedProject/'
        return {p+'Assets/Bloom.asset': '%YAML 1.1\nData: Upper\n', p+'Assets/Bloom.asset.meta': meta(1),
                p+'Assets/bloom.asset': '%YAML 1.1\nData: Lower\n', p+'Assets/bloom.asset.meta': meta(2),
                p+'ProjectSettings/ProjectVersion.txt': 'm_EditorVersion: 2022.3.62f2',
                p+'Packages/manifest.json': '{"dependencies":{}}'}
    def archive(self, files=None):
        with zipfile.ZipFile(self.source, 'w', zipfile.ZIP_DEFLATED) as z:
            for name, content in (self.files() if files is None else files).items(): z.writestr(name, content)
        return self.source

    def test_copies_both_colliding_assets_and_their_unchanged_metadata(self):
        source = self.archive(); before = hashlib.sha256(source.read_bytes()).hexdigest()
        result = c.materialize(source, self.output)
        self.assertEqual(result['renamed_asset_count'], 1)
        renamed = result['renames'][0]
        target = self.output/'One/ExportedProject'
        self.assertEqual((target/renamed['new_asset']).read_text(), '%YAML 1.1\nData: Lower\n')
        self.assertEqual((target/renamed['new_meta']).read_text(), meta(2))
        self.assertEqual((target/'Assets/Bloom.asset.meta').read_text(), meta(1))
        self.assertEqual(renamed['unchanged_guid'], guid(2))
        self.assertEqual(hashlib.sha256(source.read_bytes()).hexdigest(), before)
        self.assertEqual(asset_audit.audit(self.output)['summary']['case_conflict_count'], 0)
        self.assertFalse(result['unity_editor_verified'])

    def test_manifest_verification_detects_deleted_and_modified_payloads(self):
        c.materialize(self.archive(), self.output)
        self.assertTrue(c.verify(self.output)['ok'])
        base = self.output/'One/ExportedProject'
        (base/'Assets/Bloom.asset').unlink()
        (base/'Assets/Bloom.asset.meta').write_text('altered metadata')
        result = c.verify(self.output)
        self.assertFalse(result['ok'])
        self.assertEqual(len(result['missing_files']), 1)
        self.assertEqual(len(result['altered_files']), 1)

    def test_verification_rejects_an_incomplete_manifest(self):
        c.materialize(self.archive(), self.output)
        path = self.output/'copy-state.json'; data = json.loads(path.read_text())
        data['files'].pop(); path.write_text(json.dumps(data))
        with self.assertRaisesRegex(ValueError, 'Manifesto'): c.verify(self.output)

    def test_packages_and_project_settings_are_not_omitted(self):
        c.materialize(self.archive(), self.output)
        target = self.output/'One/ExportedProject'
        self.assertTrue((target/'Packages/manifest.json').is_file())
        self.assertTrue((target/'ProjectSettings/ProjectVersion.txt').is_file())

    def test_destination_is_never_overwritten(self):
        self.archive(); self.output.mkdir(); marker = self.output/'keep.txt'; marker.write_text('keep')
        with self.assertRaisesRegex(ValueError, 'destinazione esiste'): c.materialize(self.source, self.output)
        self.assertEqual(marker.read_text(), 'keep')

    def test_partial_copy_resumes_after_an_interrupted_read(self):
        self.archive(); original = c.Inputs.open; counter = [0]
        def fail(owner, name):
            # Metadata probes are first, then the sequential copy starts.
            if name.endswith('Assets/Bloom.asset'):
                counter[0] += 1
                if counter[0] == 1: raise OSError('simulated disk interruption')
            return original(owner, name)
        with patch.object(c.Inputs, 'open', fail), self.assertRaises(OSError): c.materialize(self.source, self.output)
        self.assertFalse(self.output.exists())
        self.assertTrue(self.output.with_name(self.output.name+'.partial').exists())
        self.assertFalse(list(self.root.rglob('copying-*.tmp')))
        result = c.materialize(self.source, self.output, resume=True)
        self.assertEqual(result['state'], 'complete')
        self.assertTrue((self.output/'copy-state.json').is_file())

    def test_resume_rejects_altered_completed_files(self):
        self.archive(); original = c.Inputs.open
        def fail(owner, name):
            if name.endswith('Assets/bloom.asset'): raise OSError('interrupted')
            return original(owner, name)
        with patch.object(c.Inputs, 'open', fail), self.assertRaises(OSError): c.materialize(self.source, self.output)
        partial = self.output.with_name(self.output.name+'.partial')
        existing = partial/'One/ExportedProject/Assets/Bloom.asset'; self.assertTrue(existing.exists())
        existing.write_text('changed')
        with self.assertRaisesRegex(ValueError, 'File parziale modificato'):
            c.materialize(self.source, self.output, resume=True)
        self.assertEqual(existing.read_text(), 'changed')

    def test_resume_rejects_a_changed_archive(self):
        self.archive(); original = c.Inputs.open
        def fail(owner, name):
            if name.endswith('Assets/Bloom.asset'): raise OSError('interrupted')
            return original(owner, name)
        with patch.object(c.Inputs, 'open', fail), self.assertRaises(OSError): c.materialize(self.source, self.output)
        changed = self.files(); changed['One/ExportedProject/Assets/Extra.asset'] = 'new'
        self.archive(changed)
        with self.assertRaisesRegex(ValueError, 'piano diversi'): c.materialize(self.source, self.output, resume=True)

    def test_project_selection_does_not_copy_other_projects(self):
        files = self.files(); files['Two/ExportedProject/Assets/Other.txt'] = 'other'; self.archive(files)
        result = c.materialize(self.source, self.output, project='One')
        self.assertEqual(len(result['project_labels']), 1)
        self.assertFalse((self.output/'Two').exists())
        with self.assertRaisesRegex(ValueError, 'ambigua'): c.plan(self.source, project='not found')

    def test_project_labels_cannot_collide_with_a_generated_suffix(self):
        second = 'Two/Foo/ExportedProject'
        suffix = hashlib.sha256(second.encode()).hexdigest()[:10]
        files = {'One/Foo/ExportedProject/Assets/A.txt': 'one',
                 'R/Foo__'+suffix+'/ExportedProject/Assets/A.txt': 'explicit label',
                 second+'/Assets/A.txt': 'two'}
        result = c.materialize(self.archive(files), self.output)
        labels = list(result['project_labels'].values())
        self.assertEqual(len(set(x.casefold() for x in labels)), 3)
        self.assertEqual(result['resumed_files'], 0)

    def test_unsupported_folder_case_conflict_is_not_silently_merged(self):
        files = {'One/ExportedProject/Assets/Folder/A.asset': 'A', 'One/ExportedProject/Assets/folder/B.asset': 'B'}
        self.archive(files)
        with self.assertRaisesRegex(ValueError, 'Cartelle'): c.materialize(self.source, self.output)
        self.assertFalse(self.output.exists())

    def test_unsafe_paths_and_windows_device_names_are_rejected(self):
        for name in ['../ExportedProject/Assets/evil.asset', '/ExportedProject/Assets/evil.asset',
                     'One/ExportedProject/Assets/CON.asset', 'One/ExportedProject/Assets/CON .asset', 'One/ExportedProject/Assets/bad?.asset']:
            with self.subTest(name=name):
                self.archive({name: 'bad'})
                with self.assertRaises(ValueError): c.materialize(self.source, self.output)
                self.assertFalse(self.output.exists())

    def test_zip_symlink_and_duplicate_names_are_rejected(self):
        self.archive()
        link = zipfile.ZipInfo('One/ExportedProject/Assets/link')
        link.create_system = 3; link.external_attr = (stat.S_IFLNK | 0o777) << 16
        with zipfile.ZipFile(self.source, 'a') as z: z.writestr(link, '../outside')
        with self.assertRaisesRegex(ValueError, 'Link simbolico'): c.plan(self.source)
        self.archive()
        with warnings.catch_warnings():
            warnings.simplefilter('ignore')
            with zipfile.ZipFile(self.source, 'a') as z: z.writestr('One/ExportedProject/Assets/Bloom.asset', 'another')
        with self.assertRaisesRegex(ValueError, 'duplicati'): c.plan(self.source)

    def test_long_destination_paths_are_supported(self):
        relative = 'Assets/'+'/'.join(['long_path_'+'x'*85]*3)+'/file.asset'
        self.archive({'One/ExportedProject/'+relative: 'long path payload'})
        result = c.materialize(self.source, self.output)
        manifest = json.loads((self.output/'copy-state.json').read_text())
        self.assertEqual(result['files_count'], 1)
        self.assertEqual(manifest['files'][0]['sha256'], hashlib.sha256(b'long path payload').hexdigest())

    def test_generated_rename_avoids_an_existing_suffix_collision(self):
        files = self.files()
        files['One/ExportedProject/Assets/bloom__'+guid(2)[:12]+'.asset'] = 'already exists'
        result = c.materialize(self.archive(files), self.output)
        self.assertNotEqual(result['renames'][0]['new_asset'], 'Assets/bloom__'+guid(2)[:12]+'.asset')


if __name__ == '__main__': unittest.main()
