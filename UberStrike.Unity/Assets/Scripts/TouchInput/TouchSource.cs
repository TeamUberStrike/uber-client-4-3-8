using System.Collections.Generic;
using UnityEngine;

public static class TouchSource
{
#if UNITY_WEBGL && !UNITY_EDITOR
    // Unity's WebGL input does not behave like the native (iOS/Android) touch input:
    // TouchPhase.Stationary is never delivered, Touch.deltaPosition is wrong (absolute position
    // on the first frame, inverted afterwards), resting fingers vanish from Input.touches, and
    // the fingerIds are renumbered whenever a finger is removed (the finger that stays down
    // inherits the removed finger's id and Unity re-delivers a Began for it).
    //
    // To make WebGL behave exactly like Android/iOS, rebuild a native-like touch stream here:
    // every active finger is tracked by position, keeps a stable self-assigned id, reports
    // Stationary every frame it rests, and ends reliably when it is removed. All touch controls
    // then run the identical code on every platform.
    private const float MatchDistSqr = 120f * 120f;

    private static readonly List<TrackedFinger> _fingers = new List<TrackedFinger>();
    private static readonly List<Touch> _touches = new List<Touch>();
    private static readonly HashSet<int> _seenRawIds = new HashSet<int>();
    private static Touch[] _touchesArray = new Touch[0];
    private static int _builtFrame = -1;
    private static int _nextFingerId = 1;

    public static Touch[] Touches
    {
        get
        {
            if (Time.frameCount != _builtFrame)
            {
                _builtFrame = Time.frameCount;
                Rebuild();
            }
            return _touchesArray;
        }
    }
#else
    public static Touch[] Touches
    {
        get
        {
#if UNITY_EDITOR
            if (EditorTouchSimulator.Active)
                return EditorTouchSimulator.Touches;
#endif
            return Input.touches;
        }
    }
#endif

    public static int TouchCount
    {
        get { return Touches.Length; }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private static void Rebuild()
    {
        for (int i = 0; i < _fingers.Count; i++)
        {
            _fingers[i].SawRawEvent = false;
            _fingers[i].EmitDelta = Vector2.zero;
            _fingers[i].IsNewThisFrame = false;
        }

        _touches.Clear();

        bool reBeganSeen = false;

        // Pass 1: releases first, so a raw id that is re-assigned to a surviving finger on this
        // frame can never be matched against the removed finger.
        foreach (Touch raw in Input.touches)
        {
            if (raw.phase != TouchPhase.Ended && raw.phase != TouchPhase.Canceled)
                continue;

            int idx = MatchRawId(raw.fingerId);
            if (idx < 0)
                idx = Match(raw.position);

            if (idx >= 0)
            {
                _touches.Add(MakeTouch(_fingers[idx].Id, _fingers[idx].Pos, Vector2.zero, TouchPhase.Ended));
                _fingers.RemoveAt(idx);
            }
        }

        // Pass 2: new and continuing fingers.
        foreach (Touch raw in Input.touches)
        {
            if (raw.phase == TouchPhase.Ended || raw.phase == TouchPhase.Canceled)
                continue;

            int idx = MatchRawId(raw.fingerId);
            if (idx >= 0 && (_fingers[idx].Pos - raw.position).sqrMagnitude >= MatchDistSqr)
            {
                // The raw id is known, but the finger it belonged to is far away: Unity
                // re-assigned the removed finger's id to a different finger. End the old owner
                // and re-match by position below.
                _touches.Add(MakeTouch(_fingers[idx].Id, _fingers[idx].Pos, Vector2.zero, TouchPhase.Ended));
                _fingers.RemoveAt(idx);
                idx = -1;
            }

            if (idx < 0 && _seenRawIds.Contains(raw.fingerId))
            {
                // A Began or Moved under a renumbered (previously seen) id: match by position.
                idx = Match(raw.position);
            }

            if (idx >= 0)
            {
                TrackedFinger f = _fingers[idx];
                f.SawRawEvent = true;
                f.EmitDelta += raw.position - f.Pos;
                f.Pos = raw.position;
                f.RawId = raw.fingerId;

                if (raw.phase == TouchPhase.Began)
                {
                    // Unity re-delivered the Began for a finger that is already down (it reorders
                    // the touch array when another finger is removed). The finger itself is
                    // unchanged, so no Began may reach the controls.
                    reBeganSeen = true;
                }
            }
            else
            {
                // A genuinely new finger. Its raw id has never been seen, so it is NOT matched
                // by position: a new finger placed close to an existing one must stay its own
                // finger (e.g. D-pad and jump button, or aim and fire fingers).
                _seenRawIds.Add(raw.fingerId);

                TrackedFinger f = new TrackedFinger();
                f.Id = _nextFingerId++;
                f.Pos = raw.position;
                f.RawId = raw.fingerId;
                f.SawRawEvent = true;
                f.IsNewThisFrame = true;
                _fingers.Add(f);
                _touches.Add(MakeTouch(f.Id, raw.position, Vector2.zero, TouchPhase.Began));
            }
        }

        if (reBeganSeen)
        {
            // Unity re-delivered Begans for the fingers that stayed down; a tracked finger that
            // got no raw event this frame is the one that was removed. End it like native would.
            for (int i = _fingers.Count - 1; i >= 0; i--)
            {
                if (!_fingers[i].SawRawEvent)
                {
                    _touches.Add(MakeTouch(_fingers[i].Id, _fingers[i].Pos, Vector2.zero, TouchPhase.Ended));
                    _fingers.RemoveAt(i);
                }
            }
        }

        for (int i = 0; i < _fingers.Count; i++)
        {
            TrackedFinger f = _fingers[i];

            // A finger created this frame already has its Began entry; native reports exactly
            // one entry per finger per frame, so emit nothing else for it.
            if (f.IsNewThisFrame)
                continue;

            if (f.SawRawEvent && f.EmitDelta != Vector2.zero)
            {
                _touches.Add(MakeTouch(f.Id, f.Pos, f.EmitDelta, TouchPhase.Moved));
            }
            else
            {
                _touches.Add(MakeTouch(f.Id, f.Pos, Vector2.zero, TouchPhase.Stationary));
            }
        }

        // Native Unity lists touches ordered by finger start order (fingerIds are assigned in
        // that order), so within one frame the Ended of an older finger always precedes the
        // Began of a newer finger. Replicate that: otherwise a quick lift + re-touch binds the
        // new finger to the wrong slot - the shooter would treat a new look finger as the fire
        // finger because the old look finger is released only afterwards.
        _touches.Sort(CompareTouches);

        _touchesArray = _touches.ToArray();
    }

    private static int CompareTouches(Touch a, Touch b)
    {
        if (a.fingerId != b.fingerId)
            return a.fingerId.CompareTo(b.fingerId);

        return PhaseRank(a.phase).CompareTo(PhaseRank(b.phase));
    }

    private static int PhaseRank(TouchPhase phase)
    {
        switch (phase)
        {
            case TouchPhase.Began: return 0;
            case TouchPhase.Moved: return 1;
            case TouchPhase.Stationary: return 2;
            default: return 3;
        }
    }

    private static int Match(Vector2 position)
    {
        int best = -1;
        float bestDist = MatchDistSqr;
        for (int i = 0; i < _fingers.Count; i++)
        {
            float dist = (_fingers[i].Pos - position).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = i;
            }
        }
        return best;
    }

    private static int MatchRawId(int rawId)
    {
        for (int i = 0; i < _fingers.Count; i++)
        {
            if (_fingers[i].RawId == rawId) return i;
        }
        return -1;
    }

    private static Touch MakeTouch(int fingerId, Vector2 position, Vector2 delta, TouchPhase phase)
    {
        Touch touch = new Touch();
        touch.fingerId = fingerId;
        touch.position = position;
        touch.rawPosition = position;
        touch.deltaPosition = delta;
        touch.deltaTime = Time.unscaledDeltaTime;
        touch.tapCount = 1;
        touch.phase = phase;
        touch.type = TouchType.Direct;
        touch.pressure = 1f;
        touch.maximumPossiblePressure = 1f;
        touch.radius = 1f;
        touch.radiusVariance = 0f;
        touch.altitudeAngle = 0f;
        touch.azimuthAngle = 0f;
        return touch;
    }

    private class TrackedFinger
    {
        public int Id;
        public int RawId;
        public Vector2 Pos;
        public Vector2 EmitDelta;
        public bool SawRawEvent;
        public bool IsNewThisFrame;
    }
#endif
}
