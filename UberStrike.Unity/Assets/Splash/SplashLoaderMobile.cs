using UnityEngine;
using System.Collections;

class SplashLoaderMobile : MonoBehaviour
{
    [SerializeField]
    private TextAsset licenseFile;

    [SerializeField]
    private string movieFilename;

    [SerializeField]
    private GUIStyle textStyle;

    [SerializeField]
    private Texture2D backgroundTexture;

    private AsyncOperation asyncOperation;
    private ProgressPopupDialog progressDialog;
    private bool movieDone;

    public static SplashLoaderMobile Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        movieDone = false;
    }

    private IEnumerator Start()
    {
        // Check if our cache license is valid
        if (!CacheManager.RunAuthorization(licenseFile.text))
        {
            Debug.LogError("Unity Caching Authorization Failed!");
        }
		
        // Start playing back the UberStrike splash movie
        Handheld.PlayFullScreenMovie(movieFilename, Color.black, FullScreenMovieControlMode.CancelOnInput);
		
		// Load the Main Scene
        StartCoroutine(LoadMainScene());

        yield return new WaitForEndOfFrame();

        movieDone = true;

        GUITools.UpdateScreenSize();
        progressDialog = new ProgressPopupDialog("Initializing", "Initializing UberStrike...", null, true);
        PopupSystem.Show(progressDialog);
    }

    private IEnumerator LoadMainScene()
    {
        // Load the level in the background
        asyncOperation = Application.LoadLevelAdditiveAsync("Latest");

        // Wait until the loading process starts
        if (asyncOperation != null)
        {
            while (!asyncOperation.isDone)
            {
                yield return new WaitForEndOfFrame();
            }

            // destroy this object, others will take over
            GameObject.Destroy(gameObject);
        }
        else
        {
            PopupSystem.HideMessage(progressDialog);
            PopupSystem.ShowMessage("Error", "A problem with UberStrike has occured. Please restart the app and try again.");
            Debug.LogError("Main Scene could not be loaded.");
        }
    }

    private void OnGUI()
    {
        if (movieDone)
        {
            GUI.depth = 10;
            GUI.DrawTexture(new Rect(0, 0, backgroundTexture.width, backgroundTexture.height), backgroundTexture);
        }
    }
}

