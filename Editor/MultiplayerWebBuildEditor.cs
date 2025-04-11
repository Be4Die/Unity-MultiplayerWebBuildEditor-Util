using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System;
using System.Linq;

public static class MultiplayerWebBuildEditor
{
    private const string GoServerName = "server_go.exe";

    [MenuItem("Tools/MultiplayerWebBuildEditor/Build")]
    private static void Build()
    {
        string path = EditorUtility.SaveFolderPanel("Select Build Folder", "", "");
        if (string.IsNullOrEmpty(path)) return;

        BuildClient(path);
        BuildServer(path);
        
        EditorUtility.RevealInFinder(Path.Combine(path, "client"));
    }

    [MenuItem("Tools/MultiplayerWebBuildEditor/Build and Run")]
    private static void BuildAndRun()
    {
        string path = EditorUtility.SaveFolderPanel("Select Build Folder", "", "");
        if (string.IsNullOrEmpty(path)) return;

        BuildClient(path);
        BuildServer(path);

        if (CopyGoServerToBuild(path))
        {
            RunServerAndClient(path);
        }
        
        EditorUtility.RevealInFinder(Path.Combine(path, "client"));
    }

    [MenuItem("Tools/MultiplayerWebBuildEditor/Run Load Test")]
    private static void RunLoadTest()
    {
        string path = EditorUtility.OpenFolderPanel("Select Build Folder", "", "");
        if (string.IsNullOrEmpty(path)) return;

        string goServerPath = Path.Combine(path, "client", GoServerName);
        
        if (File.Exists(goServerPath))
        {
            var process = new Process {
                StartInfo = {
                    FileName = goServerPath,
                    Arguments = "--ldt-clients 500 --ldt-step 20",
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(goServerPath)
                }
            };
            process.Start();
        }
        else
        {
            UnityEngine.Debug.LogError("Go server executable not found!");
        }
    }

    private static void BuildClient(string rootPath)
    {
        string clientPath = Path.Combine(rootPath, "client");
        BuildPlayerOptions buildOptions = new BuildPlayerOptions {
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

    private static void BuildServer(string rootPath)
    {
        string serverPath = Path.Combine(rootPath, "server");
        BuildPlayerOptions buildOptions = new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray(),
            locationPathName = Path.Combine(serverPath, "server.exe"),
            target = BuildTarget.StandaloneWindows64,
            subtarget = (int)StandaloneBuildSubtarget.Server,
            options = BuildOptions.Development
        };
        BuildPipeline.BuildPlayer(buildOptions);
    }

    private static bool CopyGoServerToBuild(string rootPath)
    {
        TextAsset goServer = Resources.Load<TextAsset>("start");
        if (goServer == null)
        {
            UnityEngine.Debug.LogError("Go server not found in Resources");
            return false;
        }

        string destFolder = Path.Combine(rootPath, "client");
        string destPath = Path.Combine(destFolder, GoServerName);

        try
        {
            File.WriteAllBytes(destPath, goServer.bytes);
            return true;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Error copying Go server: {e}");
            return false;
        }
    }

    private static void RunServerAndClient(string rootPath)
    {
        string gameServerPath = Path.Combine(rootPath, "server", "server.exe");
        if (File.Exists(gameServerPath))
        {
            Process.Start(new ProcessStartInfo(gameServerPath) { 
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(gameServerPath)
            });
        }

        string goServerPath = Path.Combine(rootPath, "client", GoServerName);
        if (File.Exists(goServerPath))
        {
            Process.Start(new ProcessStartInfo(goServerPath) {
                Arguments = "--serv --port 8080",
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(goServerPath)
            });
        }
    }
}