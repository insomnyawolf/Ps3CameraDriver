using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

namespace Ps3CameraDriver;

public partial class Ps3CamDriver
{
    public FrameQueue FrameQueue = null!;
    private static readonly BayerFilter BayerFilter = new BayerFilter();

    private void StartTransfer()
    {
        var rawBufferSize = FrameConfiguration.PixelCount;

        FrameQueue = new FrameQueue(rawBufferSize);

        var streamEndpoint = FindStreamEndpoint();

        var bufferSize = streamEndpoint.MaxPacketSize;

        var endpointAddress = (ReadEndpointID)streamEndpoint.EndpointAddress;

        var reader = UsbDevice.OpenEndpointReader(endpointAddress, rawBufferSize, EndpointType.Bulk);

        ReadStreamData(reader, rawBufferSize);
    }

    private int WholeFrameCounter = 0;
    private int OtherCounter = 0;

    private void ReadStreamData(UsbEndpointReader usbEndpointReader, int bufferSize)
    {
        var fcc = FrameConfiguration;
        var size = fcc.VideoSize;
        var fccBufferSize = fcc.FrameBufferSize;
        var stride = fcc.Stride;

        Span<byte> buffer = stackalloc byte[bufferSize];
        Span<byte> destSpan = stackalloc byte[fccBufferSize];
        while (IsStreaming)
        {
            // If the device hasn't sent data in the last 5 seconds,
            // a timeout error (ec = IoTimedOut) will occur. 
            var ec = usbEndpointReader.Read(buffer, Timeout, out var bytesRead);

            if (ec != Error.Success)
            {
                throw new Exception(string.Format($"Error: '{ec}'. Bytes read: '{bytesRead}"));
            }

            if (bytesRead == bufferSize)
            {
                // Whole Frames?
                WholeFrameCounter++;

#warning test if the filter works properly
                BayerFilter.ProcessFilter(size, buffer, destSpan, stride);

                FrameQueue.AddFrame(destSpan);
            }
            else
            {
                // Partial Frames ??
                // Control Data ??
                OtherCounter++;
            }

            Console.Write("\r                                                              ");

            Console.Write($"\rStats: WholeFrames:{WholeFrameCounter} Other:{OtherCounter}");
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
