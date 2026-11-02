import hashlib
import json
from pathlib import Path
import struct
import tempfile
import unittest
from unittest.mock import patch
import native_plugins as n


def pe(machine=0x8664,managed=False):
    b=bytearray(512);b[:2]=b'MZ';struct.pack_into('<I',b,60,64);b[64:68]=b'PE\0\0'
    struct.pack_into('<H',b,68,machine);struct.pack_into('<H',b,88,0x20b)
    if managed:struct.pack_into('<II',b,64+24+112+14*8,1,1)
    return bytes(b)


class NativeTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup);self.root=Path(self.tmp.name)
        self.source=self.root/'source';self.source.mkdir();self.project=self.root/'project';(self.project/'Assets').mkdir(parents=True)
        manifest=[]
        for name in ['fmodstudio.dll','resonanceaudio.dll','Rewired_DirectInput.dll','Rewired_WindowsGamingInput.dll']:
            data=pe();(self.source/name).write_bytes(data)
            manifest.append({'name':name,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()})
        (self.root/'native_plugins_manifest.json').write_text(json.dumps(manifest))
        p=patch.object(n,'ROOT',self.root);p.start();self.addCleanup(p.stop)

    def test_pairs_plugins_with_windows_only_metadata(self):
        result=n.install(self.project,self.source);folder=self.project/n.TARGET
        self.assertEqual(len(list(folder.glob('*.dll'))),4);self.assertEqual(len(list(folder.glob('*.meta'))),4)
        self.assertIn('OS: Windows',(folder/'fmodstudio.dll.meta').read_text());self.assertFalse(result['native_runtime_verified'])
        for path in folder.glob('*.dll'):self.assertEqual(path.read_bytes(),(self.source/path.name).read_bytes())

    def test_corrupt_plugin_prevents_all_writes(self):
        (self.source/'fmodstudio.dll').write_bytes(pe()+b'changed')
        with self.assertRaises(ValueError):n.install(self.project,self.source)
        self.assertFalse((self.project/n.TARGET).exists())

    def test_existing_duplicate_is_preserved(self):
        (self.project/'Assets/fmodstudio.dll').write_bytes(b'existing')
        with self.assertRaises(ValueError):n.install(self.project,self.source)
        self.assertEqual((self.project/'Assets/fmodstudio.dll').read_bytes(),b'existing')

    def test_managed_and_wrong_architecture_are_rejected(self):
        for data in [pe(managed=True),pe(machine=0x14c),b'MZ']:
            p=self.root/'bad.dll';p.write_bytes(data)
            with self.assertRaises(ValueError):n.inspect(p)
