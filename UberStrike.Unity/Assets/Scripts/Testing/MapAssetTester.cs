using UnityEngine;

public class MapAssetTester : MonoBehaviour
{
    public int mapId;
    public string mapName;

#if UNITY_EDITOR
    void Start()
    {
        LevelManager.Instance.SimulateWebplayer("file:///" + Application.dataPath + "/../../UberStrike.UnityAssets/Bundles/");
        LevelManager.Instance.AddMapView(new UberStrike.Core.Models.Views.MapView()
            {
                MapId = mapId,
                DisplayName = mapName,
                SceneName = mapName,
                FileName = mapName + ".unity3d",
            });
    }
#endif
}