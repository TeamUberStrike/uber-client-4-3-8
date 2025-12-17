
[12/15/2025 10:07 PM] [ERROR] Exception: InvalidOperationException: Insecure connection not allowed
UnityEngine.Networking.UnityWebRequest.BeginWebRequest () (at <a46cf0df1b5d4c249aedbb851cc051dd>:0)
UnityEngine.Networking.UnityWebRequest.SendWebRequest () (at /home/bokken/build/output/unity/unity/Modules/UnityWebRequest/Public/UnityWebRequest.bindings.cs:298)
UnityEngine.WWW..ctor (System.String url, System.Byte[] postData, System.Collections.Hashtable headers) (at /home/bokken/build/output/unity/unity/Modules/UnityWebRequestWWW/Public/WWW.cs:98)
UberStrike.WebService.Unity.SoapClient+<MakeRequest>d__0.MoveNext () (at <b2a1da66e3714dcb94dc700cace401ec>:0)
UnityEngine.SetupCoroutine.InvokeMoveNext (System.Collections.IEnumerator enumerator, System.IntPtr returnValueAddress) (at /home/bokken/build/output/unity/unity/Runtime/Export/Scripting/Coroutines.cs:17)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&) (at /home/bokken/build/output/unity/unity/Modules/IMGUI/GUIUtility.cs:219)

UnityEngine.Debug:LogError (object)
Cmune.Realtime.Photon.Client.Utils.UnityDebug:Log (int,string)
Cmune.Util.CmuneDebug:log (string,int)
Cmune.Util.CmuneDebug:LogError (string,object[])
DebugConsoleManager:SendExceptionReport (string,string,string) (at Assets/Scripts/DebugConsole/DebugConsoleManager.cs:194)
DebugConsoleManager:OnUnityDebugCallback (string,string,UnityEngine.LogType) (at Assets/Scripts/DebugConsole/DebugConsoleManager.cs:184)
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&) (at /home/bokken/build/output/unity/unity/Modules/IMGUI/GUIUtility.cs:219)

InvalidOperationException: Insecure connection not allowed
UnityEngine.Networking.UnityWebRequest.BeginWebRequest () (at <a46cf0df1b5d4c249aedbb851cc051dd>:0)
UnityEngine.Networking.UnityWebRequest.SendWebRequest () (at /home/bokken/build/output/unity/unity/Modules/UnityWebRequest/Public/UnityWebRequest.bindings.cs:298)
UnityEngine.WWW..ctor (System.String url, System.Byte[] postData, System.Collections.Hashtable headers) (at /home/bokken/build/output/unity/unity/Modules/UnityWebRequestWWW/Public/WWW.cs:98)
UberStrike.WebService.Unity.SoapClient+<MakeRequest>d__0.MoveNext () (at <b2a1da66e3714dcb94dc700cace401ec>:0)
UnityEngine.SetupCoroutine.InvokeMoveNext (System.Collections.IEnumerator enumerator, System.IntPtr returnValueAddress) (at /home/bokken/build/output/unity/unity/Runtime/Export/Scripting/Coroutines.cs:17)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&) (at /home/bokken/build/output/unity/unity/Modules/IMGUI/GUIUtility.cs:219)

