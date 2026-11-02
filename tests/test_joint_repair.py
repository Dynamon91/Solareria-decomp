from pathlib import Path
import os,tempfile,unittest
from unittest.mock import patch
from repair_joint_export import repair

class JointRepairTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup);self.root=Path(self.tmp.name)
        self.assets=self.root/'Assets/Scenes';self.assets.mkdir(parents=True)
        self.data=self.assets/'WorldsEnd_01/LightingData.asset';self.active=self.assets/'WorldsEnd_01/LightingData_0.asset'
        self.write(self.data,'a','%YAML 1.1\n--- !u!1120 &112000000\nLightingDataAsset:\n  m_Scene: {fileID: 102900000, guid: '+'b'*32+', type: 3}\n')
        self.write(self.active,'d','%YAML 1.1\n--- !u!1120 &112000000\nLightingDataAsset:\n  m_Scene: {fileID: 102900000, guid: '+'c'*32+', type: 3}\n')
        self.write(self.assets/'WorldsEnd_01.unity','c','%YAML 1.1\n--- !u!157 &1\nLightmapSettings:\n  m_LightingDataAsset: {fileID: 112000000, guid: '+'d'*32+', type: 2}\n')
        self.expected_case=int(os.name!='nt')
        self.write(self.assets/'Bloom.asset','e','%YAML 1.1\nA: upper\n')
        if self.expected_case:self.write(self.assets/'bloom.asset','f','%YAML 1.1\nA: lower\n')
    def write(self,path,letter,text):
        path.parent.mkdir(parents=True,exist_ok=True);path.write_text(text);Path(str(path)+'.meta').write_text('guid: '+letter*32+'\n')
    def test_quarantines_only_unused_data_and_preserves_active_scene(self):
        original=self.active.read_bytes();result=repair(self.root)
        self.assertEqual(result['after']['missing_local_guid_count'],0);self.assertEqual(result['after']['case_conflict_count'],0)
        self.assertEqual(self.active.read_bytes(),original);self.assertFalse(self.data.exists())
        self.assertEqual(result['quarantined_generated_lighting_assets'],1);self.assertEqual(result['renamed_asset_meta_pairs'],self.expected_case)
        self.assertTrue(list((self.root/'RecoveryRepairs').rglob('LightingData.asset')))
    def test_referenced_data_is_never_quarantined(self):
        self.write(self.assets/'Use.asset','1','%YAML 1.1\nUse: {fileID: 112000000, guid: '+'a'*32+', type: 2}\n')
        with self.assertRaises(ValueError):repair(self.root)
        self.assertTrue(self.data.exists())
    def test_partial_move_failure_restores_source_and_meta(self):
        rename=Path.rename
        def fail(path,target):
            if path.name=='LightingData.asset.meta' and 'quarantine' in str(target):raise OSError('simulated write failure')
            return rename(path,target)
        with patch.object(Path,'rename',fail),self.assertRaises(OSError):repair(self.root)
        self.assertTrue(self.data.exists());self.assertTrue(Path(str(self.data)+'.meta').exists())
