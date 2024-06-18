# Invoke with metal.sh iphoneos OR metal.sh iphonesimulator
xcrun -sdk $1  metal -c Test.metal -o default.air
xcrun -sdk $1  metallib default.air -o default.metallib
