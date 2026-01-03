using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class SplashLoader : MonoBehaviour
{
#if !UNITY_ANDROID && !UNITY_IPHONE
    [SerializeField]
    private TextAsset licenseFile;

    [SerializeField]
    private VideoClip uberStrikeLogoMovie;

    [SerializeField]
    private GUIStyle textStyle;

    private bool isError;
    private AsyncOperation asyncOperation;
    private Texture2D white;
    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;

    public float Progress { get; private set; }
    public string Url { get; private set; }

    public static SplashLoader Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        // Set the player to one minus the current screen resolution
        ScreenResolutionManager.SetTwoMinusMaxResolution();

        if (white == null)
        {
            white = new Texture2D(1, 1, TextureFormat.RGB24, false);
            white.SetPixels(new Color[] { new Color(0.0f, 0.643f, 0.8f) });
            white.Apply(false);
        }
    }

    private IEnumerator Start()
    {
        // Check if our cache license is valid
        if (!CacheManager.RunAuthorization(licenseFile.text))
        {
            Debug.LogError("Unity Caching Authorization Failed!");
        }

        // Setup VideoPlayer for splash movie
        if (uberStrikeLogoMovie != null)
        {
            // Create VideoPlayer component
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            
            // Create render texture
            renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            
            // Configure video playback
            videoPlayer.clip = uberStrikeLogoMovie;
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.Play();
        }
        
        // Download the bundle from web or cache
        if (!Application.isEditor)
        {
            Url = PreloaderUtil.GetMainSceneUrl();
            yield return StartCoroutine(CacheManager.LoadAssetBundle(Url, (p) => Progress = p));
        }

        // Wait for video to finish or fallback duration
        float splashStartTime = Time.time;
        while (Time.time - splashStartTime < 3.0f && (videoPlayer == null || videoPlayer.isPlaying))
        {
            yield return new WaitForEndOfFrame();
        }

        // Load the Main Scene
        yield return StartCoroutine(LoadMainScene());
    }

    private IEnumerator LoadMainScene()
    {
        // Load the level in the background
        asyncOperation = Application.LoadLevelAsync("Latest");

        // Wait until the loading process starts
        if (asyncOperation != null)
        {
            while (!asyncOperation.isDone)
            {
                yield return new WaitForEndOfFrame();
            }
        }
        else
        {
            isError = true;
        }
    }

    private void OnGUI()
    {
        // Draw video using RenderTexture
        if (renderTexture != null)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), renderTexture);
        }
        else if (uberStrikeLogoMovie != null)
        {
            float alpha = (Mathf.Sin(Time.time * 2) + 1.3f) * 0.5f;
            textStyle.normal.textColor = textStyle.normal.textColor.SetAlpha(alpha);
            if (!isError)
            {
                if (asyncOperation != null)
                {
                    Vector2 textSize = textStyle.CalcSize(new GUIContent("Initializing UberStrike. Please Wait..."));
                    GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "Initializing UberStrike. Please Wait...", textStyle);

                    GUI.color = new Color(1, 1, 1, 0.5f);
                    GUI.DrawTexture(new Rect((Screen.width * 0.5f) - (textSize.x * 0.5f), (Screen.height * 0.5f) + textSize.y + 8, textSize.x, 8), white);
                    GUI.color = Color.white;
                    GUI.DrawTexture(new Rect((Screen.width * 0.5f) - (textSize.x * 0.5f), (Screen.height * 0.5f) + textSize.y + 8, Mathf.RoundToInt(asyncOperation.progress * textSize.x), 8), white);
                }
            }
            else
            {
                if (GUI.Button(new Rect(0, 0, Screen.width, Screen.height), "There was a problem loading UberStrike, please contact support@cmune.com.\nClick here to Exit.", textStyle))
                {
                    PreloaderUtil.StartQuitOnLoadError();
                }
            }
        }
    }
#endif
}
