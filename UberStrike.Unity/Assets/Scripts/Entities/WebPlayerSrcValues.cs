using System;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using UnityEngine;

public class WebPlayerSrcValues
{
    public int Cmid { get; private set; }
    public DateTime Expiration { get; private set; }
    public string Content { get; private set; }
    public string Hash { get; private set; }
    public string EsnsId { get; private set; }
    public ChannelType ChannelType { get; private set; }
    public EmbedType EmbedType { get; private set; }
    public string Locale { get; private set; }

    public WebPlayerSrcValues(string srcValue)
    {
        Expiration = DateTime.MinValue;
        if (!string.IsNullOrEmpty(srcValue))
        {
            var dict = ParseQueryString(srcValue);

            Cmid = ParseKey<int>(dict, "cmid");
            ChannelType = ParseKey<ChannelType>(dict, "channeltype");
            EmbedType = ParseKey<EmbedType>(dict, "embedtype");
            Expiration = ParseKey<DateTime>(dict, "time");
            Content = ParseKey<string>(dict, "content");
            Hash = ParseKey<string>(dict, "hash");
            EsnsId = ParseKey<string>(dict, "esnsmemberid");
            Locale = ParseKey<string>(dict, "lang");
        }
    }

    public bool IsValid
    {
        get
        {
            return
                Cmid > 0 &&
                (Expiration != DateTime.MinValue) &&
                !string.IsNullOrEmpty(Content) &&
                !string.IsNullOrEmpty(Hash);
        }
    }

    private static T ParseKey<T>(Dictionary<string, string> dict, string key)
    {
        T returnValue = default(T);

        string value;
        if (dict.TryGetValue(key, out value))
        {
            returnValue = StringUtils.ParseValue<T>(value);
        }
        else
        {
            Debug.LogError("ParseKey didn't find value for key '" + key + "'");
        }
        return returnValue;
    }

    public static Dictionary<string, string> ParseQueryString(string queryString)
    {
        Dictionary<string, string> dict = new Dictionary<string, string>();

        // Validate Querystring
        if (string.IsNullOrEmpty(queryString) || !queryString.Contains("=") || queryString.Length < 3)
        {
            Debug.LogWarning("Invalid Querystring: " + queryString);
        }
        else
        {
            // Extract all KeyValuePairs
            foreach (string s in queryString.Substring(queryString.IndexOf("?") + 1, (queryString.Length - queryString.IndexOf("?")) - 1).Split('&'))
            {
                dict.Add((s.Substring(0, s.IndexOf("="))).ToLower(), s.Substring(s.IndexOf("=") + 1, (s.Length - s.IndexOf("=")) - 1));
            }
        }
        return dict;
    }

    public static string ReadQueryStringKey(string queryString, string key)
    {
        var dict = ParseQueryString(queryString);

        if (dict.ContainsKey(key.ToLower()))
            return dict[key.ToLower()];
        else
            return string.Empty;
    }
}