using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System;
using System.Linq;

public static class MultiplayerWebBuildEditor
{
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

        if (CopyStartExeToClient(path))
        {
            RunServerAndClient(path);
        }
        
        EditorUtility.RevealInFinder(Path.Combine(path, "client"));
    }

    private static void BuildClient(string rootPath)
    {
        string clientPath = Path.Combine(rootPath, "client");
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
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

    private static void BuildServer(string rootPath)
    {
        string serverPath = Path.Combine(rootPath, "server");
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray(),
            locationPathName = Path.Combine(serverPath, "Server.exe"),
            target = BuildTarget.StandaloneWindows64,
            subtarget = (int)StandaloneBuildSubtarget.Server,
            options = BuildOptions.Development
        };
        BuildPipeline.BuildPlayer(buildOptions);
    }

    private static bool CopyStartExeToClient(string rootPath)
    {
        TextAsset startExe = Resources.Load<TextAsset>("start");
        if (startExe == null)
        {
            UnityEngine.Debug.LogError("Start.exe not found in Resources");
            return false;
        }

        string destFolder = Path.Combine(rootPath, "client");
        string destPath = Path.Combine(destFolder, "start.exe");

        try
        {
            File.WriteAllBytes(destPath, startExe.bytes);
            return true;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Error copying start.exe: {e}");
            return false;
        }
    }

    private static void RunServerAndClient(string rootPath)
    {
        string serverPath = Path.Combine(rootPath, "server", "Server.exe");
        string clientPath = Path.Combine(rootPath, "client", "start.exe");

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
}