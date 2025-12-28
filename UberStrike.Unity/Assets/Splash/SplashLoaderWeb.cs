#if !UNITY_ANDROID && !UNITY_IPHONE
using UnityEngine;
using System.Collections;

public class SplashLoaderWeb : MonoBehaviour
{
    [SerializeField]
    private TextAsset licenseFile;

    [SerializeField]
    private MovieTexture uberStrikeLogoMovie;

    [SerializeField]
    private GUIStyle textStyle;

    private bool isAssetBundleLoaded = false;
    private bool isError;
    private AsyncOperation asyncOperation;
    private Texture2D white;


    public static float MOVIE_RESOLUTION = 9.0f / 16.0f;
    public float Progress { get; private set; }
    public string Url { get; private set; }

    public static SplashLoaderWeb Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        if (white == null)
        {
            white = new Texture2D(1, 1, TextureFormat.RGB24, false);
            white.SetPixels(new Color[] { new Color(0.0f, 0.643f, 0.8f) });
            white.Apply(false);
        }
    }

    private IEnumerator Start()
    {
        // Record the tracking step
		if (Application.isWebPlayer)
        {
            Application.ExternalCall("wsTracking", "6");
        }

        // Check if our cache license is valid
        if (!CacheManager.RunAuthorization(licenseFile.text))
        {
            Debug.LogError("Unity Caching Authorization Failed!");
        }

        // Start playing back the UberStrike splash movie
        uberStrikeLogoMovie.Play();

        // Download the bundle from web or cache (There's no assetbundle of the Latest scene in the editor, so skip this step)
        if (!Application.isEditor)
        {
            Url = PreloaderUtil.GetMainSceneUrl();
            yield return StartCoroutine(CacheManager.LoadAssetBundle(Url, (p) => Progress = p));
            isAssetBundleLoaded = true;
        }

        // Wait for the splash movie to finish
        while (uberStrikeLogoMovie.isPlaying)
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
        if (uberStrikeLogoMovie.isPlaying)
        {
            float movieWidth = Screen.height / MOVIE_RESOLUTION;
            GUI.DrawTexture(new Rect((Screen.width - movieWidth) / 2, 0, movieWidth, Screen.height), uberStrikeLogoMovie);
        }
        else
        {
            textStyle.normal.textColor = textStyle.normal.textColor.SetAlpha(GUITools.SinusPulse);

            if (!isAssetBundleLoaded)
            {
                Vector2 textSize = textStyle.CalcSize(new GUIContent("Downloading UberStrike. Please Wait..."));
                GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "Downloading UberStrike. Please Wait...", textStyle);
                
                GUI.color = new Color(1, 1, 1, 0.5f);
                GUI.DrawTexture(new Rect((Screen.width * 0.5f) - (textSize.x * 0.5f), (Screen.height * 0.5f) + textSize.y + 8, textSize.x, 8), white);
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect((Screen.width * 0.5f) - (textSize.x * 0.5f), (Screen.height * 0.5f) + textSize.y + 8, Mathf.RoundToInt(Progress * textSize.x), 8), white);
            }
            else
            {
                if (!isError && asyncOperation != null)
                {
                    Vector2 textSize = textStyle.CalcSize(new GUIContent("Initializing UberStrike. Please Wait..."));
                    GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "Initializing UberStrike. Please Wait...", textStyle);

                    GUI.color = new Color(1, 1, 1, 0.5f);
                    GUI.DrawTexture(new Rect((Screen.width * 0.5f) - (textSize.x * 0.5f), (Screen.height * 0.5f) + textSize.y + 8, textSize.x, 8), white);
                    GUI.color = Color.white;
                    GUI.DrawTexture(new Rect((Screen.width * 0.5f) - (textSize.x * 0.5f), (Screen.height * 0.5f) + textSize.y + 8, Mathf.RoundToInt(asyncOperation.progress * textSize.x), 8), white);
                }
                else
                {
                    GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "There was a problem loading UberStrike, please try refreshing the page.", textStyle);
                }
            }
        }
    }
}
#endif