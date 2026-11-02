import io
from pathlib import Path
import stat
import tempfile
import unittest
from install_unity import unpack_odc, checked_archive


def entry(name, mode=stat.S_IFREG|0o755, data=b''):
    encoded=name.encode()+b'\0'
    header='070707'+''.join(f'{x:06o}' for x in (0,0,mode,0,0,1,0))+f'{0:011o}{len(encoded):06o}{len(data):011o}'
    return header.encode()+encoded+data


class InstallTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup); self.root=Path(self.temp.name)

    def test_keeps_binary_contents_and_executable_permissions(self):
        payload=entry('.',stat.S_IFDIR|0o755)+entry('./bin/player',data=b'\0binary\xff')+entry('TRAILER!!!')
        self.assertEqual(unpack_odc(io.BytesIO(payload),self.root),1)
        self.assertEqual((self.root/'bin/player').read_bytes(),b'\0binary\xff')

    def test_blocks_parent_traversal_and_absolute_paths(self):
        for name in ['../outside','/outside']:
            with self.subTest(name=name), self.assertRaises(ValueError): unpack_odc(io.BytesIO(entry(name)),self.root)
        self.assertEqual(list(self.root.iterdir()),[])

    def test_rejects_truncated_binary(self):
        with self.assertRaises(ValueError): unpack_odc(io.BytesIO(entry('file',data=b'test')[:-2]),self.root)

    def test_rejects_symbolic_links(self):
        with self.assertRaises(ValueError): unpack_odc(io.BytesIO(entry('link',stat.S_IFLNK|0o777,b'elsewhere')),self.root)

    def test_rejects_corrupt_cached_download(self):
        p=self.root/'archive';p.write_bytes(b'changed')
        with self.assertRaises(ValueError):checked_archive(p,7,'0'*64)
