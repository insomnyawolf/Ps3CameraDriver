# Ps3CameraDriver

UserSpace PS3 Camera Driver (that hopefully doesn't hang when the connection is interrupted)

Heavily referencing [https://github.com/AllanCat/PS3EYEDriver](https://github.com/AllanCat/PS3EYEDriver)

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

