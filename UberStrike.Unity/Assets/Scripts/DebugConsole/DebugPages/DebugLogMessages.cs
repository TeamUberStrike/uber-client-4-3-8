using System.Text;
using Cmune.Util;
using UnityEngine;
using UberStrike.Helper;

public class DebugLogMessages : IDebugPage
{
    public string Title
    {
        get { return "Logs"; }
    }

    public void Draw()
    {
        GUILayout.TextArea(Console.DebugOut);
    }

    public static void Log(int type, string msg)
    {
        Console.Log(type, msg);
    }

    public static readonly ConsoleDebug Console = new ConsoleDebug();

    public class ConsoleDebug
    {
        private LimitedQueue<string> _queue = new LimitedQueue<string>(100);
        private string _debugOut = string.Empty;

        public void Log(int level, string s)
        {
            _queue.Enqueue(s);

            StringBuilder builder = new StringBuilder();
            foreach (string d in _queue)
            {
                builder.AppendLine(d);
            }

            _debugOut = builder.ToString();
        }

        public string DebugOut
        {
            get { return _debugOut; }
        }

        public string ToHTML()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<h3>DEBUG LOG</h3>");
            foreach (string d in _queue)
            {
                sb.AppendLine(d + "<br/>");
            }
            return sb.ToString();
        }
    }
}
