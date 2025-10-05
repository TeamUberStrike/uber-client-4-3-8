using UnityEngine;

public class DebugAnimation : IDebugPage
{
    public string Title
    {
        get { return "Animation"; }
    }

    // Use this for initialization
    public void Draw()
    {
        if (GameState.HasCurrentGame)
        {
            GUILayout.BeginHorizontal();
            foreach (var c in GameState.CurrentGame.AllCharacters)
            {
                if (GUILayout.Button(c.name))
                    config = c;
            }
            GUILayout.EndHorizontal();

            if (config == null)
                GUILayout.Label("Select a player");
            else if (config.Decorator == null)
                GUILayout.Label("Missing Decorator");
            else if (config.Decorator.AnimationController == null)
                GUILayout.Label("Missing Animation");
            else
                GUILayout.Label(config.Decorator.AnimationController.GetDebugInfo());
        }
        else
        {
            GUILayout.Label("No Game Running");
        }
    }

    CharacterConfig config;
}
