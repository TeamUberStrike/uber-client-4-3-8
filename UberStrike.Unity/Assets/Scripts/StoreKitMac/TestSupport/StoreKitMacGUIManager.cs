using UnityEngine;
using System.Collections.Generic;

public class StoreKitMacGUIManager : MonoBehaviour
{
#if UNITY_STANDALONE_OSX
    private string output;
    private string stack;

    private void Awake()
    {
        output = string.Empty;
        stack = string.Empty;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        output += logString + "\n\n";
        stack += stackTrace + "\n\n";
    }

    void OnGUI()
    {
        float yPos = 10.0f;
        float xPos = 20.0f;
        float width = 210.0f;


        if (GUI.Button(new Rect(xPos, yPos, width, 40), "Start Event Listener"))
        {
            StartCoroutine(StoreKitMacManager.Instance.StartEventListener());
        }

        if (GUI.Button(new Rect(xPos, yPos += 50, width, 40), "Stop Event Listener"))
        {
            StoreKitMacManager.Instance.StopEventListener();
        }

        if (GUI.Button(new Rect(xPos, yPos += 50, width, 40), "Get Can Make Payments"))
        {
            StoreKitMacBinding.canMakePayments();
        }

        if (GUI.Button(new Rect(xPos, yPos += 50, width, 40), "Get Product Data"))
        {
            // comma delimited list of product ID's from iTunesConnect.  MUST match exactly what you have there!
            string productIdentifiers = "Credits_5_01";
            StoreKitMacBinding.requestProductData(productIdentifiers);
        }

        if (GUI.Button(new Rect(xPos, yPos += 50, width, 40), "Restore Completed Transactions"))
        {
            StoreKitMacBinding.restoreCompletedTransactions();
        }

        // Second column
        xPos += xPos + width;
        yPos = 10.0f;
        if (GUI.Button(new Rect(xPos, yPos, width, 40), "Purchase Product Credits_5_01"))
        {
            StoreKitMacBinding.purchaseProduct("Credits_5_01", 1);
        }

        //if (GUI.Button(new Rect(xPos, yPos += 50, width, 40), "Purchase Product 2"))
        //{
        //    StoreKitMacBinding.purchaseProduct("anotherProduct", 1);
        //}

        //if (GUI.Button(new Rect(xPos, yPos += 50, width, 40), "Purchase Subscription"))
        //{
        //    StoreKitMacBinding.purchaseProduct("sevenDays", 1);
        //}

        if (GUI.Button(new Rect(xPos, yPos += 50, width, 40), "Get Saved Transactions"))
        {
            StoreKitMacBinding.getAllSavedTransactions();
        }

        if (GUI.Button(new Rect(xPos, yPos += 50, width, 40), "Get UDIDs"))
        {
            StoreKitMacBinding.uniqueGlobalDeviceIdentifier();
            StoreKitMacBinding.uniqueDeviceIdentifier();
        }

        GUI.TextArea(new Rect(20, 300, 300, 300), output);
        GUI.TextArea(new Rect(340, 300, 600, 300), stack);

        if (GUI.Button(new Rect(20, 620, 300, 40), "Clear"))
        {
            output = string.Empty;
            stack = string.Empty;
        }
    }
#endif
}