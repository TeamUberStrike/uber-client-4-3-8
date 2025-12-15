using UnityEngine;

/// <summary>
/// Unity 6 HUD using basic GameObjects and TextMesh instead of legacy UI
/// Works without UnityEngine.UI dependencies
/// </summary>
public class Unity6CanvasHUD : MonoBehaviour
{
    [Header("HUD Settings")]
    public bool showDebugInfo = true;
    public bool showCrosshair = true;
    public bool showHUD = true;
    
    private float _time = 0f;
    private int _frameCount = 0;
    private HudController _hudController;
    
    // HUD GameObjects
    private GameObject _headerBanner;
    private GameObject _healthDisplay;
    private GameObject _ammoDisplay;
    private GameObject _weaponDisplay;
    private GameObject _xpDisplay;
    private GameObject _crosshairDisplay;
    private GameObject _timeDisplay;
    private GameObject _frameDisplay;
    
    void Start()
    {
        Debug.Log("[Unity6CanvasHUD] 🎮 Creating Unity 6 GameObject-based HUD...");
        
        // Find existing HUD controller
        _hudController = FindObjectOfType<HudController>();
        
        // Create the HUD using basic GameObjects
        CreateGameObjectHUD();
        
        Debug.Log("[Unity6CanvasHUD] ✅ GameObject HUD created and ready!");
    }
    
    void CreateGameObjectHUD()
    {
        // Create header banner
        CreateHeaderBanner();
        
        // Create corner elements
        CreateCornerElements();
        
        // Create crosshair
        CreateCrosshair();
        
        // Create debug info
        CreateDebugInfo();
        
        Debug.Log("[Unity6CanvasHUD] GameObject UI elements created successfully!");
    }
    
    void CreateHeaderBanner()
    {
        // Create header banner using a primitive cube as background
        _headerBanner = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _headerBanner.name = "Unity6_HeaderBanner";
        
        // Position at top of screen (world space, visible to camera)
        _headerBanner.transform.position = new Vector3(0, 8, 10);
        _headerBanner.transform.localScale = new Vector3(20, 1, 0.1f);
        
        // Make it bright magenta
        var renderer = _headerBanner.GetComponent<Renderer>();
        var material = new Material(Shader.Find("Standard"));
        material.color = Color.magenta;
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Glossiness", 0.8f);
        renderer.material = material;
        
        // Add TextMesh for the header text
        var textGO = new GameObject("HeaderText");
        textGO.transform.SetParent(_headerBanner.transform);
        textGO.transform.localPosition = new Vector3(0, 0, -0.6f);
        textGO.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        
        var textMesh = textGO.AddComponent<TextMesh>();
        textMesh.text = "UNITY 6 HUD WORKING!";
        textMesh.fontSize = 20;
        textMesh.color = Color.white;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        DontDestroyOnLoad(_headerBanner);
        
        Debug.Log("[Unity6CanvasHUD] Created header banner at world position");
    }
    
    void CreateCornerElements()
    {
        // Create corner elements as 3D objects in world space
        _healthDisplay = CreateCornerCube("HealthDisplay", new Vector3(-8, 6, 10), Color.red, "HP: 100");
        _ammoDisplay = CreateCornerCube("AmmoDisplay", new Vector3(8, 6, 10), Color.blue, "AMMO: 30");
        _weaponDisplay = CreateCornerCube("WeaponDisplay", new Vector3(-8, -6, 10), Color.green, "WEAPON: M4");
        _xpDisplay = CreateCornerCube("XPDisplay", new Vector3(8, -6, 10), Color.yellow, "XP: 1250");
    }
    
    GameObject CreateCornerCube(string name, Vector3 position, Color color, string text)
    {
        // Create cube background
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.localScale = new Vector3(3, 1, 0.1f);
        
        // Set color
        var renderer = cube.GetComponent<Renderer>();
        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Glossiness", 0.5f);
        renderer.material = material;
        
        // Add text
        var textGO = new GameObject(name + "_Text");
        textGO.transform.SetParent(cube.transform);
        textGO.transform.localPosition = new Vector3(0, 0, -0.6f);
        textGO.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        
        var textMesh = textGO.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = 20;
        textMesh.color = Color.white;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        DontDestroyOnLoad(cube);
        return cube;
    }
    
    void CreateCrosshair()
    {
        if (!showCrosshair) return;
        
        // Create crosshair as a small red sphere
        _crosshairDisplay = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _crosshairDisplay.name = "Unity6_Crosshair";
        _crosshairDisplay.transform.position = new Vector3(0, 0, 8);
        _crosshairDisplay.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        
        // Make it bright red
        var renderer = _crosshairDisplay.GetComponent<Renderer>();
        var material = new Material(Shader.Find("Standard"));
        material.color = Color.red;
        material.SetFloat("_Metallic", 1f);
        material.SetFloat("_Glossiness", 1f);
        renderer.material = material;
        
        DontDestroyOnLoad(_crosshairDisplay);
        
        Debug.Log("[Unity6CanvasHUD] Created crosshair sphere");
    }
    
    void CreateDebugInfo()
    {
        if (!showDebugInfo) return;
        
        // Create time display
        _timeDisplay = CreateCornerCube("TimeDisplay", new Vector3(0, 5, 10), Color.cyan, "Time: 0.0s");
        
        // Create frame display  
        _frameDisplay = CreateCornerCube("FrameDisplay", new Vector3(0, 3.5f, 10), Color.cyan, "Frames: 0");
    }
    
    void Update()
    {
        _time += Time.deltaTime;
        _frameCount++;
        
        // Update HUD data
        UpdateHUDData();
        
        // Handle input
        HandleInput();
    }
    
    void UpdateHUDData()
    {
        // Update debug info
        if (_timeDisplay != null)
        {
            var timeText = _timeDisplay.GetComponentInChildren<TextMesh>();
            if (timeText != null) timeText.text = $"Time: {_time:F1}s";
        }
        
        if (_frameDisplay != null)
        {
            var frameText = _frameDisplay.GetComponentInChildren<TextMesh>();
            if (frameText != null) frameText.text = $"Frames: {_frameCount}";
        }
        
        // Update HUD elements with real game data if available
        if (_hudController != null)
        {
            try
            {
                // Try to get health info
                var healthHud = HpApHud.Instance;
                if (healthHud != null && _healthDisplay != null)
                {
                    var healthText = _healthDisplay.GetComponentInChildren<TextMesh>();
                    if (healthText != null) healthText.text = "HP: Connected";
                }
                
                // Try to get ammo info
                var ammoHud = AmmoHud.Instance;
                if (ammoHud != null && _ammoDisplay != null)
                {
                    var ammoText = _ammoDisplay.GetComponentInChildren<TextMesh>();
                    if (ammoText != null) ammoText.text = "AMMO: Connected";
                }
                
                // Try to get weapon info
                var weaponHud = WeaponsHud.Instance;
                if (weaponHud != null && _weaponDisplay != null)
                {
                    var weaponText = _weaponDisplay.GetComponentInChildren<TextMesh>();
                    if (weaponText != null) weaponText.text = "WEAPON: Connected";
                }
                
                // Try to get XP info
                var xpHud = XpPtsHud.Instance;
                if (xpHud != null && _xpDisplay != null)
                {
                    var xpText = _xpDisplay.GetComponentInChildren<TextMesh>();
                    if (xpText != null) xpText.text = "XP: Connected";
                }
            }
            catch (System.Exception ex)
            {
                // Safely handle any errors accessing HUD singletons
                Debug.LogWarning($"[Unity6CanvasHUD] Error accessing HUD data: {ex.Message}");
            }
        }
    }
    
    void HandleInput()
    {
        // Toggle HUD visibility with TAB
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            showHUD = !showHUD;
            SetHUDVisibility(showHUD);
            Debug.Log($"[Unity6CanvasHUD] HUD toggled: {(showHUD ? "ON" : "OFF")}");
        }
        
        // Toggle debug info with I
        if (Input.GetKeyDown(KeyCode.I))
        {
            showDebugInfo = !showDebugInfo;
            if (_timeDisplay != null) _timeDisplay.SetActive(showDebugInfo);
            if (_frameDisplay != null) _frameDisplay.SetActive(showDebugInfo);
            Debug.Log($"[Unity6CanvasHUD] Debug info: {(showDebugInfo ? "ON" : "OFF")}");
        }
        
        // Toggle crosshair with C
        if (Input.GetKeyDown(KeyCode.C))
        {
            showCrosshair = !showCrosshair;
            if (_crosshairDisplay != null) _crosshairDisplay.SetActive(showCrosshair);
            Debug.Log($"[Unity6CanvasHUD] Crosshair: {(showCrosshair ? "ON" : "OFF")}");
        }
    }
    
    void SetHUDVisibility(bool visible)
    {
        if (_headerBanner != null) _headerBanner.SetActive(visible);
        if (_healthDisplay != null) _healthDisplay.SetActive(visible);
        if (_ammoDisplay != null) _ammoDisplay.SetActive(visible);
        if (_weaponDisplay != null) _weaponDisplay.SetActive(visible);
        if (_xpDisplay != null) _xpDisplay.SetActive(visible);
        if (_crosshairDisplay != null) _crosshairDisplay.SetActive(visible && showCrosshair);
        if (_timeDisplay != null) _timeDisplay.SetActive(visible && showDebugInfo);
        if (_frameDisplay != null) _frameDisplay.SetActive(visible && showDebugInfo);
    }
}