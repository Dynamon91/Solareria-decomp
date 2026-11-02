import json
from pathlib import Path
import tempfile
import unittest
from zipfile import ZipFile
from unity_merge import assemble
from unity_copy import verify


class MergeTests(unittest.TestCase):
    def run_merge(self, changed=False, importer=False):
        tmp = tempfile.TemporaryDirectory(); self.addCleanup(tmp.cleanup)
        root = Path(tmp.name); src = root/'source.zip'; out = root/'out'
        meta = 'fileFormatVersion: 2\nguid: '+'a'*32+'\n'
        with ZipFile(src,'w') as z:
            for label in ['Base','Group_01']:
                p = label+'/ExportedProject/'
                z.writestr(p+'Assets/Plugins/Game.dll', b'different' if changed and label!='Base' else b'assembly')
                z.writestr(p+'Assets/Plugins/Game.dll.meta',meta+('changed' if importer and label!='Base' else ''))
                z.writestr(p+'ProjectSettings/ProjectVersion.txt','2022.3.62f2')
                z.writestr(p+'Packages/manifest.json','{}')
            z.writestr('Recovery/OriginalStreamingAssets/Master.bank',b'audio')
        return src,out

    def test_merges_audio_and_deduplicates_dll_without_changing_guid(self):
        src,out = self.run_merge(); result=assemble(src,out)
        self.assertEqual(len(list(out.rglob('*.dll'))),1)
        self.assertEqual((out/'Assets/StreamingAssets/Master.bank').read_bytes(),b'audio')
        self.assertTrue(verify(out)['ok'])
        self.assertFalse(result['unity_editor_verified'])
        self.assertTrue((out/'Packages/manifest.json').exists())
        with self.assertRaises(ValueError): assemble(src,out)

    def test_rejects_shared_guid_with_different_asset_before_writing(self):
        src,out=self.run_merge(changed=True)
        with self.assertRaises(ValueError): assemble(src,out)
        self.assertFalse(out.exists()); self.assertFalse(out.with_name('out.partial').exists())

    def test_rejects_different_importer(self):
        src,out=self.run_merge(importer=True)
        with self.assertRaises(ValueError): assemble(src,out)
        self.assertFalse(out.exists())

    def test_export_timestamp_is_ignored_but_guid_is_preserved(self):
        src,out=self.run_merge()
        replacement=src.with_name('timestamp.zip')
        with ZipFile(src) as old, ZipFile(replacement,'w') as new:
            for name in old.namelist():
                data=old.read(name)
                if name.endswith('.meta'):
                    data += b'timeCreated: '+(b'1' if name.startswith('Base/') else b'2')+b'\n'
                new.writestr(name,data)
        assemble(replacement,out)
        self.assertEqual(len(list(out.rglob('*.dll'))),1)
        self.assertIn('a'*32,next(out.rglob('*.meta')).read_text())

    def test_conflicting_streaming_asset_blocks_merge(self):
        src,out=self.run_merge()
        with ZipFile(src,'a') as z:
            z.writestr('Base/ExportedProject/Assets/StreamingAssets/Master.bank',b'wrong')
        with self.assertRaises(ValueError): assemble(src,out)
        self.assertFalse(out.exists())
