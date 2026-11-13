using System;
using System.Collections;
using System.Collections.Generic;


public class StoreKitiOSTransaction
{
    public string productIdentifier;
    public string base64EncodedTransactionReceipt;
    public int quantity;
	
	
	
	public static List<StoreKitiOSTransaction> transactionsFromJson( string json )
	{
		var transactionList = new List<StoreKitiOSTransaction>();
		
		ArrayList products = json.arrayListFromJson();
		foreach( Hashtable ht in products )
			transactionList.Add( transactionFromHashtable( ht ) );
		
		return transactionList;
	}
	

    public static StoreKitiOSTransaction transactionFromJson( string json )
    {
		return transactionFromHashtable( json.hashtableFromJson() );
    }
	
	
    public static StoreKitiOSTransaction transactionFromHashtable( Hashtable ht )
    {
        var transaction = new StoreKitiOSTransaction();
  		
		if( ht.ContainsKey( "productIdentifier" ) )
        	transaction.productIdentifier = ht["productIdentifier"].ToString();
		
		if( ht.ContainsKey( "base64EncodedReceipt" ) )
        	transaction.base64EncodedTransactionReceipt = ht["base64EncodedReceipt"].ToString();
		
		if( ht.ContainsKey( "quantity" ) )
        	transaction.quantity = int.Parse( ht["quantity"].ToString() );

        return transaction;
    }
	
	
	public override string ToString()
	{
		return string.Format( "<StoreKitTransaction>\nID: {0}\nReceipt: {1}\nQuantity: {2}", productIdentifier, base64EncodedTransactionReceipt, quantity );
	}
}

