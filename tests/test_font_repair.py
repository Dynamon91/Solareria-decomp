from pathlib import Path
import hashlib,json,tempfile,unittest
from unittest.mock import patch
import repair_font as r

def sha(data):return hashlib.sha256(data).hexdigest()

class FontRepairTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup);self.root=Path(self.tmp.name)
        self.project=self.root/'project';self.assets=self.project/'Assets/Fonts';self.assets.mkdir(parents=True)
        self.patch=self.root/'patch';self.patch.mkdir();self.original={};metas={}
        for name in r.NAMES:
            data=('test '+r.OLD+'\n').encode();(self.assets/name).write_bytes(data);self.original[name]=data
            m=('guid: '+sha(name.encode())[:32]+'\n').encode();(self.assets/(name+'.meta')).write_bytes(m);metas[name]=sha(m)
        atlas=b'%YAML 1.1\n--- !u!28 &2800000\nTexture2D:\n  m_Width: 0\n  m_Height: 0\n'
        (self.patch/r.ATLAS).write_bytes(atlas);(self.patch/(r.ATLAS+'.meta')).write_text('guid: '+r.GUID+'\n')
        proof={'old_material_sha256':sha(self.original[r.NAMES[0]]),'old_fallback_sha256':sha(self.original[r.NAMES[1]]),'original_meta_sha256':metas,'patch_files_sha256':{name:sha((self.patch/name).read_bytes()) for name in [r.ATLAS,r.ATLAS+'.meta']}}
        (self.patch/'provenance.json').write_text(json.dumps(proof));p=patch.object(r,'PATCH',self.patch);p.start();self.addCleanup(p.stop)
    def test_repairs_two_pointer_types_and_preserves_metadata(self):
        before={p.name:p.read_bytes() for p in self.assets.glob('*.meta')};result=r.repair(self.project)
        self.assertEqual(result['patched_references'],2)
        for name in r.NAMES:self.assertIn(r.NEW,(self.assets/name).read_text())
        for name,data in before.items():self.assertEqual((self.assets/name).read_bytes(),data)
        self.assertEqual((self.assets/r.ATLAS).read_bytes(),(self.patch/r.ATLAS).read_bytes())
    def test_edited_source_is_preserved(self):
        (self.assets/r.NAMES[0]).write_text('edited')
        with self.assertRaises(ValueError):r.repair(self.project)
        self.assertFalse((self.assets/r.ATLAS).exists())
    def test_write_failure_restores_original_files(self):
        original=r.os.replace
        def fail(src,dst):
            if str(src).endswith(r.NAMES[1]+'.new'):raise OSError('simulated write failure')
            return original(src,dst)
        with patch.object(r.os,'replace',side_effect=fail),self.assertRaises(OSError):r.repair(self.project)
        for name,data in self.original.items():self.assertEqual((self.assets/name).read_bytes(),data)
        self.assertFalse((self.assets/r.ATLAS).exists())
