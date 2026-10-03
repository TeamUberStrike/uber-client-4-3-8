using UnityEngine;

namespace UberStrike.Realtime.NakamaAdapter
{
    // Drives NakamaSession: uber_time loop, idle socket close, close on quit.
    public sealed class NakamaSessionHost : MonoBehaviour
    {
        NakamaSession _session;

        public static void Ensure(NakamaSession session)
        {
            if (!Application.isPlaying)
                return;

            var go = new GameObject("[Nakama Session]");
            DontDestroyOnLoad(go);
            go.AddComponent<NakamaSessionHost>()._session = session;
        }

        void Update()
        {
            if (_session != null)
                _session.Tick();
        }

        void OnApplicationQuit()
        {
            if (_session != null)
                _session.Shutdown();
        }
    }
}
