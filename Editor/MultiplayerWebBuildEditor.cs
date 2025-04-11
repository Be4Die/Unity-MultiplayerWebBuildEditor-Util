using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Linq;

public static class MultiplayerWebBuildEditor
{
    [MenuItem("Tools/MultiplayerWebBuildEditor/Build")]
    private static void Build()
    {
        var path = EditorUtility.SaveFolderPanel("Select Build Folder", "", "");
        if (string.IsNullOrEmpty(path)) return;

        BuildClient(path);
        BuildServer(path);
        
        EditorUtility.RevealInFinder(Path.Combine(path, "client"));
    }

    [MenuItem("Tools/MultiplayerWebBuildEditor/Build and Run")]
    private static void BuildAndRun()
    {
        var path = EditorUtility.SaveFolderPanel("Select Build Folder", "", "");
        if (string.IsNullOrEmpty(path)) return;

        BuildClient(path);
        BuildServer(path);

        if (CopyStartExeToClient(path))
        {
            RunServerAndClient(path);
        }
        
        EditorUtility.RevealInFinder(Path.Combine(path, "client"));
    }

    [MenuItem("Tools/MultiplayerWebBuildEditor/Build Test Environment")]
    private static void BuildTestEnvironment()
    {
        var path = EditorUtility.SaveFolderPanel("Select Build Folder", "", "");
        if (string.IsNullOrEmpty(path)) return;

        BuildTestClient(path);
        BuildServer(path);
        CopyLoadTestToClient(path);
        
        EditorUtility.RevealInFinder(Path.Combine(path, "test_client"));
    }

    [MenuItem("Tools/MultiplayerWebBuildEditor/Build Test and Run")]
    private static void BuildTestAndRun()
    {
        var path = EditorUtility.SaveFolderPanel("Select Build Folder", "", "");
        if (string.IsNullOrEmpty(path)) return;

        BuildTestClient(path);
        BuildServer(path);
        CopyLoadTestToClient(path);

        RunServerAndTestClient(path);
        EditorUtility.RevealInFinder(Path.Combine(path, "test_client"));
    }

    private static void BuildClient(string rootPath)
    {
        var clientPath = Path.Combine(rootPath, "client");
        var buildOptions = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray(),
            locationPathName = clientPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.Development
        };
        BuildPipeline.BuildPlayer(buildOptions);
    }

    private static void BuildTestClient(string rootPath)
    {
        var clientPath = Path.Combine(rootPath, "test_client", "test_client.exe");
        var buildOptions = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray(),
            locationPathName = clientPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development | BuildOptions.EnableHeadlessMode
        };
        BuildPipeline.BuildPlayer(buildOptions);
    }

    private static void BuildServer(string rootPath)
    {
        var serverPath = Path.Combine(rootPath, "server", "server.exe");
        var buildOptions = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray(),
            locationPathName = serverPath,
            target = BuildTarget.StandaloneWindows64,
            subtarget = (int)StandaloneBuildSubtarget.Server,
            options = BuildOptions.Development | BuildOptions.EnableHeadlessMode
        };
        BuildPipeline.BuildPlayer(buildOptions);
    }

    private static bool CopyStartExeToClient(string rootPath)
    {
        var startExe = Resources.Load<TextAsset>("host-webgl");
        if (startExe == null)
        {
            UnityEngine.Debug.LogError("host-webgl.bytes not found in Resources");
            return false;
        }

        var destFolder = Path.Combine(rootPath, "client");
        var destPath = Path.Combine(destFolder, "host-webgl.exe");

        try
        {
            Directory.CreateDirectory(destFolder);
            File.WriteAllBytes(destPath, startExe.bytes);
            return true;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Error copying host-webgl.exe: {e}");
            return false;
        }
    }

    private static bool CopyLoadTestToClient(string rootPath)
    {
        var loadTest = Resources.Load<TextAsset>("load-test");
        if (loadTest == null)
        {
            UnityEngine.Debug.LogError("load-test.bytes not found in Resources");
            return false;
        }

        var destFolder = Path.Combine(rootPath, "test_client");
        var destPath = Path.Combine(destFolder, "load-test.exe");

        try
        {
            Directory.CreateDirectory(destFolder);
            File.WriteAllBytes(destPath, loadTest.bytes);
            return true;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Error copying load-test.exe: {e}");
            return false;
        }
    }

    private static void RunServerAndClient(string rootPath)
    {
        var serverPath = Path.Combine(rootPath, "server", "server.exe");
        var clientPath = Path.Combine(rootPath, "client", "host-webgl.exe");

        if (File.Exists(serverPath))
        {
            Process.Start(new ProcessStartInfo(serverPath) { 
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(serverPath)
            });
        }

        if (File.Exists(clientPath))
        {
            Process.Start(new ProcessStartInfo(clientPath) {
                Arguments = "8080",
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(clientPath)
            });
        }
    }

    private static void RunServerAndTestClient(string rootPath)
    {
        var serverPath = Path.Combine(rootPath, "server", "server.exe");
        var clientPath = Path.Combine(rootPath, "test_client", "load-test.exe");

        if (File.Exists(serverPath))
        {
            Process.Start(new ProcessStartInfo(serverPath) {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(serverPath)
            });
        }

        if (File.Exists(clientPath))
        {
            Process.Start(new ProcessStartInfo(clientPath) {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(clientPath)
            });
        }
    }
}