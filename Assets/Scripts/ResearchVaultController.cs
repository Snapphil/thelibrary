using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

/// <summary>
/// Research Vault: A Voxel-Style Virtual Reality Academic Library
/// Main controller for the Minecraft-style two-story library with interactive research tablets
/// </summary>
public class ResearchVaultController : MonoBehaviour
{
    [Header("=== VOXEL LIBRARY SETTINGS ===")]
    [SerializeField] private float voxelSize = 0.25f;
    [SerializeField] private Material stoneBrickMaterial;
    [SerializeField] private Material woodMaterial;
    [SerializeField] private Material darkWoodMaterial;
    [SerializeField] private Material glassMaterial;
    [SerializeField] private Material bookMaterial;
    [SerializeField] private Material carpetMaterial;
    [SerializeField] private Material tabletMaterial;
    [SerializeField] private Material tabletScreenMaterial;
    [SerializeField] private Material leafMaterial;
    [SerializeField] private Material cobblestoneMaterial;
    
    [Header("=== TABLET SETTINGS ===")]
    [SerializeField] private float gazeActivationTime = 2f;
    [SerializeField] private string[] paperUrls = new string[4] {
        "StreamingAssets/Paper1.html",
        "StreamingAssets/Paper2.html", 
        "StreamingAssets/Paper3.html",
        "StreamingAssets/Paper4.html"
    };
    
    [Header("=== PLAYER SETTINGS ===")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float gravity = -9.81f;
    
    [Header("=== LIGHTING (NIGHT MODE) ===")]
    [SerializeField] private Color ambientColor = new Color(0.05f, 0.05f, 0.1f); // Dark night ambient
    [SerializeField] private Color moonColor = new Color(0.4f, 0.45f, 0.6f); // Cool moonlight
    
    // Private variables
    private Camera mainCamera;
    private CharacterController playerController;
    private GameObject player;
    private float verticalVelocity;
    private float rotationX;
    private float rotationY;
    private bool cursorLocked = true;
    
    // Tablet interaction
    private GameObject[] tablets = new GameObject[4];
    private GameObject currentGazedTablet;
    private float gazeTimer;
    private int activeTabletIndex = -1;
    private bool isViewingPaper = false;
    
    // Gaze indicator
    private GameObject gazeIndicator;
    private Image gazeProgressImage;
    
    // Materials storage
    private Dictionary<string, Material> materials = new Dictionary<string, Material>();
    
    // JavaScript interop
    [DllImport("__Internal")]
    private static extern void ShowPaperViewer(string url);
    
    [DllImport("__Internal")]
    private static extern void HidePaperViewer();
    
    // Gate variables
    private GameObject gateObject;
    private bool isGateOpen = false;
    private GameObject gateKnob;

    void Start()
    {
        Debug.Log("=== RESEARCH VAULT: Initializing Voxel Academic Library ===");
        
        CreateMaterials();
        CreatePlayer();
        CreateLighting();
        CreateSkybox();
        CreateVoxelLibrary();
        CreateTablets();
        CreateTVScreens(); // Create TVs for all tablets
        CreateGazeIndicator();
        CreateUI();
        
        // New Enhancements
        BuildGardenAndSurroundings();
        // Gate removed - was blocking stairway access
        // CreateInteractiveGate();
        
        // Add torches throughout library and outside
        CreateTorches();
        
        LockCursor();
        
        // Defer content height calculation until layout is ready
        StartCoroutine(InitializeContentHeightsDelayed());
        
        Debug.Log("=== RESEARCH VAULT: Library Ready ===");
    }
    
    IEnumerator InitializeContentHeightsDelayed()
    {
        // Wait for end of frame to let layout system calculate sizes
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame(); // Extra frame for safety
        
        // Recalculate content heights for each TV
        for (int i = 0; i < 16; i++)
        {
            if (contentContainers[i] != null)
            {
                // Get the actual content height from the RectTransform
                float actualHeight = contentContainers[i].rect.height;
                if (actualHeight > 0)
                {
                    contentHeights[i] = actualHeight;
                    Debug.Log($"TV {i}: Final content height = {contentHeights[i]}");
                }
            }
        }
    }
    
    void Update()
    {
        HandleMouseLook(); // Always update mouse look (locked or not)
        
        if (!isViewingPaper)
        {
            HandleMovement();
            HandleGazeInteraction();
            // HandleGateInteraction(); // Gate removed - was blocking stairway
            // Check if gazing at TV for passive scrolling
            HandleTVGaze();
        }
        else
        {
            HandlePanelInteraction(); // Handle active panel interaction
        }
        
        HandleCursorLock();
    }

    void HandleTVGaze()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;
        
        gazedTvIndex = -1;
        
        if (Physics.Raycast(ray, out hit, 10f))
        {
            // Check if we hit a TV screen
            // We put a BoxCollider on the TV screen object
            for (int i = 0; i < 16; i++)
            {
                if (tvScreens[i] != null && hit.collider.gameObject == tvScreens[i])
                {
                    gazedTvIndex = i;
                    break;
                }
                // Also check parent if hit frame/buttons
                if (tvScreens[i] != null && hit.collider.transform.IsChildOf(tvScreens[i].transform))
                {
                    gazedTvIndex = i;
                    break;
                }
            }
        }
        
        // Allow scrolling if gazing at a TV
        if (gazedTvIndex != -1)
        {
             HandlePanelKeyboardInput(gazedTvIndex);
        }
    }
    
    #region MATERIAL CREATION
    
    void CreateMaterials()
    {
        // Stone brick material (walls)
        stoneBrickMaterial = CreateMaterial("StoneBrick", new Color(0.45f, 0.42f, 0.38f));
        
        // Wood material (floors, furniture)
        woodMaterial = CreateMaterial("Wood", new Color(0.55f, 0.35f, 0.2f));
        
        // Dark wood (shelves, trim)
        darkWoodMaterial = CreateMaterial("DarkWood", new Color(0.25f, 0.15f, 0.1f));
        
        // Glass material (windows)
        glassMaterial = CreateTransparentMaterial("Glass", new Color(0.7f, 0.85f, 0.95f, 0.3f));
        
        // Book materials (various colors)
        bookMaterial = CreateMaterial("Book", new Color(0.6f, 0.2f, 0.15f));
        
        // Carpet material
        carpetMaterial = CreateMaterial("Carpet", new Color(0.4f, 0.15f, 0.15f));
        
        // Tablet materials
        tabletMaterial = CreateMaterial("Tablet", new Color(0.15f, 0.15f, 0.18f));
        tabletScreenMaterial = CreateEmissiveMaterial("TabletScreen", new Color(0.2f, 0.6f, 0.9f), 0.5f);
        
        materials["StoneBrick"] = stoneBrickMaterial;
        materials["Wood"] = woodMaterial;
        materials["DarkWood"] = darkWoodMaterial;
        materials["Glass"] = glassMaterial;
        materials["Book"] = bookMaterial;
        materials["Carpet"] = carpetMaterial;
        materials["Tablet"] = tabletMaterial;
        materials["TabletScreen"] = tabletScreenMaterial;
        
        // New Materials
        leafMaterial = CreateMaterial("Leaf", new Color(0.2f, 0.6f, 0.2f));
        cobblestoneMaterial = CreateMaterial("Cobblestone", new Color(0.4f, 0.4f, 0.45f));
        materials["Leaf"] = leafMaterial;
        materials["Cobblestone"] = cobblestoneMaterial;
    }
    
    Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Diffuse");
        
        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;
        return mat;
    }
    
    Material CreateTransparentMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Diffuse");
        
        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        return mat;
    }
    
    Material CreateEmissiveMaterial(string name, Color color, float intensity)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Diffuse");
        
        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * intensity);
        return mat;
    }
    
    #endregion
    
    #region PLAYER SETUP
    
    void CreatePlayer()
    {
        player = new GameObject("Player");
        player.transform.position = new Vector3(0, 0.5f, -3f); // Start at entrance
        
        playerController = player.AddComponent<CharacterController>();
        playerController.height = 1.8f;
        playerController.radius = 0.3f;
        playerController.center = new Vector3(0, 0.9f, 0);
        
        // Camera
        GameObject cameraObj = new GameObject("MainCamera");
        cameraObj.transform.SetParent(player.transform);
        cameraObj.transform.localPosition = new Vector3(0, 1.6f, 0);
        cameraObj.tag = "MainCamera";
        
        mainCamera = cameraObj.AddComponent<Camera>();
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 100f;
        mainCamera.fieldOfView = 70f;
        mainCamera.backgroundColor = new Color(0.02f, 0.02f, 0.05f); // Dark night sky
        
        cameraObj.AddComponent<AudioListener>();
    }
    
    #endregion
    
    #region LIGHTING & SKYBOX
    
    void CreateLighting()
    {
        // Main directional light (moonlight for night scene)
        GameObject moonObj = new GameObject("Moon");
        Light moon = moonObj.AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = moonColor;
        moon.intensity = 0.3f; // Dim moonlight
        moon.shadows = LightShadows.Soft;
        moonObj.transform.rotation = Quaternion.Euler(30f, 150f, 0f); // Moon angle
        
        // Dark ambient lighting for night
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
        
        // Interior lights are now provided by torches instead
        // Removed - torches will provide the warm lighting
    }
    
    void CreatePointLight(Vector3 position, Color color, float range, float intensity)
    {
        GameObject lightObj = new GameObject("Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        light.shadows = LightShadows.Soft;
        lightObj.transform.position = position;
    }
    
    void CreateSkybox()
    {
        // Create a night sky material
        Material skyMat = new Material(Shader.Find("Skybox/Procedural"));
        if (skyMat.shader != null)
        {
            skyMat.SetFloat("_SunSize", 0.02f); // Smaller moon
            skyMat.SetFloat("_SunSizeConvergence", 10f);
            skyMat.SetFloat("_AtmosphereThickness", 0.3f); // Thin atmosphere for night
            skyMat.SetFloat("_Exposure", 0.3f); // Dark exposure for night
            skyMat.SetColor("_SkyTint", new Color(0.05f, 0.05f, 0.15f)); // Deep dark blue night sky
            skyMat.SetColor("_GroundColor", new Color(0.02f, 0.02f, 0.05f)); // Very dark ground reflection
            RenderSettings.skybox = skyMat;
        }
        
        // Set dark fog for atmospheric night effect
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.015f;
        RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.05f); // Dark blue fog
    }
    
    #endregion
    
    #region VOXEL LIBRARY CONSTRUCTION
    
    void CreateVoxelLibrary()
    {
        GameObject library = new GameObject("VoxelLibrary");
        
        // Library dimensions (in voxels)
        int width = 48;  // ~12 meters
        int depth = 56;  // ~14 meters  
        int height = 32; // ~8 meters (2 floors)
        int floorHeight = 16; // 4 meters per floor
        
        Debug.Log($"Building voxel library: {width}x{depth}x{height} voxels");
        
        // Create parent objects for organization
        GameObject foundation = new GameObject("Foundation");
        foundation.transform.SetParent(library.transform);
        
        GameObject walls = new GameObject("Walls");
        walls.transform.SetParent(library.transform);
        
        GameObject floors = new GameObject("Floors");
        floors.transform.SetParent(library.transform);
        
        GameObject furniture = new GameObject("Furniture");
        furniture.transform.SetParent(library.transform);
        
        // Build foundation and ground floor (Full)
        BuildFloorRect(floors.transform, 0, 0, width, 0, depth, woodMaterial);
        
        // Build walls
        BuildWalls(walls.transform, width, depth, height);
        
        // Build second floor (Atrium Layout)
        // Left Balcony
        BuildFloorRect(floors.transform, floorHeight, 0, 10, 0, depth, woodMaterial);
        // Right Balcony
        BuildFloorRect(floors.transform, floorHeight, width - 10, width, 0, depth, woodMaterial);
        // Back Balcony (wide enough to catch stairs)
        BuildFloorRect(floors.transform, floorHeight, 10, width - 10, depth - 12, depth, woodMaterial);
        // Front Balcony
        BuildFloorRect(floors.transform, floorHeight, 10, width - 10, 0, 10, woodMaterial);
        
        // Build stairs (Grand central staircase to back balcony)
        BuildStairs(furniture.transform);
        
        // Build bookshelves
        BuildBookshelves(furniture.transform);
        
        // Build reading alcoves
        BuildReadingAlcoves(furniture.transform);
        
        // Build decorative elements
        BuildDecorations(furniture.transform);
        
        // Build windows
        BuildWindows(walls.transform, width, depth, height);
        
        // Add ceiling beams
        BuildCeilingBeams(furniture.transform, width, depth, height);
        
        // Add carpet areas
        BuildCarpets(furniture.transform);
    }
    
    void BuildFloorRect(Transform parent, int y, int startX, int endX, int startZ, int endZ, Material mat)
    {
        GameObject floorObj = new GameObject($"Floor_Y{y}_{startX}_{startZ}");
        floorObj.transform.SetParent(parent);
        
        List<CombineInstance> combines = new List<CombineInstance>();
        Mesh cubeMesh = CreateCubeMesh();
        
        int totalWidth = 48;
        int totalDepth = 56;
        
        for (int x = startX; x < endX; x++)
        {
            for (int z = startZ; z < endZ; z++)
            {
                Matrix4x4 matrix = Matrix4x4.TRS(
                    new Vector3((x - totalWidth/2) * voxelSize, y * voxelSize, (z - totalDepth/2) * voxelSize),
                    Quaternion.identity,
                    Vector3.one * voxelSize * 0.98f
                );
                
                CombineInstance ci = new CombineInstance();
                ci.mesh = cubeMesh;
                ci.transform = matrix;
                combines.Add(ci);
            }
        }
        
        if (combines.Count > 0)
        {
            CreateCombinedMesh(floorObj, combines, mat);
        }
    }

    // Deprecated but kept for compatibility if needed by other methods
    void BuildFloor(Transform parent, int y, int width, int depth, Material mat, int offsetX = 0, int offsetZ = 0)
    {
        BuildFloorRect(parent, y, 0, width, 0, depth, mat);
    }
    
    void BuildWalls(Transform parent, int width, int depth, int height)
    {
        GameObject wallsObj = new GameObject("WallMesh");
        wallsObj.transform.SetParent(parent);
        
        List<CombineInstance> combines = new List<CombineInstance>();
        Mesh cubeMesh = CreateCubeMesh();
        
        int wallThickness = 2;
        
        // Front wall (with entrance gap)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Skip entrance area
                if (x >= width/2 - 4 && x <= width/2 + 4 && y < 10) continue;
                
                for (int t = 0; t < wallThickness; t++)
                {
                    AddVoxelToCombine(combines, cubeMesh, 
                        x - width/2, y, -depth/2 + t, voxelSize);
                }
            }
        }
        
        // Back wall
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int t = 0; t < wallThickness; t++)
                {
                    AddVoxelToCombine(combines, cubeMesh,
                        x - width/2, y, depth/2 - t, voxelSize);
                }
            }
        }
        
        // Left wall
        for (int z = 0; z < depth; z++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int t = 0; t < wallThickness; t++)
                {
                    AddVoxelToCombine(combines, cubeMesh,
                        -width/2 + t, y, z - depth/2, voxelSize);
                }
            }
        }
        
        // Right wall
        for (int z = 0; z < depth; z++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int t = 0; t < wallThickness; t++)
                {
                    AddVoxelToCombine(combines, cubeMesh,
                        width/2 - t, y, z - depth/2, voxelSize);
                }
            }
        }
        
        // Roof
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                AddVoxelToCombine(combines, cubeMesh,
                    x - width/2, height, z - depth/2, voxelSize);
            }
        }
        
        CreateCombinedMesh(wallsObj, combines, stoneBrickMaterial);
    }
    
    void BuildStairs(Transform parent)
    {
        GameObject stairsObj = new GameObject("Stairs");
        stairsObj.transform.SetParent(parent);
        
        List<CombineInstance> combines = new List<CombineInstance>();
        Mesh cubeMesh = CreateCubeMesh();
        
        int stairWidth = 10; // Wider stairs
        int stairHeight = 16;
        
        // Central grand staircase leading to back balcony
        // Start Z so top lands at Z=44 (start of back balcony which is depth-12 = 44)
        // 16 steps * 2 depth = 32 voxels long.
        // End at Z=44. Start at Z = 44 - 32 = 12.
        
        float startX = -stairWidth/2 * voxelSize;
        float startZ = 12 * voxelSize - (56/2 * voxelSize); // relative to center (-28)
        // startZ index = 12. Z range is 0 to 56. Center is 28.
        // World Z = (12 - 28) * 0.25 = -4.0
        
        for (int step = 0; step < stairHeight; step++)
        {
            for (int x = 0; x < stairWidth; x++)
            {
                for (int d = 0; d < 2; d++) // Each step is 2 voxels deep
                {
                    // Position:
                    // X: Centered
                    // Y: step height
                    // Z: Start + step*2 + d
                    
                    int zIndex = 12 + (step * 2) + d;
                    
                    AddVoxelToCombine(combines, cubeMesh, 
                        x - stairWidth/2, step, zIndex - 28, voxelSize);
                }
            }
            
            // Side railings
            for (int r = 0; r < 2; r++)
            {
                int zIndex = 12 + (step * 2);
                // Left railing
                AddVoxelToCombine(combines, cubeMesh, -stairWidth/2 - 1, step + 1, zIndex - 28, voxelSize);
                AddVoxelToCombine(combines, cubeMesh, -stairWidth/2 - 1, step + 2, zIndex - 28, voxelSize);
                
                // Right railing
                AddVoxelToCombine(combines, cubeMesh, stairWidth/2, step + 1, zIndex - 28, voxelSize);
                AddVoxelToCombine(combines, cubeMesh, stairWidth/2, step + 2, zIndex - 28, voxelSize);
            }
        }
        
        CreateCombinedMesh(stairsObj, combines, darkWoodMaterial);
    }
    
    void BuildBookshelves(Transform parent)
    {
        // Ground floor bookshelves
        CreateBookshelf(parent, new Vector3(-4f, 0, 4f), 0);
        CreateBookshelf(parent, new Vector3(-4f, 0, 2f), 0);
        CreateBookshelf(parent, new Vector3(4f, 0, 4f), 0);
        CreateBookshelf(parent, new Vector3(4f, 0, 2f), 0);
        
        // Back wall bookshelves
        CreateBookshelf(parent, new Vector3(-3f, 0, 5.5f), 90);
        CreateBookshelf(parent, new Vector3(0f, 0, 5.5f), 90);
        CreateBookshelf(parent, new Vector3(3f, 0, 5.5f), 90);
        
        // Second floor bookshelves
        CreateBookshelf(parent, new Vector3(-3.5f, 4f, 4f), 0);
        CreateBookshelf(parent, new Vector3(-3.5f, 4f, 2f), 0);
        CreateBookshelf(parent, new Vector3(3.5f, 4f, 4f), 0);
        CreateBookshelf(parent, new Vector3(3.5f, 4f, 2f), 0);
    }
    
    void CreateBookshelf(Transform parent, Vector3 position, float rotation)
    {
        GameObject shelf = new GameObject("Bookshelf");
        shelf.transform.SetParent(parent);
        shelf.transform.position = position;
        shelf.transform.rotation = Quaternion.Euler(0, rotation, 0);
        
        List<CombineInstance> shelfCombines = new List<CombineInstance>();
        List<CombineInstance> bookCombines = new List<CombineInstance>();
        Mesh cubeMesh = CreateCubeMesh();
        
        int shelfWidth = 8;
        int shelfHeight = 12;
        int shelfDepth = 2;
        
        // Shelf frame
        for (int x = 0; x < shelfWidth; x++)
        {
            for (int y = 0; y < shelfHeight; y++)
            {
                // Back panel
                AddVoxelToCombine(shelfCombines, cubeMesh, x, y, 0, voxelSize);
                
                // Sides
                if (x == 0 || x == shelfWidth - 1)
                {
                    for (int z = 1; z < shelfDepth; z++)
                    {
                        AddVoxelToCombine(shelfCombines, cubeMesh, x, y, z, voxelSize);
                    }
                }
                
                // Shelves (every 3 voxels)
                if (y % 3 == 0)
                {
                    for (int z = 1; z < shelfDepth; z++)
                    {
                        AddVoxelToCombine(shelfCombines, cubeMesh, x, y, z, voxelSize);
                    }
                }
            }
        }
        
        // Books on shelves
        Color[] bookColors = {
            new Color(0.6f, 0.2f, 0.15f),
            new Color(0.15f, 0.3f, 0.5f),
            new Color(0.2f, 0.4f, 0.2f),
            new Color(0.5f, 0.35f, 0.2f),
            new Color(0.4f, 0.15f, 0.35f)
        };
        
        for (int shelfY = 1; shelfY < shelfHeight - 1; shelfY += 3)
        {
            for (int x = 1; x < shelfWidth - 1; x++)
            {
                if (Random.value > 0.15f) // 85% chance of book
                {
                    int bookHeight = Random.Range(2, 3);
                    for (int h = 0; h < bookHeight; h++)
                    {
                        AddVoxelToCombine(bookCombines, cubeMesh, x, shelfY + h, 1, voxelSize * 0.9f);
                    }
                }
            }
        }
        
        // Create shelf mesh
        GameObject shelfMesh = new GameObject("ShelfFrame");
        shelfMesh.transform.SetParent(shelf.transform);
        shelfMesh.transform.localPosition = Vector3.zero;
        CreateCombinedMesh(shelfMesh, shelfCombines, darkWoodMaterial);
        
        // Create books mesh
        if (bookCombines.Count > 0)
        {
            GameObject booksMesh = new GameObject("Books");
            booksMesh.transform.SetParent(shelf.transform);
            booksMesh.transform.localPosition = Vector3.zero;
            CreateCombinedMesh(booksMesh, bookCombines, bookMaterial);
        }
    }
    
    void BuildReadingAlcoves(Transform parent)
    {
        // Ground floor alcoves (left and right)
        CreateReadingAlcove(parent, new Vector3(-4.5f, 0, -2f), "Left Ground Alcove");
        CreateReadingAlcove(parent, new Vector3(4.5f, 0, -2f), "Right Ground Alcove");
        
        // Second floor alcoves
        CreateReadingAlcove(parent, new Vector3(-3.5f, 4f, -1f), "Left Upper Alcove");
        CreateReadingAlcove(parent, new Vector3(3.5f, 4f, -1f), "Right Upper Alcove");
    }
    
    void CreateReadingAlcove(Transform parent, Vector3 position, string name)
    {
        GameObject alcove = new GameObject(name);
        alcove.transform.SetParent(parent);
        alcove.transform.position = position;
        
        List<CombineInstance> combines = new List<CombineInstance>();
        Mesh cubeMesh = CreateCubeMesh();
        
        // Reading desk
        for (int x = 0; x < 6; x++)
        {
            for (int z = 0; z < 4; z++)
            {
                AddVoxelToCombine(combines, cubeMesh, x - 3, 3, z - 2, voxelSize);
            }
        }
        
        // Desk legs
        AddVoxelToCombine(combines, cubeMesh, -2, 0, -1, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, -2, 1, -1, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, -2, 2, -1, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, 2, 0, -1, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, 2, 1, -1, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, 2, 2, -1, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, -2, 0, 1, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, -2, 1, 1, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, -2, 2, 1, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, 2, 0, 1, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, 2, 1, 1, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, 2, 2, 1, voxelSize);
        
        // Chair
        for (int x = 0; x < 3; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                AddVoxelToCombine(combines, cubeMesh, x - 1, 2, z - 4, voxelSize);
            }
        }
        // Chair back
        for (int x = 0; x < 3; x++)
        {
            for (int y = 3; y < 6; y++)
            {
                AddVoxelToCombine(combines, cubeMesh, x - 1, y, -5, voxelSize);
            }
        }
        // Chair legs
        AddVoxelToCombine(combines, cubeMesh, -1, 0, -4, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, -1, 1, -4, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, 1, 0, -4, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, 1, 1, -4, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, -1, 0, -2, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, -1, 1, -2, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, 1, 0, -2, voxelSize);
        AddVoxelToCombine(combines, cubeMesh, 1, 1, -2, voxelSize);
        
        CreateCombinedMesh(alcove, combines, woodMaterial);
    }
    
    void BuildDecorations(Transform parent)
    {
        // Hanging lanterns
        CreateLantern(parent, new Vector3(-3f, 3.2f, 0));
        CreateLantern(parent, new Vector3(3f, 3.2f, 0));
        CreateLantern(parent, new Vector3(0, 3.2f, 4f));
        
        // Second floor lanterns
        CreateLantern(parent, new Vector3(-2.5f, 7.2f, 2f));
        CreateLantern(parent, new Vector3(2.5f, 7.2f, 2f));
        
        // Potted plants
        CreatePlant(parent, new Vector3(-5f, 0, -4f));
        CreatePlant(parent, new Vector3(5f, 0, -4f));
        CreatePlant(parent, new Vector3(-4f, 4f, 5f));
        CreatePlant(parent, new Vector3(4f, 4f, 5f));
    }
    
    void CreateLantern(Transform parent, Vector3 position)
    {
        GameObject lantern = new GameObject("Lantern");
        lantern.transform.SetParent(parent);
        lantern.transform.position = position;
        
        // Lantern frame
        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.transform.SetParent(lantern.transform);
        frame.transform.localPosition = Vector3.zero;
        frame.transform.localScale = new Vector3(0.3f, 0.4f, 0.3f);
        frame.GetComponent<Renderer>().material = darkWoodMaterial;
        Destroy(frame.GetComponent<Collider>());
        
        // Light inside
        GameObject lightObj = new GameObject("LanternLight");
        lightObj.transform.SetParent(lantern.transform);
        lightObj.transform.localPosition = Vector3.zero;
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.8f, 0.5f);
        light.range = 4f;
        light.intensity = 1f;
    }
    
    void CreatePlant(Transform parent, Vector3 position)
    {
        GameObject plant = new GameObject("Plant");
        plant.transform.SetParent(parent);
        plant.transform.position = position;
        
        // Pot
        GameObject pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pot.transform.SetParent(plant.transform);
        pot.transform.localPosition = new Vector3(0, 0.2f, 0);
        pot.transform.localScale = new Vector3(0.4f, 0.2f, 0.4f);
        pot.GetComponent<Renderer>().material = CreateMaterial("Terracotta", new Color(0.7f, 0.4f, 0.3f));
        Destroy(pot.GetComponent<Collider>());
        
        // Leaves (simple representation)
        Material leafMat = CreateMaterial("Leaf", new Color(0.2f, 0.5f, 0.2f));
        for (int i = 0; i < 5; i++)
        {
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.transform.SetParent(plant.transform);
            float angle = i * 72f * Mathf.Deg2Rad;
            leaf.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.15f, 0.5f + i * 0.1f, Mathf.Sin(angle) * 0.15f);
            leaf.transform.localScale = new Vector3(0.15f, 0.3f, 0.05f);
            leaf.transform.rotation = Quaternion.Euler(Random.Range(-20, 20), i * 72f, Random.Range(-10, 10));
            leaf.GetComponent<Renderer>().material = leafMat;
            Destroy(leaf.GetComponent<Collider>());
        }
    }
    
    void BuildWindows(Transform parent, int width, int depth, int height)
    {
        // Create window openings with glass
        // Left wall windows
        CreateWindow(parent, new Vector3(-width/2 * voxelSize - 0.1f, 2f, 0), 90);
        CreateWindow(parent, new Vector3(-width/2 * voxelSize - 0.1f, 2f, 3f), 90);
        CreateWindow(parent, new Vector3(-width/2 * voxelSize - 0.1f, 6f, 0), 90);
        CreateWindow(parent, new Vector3(-width/2 * voxelSize - 0.1f, 6f, 3f), 90);
        
        // Right wall windows
        CreateWindow(parent, new Vector3(width/2 * voxelSize + 0.1f, 2f, 0), -90);
        CreateWindow(parent, new Vector3(width/2 * voxelSize + 0.1f, 2f, 3f), -90);
        CreateWindow(parent, new Vector3(width/2 * voxelSize + 0.1f, 6f, 0), -90);
        CreateWindow(parent, new Vector3(width/2 * voxelSize + 0.1f, 6f, 3f), -90);
        
        // Back wall windows
        CreateWindow(parent, new Vector3(-2f, 2f, depth/2 * voxelSize + 0.1f), 0);
        CreateWindow(parent, new Vector3(2f, 2f, depth/2 * voxelSize + 0.1f), 0);
        CreateWindow(parent, new Vector3(-2f, 6f, depth/2 * voxelSize + 0.1f), 0);
        CreateWindow(parent, new Vector3(2f, 6f, depth/2 * voxelSize + 0.1f), 0);
    }
    
    void CreateWindow(Transform parent, Vector3 position, float rotation)
    {
        GameObject window = new GameObject("Window");
        window.transform.SetParent(parent);
        window.transform.position = position;
        window.transform.rotation = Quaternion.Euler(0, rotation, 0);
        
        // Glass pane
        GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Quad);
        glass.transform.SetParent(window.transform);
        glass.transform.localPosition = Vector3.zero;
        glass.transform.localScale = new Vector3(1.5f, 2f, 1f);
        glass.GetComponent<Renderer>().material = glassMaterial;
        Destroy(glass.GetComponent<Collider>());
        
        // Window frame
        Material frameMat = darkWoodMaterial;
        
        // Top frame
        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.transform.SetParent(window.transform);
        top.transform.localPosition = new Vector3(0, 1.05f, 0);
        top.transform.localScale = new Vector3(1.6f, 0.1f, 0.1f);
        top.GetComponent<Renderer>().material = frameMat;
        Destroy(top.GetComponent<Collider>());
        
        // Bottom frame
        GameObject bottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bottom.transform.SetParent(window.transform);
        bottom.transform.localPosition = new Vector3(0, -1.05f, 0);
        bottom.transform.localScale = new Vector3(1.6f, 0.1f, 0.1f);
        bottom.GetComponent<Renderer>().material = frameMat;
        Destroy(bottom.GetComponent<Collider>());
        
        // Left frame
        GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
        left.transform.SetParent(window.transform);
        left.transform.localPosition = new Vector3(-0.8f, 0, 0);
        left.transform.localScale = new Vector3(0.1f, 2.2f, 0.1f);
        left.GetComponent<Renderer>().material = frameMat;
        Destroy(left.GetComponent<Collider>());
        
        // Right frame
        GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
        right.transform.SetParent(window.transform);
        right.transform.localPosition = new Vector3(0.8f, 0, 0);
        right.transform.localScale = new Vector3(0.1f, 2.2f, 0.1f);
        right.GetComponent<Renderer>().material = frameMat;
        Destroy(right.GetComponent<Collider>());
    }
    
    void BuildCeilingBeams(Transform parent, int width, int depth, int height)
    {
        GameObject beams = new GameObject("CeilingBeams");
        beams.transform.SetParent(parent);
        
        Material beamMat = darkWoodMaterial;
        
        // First floor beams
        for (int i = -2; i <= 2; i++)
        {
            CreateBeam(beams.transform, new Vector3(i * 2f, 3.8f, 0), new Vector3(0.2f, 0.3f, depth * voxelSize - 1f), beamMat);
        }
        
        // Second floor beams
        for (int i = -2; i <= 2; i++)
        {
            CreateBeam(beams.transform, new Vector3(i * 1.8f, 7.8f, 1f), new Vector3(0.2f, 0.3f, (depth - 8) * voxelSize - 1f), beamMat);
        }
    }
    
    void CreateBeam(Transform parent, Vector3 position, Vector3 scale, Material mat)
    {
        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beam.transform.SetParent(parent);
        beam.transform.position = position;
        beam.transform.localScale = scale;
        beam.GetComponent<Renderer>().material = mat;
        Destroy(beam.GetComponent<Collider>());
    }
    
    void BuildCarpets(Transform parent)
    {
        // Main floor carpet
        CreateCarpet(parent, new Vector3(0, 0.01f, -1f), new Vector3(4f, 0.02f, 3f));
        
        // Reading area carpets
        CreateCarpet(parent, new Vector3(-4.5f, 0.01f, -2f), new Vector3(2f, 0.02f, 2f));
        CreateCarpet(parent, new Vector3(4.5f, 0.01f, -2f), new Vector3(2f, 0.02f, 2f));
        
        // Second floor carpets
        CreateCarpet(parent, new Vector3(-3.5f, 4.01f, -1f), new Vector3(2f, 0.02f, 2f));
        CreateCarpet(parent, new Vector3(3.5f, 4.01f, -1f), new Vector3(2f, 0.02f, 2f));
    }
    
    void CreateCarpet(Transform parent, Vector3 position, Vector3 scale)
    {
        GameObject carpet = GameObject.CreatePrimitive(PrimitiveType.Cube);
        carpet.name = "Carpet";
        carpet.transform.SetParent(parent);
        carpet.transform.position = position;
        carpet.transform.localScale = scale;
        carpet.GetComponent<Renderer>().material = carpetMaterial;
        Destroy(carpet.GetComponent<Collider>());
    }
    
    #endregion
    
    #region TV SCREEN CREATION
    
    // Store loaded HTML content for each paper
    private string[] loadedPaperContents = new string[4];
    private bool[] paperContentsLoaded = new bool[4];
    
    void CreateTVScreens()
    {
        // Paper titles (extended for 16 screens)
        string[] paperTitles = {
            "Neural Networks & Deep Learning",
            "Climate Change Data Analysis", 
            "Quantum Computing Fundamentals",
            "Human-Computer Interaction",
            "Artificial Intelligence Ethics",
            "Virtual Reality Systems",
            "Blockchain Technology",
            "Cybersecurity Protocols",
            "Big Data Analytics",
            "Cloud Computing Architecture",
            "Internet of Things (IoT)",
            "Augmented Reality Apps",
            "Machine Learning Algorithms",
            "Robotics Process Automation",
            "Digital Twin Technology",
            "5G Network Infrastructure"
        };
        
        // Create TV screens with loading placeholder, then load actual HTML content
        for (int i = 0; i < 16; i++)
        {
            // Use modulo for titles if we run out, but we defined 16 above
            string title = (i < paperTitles.Length) ? paperTitles[i] : "Digital Library Screen " + (i+1);
            
            if (i < 4)
            {
                // Original 4 screens load from files
                loadedPaperContents[i] = "<b>Loading...</b>\n\nPlease wait while content loads from HTML file...";
                tvScreens[i] = CreatePermanentTVScreen(i, title, loadedPaperContents[i]);
                StartCoroutine(LoadHTMLContent(i, title));
            }
            else
            {
                // New screens get random generated content
                string randomContent = GenerateRandomResearchContent(title);
                tvScreens[i] = CreatePermanentTVScreen(i, title, randomContent);
            }
        }
    }

    string GenerateRandomResearchContent(string title)
    {
        string[] buzzwords = { "quantum", "neural", "cybernetic", "algorithm", "data", "synthesis", "matrix", "protocol", "interface", "simulation", "heuristic", "cognitive" };
        string[] verbs = { "analyzing", "processing", "optimizing", "generating", "simulating", "encrypting", "decoding", "restructuring" };
        string[] adjectives = { "robust", "scalable", "dynamic", "integrated", "virtual", "augmented", "distributed" };
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"<b><size=18>{title}</size></b>");
        sb.AppendLine("<color=#2d3748>━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>\n");
        sb.AppendLine("<b>ABSTRACT</b>\n");
        sb.Append("This research explores the potential of ");
        sb.Append(adjectives[Random.Range(0, adjectives.Length)]);
        sb.Append(" ");
        sb.Append(buzzwords[Random.Range(0, buzzwords.Length)]);
        sb.Append(" systems in modern computing architectures. We propose a new method for ");
        sb.Append(verbs[Random.Range(0, verbs.Length)]);
        sb.Append(" complex data structures using ");
        sb.Append(buzzwords[Random.Range(0, buzzwords.Length)]);
        sb.AppendLine(" logic.\n");
        
        sb.AppendLine("<b>KEY FINDINGS</b>\n");
        for(int i=0; i<4; i++) {
            sb.Append("• ");
            sb.Append(char.ToUpper(verbs[Random.Range(0, verbs.Length)][0]) + verbs[Random.Range(0, verbs.Length)].Substring(1));
            sb.Append(" ");
            sb.Append(buzzwords[Random.Range(0, buzzwords.Length)]);
            sb.AppendLine(" efficiency increased by " + Random.Range(15, 300) + "%.");
        }
        
        sb.AppendLine("\n<b>CONCLUSION</b>\n");
        sb.AppendLine("The results indicate a significant paradigm shift in how we approach digital information systems, paving the way for future innovations in the field.");
        
        sb.AppendLine("\n\n<color=#64748b><i>↑↓ to scroll • X to close</i></color>");
        
        return sb.ToString();
    }
    
    IEnumerator LoadHTMLContent(int index, string title)
    {
        // Load clean .txt file from StreamingAssets (much better formatting than HTML parsing)
        string fileName = $"Paper{index + 1}.txt";
        string filePath;
        
        #if UNITY_EDITOR || UNITY_STANDALONE
        filePath = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
        filePath = "file:///" + filePath.Replace("\\", "/");
        #elif UNITY_WEBGL
        filePath = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
        #else
        filePath = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
        #endif
        
        Debug.Log($"Loading content from: {filePath}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(filePath))
        {
            yield return request.SendWebRequest();
            
            // Unity 2019 compatibility: use isNetworkError/isHttpError instead of result
            #if UNITY_2020_1_OR_NEWER
            bool success = request.result == UnityWebRequest.Result.Success;
            #else
            bool success = !request.isNetworkError && !request.isHttpError;
            #endif
            
            if (success)
            {
                // Use text content directly - no HTML parsing needed!
                string textContent = request.downloadHandler.text;
                loadedPaperContents[index] = textContent;
                paperContentsLoaded[index] = true;
                
                // Update the TV screen content
                UpdateTVScreenContent(index, textContent);
                
                Debug.Log($"Successfully loaded Paper{index + 1}.txt ({textContent.Length} chars)");
            }
            else
            {
                Debug.LogError($"Failed to load {fileName}: {request.error}");
                loadedPaperContents[index] = $"<b><color=#ff0000>Error Loading Content</color></b>\n\nFailed to load {fileName}:\n{request.error}";
                UpdateTVScreenContent(index, loadedPaperContents[index]);
            }
        }
    }
    
    string ParseHTMLToRichText(string html, string title)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        
        // Add title header
        result.AppendLine($"<b><size=18>{title}</size></b>");
        result.AppendLine("<color=#2d3748>━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>\n");
        
        // --- 1. REMOVE NON-TEXT ELEMENTS COMPLETELY ---
        
        // Remove SVG elements
        html = Regex.Replace(html, @"<svg[^>]*>[\s\S]*?</svg>", "[DIAGRAM]", RegexOptions.IgnoreCase);
        
        // Remove script and style sections
        html = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<head[^>]*>[\s\S]*?</head>", "", RegexOptions.IgnoreCase);
        
        // Extract body content
        Match bodyMatch = Regex.Match(html, @"<body[^>]*>([\s\S]*)</body>", RegexOptions.IgnoreCase);
        if (bodyMatch.Success)
        {
            html = bodyMatch.Groups[1].Value;
        }
        
        // --- 2. CONVERT STRUCTURE TAGS TO SPACING ---
        
        // Headers
        html = Regex.Replace(html, @"<h1[^>]*>(.*?)</h1>", "\n<b><size=16>$1</size></b>\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<h2[^>]*>(.*?)</h2>", "\n<b><size=15>$1</size></b>\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<h3[^>]*>(.*?)</h3>", "\n<b>$1</b>\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<h4[^>]*>(.*?)</h4>", "\n<b>$1</b>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        // Paragraphs and divs - simple newlines
        html = Regex.Replace(html, @"<p[^>]*>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</p>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<div[^>]*>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</div>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<section[^>]*>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</section>", "\n", RegexOptions.IgnoreCase);
        
        // Lists
        html = Regex.Replace(html, @"<ul[^>]*>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</ul>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<ol[^>]*>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</ol>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<li[^>]*>", "• ", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</li>", "\n", RegexOptions.IgnoreCase);
        
        // Line breaks and Rules
        html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<hr[^>]*>", "\n<color=#2d3748>━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>\n", RegexOptions.IgnoreCase);
        
        // Tables - handle minimal table structure
        html = Regex.Replace(html, @"<table[^>]*>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</table>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<tr[^>]*>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</tr>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<t[hd][^>]*>", "  ", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</t[hd]>", "  |", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<thead[^>]*>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</thead>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<tbody[^>]*>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</tbody>", "", RegexOptions.IgnoreCase);
        
        // --- 3. PRESERVE FORMATTING ---
        
        // Bold/Strong
        html = Regex.Replace(html, @"<strong[^>]*>(.*?)</strong>", "<b>$1</b>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<b[^>]*>(.*?)</b>", "<b>$1</b>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        // Italic/Em
        html = Regex.Replace(html, @"<em[^>]*>(.*?)</em>", "<i>$1</i>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<i[^>]*>(.*?)</i>", "<i>$1</i>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        // Spans with colors (simplified)
        html = Regex.Replace(html, @"<span[^>]*color:\s*#([0-9a-fA-F]{6})[^""]*""[^>]*>(.*?)</span>", "<color=#$1>$2</color>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        // Handle specific classes from your CSS
        html = Regex.Replace(html, @"<span[^>]*class=""[^""]*primary[^""]*""[^>]*>(.*?)</span>", "<color=#2563eb>$1</color>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<span[^>]*class=""[^""]*secondary[^""]*""[^>]*>(.*?)</span>", "<color=#0891b2>$1</color>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<span[^>]*class=""[^""]*accent[^""]*""[^>]*>(.*?)</span>", "<color=#8b5cf6>$1</color>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<span[^>]*class=""[^""]*warning[^""]*""[^>]*>(.*?)</span>", "<color=#d97706>$1</color>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        // Links - convert to blue text
        html = Regex.Replace(html, @"<a[^>]*>(.*?)</a>", "<color=#0066cc>$1</color>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        // --- 4. CLEANUP ---
        
        // Remove ANY remaining HTML tags
        html = Regex.Replace(html, @"<[^>]+>", "", RegexOptions.IgnoreCase);
        
        // Decode HTML entities (&amp; -> &, &lt; -> <, etc)
        html = System.Net.WebUtility.HtmlDecode(html);
        
        // Clean up whitespace
        // Replace multiple spaces/tabs with single space
        html = Regex.Replace(html, @"[ \t]+", " "); 
        
        // Fix spacing around bullets
        html = html.Replace("\n •", "\n•");
        html = html.Replace("• ", "\n• ");
        
        // Limit consecutive newlines to 2
        html = Regex.Replace(html, @"\n\s*\n\s*\n", "\n\n");
        
        // Trim start/end
        html = html.Trim();
        
        result.Append(html);
        
        // Add footer
        result.AppendLine("\n\n<color=#64748b><i>↑↓ to scroll • X to close</i></color>");
        
        return result.ToString();
    }
    
    void UpdateTVScreenContent(int index, string content)
    {
        if (tvScreens[index] == null) return;
        
        // Find the content text in the TV screen hierarchy: ScrollView/Viewport/Content/Text
        Transform scrollView = tvScreens[index].transform.Find("ScrollView");
        if (scrollView == null) { Debug.LogError($"TV {index}: ScrollView not found"); return; }
        
        Transform viewport = scrollView.Find("Viewport");
        if (viewport == null) { Debug.LogError($"TV {index}: Viewport not found"); return; }
        
        Transform contentTransform = viewport.Find("Content");
        if (contentTransform == null) { Debug.LogError($"TV {index}: Content not found"); return; }
        
        Transform textTransform = contentTransform.Find("Text");
        if (textTransform == null) { Debug.LogError($"TV {index}: Text not found"); return; }
        
        Text textComponent = textTransform.GetComponent<Text>();
        if (textComponent == null) { Debug.LogError($"TV {index}: Text component not found"); return; }
        
        // Update the text
        textComponent.text = content;
        
        // Force layout rebuild
        Canvas.ForceUpdateCanvases();
        
        RectTransform textRect = textTransform.GetComponent<RectTransform>();
        RectTransform contentRect = contentTransform.GetComponent<RectTransform>();
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);
        
        // Update content container size based on new text
        float textHeight = textComponent.preferredHeight;
        contentRect.sizeDelta = new Vector2(0, textHeight + 20);
        
        // Update stored content height
        contentContainers[index] = contentRect;
        contentHeights[index] = textHeight + 20;
        contentScrollPositions[index] = 0;
        contentRect.anchoredPosition = Vector2.zero;
        
        Debug.Log($"Updated TV {index} content: text height = {textHeight}");
    }
    
    IEnumerator UpdateContentHeightDelayed(int index)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        if (contentContainers[index] != null)
        {
            float newHeight = contentContainers[index].rect.height;
            if (newHeight > 0)
            {
                contentHeights[index] = newHeight;
                contentScrollPositions[index] = 0; // Reset scroll to top
                contentContainers[index].anchoredPosition = Vector2.zero;
                Debug.Log($"TV {index}: Updated content height to {newHeight}");
            }
        }
    }
    
    GameObject CreatePermanentTVScreen(int tabletIndex, string title, string content)
    {
        // === SMALL TV SCREEN NEAR TABLET ===
        // Panel size: 600x450 pixels, scaled to ~0.6m x 0.45m
        
        GameObject tv = new GameObject($"TVScreen_{tabletIndex}");
        
        // Add the main canvas - ALL UI will be children of this
        Canvas mainCanvas = tv.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.WorldSpace;
        
        // Add CanvasScaler for proper World Space rendering
        CanvasScaler scaler = tv.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        
        RectTransform canvasRect = tv.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(600, 450); // 600x450 pixel screen
        
        // Position near the tablet
        tv.transform.position = tvPositions[tabletIndex];
        tv.transform.rotation = Quaternion.identity; // Facing forward (toward player)
        tv.transform.localScale = Vector3.one * TV_SCREEN_SCALE;
        
        // Add GraphicRaycaster for UI interaction
        tv.AddComponent<GraphicRaycaster>();
        
        // Add BoxCollider for gaze detection (so we can scroll without activation)
        BoxCollider tvCol = tv.AddComponent<BoxCollider>();
        tvCol.size = new Vector3(600, 450, 10);
        tvCol.center = new Vector3(0, 0, 5);
        
        // === TV HOUSING (black frame around screen) ===
        GameObject frameObj = new GameObject("TVFrame");
        frameObj.transform.SetParent(tv.transform, false);
        Image frameImage = frameObj.AddComponent<Image>();
        frameImage.color = new Color(0.05f, 0.05f, 0.08f);
        RectTransform frameRect = frameObj.GetComponent<RectTransform>();
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.sizeDelta = new Vector2(30, 30); // Frame border
        frameRect.anchoredPosition = Vector2.zero;
        
        // === BACKGROUND ===
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(tv.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.12f, 0.14f, 0.18f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        // === HEADER BAR (Title) ===
        GameObject headerObj = CreateUIElement(tv.transform, "Header", 
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(550, 40),
            new Color(0.2f, 0.4f, 0.7f), title, 18, false);
            
        // === CLOSE BUTTON (Hidden until active?) ===
        // Let's make it always visible but only interactive if gazed at
        closeButtons[tabletIndex] = CreateUIButton(tv.transform, "CloseButton",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-25, -20), new Vector2(40, 40),
            new Color(0.9f, 0.3f, 0.3f), "✕", 20);

        // === SCROLL UP BUTTON ===
        scrollUpButtons[tabletIndex] = CreateUIButton(tv.transform, "ScrollUpButton",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-25, 70), new Vector2(40, 60),
            new Color(0.3f, 0.7f, 0.4f), "▲", 22);
        
        // === SCROLL DOWN BUTTON ===
        scrollDownButtons[tabletIndex] = CreateUIButton(tv.transform, "ScrollDownButton",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-25, -70), new Vector2(40, 60),
            new Color(0.3f, 0.7f, 0.4f), "▼", 22);
        
        // === CONTENT AREA WITH SCROLL ===
        // Create scrollable content with proper masking
        CreateScrollableContentUI(tv.transform, content, tabletIndex); 
        
        // === FOOTER INSTRUCTIONS ===
        GameObject footer = new GameObject("Footer");
        footer.transform.SetParent(tv.transform, false);
        Text footerText = footer.AddComponent<Text>();
        footerText.text = "[↑↓] Scroll • [X] Close";
        footerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        footerText.fontSize = 12;
        footerText.color = new Color(0.5f, 0.5f, 0.55f);
        footerText.alignment = TextAnchor.MiddleCenter;
        RectTransform footerRect = footer.GetComponent<RectTransform>();
        footerRect.anchorMin = new Vector2(0, 0);
        footerRect.anchorMax = new Vector2(1, 0);
        footerRect.pivot = new Vector2(0.5f, 0);
        footerRect.anchoredPosition = new Vector2(0, 5);
        footerRect.sizeDelta = new Vector2(0, 25);
        
        Debug.Log($"Permanent TV screen created near tablet {tabletIndex} at position {tvPositions[tabletIndex]}");
        return tv;
    }
    
    #endregion
    
    #region TABLET CREATION
    
    void CreateTablets()
    {
        // Update tablet positions for new Atrium layout
        // Ground floor: Left and Right alcoves/corners
        // Second floor: Left and Right balconies (overlooking atrium)
        
        Vector3[] tabletPositions = new Vector3[] {
            new Vector3(-3.5f, 1.05f, -2f),  // Ground floor left
            new Vector3(3.5f, 1.05f, -2f),   // Ground floor right
            new Vector3(-3.5f, 5.05f, 2f),   // Second floor left (moved back)
            new Vector3(3.5f, 5.05f, 2f)     // Second floor right (moved back)
        };
        
        float[] tabletRotations = new float[] { 15f, -15f, 15f, -15f };
        
        string[] tabletLabels = new string[] {
            "Neural Networks\n& Deep Learning",
            "Climate Change\nData Analysis",
            "Quantum Computing\nFundamentals",
            "Human-Computer\nInteraction"
        };
        
        for (int i = 0; i < 4; i++)
        {
            tablets[i] = CreateTablet(tabletPositions[i], tabletRotations[i], i, tabletLabels[i]);
        }
    }
    
    GameObject CreateTablet(Vector3 position, float tiltAngle, int index, string label)
    {
        GameObject tablet = new GameObject($"Tablet_{index + 1}");
        tablet.transform.position = position;
        tablet.transform.rotation = Quaternion.Euler(tiltAngle, 0, 0);
        // Using TabletData component for identification instead of tags
        
        // Tablet body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "TabletBody";
        body.transform.SetParent(tablet.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.5f, 0.35f, 0.02f);
        body.GetComponent<Renderer>().material = tabletMaterial;
        
        // Add collider for gaze detection
        BoxCollider col = body.GetComponent<BoxCollider>();
        col.isTrigger = false;
        
        // Screen
        GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        screen.name = "TabletScreen";
        screen.transform.SetParent(tablet.transform);
        screen.transform.localPosition = new Vector3(0, 0, -0.015f);
        screen.transform.localScale = new Vector3(0.45f, 0.3f, 1f);
        
        // Create unique screen material for each tablet
        Material screenMat = new Material(tabletScreenMaterial);
        Color[] screenColors = {
            new Color(0.2f, 0.6f, 0.9f),
            new Color(0.3f, 0.8f, 0.4f),
            new Color(0.9f, 0.5f, 0.2f),
            new Color(0.7f, 0.3f, 0.8f)
        };
        screenMat.color = screenColors[index];
        screenMat.SetColor("_EmissionColor", screenColors[index] * 0.8f);
        screen.GetComponent<Renderer>().material = screenMat;
        Destroy(screen.GetComponent<Collider>());
        
        // Label text (using 3D text or simple indicator)
        CreateTabletLabel(tablet.transform, label, index);
        
        // Store tablet index
        TabletData data = tablet.AddComponent<TabletData>();
        data.tabletIndex = index;
        data.paperUrl = paperUrls[index];
        
        return tablet;
    }
    
    void CreateTabletLabel(Transform parent, string text, int index)
    {
        // Create a simple label indicator above the tablet
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(parent);
        labelObj.transform.localPosition = new Vector3(0, 0.25f, 0);
        
        // Small indicator sphere
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.transform.SetParent(labelObj.transform);
        indicator.transform.localPosition = Vector3.zero;
        indicator.transform.localScale = Vector3.one * 0.05f;
        
        Color[] colors = {
            new Color(0.2f, 0.6f, 0.9f),
            new Color(0.3f, 0.8f, 0.4f),
            new Color(0.9f, 0.5f, 0.2f),
            new Color(0.7f, 0.3f, 0.8f)
        };
        
        Material indicatorMat = CreateEmissiveMaterial($"Indicator{index}", colors[index], 2f);
        indicator.GetComponent<Renderer>().material = indicatorMat;
        Destroy(indicator.GetComponent<Collider>());
        
        // Add pulsing light
        GameObject lightObj = new GameObject("IndicatorLight");
        lightObj.transform.SetParent(labelObj.transform);
        lightObj.transform.localPosition = Vector3.zero;
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = colors[index];
        light.range = 1f;
        light.intensity = 0.5f;
    }
    
    #endregion
    
    #region UI CREATION
    
    void CreateGazeIndicator()
    {
        // Create canvas for gaze progress
        GameObject canvasObj = new GameObject("GazeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Gaze indicator (circular progress)
        gazeIndicator = new GameObject("GazeIndicator");
        gazeIndicator.transform.SetParent(canvasObj.transform);
        
        RectTransform rt = gazeIndicator.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(60, 60);
        rt.anchoredPosition = Vector2.zero;
        
        // Background circle
        GameObject bgCircle = new GameObject("Background");
        bgCircle.transform.SetParent(gazeIndicator.transform);
        RectTransform bgRt = bgCircle.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        bgRt.anchoredPosition = Vector2.zero;
        Image bgImg = bgCircle.AddComponent<Image>();
        bgImg.color = new Color(1, 1, 1, 0.3f);
        
        // Progress circle
        GameObject progressCircle = new GameObject("Progress");
        progressCircle.transform.SetParent(gazeIndicator.transform);
        RectTransform progRt = progressCircle.AddComponent<RectTransform>();
        progRt.anchorMin = Vector2.zero;
        progRt.anchorMax = Vector2.one;
        progRt.sizeDelta = new Vector2(-10, -10);
        progRt.anchoredPosition = Vector2.zero;
        gazeProgressImage = progressCircle.AddComponent<Image>();
        gazeProgressImage.color = new Color(0.2f, 0.8f, 0.4f, 0.8f);
        gazeProgressImage.type = Image.Type.Filled;
        gazeProgressImage.fillMethod = Image.FillMethod.Radial360;
        gazeProgressImage.fillAmount = 0;
        
        // Center dot (reticle)
        GameObject centerDot = new GameObject("CenterDot");
        centerDot.transform.SetParent(gazeIndicator.transform);
        RectTransform dotRt = centerDot.AddComponent<RectTransform>();
        dotRt.anchorMin = new Vector2(0.5f, 0.5f);
        dotRt.anchorMax = new Vector2(0.5f, 0.5f);
        dotRt.sizeDelta = new Vector2(8, 8);
        dotRt.anchoredPosition = Vector2.zero;
        Image dotImg = centerDot.AddComponent<Image>();
        dotImg.color = Color.white;
        
        gazeIndicator.SetActive(false);
    }
    
    void CreateUI()
    {
        // Create instruction panel
        GameObject uiCanvas = new GameObject("UICanvas");
        Canvas canvas = uiCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = uiCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        uiCanvas.AddComponent<GraphicRaycaster>();
        
        // Title
        CreateUIText(uiCanvas.transform, "Research Vault", new Vector2(0, -50), 36, TextAnchor.UpperCenter, new Color(0.9f, 0.85f, 0.7f));
        
        // Instructions panel
        GameObject instructionPanel = new GameObject("InstructionPanel");
        instructionPanel.transform.SetParent(uiCanvas.transform);
        RectTransform panelRt = instructionPanel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0, 1);
        panelRt.anchorMax = new Vector2(0, 1);
        panelRt.pivot = new Vector2(0, 1);
        panelRt.anchoredPosition = new Vector2(20, -20);
        panelRt.sizeDelta = new Vector2(300, 190);
        
        Image panelBg = instructionPanel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.5f);
        
        // Instructions text
        string instructions = "CONTROLS\n" +
                            "WASD - Move\n" +
                            "Mouse - Look around\n" +
                            "Gaze at tablet for 2s - View paper\n" +
                            "Gaze at X button - Close paper\n" +
                            "ESC - Toggle cursor lock\n\n" +
                            "TV SCREEN\n" +
                            "↑↓/WS - Scroll\n" +
                            "X - Close";
        
        CreateUIText(instructionPanel.transform, instructions, new Vector2(15, -15), 14, TextAnchor.UpperLeft, Color.white, new Vector2(270, 180));
    }
    
    void CreateUIText(Transform parent, string text, Vector2 position, int fontSize, TextAnchor anchor, Color color, Vector2? size = null)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent);
        
        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = position;
        rt.sizeDelta = size ?? new Vector2(400, 50);
        
        Text uiText = textObj.AddComponent<Text>();
        uiText.text = text;
        // Try to get Arial font - this works in most Unity versions
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            uiText.font = font;
        }
        uiText.fontSize = fontSize;
        uiText.alignment = anchor;
        uiText.color = color;
    }
    
    #endregion
    
    #region PLAYER CONTROLS
    
    void HandleMovement()
    {
        if (player == null || playerController == null) return;
        
        float horizontal = 0;
        float vertical = 0;
        
        if (Input.GetKey(KeyCode.W)) vertical = 1;
        if (Input.GetKey(KeyCode.S)) vertical = -1;
        if (Input.GetKey(KeyCode.A)) horizontal = -1;
        if (Input.GetKey(KeyCode.D)) horizontal = 1;
        
        Vector3 move = player.transform.right * horizontal + player.transform.forward * vertical;
        move.y = 0;
        move = move.normalized * moveSpeed;
        
        // Gravity
        if (playerController.isGrounded)
        {
            verticalVelocity = -0.5f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        
        move.y = verticalVelocity;
        playerController.Move(move * Time.deltaTime);
    }
    
    void HandleMouseLook()
    {
        if (!cursorLocked) return;
        if (player == null || mainCamera == null) return; // Safety check
        
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        rotationY += mouseX;
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        
        player.transform.rotation = Quaternion.Euler(0, rotationY, 0);
        mainCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
    }
    
    void HandleCursorLock()
    {
        // ESC only toggles cursor lock - paper is closed via the close button on the panel
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (cursorLocked)
            {
                UnlockCursor();
            }
            else
            {
                LockCursor();
            }
        }
        
        if (Input.GetMouseButtonDown(0) && !cursorLocked && !isViewingPaper)
        {
            LockCursor();
        }
    }
    
    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLocked = true;
    }
    
    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLocked = false;
    }
    
    #endregion
    
    #region GAZE INTERACTION
    
    void HandleGazeInteraction()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 10f))
        {
            // Check if we hit a tablet
            TabletData tabletData = hit.collider.GetComponentInParent<TabletData>();
            
            if (tabletData != null)
            {
                GameObject tablet = tabletData.gameObject;
                
                if (currentGazedTablet != tablet)
                {
                    // New tablet being gazed at
                    currentGazedTablet = tablet;
                    gazeTimer = 0;
                    if (gazeIndicator != null)
                        gazeIndicator.SetActive(true);
                }
                
                // Update gaze timer
                gazeTimer += Time.deltaTime;
                
                if (gazeProgressImage != null)
                {
                    gazeProgressImage.fillAmount = gazeTimer / gazeActivationTime;
                    
                    // Change color as progress increases
                    gazeProgressImage.color = Color.Lerp(
                        new Color(0.2f, 0.8f, 0.4f, 0.8f),
                        new Color(0.2f, 0.4f, 0.9f, 1f),
                        gazeTimer / gazeActivationTime
                    );
                }
                
                // Activate tablet when gaze time reached
                if (gazeTimer >= gazeActivationTime)
                {
                    ActivateTablet(tabletData.tabletIndex, tabletData.paperUrl);
                }
            }
            else
            {
                ResetGaze();
            }
        }
        else
        {
            ResetGaze();
        }
    }
    
    void ResetGaze()
    {
        if (currentGazedTablet != null)
        {
            currentGazedTablet = null;
            gazeTimer = 0;
            if (gazeIndicator != null)
                gazeIndicator.SetActive(false);
            if (gazeProgressImage != null)
                gazeProgressImage.fillAmount = 0;
        }
    }
    
    void ActivateTablet(int index, string url)
    {
        Debug.Log($"Activating tablet {index + 1}: {url}");
        activeTabletIndex = index;
        isViewingPaper = true;
        
        if (gazeIndicator != null)
            gazeIndicator.SetActive(false);
        
        // Just lock cursor and let them view the permanent screen
        // Maybe highlight it or something?
        Debug.Log("Viewing paper on TV screen. Press X to close.");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        // For WebGL we still might want the HTML overlay if desired, but for now assume TV
        ShowPaperViewer(url);
        UnlockCursor();
        #else
        // For Editor/Desktop: just use the 3D TV screen
        LockCursor();
        #endif
    }
    
    // ... (CreateTVScreens method here) ...
    
    // Small TV screens near each tablet  
    private const float TV_SCREEN_SCALE = 0.002f; // Doubled size: 1 pixel = 2mm (so 600px = 1.2 meters wide)
    
    // TV positions near each tablet + 12 strategic new positions
    private Vector3[] tvPositions = new Vector3[] {
        // Original 4 (Adjusted to avoid clipping)
        new Vector3(-5.4f, 1.8f, -2.8f),   // Near tablet 0
        new Vector3(5.4f, 1.8f, -2.8f),    // Near tablet 1
        new Vector3(-5.4f, 5.8f, 1.2f),    // Near tablet 2
        new Vector3(5.4f, 5.8f, 1.2f),      // Near tablet 3
        
        // Ground Floor Extra (6)
        new Vector3(-5.4f, 1.8f, 2.0f),    // Left wall mid
        new Vector3(5.4f, 1.8f, 2.0f),     // Right wall mid
        new Vector3(0f, 1.8f, 4.8f),       // Back wall center (Moved forward from 5.5)
        new Vector3(-3f, 1.8f, 4.8f),      // Back wall left (Moved forward from 5.5)
        new Vector3(3f, 1.8f, 4.8f),       // Back wall right (Moved forward from 5.5)
        new Vector3(0f, 1.8f, -4.0f),      // Front near entrance
        
        // Second Floor Extra (6)
        new Vector3(-5.4f, 5.8f, -2.0f),   // Left balcony front
        new Vector3(5.4f, 5.8f, -2.0f),    // Right balcony front
        new Vector3(0f, 5.8f, 4.8f),       // Back wall center upper (Moved forward from 5.5)
        new Vector3(-3f, 5.8f, 4.8f),      // Back wall left upper (Moved forward from 5.5)
        new Vector3(3f, 5.8f, 4.8f),       // Back wall right upper (Moved forward from 5.5)
        new Vector3(0f, 5.8f, 0f)          // Center atrium hanging
    };
    
    // Array to store all TV screens (one for each tablet)
    private GameObject[] tvScreens = new GameObject[16];
    private ScrollRect[] tvScrollRects = new ScrollRect[16];
    private ScrollRect contentScrollRect; // Temp variable for CreateScrollableContentUI
    private GameObject[] closeButtons = new GameObject[16];
    private GameObject[] scrollUpButtons = new GameObject[16];
    private GameObject[] scrollDownButtons = new GameObject[16];
    
    // Content containers for direct position scrolling
    private RectTransform[] contentContainers = new RectTransform[16];
    private float[] contentScrollPositions = new float[16]; // Track scroll Y position for each TV
    private float[] contentHeights = new float[16]; // Store total content height for scroll limits
    private float viewportHeight = 340f; // Visible area height in pixels
    
    // Collider-based buttons for gaze interaction (active session)
    private float panelGazeTimer = 0f;
    private GameObject currentPanelTarget = null;
    
    // Track which TV is currently being looked at (for scrolling without activation)
    private int gazedTvIndex = -1;
    
    void ShowEditorPaperOverlay(int index)
    {
        // This function is no longer needed - TV screens are created at startup
        // and content is loaded from actual HTML files
        // The permanent TV screens already display the loaded content
        Debug.Log($"ShowEditorPaperOverlay called for index {index} - using preloaded content");
        
        // If content hasn't loaded yet, it will update automatically when ready
        if (!paperContentsLoaded[index])
        {
            Debug.Log($"Paper {index} content still loading...");
        }
    }
    
    // Create a UI element without a collider (for non-interactive elements like title)
    GameObject CreateUIElement(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, 
        Vector2 position, Vector2 size, Color bgColor, string label, int fontSize, bool addCollider)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        
        // Background image
        Image img = obj.AddComponent<Image>();
        img.color = bgColor;
        
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        // Label text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        if (addCollider)
        {
            BoxCollider col = obj.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 500f);
            col.center = new Vector3(0, 0, -250f);
        }
        
        return obj;
    }
    
    GameObject CreateUIButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, 
        Vector2 position, Vector2 size, Color bgColor, string label, int fontSize)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        // Background image
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        // Label text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        // Add a 3D BoxCollider for gaze raycasting (UI elements don't have colliders by default)
        // Make it thick and extend toward camera for reliable detection
        BoxCollider col = btnObj.AddComponent<BoxCollider>();
        col.size = new Vector3(size.x, size.y, 500f); // Thick box to ensure raycast hits
        col.center = new Vector3(0, 0, -250f); // Extend toward camera (local -Z is toward viewer)
        
        return btnObj;
    }
    
    void CreateScrollableContentUI(Transform parent, string content, int tabletIndex)
    {
        // === SCROLL VIEW (outer container) ===
        GameObject scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(parent, false);
        
        RectTransform scrollViewRect = scrollView.AddComponent<RectTransform>();
        scrollViewRect.anchorMin = new Vector2(0, 0);
        scrollViewRect.anchorMax = new Vector2(1, 1);
        scrollViewRect.offsetMin = new Vector2(15, 35);  // Left, Bottom padding
        scrollViewRect.offsetMax = new Vector2(-55, -50); // Right (space for buttons), Top padding
        
        // Background
        Image scrollBg = scrollView.AddComponent<Image>();
        scrollBg.color = new Color(0.95f, 0.95f, 0.97f);
        
        // === VIEWPORT (clips content) ===
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportRect.pivot = new Vector2(0, 1); // Top-left pivot
        
        // Viewport background (optional, for debugging)
        Image viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = new Color(0.95f, 0.95f, 0.97f);
        
        // RectMask2D clips children outside bounds (works better in World Space)
        viewport.AddComponent<RectMask2D>();
        
        // === CONTENT CONTAINER (moves when scrolling) ===
        GameObject contentContainer = new GameObject("Content");
        contentContainer.transform.SetParent(viewport.transform, false);
        
        RectTransform contentRect = contentContainer.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1); // Anchor to top
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0, 1); // Top-left pivot
        contentRect.anchoredPosition = Vector2.zero;
        
        // === TEXT ===
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(contentContainer.transform, false);
        
        Text contentText = textObj.AddComponent<Text>();
        contentText.text = content;
        contentText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        contentText.fontSize = 14;
        contentText.color = new Color(0.1f, 0.1f, 0.15f);
        contentText.alignment = TextAnchor.UpperLeft;
        contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
        contentText.verticalOverflow = VerticalWrapMode.Overflow;
        contentText.supportRichText = true;
        contentText.lineSpacing = 1.2f;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 1);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0, 1);
        textRect.anchoredPosition = new Vector2(10, -5);
        
        // Force canvas update to get viewport size
        Canvas.ForceUpdateCanvases();
        
        // Calculate text width based on viewport
        float viewportWidth = viewportRect.rect.width > 0 ? viewportRect.rect.width : 500f;
        textRect.sizeDelta = new Vector2(viewportWidth - 20, 0); // Fixed width, auto height
        
        // ContentSizeFitter for auto height
        ContentSizeFitter textFitter = textObj.AddComponent<ContentSizeFitter>();
        textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // Force another update
        LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);
        Canvas.ForceUpdateCanvases();
        
        // Get text height
        float textHeight = contentText.preferredHeight;
        if (textHeight < 100) textHeight = 2000f; // Default if not calculated yet
        
        // Set content size (taller than viewport for scrolling)
        contentRect.sizeDelta = new Vector2(0, textHeight + 20);
        
        // Store viewport height
        float vpHeight = viewportRect.rect.height > 0 ? viewportRect.rect.height : 340f;
        viewportHeight = vpHeight;
        
        // Store references
        contentContainers[tabletIndex] = contentRect;
        contentScrollPositions[tabletIndex] = 0f;
        contentHeights[tabletIndex] = textHeight + 20;
        
        // Setup ScrollRect (for reference, we scroll manually)
        ScrollRect sr = scrollView.AddComponent<ScrollRect>();
        sr.content = contentRect;
        sr.viewport = viewportRect;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.inertia = false;
        sr.scrollSensitivity = 0;
        contentScrollRect = sr;
        tvScrollRects[tabletIndex] = sr;
        
        Debug.Log($"TV {tabletIndex}: Content height = {textHeight}, Viewport height = {vpHeight}");
    }
    
    void HideEditorPaperOverlay()
    {
        // No longer needed as we don't create/destroy screens
    }
    
    void HandlePanelInteraction()
    {
        if (!isViewingPaper || activeTabletIndex == -1) return;
        
        int index = activeTabletIndex;
        
        // === KEYBOARD CONTROLS ===
        HandlePanelKeyboardInput(index);
        
        // === GAZE-BASED CONTROLS FOR BUTTONS ===
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;
        
        // Check if gazing at TV screen buttons
        if (Physics.Raycast(ray, out hit, 5f))
        {
            GameObject hitObj = hit.collider.gameObject;
            
            // Check if it's a screen button for the ACTIVE tablet
            bool isButton = (hitObj == closeButtons[index] || 
                             hitObj == scrollUpButtons[index] || 
                             hitObj == scrollDownButtons[index]);
            
            if (isButton)
            {
                if (currentPanelTarget != hitObj)
                {
                    // New target - reset timer
                    currentPanelTarget = hitObj;
                    panelGazeTimer = 0f;
                    HighlightButton(hitObj, true);
                }
                
                panelGazeTimer += Time.deltaTime;
                
                // Update gaze indicator
                if (gazeIndicator != null)
                {
                    gazeIndicator.SetActive(true);
                    if (gazeProgressImage != null)
                    {
                        gazeProgressImage.fillAmount = panelGazeTimer / 1.5f; // 1.5 second activation
                        gazeProgressImage.color = Color.Lerp(
                            new Color(0.9f, 0.6f, 0.2f, 0.8f),
                            new Color(0.2f, 0.9f, 0.4f, 1f),
                            panelGazeTimer / 1.5f
                        );
                    }
                }
                
                // Activate button after gaze time
                if (panelGazeTimer >= 1.5f)
                {
                    ActivatePanelButton(hitObj, index);
                    panelGazeTimer = 0f;
                }
            }
            else
            {
                ResetPanelGaze();
            }
        }
        else
        {
            ResetPanelGaze();
        }
    }
    
    void HandlePanelKeyboardInput(int index)
    {
        // Close panel: X key or Backspace (ONLY if viewing)
        if (isViewingPaper && (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Backspace)))
        {
            Debug.Log("Keyboard: Close screen");
            ClosePaperViewer();
            return;
        }
        
        // Scroll Up: Up Arrow, W, or Page Up
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.PageUp))
        {
            ScrollContent(index, -0.02f); // Smooth continuous scroll
        }
        
        // Scroll Down: Down Arrow, S, or Page Down
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.PageDown))
        {
            ScrollContent(index, 0.02f); // Smooth continuous scroll
        }
    }
    
    void HighlightButton(GameObject btn, bool highlight)
    {
        if (btn == null) return;
        
        // Use UI Image component instead of Renderer
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            Color baseColor = img.color;
            if (highlight)
            {
                img.color = new Color(
                    Mathf.Min(baseColor.r * 1.3f, 1f),
                    Mathf.Min(baseColor.g * 1.3f, 1f),
                    Mathf.Min(baseColor.b * 1.3f, 1f),
                    baseColor.a
                );
            }
        }
    }
    
    void ResetPanelGaze()
    {
        if (currentPanelTarget != null)
        {
            // Reset highlight
            currentPanelTarget = null;
        }
        panelGazeTimer = 0f;
        
        if (gazeIndicator != null)
        {
            gazeIndicator.SetActive(false);
            if (gazeProgressImage != null)
                gazeProgressImage.fillAmount = 0;
        }
    }
    
    void ActivatePanelButton(GameObject btn, int index)
    {
        if (btn == closeButtons[index])
        {
            Debug.Log("Close button activated!");
            ClosePaperViewer();
        }
        else if (btn == scrollUpButtons[index])
        {
            ScrollContent(index, -0.2f); // Scroll up
            Debug.Log("Scrolling up");
        }
        else if (btn == scrollDownButtons[index])
        {
            ScrollContent(index, 0.2f); // Scroll down
            Debug.Log("Scrolling down");
        }
    }
    
    void ScrollContent(int index, float amount)
    {
        // Scroll by directly moving the content container position
        if (contentContainers[index] != null)
        {
            // Amount is normalized (0-1), convert to pixels
            float scrollPixels = amount * 100f; // 100 pixels per scroll unit
            
            // Update scroll position (positive = scroll down = content moves up)
            contentScrollPositions[index] += scrollPixels;
            
            // Calculate max scroll (content height minus viewport height)
            float maxScroll = Mathf.Max(0, contentHeights[index] - viewportHeight);
            
            // Clamp scroll position
            contentScrollPositions[index] = Mathf.Clamp(contentScrollPositions[index], 0, maxScroll);
            
            // Apply position to content container (moves down as we scroll down)
            Vector2 pos = contentContainers[index].anchoredPosition;
            pos.y = contentScrollPositions[index];
            contentContainers[index].anchoredPosition = pos;
            
            Debug.Log($"Scroll TV {index}: pos={contentScrollPositions[index]:F0}/{maxScroll:F0}");
        }
        // Fallback to ScrollRect if available
        else if (tvScrollRects[index] != null)
        {
            float newPos = tvScrollRects[index].verticalNormalizedPosition - amount;
            tvScrollRects[index].verticalNormalizedPosition = Mathf.Clamp01(newPos);
        }
    }
    
    
    void HandlePaperViewingExit()
    {
        // Paper viewing exit is now handled by the close button on the panel
        // This method is kept for WebGL compatibility
        #if UNITY_WEBGL && !UNITY_EDITOR
        // In WebGL, the JavaScript handles the actual closing
        #endif
    }
    
    void ClosePaperViewer()
    {
        Debug.Log("Closing paper viewer");
        isViewingPaper = false;
        activeTabletIndex = -1;
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        HidePaperViewer();
        #else
        HideEditorPaperOverlay();
        #endif
        
        LockCursor();
        ResetGaze();
    }
    
    // Called from JavaScript when user closes the paper viewer
    public void OnPaperViewerClosed()
    {
        ClosePaperViewer();
    }
    
    #endregion
    
    #region TORCHES
    
    void CreateTorches()
    {
        GameObject torchesParent = new GameObject("Torches");
        torchesParent.transform.SetParent(transform);
        
        // === INTERIOR TORCHES (Ground Floor) ===
        // Near entrance
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-2.5f, 0, -5f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(2.5f, 0, -5f));
        
        // Along walls - left side
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-5.5f, 0, -2f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-5.5f, 0, 2f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-5.5f, 0, 5f));
        
        // Along walls - right side
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(5.5f, 0, -2f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(5.5f, 0, 2f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(5.5f, 0, 5f));
        
        // Near bookshelves
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-3f, 0, 0f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(3f, 0, 0f));
        
        // Back area near stairs
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-3f, 0, 3f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(3f, 0, 3f));
        
        // === INTERIOR TORCHES (Second Floor / Balconies) ===
        // Left balcony
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-4.5f, 4f, -2f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-4.5f, 4f, 2f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-4.5f, 4f, 5f));
        
        // Right balcony
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(4.5f, 4f, -2f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(4.5f, 4f, 2f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(4.5f, 4f, 5f));
        
        // Back balcony (near stairs top)
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-2f, 4f, 5.5f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(2f, 4f, 5.5f));
        
        // Front balcony
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-2f, 4f, -4f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(2f, 4f, -4f));
        
        // Central hanging area
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(0, 4f, 0f));
        
        // === EXTERIOR TORCHES (Outside Library) ===
        // Along the path to entrance
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-3f, -4f, -10f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(3f, -4f, -10f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-3f, -4f, -15f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(3f, -4f, -15f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-3f, -4f, -20f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(3f, -4f, -20f));
        
        // Around the library exterior
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-8f, -4f, -7f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(8f, -4f, -7f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-8f, -4f, 0f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(8f, -4f, 0f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-8f, -4f, 7f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(8f, -4f, 7f));
        
        // Back of library
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-3f, -4f, 10f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(3f, -4f, 10f));
        
        // Around castle wall entrances
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-5f, -4f, -50f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(5f, -4f, -50f));
        
        // Garden corners - distant ambiance
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-15f, -4f, -25f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(15f, -4f, -25f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(-15f, -4f, 15f));
        CreateWoodenConeTorch(torchesParent.transform, new Vector3(15f, -4f, 15f));
        
        Debug.Log("=== Created wooden cone torches for night lighting ===");
    }
    
    void CreateWoodenConeTorch(Transform parent, Vector3 position)
    {
        GameObject torch = new GameObject("WoodenConeTorch");
        torch.transform.SetParent(parent);
        torch.transform.position = position;
        
        // === WOODEN POLE (Cone shape) ===
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "TorchPole";
        pole.transform.SetParent(torch.transform);
        pole.transform.localPosition = new Vector3(0, 0.6f, 0);
        pole.transform.localScale = new Vector3(0.08f, 0.6f, 0.08f); // Thin tall pole
        pole.GetComponent<Renderer>().material = darkWoodMaterial;
        Destroy(pole.GetComponent<Collider>());
        
        // === WOODEN CONE (Torch holder) ===
        // Create a cone using a cylinder with tapered scale trick
        // Top part of cone
        GameObject coneBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coneBase.name = "ConeBase";
        coneBase.transform.SetParent(torch.transform);
        coneBase.transform.localPosition = new Vector3(0, 1.25f, 0);
        coneBase.transform.localScale = new Vector3(0.2f, 0.1f, 0.2f);
        coneBase.GetComponent<Renderer>().material = woodMaterial;
        Destroy(coneBase.GetComponent<Collider>());
        
        // Cone middle
        GameObject coneMid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coneMid.name = "ConeMid";
        coneMid.transform.SetParent(torch.transform);
        coneMid.transform.localPosition = new Vector3(0, 1.35f, 0);
        coneMid.transform.localScale = new Vector3(0.15f, 0.08f, 0.15f);
        coneMid.GetComponent<Renderer>().material = woodMaterial;
        Destroy(coneMid.GetComponent<Collider>());
        
        // Cone top ring
        GameObject coneTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coneTop.name = "ConeTop";
        coneTop.transform.SetParent(torch.transform);
        coneTop.transform.localPosition = new Vector3(0, 1.42f, 0);
        coneTop.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);
        coneTop.GetComponent<Renderer>().material = woodMaterial;
        Destroy(coneTop.GetComponent<Collider>());
        
        // === FIRE GLOW (Emissive cube for flame illusion) ===
        GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flame.name = "TorchFlame";
        flame.transform.SetParent(torch.transform);
        flame.transform.localPosition = new Vector3(0, 1.55f, 0);
        flame.transform.localScale = new Vector3(0.1f, 0.2f, 0.1f);
        
        // Create emissive flame material
        Material flameMat = new Material(Shader.Find("Standard"));
        flameMat.SetColor("_Color", new Color(1f, 0.5f, 0.1f));
        flameMat.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.2f) * 3f);
        flameMat.EnableKeyword("_EMISSION");
        flame.GetComponent<Renderer>().material = flameMat;
        Destroy(flame.GetComponent<Collider>());
        
        // Secondary flame piece (for depth)
        GameObject flame2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flame2.name = "TorchFlame2";
        flame2.transform.SetParent(torch.transform);
        flame2.transform.localPosition = new Vector3(0, 1.65f, 0);
        flame2.transform.localScale = new Vector3(0.06f, 0.15f, 0.06f);
        flame2.transform.rotation = Quaternion.Euler(0, 45, 0);
        
        Material flameMat2 = new Material(Shader.Find("Standard"));
        flameMat2.SetColor("_Color", new Color(1f, 0.8f, 0.3f));
        flameMat2.SetColor("_EmissionColor", new Color(1f, 0.9f, 0.4f) * 4f);
        flameMat2.EnableKeyword("_EMISSION");
        flame2.GetComponent<Renderer>().material = flameMat2;
        Destroy(flame2.GetComponent<Collider>());
        
        // === TORCH LIGHT (Point light for illumination) ===
        GameObject lightObj = new GameObject("TorchLight");
        lightObj.transform.SetParent(torch.transform);
        lightObj.transform.localPosition = new Vector3(0, 1.6f, 0);
        Light torchLight = lightObj.AddComponent<Light>();
        torchLight.type = LightType.Point;
        torchLight.color = new Color(1f, 0.7f, 0.3f); // Warm fire color
        torchLight.range = 8f;
        torchLight.intensity = 1.5f;
        torchLight.shadows = LightShadows.Soft;
    }
    
    #endregion
    
    #region CASTLE & GARDEN

    void BuildGardenAndSurroundings()
    {
        GameObject garden = new GameObject("Garden");
        garden.transform.SetParent(transform);

        // 1. Extended Ground (Grass/Dirt)
        BuildFloorRect(garden.transform, -1, -120, 120, -120, 120, materials["Cobblestone"]); // Large cobblestone ground

        // 2. Palm Trees
        for (int i = 0; i < 40; i++)
        {
            float angle = Random.Range(0, Mathf.PI * 2);
            float dist = Random.Range(25, 80);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, -4f, Mathf.Sin(angle) * dist);
            CreatePalmTree(garden.transform, pos);
        }

        // 3. 30k Detail Blocks (Cobblestone paths/features)
        BuildDetailedPath(garden.transform);

        // 4. Outer Castle Wall
        BuildCastleWall(garden.transform);
    }

    void CreatePalmTree(Transform parent, Vector3 pos)
    {
        GameObject tree = new GameObject("PalmTree");
        tree.transform.SetParent(parent);
        tree.transform.position = pos;

        List<CombineInstance> combines = new List<CombineInstance>();
        Mesh cubeMesh = CreateCubeMesh();

        // Trunk (curved)
        float height = Random.Range(6f, 9f);
        Vector3 currentPos = Vector3.zero;
        for (int i = 0; i < 25; i++)
        {
            AddVoxelToCombine(combines, cubeMesh, 
                (int)(currentPos.x/voxelSize), (int)(currentPos.y/voxelSize), (int)(currentPos.z/voxelSize), 
                voxelSize * 2f); // Thicker trunk
            
            currentPos += Vector3.up * 0.4f;
            currentPos += new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));
        }
        
        // Create trunk mesh
        GameObject trunkObj = new GameObject("Trunk");
        trunkObj.transform.SetParent(tree.transform);
        trunkObj.transform.localPosition = Vector3.zero;
        CreateCombinedMesh(trunkObj, combines, woodMaterial);

        // Leaves (Separate mesh for material)
        List<CombineInstance> leafCombines = new List<CombineInstance>();
        
        for (int i = 0; i < 8; i++)
        {
            float angle = i * (360f/8f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            
            Vector3 leafStart = currentPos;
            
            for (int j = 0; j < 8; j++)
            {
                Vector3 leafPos = leafStart + dir * (j * 0.6f) + Vector3.up * Mathf.Sin(j * 0.5f) + Vector3.down * (j * 0.2f);
                 AddVoxelToCombine(leafCombines, cubeMesh, 
                (int)(leafPos.x/voxelSize), (int)(leafPos.y/voxelSize), (int)(leafPos.z/voxelSize), 
                voxelSize);
            }
        }
        
        GameObject leavesObj = new GameObject("Leaves");
        leavesObj.transform.SetParent(tree.transform);
        leavesObj.transform.localPosition = Vector3.zero;
        CreateCombinedMesh(leavesObj, leafCombines, leafMaterial);
    }

    void BuildDetailedPath(Transform parent)
    {
        GameObject path = new GameObject("DetailedPath");
        path.transform.SetParent(parent);
        
        List<CombineInstance> combines = new List<CombineInstance>();
        Mesh cubeMesh = CreateCubeMesh();

        // Generate ~30k blocks as a detailed mosaic/path around the library
        int count = 30000;
        
        for (int i = 0; i < count; i++)
        {
            // distribute in a large circle or area outside library
            float angle = Random.Range(0, Mathf.PI * 2);
            float dist = Random.Range(15f, 90f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, -3.8f, Mathf.Sin(angle) * dist);
            
            // Add noise to position
            pos += new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(0, 0.5f), Random.Range(-0.1f, 0.1f));

            // Create a small stone
            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)),
                Vector3.one * 0.15f // Small detail blocks
            );
            
            CombineInstance ci = new CombineInstance();
            ci.mesh = cubeMesh;
            ci.transform = matrix;
            combines.Add(ci);
        }
        
        CreateCombinedMesh(path, combines, cobblestoneMaterial);
    }

    void BuildCastleWall(Transform parent)
    {
        GameObject wallRoot = new GameObject("CastleWall");
        wallRoot.transform.SetParent(parent);
        
        List<CombineInstance> combines = new List<CombineInstance>();
        Mesh cubeMesh = CreateCubeMesh();
        
        // Build a square wall around at 50m distance (approx 200 voxels from center)
        int range = 200; // 50m / 0.25 = 200 voxels
        int height = 40; // 10m high
        
        // Iterate perimeter
        for (int h = 0; h < height; h++)
        {
            // North/South Walls
            for (int x = -range; x <= range; x++)
            {
                // Add Arch gaps every 50 units
                if (Mathf.Abs(x % 50) < 8 && h < 25) continue; // Arch opening
                
                // North
                AddVoxelToCombine(combines, cubeMesh, x, h, range, voxelSize);
                AddVoxelToCombine(combines, cubeMesh, x, h, range+1, voxelSize); // Double thickness
                // South
                AddVoxelToCombine(combines, cubeMesh, x, h, -range, voxelSize);
                AddVoxelToCombine(combines, cubeMesh, x, h, -range-1, voxelSize);
            }
            
            // East/West Walls
            for (int z = -range; z <= range; z++)
            {
                if (Mathf.Abs(z % 50) < 8 && h < 25) continue; // Arch opening

                // East
                AddVoxelToCombine(combines, cubeMesh, range, h, z, voxelSize);
                AddVoxelToCombine(combines, cubeMesh, range+1, h, z, voxelSize);
                // West
                AddVoxelToCombine(combines, cubeMesh, -range, h, z, voxelSize);
                AddVoxelToCombine(combines, cubeMesh, -range-1, h, z, voxelSize);
            }
        }
        
        // Crenellations (Top)
        for (int x = -range; x <= range; x+=4)
        {
            if (Mathf.Abs(x % 50) < 8) continue;
            AddVoxelToCombine(combines, cubeMesh, x, height, range, voxelSize);
            AddVoxelToCombine(combines, cubeMesh, x, height, -range, voxelSize);
        }
        for (int z = -range; z <= range; z+=4)
        {
            if (Mathf.Abs(z % 50) < 8) continue;
            AddVoxelToCombine(combines, cubeMesh, range, height, z, voxelSize);
            AddVoxelToCombine(combines, cubeMesh, -range, height, z, voxelSize);
        }
        
        CreateCombinedMesh(wallRoot, combines, stoneBrickMaterial);
        
        // Add Decorative Arches over the gaps
        // (London Castle style arches - rounded top)
        BuildArches(wallRoot.transform);
    }

    void BuildArches(Transform parent)
    {
         // Castle Wall Entrance
         CreateArch(parent, new Vector3(0, 0, -52f), 0); 
         
         // Library Main Entrance (Inner)
         GameObject mainArch = CreateArch(parent, new Vector3(0, 0, -7.5f), 0);
         
         // Header for Futuristic Library
         CreateUIText(mainArch.transform, "FUTURISTIC LIBRARY", new Vector2(0, 3.5f), 36, TextAnchor.MiddleCenter, new Color(0.2f, 0.8f, 1f), new Vector2(500, 100));
         
         // Library Back Entrance
         CreateArch(parent, new Vector3(0, 0, 6.5f), 0);
    }
    
    GameObject CreateArch(Transform parent, Vector3 pos, float rotation)
    {
        GameObject arch = new GameObject("LondonArch");
        arch.transform.SetParent(parent);
        arch.transform.position = pos;
        arch.transform.rotation = Quaternion.Euler(0, rotation, 0);
        
        List<CombineInstance> combines = new List<CombineInstance>();
        Mesh cubeMesh = CreateCubeMesh();
        
        // Pillars
        for (int y = 0; y < 25; y++)
        {
            for (int x = -10; x <= 10; x++)
            {
                if (Mathf.Abs(x) > 6) // Side pillars
                {
                     AddVoxelToCombine(combines, cubeMesh, x, y, 0, voxelSize);
                     AddVoxelToCombine(combines, cubeMesh, x, y, 1, voxelSize);
                }
            }
        }
        
        // Top Curve
        for (int x = -10; x <= 10; x++)
        {
            int yBase = 25;
            // Parabola or circle
            float curve = Mathf.Sqrt(100 - x*x); // Circle equation r=10
            int yHeight = (int)curve;
            
            for (int y = 0; y < yHeight + 2; y++)
            {
                 AddVoxelToCombine(combines, cubeMesh, x, yBase + y, 0, voxelSize);
                 AddVoxelToCombine(combines, cubeMesh, x, yBase + y, 1, voxelSize);
            }
        }
        
        CreateCombinedMesh(arch, combines, stoneBrickMaterial);
        return arch;
    }
    
    void CreateInteractiveGate()
    {
        gateObject = new GameObject("InteractiveGate");
        gateObject.transform.SetParent(transform);
        // Position on 2nd floor (Y approx 4m = 16 voxels)
        gateObject.transform.position = new Vector3(0, 4f, 8f); 
        
        // Gate Mesh (Bars)
        GameObject bars = new GameObject("Bars");
        bars.transform.SetParent(gateObject.transform);
        
        List<CombineInstance> combines = new List<CombineInstance>();
        Mesh cubeMesh = CreateCubeMesh();
        
        for (int x = -4; x <= 4; x+=2)
        {
            for (int y = 0; y < 12; y++)
            {
                AddVoxelToCombine(combines, cubeMesh, x, y, 0, voxelSize);
            }
        }
        // Cross bars
        for (int x = -4; x <= 4; x++)
        {
            AddVoxelToCombine(combines, cubeMesh, x, 0, 0, voxelSize);
            AddVoxelToCombine(combines, cubeMesh, x, 11, 0, voxelSize);
            AddVoxelToCombine(combines, cubeMesh, x, 6, 0, voxelSize);
        }
        
        CreateCombinedMesh(bars, combines, materials["DarkWood"]);
        
        // Knob
        gateKnob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gateKnob.name = "GateKnob";
        gateKnob.transform.SetParent(gateObject.transform);
        gateKnob.transform.localPosition = new Vector3(1f, 1.5f, 0.1f);
        gateKnob.transform.localScale = Vector3.one * 0.2f;
        gateKnob.GetComponent<Renderer>().material = materials["TabletScreen"]; // Glowing knob
        
        // Text - Needs World Space Canvas
        GameObject textCanvasObj = new GameObject("GateTextCanvas");
        textCanvasObj.transform.SetParent(gateObject.transform);
        textCanvasObj.transform.localPosition = new Vector3(1f, 2.0f, 0.1f);
        textCanvasObj.transform.localScale = Vector3.one * 0.01f; // Scale down for world space
        
        Canvas canvas = textCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRect = textCanvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(400, 100);
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textCanvasObj.transform);
        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localRotation = Quaternion.identity;
        
        Text uiText = textObj.AddComponent<Text>();
        uiText.text = "CLICK KNOB\nTO OPEN";
        uiText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        uiText.fontSize = 36;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.color = Color.white;
        uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.localScale = Vector3.one;

        // Collider for interaction
        SphereCollider col = gateKnob.GetComponent<SphereCollider>();
        if (col == null) col = gateKnob.AddComponent<SphereCollider>();
        col.radius = 0.5f; // Easier to hit
    }
    
    void HandleGateInteraction()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 5f))
            {
                if (hit.collider.gameObject == gateKnob)
                {
                    ToggleGate();
                }
            }
        }
    }
    
    void ToggleGate()
    {
        isGateOpen = !isGateOpen;
        
        // Simple animation - rotate
        if (isGateOpen)
        {
            gateObject.transform.rotation = Quaternion.Euler(0, 90, 0);
        }
        else
        {
            gateObject.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        
        Debug.Log("Gate toggled: " + isGateOpen);
    }

    #endregion

    #region MESH UTILITIES
    
    Mesh CreateCubeMesh()
    {
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[] {
            // Front
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
            // Back
            new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f),
            // Top
            new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
            // Bottom
            new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f),
            // Left
            new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
            // Right
            new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f)
        };
        
        int[] triangles = new int[] {
            0, 2, 1, 0, 3, 2,       // Front
            4, 6, 5, 4, 7, 6,       // Back
            8, 10, 9, 8, 11, 10,    // Top
            12, 14, 13, 12, 15, 14, // Bottom
            16, 18, 17, 16, 19, 18, // Left
            20, 22, 21, 20, 23, 22  // Right
        };
        
        Vector3[] normals = new Vector3[] {
            Vector3.back, Vector3.back, Vector3.back, Vector3.back,
            Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
            Vector3.up, Vector3.up, Vector3.up, Vector3.up,
            Vector3.down, Vector3.down, Vector3.down, Vector3.down,
            Vector3.left, Vector3.left, Vector3.left, Vector3.left,
            Vector3.right, Vector3.right, Vector3.right, Vector3.right
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        
        return mesh;
    }
    
    void AddVoxelToCombine(List<CombineInstance> combines, Mesh mesh, int x, int y, int z, float size)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(
            new Vector3(x * size, y * size, z * size),
            Quaternion.identity,
            Vector3.one * size * 0.98f
        );
        
        CombineInstance ci = new CombineInstance();
        ci.mesh = mesh;
        ci.transform = matrix;
        combines.Add(ci);
    }
    
    void CreateCombinedMesh(GameObject obj, List<CombineInstance> combines, Material mat)
    {
        if (combines.Count == 0) return;
        
        MeshFilter mf = obj.AddComponent<MeshFilter>();
        mf.mesh = new Mesh();
        mf.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mf.mesh.CombineMeshes(combines.ToArray(), true, true);
        mf.mesh.RecalculateNormals();
        mf.mesh.RecalculateBounds();
        
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        mr.material = mat;
        
        MeshCollider mc = obj.AddComponent<MeshCollider>();
        mc.sharedMesh = mf.mesh;
    }
    
    #endregion
}

/// <summary>
/// Component to store tablet data
/// </summary>
public class TabletData : MonoBehaviour
{
    public int tabletIndex;
    public string paperUrl;
}

