
using System.Linq;
using Cmune.Util;
using UnityEngine;
using UberStrike.Helper;

public class BezierSplines
{
    public readonly LimitedQueue<Packet> Packets = new LimitedQueue<Packet>(10);
    public Packet PreviousPacket;
    public Packet LastPacket;

    public Vector3 _velocity;
    public Vector3 _checkVelocity;
    private Vector3 _lastPosition;

    private int debugCounter = 0;
    //private int g1, g2;

#if UNITY_EDITOR
    public BezierSplines()
    {
        GameState.DrawGizmos += OnDebugGizmos;
        //        g1 = ++GLDrawGraph.GraphId;
        //        g2 = ++GLDrawGraph.GraphId;
        //        //GameState.DrawGUI += OnGUI;
        //        GLDrawGraph.AddCaption(g1, "Time Jitter");
        //        GLDrawGraph.AddCaption(g2, "Space Jitter");
    }
#endif

    public void AddSample(int serverTime, Vector3 pos)
    {
        if (LastPacket.Time < serverTime)
        {
            //#if UNITY_EDITOR
            //            GLDrawGraph.AddSampleValue(g1, (serverTime - LastPacket.Time) / 100f);
            //            GLDrawGraph.AddSampleValue(g2, Vector3.Distance(pos, LastPacket.Pos));
            //#endif
            PreviousPacket = LastPacket;
            LastPacket = new Packet(serverTime, pos, PreviousPacket.Pos - pos);
            Packets.Enqueue(LastPacket);
        }
    }

    public int ReadPosition(int time, out Vector3 oPos)
    {
        int i = 0;

        if (Packets.Count > 0)
        {
            Packet? current = null, next = null, previous = null;

            // locate the recent two packets according to time
            for (i = Packets.Count - 1; i > 0; i--)
            {
                Packet r = Packets[i];
                next = current;
                current = r;
                if (r.Time < time)
                {
                    if (!next.HasValue && i > 1)
                        previous = Packets[i - 1];
                    break;
                }
            }
            debugCounter = i;

            if (current.HasValue && next.HasValue)
            {
                /* we have two packets */
                float totalTime = next.Value.Time - current.Value.Time;
                float partTime = time - current.Value.Time;
                float span = Mathf.Clamp01(Mathf.Abs(partTime / totalTime));

                oPos = InterpolateBetween(span, current.Value, next.Value);
                _lastPosition = oPos;
            }
            else if (current.HasValue && previous.HasValue)
            {
                oPos = current.Value.Pos + GetVelocity(current.Value, previous.Value) * Mathf.Clamp(time - current.Value.Time, 0, 100);
                _lastPosition = oPos;
            }
            else
            {
                i = 0;
                oPos = _lastPosition;
            }
        }
        else
        {
            oPos = _lastPosition;
        }

        return i;
    }

    private static Vector3 GetVelocity(Packet a, Packet b)
    {
        return (a.Pos - b.Pos) / (float)Mathf.Abs(a.Time - b.Time);
    }

    public Vector3 LatestPosition()
    {
        if (Packets.Count > 0)
            return Packets.Last().Pos;
        else
            return Vector3.zero;
    }

    private Vector3 InterpolateBetween(float t, Packet a, Packet b)
    {
        return Vector3.Lerp(a.Pos, b.Pos, t);
    }

    public static Vector3 GetBSplineAt(float t, Vector3 p1, Vector3 ss1, Vector3 ss2, Vector3 p2)
    {
        Vector3 A = p2 - 3 * ss2 + 3 * ss1 - p1;
        Vector3 B = 3 * ss2 - 6 * ss1 + 3 * p1;
        Vector3 C = 3 * ss1 - 3 * p1;
        Vector3 D = p1;
        return A * Mathf.Pow(t, 3) + B * Mathf.Pow(t, 2) + C * t + D;
    }

#if UNITY_EDITOR
    private void OnDebugGizmos()
    {
        //if (GameConnectionManager.IsInitialized)
        //{
        for (int i = Packets.Count - 1; i > 0; i--)
        {
            Packet p = Packets[i];

            if (debugCounter == i)
                Gizmos.color = Color.red;
            else
                Gizmos.color = Color.yellow.SetAlpha(0.5f);

            Gizmos.DrawCube(p.Pos, Vector3.one * 0.2f);
            Gizmos.color = Color.black;
            Gizmos.DrawLine(p.Pos + p.S1, p.Pos + p.S2);
            Gizmos.DrawCube(p.Pos + p.S1, Vector3.one * 0.1f);
        }

        //Gizmos.color = Color.blue;
        //Vector3 pos;
        //ReadPosition(GameConnectionManager.Client.PeerListener.ServerTimeTicks - GameState.PredictionTimeMax, out pos);
        //Gizmos.DrawCube(pos, Vector3.one * 0.1f);
        //}
    }
#endif

    [System.Serializable]
    public struct Packet
    {
        public int Time;
        public Vector3 Pos;
        public Vector3 S1;
        public Vector3 S2;
        public float GameTime;

        public Packet(int t, Vector3 p, Vector3 prev)
        {
            Time = t;
            Pos = p;
            S1 = prev * GameState.Tangent;
            S2 = -prev * GameState.Tangent;
            GameTime = UnityEngine.Time.realtimeSinceStartup;
        }
    }
}