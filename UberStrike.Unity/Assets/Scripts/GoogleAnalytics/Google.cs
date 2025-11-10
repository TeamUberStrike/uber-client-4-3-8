using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class Google : AutoMonoBehaviour<Google>
{
    public bool rememberPlayerBetweenSessions = true;
    public bool debug = false;
    public const string trackingCode = "UA-5221837-14";
    public const string domain = "ga-events.uberstrike.com";
    public const float wwwTimeout = 15;

    private const string _baseURL = "http://www.google-analytics.com/__utm.gif?";
    private const string _archivedURLSeperator = "||~||";
    private const string _unfinishedRequestsPrefsKey = "_GAUnfinishedRequests";
    private const string _persistancePrefsKey = "_GAPlayerMemory";
    private const string _visitCountPrefsKey = "_GAVisitCount";
    private IGALogger _logger;
    private Queue<string> _requestQueue = new Queue<string>();
    private bool _queueIsProcessing = false;
    private int _domainHash;

    private double _epochTime
    {
        get
        {
            System.TimeSpan t = (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1));
            return t.TotalSeconds;
        }
    }

    #region Unity Delegates

    void Awake()
    {
        if (trackingCode.Length == 0 || domain.Length == 0)
        {
            Debug.LogError("Please enter your tracking code and domain in the prefab!");
            return;
        }

        // prep our logger.  Don't allow the web player to attempt to write to disk!
        if (!Application.isWebPlayer && debug)
            _logger = new GADebugLogger();
        else
            _logger = new GAEmptyLogger();

        DontDestroyOnLoad(gameObject);
        calculateDomainHash();

        // Resurrect and start sending any unsent requests
        resurrectQueue();
    }


    void OnApplicationPause(bool didPause)
    {
        if (didPause)
            persistQueue();
        else
            resurrectQueue();
    }


    public void OnApplicationExit()
    {
        persistQueue();
    }

    #endregion;


    #region Public Page View and Event Methods

    // Logs a page view with the current level name
    public void logPageView()
    {
        logPageView(Application.loadedLevelName);
    }


    // Logs a page view with the passed in page
    public void logPageView(string page)
    {
        logPageView(page, page);
    }


    // Logs a page view with the given page and title
    public void logPageView(string page, string pageTitle)
    {
        sendRequest(page, pageTitle, null, null, null, null);
    }


    // Logs an event for the page with the given category and action
    public void logEvent(string page, string category, string action)
    {
        logEvent(page, page, category, action);
    }


    // Logs an event for the titled page with the given category and action
    public void logEvent(string page, string pageTitle, string category, string action)
    {
        sendRequest(page, pageTitle, category, action, null, null);
    }


    // Logs an event for the page with the given category, action, label and value
    public void logEvent(string page, string category, string action, string label, int value)
    {
        logEvent(page, page, category, action, label, value);
    }


    // Logs an event for the titled page with the given category, action, label and value
    public void logEvent(string page, string pageTitle, string category, string action, string label, int value)
    {
        sendRequest(page, pageTitle, category, action, label, value);
    }

    #endregion;


    #region Private

    // Queues and sends a page view or event requests
    private void sendRequest(string page, string pageTitle, string category, string action, string label, int? value)
    {
        // Ensure the page starts with a '/'
        if (!page.StartsWith("/"))
            page = "/" + page;

        // prep some of our param pieces
        var random = new System.Random();
        int currentTimeStamp = (int)_epochTime;

        // persist visit count and increment for each call
        int visitCount = PlayerPrefs.GetInt(_visitCountPrefsKey, 0);
        visitCount++;
        PlayerPrefs.SetInt(_visitCountPrefsKey, visitCount);

        // Compose the cookie string.  If we are set to persist users between sessions setup utma appropriately
        var _utma = string.Empty;
        if (rememberPlayerBetweenSessions)
        {
            // grab the utma from PlayerPrefs if we have it
            var tempUtma = PlayerPrefs.GetString(_persistancePrefsKey);
            if (tempUtma.Length == 0)
            {
                // leave off visitCount so we can add it for each request
                tempUtma = string.Format("{0}.{1}.{2}.{3}.{4}.", _domainHash, random.Next(1000000000), currentTimeStamp, currentTimeStamp, currentTimeStamp);
                PlayerPrefs.SetString(_persistancePrefsKey, tempUtma);
            }

            // add the visitCount
            _utma = tempUtma + visitCount.ToString();
        }
        else
        {
            // no persistance across launches so recreate from scratch
            _utma = string.Format("{0}.{1}.{2}.{3}.{4}.{5}", _domainHash, random.Next(1000000000), currentTimeStamp, currentTimeStamp, currentTimeStamp, visitCount);
        }


        var _utmz = string.Format("{0}.{1}.1.1.utmcsr=(direct)|utmccn=(direct)|utmcmd=(none)", _domainHash, currentTimeStamp);
        var _utmcc = WWW.EscapeURL(string.Format("__utma={0};+__utmz={1};", _utma, _utmz));

        // WWW.EscapeURL doesn't proper escape '|'s so do it manually
        _utmcc = _utmcc.Replace("|", "%7C");

        var parameters = new Dictionary<string, string>()
		{
			{ "utmwv", "4.8.8" }, // Analytics version
			{ "utmn", random.Next( 1000000000 ).ToString() }, // Random number
			{ "utmhn", WWW.EscapeURL( domain ) }, // Host name
			{ "utmcs", "UTF-8" }, // Charset
			{ "utmsr", string.Format( "{0}x{1}", Screen.currentResolution.width, Screen.currentResolution.height ) }, // Screen resolution
			{ "utmsc", "24-bit" }, // Color depth
			{ "utmul", "en-us" }, // Language
			{ "utmje", "0" }, // Java enabled or not
			{ "utmfl", "-" },	// User Flash version
			{ "utmdt", WWW.EscapeURL( pageTitle ) }, // Page title
			{ "utmhid", random.Next( 1000000000 ).ToString() }, // Random number (unique for all session requests)
			{ "utmr", "-" }, // Referrer
			{ "utmp", WWW.EscapeURL( page ) }, // Page URL
			{ "utmac", trackingCode }, // Google Analytics account
			{ "utmcc", _utmcc } // Cookie string (encoded)
		};


        /// Add the event if we have one
        if (category != null && action != null && category.Length > 0 && action.Length > 0)
        {
            var eventString = string.Format("5({0}*{1}", category, action);

            if (label != null && value.HasValue && label.Length > 0)
                eventString += string.Format("*{0})({1})", label, value.ToString());
            else
                eventString += ")";

            parameters.Add("utme", WWW.EscapeURL(eventString));
            parameters.Add("utmt", "event"); // Analytics type (event)
        }


        // Put together the final query string
        var finalParams = new System.Text.StringBuilder();
        foreach (string key in parameters.Keys)
            finalParams.AppendFormat("{0}={1}&", key, parameters[key]);

        // strip the final trailing '&'
        finalParams.Remove(finalParams.Length - 1, 1);

        // finish off the url
        string url = _baseURL + finalParams.ToString();

        // Enqueue and start the request
        _requestQueue.Enqueue(url);

        StartCoroutine(processRequestQueue());
    }


    // Waits for the request to finish or a timeout to occur
    private IEnumerator waitForRequest(WWW www)
    {
        float timeoutCompletion = Time.realtimeSinceStartup + wwwTimeout;

        while (Time.realtimeSinceStartup < timeoutCompletion && !www.isDone)
            yield return null;
    }


    // Runs through all the queued requests and sends them
    private IEnumerator processRequestQueue()
    {
        // early out if we are already running
        if (_queueIsProcessing)
            yield break;

        _queueIsProcessing = true;

        while (_requestQueue.Count > 0)
        {
            var url = _requestQueue.Dequeue();

            _logger.logStartRequest(url);

            // The web player gets special treatment
            if (Application.isWebPlayer)
            {
                var eval = string.Format("var i = new Image(); i.src = '{0}'; document.body.appendChild( i );", url);
                Application.ExternalEval(eval);
                _logger.logSuccessfulRequest(url);
            }
            else
            {
                // Kick off the request
                WWW www = new WWW(url);

                yield return StartCoroutine(waitForRequest(www));

                // we might have timed out, so check for that
                if (www.isDone)
                {
                    if (www.error != null)
                    {
                        // failed.  Log the error, enqueue the url and break out of the queue processing
                        _logger.logFailedRequest(url, www.error);
                        _requestQueue.Enqueue(url);
                        break;
                    }
                    else
                    {
                        _logger.logSuccessfulRequest(url);
                    }
                }
                else // timed out.  Save this for later and exit the queue iteration for now
                {
                    _requestQueue.Enqueue(url);
                    break;
                }
            } // end else
        } // end while

        _queueIsProcessing = false;
    }


    // Persists any unsent requests to PlayerPrefs
    private void persistQueue()
    {
        // Store any requests that weren't yet sent in PlayerPrefs
        if (_requestQueue.Count > 0)
        {
            _logger.log(string.Format("[Saving {0} unsent requests]", _requestQueue.Count.ToString()));

            var items = new string[_requestQueue.Count];
            _requestQueue.CopyTo(items, 0);

            var saveString = string.Join(_archivedURLSeperator, items);
            PlayerPrefs.SetString(_unfinishedRequestsPrefsKey, saveString);

            _requestQueue.Clear();
        }
    }


    // Grabs any unsent requests from PlayerPrefs and starts sending them
    private void resurrectQueue()
    {
        // check for unsent requests
        var unsentRequests = PlayerPrefs.GetString(_unfinishedRequestsPrefsKey);
        if (unsentRequests != string.Empty)
        {
            foreach (var url in unsentRequests.Split(new string[] { _archivedURLSeperator }, System.StringSplitOptions.RemoveEmptyEntries))
                _requestQueue.Enqueue(url);

            // clear out the PlayerPrefs and start up the queue
            PlayerPrefs.SetString(_unfinishedRequestsPrefsKey, string.Empty);

            _logger.log(string.Format("[Resurrected {0} unsent requests]", _requestQueue.Count.ToString()));

            StartCoroutine(processRequestQueue());
        }
    }


    private void calculateDomainHash()
    {
        // http://www.google.com/support/forum/p/Google+Analytics/thread?tid=626b0e277aaedc3c&hl=en
        int a = 1;
        int c = 0;
        int h;
        char chrCharacter;
        int intCharacter;

        a = 0;
        for (h = domain.Length - 1; h >= 0; h--)
        {
            chrCharacter = char.Parse(domain.Substring(h, 1));
            intCharacter = (int)chrCharacter;
            a = (a << 6 & 268435455) + intCharacter + (intCharacter << 14);
            c = a & 266338304;
            a = c != 0 ? a ^ c >> 21 : a;
        }

        _domainHash = a;
    }

    #endregion;
}