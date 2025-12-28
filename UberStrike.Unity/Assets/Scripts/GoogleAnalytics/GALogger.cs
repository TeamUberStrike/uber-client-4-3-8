using System;
using System.IO;
using UnityEngine;

// Dumps all activity to a log file
public class GADebugLogger : IGALogger
{
    private string _pathToLogFile;


    public GADebugLogger()
    {
        // this will drop the files in the Assets/Documents directory for the editor. Be sure to create the directory first!
        string basePath = (Application.isEditor) ? Application.dataPath : Environment.GetFolderPath(Environment.SpecialFolder.Personal);

        // cover new Unity 3.2 behaviour where it returns "Documents" as the Personal folder on iOS
        if (!basePath.EndsWith("Documents"))
            basePath = Path.Combine(basePath, "Documents");

        // create the documents directory if it doesnt exist while in the editor
        if (Application.isEditor && !Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        _pathToLogFile = Path.Combine(basePath, "GALog.txt");
    }


    public void log(string data)
    {
        using (var sw = new StreamWriter(_pathToLogFile, true))
            sw.Write(string.Format("{0} {1}\r\n", System.DateTime.Now, data));
    }


    public void logStartRequest(string url)
    {
        log(string.Format("[started] {0}", url));
    }


    public void logSuccessfulRequest(string url)
    {
        log(string.Format("[succeeded] {0}", url));
    }


    public void logFailedRequest(string url, string error)
    {
        log(string.Format("[failed] {0}, {1}", url, error));
    }
}


// Empty implementation for production use
public class GAEmptyLogger : IGALogger
{
    public void log(string data)
    { }


    public void logStartRequest(string url)
    { }


    public void logSuccessfulRequest(string url)
    { }


    public void logFailedRequest(string url, string error)
    { }
}



interface IGALogger
{
    void log(string data);
    void logStartRequest(string url);
    void logSuccessfulRequest(string url);
    void logFailedRequest(string url, string error);
}