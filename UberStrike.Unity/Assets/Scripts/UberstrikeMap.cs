using System.Collections.Generic;
using UberStrike.Core.Models.Views;
using UberStrike.Core.Types;
using UberStrike.Realtime.Common;
using UnityEngine;

public class UberstrikeMap
{
    public UberstrikeMap(MapView view)
    {
        View = view;
        IsEnabled = true;
        Icon = new DynamicTexture(ApplicationDataManager.BaseImageURL + "MapIcons/" + View.SceneName + ".jpg");
    }

    public bool IsEnabled { get; set; }
    public MapView View { get; private set; }
    public MapConfiguration Space { get; set; }

    public DynamicTexture Icon { get; private set; }
    public bool IsLoaded { get { return Space != null; } }
    public bool IsBluebox { get { return View.IsBlueBox; } }

    public int Id { get { return View.MapId; } }
    public string Name { get { return View.DisplayName; } }
    public string Description { get { return View.Description; } }
    public string SceneName { get { return View.SceneName; } }
    public string FileName { get { return View.FileName; } }

    public bool IsGameModeSupported(GameModeType mode)
    {
        bool hasSettings = View.Settings != null;
        bool containsKey = hasSettings && View.Settings.ContainsKey(mode);
        bool result = hasSettings && containsKey;
        
        Debug.Log("=== IsGameModeSupported DEBUG ===");
        Debug.Log("Map: " + Name + " (ID: " + Id + ")");
        Debug.Log("GameMode: " + mode);
        Debug.Log("View.Settings != null: " + hasSettings);
        if (hasSettings)
        {
            Debug.Log("View.Settings.ContainsKey(" + mode + "): " + containsKey);
            string modes = "";
            foreach (var key in View.Settings.Keys)
            {
                modes += key.ToString() + ", ";
            }
            Debug.Log("Available modes: " + modes.TrimEnd(',', ' '));
        }
        Debug.Log("Return value: " + result);
        
        return result;
    }
}