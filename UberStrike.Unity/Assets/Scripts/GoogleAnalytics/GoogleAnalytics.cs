using System.Collections.Generic;
using UnityEngine;

public class GoogleAnalytics : Singleton<GoogleAnalytics>
{
    private List<string> GAEventList = new List<string>();

    private GoogleAnalytics() { }

    public void LogEvent(string category, string action, bool unique = false)
    {
        LogEvent(category, action, (int)Time.time, unique);
    }

    public void LogEvent(string category, string action, float time, bool unique)
    {
        // Decide if we record a unique event
        if (unique && GAEventList.Contains(category + "-" + action))
        {
            //Debug.Log(string.Format("LogGoogleAnalyticsEvent={0}, {1}", "Do Not Log", category + "-" + action));
        }
        else
        {
            // Log Event to Google Analytics
            //Debug.Log(string.Format("LogGoogleAnalyticsEvent={0}, {1}, {2}, {3}, {4}", GetUserContext(), category, action, ApplicationDataManager.Channel.ToString(), (int)time));
            Google.Instance.logEvent(GetUserContext(), category, action, ApplicationDataManager.Channel.ToString(), (int)time);

            // If we want the log to be unique, record it in the list
            if (unique) GAEventList.Add(category + "-" + action);
        }
    }

    public void LogPageView(string page)
    {
        // Debug out
        //Debug.Log(string.Format("LogGoogleAnalyticsPageView={0}, {1}", ApplicationDataManager.Channel.ToString() + "-" + GetUserContext(), page));

        // Log Page to Google Analytics
        Google.Instance.logPageView(page, ApplicationDataManager.Channel.ToString() + "-" + GetUserContext());
    }

    private string GetUserContext()
    {
        // Player context (current page if out of game, game mode if in game)
        if (GameState.HasCurrentGame)
            return GameState.CurrentGameMode.ToString();
        else
            return MenuPageManager.GetCurrentPage().ToString();
    }
}