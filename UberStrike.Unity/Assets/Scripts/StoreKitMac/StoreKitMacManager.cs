#if UNITY_STANDALONE_OSX
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoreKitMacManager
{
    #region Events and delegates

    public delegate void ProductPurchasedEventHandler(string productIdentifier, string receipt, int quantity);
    public delegate void ProductListReceivedEventHandler(List<StoreKitMacProductModel> productList);
    public delegate void StoreKitErrorEventHandler(string error);
    public delegate void StoreKitEmptyEventHandler();
    public delegate void StoreKitStringEventHandler(string response);
    public delegate void ValidateReceiptSuccessfulEventHandler();

    // Fired when a product is successfully paid for.  returnValue will hold the productIdentifer and receipt of the purchased product.
    public static event ProductPurchasedEventHandler purchaseSuccessful;

    // Fired when the product list your required returns.  Automatically serializes the productString into StoreKitProduct's.
    public static event ProductListReceivedEventHandler productListReceived;

    // Fired when requesting product data fails
    public static event StoreKitErrorEventHandler productListRequestFailed;

    // Fired when a product purchase fails
    public static event StoreKitErrorEventHandler purchaseFailed;

    // Fired when a product purchase is cancelled by the user or system
    public static event StoreKitErrorEventHandler purchaseCancelled;

    // Fired when the validateReceipt call fails
    public static event StoreKitErrorEventHandler receiptValidationFailed;

    // Fired when receive validation completes and returns the raw receipt data
    public static event StoreKitStringEventHandler receiptValidationRawResponseReceived;

    // Fired when the validateReceipt method finishes.  It does not automatically mean success.
    public static event ValidateReceiptSuccessfulEventHandler receiptValidationSuccessful;

    // Fired when an error is encountered while adding transactions from the user's purchase history back to the queue
    public static event StoreKitErrorEventHandler restoreTransactionsFailed;

    // Fired when all transactions from the user's purchase history have successfully been added back to the queue
    public static event StoreKitEmptyEventHandler restoreTransactionsFinished;

    #endregion

    private bool _keepListening;

    public static readonly StoreKitMacManager Instance = new StoreKitMacManager();

    private StoreKitMacManager() { }

    public IEnumerator StartEventListener()
    {
        _keepListening = true;

        while (_keepListening)
        {
            // see if we have a message
            string message = StoreKitMacBinding.getNextMessage();
            if (message != null)
            {
                var parts = message.Split(new string[] { ":::", }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    //wait until the p-invoke finished to avoid WWW thread interferences
                    yield return new WaitForSeconds(1);
                    SendMessage(parts[0], parts[1]);
                }
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    public void StopEventListener()
    {
        _keepListening = false;
    }

    private void SendMessage(string methodName, string argument)
    {
        switch (methodName)
        {
            case "productPurchased":
                productPurchased(argument);
                break;
            case "productPurchaseFailed":
                productPurchaseFailed(argument);
                break;
            case "productPurchaseCancelled":
                productPurchaseCancelled(argument);
                break;
            case "productsReceived":
                productsReceived(argument);
                break;
            case "productsRequestDidFail":
                productsRequestDidFail(argument);
                break;
            case "validateReceiptFailed":
                validateReceiptFailed(argument);
                break;
            case "validateReceiptRawResponse":
                validateReceiptRawResponse(argument);
                break;
            case "validateReceiptFinished":
                validateReceiptFinished(argument);
                break;
            case "restoreCompletedTransactionsFailed":
                restoreCompletedTransactionsFailed(argument);
                break;
            case "restoreCompletedTransactionsFinished":
                restoreCompletedTransactionsFinished(argument);
                break;
            default:
                Debug.LogError("StoreKitMacManager: Method (" + methodName + ") could not be found.");
                break;
        }
    }

    #region MAS callback functions

    private void productPurchased(string returnValue)
    {
        // split up into useful data
        string[] receiptParts = returnValue.Split(new string[] { "|||" }, StringSplitOptions.RemoveEmptyEntries);
        if (receiptParts.Length != 3)
        {
            if (purchaseFailed != null)
                purchaseFailed("Could not parse receipt information: " + returnValue);
            return;
        }

        string productIdentifier = receiptParts[0];
        string receipt = receiptParts[1];
        int quantity = int.Parse(receiptParts[2]);

        if (purchaseSuccessful != null)
            purchaseSuccessful(productIdentifier, receipt, quantity);
    }

    private void productPurchaseFailed(string error)
    {
        if (purchaseFailed != null)
            purchaseFailed(error);
    }

    private void productPurchaseCancelled(string error)
    {
        if (purchaseCancelled != null)
            purchaseCancelled(error);
    }

    private void productsReceived(string productString)
    {
        var productList = new List<StoreKitMacProductModel>();

        // parse out the products
        string[] productParts = productString.Split(new string[] { "||||" }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < productParts.Length; i++)
            productList.Add(StoreKitMacProductModel.productFromString(productParts[i]));

        if (productListReceived != null)
            productListReceived(productList);
    }

    private void productsRequestDidFail(string error)
    {
        if (productListRequestFailed != null)
            productListRequestFailed(error);
    }

    private void validateReceiptFailed(string error)
    {
        if (receiptValidationFailed != null)
            receiptValidationFailed(error);
    }

    private void validateReceiptRawResponse(string response)
    {
        if (receiptValidationRawResponseReceived != null)
            receiptValidationRawResponseReceived(response);
    }

    private void validateReceiptFinished(string statusCode)
    {
        if (statusCode == "0")
        {
            if (receiptValidationSuccessful != null)
                receiptValidationSuccessful();
        }
        else
        {
            if (receiptValidationFailed != null)
                receiptValidationFailed("Receipt validation failed with statusCode: " + statusCode);
        }
    }

    private void restoreCompletedTransactionsFailed(string error)
    {
        if (restoreTransactionsFailed != null)
            restoreTransactionsFailed(error);
    }

    private void restoreCompletedTransactionsFinished(string empty)
    {
        if (restoreTransactionsFinished != null)
            restoreTransactionsFinished();
    }

    #endregion
}
#endif