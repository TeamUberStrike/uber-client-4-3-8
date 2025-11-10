using UnityEngine;

public class DebugMaps : IDebugPage
{
    public string Title
    {
        get { return "Maps"; }
    }

    private Vector2 scroll;

    // Use this for initialization
    public void Draw()
    {
        scroll = GUILayout.BeginScrollView(scroll);
        foreach (var map in LevelManager.Instance.AllMaps)
        {
            GUILayout.Label(map.Id + ", Modes: " + map.View.SupportedGameModes + ", Loaded: " + map.IsLoaded + ", Max: " + (map.Space != null) + ", Item: " + map.View.RecommendedItemId + ", Scene: " + map.SceneName + ", File: " + map.FileName);
        }
        GUILayout.EndScrollView();
    }
}