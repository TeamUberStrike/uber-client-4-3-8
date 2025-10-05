using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace UberStrike.Unity.ArtTools
{
    public class ShellCommand
    {
        private ProcessStartInfo processInfo;
        private Callback callback;
        private float estimatedTime = 60;
        private string title = "Updating";

        public delegate void Callback(string output, string error);

        public static ShellCommand Create(string command, string arguments)
        {
            var c = new ShellCommand();
            c.processInfo = new ProcessStartInfo(command, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = false,
                RedirectStandardError = true,
            };
            return c;
        }

        public static ShellCommand Create(string command)
        {
            var c = new ShellCommand();
            c.processInfo = new ProcessStartInfo("cmd.exe", "/C " + command)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = false,
                RedirectStandardError = true,
            };
            return c;
        }

        public ShellCommand SetCallback(Callback callback)
        {
            this.callback = callback;
            return this;
        }

        public ShellCommand SetWorkingDirectory(string workingDirectory)
        {
            this.processInfo.WorkingDirectory = workingDirectory;
            return this;
        }

        public ShellCommand SetEstimatedTime(float estimatedTime)
        {
            this.estimatedTime = Mathf.Clamp(estimatedTime, 1, 100);
            return this;
        }

        public ShellCommand SetTitle(string title)
        {
            this.title = title;
            return this;
        }

        public void RunAsync()
        {
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                try
                {
                    UnityEditor.EditorApplication.update += Update;

                    estimatedTime = Mathf.Clamp(estimatedTime, 1, 100);
                    float progress = estimatedTime;
                    var process = Process.Start(processInfo);

                    title += "  -  [pid:" + process.Id + "]";
                    string message = "Please wait a bit..";

                    while (!process.HasExited)
                    {
                        EnqueueAction(() =>
                        {
                            UnityEditor.EditorUtility.DisplayProgressBar(title, message, (estimatedTime - progress) / estimatedTime);
                        });

                        Thread.Sleep(100);
                        progress -= 0.1f;
                    }

                    process.Exited += (sender, e) =>
                    {
                        UnityEngine.Debug.LogWarning("Process exit with: " + process.ExitCode);
                    };

                    EnqueueAction(UnityEditor.EditorUtility.ClearProgressBar);
                    if (callback != null)
                        EnqueueAction(() => { callback(process.StandardOutput.ReadToEnd(), process.StandardError.ReadToEnd()); });
                }
                catch (Exception e)
                {
                    EnqueueAction(UnityEditor.EditorUtility.ClearProgressBar);
                    if (callback != null)
                        EnqueueAction(() => { callback(string.Empty, e.GetType() + ": " + e.Message); });
                }
                finally
                {
                    Thread.Sleep(100);
                    UnityEditor.EditorApplication.update -= Update;
                }
            });
        }

        public bool Run()
        {
            try
            {
                var process = Process.Start(processInfo);
                process.Exited += (sender, e) =>
                {
                    UnityEngine.Debug.LogWarning("Process exit with: " + process.ExitCode);
                };
                process.WaitForExit();

                string error = process.StandardError.ReadToEnd();

                if (callback != null)
                    callback(process.StandardOutput.ReadToEnd(), error);

                //if (!string.IsNullOrEmpty(error))
                //    UnityEngine.Debug.LogWarning("Error executing: " + processInfo.Arguments + "\n" + error);

                return string.IsNullOrEmpty(error);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Error executing: " + processInfo.Arguments + "\n" + e.GetType() + ": " + e.Message);

                if (callback != null)
                    callback(string.Empty, e.GetType() + ": " + e.Message);

                return false;
            }
        }

        #region GUI Thread Interaction

        private Queue<Action> editorActions = new Queue<Action>();

        private void EnqueueAction(Action action)
        {
            lock (editorActions)
            {
                editorActions.Enqueue(action);
            }
        }

        private void Update()
        {
            lock (editorActions)
            {
                while (editorActions.Count > 0)
                {
                    editorActions.Dequeue()();
                }
            }
        }

        #endregion
    }
}