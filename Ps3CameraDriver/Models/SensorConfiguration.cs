using Ps3CameraDriver.Protocol;

namespace Ps3CameraDriver.Models;

public struct SensorConfiguration
{
    private const RegisterOV534 Field1Address = (RegisterOV534)0x11;
    private const RegisterOV534 Field2Address = (RegisterOV534)0x0d;

    public int FramesPerSecond;

    // Maybe color configs?
    public byte Field1;
    public byte Field2;
    public byte Field3;

    public void WriteTo(Ps3CamDriver ps3CamDriver)
    {
        //sccb_reg_write(0x11, rate.r11);
        ps3CamDriver.SerialCameraControlBusRegisterWrite(Field1Address, Field1);
        //sccb_reg_write(0x0d, rate.r0d);
        ps3CamDriver.SerialCameraControlBusRegisterWrite(Field2Address, Field2);
        //ov534_reg_write(0xe5, rate.re5);
        ps3CamDriver.HardwareRegisterWrite(OperationsOV534.Unknown0xe5, Field3);
    }
};
