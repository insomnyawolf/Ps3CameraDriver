# Ps3CameraDriver

UserSpace PS3 Camera Driver (that hopefully doesn't hang when the connection is interrupted)

Heavily referencing 

* [https://github.com/AllanCat/PS3EYEDriver](https://github.com/AllanCat/PS3EYEDriver)
* [https://github.com/smourier/VCamNetSample](https://github.com/smourier/VCamNetSample)

Clone the repo in this directory if you don't wanna have torubles with the virtual camera registration:

```
C:\VCameraWorkaroundFolder
```

This repo contains a submodule, remember cloning with

```
git clone --recursive
```

Other useful commands:

```
git submodule update --remote --merge
```

The driver mostly work but i fucked up something on the debayern pattern or on the sensor init (idk whichone)

WIC is a fucking nightmare, making this appear as a camera is a challenge and it's windows fault, they didnt need to make it so complicated.


## Current issues

May need some help

### Camera image decoding issue

De-bayer filter may not working properly

Maybe i am not reading the sensor data properly lol

Even with a simple filter the image is broken, looks like it gets shifted each couple rows a bit

### Camera output performance is bad

I am trying to optimize it but for some reason it breaks when i try to do so ;-;