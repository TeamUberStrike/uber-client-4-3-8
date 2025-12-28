
using UnityEditor;
using UberStrike.Unity.ArtTools;
using System.IO;
using System.Text;

public static class ProjectSynchronization
{
    [MenuItem("Cmune/Update Project", priority = 1)]
    static void UpdateProject()
    {
        //update base project
        ShellCommand.Create("hg pull -u")
                    .SetTitle("Updating Project")
                    .SetCallback((a, b) =>
                    {
                        UnityEditor.EditorUtility.DisplayDialog("Update Finished", a, "OK");
                        UnityEngine.Debug.Log(a + "\n" + b);
                    }).RunAsync();
    }

    [MenuItem("Cmune/Pull Art Assets", priority = 2)]
    static void PullArtAssets()
    {
        if (Directory.Exists(ArtAssetDefines.WorkingDirectory))
        {
            ShellCommand.Create("hg pull -u")
                    .SetWorkingDirectory(ArtAssetDefines.WorkingDirectory).SetTitle("Updating Art Assets")
                    .SetCallback((a, b) =>
                    {
                        UnityEditor.EditorUtility.DisplayDialog("Update Finished", a, "OK");
                        UnityEngine.Debug.Log(a + "\n" + b);
                    }).RunAsync();
        }
        else
        {
            Directory.CreateDirectory(ArtAssetDefines.WorkingDirectory);
            ShellCommand.Create("hg clone " + ArtAssetDefines.RepositoryUrl + " " + ArtAssetDefines.WorkingDirectory)
             .SetTitle("Loading Assets")
             .SetCallback((a, b) =>
             {
                 UnityEditor.EditorUtility.DisplayDialog("Update Finished", a, "OK");
                 UnityEngine.Debug.Log(a + "\n" + b);
             }).RunAsync();
        }
    }

    //[MenuItem("Cmune/Push Art Assets", priority = 3)]
    public static void PushArtAssets(string commitMessage)
    {
        ShellCommand.Create("hg", "push")
                     .SetWorkingDirectory(ArtAssetDefines.WorkingDirectory).SetTitle("Uploading Assets")
                     .SetCallback((a, b) =>
                     {
                         UnityEditor.EditorUtility.DisplayDialog("Upload Finished", a, "OK");
                         UnityEngine.Debug.Log(a + "\n" + b);
                     }).RunAsync();

        return;

        //if (Directory.Exists(ArtAssetDefines.WorkingDirectory))
        //{
        //    bool noChanges = true;
        //    if (ShellCommand.Create("hg commit -A -m \"" + commitMessage + "\"")
        //             .SetWorkingDirectory(ArtAssetDefines.WorkingDirectory)
        //             .SetCallback((output, error) =>
        //                 {
        //                     noChanges = output.StartsWith("nothing changed");
        //                     UnityEngine.Debug.Log(output + " " + noChanges);
        //                     UnityEngine.Debug.LogError(error);
        //                 })
        //             .Run() && !noChanges)
        //    {
        //        ShellCommand.Create("hg push")
        //             .SetWorkingDirectory(ArtAssetDefines.WorkingDirectory).SetTitle("Uploading Assets")
        //             .SetCallback((a, b) =>
        //             {
        //                 UnityEditor.EditorUtility.DisplayDialog("Upload Finished", a, "OK");
        //                 UnityEngine.Debug.Log(a + "\n" + b);
        //             }).RunAsync();
        //    }
        //    else if (!noChanges)
        //    {
        //        UnityEditor.EditorUtility.DisplayDialog("Error", "Something went wrong!", "Call Tommy");
        //    }
        //}
        //else
        //{
        //    UnityEditor.EditorUtility.DisplayDialog("Error", "There is nothing to upload!", "Call Tommy");
        //}
    }
}