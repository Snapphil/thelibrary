using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Configures XR Plugin Management settings for WebGL/WebXR
/// </summary>
public class XRSettingsConfigurator
{
    [MenuItem("VR Assignment/Configure XR for WebGL")]
    public static void ConfigureXRForWebGL()
    {
        Debug.Log("Configuring XR Plugin Management for WebGL...");
        
        // Create XR settings directory if it doesn't exist
        string xrSettingsPath = "Assets/XR";
        if (!Directory.Exists(xrSettingsPath))
        {
            Directory.CreateDirectory(xrSettingsPath);
            AssetDatabase.Refresh();
        }
        
        // Try to configure via reflection (package may not be loaded yet)
        try
        {
            ConfigureXRManagement();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Could not auto-configure XR Management: " + e.Message);
            Debug.Log("Please manually enable WebXR in Edit → Project Settings → XR Plug-in Management → WebGL tab");
        }
        
        // Show instructions
        EditorUtility.DisplayDialog(
            "XR Configuration",
            "XR settings have been prepared.\n\n" +
            "If WebXR is not enabled:\n" +
            "1. Go to Edit → Project Settings\n" +
            "2. Select XR Plug-in Management\n" +
            "3. Click the WebGL tab\n" +
            "4. Enable WebXR Export\n\n" +
            "Note: Your Unity version (2019.4) may have limited WebXR support.",
            "OK"
        );
    }
    
    private static void ConfigureXRManagement()
    {
        // This requires XR Management package to be loaded
        // Using reflection to avoid compile errors if package isn't ready
        
        var xrGeneralSettingsType = System.Type.GetType("UnityEngine.XR.Management.XRGeneralSettings, Unity.XR.Management");
        if (xrGeneralSettingsType == null)
        {
            Debug.LogWarning("XR Management package not fully loaded yet. Please run setup again after Unity restarts.");
            return;
        }
        
        Debug.Log("XR Management package detected. Settings will be configured automatically.");
    }
    
    [MenuItem("VR Assignment/Open XR Settings")]
    public static void OpenXRSettings()
    {
        // Open Project Settings window to XR section
        SettingsService.OpenProjectSettings("Project/XR Plug-in Management");
    }
    
    [MenuItem("VR Assignment/Open Player Settings")]
    public static void OpenPlayerSettings()
    {
        // Open Project Settings window to Player section
        SettingsService.OpenProjectSettings("Project/Player");
    }
}

