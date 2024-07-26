namespace Ps3CameraDriver;

public class FrameQueue
{
    public const int MaxFramesInBuffer = 20;
    public const int MaxLength = MaxFramesInBuffer - 1;
    private readonly List<Stream> FrameBuffers = new();

    private int WriteIndex;
    private int ReadIndex;

    public FrameQueue(int bufferSize)
    {
        for (int i = 0; i < MaxFramesInBuffer; i++)
        {
            FrameBuffers.Add(new MemoryStream(bufferSize));
        }
    }

    // If too much frames in queue rewrite the last

    public Stream AddFrame(Span<byte> frameBuffer)
    {
        if (WriteIndex > MaxLength)
        {
            WriteIndex = 0;
        }

        if (ReadIndex == WriteIndex)
        {
            WriteIndex--;
        }

        if (WriteIndex < 0)
        {
            WriteIndex = MaxLength;
        }

        var frame = FrameBuffers[WriteIndex];

        frame.Position = 0;

        frame.Write(frameBuffer);

        WriteIndex++;

        return frame;
    }

    // If not enough frames replay the last frame

    public Stream ReadFrame()
    {
        if (ReadIndex > MaxLength)
        {
            ReadIndex = 0;
        }

        if (ReadIndex == WriteIndex)
        {
            ReadIndex--;
        }

        if (ReadIndex < 0)
        {
            ReadIndex = MaxLength;
        }

        var frame = FrameBuffers[ReadIndex];

        ReadIndex++;

        return frame;
    }
};