using UnityEngine;

/// <summary>
/// Modern UI Page component - Replaces PageScene for Unity 6
/// Base class for all UI pages in the new OnGUI-based system
/// </summary>
public class UIPage : MonoBehaviour
{
    [Header("Page Configuration")]
    public PageType pageType;
    public string pageName;
    public bool isModal = false;
    
    [Header("Animation")]
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.2f;
    
    [Header("UI Components")]
    public LegacyTextData[] textElements;
    public LegacyButtonData[] buttonElements;
    public LegacyImageData[] imageElements;
    
    private float _alpha = 0f;
    private bool _isVisible = false;
    private bool _isAnimating = false;
    private float _animationStartTime;
    private float _targetAlpha;
    
    /// <summary>
    /// Current alpha value for OnGUI rendering
    /// </summary>
    public float Alpha => _alpha;
    
    /// <summary>
    /// Is this page currently visible?
    /// </summary>
    public bool IsVisible => _isVisible && _alpha > 0f;
    
    protected virtual void Awake()
    {
        // Initialize UI components
        InitializeComponents();
        
        // Start invisible
        _alpha = 0f;
        _isVisible = false;
    }
    
    protected virtual void Start()
    {
        // Override in derived classes for initialization
    }
    
    void Update()
    {
        UpdateAnimation();
    }
    
    void UpdateAnimation()
    {
        if (!_isAnimating) return;
        
        float elapsed = Time.time - _animationStartTime;
        float duration = _isVisible ? fadeInDuration : fadeOutDuration;
        
        if (elapsed >= duration)
        {
            // Animation complete
            _alpha = _targetAlpha;
            _isAnimating = false;
            
            if (!_isVisible && _alpha == 0f)
            {
                OnHideComplete();
            }
            else if (_isVisible && _alpha == 1f)
            {
                OnShowComplete();
            }
        }
        else
        {
            // Interpolate alpha
            float t = elapsed / duration;
            _alpha = Mathf.Lerp(_alpha, _targetAlpha, t);
        }
    }
    
    /// <summary>
    /// Show this page with fade-in animation
    /// </summary>
    public virtual void Show()
    {
        if (_isVisible) return;
        
        _isVisible = true;
        _targetAlpha = 1f;
        _isAnimating = true;
        _animationStartTime = Time.time;
        
        OnShowStart();
    }
    
    /// <summary>
    /// Hide this page with fade-out animation
    /// </summary>
    public virtual void Hide()
    {
        if (!_isVisible) return;
        
        _isVisible = false;
        _targetAlpha = 0f;
        _isAnimating = true;
        _animationStartTime = Time.time;
        
        OnHideStart();
    }
    
    /// <summary>
    /// Fade out the page with callback
    /// </summary>
    public virtual void FadeOut(System.Action onComplete = null)
    {
        if (!_isVisible) return;
        
        _isVisible = false;
        _targetAlpha = 0f;
        _isAnimating = true;
        _animationStartTime = Time.time;
        
        OnHideStart();
        
        if (onComplete != null)
        {
            StartCoroutine(WaitForFadeComplete(onComplete));
        }
    }
    
    /// <summary>
    /// Fade in the page
    /// </summary>
    public virtual void FadeIn()
    {
        Show();
    }
    
    private System.Collections.IEnumerator WaitForFadeComplete(System.Action onComplete)
    {
        while (_isAnimating)
        {
            yield return null;
        }
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Initialize UI components - find or create required data components
    /// </summary>
    protected virtual void InitializeComponents()
    {
        // Find existing components
        textElements = GetComponentsInChildren<LegacyTextData>();
        buttonElements = GetComponentsInChildren<LegacyButtonData>();
        imageElements = GetComponentsInChildren<LegacyImageData>();
        
        Debug.Log($"[UIPage] {pageName} initialized with {textElements.Length} texts, {buttonElements.Length} buttons, {imageElements.Length} images");
    }
    
    /// <summary>
    /// Called when show animation starts
    /// </summary>
    protected virtual void OnShowStart()
    {
        Debug.Log($"[UIPage] Showing {pageName}");
    }
    
    /// <summary>
    /// Called when show animation completes
    /// </summary>
    protected virtual void OnShowComplete()
    {
        Debug.Log($"[UIPage] {pageName} show complete");
    }
    
    /// <summary>
    /// Called when hide animation starts
    /// </summary>
    protected virtual void OnHideStart()
    {
        Debug.Log($"[UIPage] Hiding {pageName}");
    }
    
    /// <summary>
    /// Called when hide animation completes
    /// </summary>
    protected virtual void OnHideComplete()
    {
        Debug.Log($"[UIPage] {pageName} hide complete");
    }
    
    /// <summary>
    /// Called by UIManager when page is shown
    /// </summary>
    public virtual void OnPageShown()
    {
        Show();
    }
    
    /// <summary>
    /// Called by UIManager when page is hidden
    /// </summary>
    public virtual void OnPageHidden()
    {
        Hide();
    }
    
    /// <summary>
    /// Handle input for this page - override in derived classes
    /// </summary>
    public virtual void HandleInput()
    {
        // Override in derived classes
    }
    
    /// <summary>
    /// Render this page using OnGUI - called by LegacyUIManager
    /// </summary>
    public virtual void OnGUIRender()
    {
        if (_alpha <= 0f) return;
        
        // Apply alpha to all GUI elements
        var originalColor = GUI.color;
        GUI.color = new Color(originalColor.r, originalColor.g, originalColor.b, _alpha);
        
        RenderPageContent();
        
        // Restore original color
        GUI.color = originalColor;
    }
    
    /// <summary>
    /// Override this method to render page-specific content
    /// </summary>
    protected virtual void RenderPageContent()
    {
        // Default rendering of components
        RenderTextElements();
        RenderButtonElements();
        RenderImageElements();
    }
    
    protected virtual void RenderTextElements()
    {
        if (textElements == null) return;
        
        foreach (var textData in textElements)
        {
            if (textData == null || !textData.enabled) continue;
            
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = textData.fontSize;
            style.normal.textColor = textData.color;
            style.alignment = textData.alignment;
            if (textData.font != null) style.font = textData.font;
            
            var rect = GetComponentRect(textData);
            GUI.Label(rect, textData.text, style);
        }
    }
    
    protected virtual void RenderButtonElements()
    {
        if (buttonElements == null) return;
        
        foreach (var buttonData in buttonElements)
        {
            if (buttonData == null || !buttonData.enabled) continue;
            
            var style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = buttonData.textColor;
            if (buttonData.font != null) style.font = buttonData.font;
            
            var rect = GetComponentRect(buttonData);
            
            var originalBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = buttonData.backgroundColor;
            
            if (GUI.Button(rect, buttonData.text, style))
            {
                buttonData.onClick?.Invoke();
            }
            
            GUI.backgroundColor = originalBackgroundColor;
        }
    }
    
    protected virtual void RenderImageElements()
    {
        if (imageElements == null) return;
        
        foreach (var imageData in imageElements)
        {
            if (imageData == null || !imageData.enabled || imageData.texture == null) continue;
            
            var rect = GetComponentRect(imageData);
            
            var originalColor = GUI.color;
            GUI.color = imageData.color;
            
            GUI.DrawTexture(rect, imageData.texture, imageData.scaleMode);
            
            GUI.color = originalColor;
        }
    }
    
    /// <summary>
    /// Get screen rect for a component based on its transform
    /// </summary>
    protected virtual Rect GetComponentRect(MonoBehaviour component)
    {
        // Simple default positioning - can be overridden for more complex layouts
        var transform = component.transform;
        var position = transform.position;
        
        return new Rect(
            position.x, 
            position.y, 
            transform.localScale.x * 100f, 
            transform.localScale.y * 50f
        );
    }
    
    /// <summary>
    /// Navigate to another page using the UI manager
    /// </summary>
    protected void NavigateToPage(PageType pageType)
    {
        var legacyUI = LegacyUIManager.Instance;
        if (legacyUI != null)
        {
            legacyUI.ShowPage(pageType);
        }
        else
        {
            Debug.LogError($"[UIPage] Cannot navigate to {pageType} - LegacyUIManager not found!");
        }
    }
}