using UnityEngine;

public class DebugProjectiles : IDebugPage
{
    public string Title
    {
        get { return "Projectiles"; }
    }

    private Vector2 scroll1;
    private Vector2 scroll2;

    // Use this for initialization
    public void Draw()
    {
        scroll1 = GUILayout.BeginScrollView(scroll1);
        foreach (var s in ProjectileManager.Instance.AllProjectiles)
        {
            GUILayout.Label(s.Key + " - " + s.Value != null ? ProjectileManager.PrintID(s.Key) : ProjectileManager.PrintID(s.Key) + " (exploded zombie)");
        }
        GUILayout.EndScrollView();

        GUILayout.Space(30);
        scroll2 = GUILayout.BeginScrollView(scroll2);
        foreach (var s in ProjectileManager.Instance.LimitedProjectiles)
        {
            GUILayout.Label("Limited " + ProjectileManager.PrintID(s));
        }
        GUILayout.EndScrollView();
    }
}