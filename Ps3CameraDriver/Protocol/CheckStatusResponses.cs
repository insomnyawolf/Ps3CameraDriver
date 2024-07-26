namespace Ps3CameraDriver;

public enum CheckStatusResponses : byte
{
    Ok = 0x00,
    Retry = 0x03,
    Error = 0x04,
};