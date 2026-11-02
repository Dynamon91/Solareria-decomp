from pathlib import Path
import tempfile,hashlib,json,unittest
from join_parts import join

class JoinTests(unittest.TestCase):
    def fixture(self):
        temp=tempfile.TemporaryDirectory();self.addCleanup(temp.cleanup);root=Path(temp.name);records=[];full=b''
        for i,b in enumerate([b'first',b'\0second'],1):
            name=f'Game.zip.{i:03d}';(root/name).write_bytes(b);records.append({'file_name':name,'size_bytes':len(b),'sha256':hashlib.sha256(b).hexdigest()});full+=b
        m=root/'manifest.json';m.write_text(json.dumps({'file_name':'Game.zip','file_size_bytes':len(full),'sha256':hashlib.sha256(full).hexdigest(),'volumes':records}));return root,m,full
    def test_verified_join_preserves_all_bytes(self):
        root,m,full=self.fixture();self.assertEqual(join(m).read_bytes(),full)
        with self.assertRaises(ValueError):join(m)
    def test_corruption_never_becomes_final_zip(self):
        root,m,_=self.fixture();(root/'Game.zip.002').write_bytes(b'bad')
        with self.assertRaises(ValueError):join(m)
        self.assertFalse((root/'Game.zip').exists())
    def test_path_traversal_is_rejected_before_writing(self):
        root,m,_=self.fixture();d=json.loads(m.read_text());d['file_name']='../outside.zip';m.write_text(json.dumps(d))
        with self.assertRaises(ValueError):join(m)
        self.assertFalse(list(root.glob('*.joining')))
