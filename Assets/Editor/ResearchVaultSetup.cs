using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor utility for setting up the Research Vault scene
/// </summary>
public class ResearchVaultSetup : EditorWindow
{
    [MenuItem("Research Vault/Setup New Scene")]
    public static void SetupScene()
    {
        // Create new scene
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // Create the controller GameObject
        GameObject controller = new GameObject("ResearchVaultController");
        controller.AddComponent<ResearchVaultController>();
        
        // Save the scene
        string scenePath = "Assets/Scenes/ResearchVault.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        
        Debug.Log("Research Vault scene created at: " + scenePath);
        Debug.Log("Press Play to generate the voxel library!");
        
        // Select the controller
        Selection.activeGameObject = controller;
    }
    
    [MenuItem("Research Vault/Open Documentation")]
    public static void OpenDocumentation()
    {
        EditorWindow.GetWindow<ResearchVaultDocWindow>("Research Vault");
    }
    
    [MenuItem("Research Vault/Build WebGL")]
    public static void BuildWebGL()
    {
        // Set WebGL template
        PlayerSettings.WebGL.template = "PROJECT:WebXR";
        
        // Build settings
        BuildPlayerOptions buildOptions = new BuildPlayerOptions();
        buildOptions.scenes = new[] { "Assets/Scenes/ResearchVault.unity" };
        buildOptions.locationPathName = "Build/WebGL";
        buildOptions.target = BuildTarget.WebGL;
        buildOptions.options = BuildOptions.None;
        
        // Build
        var report = BuildPipeline.BuildPlayer(buildOptions);
        
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("WebGL Build Succeeded! Output: Build/WebGL");
            EditorUtility.RevealInFinder("Build/WebGL");
        }
        else
        {
            Debug.LogError("WebGL Build Failed!");
        }
    }
}

/// <summary>
/// Documentation window for Research Vault
/// </summary>
public class ResearchVaultDocWindow : EditorWindow
{
    private Vector2 scrollPos;
    
    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 20;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 14;
        
        GUIStyle bodyStyle = new GUIStyle(EditorStyles.label);
        bodyStyle.wordWrap = true;
        
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("📚 Research Vault", titleStyle);
        EditorGUILayout.LabelField("A Voxel-Style Virtual Reality Academic Library", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(20);
        
        EditorGUILayout.LabelField("Overview", headerStyle);
        EditorGUILayout.LabelField(
            "Research Vault is a Minecraft-style two-floor library where users can explore and view interactive HTML visualizations of research papers on embedded tablets.",
            bodyStyle);
        EditorGUILayout.Space(15);
        
        EditorGUILayout.LabelField("Features", headerStyle);
        EditorGUILayout.LabelField("• Procedurally generated voxel library (~20,000 bricks)", bodyStyle);
        EditorGUILayout.LabelField("• Two floors with stairs, bookshelves, and reading alcoves", bodyStyle);
        EditorGUILayout.LabelField("• 4 interactive tablets (2 per floor)", bodyStyle);
        EditorGUILayout.LabelField("• Gaze-based interaction (look at tablet for 2 seconds)", bodyStyle);
        EditorGUILayout.LabelField("• HTML research paper visualizations", bodyStyle);
        EditorGUILayout.Space(15);
        
        EditorGUILayout.LabelField("Controls", headerStyle);
        EditorGUILayout.LabelField("• WASD - Move around", bodyStyle);
        EditorGUILayout.LabelField("• Mouse - Look around", bodyStyle);
        EditorGUILayout.LabelField("• Gaze at tablet for 2s - View paper", bodyStyle);
        EditorGUILayout.LabelField("• ESC - Close paper / Unlock cursor", bodyStyle);
        EditorGUILayout.Space(15);
        
        EditorGUILayout.LabelField("Research Papers", headerStyle);
        EditorGUILayout.LabelField("1. Neural Networks & Deep Learning", bodyStyle);
        EditorGUILayout.LabelField("2. Climate Change Data Analysis", bodyStyle);
        EditorGUILayout.LabelField("3. Quantum Computing Fundamentals", bodyStyle);
        EditorGUILayout.LabelField("4. Human-Computer Interaction", bodyStyle);
        EditorGUILayout.Space(15);
        
        EditorGUILayout.LabelField("Quick Start", headerStyle);
        EditorGUILayout.LabelField("1. Go to Research Vault > Setup New Scene", bodyStyle);
        EditorGUILayout.LabelField("2. Press Play to generate the library", bodyStyle);
        EditorGUILayout.LabelField("3. Use Research Vault > Build WebGL to deploy", bodyStyle);
        EditorGUILayout.Space(15);
        
        if (GUILayout.Button("Setup New Scene", GUILayout.Height(30)))
        {
            ResearchVaultSetup.SetupScene();
        }
        
        if (GUILayout.Button("Build WebGL", GUILayout.Height(30)))
        {
            ResearchVaultSetup.BuildWebGL();
        }
        
        EditorGUILayout.EndScrollView();
    }
}

