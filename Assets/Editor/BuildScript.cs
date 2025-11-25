using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Build script for WebGL deployment
/// Can be called from command line for automated builds
/// </summary>
public class BuildScript
{
    [MenuItem("Build/Build WebGL")]
    public static void BuildWebGL()
    {
        // Always use ResearchVault scene
        string[] scenes = new string[] { "Assets/Scenes/ResearchVault.unity" };
        
        // Set build path
        string buildPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "WebBuild");
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }
        
        // Build options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        
        // Perform build
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + buildPath);
            Debug.Log("Total size: " + report.summary.totalSize + " bytes");
            Debug.Log("Total time: " + report.summary.totalTime);
        }
        else
        {
            Debug.LogError("Build failed with " + report.summary.totalErrors + " errors");
        }
    }
    
    [MenuItem("Build/Build WebGL (Development)")]
    public static void BuildWebGLDevelopment()
    {
        string[] scenes = GetScenesFromBuildSettings();
        
        if (scenes.Length == 0)
        {
            scenes = new string[] { "Assets/Scenes/ResearchVault.unity" };
        }
        
        string buildPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "WebBuild_Dev");
        
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }
        
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.Development | BuildOptions.AllowDebugging
        };
        
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("Development build succeeded: " + buildPath);
        }
        else
        {
            Debug.LogError("Development build failed");
        }
    }
    
    private static string[] GetScenesFromBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
            }
        }
        return scenes.ToArray();
    }
    
    // Command line build method
    public static void PerformBuild()
    {
        BuildWebGL();
    }
}

