using System;
using System.Collections;
using System.Collections.Generic;
using Cmune.Realtime.Common.Security;
using Cmune.Realtime.Photon.Client;
using UberStrike.Helper;
using UnityEngine;
using Cmune.Util;

public class CheatDetection : MonoBehaviour
{
#if !UNITY_IPHONE
    private static int _gameTime;
    private static DateTime _dateTime;

    /// <summary>
    /// We are using the difference between the System time and the 
    /// Process (Game) time to detect if a player is speed hacking.
    /// Unfortunately, sometimes they can get out of sync!
    /// Use this method to sync the system time and the process time in regular intervals.
    /// </summary>
    public static void SyncSystemTime()
    {
        _gameTime = SystemTime.Running;
        _dateTime = DateTime.Now;
    }

    public static int GameTime { get { return SystemTime.Running - _gameTime; } }
    public static int RealTime { get { return (int)((DateTime.Now - _dateTime).TotalMilliseconds); } }

    void Start()
    {
        StartCoroutine(StartNewSpeedhackDetection());
        StartCoroutine(StartCheckSecureMemory());
    }

    private IEnumerator StartCheckSecureMemory()
    {
        while (true)
        {
            try
            {
                SecureMemoryMonitor.Instance.PerformCheck();
            }
            catch
            {
                CommConnectionManager.CommCenter.OnDisconnectAndDisablePhoton("You have been disconnected. Please restart UberStrike.");
            }
            yield return new WaitForSeconds(10);
        }
    }

    private IEnumerator StartNewSpeedhackDetection()
    {
        //wait a bit until game is properly loaded
        yield return new WaitForSeconds(5);

        SyncSystemTime();

        LimitedQueue<float> timeDifference = new LimitedQueue<float>(5);

        while (true)
        {
            yield return new WaitForSeconds(5);

            if (GameState.HasCurrentGame)
            {
                timeDifference.Enqueue(GameTime / (float)RealTime);
                SyncSystemTime();

                if (timeDifference.Count == 5 && IsSpeedHacking(timeDifference))
                {
                    CommConnectionManager.CommCenter.SendSpeedhackDetection(timeDifference);
                    //CommConnectionManager.CommCenter.OnDisconnectAndDisablePhoton("You were temporarily banned for Speedhacking");
                    break;
                }
            }
        }
    }

    private bool IsSpeedHacking(IEnumerable<float> list)
    {
        int count = 0;
        float mean = 0;
        foreach (float f in list)
        {
            mean += f;
            count++;
        }
        mean /= count;

        float variance = 0;
        foreach (float f in list)
        {
            variance += Mathf.Pow(f - mean, 2);
        }
        variance *= 100;

        if (mean > 2) return true;
        else if (mean > 1.1f && variance < 0.05f) return true;
        else return false;
    }
#endif
}