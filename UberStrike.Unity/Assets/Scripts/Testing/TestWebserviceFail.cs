using UberStrike.WebService.Unity;
using UnityEngine;

public class TestWebserviceFail : MonoBehaviour
{
    // Update is called once per frame
    void OnGUI()
    {
        if (GUI.Button(new Rect(Screen.width - 200, Screen.height - 30, 200, 30), "Exception: " + (Configuration.SimulateWebservicesFail ? "ON" : "OFF")))
        {
            Configuration.SimulateWebservicesFail = !Configuration.SimulateWebservicesFail;
        }
    }
}