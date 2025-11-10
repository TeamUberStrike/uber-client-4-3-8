using Cmune.DataCenter.Common.Entities;
using UnityEngine;
using System;

public class ClientConfiguration
{
    public string Version = string.Empty;
    public BuildType BuildType = BuildType.Dev;
    public DebugLevel DebugLevel = DebugLevel.Debug;
    public string WebServiceBaseUrl = string.Empty;
    public string ContentBaseUrl = string.Empty;
    public ChannelType ChannelType = ChannelType.WebPortal;
    public bool IsLocalWebplayer = false;

    public string BuildString
    {
        set
        {
            try
            {
                BuildType = (BuildType)Enum.Parse(typeof(BuildType), value);
            }
            catch
            {
                Debug.LogError("Unsupported BuildType!");
            }
        }
    }

    public string ChannelTypeString
    {
        set
        {
            try
            {
                ChannelType = (ChannelType)Enum.Parse(typeof(ChannelType), value);
            }
            catch
            {
                Debug.LogError("Unsupported ChannelType!");
            }
        }
    }

    public string DebugLevelString
    {
        set
        {
            try
            {
                DebugLevel = (DebugLevel)Enum.Parse(typeof(DebugLevel), value);
            }
            catch
            {
                Debug.LogError("Unsupported DebugLevel!");
            }
        }
    }

    public string IsLocalWebplayerString
    {
        set
        {
            try
            {
                IsLocalWebplayer = (bool)bool.Parse(value);
            }
            catch
            {
                Debug.LogError("Unsupported IsLocalWebplayer!");
            }
        }
    }

    public bool IsValid()
    {
        return !(string.IsNullOrEmpty(Version) || string.IsNullOrEmpty(WebServiceBaseUrl) || string.IsNullOrEmpty(ContentBaseUrl));
    }
}
