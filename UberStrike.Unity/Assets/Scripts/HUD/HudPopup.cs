
using System.Collections.Generic;
using UnityEngine;

public class HudPopup : Singleton<HudPopup>
{
    private Queue<HudMessage> queue = new Queue<HudMessage>();

    private const int ShowTime = 3;
    private const int FadeTime = 1;

    private HudPopup()
    { }

    public void Show(string text, Texture2D icon)
    {
        UnityRuntime.Instance.OnGui += OnGUI;
        queue.Enqueue(new HudMessage()
            {
                Text = text,
                Texture = icon,
                Time = Time.time + ShowTime
            });
    }

    private void OnGUI()
    {
        if (queue.Count > 0)
        {
            if (queue.Peek().Time > Time.time)
            {
                var popup = queue.Peek();
                Vector2 size = new Vector2(260, 50);

                GUI.color = popup.Color;
                GUI.BeginGroup(new Rect(0, Screen.height * 0.5f, size.x, size.y), BlueStonez.window);
                {
                    GUI.Label(new Rect(10, 5, size.x - popup.IconWidth(size.y) - 20, size.y - 10), popup.Text, BlueStonez.label_interparkbold_13pt_left);
                    GUI.DrawTexture(new Rect(size.x - popup.IconWidth(size.y), 0, popup.IconWidth(size.y), size.y), popup.Texture);
                }
                GUI.EndGroup();
                GUI.color = Color.white;
            }
            else
            {
                queue.Dequeue();
                if (queue.Count > 0)
                    queue.Peek().Time = Time.time + ShowTime;
            }
        }
        else
        {
            UnityRuntime.Instance.OnGui -= OnGUI;
        }
    }

    private class HudMessage
    {
        public Texture2D Texture;
        public string Text;
        public float Time;

        public float Alpha
        {
            get { return Mathf.Lerp(1, 0, UnityEngine.Time.time - Time + FadeTime); }
        }

        public Color Color
        {
            get { return new Color(1, 1, 1, Alpha); }
        }

        public float IconWidth(float height)
        {
            if (Texture != null)
            {
                float ratio = Texture.width / (float)Texture.height;
                return ratio * height;
            }
            else
            {
                return 0;
            }
        }
    }
}