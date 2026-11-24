using System.Runtime.InteropServices;
using UnityEngine;

public static class WebGLKeyboard
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int UberGetDeviceType();

    [DllImport("__Internal")]
    private static extern void UberOpenTextKeyboard(string text, string gameObjectName, string methodName, bool masked);

    [DllImport("__Internal")]
    private static extern void UberCloseChatKeyboard();
#endif

    public static bool IsMobile
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return UberGetDeviceType() != 0;
#else
            return false;
#endif
        }
    }

    public static void Open(string text, GameObject receiver, string methodName, bool masked)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (receiver != null && IsMobile)
            UberOpenTextKeyboard(text ?? string.Empty, receiver.name, methodName, masked);
#endif
    }

    public static void Close()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UberCloseChatKeyboard();
#endif
    }
}
