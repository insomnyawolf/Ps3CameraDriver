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

## Working

* Resolution
	* QVGA
	* VGA
* Color
	* Bayer
	* BGR
* Framerate
	* 30
	* 60
	* Maybe others (untested)

* Tester app

## To do

* Read packet independently of transfer buffer size
* Fix windows camera driver
	* Transfer read size makes the camera driver break sometimes (maybe stack overflow?)
	* Improve performance

## Current issues

I need some help

* De-bayer filter with interpolation not working properly
* Frame Flickering when dropped frames occur
* Make frame reading not dependant on packet size to make the driver work properly in usb hubs

## Fixed issues

* Sync issues reading frames

### Weirdness:

* padding when reading from the sensor split in 12 bytes each 2048bytes

