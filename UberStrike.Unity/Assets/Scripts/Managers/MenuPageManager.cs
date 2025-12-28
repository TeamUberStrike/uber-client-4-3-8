using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles loading of pages and camera transition
/// from and to pages camera view points
/// Also handled animation of cameras rendering pixel rect
/// </summary>
public class MenuPageManager : MonoSingleton<MenuPageManager>
{
    #region Member Variables

    private static IDictionary<PageType, PageScene> _pageByPageType;
    private static PageType _currentPageType = PageType.None;

    private EaseType _transitionType = EaseType.InOut;

    #endregion

    #region Private Methods

    private void Awake()
    {
        _pageByPageType = new Dictionary<PageType, PageScene>();
    }

    private void Start()
    {
        foreach (var p in GetComponentsInChildren<PageScene>(true))
        {
            _pageByPageType.Add(p.PageType, p);
        }
    }

    private void OnGUI()
    {
        int width = MenuConfiguration.Exists ? MenuConfiguration.Instance.GetPagePanelWidth(_currentPageType) : 0;

        // Make sure camera pixel rect is always correct if window gets resized
        if (GameState.HasCurrentSpace && IsScreenResolutionChanged() )
            GameState.CurrentSpace.Camera.pixelRect = new Rect(0, 0, Screen.width - width, Screen.height);
    }

    /// <summary>
    /// Moves the camera to the a page camera viewpoint
    /// </summary>
    /// <param name="newPage">New page to be loaded</param>
    /// <param name="time">Transition time in milliseconds</param>
    /// <returns></returns>
    private IEnumerator StartPageTransition(PageScene newPage, float time)
    {
        newPage.Load();

        Vector3 endPosition;
        Quaternion endRotation;
        // Only do page transition animation if the new page have a viewpoint
        if (MenuConfiguration.Instance.GetPageViewPoint(newPage.PageType, out endPosition, out endRotation))
        {
            Vector3 startPosition = GameState.CurrentSpace.Camera.transform.position;
            Quaternion startRotation = GameState.CurrentSpace.Camera.transform.rotation;

            float t = 0;
            while (t < time && newPage.PageType == _currentPageType)
            {
                t += Time.deltaTime;
                GameState.CurrentSpace.Camera.transform.position = Vector3.Lerp(startPosition, endPosition, Mathfx.Ease((t / time), _transitionType));
                GameState.CurrentSpace.Camera.transform.rotation = Quaternion.Lerp(startRotation, endRotation, Mathfx.Ease((t / time), _transitionType));
                yield return new WaitForEndOfFrame();
            }

            if (newPage.PageType == _currentPageType)
            {
                GameState.CurrentSpace.Camera.transform.position = endPosition;
                GameState.CurrentSpace.Camera.transform.rotation = endRotation;

                MouseOrbit.Instance.enabled = newPage.HaveMouseOrbitCamera;
            }
        }
        //else
        //{
        //    Debug.LogWarning("No Viewoint defined for Page of type " + newPage.PageType);
        //}
    }

    /// <summary>
    /// Animates the rendering of cameras pixel rect
    /// From the current page pixelrect to the page to be loaded pixel rect
    /// </summary>
    /// <param name="type"></param>
    /// <param name="time"></param>
    /// <returns></returns>
    private IEnumerator AnimateCameraPixelRect(PageType type, float time)
    {
        float t = 0;
        float oldCameraWidth = GameState.CurrentSpace.Camera.pixelWidth;
        int panelWidth = MenuConfiguration.Instance.GetPagePanelWidth(type);

        while (t < time && type == _currentPageType)
        {
            t += Time.deltaTime;
            GameState.CurrentSpace.Camera.pixelRect = new Rect(0, 0, Mathf.Lerp(oldCameraWidth, Screen.width - panelWidth, (t / time) * (t / time)), Screen.height);
            yield return new WaitForEndOfFrame();
        }

        GameState.CurrentSpace.Camera.pixelRect = new Rect(0, 0, Screen.width - MenuConfiguration.Instance.GetPagePanelWidth(_currentPageType), Screen.height);
    }

    #endregion

    #region Public Methods

    public static bool IsCurrentPage(PageType type)
    {
        return _currentPageType == type;
    }

    public static PageType GetCurrentPage()
    {
        return _currentPageType;
    }

    /// <summary>
    /// Unloads the current page
    /// </summary>
    public void UnloadCurrentPage()
    {
        PageScene currentPage;
        if (_pageByPageType.TryGetValue(_currentPageType, out currentPage))
        {
            if (currentPage)
            {
                currentPage.Unload();
                _currentPageType = PageType.None;
                MouseOrbit.Instance.enabled = false;
            }
        }
    }

    /// <summary>
    /// Loads a page and executes an animated camera transition to that page
    /// </summary>
    /// <param name="pageType"></param>
    /// <param name="forceReload">If true the page will reload and replay the camera animation eventhough we are already on that page</param>
    public void LoadPage(PageType pageType, bool forceReload = false)
    {
        if (GameState.HasCurrentGame)
        {
            ShowLeaveGameWarning(pageType);
        }
        else
        {
            PanelManager.Instance.CloseAllPanels();

            //make sure to always load the default menu space
            UberstrikeMap map = LevelManager.Instance.GetMapWithId(0);
            if (map.IsLoaded && GameState.CurrentSpace != map.Space)
                GameState.SetCurrentSpace(map.Space);

            if (pageType == _currentPageType && !forceReload) return;

            PageScene newPage = null;
            if (_pageByPageType.TryGetValue(pageType, out newPage))
            {
                PageScene currentPage = null;
                _pageByPageType.TryGetValue(_currentPageType, out currentPage);

                if (currentPage && !forceReload)
                {
                    currentPage.Unload();
                }
                _currentPageType = pageType;

                if (MenuConfiguration.Instance.PageHavePanel(newPage.PageType))
                {
                    StartCoroutine(AnimateCameraPixelRect(newPage.PageType, 0.25f));
                }

                MouseOrbit.Instance.enabled = false;
                Instance.StartCoroutine(StartPageTransition(newPage, 1.0f));
            }
        }
    }

    private void ShowLeaveGameWarning(PageType page)
    {
        if (GameState.CurrentGame.IsGameStarted)
        {
            PopupSystem.ShowMessage(
                            LocalizedStrings.LeavingGame,
                            LocalizedStrings.LeaveGameWarningMsg,
                            PopupSystem.AlertType.OKCancel,
                            () => GameStateController.Instance.UnloadLevelAndLoadPage(page),
                            LocalizedStrings.LeaveCaps,
                            null, LocalizedStrings.CancelCaps,
                            PopupSystem.ActionType.Negative);
        }
        else
        {
            GameStateController.Instance.UnloadLevelAndLoadPage(page);
        }
    }

    private bool IsScreenResolutionChanged()
    {
        if (Screen.width != _lastScreenWidth ||
             Screen.height != _lastScreenHeight)
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            return true;
        }
        return false;
    }

    private int _lastScreenWidth;
    private int _lastScreenHeight;

    #endregion
}