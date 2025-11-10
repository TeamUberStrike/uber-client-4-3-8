using System;
using UnityEngine;

public class StoreKitMacTransaction
{
    public string ProductIdentifier;
    public string Base64EncodedTransactionReceipt;
    public int Quantity;

    public static StoreKitMacTransaction transactionFromString( string transactionString )
    {
        var transaction = new StoreKitMacTransaction();

        string[] transactionParts = transactionString.Split( new string[] { "|||" }, StringSplitOptions.None );
        if( transactionParts.Length == 3 )
        {
            transaction.ProductIdentifier = transactionParts[0];
            transaction.Base64EncodedTransactionReceipt = transactionParts[1];
            transaction.Quantity = int.Parse( transactionParts[2] );
        }

        return transaction;
    }
	
	public override string ToString()
	{
		return string.Format( "<StoreKitTransaction>\nID: {0}\nReceipt: {1}\nQuantity: {2}", ProductIdentifier, Base64EncodedTransactionReceipt, Quantity );
	}
}