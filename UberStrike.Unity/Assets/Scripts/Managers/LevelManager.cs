using System.Collections;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.Core.Models.Views;
using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{
    private LightmapData[] _originalLightmaps;
    private Dictionary<int, UberstrikeMap> mapsById = new Dictionary<int, UberstrikeMap>();
    private MapLoader _loader;
    private UberstrikeMap _initialMap;
    private readonly HashSet<int> _mobileSupportedMaps = new HashSet<int> { 3, 4, 5, 7, 8, 10 };

    public IEnumerable<UberstrikeMap> AllMaps { get { return mapsById.Values; } }
    public int CurrentLoadingLevelId { get { return _loader.MapToLoad != null ? _loader.MapToLoad.Id : -1; } }
    public float CurrentProgress { get { return _loader.Progress; } }
    public bool IsLoading { get { return _loader.Progress != 1; } }
    public int Count { get { return mapsById.Count; } }

    public bool IsSimulateWebplayer { get; private set; }
    public string SimulatedWebPlayerPath { get; private set; }
    public void SimulateWebplayer(string path)
    {
        IsSimulateWebplayer = true;
        SimulatedWebPlayerPath = path;
    }

    private LevelManager()
    {
        Clear();

        _loader = new MapLoader();
    }

    private MapView CreateMapView(string name, int id, bool isBluebox = false)
    {
        return new MapView
        {
            Description = name,
            DisplayName = name,
            MapId = id,
            SceneName = (name.StartsWith("Level") ? "" : "Level") + name,
            IsBlueBox = isBluebox,
            FileName = string.Format("Map-{0:00}.unity3d", id)
        };
    }

    public string GetMapDescription(int mapId)
    {
        UberstrikeMap map;
        if (mapsById.TryGetValue(mapId, out map) && map != null)
        {
            return map.Description;
        }
        else
        {
            return LocalizedStrings.None;
        }
    }

    public string GetMapName(int mapId)
    {
        UberstrikeMap map;
        if (mapsById.TryGetValue(mapId, out map) && map != null)
        {
            return map.Name;
        }
        else
        {
            return LocalizedStrings.None;
        }
    }

    public UberstrikeMap GetMapWithId(int mapId)
    {
        UberstrikeMap map = null;
        mapsById.TryGetValue(mapId, out map);
        return map;
    }

    public bool IsBlueBox(int mapId)
    {
        UberstrikeMap mapContainer;
        if (mapsById.TryGetValue(mapId, out mapContainer))
        {
            return mapContainer.IsBluebox;
        }
        else
        {
            return false;
        }
    }

    public bool HasMapWithId(int mapId)
    {
        return mapsById.ContainsKey(mapId);
    }

    public void AddLoadedMap(MapConfiguration map)
    {
        UberstrikeMap mapContainer;
        if (mapsById.TryGetValue(map.MapId, out mapContainer))
        {
            mapContainer.Space = map;
        }
        else
        {
            //TODO: why would that ever happen?
            mapContainer = new UberstrikeMap(new MapView()
                {
                    MapId = map.MapId,
                });
            mapContainer.Space = map;
            mapsById.Add(map.MapId, mapContainer);
        }

        //TODO: Scene system refactoring - TF
        // Right now we always load all maps 'additively' to our existing main scene.
        // Along with all assets unity is also loading the lightmaps and is automatically adding them to the LightmapSettings.lightmaps array.
        // Because we never unload scenes but only delete the gameobject hierarchy from our main scene
        // unity keeps filling up the lightmap array every time we load a new level!
        // The real fix would be to define every scene as self contained and stop using Application.LoadLevelAdditive
        // but just use Application.LoadLevel. We will be forced to keep a clean MVC archicture and get rid of all the 
        // mono configuration scripts we are using currently. But because 4.3.8 is on the way out we go for a HACK.
        if (map.MapId == 0)
            _originalLightmaps = LightmapSettings.lightmaps;

        map.transform.parent = GetLevelsParent();

        //clear all old levels
        foreach (KeyValuePair<int, UberstrikeMap> kvp in mapsById)
        {
            int id = kvp.Key;
            UberstrikeMap m = kvp.Value;
            // if not initial level and not this level
            if (id != 0 && mapContainer.Space != m.Space)
            {
                if (m.Space != null)
                {
                    GameObject.DestroyImmediate(m.Space.gameObject);
                    m.Space = null;
                }
            }
        }
#if !UNITY_ANDROID
        Resources.UnloadUnusedAssets();
#endif
    }

    private Transform GetLevelsParent()
    {
        GameObject levels = GameObject.Find("Levels");
        if (levels == null)
        {
            levels = new GameObject("Levels");
        }
        return levels.transform;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mapId">The ID of the map, as defined in instrumentation</param>
    /// <param name="name">The name of the scene to load, without the [Level] prefix</param>
    public void AddMapView(int mapId, string name)
    {
        mapsById.Add(mapId, new UberstrikeMap(CreateMapView(name, mapId)));
    }

    public void AddMapView(MapView mapView)
    {
        mapsById.Add(mapView.MapId, new UberstrikeMap(mapView));
    }

    // restore to initial settings
    private void Clear()
    {
        if (_initialMap == null)
        {
            _initialMap = new UberstrikeMap(CreateMapView("LevelSpaceShip", 0)) { IsEnabled = false };
        }
        mapsById.Clear();
        mapsById.Add(0, _initialMap);
    }

    public bool InitializeMapsToLoad(List<MapView> maps)
    {
        Clear();

        // If we are in the editor and Load Maps Using WebService is disabled, manually add our maps here
        if (Application.isEditor && !ApplicationDataManager.Instance.LoadMapsUsingWebService)
        {
            mapsById.Add(1, new UberstrikeMap(CreateMapView("MonkeyIsland", 1)));
            mapsById.Add(2, new UberstrikeMap(CreateMapView("LostParadise2", 2)));
            mapsById.Add(3, new UberstrikeMap(CreateMapView("TheWarehouse", 3)));
            mapsById.Add(4, new UberstrikeMap(CreateMapView("TempleOfTheRaven", 4)));
            mapsById.Add(5, new UberstrikeMap(CreateMapView("FortWinter", 5)));
            mapsById.Add(6, new UberstrikeMap(CreateMapView("GideonsTower", 6)));
            mapsById.Add(7, new UberstrikeMap(CreateMapView("SkyGarden", 7, false)));
            mapsById.Add(8, new UberstrikeMap(CreateMapView("CuberStrike", 8, true)));
            mapsById.Add(10, new UberstrikeMap(CreateMapView("SpaceportAlpha", 10, true)));
        }
        else
        {
            foreach (var m in maps)
            {
                if (!mapsById.ContainsKey(m.MapId))
                {
                    if ((ApplicationDataManager.Channel != ChannelType.Android &&
                        ApplicationDataManager.Channel != ChannelType.IPhone &&
                        ApplicationDataManager.Channel != ChannelType.IPad) ||
                        _mobileSupportedMaps.Contains(m.MapId))
                        mapsById.Add(m.MapId, new UberstrikeMap(m));
                }
            }
        }

        return (mapsById.Count > 0);
    }


    public void CancelLoadMap(int mapId)
    {
        if (_loader.MapToLoad != null && _loader.MapToLoad.Id == mapId)
        {
            CoroutineManager.StopCoroutine(_loader.StartLoadingMap);
        }
    }

    public void LoadMap(int mapId)
    {
        UberstrikeMap map;
        if (mapsById.TryGetValue(mapId, out map) && !map.IsLoaded)
        {
            _loader.MapToLoad = map;
            CoroutineManager.StartCoroutine(_loader.StartLoadingMap, true);
        }
    }

    private class MapLoader
    {
        public UberstrikeMap MapToLoad { get; set; }
        public float Progress { get; private set; }

        public IEnumerator StartLoadingMap()
        {
            int id = CoroutineManager.Begin(StartLoadingMap);

            if (Application.isEditor && !ApplicationDataManager.Instance.LoadMapsUsingWebService)//Instance.IsSimulateWebplayer)
            {
                yield return Application.LoadLevelAdditiveAsync(MapToLoad.SceneName);
            }
            else if (ApplicationDataManager.Channel == ChannelType.Android || ApplicationDataManager.Channel == ChannelType.IPad || ApplicationDataManager.Channel == ChannelType.IPhone)
            {
                yield return Application.LoadLevelAdditiveAsync(MapToLoad.SceneName);
            }
            else
            {
                WWW loader;
                string fileToLoad = string.Empty;

#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
                if (System.IO.File.Exists(ApplicationDataManager.BaseStandaloneMapsURL + MapToLoad.FileName))
                {
                    Debug.LogError("Map found:" + ApplicationDataManager.BaseStandaloneMapsURL + MapToLoad.FileName);
                    fileToLoad = ApplicationDataManager.BaseStandaloneMapsURL + MapToLoad.FileName;
                    loader = new WWW("file://" + fileToLoad);
                }
                else
                {
                    Debug.LogWarning("Map NOT found:" + ApplicationDataManager.BaseStandaloneMapsURL + MapToLoad.FileName);
                    fileToLoad = ApplicationDataManager.BaseMapsURL + MapToLoad.FileName;
                    loader = WWW.LoadFromCacheOrDownload(fileToLoad, 1);
                }
#elif UNITY_EDITOR
                if (ApplicationDataManager.Instance.LoadMapsUsingWebService)
                {
                    fileToLoad = ApplicationDataManager.BaseMapsURL + MapToLoad.FileName;
                    loader = WWW.LoadFromCacheOrDownload(fileToLoad, 1);
                }
                //if (Instance.IsSimulateWebplayer)
                //{
                //    fileToLoad = Instance.SimulatedWebPlayerPath + MapToLoad.FileName;
                //    loader = new WWW(fileToLoad);
                //}
                else
                {
                    fileToLoad = ApplicationDataManager.BaseStandaloneMapsURL + MapToLoad.SceneName + ".unity3d";
                    loader = WWW.LoadFromCacheOrDownload(fileToLoad, 1);
                }
#else
                fileToLoad = ApplicationDataManager.BaseMapsURL + MapToLoad.FileName;
                loader = WWW.LoadFromCacheOrDownload(fileToLoad, 1);
#endif

                Progress = 0;
                while (!loader.isDone && CoroutineManager.IsCurrent(StartLoadingMap, id))
                {
                    yield return new WaitForEndOfFrame();
                    Progress = loader.progress;
                }

                Progress = 1;

                if (CoroutineManager.IsCurrent(StartLoadingMap, id))
                {
                    if (string.IsNullOrEmpty(loader.error))
                    {
                        AssetBundle assetBundle = loader.assetBundle;
                        if (assetBundle != null)
                        {
                            LightmapSettings.lightmaps = LevelManager.Instance._originalLightmaps;

                            //wait one more grace frame
                            yield return new WaitForEndOfFrame();
                            yield return Application.LoadLevelAdditiveAsync(MapToLoad.SceneName);

                            // Log the time it took to load a map
                            //GoogleAnalytics.Instance.LogEvent("app-map-load", MapToLoad.SceneName, Time.time - gaStartTime, true);

                            assetBundle.Unload(false);
                        }
                        else
                        {
                            CmuneDebug.LogError("Failed to load " + fileToLoad + ", probably outdated asset");
                        }
                    }
                    else
                    {
                        Debug.LogError("Loading Streamed Level at '" + fileToLoad + "' failed with error: " + loader.error);
                    }
                }

                loader.Dispose();
            }

            CoroutineManager.End(StartLoadingMap, id);
        }
    }
}