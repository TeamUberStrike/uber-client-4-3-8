using System;
using System.Collections;
using System.IO;
using Cmune.Realtime.Photon.Client;
using UnityEngine;
using UnityEngine.Networking;

namespace UberStrike.Realtime.NakamaAdapter
{
    using Cmune.Realtime.Photon.Client.Transport;

    // Installs the Nakama transport before any connection manager Awake. No Photon fallback.
    public static class NakamaBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            NakamaSession.ResetStatics();
            NakamaConfig.Current = null;
            PeerFactory.Create = CreatePeer;
#if UNITY_WEBGL && !UNITY_EDITOR
            _webglConfigLoading = false;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (NakamaConfig.Current == null)
                NakamaConfig.Current = LoadConfig();

#if UNITY_WEBGL && !UNITY_EDITOR
            BeginLoadWebGLConfig();
#endif

            PeerFactory.Create = CreatePeer;
            NakamaSession.IdentitySource = CurrentPlayer;
            NakamaSession.SessionReplaced = DisableRealtime;
            Debug.Log("[nakama] " + NakamaConfig.Current);
        }

        public static INetworkPeer CreatePeer()
        {
            return new NakamaPeer(NakamaSession.Instance);
        }

        public static NakamaConfig LoadConfig()
        {
            string[] args = null;
            string json = null;
            try
            {
                args = Environment.GetCommandLineArgs();
#if !UNITY_WEBGL || UNITY_EDITOR
                string path = Path.Combine(Application.streamingAssetsPath, "nakama.json");
#if UNITY_ANDROID && !UNITY_EDITOR
                using (UnityWebRequest request = UnityWebRequest.Get(path))
                {
                    request.timeout = 10;
                    UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                    while (!operation.isDone)
                    {
                    }
                    if (request.isNetworkError || request.isHttpError)
                        throw new IOException("nakama.json: " + request.error);
                    json = request.downloadHandler.text;
                }
#else
                if (File.Exists(path))
                    json = File.ReadAllText(path);
#endif
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning("[nakama] config read: " + e.Message);
            }

            try
            {
                return NakamaConfig.Load(json, args);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[nakama] bad nakama.json, using defaults: " + e.Message);
                return NakamaConfig.Load(null, args);
            }
        }

        static NakamaIdentity CurrentPlayer()
        {
            if (!PlayerDataManager.IsPlayerLoggedIn)
                return new NakamaIdentity();
            return new NakamaIdentity
            {
                Cmid = PlayerDataManager.CmidSecure,
                Access = (int)PlayerDataManager.AccessLevelSecure,
                Name = PlayerDataManager.NameSecure,
            };
        }

        // same path as CommRPC 36 DisconnectAndDisablePhoton
        static void DisableRealtime(string message)
        {
            ClientCommCenter comm = CommConnectionManager.CommCenter;
            if (comm != null)
                comm.OnDisconnectAndDisablePhoton(message);
            else
                PhotonClient.IsPhotonEnabled = false;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        static bool _webglConfigLoading;

        static void BeginLoadWebGLConfig()
        {
            if (_webglConfigLoading)
                return;
            _webglConfigLoading = true;
            MonoRoutine.Start(LoadWebGLConfig());
        }

        static IEnumerator LoadWebGLConfig()
        {
            string path = Application.streamingAssetsPath + "/nakama.json";
            using (UnityWebRequest request = UnityWebRequest.Get(path))
            {
                request.timeout = 10;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("[nakama] config read: " + request.error);
                }
                else
                {
                    try
                    {
                        NakamaConfig loaded = NakamaConfig.Load(request.downloadHandler.text, Environment.GetCommandLineArgs());
                        if (NakamaConfig.Current != null)
                            NakamaConfig.Current.CopyFrom(loaded);
                        else
                            NakamaConfig.Current = loaded;
                        Debug.Log("[nakama] " + NakamaConfig.Current);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[nakama] bad nakama.json: " + e.Message);
                    }
                }
            }
        }
#endif
    }
}
