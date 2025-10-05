using System;
using System.Net;
using UberStrike.Core.Types;
using UnityEngine;

namespace UberStrike.Unity.ArtTools
{
    public static class WebserviceUtil
    {
        public static void UpdateMapVersion(string bundleName, int mapId, MapType type)
        {
            try
            {
                //set security settings in case we are running on "web player" platform mode
                UnityEditor.EditorSettings.webSecurityEmulationHostUrl = ArtAssetDefines.InstrumentationWebServicesUrl;
                string args = string.Format("mapId={0}&fileName={1}&appVersion={2}&mapType={3}", mapId, bundleName, ArtAssetDefines.ApplicationVersion, (int)type);
                WebRequest r = HttpWebRequest.Create(ArtAssetDefines.UpdateMapVersionUrl + "?" + args);
                r.Method = "GET";
                r.Timeout = 1000;
                //Debug.Log(r.RequestUri);
                r.GetResponse();
            }
            catch (Exception e)
            {
                UnityEditor.EditorUtility.DisplayDialog("UpdateMapVersion", e.Message, "Heeeelp me Tommy!!!");
                Debug.LogError(string.Format("UpdateMapVersion: {0}\n{1}", e.Message, e.StackTrace));
            }
        }
    }
}