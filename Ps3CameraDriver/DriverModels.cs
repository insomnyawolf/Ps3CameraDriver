using VirtualCameraCommon;

namespace Ps3CameraDriver;

public class CameraConfigurations
{
    public VideoSize VideoSize { get; init; }
    public IReadOnlyList<SensorConfiguration> SensorConfiguration { get; init; } = null!;
    public IReadOnlyList<Command> SensorStart { get; init; } = null!;
    public IReadOnlyList<Command> BridgeStart { get; init; } = null!;

    // _normalize_framerate()
    public SensorConfiguration GetSensorConfiguration(int framesPerSecond)
    {
        var candidates = SensorConfiguration;

        SensorConfiguration config = candidates[0];

        for (int i = 1; i < candidates.Count; i++)
        {
            var current = candidates[i];

            if (current.FramesPerSecond < framesPerSecond)
            {
                break;
            }

            config = current;
        }

        return config;
    }
}

public readonly struct Command
{
    public readonly byte Register;
    public readonly byte Value;

    public Command(byte Register, byte Value)
    {
        this.Register = Register;
        this.Value = Value;
    }
}

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
