using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildScript
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    static void Prepare()
    {
        AssetDatabase.Refresh();
        PlayerPrefabBuilder.Build();
        UiCatalogBuilder.Build();
        PrefabCatalogBuilder.Build();
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera") { tag = "MainCamera" };
        cam.AddComponent<Camera>();
        cam.AddComponent<AudioListener>();
        var ship = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Original/Ship/SkeldShip.prefab"));
        ship.name = "SkeldShip";
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.companyName = "AmongUsLan";
        PlayerSettings.productName = "HayUnImpostor";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
    }

    public static void BuildMac()
    {
        Prepare();
        Build(BuildTarget.StandaloneOSX, "Builds/macOS/HayUnImpostor.app");
    }

    public static void BuildWindows()
    {
        Prepare();
        Build(BuildTarget.StandaloneWindows64, "Builds/Windows/HayUnImpostor.exe");
    }

    static void Build(BuildTarget target, string path)
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = path,
            target = target,
            options = BuildOptions.None,
        });
        Debug.Log("Build " + target + ": " + report.summary.result + " errors=" + report.summary.totalErrors);
        EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
}
