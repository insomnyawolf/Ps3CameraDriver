using System.Runtime.InteropServices;

namespace Ps3CameraDriver;

public class FrameQueue
{
    public const int MaxFramesInBuffer = Ps3CamDriver.MaxFramesInBuffer;
    public const int MaxLength = MaxFramesInBuffer - 1;
    private readonly List<byte[]> FrameBuffers = new();

    private int WriteIndex = 0;
    private int ReadIndex = 1;
    private Ps3CamDriver Ps3CamDriver;
    public FrameQueue(int bufferSize, Ps3CamDriver Ps3CamDriver)
    {
        this.Ps3CamDriver = Ps3CamDriver;

        for (int i = 0; i < MaxFramesInBuffer; i++)
        {
            FrameBuffers.Add(new byte[bufferSize]);
        }
    }

    // If too much frames in queue rewrite the last
    public byte[] WriteFrame()
    {
        WriteIndex++;

        if (ReadIndex == WriteIndex)
        {
            WriteIndex--;
        }

        if (WriteIndex > MaxLength)
        {
            WriteIndex = 0;
        }

        if (WriteIndex < 0)
        {
            WriteIndex = MaxLength;
        }

        var frame = FrameBuffers[WriteIndex];

        //frame.Position = 0;

        return frame;
    }

    // If not enough frames replay the last frame

    public byte[] StartReadFrame()
    {
        if (!Ps3CamDriver.IsStreaming)
        {
            throw new Exception("Error, camera not streaming");
        }
        
        ReadIndex++;

        if (ReadIndex == WriteIndex)
        {
            ReadIndex--;
        }

        if (ReadIndex > MaxLength)
        {
            ReadIndex = 0;
        }

        if (ReadIndex < 0)
        {
            ReadIndex = MaxLength;
        }

        var frame = FrameBuffers[ReadIndex];

        return frame;
    }

    public void FinishReadFrame()
    {
        ReadIndex++;
    }
};

//public class Frame
//{
//    public readonly Pixel[] Pixels;

//    public Frame(int pixelCount)
//    {
//        Pixels = new Pixel[pixelCount];
//    }

//    public unsafe void Read(Span<byte> source)
//    {
//        var length = source.Length;

//        var pixelIndex = 0;
//        var srcIndex = 0;

//        fixed (Pixel* dstPtr = Pixels)
//        {
//            fixed (byte* srcPtr = &source.GetPinnableReference())
//            {
//                while (srcIndex < length)
//                {
//                    var R = srcPtr[srcIndex++];
//                    var G = srcPtr[srcIndex++];
//                    var B = srcPtr[srcIndex++];

//                    var temp = new Pixel(R, G, B);

//                    dstPtr[pixelIndex] = temp;
//                }
//            }
//        }
//    }
//}

//public class Pixel
//{
//    public readonly byte R;
//    public readonly byte G;
//    public readonly byte B;
//    public Pixel(byte R, byte G, byte B)
//    {
//        this.R = R;
//        this.G = G;
//        this.B = B;
//    }

//    public byte GetGrey()
//    {
//        var r = R * 0.33;
//        var g = G * 0.34;
//        var b = B * 0.33;

//        var temp = r + g + b;

//        return (byte)temp;
//    }
//}