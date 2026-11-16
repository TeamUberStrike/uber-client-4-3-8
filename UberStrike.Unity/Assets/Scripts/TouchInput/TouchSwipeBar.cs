using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cmune.Util;
using System;

public class TouchSwipeBar : TouchControl
{
    private bool enabled = false;
    public override bool Enabled
    {
        get { return enabled; }
        set
        {
            if (value != enabled)
            {
                enabled = value;
                if (!enabled)
                {
                    finger.Reset();
                }
            }
        }
    }

    public bool Active
    {
        get
        {
            return Enabled && finger.FingerId != -1;
        }
    }

    public int SwipeThreshold = 60;

    public GUIContent Content;
    public GUIStyle Style;

    public event Action OnSwipeUp;
    public event Action OnSwipeDown;

    public TouchSwipeBar()
        : base()
    {
        OnTouchBegan += OnSwipeBarTouchBegan;
        OnTouchMoved += OnSwipeBarTouchMoved;
    }

    void OnSwipeBarTouchBegan(Vector2 obj)
    {
        _touchStartPos = finger.StartPos;
    }

    void OnSwipeBarTouchMoved(Vector2 pos, Vector2 delta)
    {
        if (_touchStartPos.y - pos.y > SwipeThreshold)
        {
            _touchStartPos = pos;
            if (OnSwipeDown != null) OnSwipeDown();
        }
        else if (_touchStartPos.y - pos.y < -SwipeThreshold)
        {
            _touchStartPos = pos;
            if (OnSwipeUp != null) OnSwipeUp();
        }
    }

    public TouchSwipeBar(Texture tex)
        : this()
    {
        this.Content = new GUIContent(tex);
    }

    public override void Draw()
    {
        if (Content != null)
        {
            GUI.Label(Boundary, Content);
        }
    }

    private Vector2 _touchStartPos;
}

