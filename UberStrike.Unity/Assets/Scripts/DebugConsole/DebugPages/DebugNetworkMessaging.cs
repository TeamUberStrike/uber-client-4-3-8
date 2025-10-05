using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cmune.Realtime.Photon.Client;
using UnityEngine;

public class DebugNetworkMessaging : IDebugPage
{
    public DebugNetworkMessaging()
    {
        _outMessageQueue = new Queue();
        _inMessageQueue = new Queue();

        // StartCoroutine(countIncomingMessages());
        // StartCoroutine(countOutgoingMessages());
    }

    public string Title
    {
        get { return "Traffic"; }
    }

    private IEnumerator countIncomingMessages()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            //if (CmuneNetworkState.NumberOfIncomingMessages > 0)
            {
                int inPerSec = CmuneNetworkState.IncomingMessagesCount - _inTotalPackages;

                _inTotalPackages = CmuneNetworkState.IncomingMessagesCount;

                //string m = System.DateTime.Now.ToLongTimeString() + " Messages " + CmuneNetworkState.NumberOfMessages.ToString();
                _inMessageQueue.Enqueue(new MessageInfo(inPerSec));
                if (_inMessageQueue.Count > 10)
                    _inMessageQueue.Dequeue();

                _inAvgPackPerSec = 0;
                foreach (MessageInfo i in _inMessageQueue)
                    _inAvgPackPerSec += i.number;
                _inAvgPackPerSec /= _inMessageQueue.Count;

                if (_inMaxPackPerSec < inPerSec)
                    _inMaxPackPerSec = inPerSec;
            }
        }
    }

    private IEnumerator countOutgoingMessages()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            //if (CmuneNetworkState.NumberOfOutgoingMessages > 0)
            {
                int outPerSec = CmuneNetworkState.OutgoingMessagesCount - _outTotalPackages;

                _outTotalPackages = CmuneNetworkState.OutgoingMessagesCount;

                _outMessageQueue.Enqueue(new MessageInfo(outPerSec));
                if (_outMessageQueue.Count > 10)
                    _outMessageQueue.Dequeue();

                _outAvgPackPerSec = 0;
                foreach (MessageInfo i in _outMessageQueue)
                    _outAvgPackPerSec += i.number;
                _outAvgPackPerSec /= _outMessageQueue.Count;

                if (_outMaxPackPerSec < outPerSec)
                    _outMaxPackPerSec = outPerSec;
            }
        }
    }

    private class MessageInfo
    {
        public MessageInfo(int num)
        {
            number = num;
            timestamp = DateTime.Now;
        }
        public DateTime timestamp;
        public int number;
        public override string ToString()
        {
            return timestamp.ToLongTimeString() + " Messages " + number.ToString();
        }
    }

    public void Draw()
    {
        GUILayout.Label("OUT (tot):" + _outTotalPackages);
        GUILayout.Label("OUT (avg):" + _outAvgPackPerSec);
        GUILayout.Label("OUT (max):" + _outMaxPackPerSec);

        GUILayout.Label("IN (tot):" + _inTotalPackages);
        GUILayout.Label("IN (avg):" + _inAvgPackPerSec);
        GUILayout.Label("IN (max):" + _inMaxPackPerSec);

        if (GUILayout.Button("Dump To File"))
        {
            FileStream s = new FileStream("NetworkMessages.txt", FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter w = new StreamWriter(s);

            try
            {
                foreach (KeyValuePair<short, NetworkMessenger.NetworkClassInfo> info in LobbyConnectionManager.Rmi.Messenger.CallStatistics)
                {
                    foreach (KeyValuePair<byte, int> calls in info.Value._functionCalls)
                    {

                        string i = string.Format("{0}\t{1}\t{2}\t{3}", calls.Value, LobbyConnectionManager.Rmi.GetAddress(info.Key, calls.Key), info.Value.GetTotalExecutionTime(calls.Key), info.Value.GetAvarageExecutionTime(calls.Key));
                        w.WriteLine(i);
                    }
                }
            }
            finally
            {
                w.Close();
                s.Close();
            }
        }
        v1 = GUILayout.BeginScrollView(v1);
        foreach (KeyValuePair<short, NetworkMessenger.NetworkClassInfo> info in LobbyConnectionManager.Rmi.Messenger.CallStatistics)
        {
            foreach (KeyValuePair<byte, int> calls in info.Value._functionCalls)
            {

                string i = string.Format("{0} {1}: [{2}ms /{3}ms]", calls.Value, LobbyConnectionManager.Rmi.GetAddress(info.Key, calls.Key), info.Value.GetTotalExecutionTime(calls.Key), info.Value.GetAvarageExecutionTime(calls.Key));
                GUILayout.Label(i);
            }
        }
        GUILayout.EndScrollView();
    }

    Vector2 v1;

    private Queue _outMessageQueue;
    private int _outMaxPackPerSec = 0;
    private float _outAvgPackPerSec = 0;
    private int _outTotalPackages = 0;

    private Queue _inMessageQueue;
    private int _inMaxPackPerSec = 0;
    private float _inAvgPackPerSec = 0;
    private int _inTotalPackages = 0;
}