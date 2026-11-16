//
//  StoreKitBinding.mm
//  Unity-iPhone
//
//  Created by Prime31 Studios on 11/8/10.
//

#import <StoreKit/StoreKit.h>
#import <Foundation/Foundation.h>

#if __has_feature(objc_arc)
	#define MakeStringCopy( _x_ ) ( _x_ != NULL && [_x_ isKindOfClass:[NSString class]] ) ? strdup( [_x_ UTF8String] ) : NULL
#else
	#define MakeStringCopy( _x_ ) ( _x_ != NULL && [_x_ isKindOfClass:[NSString class]] ) ? strdup( [_x_ UTF8String] ) : NULL
#endif

#define GetStringParam( _x_ ) ( _x_ != NULL ) ? [NSString stringWithUTF8String:_x_] : [NSString stringWithUTF8String:""]

extern "C" {

// Can make payments
bool _storeKitCanMakePayments()
{
    return [SKPaymentQueue canMakePayments];
}

// Request product data
void _storeKitRequestProductData( const char* productIds )
{
    // This is a stub implementation - you would need to implement a proper StoreKit manager
    NSString *productIdsString = GetStringParam(productIds);
    NSArray *productIdentifiers = [productIdsString componentsSeparatedByString:@","];
    NSSet *productSet = [NSSet setWithArray:productIdentifiers];
    
    SKProductsRequest *request = [[SKProductsRequest alloc] initWithProductIdentifiers:productSet];
    [request start];
}

// Purchase product
void _storeKitPurchaseProduct( const char* productId, int quantity )
{
    // Stub implementation
    NSString *productIdentifier = GetStringParam(productId);
    // Would need proper implementation with SKPayment and SKPaymentQueue
}

// Restore completed transactions
void _storeKitRestoreCompletedTransactions()
{
    [[SKPaymentQueue defaultQueue] restoreCompletedTransactions];
}

// Validate receipt
void _storeKitValidateReceipt( const char* receipt, bool isTest )
{
    // Stub implementation for receipt validation
}

// Validate auto-renewable receipt
void _storeKitValidateAutoRenewableReceipt( const char* receipt, const char* secret, bool isTest )
{
    // Stub implementation for auto-renewable receipt validation
}

// Get all saved transactions
const char* _storeKitGetAllSavedTransactions()
{
    // Stub implementation - return empty JSON array
    return MakeStringCopy(@"[]");
}

}
