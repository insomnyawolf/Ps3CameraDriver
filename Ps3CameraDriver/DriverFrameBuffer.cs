using LibUsbDotNet.Info;
using LibUsbDotNet.Main;

namespace Ps3CameraDriver;

public partial class Ps3CamDriver
{
    private FrameQueue FrameQueue = null!;

    public async Task StartTransfer()
    {
        var size = GetSize();

        var bufferLength = size.GetBufferLength();

        FrameQueue = new FrameQueue(bufferLength);

        var streamEndpoint = FindStreamEndpoint();

#warning  To do read from that endpoint
        // var asd = UsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);
    }

#warning I know this sucks, i will refactor it later i promise
    public UsbEndpointInfo FindStreamEndpoint()
    {
        var configs = UsbDevice.Configs;
        for (int i = 0; i < configs.Count; i++)
        {
            var config = configs[i];

            var interfaces = config.Interfaces;

            for (int j = 0; j < interfaces.Count; j++)
            {
                var interf = interfaces[j];

                if (interf.Number != 0)
                {
                    continue;
                }

                var endpoints = interf.Endpoints;

                for (int x = 0; x < endpoints.Count; x++)
                {
                    var endpoint = endpoints[x];

                    if (endpoint.MaxPacketSize == 0)
                    {
                        continue;
                    }

                    var mask = endpoint.Attributes & (byte)EndpointType.Bulk;

                    if (mask == (int)EndpointType.Bulk)
                    {
                        return endpoint;
                    }
                }

            }
        }

        throw new Exception();
    }

    public void CloseTransfer()
    {

    }
}

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

    public Stream AddFrame()
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

        WriteIndex++;

        frame.Position = 0;

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