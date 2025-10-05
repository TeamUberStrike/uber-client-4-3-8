#if UNITY_STANDALONE_OSX
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class StoreKitMacBinding
{
    [DllImport("StoreKitPlugin")]
    private static extern bool _storeKitCanMakePayments();

    public static bool canMakePayments()
    {
        return _storeKitCanMakePayments();
    }


    [DllImport("StoreKitPlugin")]
    private static extern void _storeKitRequestProductData(string productIdentifier);

    // Accepts comma-delimited set of product identifiers
    public static void requestProductData(string productIdentifier)
    {
        _storeKitRequestProductData(productIdentifier);
    }


    [DllImport("StoreKitPlugin")]
    private static extern void _storeKitPurchaseProduct(string productIdentifier, int quantity);

    // Purchases the given product and quantity
    public static void purchaseProduct(string productIdentifier, int quantity)
    {
        _storeKitPurchaseProduct(productIdentifier, quantity);
    }


    [DllImport("StoreKitPlugin")]
    private static extern void _storeKitRestoreCompletedTransactions();

    // Restores all previous transactions.  This is used when a user gets a new device and they need to restore their old purchases.
    // DO NOT call this on every launch.  It will prompt the user for their password.
    public static void restoreCompletedTransactions()
    {
        _storeKitRestoreCompletedTransactions();
    }


    [DllImport("StoreKitPlugin")]
    private static extern string _storeKitGetAllSavedTransactions();

    // Returns a list of all the transactions that occured on this device.  They are stored in the Document directory.
    public static List<StoreKitMacTransaction> getAllSavedTransactions()
    {
        var transactionList = new List<StoreKitMacTransaction>();

        if (Application.platform == RuntimePlatform.OSXPlayer)
        {
            // Grab the transactions and parse them out
            string allTransactions = _storeKitGetAllSavedTransactions();

            // parse out the products
            string[] transactionParts = allTransactions.Split(new string[] { "||||" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < transactionParts.Length; i++)
                transactionList.Add(StoreKitMacTransaction.transactionFromString(transactionParts[i]));
        }

        return transactionList;
    }


    [DllImport("StoreKitPlugin")]
    private static extern string _storeKitGetNextMessage();

    public static string getNextMessage()
    {
        var mess = _storeKitGetNextMessage();
        if (mess.Length == 0)
            return null;

        return mess;
    }

    [DllImport("StoreKitPlugin")]
    private static extern string _storeKitUniqueDeviceIdentifier();

    // Returns a unique identifier for the current device and application
    public static string uniqueDeviceIdentifier()
    {
        return _storeKitUniqueDeviceIdentifier();
    }

    [DllImport("StoreKitPlugin")]
    private static extern string _storeKitUniqueGlobalDeviceIdentifier();

    // Returns a unique identifier for the current device
    public static string uniqueGlobalDeviceIdentifier()
    {
        return _storeKitUniqueGlobalDeviceIdentifier();
    }
}
#endif