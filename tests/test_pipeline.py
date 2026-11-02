"""Failure-path tests with synthetic inputs; no game assets or .NET required."""
import argparse
from contextlib import contextmanager
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import shutil
import subprocess
import sys
import stat
import tempfile
import unittest
from unittest.mock import patch
import urllib.parse

PROJECT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('solateria_manage', PROJECT/'manage.py')
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)


class PipelineTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='solateria test ')
        self.root = Path(self.temp.name)/'repo with spaces'
        self.root.mkdir()
        self.root_patch = patch.object(m, 'ROOT', self.root); self.root_patch.start()
        self.addCleanup(self.root_patch.stop); self.addCleanup(self.temp.cleanup)
        self.data = self.root.parent/'game with spaces'/'Solateria_Data'
        (self.data/'Managed').mkdir(parents=True)
        self.expected = []
        for name in ['Core.dll', 'Extra.dll']:
            data = ('original '+name).encode()
            (self.data/'Managed'/name).write_bytes(data)
            self.expected.append({'name': name, 'bytes': len(data), 'sha256': hashlib.sha256(data).hexdigest()})
        (self.root/'references_manifest.json').write_text(json.dumps(self.expected))
        (self.root/'type_recovery.json').write_text('[]')
        (self.root/'NuGet.Config').write_text('<configuration/>')
        (self.root/'Build.proj').write_text('<Project/>')
        for name in ['globalgamemanagers', 'app.info', 'boot.config', 'resources.assets']:
            (self.data/name).write_text('test '+name)
        bundles = self.data/'StreamingAssets/aa/StandaloneWindows64'; bundles.mkdir(parents=True)
        self.bundle = bundles/'animation_assets_all.bundle'; self.bundle.write_bytes(b'synthetic bundle')
        self.tool = self.root/'synthetic tool.exe'; self.tool.write_bytes(b'not executable: mocked boundary')
        m.prepare(self.data)
        self.args = argparse.Namespace(asset_ripper=str(self.tool), dotnet=str(self.tool),
                                       bundle=self.bundle.name, heap_mib=2048, retry=False)
        self.output = self.root/'.local/assets'/self.bundle.stem
        self.fail_import = False
        self.processes = []

    @contextmanager
    def fake_exporter(self):
        owner = self
        class Process:
            def __init__(self, command, **kwargs):
                self.log = Path(command[command.index('--log-path')+1])
                self.log.write_text('')
                self.returncode = None
                self.terminated = False
                owner.processes.append(self)
                self.env = kwargs['env']
            def poll(self): return self.returncode
            def terminate(self): self.terminated = True; self.returncode = 0
            def wait(self, timeout=None): return self.returncode
            def kill(self): self.returncode = -9
        def urlopen(request, timeout=None):
            url = request.full_url if hasattr(request, 'full_url') else request
            route = urllib.parse.urlparse(url).path
            process = owner.processes[-1]
            if route == '/LoadFolder':
                process.log.write_text('Import [Error] : memory exhausted\n' if owner.fail_import else 'Import complete\n')
            if route == '/Export/UnityProject':
                out = Path(urllib.parse.parse_qs(request.data.decode())['path'][0])
                assets = out/'ExportedProject/Assets'; assets.mkdir(parents=True)
                (assets/'scene.asset').write_text('complete synthetic scene')
                (assets/'dependency.dll').write_bytes(b'external dependency')
                process.log.write_text('Export : Finished post-export\n')
            return io.BytesIO(b'<select name="ScriptExportMode"><option value="Hybrid" selected></option></select>')
        with patch.object(m.subprocess, 'Popen', Process), patch.object(m.urllib.request, 'urlopen', urlopen):
            yield

    def test_prepare_rejects_wrong_build_without_touching_previous(self):
        before = (self.root/'References/Core.dll').read_bytes()
        config = (self.root/'.local/config.json').read_bytes()
        (self.data/'Managed/Core.dll').write_bytes(b'wrong build')
        with self.assertRaises(ValueError): m.prepare(self.data)
        self.assertEqual(before, (self.root/'References/Core.dll').read_bytes())
        self.assertEqual(config, (self.root/'.local/config.json').read_bytes())

    def test_prepare_mid_copy_failure_preserves_dependencies(self):
        original = shutil.copyfile
        def copy(src, dst):
            if Path(src).name == 'Extra.dll': raise OSError('simulated full disk')
            return original(src, dst)
        with patch.object(m.shutil, 'copyfile', side_effect=copy), self.assertRaises(OSError): m.prepare(self.data)
        self.assertEqual(m.validate_references(self.root/'References'), m.validate_references(self.data/'Managed'))
        self.assertFalse(list((self.root/'.local').glob('references-new-*')))

    def test_read_only_input_is_copied_without_changing_original_permissions(self):
        source = self.data/'Managed/Core.dll'
        source.chmod(stat.S_IREAD)
        self.addCleanup(lambda: source.chmod(stat.S_IREAD | stat.S_IWRITE))
        before = source.stat().st_mode
        target = self.root/'staged.dll'
        m.link_or_copy(source, target)
        self.assertEqual(before, source.stat().st_mode)
        self.assertEqual(source.read_bytes(), target.read_bytes())
        self.assertNotEqual(source.stat().st_ino, target.stat().st_ino)
        self.assertTrue(target.stat().st_mode & stat.S_IWRITE)

    def test_prepare_replaces_old_read_only_reference_snapshot(self):
        (self.root/'References/Core.dll').chmod(stat.S_IREAD)
        m.prepare(self.data)
        m.validate_references(self.root/'References')
        self.assertTrue((self.root/'References/Core.dll').stat().st_mode & stat.S_IWRITE)
        self.assertFalse(list((self.root/'.local').glob('references-old-*')))

    def test_prepare_config_failure_rolls_back_the_directory(self):
        marker = self.root/'References/keep.txt'; marker.write_text('old snapshot')
        before = (self.root/'.local/config.json').read_bytes()
        with patch.object(m, 'write_json', side_effect=OSError('simulated write failure')), self.assertRaises(OSError):
            m.prepare(self.data)
        self.assertEqual(marker.read_text(), 'old snapshot')
        self.assertEqual(before, (self.root/'.local/config.json').read_bytes())
        self.assertFalse(list((self.root/'.local').glob('references-old-*')))

    def test_cache_corruption_cannot_replace_a_good_dll(self):
        output = self.root/'output'; output.mkdir()
        content = b'valid exported DLL'; (output/'example.dll').write_bytes(content)
        key = hashlib.sha256(content).hexdigest()
        cache = self.root/'.local/dll-cache'; cache.mkdir()
        (cache/(key+'.dll')).write_bytes(b'corrupt cached bytes')
        m.compact_dlls(output)
        self.assertEqual(content, (output/'example.dll').read_bytes())
        self.assertEqual(content, (cache/(key+'.dll')).read_bytes())

    def test_manifest_detects_deleted_modified_and_additional_files(self):
        folder = self.root/'files'; folder.mkdir(); file = folder/'x.asset'; file.write_text('original')
        manifest = m.file_manifest(folder)
        self.assertTrue(m.verify_manifest(folder, manifest))
        file.write_text('changed'); self.assertFalse(m.verify_manifest(folder, manifest))
        file.unlink(); self.assertFalse(m.verify_manifest(folder, manifest))
        file.write_text('original'); (folder/'extra.asset').write_text('extra')
        self.assertFalse(m.verify_manifest(folder, manifest))
        self.assertFalse(m.verify_manifest(folder, [{'invalid': True}]))

    def test_export_success_validates_files_and_cleans_process_and_stage(self):
        with self.fake_exporter(): m.export(self.args)
        state = m.read_checkpoint(self.output/'complete.json')
        self.assertTrue(m.verify_manifest(self.output, state['files']))
        self.assertTrue(self.processes[-1].terminated)
        self.assertEqual(self.processes[-1].env['DOTNET_GCHeapHardLimit'], '80000000')
        self.assertFalse(list((self.root/'.local/staging').iterdir()))

    def test_export_resume_does_not_start_another_process(self):
        with self.fake_exporter(): m.export(self.args)
        with patch.object(m.subprocess, 'Popen', side_effect=AssertionError('must skip')):
            m.export(self.args)

    def test_missing_asset_is_not_skipped_and_retry_preserves_old_output(self):
        with self.fake_exporter(): m.export(self.args)
        (self.output/'ExportedProject/Assets/scene.asset').unlink()
        note = self.output/'keep-note.txt'; note.write_text('preserve evidence')
        with self.assertRaisesRegex(ValueError, '--retry'): m.export(self.args)
        self.args.retry = True
        with self.fake_exporter(): m.export(self.args)
        previous = list((self.root/'.local/interrupted').glob(self.output.name+'.interrupted-*'))
        self.assertEqual(len(previous), 1)
        self.assertEqual((previous[0]/'keep-note.txt').read_text(), 'preserve evidence')
        self.assertTrue((self.output/'ExportedProject/Assets/scene.asset').exists())

    def test_changed_bundle_requires_retry(self):
        with self.fake_exporter(): m.export(self.args)
        self.bundle.write_bytes(b'new bundle bytes')
        with self.assertRaisesRegex(ValueError, '--retry'): m.export(self.args)

    def test_corrupt_checkpoint_can_be_retried(self):
        with self.fake_exporter(): m.export(self.args)
        (self.output/'complete.json').write_text('{broken')
        self.args.retry = True
        with self.fake_exporter(): m.export(self.args)
        self.assertTrue(m.verify_manifest(self.output, m.read_checkpoint(self.output/'complete.json')['files']))

    def test_import_failure_not_marked_complete_and_old_log_does_not_poison_retry(self):
        self.fail_import = True
        with self.fake_exporter(), self.assertRaisesRegex(RuntimeError, 'importazione'): m.export(self.args)
        self.assertFalse((self.output/'complete.json').exists())
        self.assertTrue(self.processes[-1].terminated)
        self.fail_import = False
        with self.fake_exporter(): m.export(self.args)
        self.assertTrue((self.output/'complete.json').exists())
        self.assertEqual(len(list((self.root/'.local/logs').glob('animation_assets_all-*-server.log'))), 2)

    def test_failed_process_start_still_removes_staging(self):
        with patch.object(m.subprocess, 'Popen', side_effect=OSError('bad executable')), self.assertRaises(OSError):
            m.export(self.args)
        self.assertFalse(list((self.root/'.local/staging').iterdir()))

    def test_missing_bundles_is_an_error_not_an_empty_success(self):
        self.bundle.unlink(); self.args.bundle = None
        with self.assertRaisesRegex(ValueError, 'Nessun bundle'): m.export(self.args)

    def test_lock_rejects_second_job_and_releases_after_error(self):
        with self.assertRaisesRegex(RuntimeError, 'first job'):
            with m.operation_lock():
                with self.assertRaisesRegex(ValueError, 'altro lavoro'):
                    with m.operation_lock(): pass
                raise RuntimeError('first job interrupted')
        with m.operation_lock(): pass

    def test_os_lock_is_released_after_abrupt_process_exit(self):
        code = ("import sys, os; from pathlib import Path; "
                "sys.path.insert(0, sys.argv[1]); import manage; manage.ROOT=Path(sys.argv[2]); "
                "lock=manage.operation_lock(); lock.__enter__(); os._exit(17)")
        result = subprocess.run([sys.executable, '-c', code, str(PROJECT), str(self.root)], timeout=10)
        self.assertEqual(result.returncode, 17)
        with m.operation_lock(): pass

    def test_doctor_accepts_sdk_and_reports_missing_dependencies(self):
        args = argparse.Namespace(dotnet=str(self.tool), ilspy=str(self.tool), asset_ripper=str(self.tool))
        with patch.object(m.subprocess, 'run', return_value=argparse.Namespace(stdout='8.0.100 [test]\n')):
            self.assertEqual(m.doctor(args), 0)
            (self.root/'References/Extra.dll').unlink()
            self.assertEqual(m.doctor(args), 1)
        self.assertFalse(m.read_checkpoint(self.root/'.local/doctor.json')['ok'])

    def test_doctor_rejects_runtime_without_sdk(self):
        args = argparse.Namespace(dotnet=str(self.tool), ilspy=None, asset_ripper=None)
        with patch.object(m.subprocess, 'run', return_value=argparse.Namespace(stdout='')):
            self.assertEqual(m.doctor(args), 1)

    def test_build_revalidates_every_reference_and_keeps_spaces_in_one_argument(self):
        with patch.object(m.subprocess, 'run') as run:
            m.build(str(self.tool), self.root)
            command = run.call_args.args[0]
            self.assertIn('-p:RecoveryRoot='+str(self.root), command)
            self.assertIn('-m:1', command)
            (self.root/'References/Extra.dll').write_bytes(b'corrupt')
            with self.assertRaisesRegex(ValueError, 'Extra.dll'): m.build(str(self.tool), self.root)
            self.assertEqual(run.call_count, 1)

    def test_decompile_resume_checks_files_and_reexports_interrupted_assembly(self):
        args = argparse.Namespace(ilspy=str(self.tool), dotnet=str(self.tool), heap_mib=1024,
                                  resume=False, verify=False, msbuild=None)
        def run(command, **kwargs):
            output = Path(command[command.index('-o')+1])
            (output/'Example.cs').write_text('class Example {}')
            (output/'Example.csproj').write_text('<Project><ItemGroup/></Project>')
            return argparse.Namespace(returncode=0)
        with patch.object(m, 'ASSEMBLIES', ['Example']), patch.object(m.subprocess, 'run', side_effect=run) as exporter:
            m.decompile(args)
            args.resume = True; m.decompile(args)
            self.assertEqual(exporter.call_count, 1)
            output = self.root/'.local/fresh/Code/Example'
            (output/'Example.cs').unlink()
            m.decompile(args)
            self.assertEqual(exporter.call_count, 2)
            self.assertTrue((output/'Example.cs').exists())
            self.assertEqual(len(list((self.root/'.local/interrupted').glob('Example.interrupted-*'))), 1)


if __name__ == '__main__': unittest.main()
