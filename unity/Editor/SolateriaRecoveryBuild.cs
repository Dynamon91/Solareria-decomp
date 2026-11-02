// Installed only in the integration candidate's Editor folder.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SolateriaRecoveryBuild
{
    [Serializable] public class Request { public string mode; public string report; public string output; public string[] scenes; public string token; }
    [Serializable] public class Result {
        public string token; public string editorVersion; public bool ok;
        public bool unity_editor_verified; public bool build_verified;
        public bool runtime_verified = false;
        public string[] scenes; public string[] errors; public string buildResult;
        public long missingSceneScripts;
    }
    public static void Run()
    {
        Request request = null;
        var result = new Result { editorVersion = Application.unityVersion };
        var errors = new List<string>();
        try {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-recoveryRequest");
            if (index < 0 || index + 1 >= args.Length) throw new Exception("Missing recovery request");
            request = JsonUtility.FromJson<Request>(File.ReadAllText(args[index + 1]));
            result.token = request.token;
            if (Application.unityVersion != "2022.3.62f2") throw new Exception("Expected Unity 2022.3.62f2");
            if (request.mode != "validate" && request.mode != "build") throw new Exception("Invalid mode");
            var scenes = new List<string>();
            if (request.scenes != null && request.scenes.Length > 0) scenes.AddRange(request.scenes);
            else foreach (var scene in EditorBuildSettings.scenes.Where(s => s.enabled)) {
                // Exports move scene paths; retain configured order and resolve the original GUID.
                string path = AssetDatabase.GUIDToAssetPath(scene.guid.ToString());
                scenes.Add(string.IsNullOrEmpty(path) ? scene.path : path);
            }
            if (scenes.Count == 0) throw new Exception("No startup scene configured; supply --scene Assets/...unity in startup order");
            if (scenes.Distinct().Count() != scenes.Count) throw new Exception("Duplicate build scenes");
            result.scenes = scenes.ToArray();
            foreach (string path in scenes) {
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !path.EndsWith(".unity", StringComparison.Ordinal) || AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    throw new Exception("Missing scene: " + path);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var node in root.GetComponentsInChildren<Transform>(true))
                        result.missingSceneScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject);
            }
            if (result.missingSceneScripts > 0) throw new Exception("Missing component scripts in build scenes: " + result.missingSceneScripts);
            result.unity_editor_verified = true;
            if (request.mode == "build") {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64) throw new Exception("Expected Windows64 target");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = scenes.ToArray(), locationPathName = request.output,
                    target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
                });
                result.buildResult = report.summary.result.ToString();
                result.build_verified = report.summary.result == BuildResult.Succeeded && File.Exists(request.output);
                if (!result.build_verified) throw new Exception("Build failed: " + result.buildResult);
            }
            result.ok = true;
        } catch (Exception error) {
            errors.Add(error.ToString()); Debug.LogException(error);
        } finally {
            result.errors = errors.ToArray();
            if (request != null) File.WriteAllText(request.report, JsonUtility.ToJson(result, true));
            EditorApplication.Exit(result.ok ? 0 : 2);
        }
    }
}
