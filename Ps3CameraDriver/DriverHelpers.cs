using System.Runtime.CompilerServices;

namespace Ps3CameraDriver;

public partial class Ps3CamDriver
{
#warning optimize and unspaguetti this
    public static byte[] CopyToBuffer<TOrigin>(TOrigin value, byte[] buffer)
    {
        ClearBuffer(buffer);
        Unsafe.As<byte, TOrigin>(ref buffer[0]) = value;
        return UsbBuffer;
    }

#warning all the unchecked blocks may break things, better if you check before what's going on
    public static byte AsByte<TOrigin>(TOrigin value)
    {
        byte buffer = default;
        Unsafe.As<byte, TOrigin>(ref buffer) = value;
        return buffer;
    }

    public static void ClearBuffer(byte[] buffer)
    {
        Array.Clear(buffer, 0, buffer.Length);
    }
}