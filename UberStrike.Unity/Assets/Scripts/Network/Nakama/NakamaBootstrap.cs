using System;
using System.IO;
using Cmune.Realtime.Photon.Client;
using UnityEngine;

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
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (NakamaConfig.Current == null)
                NakamaConfig.Current = LoadConfig();

            PeerFactory.Create = CreatePeer;
            NakamaSession.IdentitySource = CurrentPlayer;
            NakamaSession.SessionReplaced = DisableRealtime;
            Debug.Log("[nakama] " + NakamaConfig.Current);
        }

        public static INetworkPeer CreatePeer()
        {
            return new NakamaPeer(NakamaSession.Instance, NakamaServerList.KindOf);
        }

        public static NakamaConfig LoadConfig()
        {
            string[] args = null;
            string json = null;
            try
            {
                args = Environment.GetCommandLineArgs();
                string path = Path.Combine(Application.streamingAssetsPath, "nakama.json");
                if (File.Exists(path))
                    json = File.ReadAllText(path);
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
    }
}
