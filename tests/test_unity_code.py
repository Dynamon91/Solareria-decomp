import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import unity_code as c


def digest(data):return hashlib.sha256(data).hexdigest()


class CodeOverlayTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup);self.root=Path(self.tmp.name)
        self.stage=self.root/'stage';self.stage.mkdir();self.project=self.root/'project';self.assets=self.project/'Assets/Recovered/Base/Plugins';self.assets.mkdir(parents=True)
        files=[];refs=[]
        for i,name in enumerate(c.ASSEMBLIES):
            old=('old-'+name).encode();new=('new-'+name).encode();file=name+'.dll'
            (self.assets/file).write_bytes(old);(self.stage/file).write_bytes(new)
            (self.assets/(file+'.meta')).write_text('guid: '+f'{i+1:032x}'+'\n')
            files.append(dict(name=file,sha256=digest(new),original_sha256=digest(old),bytes=len(new)))
            refs.append(dict(name=file,sha256=digest(old)))
        (self.stage/'code-manifest.json').write_text(json.dumps({'files':files,'compiled':True,'metadata_name_check':{'ok':True}}))
        (self.root/'references_manifest.json').write_text(json.dumps(refs))
        self.patch=patch.object(c,'ROOT',self.root);self.patch.start();self.addCleanup(self.patch.stop)

    def test_apply_verify_and_rollback_preserve_all_metadata(self):
        metas={p.name:p.read_bytes() for p in self.assets.glob('*.meta')}
        result=c.apply(self.project,self.stage);self.assertTrue(c.verify(self.project,result['transaction'])['ok'])
        self.assertEqual({p.name:p.read_bytes() for p in self.assets.glob('*.meta')},metas)
        self.assertFalse(result['runtime_verified']);c.rollback(self.project,result['transaction'])
        for name in c.ASSEMBLIES:self.assertEqual((self.assets/(name+'.dll')).read_bytes(),('old-'+name).encode())
        self.assertEqual(len(list((self.project/'Assets').rglob('*.dll'))),4)

    def test_changed_stage_is_rejected_before_project_changes(self):
        (self.stage/'Core.dll').write_bytes(b'changed')
        with self.assertRaises(ValueError):c.apply(self.project,self.stage)
        self.assertFalse((self.project/'RecoveryCodeBackups').exists())

    def test_duplicate_dll_is_rejected(self):
        (self.project/'Assets/Core.dll').write_bytes(b'duplicate')
        with self.assertRaises(ValueError):c.apply(self.project,self.stage)
        self.assertFalse((self.project/'RecoveryCodeBackups').exists())

    def test_failure_after_first_install_rolls_back(self):
        real=c.os.replace
        def fail(src,dst):
            if str(src).endswith('StaticData.dll.new'):raise OSError('simulated disk failure')
            return real(src,dst)
        with patch.object(c.os,'replace',side_effect=fail),self.assertRaises(OSError):c.apply(self.project,self.stage)
        for name in c.ASSEMBLIES:self.assertEqual((self.assets/(name+'.dll')).read_bytes(),('old-'+name).encode())

    def test_rollback_does_not_overwrite_user_changes(self):
        result=c.apply(self.project,self.stage);(self.assets/'Core.dll').write_bytes(b'user-edit')
        with self.assertRaises(ValueError):c.rollback(self.project,result['transaction'])
        self.assertEqual((self.assets/'Core.dll').read_bytes(),b'user-edit')

    def test_verify_detects_meta_edits(self):
        result=c.apply(self.project,self.stage);(self.assets/'Core.dll.meta').write_text('changed')
        self.assertFalse(c.verify(self.project,result['transaction'])['ok'])
