using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles loading of pages and camera transition
/// from and to pages camera view points
/// Also handled animation of cameras rendering pixel rect
/// </summary>
public class GamePageManager : MonoSingleton<GamePageManager>
{
    #region Member Variables

    private static IDictionary<PageType, PageScene> _pageByPageType;
    private static PageType _currentPageType = PageType.None;

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

    #endregion

    #region Public Methods

    public static bool IsCurrentPage(PageType type)
    {
        return _currentPageType == type;
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
            }
        }
    }

    /// <summary>
    /// Loads a page and executes an animated camera transition to that page
    /// </summary>
    /// <param name="pageType"></param>
    /// <param name="forecReload">If true the page will reload and replay the camera animation eventhough we are already on that page</param>
    public void LoadPage(PageType pageType)
    {
        if (pageType == _currentPageType) return;

        PageScene newPage = null;
        if (_pageByPageType.TryGetValue(pageType, out newPage))
        {
            PageScene currentPage = null;
            _pageByPageType.TryGetValue(_currentPageType, out currentPage);

            if (currentPage)
            {
                currentPage.Unload();
            }
            _currentPageType = pageType;

            newPage.Load();
        }
    }

    #endregion
}