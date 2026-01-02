using System;
using UnityEngine;

public class TestAnimations : MonoBehaviour
{
    private Vector2 scroll;
    private Vector2 itemSize = new Vector2(220, 30);

    // Update is called once per frame
    void OnGUI()
    {
        if (GameState.LocalDecorator != null)
        {
            //scroll = GUITools.BeginScrollView(new Rect(10, 100, itemSize.x + 20, Screen.height - 110), scroll, new Rect(0, 0, itemSize.x, animation.GetClipCount() * itemSize.y));
            //{
            //    int i = 0;
            //    foreach (AnimationState clip in animation)
            //    {
            //        if (clip)
            //        {
            //            if (GUI.Button(new Rect(0, i * itemSize.y, itemSize.x, itemSize.y), "Play " + clip.name))
            //            {
            //                animation.Play(clip.name, AnimationPlayMode.Stop);
            //            }
            //        }
            //        else
            //        {
            //            GUI.Label(new Rect(0, i * itemSize.y, itemSize.x, itemSize.y), "Missing clip");
            //        }
            //        i++;
            //    }
            //}
            //GUITools.EndScrollView();

            scroll = GUITools.BeginScrollView(new Rect(1, 100, itemSize.x + 20, Screen.height - 20), scroll, new Rect(0, 0, itemSize.x, GameState.LocalDecorator.Animation.GetClipCount() * itemSize.y));
            {
                int i = 0;
                foreach (AnimationState clip in GameState.LocalDecorator.Animation)
                {
                    if (clip)
                    {
                        if (GUI.Button(new Rect(0, i * itemSize.y, itemSize.x, itemSize.y), "Play " + clip.name))
                        {
                            AnimationIndex index = (AnimationIndex)Enum.Parse(typeof(AnimationIndex), clip.name, true);
                            GameState.LocalDecorator.AnimationController.TriggerAnimation(index, stopall);
                        }
                    }
                    else
                    {
                        GUI.Label(new Rect(0, i * itemSize.y, itemSize.x, itemSize.y), "Missing clip");
                    }
                    i++;
                }
            }
            GUITools.EndScrollView();
        }
    }

    public bool stopall = false;
}
