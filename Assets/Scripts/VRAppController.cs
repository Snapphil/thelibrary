using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VR App Controller - Single file VR experience
/// Assignment 9: Creates a complete VR environment with gaze-based interaction
/// </summary>
public class VRAppController : MonoBehaviour
{
    // ============================================
    // CONFIGURATION - Customize these in Inspector
    // ============================================
    
    public enum AppTheme
    {
        OceanConservation, // New Social Cause Theme
        MentalHealthRelaxation,
        EducationalLearning,
        FitnessWellness,
        ProductivityFocus,
        SocialConnection
    }
    
    [Header("App Settings")]
    [Tooltip("Choose the theme for your VR experience")]
    public AppTheme appTheme = AppTheme.OceanConservation; // Default to new theme
    
    [Tooltip("Time in seconds to look at a button to activate it")]
    [Range(0.5f, 5f)]
    public float gazeTime = 2f;
    
    [Tooltip("Maximum distance for gaze interaction")]
    [Range(5f, 50f)]
    public float interactionDistance = 10f;
    
    [Header("Visual Settings")]
    public Color skyColorTop = new Color(0.4f, 0.6f, 0.9f);
    public Color skyColorBottom = new Color(0.8f, 0.9f, 1f);
    public Color ambientLightColor = new Color(0.9f, 0.85f, 0.8f);
    
    // ============================================
    // INTERNAL VARIABLES
    // ============================================
    
    private Camera mainCamera;
    private GameObject environmentRoot;
    private GameObject uiRoot;
    private GameObject reticle;
    private GameObject progressIndicator;
    private Text messageText;
    private Text titleText;
    
    private GameObject currentTarget;
    private float gazeTimer = 0f;
    private bool isGazing = false;
    
    private List<InteractiveButton> buttons = new List<InteractiveButton>();
    
    // Floating Panel System
    private GameObject floatingPanel;
    private GameObject tabletObject;
    private bool isPanelOpen = false;
    private bool isDraggingPanel = false;
    private bool isResizingPanel = false;
    private Vector3 panelDragOffset;
    private float panelScale = 1f;
    private const float MIN_PANEL_SCALE = 0.5f;
    private const float MAX_PANEL_SCALE = 2f;
    
    // Button class for tracking interactive elements
    private class InteractiveButton
    {
        public GameObject gameObject;
        public string label;
        public System.Action callback;
        public Color originalColor;
        public Color hoverColor;
        public Text labelText;
    }
    
    // ============================================
    // UNITY LIFECYCLE
    // ============================================
    
    void Start()
    {
        InitializeVREnvironment();
    }
    
    void Update()
    {
        HandleGazeInteraction();
        HandleKeyboardInput();
        UpdatePanelDragging();
    }
    
    // ============================================
    // INITIALIZATION
    // ============================================
    
    void InitializeVREnvironment()
    {
        // Create root objects
        environmentRoot = new GameObject("Environment");
        uiRoot = new GameObject("UI");
        
        // Setup camera
        SetupCamera();
        
        // Setup lighting
        SetupLighting();
        
        // Create environment based on theme
        CreateEnvironment();
        
        // Create UI elements
        CreateUI();
        
        // Create gaze reticle
        CreateReticle();
        
        // Create interactive tablet
        CreateInteractiveTablet();
        
        // Create floating panel (hidden initially)
        CreateFloatingPanel();
        
        // Show welcome message
        ShowWelcomeMessage();
        
        Debug.Log("VR Environment Initialized - Theme: " + appTheme);
    }
    
    void SetupCamera()
    {
        // Find or create main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCamera = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";
        }
        
        mainCamera.transform.position = new Vector3(0, 1.6f, 0);
        mainCamera.transform.rotation = Quaternion.identity;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = skyColorBottom;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 1000f;
        
        // Disable any existing camera controllers that might interfere
        var existingController = mainCamera.GetComponent("SimpleCameraController") as MonoBehaviour;
        if (existingController != null)
        {
            existingController.enabled = false;
        }
        
        // Add our desktop camera controller
        if (mainCamera.GetComponent<DesktopCameraController>() == null)
        {
            mainCamera.gameObject.AddComponent<DesktopCameraController>();
        }
    }
    
    void SetupLighting()
    {
        // Main directional light
        GameObject lightObj = new GameObject("Directional Light");
        Light mainLight = lightObj.AddComponent<Light>();
        mainLight.type = LightType.Directional;
        mainLight.color = new Color(1f, 0.95f, 0.9f);
        mainLight.intensity = 1.2f;
        mainLight.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        
        // Ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientLightColor;
        
        // Fog for depth
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = skyColorBottom;
        RenderSettings.fogStartDistance = 30f;
        RenderSettings.fogEndDistance = 100f;
    }
    
    // ============================================
    // ENVIRONMENT CREATION
    // ============================================
    
    void CreateEnvironment()
    {
        switch (appTheme)
        {
            case AppTheme.OceanConservation:
                CreateOceanConservationEnvironment();
                CreateOceanConservationButtons();
                break;
            case AppTheme.MentalHealthRelaxation:
                CreateRelaxationEnvironment();
                CreateRelaxationButtons();
                break;
            case AppTheme.EducationalLearning:
                CreateEducationalEnvironment();
                CreateEducationalButtons();
                break;
            case AppTheme.FitnessWellness:
                CreateFitnessEnvironment();
                CreateFitnessButtons();
                break;
            case AppTheme.ProductivityFocus:
                CreateProductivityEnvironment();
                CreateProductivityButtons();
                break;
            case AppTheme.SocialConnection:
                CreateSocialEnvironment();
                CreateSocialButtons();
                break;
        }
    }
    
    // ----- OCEAN CONSERVATION ENVIRONMENT (Social Cause - Voxel Style) -----
    
    private List<GameObject> trashObjects = new List<GameObject>();
    
    void CreateOceanConservationEnvironment()
    {
        // 1. REMOVE FOG (Clear view)
        RenderSettings.fog = false;
        mainCamera.backgroundColor = new Color(0.2f, 0.6f, 0.9f); // Clear blue water
        ambientLightColor = new Color(0.6f, 0.8f, 1f);
        RenderSettings.ambientLight = ambientLightColor;
        
        // Find Directional Light and adjust
        Light mainLight = GameObject.FindObjectOfType<Light>();
        if(mainLight != null) 
        {
            mainLight.color = Color.white;
            mainLight.intensity = 1f;
            mainLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // 2. VOXEL GROUND (Many bricks)
        CreateVoxelGround();
        
        // 3. VOXEL REEFS
        CreateVoxelReef(new Vector3(0, 0, 8));
        CreateVoxelReef(new Vector3(-8, 0, 10));
        CreateVoxelReef(new Vector3(8, 0, 12));
        
        // 4. VOXEL TRASH
        for(int i = 0; i < 10; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-8f, 8f), 1f, Random.Range(4f, 12f));
            CreateVoxelTrash(pos);
        }
        
        // 5. VOXEL FISH (School)
        for (int i = 0; i < 15; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-15f, 15f), Random.Range(2f, 8f), Random.Range(0f, 20f));
            CreateVoxelFish(pos);
        }
    }
    
    void CreateVoxelGround()
    {
        // Create a floor made of many individual cubes (bricks)
        int width = 20;
        int depth = 20;
        float blockSize = 2f;
        float startX = -(width * blockSize) / 2f;
        float startZ = -(depth * blockSize) / 2f; // Start closer to camera
        
        GameObject floorRoot = new GameObject("VoxelFloor");
        floorRoot.transform.SetParent(environmentRoot.transform);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector3 pos = new Vector3(startX + x * blockSize, -1f, startZ + z * blockSize);
                
                // Add slight height variation for "Real" feel
                float heightNoise = Mathf.PerlinNoise(x * 0.1f, z * 0.1f) * 1f;
                Vector3 finalPos = pos + Vector3.up * heightNoise;
                
                GameObject brick = GameObject.CreatePrimitive(PrimitiveType.Cube);
                brick.transform.SetParent(floorRoot.transform);
                brick.transform.position = finalPos;
                brick.transform.localScale = new Vector3(blockSize, blockSize, blockSize);
                
                // Voxel sand colors (checkered slightly)
                Color sand1 = new Color(0.9f, 0.8f, 0.6f);
                Color sand2 = new Color(0.85f, 0.75f, 0.55f);
                Color brickColor = ((x + z) % 2 == 0) ? sand1 : sand2;
                
                brick.GetComponent<Renderer>().material = CreateMaterial(brickColor);
            }
        }
    }

    void CreateVoxelReef(Vector3 centerPos)
    {
        // Base of the reef (pyramid of cubes)
        for (int y = 0; y < 3; y++)
        {
            for (int x = -2 + y; x <= 2 - y; x++)
            {
                for (int z = -2 + y; z <= 2 - y; z++)
                {
                    Vector3 pos = centerPos + new Vector3(x, y, z);
                    CreateVoxelBlock(pos, new Color(0.5f, 0.5f, 0.5f)); // Grey dead rock base
                }
            }
        }
        
        // Dead coral structures (stacks of cubes)
        for(int i=0; i<4; i++)
        {
            Vector3 stemPos = centerPos + new Vector3(Random.Range(-1, 2), 3, Random.Range(-1, 2));
            int height = Random.Range(2, 5);
            
            GameObject coralGroup = new GameObject("DeadCoralGroup");
            coralGroup.transform.SetParent(environmentRoot.transform);
            
            for(int h=0; h<height; h++)
            {
                GameObject voxel = CreateVoxelBlock(stemPos + Vector3.up * h * 0.5f, new Color(0.8f, 0.8f, 0.8f));
                voxel.transform.localScale = Vector3.one * 0.5f; // Smaller blocks for coral
                voxel.name = "DeadCoralBlock"; // Tag for restoration
                voxel.transform.SetParent(coralGroup.transform);
            }
        }
    }

    void CreateVoxelTrash(Vector3 pos)
    {
        GameObject trashGroup = new GameObject("VoxelTrash");
        trashGroup.transform.SetParent(environmentRoot.transform);
        trashGroup.transform.position = pos;
        
        // Make a bottle or can shape out of small cubes
        Color trashColor = Random.value > 0.5f ? Color.red : Color.black;
        
        // Body
        CreateVoxelBlockLocal(new Vector3(0, 0, 0), Vector3.one * 0.4f, trashColor, trashGroup.transform);
        CreateVoxelBlockLocal(new Vector3(0, 0.4f, 0), Vector3.one * 0.4f, trashColor, trashGroup.transform);
        // Neck
        CreateVoxelBlockLocal(new Vector3(0, 0.7f, 0), Vector3.one * 0.2f, Color.white, trashGroup.transform);
        
        // Random rotation to look discarded
        trashGroup.transform.rotation = Quaternion.Euler(Random.Range(0, 90), Random.Range(0, 360), Random.Range(0, 90));
        
        trashObjects.Add(trashGroup);
    }
    
    void CreateVoxelFish(Vector3 pos)
    {
        GameObject fishGroup = new GameObject("VoxelFish");
        fishGroup.transform.SetParent(environmentRoot.transform);
        fishGroup.transform.position = pos;
        
        Color fishColor = new Color(1f, 0.6f, 0f); // Goldfish
        
        // Body
        CreateVoxelBlockLocal(Vector3.zero, new Vector3(0.8f, 0.4f, 0.2f), fishColor, fishGroup.transform);
        // Tail
        CreateVoxelBlockLocal(new Vector3(-0.5f, 0, 0), new Vector3(0.3f, 0.3f, 0.1f), fishColor, fishGroup.transform);
        // Eye
        CreateVoxelBlockLocal(new Vector3(0.3f, 0.1f, 0.11f), new Vector3(0.1f, 0.1f, 0.1f), Color.black, fishGroup.transform);
        
        fishGroup.AddComponent<FloatingAnimation>();
    }

    // Helper for global positioning
    GameObject CreateVoxelBlock(Vector3 pos, Color color)
    {
        GameObject voxel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        voxel.transform.SetParent(environmentRoot.transform);
        voxel.transform.position = pos;
        voxel.GetComponent<Renderer>().material = CreateMaterial(color);
        return voxel;
    }

    // Helper for local positioning (parts of an object)
    GameObject CreateVoxelBlockLocal(Vector3 localPos, Vector3 scale, Color color, Transform parent)
    {
        GameObject voxel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        voxel.transform.SetParent(parent);
        voxel.transform.localPosition = localPos;
        voxel.transform.localScale = scale;
        voxel.GetComponent<Renderer>().material = CreateMaterial(color);
        return voxel;
    }
    
    void CreateOceanConservationButtons()
    {
        // Control Panel Style Interaction
        Create3DButton(new Vector3(-3, 1.5f, 5), "Clean Debris", new Color(0.9f, 0.3f, 0.3f), OnCleanDebrisButton);
        Create3DButton(new Vector3(0, 1.5f, 6), "Restore Reef", new Color(0.3f, 0.9f, 0.5f), OnRestoreReefButton);
        Create3DButton(new Vector3(3, 1.5f, 5), "Deploy Life", new Color(0.3f, 0.6f, 0.9f), OnDeployLifeButton);
        Create3DButton(new Vector3(0, 1.5f, 8), "Ocean Facts", new Color(0.9f, 0.8f, 0.2f), OnOceanFactsButton);
    }

    // ----- OCEAN CALLBACKS -----

    void OnCleanDebrisButton()
    {
        if(trashObjects.Count > 0)
        {
            ShowMessage("VOXEL DRONE ACTIVATED... DELETING BLOCKS.");
            foreach(var obj in trashObjects)
            {
                if(obj != null)
                {
                    StartCoroutine(AnimateRemoval(obj));
                }
            }
            trashObjects.Clear();
        }
        else
        {
            ShowMessage("OCEAN FLOOR IS ALREADY CLEAN!");
        }
    }

    IEnumerator AnimateRemoval(GameObject obj)
    {
        float timer = 0;
        Vector3 startPos = obj.transform.position;
        Vector3 endPos = startPos + Vector3.up * 5f;
        
        while(timer < 1f)
        {
            timer += Time.deltaTime;
            if(obj != null)
            {
                obj.transform.position = Vector3.Lerp(startPos, endPos, timer);
                obj.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, timer);
            }
            yield return null;
        }
        if(obj != null) Destroy(obj);
    }

    void OnRestoreReefButton()
    {
        if(trashObjects.Count > 0)
        {
            ShowMessage("WARNING: REMOVE DEBRIS BEFORE RESTORING REEF!");
            return;
        }

        ShowMessage("REGENERATING VOXEL CORAL...");
        
        // Find all DeadCoralBlocks and color them
        Renderer[] renderers = environmentRoot.GetComponentsInChildren<Renderer>();
        foreach(var r in renderers)
        {
            if(r.gameObject.name == "DeadCoralBlock")
            {
                Color healthyColor = Random.value > 0.5f ? new Color(1f, 0.4f, 0.6f) : new Color(0.4f, 1f, 0.6f);
                r.material.color = healthyColor;
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", healthyColor * 0.5f);
            }
        }
    }

    void OnDeployLifeButton()
    {
        ShowMessage("SPAWNING VOXEL FISH...");
        for(int i=0; i<10; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-10f, 10f), Random.Range(2f, 6f), Random.Range(5f, 15f));
            CreateVoxelFish(pos);
        }
    }

    void OnOceanFactsButton()
    {
        string[] facts = {
            "Did you know?\nOver 8 million tons of plastic enter our oceans every year.",
            "Fact:\nBy 2050, there could be more plastic in the ocean than fish (by weight).",
            "Alert:\nCoral reefs support 25% of all marine life but are dying due to pollution."
        };
        ShowMessage(facts[Random.Range(0, facts.Length)]);
    }

    // ----- RELAXATION ENVIRONMENT -----
    
    void CreateRelaxationEnvironment()
    {
        // Ground - grass
        CreateGround(new Color(0.3f, 0.5f, 0.3f), 100f);
        
        // Water feature
        CreateWater(new Vector3(5, 0.01f, 8), 6f);
        
        // Trees
        CreateTree(new Vector3(-8, 0, 10), 4f);
        CreateTree(new Vector3(-12, 0, 15), 5f);
        CreateTree(new Vector3(12, 0, 12), 3.5f);
        CreateTree(new Vector3(-5, 0, 20), 4.5f);
        CreateTree(new Vector3(8, 0, 18), 4f);
        
        // Rocks
        CreateRock(new Vector3(-3, 0, 5), 0.8f);
        CreateRock(new Vector3(2, 0, 4), 0.5f);
        CreateRock(new Vector3(6, 0, 6), 0.6f);
        
        // Floating orbs (calming particles)
        for (int i = 0; i < 10; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-15f, 15f),
                Random.Range(1f, 4f),
                Random.Range(5f, 25f)
            );
            CreateFloatingOrb(pos, new Color(0.8f, 0.9f, 1f, 0.4f));
        }
        
        // Meditation platform
        CreatePlatform(new Vector3(0, 0.1f, 3), 2f, new Color(0.6f, 0.5f, 0.4f));
    }
    
    void CreateRelaxationButtons()
    {
        Create3DButton(new Vector3(-3, 1.5f, 6), "Breathe", new Color(0.4f, 0.7f, 0.9f), OnBreatheButton);
        Create3DButton(new Vector3(0, 1.5f, 7), "Meditate", new Color(0.6f, 0.5f, 0.8f), OnMeditateButton);
        Create3DButton(new Vector3(3, 1.5f, 6), "Sounds", new Color(0.5f, 0.8f, 0.5f), OnSoundsButton);
        Create3DButton(new Vector3(-2, 1.5f, 8), "Affirmations", new Color(0.9f, 0.7f, 0.5f), OnAffirmationsButton);
        Create3DButton(new Vector3(2, 1.5f, 8), "Body Scan", new Color(0.8f, 0.6f, 0.7f), OnBodyScanButton);
    }
    
    // ----- EDUCATIONAL ENVIRONMENT -----
    
    void CreateEducationalEnvironment()
    {
        // Floor - classroom style
        CreateGround(new Color(0.6f, 0.55f, 0.5f), 50f);
        
        // Walls
        CreateWall(new Vector3(0, 2.5f, 15), new Vector3(30, 5, 0.3f), new Color(0.9f, 0.9f, 0.85f));
        CreateWall(new Vector3(-15, 2.5f, 7.5f), new Vector3(0.3f, 5, 15), new Color(0.85f, 0.85f, 0.8f));
        CreateWall(new Vector3(15, 2.5f, 7.5f), new Vector3(0.3f, 5, 15), new Color(0.85f, 0.85f, 0.8f));
        
        // Whiteboard
        CreateWhiteboard(new Vector3(0, 2f, 14.7f), 8f, 3f);
        
        // Desks
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                CreateDesk(new Vector3(-4.5f + col * 3f, 0, 4 + row * 3f));
            }
        }
        
        // Globe
        CreateGlobe(new Vector3(-10, 1.2f, 12));
        
        // Bookshelf
        CreateBookshelf(new Vector3(12, 0, 10));
    }
    
    void CreateEducationalButtons()
    {
        Create3DButton(new Vector3(-5, 1.5f, 3), "Start Lesson", new Color(0.3f, 0.6f, 0.9f), OnStartLessonButton);
        Create3DButton(new Vector3(-2, 1.5f, 3), "Quiz", new Color(0.9f, 0.6f, 0.3f), OnQuizButton);
        Create3DButton(new Vector3(1, 1.5f, 3), "Resources", new Color(0.5f, 0.8f, 0.5f), OnResourcesButton);
        Create3DButton(new Vector3(4, 1.5f, 3), "Progress", new Color(0.7f, 0.5f, 0.8f), OnProgressButton);
    }
    
    // ----- FITNESS ENVIRONMENT -----
    
    void CreateFitnessEnvironment()
    {
        // Floor - gym mat style
        CreateGround(new Color(0.2f, 0.2f, 0.25f), 50f);
        
        // Gym equipment silhouettes
        CreateGymEquipment(new Vector3(-8, 0, 10), "treadmill");
        CreateGymEquipment(new Vector3(-4, 0, 10), "weights");
        CreateGymEquipment(new Vector3(4, 0, 10), "bike");
        CreateGymEquipment(new Vector3(8, 0, 10), "mat");
        
        // Mirror wall
        CreateMirrorWall(new Vector3(0, 2f, 15), 20f, 4f);
        
        // Exercise area markers
        for (int i = 0; i < 4; i++)
        {
            CreateExerciseMarker(new Vector3(-3 + i * 2, 0.01f, 5), new Color(0.8f, 0.3f, 0.3f));
        }
        
        // Motivational banners
        CreateBanner(new Vector3(-8, 3.5f, 14), "PUSH YOUR LIMITS", new Color(0.9f, 0.3f, 0.2f));
        CreateBanner(new Vector3(8, 3.5f, 14), "STAY STRONG", new Color(0.2f, 0.7f, 0.3f));
    }
    
    void CreateFitnessButtons()
    {
        Create3DButton(new Vector3(-4, 1.5f, 4), "Warm Up", new Color(0.9f, 0.6f, 0.2f), OnWarmUpButton);
        Create3DButton(new Vector3(-1.5f, 1.5f, 4), "Cardio", new Color(0.9f, 0.3f, 0.3f), OnCardioButton);
        Create3DButton(new Vector3(1.5f, 1.5f, 4), "Strength", new Color(0.3f, 0.6f, 0.9f), OnStrengthButton);
        Create3DButton(new Vector3(4, 1.5f, 4), "Cool Down", new Color(0.5f, 0.8f, 0.9f), OnCoolDownButton);
    }
    
    // ----- PRODUCTIVITY ENVIRONMENT -----
    
    void CreateProductivityEnvironment()
    {
        // Floor - office carpet
        CreateGround(new Color(0.3f, 0.35f, 0.4f), 50f);
        
        // Office desk
        CreateOfficeDesk(new Vector3(0, 0, 4));
        
        // Computer monitor
        CreateMonitor(new Vector3(0, 1.1f, 4.5f));
        
        // Office chair
        CreateOfficeChair(new Vector3(0, 0, 2));
        
        // Window with view
        CreateWindow(new Vector3(8, 2f, 8), 4f, 3f);
        
        // Plants
        CreateOfficePlant(new Vector3(-5, 0, 6));
        CreateOfficePlant(new Vector3(6, 0, 3));
        
        // Whiteboard
        CreateWhiteboard(new Vector3(-7, 2f, 8), 4f, 2.5f);
        
        // Clock
        CreateClock(new Vector3(0, 3f, 9));
    }
    
    void CreateProductivityButtons()
    {
        Create3DButton(new Vector3(-3, 1.5f, 6), "Focus Timer", new Color(0.3f, 0.7f, 0.4f), OnFocusTimerButton);
        Create3DButton(new Vector3(0, 1.5f, 6), "Task List", new Color(0.4f, 0.5f, 0.9f), OnTaskListButton);
        Create3DButton(new Vector3(3, 1.5f, 6), "Break Time", new Color(0.9f, 0.6f, 0.3f), OnBreakTimeButton);
        Create3DButton(new Vector3(0, 1.5f, 8), "Notes", new Color(0.8f, 0.8f, 0.3f), OnNotesButton);
    }
    
    // ----- SOCIAL ENVIRONMENT -----
    
    void CreateSocialEnvironment()
    {
        // Floor - community center
        CreateGround(new Color(0.5f, 0.45f, 0.4f), 50f);
        
        // Seating area - circular arrangement
        float radius = 6f;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8f;
            Vector3 pos = new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius + 8);
            CreateSeat(pos, -angle * Mathf.Rad2Deg);
        }
        
        // Central table
        CreateTable(new Vector3(0, 0, 8), 2f);
        
        // Decorative elements
        CreatePlanter(new Vector3(-8, 0, 5));
        CreatePlanter(new Vector3(8, 0, 5));
        
        // Ambient lighting fixtures
        CreateLightFixture(new Vector3(0, 4f, 8));
        CreateLightFixture(new Vector3(-5, 4f, 10));
        CreateLightFixture(new Vector3(5, 4f, 10));
    }
    
    void CreateSocialButtons()
    {
        Create3DButton(new Vector3(-3, 1.5f, 4), "Join Chat", new Color(0.3f, 0.7f, 0.9f), OnJoinChatButton);
        Create3DButton(new Vector3(0, 1.5f, 4), "Find Friends", new Color(0.9f, 0.5f, 0.7f), OnFindFriendsButton);
        Create3DButton(new Vector3(3, 1.5f, 4), "Events", new Color(0.5f, 0.8f, 0.4f), OnEventsButton);
        Create3DButton(new Vector3(0, 1.5f, 6), "My Profile", new Color(0.9f, 0.7f, 0.3f), OnMyProfileButton);
    }
    
    // ============================================
    // OBJECT CREATION HELPERS
    // ============================================
    
    void CreateGround(Color color, float size)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(environmentRoot.transform);
        ground.transform.localScale = new Vector3(size / 10f, 1, size / 10f);
        ground.GetComponent<Renderer>().material = CreateMaterial(color);
    }
    
    void CreateWater(Vector3 position, float size)
    {
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        water.name = "Water";
        water.transform.SetParent(environmentRoot.transform);
        water.transform.position = position;
        water.transform.localScale = new Vector3(size, 0.1f, size);
        
        Material waterMat = CreateMaterial(new Color(0.3f, 0.5f, 0.7f, 0.7f));
        waterMat.SetFloat("_Mode", 3); // Transparent
        waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        waterMat.renderQueue = 3000;
        water.GetComponent<Renderer>().material = waterMat;
        
        // Remove collider
        Destroy(water.GetComponent<Collider>());
    }
    
    void CreateTree(Vector3 position, float height)
    {
        GameObject tree = new GameObject("Tree");
        tree.transform.SetParent(environmentRoot.transform);
        tree.transform.position = position;
        
        // Trunk
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(tree.transform);
        trunk.transform.localPosition = new Vector3(0, height * 0.3f, 0);
        trunk.transform.localScale = new Vector3(height * 0.15f, height * 0.3f, height * 0.15f);
        trunk.GetComponent<Renderer>().material = CreateMaterial(new Color(0.4f, 0.3f, 0.2f));
        Destroy(trunk.GetComponent<Collider>());
        
        // Foliage
        GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foliage.transform.SetParent(tree.transform);
        foliage.transform.localPosition = new Vector3(0, height * 0.7f, 0);
        foliage.transform.localScale = new Vector3(height * 0.6f, height * 0.5f, height * 0.6f);
        foliage.GetComponent<Renderer>().material = CreateMaterial(new Color(0.2f, 0.5f, 0.2f));
        Destroy(foliage.GetComponent<Collider>());
    }
    
    void CreateRock(Vector3 position, float scale)
    {
        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = "Rock";
        rock.transform.SetParent(environmentRoot.transform);
        rock.transform.position = position + Vector3.up * scale * 0.3f;
        rock.transform.localScale = new Vector3(scale, scale * 0.6f, scale * 0.8f);
        rock.transform.rotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
        rock.GetComponent<Renderer>().material = CreateMaterial(new Color(0.5f, 0.5f, 0.5f));
        Destroy(rock.GetComponent<Collider>());
    }
    
    void CreateFloatingOrb(Vector3 position, Color color)
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "FloatingOrb";
        orb.transform.SetParent(environmentRoot.transform);
        orb.transform.position = position;
        orb.transform.localScale = Vector3.one * Random.Range(0.1f, 0.3f);
        
        Material orbMat = CreateMaterial(color);
        orbMat.SetFloat("_Mode", 3);
        orbMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        orbMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        orbMat.renderQueue = 3000;
        orb.GetComponent<Renderer>().material = orbMat;
        
        Destroy(orb.GetComponent<Collider>());
        
        // Add floating animation
        orb.AddComponent<FloatingAnimation>();
    }
    
    void CreatePlatform(Vector3 position, float radius, Color color)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platform.name = "Platform";
        platform.transform.SetParent(environmentRoot.transform);
        platform.transform.position = position;
        platform.transform.localScale = new Vector3(radius * 2, 0.1f, radius * 2);
        platform.GetComponent<Renderer>().material = CreateMaterial(color);
    }
    
    void CreateWall(Vector3 position, Vector3 scale, Color color)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.SetParent(environmentRoot.transform);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material = CreateMaterial(color);
    }
    
    void CreateWhiteboard(Vector3 position, float width, float height)
    {
        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Whiteboard";
        board.transform.SetParent(environmentRoot.transform);
        board.transform.position = position;
        board.transform.localScale = new Vector3(width, height, 0.1f);
        board.GetComponent<Renderer>().material = CreateMaterial(new Color(0.95f, 0.95f, 0.95f));
        
        // Frame
        CreateFrame(position, width, height);
    }
    
    void CreateFrame(Vector3 position, float width, float height)
    {
        Color frameColor = new Color(0.3f, 0.25f, 0.2f);
        float frameWidth = 0.1f;
        
        // Top
        CreateFramePart(position + new Vector3(0, height/2, 0.06f), new Vector3(width + frameWidth*2, frameWidth, frameWidth), frameColor);
        // Bottom
        CreateFramePart(position + new Vector3(0, -height/2, 0.06f), new Vector3(width + frameWidth*2, frameWidth, frameWidth), frameColor);
        // Left
        CreateFramePart(position + new Vector3(-width/2, 0, 0.06f), new Vector3(frameWidth, height, frameWidth), frameColor);
        // Right
        CreateFramePart(position + new Vector3(width/2, 0, 0.06f), new Vector3(frameWidth, height, frameWidth), frameColor);
    }
    
    void CreateFramePart(Vector3 position, Vector3 scale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = "FramePart";
        part.transform.SetParent(environmentRoot.transform);
        part.transform.position = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().material = CreateMaterial(color);
        Destroy(part.GetComponent<Collider>());
    }
    
    void CreateDesk(Vector3 position)
    {
        GameObject desk = new GameObject("Desk");
        desk.transform.SetParent(environmentRoot.transform);
        desk.transform.position = position;
        
        // Tabletop
        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.transform.SetParent(desk.transform);
        top.transform.localPosition = new Vector3(0, 0.75f, 0);
        top.transform.localScale = new Vector3(1.2f, 0.05f, 0.6f);
        top.GetComponent<Renderer>().material = CreateMaterial(new Color(0.6f, 0.5f, 0.4f));
        
        // Legs
        for (int i = 0; i < 4; i++)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.transform.SetParent(desk.transform);
            float x = (i % 2 == 0) ? -0.5f : 0.5f;
            float z = (i < 2) ? -0.25f : 0.25f;
            leg.transform.localPosition = new Vector3(x, 0.375f, z);
            leg.transform.localScale = new Vector3(0.05f, 0.75f, 0.05f);
            leg.GetComponent<Renderer>().material = CreateMaterial(new Color(0.3f, 0.3f, 0.3f));
            Destroy(leg.GetComponent<Collider>());
        }
    }
    
    void CreateGlobe(Vector3 position)
    {
        GameObject globe = new GameObject("Globe");
        globe.transform.SetParent(environmentRoot.transform);
        globe.transform.position = position;
        
        // Stand
        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stand.transform.SetParent(globe.transform);
        stand.transform.localPosition = new Vector3(0, 0.3f, 0);
        stand.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        stand.GetComponent<Renderer>().material = CreateMaterial(new Color(0.4f, 0.3f, 0.2f));
        Destroy(stand.GetComponent<Collider>());
        
        // Globe sphere
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(globe.transform);
        sphere.transform.localPosition = new Vector3(0, 0.8f, 0);
        sphere.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        sphere.GetComponent<Renderer>().material = CreateMaterial(new Color(0.3f, 0.5f, 0.8f));
        Destroy(sphere.GetComponent<Collider>());
    }
    
    void CreateBookshelf(Vector3 position)
    {
        GameObject shelf = new GameObject("Bookshelf");
        shelf.transform.SetParent(environmentRoot.transform);
        shelf.transform.position = position;
        
        // Main frame
        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.transform.SetParent(shelf.transform);
        frame.transform.localPosition = new Vector3(0, 1.5f, 0);
        frame.transform.localScale = new Vector3(2f, 3f, 0.4f);
        frame.GetComponent<Renderer>().material = CreateMaterial(new Color(0.5f, 0.35f, 0.2f));
        
        // Books (colored rectangles)
        Color[] bookColors = { Color.red, Color.blue, Color.green, new Color(0.8f, 0.6f, 0.2f), Color.magenta };
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 6; col++)
            {
                GameObject book = GameObject.CreatePrimitive(PrimitiveType.Cube);
                book.transform.SetParent(shelf.transform);
                book.transform.localPosition = new Vector3(-0.7f + col * 0.28f, 0.5f + row * 0.75f, 0.1f);
                book.transform.localScale = new Vector3(0.2f, 0.5f, 0.25f);
                book.GetComponent<Renderer>().material = CreateMaterial(bookColors[Random.Range(0, bookColors.Length)]);
                Destroy(book.GetComponent<Collider>());
            }
        }
    }
    
    void CreateGymEquipment(Vector3 position, string type)
    {
        GameObject equipment = new GameObject("Equipment_" + type);
        equipment.transform.SetParent(environmentRoot.transform);
        equipment.transform.position = position;
        
        switch (type)
        {
            case "treadmill":
                CreateTreadmill(equipment);
                break;
            case "weights":
                CreateWeightRack(equipment);
                break;
            case "bike":
                CreateExerciseBike(equipment);
                break;
            case "mat":
                CreateExerciseMat(equipment);
                break;
        }
    }
    
    void CreateTreadmill(GameObject parent)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(parent.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0);
        body.transform.localScale = new Vector3(0.8f, 0.2f, 2f);
        body.GetComponent<Renderer>().material = CreateMaterial(new Color(0.2f, 0.2f, 0.2f));
        Destroy(body.GetComponent<Collider>());
        
        GameObject console = GameObject.CreatePrimitive(PrimitiveType.Cube);
        console.transform.SetParent(parent.transform);
        console.transform.localPosition = new Vector3(0, 1.2f, -0.8f);
        console.transform.localScale = new Vector3(0.6f, 0.4f, 0.1f);
        console.GetComponent<Renderer>().material = CreateMaterial(new Color(0.3f, 0.3f, 0.3f));
        Destroy(console.GetComponent<Collider>());
    }
    
    void CreateWeightRack(GameObject parent)
    {
        GameObject rack = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rack.transform.SetParent(parent.transform);
        rack.transform.localPosition = new Vector3(0, 1f, 0);
        rack.transform.localScale = new Vector3(1.5f, 2f, 0.5f);
        rack.GetComponent<Renderer>().material = CreateMaterial(new Color(0.3f, 0.3f, 0.3f));
        
        // Weights
        for (int i = 0; i < 4; i++)
        {
            GameObject weight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            weight.transform.SetParent(parent.transform);
            weight.transform.localPosition = new Vector3(-0.4f + i * 0.3f, 0.8f, 0.3f);
            weight.transform.localScale = new Vector3(0.2f, 0.1f, 0.2f);
            weight.GetComponent<Renderer>().material = CreateMaterial(new Color(0.1f, 0.1f, 0.1f));
            Destroy(weight.GetComponent<Collider>());
        }
    }
    
    void CreateExerciseBike(GameObject parent)
    {
        GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seat.transform.SetParent(parent.transform);
        seat.transform.localPosition = new Vector3(0, 1f, 0);
        seat.transform.localScale = new Vector3(0.3f, 0.1f, 0.4f);
        seat.GetComponent<Renderer>().material = CreateMaterial(new Color(0.2f, 0.2f, 0.2f));
        Destroy(seat.GetComponent<Collider>());
        
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(parent.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0.3f);
        body.transform.localScale = new Vector3(0.3f, 1f, 0.5f);
        body.GetComponent<Renderer>().material = CreateMaterial(new Color(0.8f, 0.2f, 0.2f));
        Destroy(body.GetComponent<Collider>());
        
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.transform.SetParent(parent.transform);
        handle.transform.localPosition = new Vector3(0, 1.2f, 0.5f);
        handle.transform.localScale = new Vector3(0.5f, 0.1f, 0.1f);
        handle.GetComponent<Renderer>().material = CreateMaterial(new Color(0.2f, 0.2f, 0.2f));
        Destroy(handle.GetComponent<Collider>());
    }
    
    void CreateExerciseMat(GameObject parent)
    {
        GameObject mat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mat.transform.SetParent(parent.transform);
        mat.transform.localPosition = new Vector3(0, 0.02f, 0);
        mat.transform.localScale = new Vector3(0.8f, 0.04f, 2f);
        mat.GetComponent<Renderer>().material = CreateMaterial(new Color(0.3f, 0.5f, 0.8f));
    }
    
    void CreateMirrorWall(Vector3 position, float width, float height)
    {
        GameObject mirror = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mirror.name = "MirrorWall";
        mirror.transform.SetParent(environmentRoot.transform);
        mirror.transform.position = position;
        mirror.transform.localScale = new Vector3(width, height, 0.1f);
        
        Material mirrorMat = CreateMaterial(new Color(0.7f, 0.75f, 0.8f));
        mirrorMat.SetFloat("_Metallic", 0.9f);
        mirrorMat.SetFloat("_Glossiness", 0.95f);
        mirror.GetComponent<Renderer>().material = mirrorMat;
    }
    
    void CreateExerciseMarker(Vector3 position, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "ExerciseMarker";
        marker.transform.SetParent(environmentRoot.transform);
        marker.transform.position = position;
        marker.transform.localScale = new Vector3(1.5f, 0.01f, 1.5f);
        marker.GetComponent<Renderer>().material = CreateMaterial(color);
        Destroy(marker.GetComponent<Collider>());
    }
    
    void CreateBanner(Vector3 position, string text, Color color)
    {
        GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
        banner.name = "Banner";
        banner.transform.SetParent(environmentRoot.transform);
        banner.transform.position = position;
        banner.transform.localScale = new Vector3(6f, 1f, 0.1f);
        banner.GetComponent<Renderer>().material = CreateMaterial(color);
        Destroy(banner.GetComponent<Collider>());
    }
    
    void CreateOfficeDesk(Vector3 position)
    {
        GameObject desk = new GameObject("OfficeDesk");
        desk.transform.SetParent(environmentRoot.transform);
        desk.transform.position = position;
        
        // L-shaped desk
        GameObject main = GameObject.CreatePrimitive(PrimitiveType.Cube);
        main.transform.SetParent(desk.transform);
        main.transform.localPosition = new Vector3(0, 0.75f, 0);
        main.transform.localScale = new Vector3(2f, 0.05f, 1f);
        main.GetComponent<Renderer>().material = CreateMaterial(new Color(0.5f, 0.4f, 0.3f));
        
        // Side extension
        GameObject side = GameObject.CreatePrimitive(PrimitiveType.Cube);
        side.transform.SetParent(desk.transform);
        side.transform.localPosition = new Vector3(1.25f, 0.75f, 0.75f);
        side.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
        side.GetComponent<Renderer>().material = CreateMaterial(new Color(0.5f, 0.4f, 0.3f));
        
        // Drawers
        GameObject drawer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        drawer.transform.SetParent(desk.transform);
        drawer.transform.localPosition = new Vector3(-0.7f, 0.4f, 0);
        drawer.transform.localScale = new Vector3(0.5f, 0.6f, 0.9f);
        drawer.GetComponent<Renderer>().material = CreateMaterial(new Color(0.45f, 0.35f, 0.25f));
    }
    
    void CreateMonitor(Vector3 position)
    {
        GameObject monitor = new GameObject("Monitor");
        monitor.transform.SetParent(environmentRoot.transform);
        monitor.transform.position = position;
        
        // Screen
        GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        screen.transform.SetParent(monitor.transform);
        screen.transform.localPosition = Vector3.zero;
        screen.transform.localScale = new Vector3(0.8f, 0.5f, 0.03f);
        screen.GetComponent<Renderer>().material = CreateMaterial(new Color(0.1f, 0.1f, 0.15f));
        
        // Stand
        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stand.transform.SetParent(monitor.transform);
        stand.transform.localPosition = new Vector3(0, -0.35f, 0);
        stand.transform.localScale = new Vector3(0.1f, 0.2f, 0.15f);
        stand.GetComponent<Renderer>().material = CreateMaterial(new Color(0.3f, 0.3f, 0.3f));
        Destroy(stand.GetComponent<Collider>());
        
        // Base
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.transform.SetParent(monitor.transform);
        baseObj.transform.localPosition = new Vector3(0, -0.45f, 0);
        baseObj.transform.localScale = new Vector3(0.4f, 0.02f, 0.25f);
        baseObj.GetComponent<Renderer>().material = CreateMaterial(new Color(0.3f, 0.3f, 0.3f));
        Destroy(baseObj.GetComponent<Collider>());
    }
    
    void CreateOfficeChair(Vector3 position)
    {
        GameObject chair = new GameObject("OfficeChair");
        chair.transform.SetParent(environmentRoot.transform);
        chair.transform.position = position;
        
        // Seat
        GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seat.transform.SetParent(chair.transform);
        seat.transform.localPosition = new Vector3(0, 0.5f, 0);
        seat.transform.localScale = new Vector3(0.5f, 0.08f, 0.5f);
        seat.GetComponent<Renderer>().material = CreateMaterial(new Color(0.2f, 0.2f, 0.2f));
        
        // Back
        GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.transform.SetParent(chair.transform);
        back.transform.localPosition = new Vector3(0, 0.9f, 0.2f);
        back.transform.localScale = new Vector3(0.5f, 0.7f, 0.08f);
        back.GetComponent<Renderer>().material = CreateMaterial(new Color(0.2f, 0.2f, 0.2f));
        Destroy(back.GetComponent<Collider>());
        
        // Base
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObj.transform.SetParent(chair.transform);
        baseObj.transform.localPosition = new Vector3(0, 0.25f, 0);
        baseObj.transform.localScale = new Vector3(0.1f, 0.25f, 0.1f);
        baseObj.GetComponent<Renderer>().material = CreateMaterial(new Color(0.3f, 0.3f, 0.3f));
        Destroy(baseObj.GetComponent<Collider>());
    }
    
    void CreateWindow(Vector3 position, float width, float height)
    {
        GameObject window = new GameObject("Window");
        window.transform.SetParent(environmentRoot.transform);
        window.transform.position = position;
        
        // Frame
        CreateFrame(position, width, height);
        
        // Glass
        GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glass.transform.SetParent(window.transform);
        glass.transform.localPosition = Vector3.zero;
        glass.transform.localScale = new Vector3(width, height, 0.05f);
        
        Material glassMat = CreateMaterial(new Color(0.7f, 0.85f, 0.95f, 0.5f));
        glassMat.SetFloat("_Mode", 3);
        glassMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glassMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        glassMat.renderQueue = 3000;
        glass.GetComponent<Renderer>().material = glassMat;
        Destroy(glass.GetComponent<Collider>());
    }
    
    void CreateOfficePlant(Vector3 position)
    {
        GameObject plant = new GameObject("OfficePlant");
        plant.transform.SetParent(environmentRoot.transform);
        plant.transform.position = position;
        
        // Pot
        GameObject pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pot.transform.SetParent(plant.transform);
        pot.transform.localPosition = new Vector3(0, 0.2f, 0);
        pot.transform.localScale = new Vector3(0.4f, 0.2f, 0.4f);
        pot.GetComponent<Renderer>().material = CreateMaterial(new Color(0.6f, 0.4f, 0.3f));
        Destroy(pot.GetComponent<Collider>());
        
        // Foliage
        GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foliage.transform.SetParent(plant.transform);
        foliage.transform.localPosition = new Vector3(0, 0.6f, 0);
        foliage.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
        foliage.GetComponent<Renderer>().material = CreateMaterial(new Color(0.2f, 0.5f, 0.2f));
        Destroy(foliage.GetComponent<Collider>());
    }
    
    void CreateClock(Vector3 position)
    {
        GameObject clock = new GameObject("Clock");
        clock.transform.SetParent(environmentRoot.transform);
        clock.transform.position = position;
        
        // Face
        GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        face.transform.SetParent(clock.transform);
        face.transform.localRotation = Quaternion.Euler(90, 0, 0);
        face.transform.localScale = new Vector3(0.6f, 0.05f, 0.6f);
        face.GetComponent<Renderer>().material = CreateMaterial(new Color(0.95f, 0.95f, 0.95f));
        Destroy(face.GetComponent<Collider>());
        
        // Frame
        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        frame.transform.SetParent(clock.transform);
        frame.transform.localRotation = Quaternion.Euler(90, 0, 0);
        frame.transform.localPosition = new Vector3(0, 0, 0.03f);
        frame.transform.localScale = new Vector3(0.7f, 0.02f, 0.7f);
        frame.GetComponent<Renderer>().material = CreateMaterial(new Color(0.2f, 0.2f, 0.2f));
        Destroy(frame.GetComponent<Collider>());
    }
    
    void CreateSeat(Vector3 position, float rotation)
    {
        GameObject seat = new GameObject("Seat");
        seat.transform.SetParent(environmentRoot.transform);
        seat.transform.position = position;
        seat.transform.rotation = Quaternion.Euler(0, rotation, 0);
        
        // Cushion
        GameObject cushion = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cushion.transform.SetParent(seat.transform);
        cushion.transform.localPosition = new Vector3(0, 0.4f, 0);
        cushion.transform.localScale = new Vector3(0.6f, 0.15f, 0.6f);
        cushion.GetComponent<Renderer>().material = CreateMaterial(new Color(0.6f, 0.4f, 0.3f));
        
        // Backrest
        GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.transform.SetParent(seat.transform);
        back.transform.localPosition = new Vector3(0, 0.7f, 0.25f);
        back.transform.localScale = new Vector3(0.5f, 0.5f, 0.1f);
        back.GetComponent<Renderer>().material = CreateMaterial(new Color(0.6f, 0.4f, 0.3f));
        Destroy(back.GetComponent<Collider>());
    }
    
    void CreateTable(Vector3 position, float radius)
    {
        GameObject table = new GameObject("Table");
        table.transform.SetParent(environmentRoot.transform);
        table.transform.position = position;
        
        // Top
        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        top.transform.SetParent(table.transform);
        top.transform.localPosition = new Vector3(0, 0.7f, 0);
        top.transform.localScale = new Vector3(radius * 2, 0.05f, radius * 2);
        top.GetComponent<Renderer>().material = CreateMaterial(new Color(0.5f, 0.4f, 0.3f));
        
        // Pedestal
        GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestal.transform.SetParent(table.transform);
        pedestal.transform.localPosition = new Vector3(0, 0.35f, 0);
        pedestal.transform.localScale = new Vector3(0.3f, 0.35f, 0.3f);
        pedestal.GetComponent<Renderer>().material = CreateMaterial(new Color(0.3f, 0.3f, 0.3f));
        Destroy(pedestal.GetComponent<Collider>());
    }
    
    void CreatePlanter(Vector3 position)
    {
        GameObject planter = new GameObject("Planter");
        planter.transform.SetParent(environmentRoot.transform);
        planter.transform.position = position;
        
        // Container
        GameObject container = GameObject.CreatePrimitive(PrimitiveType.Cube);
        container.transform.SetParent(planter.transform);
        container.transform.localPosition = new Vector3(0, 0.4f, 0);
        container.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        container.GetComponent<Renderer>().material = CreateMaterial(new Color(0.5f, 0.5f, 0.5f));
        
        // Plant
        GameObject plant = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        plant.transform.SetParent(planter.transform);
        plant.transform.localPosition = new Vector3(0, 1f, 0);
        plant.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);
        plant.GetComponent<Renderer>().material = CreateMaterial(new Color(0.2f, 0.6f, 0.3f));
        Destroy(plant.GetComponent<Collider>());
    }
    
    void CreateLightFixture(Vector3 position)
    {
        GameObject fixture = new GameObject("LightFixture");
        fixture.transform.SetParent(environmentRoot.transform);
        fixture.transform.position = position;
        
        // Dome
        GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dome.transform.SetParent(fixture.transform);
        dome.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);
        dome.GetComponent<Renderer>().material = CreateMaterial(new Color(0.9f, 0.85f, 0.7f));
        Destroy(dome.GetComponent<Collider>());
        
        // Add point light
        Light light = fixture.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.95f, 0.8f);
        light.intensity = 0.8f;
        light.range = 8f;
    }
    
    // ============================================
    // 3D BUTTON CREATION
    // ============================================
    
    void Create3DButton(Vector3 position, string label, Color color, System.Action callback)
    {
        GameObject button = new GameObject("Button_" + label);
        button.transform.SetParent(uiRoot.transform);
        button.transform.position = position;
        
        // Button body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(button.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(1.5f, 0.5f, 0.1f);
        body.GetComponent<Renderer>().material = CreateMaterial(color);
        
        // Make button face camera (billboard effect handled in Update)
        button.AddComponent<FaceCamera>().mainCamera = mainCamera;
        
        // Create label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(button.transform);
        labelObj.transform.localPosition = new Vector3(0, 0, -0.06f);
        labelObj.transform.localScale = Vector3.one * 0.01f;
        
        // Add Canvas for text
        Canvas canvas = labelObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 50);
        
        // Add text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(labelObj.transform);
        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 30;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        
        RectTransform rectTransform = text.GetComponent<RectTransform>();
        rectTransform.localPosition = Vector3.zero;
        rectTransform.sizeDelta = new Vector2(200, 50);
        
        // Store button data
        InteractiveButton btn = new InteractiveButton
        {
            gameObject = body,
            label = label,
            callback = callback,
            originalColor = color,
            hoverColor = color * 1.3f,
            labelText = text
        };
        buttons.Add(btn);
    }
    
    // ============================================
    // UI CREATION
    // ============================================
    
    void CreateUI()
    {
        // Create canvas for HUD
        GameObject canvasObj = new GameObject("HUD Canvas");
        canvasObj.transform.SetParent(uiRoot.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Title text (top center)
        titleText = CreateUIText(canvasObj, "TitleText", "", 28, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50));
        titleText.alignment = TextAnchor.MiddleCenter;
        
        // Message text (bottom center)
        messageText = CreateUIText(canvasObj, "MessageText", "", 24, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 100));
        messageText.alignment = TextAnchor.MiddleCenter;
        
        // Add background panel to message
        GameObject msgBg = new GameObject("MessageBackground");
        msgBg.transform.SetParent(messageText.transform.parent);
        msgBg.transform.SetAsFirstSibling();
        Image bgImage = msgBg.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f);
        RectTransform bgRect = bgImage.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0);
        bgRect.anchorMax = new Vector2(1, 1);
        bgRect.offsetMin = new Vector2(-20, -10);
        bgRect.offsetMax = new Vector2(20, 10);
    }
    
    Text CreateUIText(GameObject parent, string name, string content, int fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 position)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform);
        
        Text text = textObj.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        
        RectTransform rectTransform = text.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(600, 100);
        
        // Add outline for readability
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);
        
        return text;
    }
    
    void CreateReticle()
    {
        // Center dot reticle
        GameObject canvasObj = new GameObject("Reticle Canvas");
        canvasObj.transform.SetParent(uiRoot.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        // Reticle dot
        reticle = new GameObject("Reticle");
        reticle.transform.SetParent(canvasObj.transform);
        Image img = reticle.AddComponent<Image>();
        img.color = Color.white;
        
        RectTransform rect = img.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(10, 10);
        
        // Progress indicator (ring around reticle)
        progressIndicator = new GameObject("Progress");
        progressIndicator.transform.SetParent(canvasObj.transform);
        Image progressImg = progressIndicator.AddComponent<Image>();
        progressImg.color = new Color(0, 1, 0, 0.5f);
        progressImg.type = Image.Type.Filled;
        progressImg.fillMethod = Image.FillMethod.Radial360;
        progressImg.fillAmount = 0;
        
        RectTransform progressRect = progressImg.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.5f, 0.5f);
        progressRect.anchorMax = new Vector2(0.5f, 0.5f);
        progressRect.sizeDelta = new Vector2(40, 40);
    }
    
    // ============================================
    // INTERACTIVE TABLET
    // ============================================
    
    void CreateInteractiveTablet()
    {
        // Create tablet object in the environment
        tabletObject = new GameObject("InteractiveTablet");
        tabletObject.transform.SetParent(environmentRoot.transform);
        tabletObject.transform.position = new Vector3(2f, 1.2f, 3f);
        tabletObject.transform.rotation = Quaternion.Euler(15f, -20f, 0f);
        
        // Tablet body (frame)
        GameObject tabletBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tabletBody.name = "TabletBody";
        tabletBody.transform.SetParent(tabletObject.transform);
        tabletBody.transform.localPosition = Vector3.zero;
        tabletBody.transform.localScale = new Vector3(0.5f, 0.35f, 0.02f);
        tabletBody.GetComponent<Renderer>().material = CreateMaterial(new Color(0.15f, 0.15f, 0.15f));
        
        // Tablet screen
        GameObject tabletScreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tabletScreen.name = "TabletScreen";
        tabletScreen.transform.SetParent(tabletObject.transform);
        tabletScreen.transform.localPosition = new Vector3(0, 0, -0.011f);
        tabletScreen.transform.localScale = new Vector3(0.45f, 0.30f, 0.005f);
        Material screenMat = CreateMaterial(new Color(0.1f, 0.3f, 0.5f));
        screenMat.EnableKeyword("_EMISSION");
        screenMat.SetColor("_EmissionColor", new Color(0.1f, 0.3f, 0.5f) * 0.3f);
        tabletScreen.GetComponent<Renderer>().material = screenMat;
        
        // Screen content hint
        GameObject labelObj = new GameObject("TabletLabel");
        labelObj.transform.SetParent(tabletObject.transform);
        labelObj.transform.localPosition = new Vector3(0, 0, -0.015f);
        labelObj.transform.localScale = Vector3.one * 0.005f;
        
        Canvas labelCanvas = labelObj.AddComponent<Canvas>();
        labelCanvas.renderMode = RenderMode.WorldSpace;
        labelCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 300);
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(labelObj.transform);
        Text labelText = textObj.AddComponent<Text>();
        labelText.text = "📱 INFO\n\nGaze here to\nopen panel";
        labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelText.fontSize = 36;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = labelText.GetComponent<RectTransform>();
        textRect.localPosition = Vector3.zero;
        textRect.sizeDelta = new Vector2(400, 300);
        
        // Register tablet as interactive button
        InteractiveButton tabletBtn = new InteractiveButton
        {
            gameObject = tabletBody,
            label = "Open Panel",
            callback = OnTabletGazed,
            originalColor = new Color(0.15f, 0.15f, 0.15f),
            hoverColor = new Color(0.25f, 0.25f, 0.3f),
            labelText = labelText
        };
        buttons.Add(tabletBtn);
    }
    
    void OnTabletGazed()
    {
        if (!isPanelOpen)
        {
            OpenFloatingPanel();
        }
    }
    
    // ============================================
    // FLOATING PANEL SYSTEM
    // ============================================
    
    void CreateFloatingPanel()
    {
        floatingPanel = new GameObject("FloatingPanel");
        floatingPanel.transform.SetParent(uiRoot.transform);
        
        // Position panel in front of camera initially
        PositionPanelInFrontOfCamera();
        
        // Main panel background
        GameObject panelBg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panelBg.name = "PanelBackground";
        panelBg.transform.SetParent(floatingPanel.transform);
        panelBg.transform.localPosition = Vector3.zero;
        panelBg.transform.localScale = new Vector3(1.2f, 0.9f, 0.02f);
        Material panelMat = CreateMaterial(new Color(0.12f, 0.14f, 0.18f, 0.95f));
        panelBg.GetComponent<Renderer>().material = panelMat;
        
        // Panel border/frame
        GameObject panelFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panelFrame.name = "PanelFrame";
        panelFrame.transform.SetParent(floatingPanel.transform);
        panelFrame.transform.localPosition = new Vector3(0, 0, 0.005f);
        panelFrame.transform.localScale = new Vector3(1.25f, 0.95f, 0.015f);
        panelFrame.GetComponent<Renderer>().material = CreateMaterial(new Color(0.3f, 0.5f, 0.8f));
        Destroy(panelFrame.GetComponent<Collider>());
        
        // Header bar (for dragging)
        GameObject headerBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headerBar.name = "DragHandle";
        headerBar.transform.SetParent(floatingPanel.transform);
        headerBar.transform.localPosition = new Vector3(0, 0.38f, -0.015f);
        headerBar.transform.localScale = new Vector3(1.15f, 0.12f, 0.01f);
        headerBar.GetComponent<Renderer>().material = CreateMaterial(new Color(0.2f, 0.4f, 0.7f));
        
        // Register drag handle as interactive
        InteractiveButton dragBtn = new InteractiveButton
        {
            gameObject = headerBar,
            label = "Drag Panel",
            callback = StartDraggingPanel,
            originalColor = new Color(0.2f, 0.4f, 0.7f),
            hoverColor = new Color(0.3f, 0.5f, 0.8f),
            labelText = null
        };
        buttons.Add(dragBtn);
        
        // Close button (top-right)
        GameObject closeBtn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        closeBtn.name = "CloseButton";
        closeBtn.transform.SetParent(floatingPanel.transform);
        closeBtn.transform.localPosition = new Vector3(0.52f, 0.38f, -0.02f);
        closeBtn.transform.localScale = new Vector3(0.1f, 0.1f, 0.01f);
        closeBtn.GetComponent<Renderer>().material = CreateMaterial(new Color(0.9f, 0.3f, 0.3f));
        
        // Close button X label
        CreatePanelButtonLabel(closeBtn.transform, "X", 40);
        
        // Register close button
        InteractiveButton closeBtnData = new InteractiveButton
        {
            gameObject = closeBtn,
            label = "Close",
            callback = CloseFloatingPanel,
            originalColor = new Color(0.9f, 0.3f, 0.3f),
            hoverColor = new Color(1f, 0.4f, 0.4f),
            labelText = null
        };
        buttons.Add(closeBtnData);
        
        // Resize button (bottom-right corner)
        GameObject resizeBtn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        resizeBtn.name = "ResizeHandle";
        resizeBtn.transform.SetParent(floatingPanel.transform);
        resizeBtn.transform.localPosition = new Vector3(0.52f, -0.38f, -0.02f);
        resizeBtn.transform.localScale = new Vector3(0.12f, 0.12f, 0.01f);
        resizeBtn.GetComponent<Renderer>().material = CreateMaterial(new Color(0.5f, 0.7f, 0.3f));
        
        // Resize label
        CreatePanelButtonLabel(resizeBtn.transform, "⤡", 36);
        
        // Register resize button
        InteractiveButton resizeBtnData = new InteractiveButton
        {
            gameObject = resizeBtn,
            label = "Resize",
            callback = StartResizingPanel,
            originalColor = new Color(0.5f, 0.7f, 0.3f),
            hoverColor = new Color(0.6f, 0.8f, 0.4f),
            labelText = null
        };
        buttons.Add(resizeBtnData);
        
        // Size increase button
        GameObject sizeUpBtn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sizeUpBtn.name = "SizeUpButton";
        sizeUpBtn.transform.SetParent(floatingPanel.transform);
        sizeUpBtn.transform.localPosition = new Vector3(0.38f, -0.38f, -0.02f);
        sizeUpBtn.transform.localScale = new Vector3(0.1f, 0.1f, 0.01f);
        sizeUpBtn.GetComponent<Renderer>().material = CreateMaterial(new Color(0.3f, 0.6f, 0.9f));
        
        CreatePanelButtonLabel(sizeUpBtn.transform, "+", 48);
        
        InteractiveButton sizeUpBtnData = new InteractiveButton
        {
            gameObject = sizeUpBtn,
            label = "Increase Size",
            callback = IncreasePanelSize,
            originalColor = new Color(0.3f, 0.6f, 0.9f),
            hoverColor = new Color(0.4f, 0.7f, 1f),
            labelText = null
        };
        buttons.Add(sizeUpBtnData);
        
        // Size decrease button
        GameObject sizeDownBtn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sizeDownBtn.name = "SizeDownButton";
        sizeDownBtn.transform.SetParent(floatingPanel.transform);
        sizeDownBtn.transform.localPosition = new Vector3(0.25f, -0.38f, -0.02f);
        sizeDownBtn.transform.localScale = new Vector3(0.1f, 0.1f, 0.01f);
        sizeDownBtn.GetComponent<Renderer>().material = CreateMaterial(new Color(0.9f, 0.6f, 0.3f));
        
        CreatePanelButtonLabel(sizeDownBtn.transform, "-", 48);
        
        InteractiveButton sizeDownBtnData = new InteractiveButton
        {
            gameObject = sizeDownBtn,
            label = "Decrease Size",
            callback = DecreasePanelSize,
            originalColor = new Color(0.9f, 0.6f, 0.3f),
            hoverColor = new Color(1f, 0.7f, 0.4f),
            labelText = null
        };
        buttons.Add(sizeDownBtnData);
        
        // Bring to Front button (bottom left)
        GameObject bringToFrontBtn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bringToFrontBtn.name = "BringToFrontButton";
        bringToFrontBtn.transform.SetParent(floatingPanel.transform);
        bringToFrontBtn.transform.localPosition = new Vector3(-0.45f, -0.38f, -0.02f);
        bringToFrontBtn.transform.localScale = new Vector3(0.2f, 0.1f, 0.01f);
        bringToFrontBtn.GetComponent<Renderer>().material = CreateMaterial(new Color(0.6f, 0.3f, 0.8f));
        
        CreatePanelButtonLabel(bringToFrontBtn.transform, "↻", 36);
        
        InteractiveButton bringToFrontBtnData = new InteractiveButton
        {
            gameObject = bringToFrontBtn,
            label = "Bring to Front",
            callback = BringPanelToFront,
            originalColor = new Color(0.6f, 0.3f, 0.8f),
            hoverColor = new Color(0.7f, 0.4f, 0.9f),
            labelText = null
        };
        buttons.Add(bringToFrontBtnData);
        
        // Content area with HTML-like content
        CreatePanelContent();
        
        // Hide panel initially
        floatingPanel.SetActive(false);
    }
    
    void CreatePanelButtonLabel(Transform parent, string text, int fontSize)
    {
        GameObject labelObj = new GameObject("ButtonLabel");
        labelObj.transform.SetParent(parent);
        labelObj.transform.localPosition = new Vector3(0, 0, -0.6f);
        labelObj.transform.localScale = Vector3.one * 0.08f;
        
        Canvas canvas = labelObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(labelObj.transform);
        Text labelText = textObj.AddComponent<Text>();
        labelText.text = text;
        labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelText.fontSize = fontSize;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.fontStyle = FontStyle.Bold;
        
        RectTransform textRect = labelText.GetComponent<RectTransform>();
        textRect.localPosition = Vector3.zero;
        textRect.sizeDelta = new Vector2(100, 100);
    }
    
    void CreatePanelContent()
    {
        GameObject contentArea = new GameObject("ContentArea");
        contentArea.transform.SetParent(floatingPanel.transform);
        contentArea.transform.localPosition = new Vector3(0, -0.02f, -0.015f);
        contentArea.transform.localScale = Vector3.one * 0.004f;
        
        Canvas contentCanvas = contentArea.AddComponent<Canvas>();
        contentCanvas.renderMode = RenderMode.WorldSpace;
        contentCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(550, 400);
        
        // Background for content
        GameObject bgObj = new GameObject("ContentBg");
        bgObj.transform.SetParent(contentArea.transform);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.95f, 0.95f, 0.97f);
        RectTransform bgRect = bgImage.GetComponent<RectTransform>();
        bgRect.localPosition = Vector3.zero;
        bgRect.sizeDelta = new Vector2(550, 400);
        
        // HTML-style content
        string htmlContent = @"<b><color=#2563eb>🌊 Ocean Conservation Info</color></b>

<color=#1e3a5f>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>

<b>Why Ocean Conservation Matters</b>

• Oceans produce <color=#059669>50% of Earth's oxygen</color>
• Home to <color=#0891b2>over 230,000 known species</color>
• Absorbs <color=#7c3aed>30% of CO2</color> produced by humans
• <color=#dc2626>8 million tons</color> of plastic enter oceans yearly

<color=#1e3a5f>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>

<b>What You Can Do:</b>

  ✓ Reduce single-use plastics
  ✓ Support marine protected areas
  ✓ Choose sustainable seafood
  ✓ Participate in beach cleanups

<color=#64748b><i>Drag header to move • Use +/- to resize</i></color>";

        GameObject textObj = new GameObject("HTMLContent");
        textObj.transform.SetParent(contentArea.transform);
        Text contentText = textObj.AddComponent<Text>();
        contentText.text = htmlContent;
        contentText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        contentText.fontSize = 22;
        contentText.color = new Color(0.1f, 0.1f, 0.15f);
        contentText.alignment = TextAnchor.UpperLeft;
        contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
        contentText.verticalOverflow = VerticalWrapMode.Truncate;
        contentText.supportRichText = true;
        contentText.lineSpacing = 1.1f;
        
        RectTransform textRect = contentText.GetComponent<RectTransform>();
        textRect.localPosition = Vector3.zero;
        textRect.sizeDelta = new Vector2(520, 380);
    }
    
    void PositionPanelInFrontOfCamera()
    {
        if (mainCamera != null)
        {
            Vector3 forward = mainCamera.transform.forward;
            forward.y = 0; // Keep panel level
            forward.Normalize();
            
            floatingPanel.transform.position = mainCamera.transform.position + forward * 2f + Vector3.up * 0.2f;
            floatingPanel.transform.rotation = Quaternion.LookRotation(forward);
        }
    }
    
    void OpenFloatingPanel()
    {
        isPanelOpen = true;
        PositionPanelInFrontOfCamera();
        floatingPanel.SetActive(true);
        ShowMessage("Panel opened! Gaze at X to close, drag header to move, +/- to resize.");
    }
    
    void CloseFloatingPanel()
    {
        isPanelOpen = false;
        isDraggingPanel = false;
        isResizingPanel = false;
        floatingPanel.SetActive(false);
        ShowMessage("Panel closed. Gaze at tablet to reopen.");
    }
    
    void StartDraggingPanel()
    {
        isDraggingPanel = true;
        ShowMessage("Panel grabbed! Look around to move it. Gaze at header again to release.");
    }
    
    void StartResizingPanel()
    {
        if (isDraggingPanel)
        {
            isDraggingPanel = false;
            ShowMessage("Panel released.");
        }
    }
    
    void IncreasePanelSize()
    {
        panelScale = Mathf.Min(panelScale + 0.15f, MAX_PANEL_SCALE);
        floatingPanel.transform.localScale = Vector3.one * panelScale;
        ShowMessage("Panel size: " + Mathf.RoundToInt(panelScale * 100) + "%");
    }
    
    void DecreasePanelSize()
    {
        panelScale = Mathf.Max(panelScale - 0.15f, MIN_PANEL_SCALE);
        floatingPanel.transform.localScale = Vector3.one * panelScale;
        ShowMessage("Panel size: " + Mathf.RoundToInt(panelScale * 100) + "%");
    }
    
    void BringPanelToFront()
    {
        isDraggingPanel = false;
        PositionPanelInFrontOfCamera();
        ShowMessage("Panel repositioned in front of you!");
    }
    
    void UpdatePanelDragging()
    {
        if (isDraggingPanel && isPanelOpen && mainCamera != null)
        {
            // Move panel to follow gaze direction
            Vector3 targetPos = mainCamera.transform.position + mainCamera.transform.forward * 2f;
            floatingPanel.transform.position = Vector3.Lerp(floatingPanel.transform.position, targetPos, Time.deltaTime * 8f);
            
            // Make panel face camera
            Vector3 lookDir = floatingPanel.transform.position - mainCamera.transform.position;
            lookDir.y = 0;
            if (lookDir.magnitude > 0.1f)
            {
                floatingPanel.transform.rotation = Quaternion.Slerp(
                    floatingPanel.transform.rotation,
                    Quaternion.LookRotation(lookDir),
                    Time.deltaTime * 8f
                );
            }
        }
    }
    
    // ============================================
    // GAZE INTERACTION
    // ============================================
    
    void HandleGazeInteraction()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;
        
        Image progressImage = progressIndicator.GetComponent<Image>();
        Image reticleImage = reticle.GetComponent<Image>();
        
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // Check if we hit a button
            InteractiveButton hitButton = null;
            foreach (var btn in buttons)
            {
                if (hit.collider.gameObject == btn.gameObject)
                {
                    hitButton = btn;
                    break;
                }
            }
            
            if (hitButton != null)
            {
                if (currentTarget != hitButton.gameObject)
                {
                    // New target
                    ResetCurrentTarget();
                    currentTarget = hitButton.gameObject;
                    gazeTimer = 0f;
                    isGazing = true;
                    
                    // Highlight button
                    hitButton.gameObject.GetComponent<Renderer>().material.color = hitButton.hoverColor;
                }
                
                // Update gaze timer
                gazeTimer += Time.deltaTime;
                progressImage.fillAmount = gazeTimer / gazeTime;
                reticleImage.color = Color.green;
                
                // Check if gaze completed
                if (gazeTimer >= gazeTime)
                {
                    // Activate button
                    hitButton.callback?.Invoke();
                    
                    // Reset
                    gazeTimer = 0f;
                    progressImage.fillAmount = 0f;
                    
                    // Visual feedback
                    StartCoroutine(ButtonActivatedFeedback(hitButton));
                }
            }
            else
            {
                ResetGaze(progressImage, reticleImage);
            }
        }
        else
        {
            ResetGaze(progressImage, reticleImage);
        }
    }
    
    void ResetGaze(Image progressImage, Image reticleImage)
    {
        if (isGazing)
        {
            ResetCurrentTarget();
            isGazing = false;
        }
        currentTarget = null;
        gazeTimer = 0f;
        progressImage.fillAmount = 0f;
        reticleImage.color = Color.white;
    }
    
    void ResetCurrentTarget()
    {
        if (currentTarget != null)
        {
            foreach (var btn in buttons)
            {
                if (btn.gameObject == currentTarget)
                {
                    btn.gameObject.GetComponent<Renderer>().material.color = btn.originalColor;
                    break;
                }
            }
        }
    }
    
    IEnumerator ButtonActivatedFeedback(InteractiveButton button)
    {
        Color originalColor = button.originalColor;
        Renderer renderer = button.gameObject.GetComponent<Renderer>();
        
        // Flash white
        renderer.material.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        renderer.material.color = originalColor;
    }
    
    // ============================================
    // INPUT HANDLING
    // ============================================
    
    void HandleKeyboardInput()
    {
        // Keyboard input is now handled by DesktopCameraController
        // This method is kept for any future custom keyboard shortcuts
    }
    
    // ============================================
    // BUTTON CALLBACKS
    // ============================================
    
    // Relaxation callbacks
    void OnBreatheButton()
    {
        ShowMessage("Take a deep breath in... hold... and release slowly.\nRepeat 5 times for calm.");
    }
    
    void OnMeditateButton()
    {
        ShowMessage("Close your eyes. Focus on your breath.\nLet thoughts pass like clouds in the sky.");
    }
    
    void OnSoundsButton()
    {
        ShowMessage("🎵 Nature sounds activated.\nListen to the gentle water and birdsong.");
    }
    
    void OnAffirmationsButton()
    {
        string[] affirmations = {
            "I am calm and at peace.",
            "I choose to feel good about myself.",
            "I am worthy of love and happiness.",
            "Today I choose joy.",
            "I am grateful for this moment."
        };
        ShowMessage(affirmations[Random.Range(0, affirmations.Length)]);
    }
    
    void OnBodyScanButton()
    {
        ShowMessage("Starting body scan...\nBring attention to your feet, legs, torso, arms, and head.\nRelease tension as you go.");
    }
    
    // Educational callbacks
    void OnStartLessonButton()
    {
        ShowMessage("📚 Welcome to today's lesson!\nTopic: Introduction to Virtual Reality\nLook around to explore the classroom.");
    }
    
    void OnQuizButton()
    {
        ShowMessage("❓ Quiz Time!\nQ: What does VR stand for?\nA: Virtual Reality!");
    }
    
    void OnResourcesButton()
    {
        ShowMessage("📖 Additional Resources:\n- Unity Documentation\n- VR Best Practices Guide\n- Interactive Tutorials");
    }
    
    void OnProgressButton()
    {
        ShowMessage("📊 Your Progress:\nLessons Completed: 3/10\nQuizzes Passed: 2/5\nKeep up the great work!");
    }
    
    // Fitness callbacks
    void OnWarmUpButton()
    {
        ShowMessage("🔥 Warm Up Routine:\n1. Arm circles - 30 sec\n2. Leg swings - 30 sec\n3. Torso twists - 30 sec");
    }
    
    void OnCardioButton()
    {
        ShowMessage("❤️ Cardio Session:\nJumping jacks - 1 min\nHigh knees - 1 min\nBurpees - 30 sec\nRest and repeat!");
    }
    
    void OnStrengthButton()
    {
        ShowMessage("💪 Strength Training:\nPush-ups - 15 reps\nSquats - 20 reps\nPlanks - 45 sec\n3 sets each!");
    }
    
    void OnCoolDownButton()
    {
        ShowMessage("🧘 Cool Down:\nDeep breathing - 1 min\nHamstring stretch - 30 sec each leg\nShoulder stretch - 30 sec each arm");
    }
    
    // Productivity callbacks
    void OnFocusTimerButton()
    {
        ShowMessage("⏱️ Focus Timer Started!\n25 minutes of deep work.\nStay focused, you've got this!");
    }
    
    void OnTaskListButton()
    {
        ShowMessage("📋 Today's Tasks:\n☐ Complete VR assignment\n☐ Review project code\n☐ Team meeting at 3pm\n☑ Morning planning");
    }
    
    void OnBreakTimeButton()
    {
        ShowMessage("☕ Break Time!\nStep away from work for 5 minutes.\nStretch, hydrate, and rest your eyes.");
    }
    
    void OnNotesButton()
    {
        ShowMessage("📝 Quick Notes:\n- Remember to save frequently\n- Test in VR headset\n- Check lighting settings");
    }
    
    // Social callbacks
    void OnJoinChatButton()
    {
        ShowMessage("💬 Joining community chat...\nWelcome! There are 12 people online.\nSay hi to make new friends!");
    }
    
    void OnFindFriendsButton()
    {
        ShowMessage("👥 Find Friends:\n- Browse by interests\n- Join activity groups\n- Attend virtual events\nNew connections await!");
    }
    
    void OnEventsButton()
    {
        ShowMessage("📅 Upcoming Events:\n- VR Game Night - Friday 7pm\n- Meditation Circle - Saturday 10am\n- Study Group - Sunday 2pm");
    }
    
    void OnMyProfileButton()
    {
        ShowMessage("👤 Your Profile:\nLevel: Explorer\nFriends: 15\nEvents Attended: 8\nAchievements: 5/20");
    }
    
    // ============================================
    // HELPER METHODS
    // ============================================
    
    void ShowMessage(string message)
    {
        messageText.text = message;
        CancelInvoke("ClearMessage");
        Invoke("ClearMessage", 5f);
    }
    
    void ClearMessage()
    {
        messageText.text = "";
    }
    
    void ShowWelcomeMessage()
    {
        string themeName = appTheme.ToString();
        themeName = System.Text.RegularExpressions.Regex.Replace(themeName, "([a-z])([A-Z])", "$1 $2");
        
        titleText.text = "Welcome to " + themeName;
        ShowMessage("Look at buttons for " + gazeTime + " seconds to interact.\nGaze at the TABLET to open info panel. Use mouse to look around, WASD to move.");
    }
    
    Material CreateMaterial(Color color)
    {
        // Try to use URP Lit shader, fall back to Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        
        Material mat = new Material(shader);
        mat.color = color;
        
        return mat;
    }
}

// ============================================
// HELPER COMPONENTS
// ============================================

/// <summary>
/// Makes an object always face the camera
/// </summary>
public class FaceCamera : MonoBehaviour
{
    public Camera mainCamera;
    
    void Update()
    {
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up);
        }
    }
}

/// <summary>
/// Adds floating animation to objects
/// </summary>
public class FloatingAnimation : MonoBehaviour
{
    private Vector3 startPos;
    private float offset;
    
    void Start()
    {
        startPos = transform.position;
        offset = Random.Range(0f, Mathf.PI * 2f);
    }
    
    void Update()
    {
        float y = Mathf.Sin(Time.time + offset) * 0.3f;
        transform.position = startPos + new Vector3(0, y, 0);
    }
}

/// <summary>
/// Desktop camera controller for testing
/// </summary>
public class DesktopCameraController : MonoBehaviour
{
    public float mouseSensitivity = 2f;
    public float moveSpeed = 5f;
    public float minY = 1.5f; // Minimum height (ground collision)
    
    private float rotationX = 0f;
    private float rotationY = 0f;
    private bool isActive = false;
    
    void Start()
    {
        // Initialize rotation from current camera orientation
        Vector3 euler = transform.eulerAngles;
        rotationY = euler.y;
        rotationX = euler.x;
        if (rotationX > 180f) rotationX -= 360f;
        
        // Lock cursor on start
        LockCursor();
    }
    
    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isActive = true;
    }
    
    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isActive = false;
    }
    
    void Update()
    {
        // Click to lock cursor / activate camera control
        if (Input.GetMouseButtonDown(0) && !isActive)
        {
            LockCursor();
        }
        
        // Escape to unlock cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isActive)
            {
                UnlockCursor();
            }
            else
            {
                LockCursor();
            }
        }
        
        // Mouse look - only when cursor is locked
        if (isActive && Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            rotationY += mouseX;
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
            
            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
        }
        
        // WASD movement - always active
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move += transform.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move -= transform.forward;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move -= transform.right;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += transform.right;
        if (Input.GetKey(KeyCode.Q)) move -= transform.up;
        if (Input.GetKey(KeyCode.E)) move += transform.up;
        
        // Sprint with shift
        float speed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * 2f : moveSpeed;
        
        if (move.magnitude > 0)
        {
            transform.position += move.normalized * speed * Time.deltaTime;
            
            // Prevent going below ground
            if (transform.position.y < minY)
            {
                transform.position = new Vector3(transform.position.x, minY, transform.position.z);
            }
        }
    }
    
    void OnGUI()
    {
        // Show instructions when cursor is unlocked
        if (!isActive)
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 50, 300, 100), 
                "<size=20><b>Click to start</b></size>\n\nMouse = Look\nWASD = Move\nESC = Pause", 
                new GUIStyle { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } });
        }
    }
}

