using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StoreKitMacEventListener : MonoBehaviour
{
#if UNITY_STANDALONE_OSX
	void Start()
	{
		// Listens to all the StoreKit events.  All event listeners MUST be removed before this object is disposed!
		StoreKitMacManager.purchaseSuccessful += purchaseSuccessful;
		StoreKitMacManager.purchaseCancelled += purchaseCancelled;
		StoreKitMacManager.purchaseFailed += purchaseFailed;
		StoreKitMacManager.receiptValidationFailed += receiptValidationFailed;
		StoreKitMacManager.receiptValidationRawResponseReceived += receiptValidationRawResponseReceived;
		StoreKitMacManager.receiptValidationSuccessful += receiptValidationSuccessful;
		StoreKitMacManager.productListReceived += productListReceived;
		StoreKitMacManager.productListRequestFailed += productListRequestFailed;
		StoreKitMacManager.restoreTransactionsFailed += restoreTransactionsFailed;
		StoreKitMacManager.restoreTransactionsFinished += restoreTransactionsFinished;
	}
	
	void OnDisable()
	{
		// Remove all the event handlers
		StoreKitMacManager.purchaseSuccessful -= purchaseSuccessful;
		StoreKitMacManager.purchaseCancelled -= purchaseCancelled;
		StoreKitMacManager.purchaseFailed -= purchaseFailed;
		StoreKitMacManager.receiptValidationFailed -= receiptValidationFailed;
		StoreKitMacManager.receiptValidationRawResponseReceived -= receiptValidationRawResponseReceived;
		StoreKitMacManager.receiptValidationSuccessful -= receiptValidationSuccessful;
		StoreKitMacManager.productListReceived -= productListReceived;
		StoreKitMacManager.productListRequestFailed -= productListRequestFailed;
		StoreKitMacManager.restoreTransactionsFailed -= restoreTransactionsFailed;
		StoreKitMacManager.restoreTransactionsFinished -= restoreTransactionsFinished;
	}
	
	
	void productListReceived( List<StoreKitMacProductModel> productList )
	{
	}
	
	void productListRequestFailed( string error )
	{
	}
	
	void receiptValidationSuccessful()
	{
	}
	
	void receiptValidationFailed( string error )
	{
	}
		
	void receiptValidationRawResponseReceived( string response )
	{
		//Debug.Log( "receipt validation raw response: " + response );
	}

	void purchaseFailed( string error )
	{
		//Debug.Log( "purchase failed with error: " + error );
	}
	
	void purchaseCancelled( string error )
	{
		//Debug.Log( "purchase cancelled with error: " + error );
	}
	
	void purchaseSuccessful( string productIdentifier, string receipt, int quantity )
	{
		//Debug.Log( "purchased product: " + productIdentifier + ", quantity: " + quantity );
	}
	
	void restoreTransactionsFailed( string error )
	{
		//Debug.Log( "restoreTransactionsFailed: " + error );
	}
	
	void restoreTransactionsFinished()
	{
		//Debug.Log( "restoreTransactionsFinished" );
	}
#endif
}