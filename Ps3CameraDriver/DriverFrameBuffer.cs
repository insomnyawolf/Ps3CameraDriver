using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System.Security.Claims;
using System.Text;

namespace Ps3CameraDriver;

public partial class Ps3CamDriver
{
    private FrameQueue FrameQueue = null!;

    private void StartTransfer()
    {
        var size = GetSize();

        var bufferLength = size.GetBufferLength();

        FrameQueue = new FrameQueue(bufferLength);

        var streamEndpoint = FindStreamEndpoint();

        var bufferSize = streamEndpoint.MaxPacketSize;

        var endpointAddress = (ReadEndpointID)streamEndpoint.EndpointAddress;

        var reader = UsbDevice.OpenEndpointReader(endpointAddress, bufferSize, EndpointType.Bulk);

        ReadStreamData(reader, bufferSize);
    }

    private void ReadStreamData(UsbEndpointReader usbEndpointReader, int bufferSize)
    {
        Span<byte> buffer = stackalloc byte[bufferSize];

        while (IsStreaming)
        {
            // If the device hasn't sent data in the last 5 seconds,
            // a timeout error (ec = IoTimedOut) will occur. 
            var ec = usbEndpointReader.Read(buffer, Timeout, out var bytesRead);

            if (ec != Error.Success)
            {
                throw new Exception(string.Format($"Error: '{ec}'. Bytes read: '{bytesRead}"));
            }

            // Write that output to the console.

            var raw = Convert.ToBase64String(buffer);

            Console.Write(raw);
        }
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