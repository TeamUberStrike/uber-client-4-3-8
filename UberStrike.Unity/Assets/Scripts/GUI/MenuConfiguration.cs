using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds data about pages available from the menu
/// Pages handled:
/// * Home Page
/// * Stats Page
/// * Shop Page
/// Provides methods to fetch the following data about a page:
/// * Camera View points
/// * Avatar Anchor points
/// * Avatar Spawn points
/// </summary>
public class MenuConfiguration : MonoSingleton<MenuConfiguration>
{
    #region Inspector Variables

    [SerializeField]
    private PageViewPoint[] viewPoints;

    [SerializeField]
    private PageViewPoint[] anchorPoints;

    [SerializeField]
    private PagePanelWidth[] panelWidths;

    #endregion

    #region Members

    private IDictionary<PageType, Transform> viewPointByPage = new Dictionary<PageType, Transform>();
    private IDictionary<PageType, Transform> anchorPointByPage = new Dictionary<PageType, Transform>();
    private IDictionary<PageType, int> panelWidthByPage = new Dictionary<PageType, int>();

    #endregion

    #region Internal classes

    [System.Serializable]
    public class PageViewPoint
    {
        public PageType PageType;
        public Transform ViewPoint;
    }

    [System.Serializable]
    public class PagePanelWidth
    {
        public PageType PageType;
        public int Width;
    }

    #endregion

    #region Private Methods

    private void Awake()
    {
        InitAnchorPoints();
        InitViewPoints();
        InitPanelWidths();
    }

    private void InitAnchorPoints()
    {
        foreach (var p in anchorPoints)
        {
            anchorPointByPage[p.PageType] = p.ViewPoint;
        }
    }

    private void InitViewPoints()
    {
        foreach (var p in viewPoints)
        {
            viewPointByPage[p.PageType] = p.ViewPoint;
        }
    }

    private void InitPanelWidths()
    {
        foreach (var p in panelWidths)
        {
            panelWidthByPage[p.PageType] = p.Width;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Checks if page have a viewpoint
    /// </summary>
    /// <param name="type"></param>
    /// <returns>True, if page have a viewpoint</returns>
    public bool PageHaveViewPoint(PageType type)
    {
        return viewPointByPage.ContainsKey(type);
    }

    /// <summary>
    /// Checks if page have a panel
    /// </summary>
    /// <param name="type"></param>
    /// <returns>True, if page have a panel</returns>
    public bool PageHavePanel(PageType type)
    {
        return panelWidthByPage.ContainsKey(type);
    }

    /// <summary>
    /// Gets the camera viewpoint for a page
    /// </summary>
    /// <param name="type"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <returns>True, if view points were successfully set</returns>
    public bool GetPageViewPoint(PageType type, out Vector3 position, out Quaternion rotation)
    {
        Transform transform;
        if (viewPointByPage.TryGetValue(type, out transform))
        {
            position = transform.position;
            rotation = transform.rotation;
        }
        else
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
        }

        return transform != null;
    }

    /// <summary>
    /// Gets the spawn point for a page
    /// </summary>
    /// <param name="type"></param>
    /// <param name="point"></param>
    /// <returns>True, if spawn point were successfully set</returns>
    public bool GetPageSpawnPoint(PageType type, out SpawnPoint point)
    {
        Transform transform;
        if (anchorPointByPage.TryGetValue(type, out transform))
        {
            point = transform.GetComponent<SpawnPoint>();
        }
        else
        {
            point = null;
        }

        return point != null;
    }

    /// <summary>
    /// Gets the avatar anchor point for a page
    /// </summary>
    /// <param name="type"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <returns>True, if page anchoir points were successfully set</returns>
    public bool GetPageAnchorPoint(PageType type, out Vector3 position, out Quaternion rotation)
    {
        Transform transform;
        if (anchorPointByPage.TryGetValue(type, out transform))
        {
            position = transform.position;
            rotation = transform.rotation;
        }
        else
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
        }

        return transform != null;
    }

    /// <summary>
    /// Gets the panel width for a page
    /// </summary>
    /// <param name="type"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <returns>True, if page panel width was successfully set</returns>
    public int GetPagePanelWidth(PageType type)
    {
        int width;
        if (panelWidthByPage.TryGetValue(type, out width))
        {
            return width;
        }

        return 0;
    }

    #endregion
}
