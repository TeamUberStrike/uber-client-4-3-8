using UnityEngine;
using System.Collections;

public class CmuneBundleAttribution : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
        UberStrike.WebService.Unity.Configuration.EncryptionInitVector = "C15netodx9234d67";
        UberStrike.WebService.Unity.Configuration.EncryptionPassPhrase = "s2a542av1a5d21";
    }

    // Update is called once per frame
    void OnGUI()
    {
        if (GUI.Button(new Rect(100, 100, 200, 20), "buy bundle"))
        {
            // Attribute the item and record the receipt
            UberStrike.WebService.Unity.ShopWebServiceClient.BuyMasBundle(PlayerDataManager.CmidSecure, 56, "test", 1,
                (success) =>
                {
                    Debug.LogWarning("BuyMasBundle success: " + success);
                },
                (ex) =>
                {
                    DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
                });
        }
    }
}
