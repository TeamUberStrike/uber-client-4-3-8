//
//  P31RadialBackgroundView.m
//  P31AlertView
//
//  Created by Mike on 9/12/10.
//  Copyright 2010 Prime31 Studios. All rights reserved.
//

#import "P31RadialBackgroundView.h"
#import <QuartzCore/QuartzCore.h>


@implementation P31RadialBackgroundView


- (id)initWithFrame:(CGRect)frame
{
    if( ( self = [super initWithFrame:frame] ) )
	{
        // Initialization code
		self.opaque = NO;
    }
    return self;
}


- (void)drawRect:(CGRect)rect
{
	CGContextRef ctx = UIGraphicsGetCurrentContext();
	
	CGColorSpaceRef space = CGColorSpaceCreateDeviceRGB();
	CGFloat comps[8] = {0.0, 0.0, 0.0, 0.4, 0, 0, 0, 0.7};
	CGFloat locs[2] = {0, 1};
	CGGradientRef gradient = CGGradientCreateWithColorComponents( space, comps, locs, 2 );
	
	CGFloat x = [self bounds].size.width / 2.0;
	CGFloat y = [self bounds].size.height / 2.0;

	CGContextDrawRadialGradient(ctx,
								gradient, 
								CGPointMake(x, y), 0, 
								CGPointMake(x, y), 160, 
								kCGGradientDrawsAfterEndLocation);
	CGColorSpaceRelease( space );
	CGGradientRelease( gradient );
}


- (void)dealloc
{
	// ARC handles deallocation automatically
}


@end
