import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import unity_runner as r


class UnityRunnerTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name); self.project=self.root/'Game'
        (self.project/'Assets').mkdir(parents=True)
        (self.project/'ProjectSettings').mkdir()
        (self.project/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2022.3.62f2\n')
        self.editor=self.root/'Unity'; self.editor.write_text('test double')

    def fake(self, success=True, token=True, build=False):
        def invoke(command, **kwargs):
            request=json.loads(Path(command[command.index('-recoveryRequest')+1]).read_text())
            report={'token':request['token'] if token else 'stale', 'ok':success,
                    'unity_editor_verified':success,'build_verified':build}
            Path(request['report']).write_text(json.dumps(report))
            return subprocess.CompletedProcess(command,0)
        return invoke

    def test_zero_exit_without_report_is_failure(self):
        with patch.object(r.subprocess,'run',return_value=subprocess.CompletedProcess([],0)):
            result=r.run(self.project,self.editor)
        self.assertFalse(result['ok']); self.assertIn('error',result)

    def test_unity_failure_is_not_overridden_by_zero_exit(self):
        with patch.object(r.subprocess,'run',side_effect=self.fake(success=False)):
            self.assertFalse(r.run(self.project,self.editor)['ok'])

    def test_stale_report_is_rejected(self):
        with patch.object(r.subprocess,'run',side_effect=self.fake(token=False)):
            self.assertFalse(r.run(self.project,self.editor)['ok'])

    def test_valid_editor_report_does_not_claim_game_runtime(self):
        with patch.object(r.subprocess,'run',side_effect=self.fake()):
            result=r.run(self.project,self.editor)
        self.assertTrue(result['ok']); self.assertFalse(result['runtime_verified'])
        self.assertFalse(result['build_verified'])

    def test_build_requires_executable_even_when_report_claims_success(self):
        with patch.object(r.subprocess,'run',side_effect=self.fake(build=True)):
            result=r.run(self.project,self.editor,'build',output=self.root/'newbuild/Game.exe')
        self.assertFalse(result['ok']); self.assertFalse(result['build_verified'])

    def test_timeout_records_failure_and_keeps_logs(self):
        with patch.object(r.subprocess,'run',side_effect=subprocess.TimeoutExpired('Unity',1)):
            result=r.run(self.project,self.editor,timeout=1)
        self.assertFalse(result['ok']); self.assertTrue((Path(result['logs'])/'runner-result.json').exists())

    def test_existing_build_folder_is_not_overwritten(self):
        with self.assertRaises(ValueError):
            r.run(self.project,self.editor,'build',output=self.root/'Game.exe')
        self.assertFalse((self.project/'Assets/SolateriaRecoveryTools').exists())

    def test_missing_editor_does_not_modify_project(self):
        with self.assertRaises(ValueError): r.run(self.project,self.root/'absent')
        self.assertFalse((self.project/'RecoveryLogs').exists())
