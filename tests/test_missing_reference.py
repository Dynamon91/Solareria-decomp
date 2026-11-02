from pathlib import Path
import tempfile
import unittest
from asset_audit import audit, ASSET_RIPPER_MISSING_REFERENCE


class MissingReferenceTests(unittest.TestCase):
    def test_export_placeholder_cannot_be_resolved_by_inventing_an_asset(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);assets=root/'Assets';assets.mkdir()
            (assets/'Material.mat').write_text('%YAML 1.1\n--- !u!21 &2100000\nMaterial:\n  m_Shader: {fileID: 4800000, guid: '+ASSET_RIPPER_MISSING_REFERENCE+', type: 3}\n')
            (assets/'Material.mat.meta').write_text('guid: '+'a'*32+'\n')
            (assets/'Invented.asset').write_text('not a recovered asset')
            (assets/'Invented.asset.meta').write_text('guid: '+ASSET_RIPPER_MISSING_REFERENCE+'\n')
            report=audit(root);missing=report['projects'][0]['missing_local_guids']
            self.assertEqual(len(missing),1)
            self.assertEqual(missing[0]['reference_kind'],'assetripper_missing_reference')
            self.assertEqual(missing[0]['candidate_provider_count'],0)
