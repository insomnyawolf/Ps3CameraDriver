namespace Ps3CameraDriver;

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
