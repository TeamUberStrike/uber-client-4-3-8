# Make sure valid parameters have been passed
if [ -z "$1" ]; then 
echo Parameter 1 not specified. Source application path. i.e. ../Latest/UberStrike.app 
echo Example: buildMacAppStore.sh ../Latest/OsxStandalone/UberStrike.app ./UberStrike.app 4.3.3.1864 4.3.3
exit 1
fi

if [ -z "$2" ]; then 
echo Parameter 2 not specified. Destination application path. i.e. ./PublishedUberStrike.app
echo Example: buildMacAppStore.sh ../Latest/OsxStandalone/UberStrike.app ./UberStrike.app 4.3.3.1864 4.3.3
exit 1
fi

if [ -z "$3" ]; then 
echo Parameter 3 not specified. Full application version. i.e. 4.3.3.1864
echo Example: buildMacAppStore.sh ../Latest/OsxStandalone/UberStrike.app ./UberStrike.app 4.3.3.1864 4.3.3
exit 1
fi 

if [ -z "$4" ]; then 
echo Parameter 4 not specified. Short application version. i.e. 4.3.3
echo Example: buildMacAppStore.sh ../Latest/OsxStandalone/UberStrike.app ./UberStrike.app 4.3.3.1864 4.3.3
exit 1
fi 

#Start creating the MAS build

echo ****Remove old build****
rm -R $2

echo ****Stripping unsupported architectures from $1, saving as $2****
ditto -v --arch i386 $1 $2

echo ****Copy UberStrike.icns****
cp ../Assets/Artwork/GUI/StandaloneIcons/UberStrike.icns ./UberStrike.icns

echo ****Copy icon and clean up legacy resources****
mv ./UberStrike.icns $2/Contents/Resources
rm $2/Contents/Resources/UnityPlayer.icns
rm $2/Contents/Resources/ScreenSelector.tif

echo Update Info.plist
sed -e 's/UsFullVersion/'"$3"'/g' -e 's/UsShortVersion/'"$4"'/g' ./Info.template.plist > $2/Contents/Info.plist

echo ****Fix App Permissions****
chmod -R a+rwx $2

echo **** Codesigning libmono.0.dylib****
codesign -f -v -s "3rd Party Mac Developer Application: CMUNE Ltd" $2/Contents/Frameworks/MonoEmbedRuntime/osx/libmono.0.dylib

echo **** Codesigning StoreKitPlugin.bundle****
codesign -f -v -s "3rd Party Mac Developer Application: CMUNE Ltd" $2/Contents/Plugins/StoreKitPlugin.bundle

echo **** Codesigning StoreKitReceiptValidator.bundle****
codesign -f -v -s "3rd Party Mac Developer Application: CMUNE Ltd" $2/Contents/Plugins/StoreKitReceiptValidator.bundle

echo ****Codesigning UberStrike.app with Sandboxing****
codesign -f -v -s "3rd Party Mac Developer Application: CMUNE Ltd" --entitlements UberStrike.entitlements $2

echo ****Build the pkg****
productbuild --component $2 "/Applications" --sign "3rd Party Mac Developer Installer: CMUNE Ltd" "./UberStrike.pkg"

