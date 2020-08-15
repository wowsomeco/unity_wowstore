#if !TARGET_OS_TV

#import <Foundation/Foundation.h>

@interface WowPurchasing : NSObject
+ (id)   sharedInstance;
- (void) saveToCameraRoll:(NSString*)media;
@end

@implementation WowPurchasing

static WowPurchasing * _sharedInstance;
static UIImagePickerController * _imagePicker = NULL;

+ (id)sharedInstance {
    if (_sharedInstance == nil)  {
        _sharedInstance = [[self alloc] init];
    }
    return _sharedInstance;
}

- (void) saveToCameraRoll:(NSString *)media {
    NSData *imageData = [[NSData alloc] initWithBase64Encoding:media];
    UIImage *image = [UIImage imageWithData:imageData];
    UIImageWriteToSavedPhotosAlbum(image, nil, nil, nil);
}

extern "C" {
    //--------------------------------------
    //  IOS Native Plugin Section
    //--------------------------------------
    void _Cav_SaveToCameraRoll(char* encodedMedia) {
        NSString *media = [NSString stringWithUTF8String:encodedMedia];
        [[Cav_Camera sharedInstance] saveToCameraRoll:media];
    }
}

@end

#endif
