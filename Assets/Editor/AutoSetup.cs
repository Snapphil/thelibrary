using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// Automatic setup script that configures the VR project when Unity opens.
/// This script runs once and sets up everything automatically.
/// </summary>
[InitializeOnLoad]
public class AutoSetup
{
    private const string SETUP_COMPLETE_KEY = "VRAssignment9_SetupComplete";
    
    static AutoSetup()
    {
        // Run setup when Unity loads
        EditorApplication.delayCall += RunSetup;
    }
    
    [MenuItem("VR Assignment/Run Full Setup")]
    public static void RunSetupManual()
    {
        // Reset the flag to allow re-running
        EditorPrefs.SetBool(SETUP_COMPLETE_KEY, false);
        RunSetup();
    }
    
    [MenuItem("VR Assignment/Setup Scene Only")]
    public static void SetupSceneOnly()
    {
        SetupScene();
    }
    
    [MenuItem("VR Assignment/Configure Build Settings")]
    public static void ConfigureBuildSettingsMenu()
    {
        ConfigureBuildSettings();
    }
    
    private static void RunSetup()
    {
        // Check if setup has already been done
        if (EditorPrefs.GetBool(SETUP_COMPLETE_KEY, false))
        {
            return;
        }
        
        Debug.Log("=== VR Assignment 9 - Auto Setup Starting ===");
        
        // Configure project settings
        ConfigurePlayerSettings();
        
        // Configure build settings
        ConfigureBuildSettings();
        
        // Setup the scene
        SetupScene();
        
        // Configure XR settings
        ConfigureXRSettings();
        
        // Mark setup as complete
        EditorPrefs.SetBool(SETUP_COMPLETE_KEY, true);
        
        Debug.Log("=== VR Assignment 9 - Auto Setup Complete! ===");
        Debug.Log("Press PLAY to test your VR app!");
        Debug.Log("Use Build → Build WebGL to create a web build.");
        
        // Show completion dialog
        EditorUtility.DisplayDialog(
            "VR Assignment 9 - Setup Complete!",
            "Your VR project has been automatically configured!\n\n" +
            "✓ Scene setup with AppController\n" +
            "✓ VRAppController script attached\n" +
            "✓ Build settings configured for WebGL\n" +
            "✓ Player settings optimized\n\n" +
            "Press PLAY to test, or use Build → Build WebGL to create your web build.",
            "OK"
        );
    }
    
    private static void ConfigurePlayerSettings()
    {
        Debug.Log("Configuring Player Settings...");
        
        // Set company and product name
        PlayerSettings.companyName = "VR Assignment";
        PlayerSettings.productName = "VR Assignment 9";
        
        // WebGL specific settings
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.memorySize = 256;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
        
        // General settings
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.runInBackground = true;
        
        // Set color space (Linear is better for VR, but Gamma is more compatible)
        // Keeping existing setting to avoid issues
        
        Debug.Log("Player Settings configured.");
    }
    
    private static void ConfigureBuildSettings()
    {
        Debug.Log("Configuring Build Settings...");
        
        // Get the sample scene path
        string scenePath = "Assets/Scenes/SampleScene.unity";
        
        // Check if scene exists
        if (!File.Exists(Path.Combine(Application.dataPath, "../", scenePath)))
        {
            Debug.LogWarning("SampleScene.unity not found at expected path.");
            return;
        }
        
        // Add scene to build settings
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
        
        EditorBuildSettings.scenes = scenes;
        
        Debug.Log("Build Settings configured with SampleScene.");
    }
    
    private static void SetupScene()
    {
        Debug.Log("Setting up Scene...");
        
        // Open the sample scene
        string scenePath = "Assets/Scenes/SampleScene.unity";
        
        if (!File.Exists(Path.Combine(Application.dataPath, "../", scenePath)))
        {
            Debug.LogWarning("SampleScene.unity not found. Creating new scene...");
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(newScene, scenePath);
        }
        
        // Open the scene
        EditorSceneManager.OpenScene(scenePath);
        
        // Check if AppController already exists
        GameObject existingController = GameObject.Find("AppController");
        if (existingController != null)
        {
            Debug.Log("AppController already exists in scene.");
            
            // Make sure it has the VRAppController component
            if (existingController.GetComponent<VRAppController>() == null)
            {
                existingController.AddComponent<VRAppController>();
                Debug.Log("Added VRAppController component to existing AppController.");
            }
        }
        else
        {
            // Create AppController GameObject
            GameObject appController = new GameObject("AppController");
            
            // Add VRAppController component
            VRAppController vrController = appController.AddComponent<VRAppController>();
            
            // Configure default settings
            vrController.appTheme = VRAppController.AppTheme.MentalHealthRelaxation;
            vrController.gazeTime = 2f;
            vrController.interactionDistance = 10f;
            
            Debug.Log("Created AppController with VRAppController component.");
        }
        
        // Clean up default objects that might interfere
        CleanupDefaultObjects();
        
        // Save the scene
        EditorSceneManager.SaveOpenScenes();
        
        Debug.Log("Scene setup complete.");
    }
    
    private static void CleanupDefaultObjects()
    {
        // Remove default directional light (VRAppController creates its own)
        GameObject defaultLight = GameObject.Find("Directional Light");
        if (defaultLight != null)
        {
            Object.DestroyImmediate(defaultLight);
            Debug.Log("Removed default Directional Light (VRAppController creates its own).");
        }
        
        // The VRAppController will handle camera setup, but keep the default camera
        // so the script can find and configure it
    }
    
    private static void ConfigureXRSettings()
    {
        Debug.Log("Configuring XR Settings...");
        
        // Note: Full XR configuration requires the XR Management package to be fully loaded
        // This is a placeholder - the settings will need to be verified in the editor
        
        // Try to enable VR support (legacy setting)
        PlayerSettings.virtualRealitySupported = true;
        
        Debug.Log("XR Settings configured (verify in Edit → Project Settings → XR Plug-in Management).");
    }
}

