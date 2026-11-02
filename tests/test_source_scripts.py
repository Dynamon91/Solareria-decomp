"""Source-header provenance tests, independent of the optional UnityPy package."""
import argparse
import hashlib
import io
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace as N
import unittest
from unittest.mock import patch

import source_scripts as s


def item(name='', path='1', opaque=0):
    return {'collection':'CAB-example','path_id':path,'name':name,'script_file_id':0,'script_path_id':'0',
            'game_object_path_id':'0','source_bytes':32+opaque,'header_bytes':32,'opaque_payload_bytes':opaque,
            'raw_sha256':hashlib.sha256(b'original'+path.encode()).hexdigest()}


class SourceTests(unittest.TestCase):
    def test_header_inventory_reads_only_monobehaviours_and_preserves_int64_ids(self):
        class Obj:
            type=N(name='MonoBehaviour'); assets_file=N(name='CAB-test',unity_version=s.UNITY_VERSION)
            path_id=9223372036854775806;byte_start=100;byte_size=32;Position=100
            def parse_monobehaviour_head(self):
                self.Position=132
                return N(m_Name='',m_Script=N(m_FileID=0,m_PathID=0),m_GameObject=N(m_PathID=0))
            def get_raw_data(self):return bytes(32)
        result=s.inspect_environment(N(objects=[N(type=N(name='Texture2D')),Obj()]))
        self.assertEqual(result['mono_behaviours_read'],1)
        row=result['original_null_scripts'][0]
        self.assertEqual(row['path_id'],'9223372036854775806')
        self.assertEqual(row['opaque_payload_bytes'],0)
        self.assertEqual(row['raw_sha256'],hashlib.sha256(bytes(32)).hexdigest())

    def test_reader_errors_never_produce_a_verified_null(self):
        class Obj:
            type=N(name='MonoBehaviour');assets_file=N(name='CAB-test',unity_version=s.UNITY_VERSION);path_id=1
            def parse_monobehaviour_head(self):raise ValueError('corrupt header')
        result=s.inspect_environment(N(objects=[Obj()]))
        self.assertEqual(len(result['errors']),1)
        self.assertFalse(result['original_null_scripts'])

    def test_preexisting_nulls_are_classified_without_assigning_a_class(self):
        merged={'original_null_scripts':[item('old'),item('')],'errors':[]}
        export={'exported_null_scripts':[{'name':'old'},{'name':''}],'export_errors':[]}
        result=s.compare(merged,export)
        self.assertTrue(result['header_only_baseline_confirmed'])
        self.assertFalse(result['individual_export_identity_mapping_verified'])
        self.assertFalse(result['unity_editor_verified'])

    def test_opaque_payload_is_not_silently_declared_restored(self):
        result=s.compare({'original_null_scripts':[item('x',opaque=8)],'errors':[]},
                         {'exported_null_scripts':[{'name':'x'}],'export_errors':[]})
        self.assertFalse(result['header_only_baseline_confirmed'])
        self.assertEqual(result['source_opaque_payload_bytes'],8)

    def test_name_or_count_mismatch_remains_unverified(self):
        result=s.compare({'original_null_scripts':[item('original')],'errors':[]},
                         {'exported_null_scripts':[{'name':'different'}],'export_errors':[]})
        self.assertFalse(result['name_multisets_equal'])
        self.assertFalse(result['header_only_baseline_confirmed'])

    def test_source_reader_failure_prevents_a_false_match(self):
        result=s.compare({'original_null_scripts':[item('x')],'errors':[{'error':'read failed'}]},
                         {'exported_null_scripts':[{'name':'x'}],'export_errors':[]})
        self.assertEqual(result['status'],'incomplete_evidence')
        self.assertFalse(result['header_only_baseline_confirmed'])

    def test_duplicate_original_id_is_deduplicated_only_if_contents_match(self):
        row=item();good=s.merge_sources([{'original_null_scripts':[row]}, {'original_null_scripts':[dict(row)]}])
        self.assertEqual(len(good['original_null_scripts']),1)
        changed={**row,'raw_sha256':'changed'}
        bad=s.merge_sources([{'original_null_scripts':[row]}, {'original_null_scripts':[changed]}])
        self.assertEqual(len(bad['errors']),1)

    def test_worker_failure_is_retained_in_report(self):
        with tempfile.TemporaryDirectory() as root:
            source=Path(root)/'original.bundle';source.write_bytes(b'synthetic')
            with patch.object(s.subprocess,'run',return_value=N(returncode=1,stderr='reader crashed')):
                result=s.inspect_sources(source)
            self.assertEqual(len(result['merged']['errors']),1)
            self.assertIsNone(result['comparison'])

    def test_export_documents_with_multiple_objects_are_ambiguous(self):
        with tempfile.TemporaryDirectory() as root:
            project=Path(root);(project/'Assets').mkdir()
            (project/'Assets/X.asset.meta').write_text('guid: '+('1'*32)+'\n')
            (project/'Assets/X.asset').write_text('%YAML 1.1\n--- !u!114 &1\nMonoBehaviour:\n  m_Script: {fileID: 0}\n  m_Name: one\n--- !u!114 &2\nMonoBehaviour:\n  m_Script: {fileID: 1}\n  m_Name: two\n')
            result=s.exported_nulls(project)
            self.assertEqual(len(result['export_errors']),1)
            self.assertFalse(result['exported_null_scripts'])

    def test_quoted_names_and_empty_names_are_read_without_execution(self):
        self.assertEqual(s._scalar(''), '')
        self.assertEqual(s._scalar('"Time Dilation Track"'),'Time Dilation Track')
        self.assertEqual(s._scalar("'Bob''s asset'"),"Bob's asset")


if __name__=='__main__':unittest.main()
