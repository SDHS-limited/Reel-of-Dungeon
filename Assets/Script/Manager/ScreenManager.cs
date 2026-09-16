using UnityEngine;
using System.Runtime.InteropServices; // 외부 DLL/jslib 호출 필수

public class ScreenManager : MonoBehaviour
{
    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void InstallLandscapeWebGL();

    [DllImport("__Internal")]
    private static extern void LockLandscapeWebGL();
    #endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallLandscapeMode()
    {
        ApplyUnityLandscapeSettings();

        #if UNITY_WEBGL && !UNITY_EDITOR
        InstallLandscapeWebGL();
        LockLandscapeWebGL();
        #endif
    }

    public static void RequestLandscape()
    {
        ApplyUnityLandscapeSettings();

        #if UNITY_WEBGL && !UNITY_EDITOR
        LockLandscapeWebGL();
        #endif
    }

    private static void ApplyUnityLandscapeSettings()
    {
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }

    public void RequestLandscapeMode()
    {
        RequestLandscape();
    }
}
