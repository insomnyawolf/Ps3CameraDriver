using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using VirtualCameraCommon;

namespace Ps3CameraDriver;

public partial class Ps3CamDriver
{
    public FrameQueue FrameQueue = null!;
    private static readonly BayerFilter BayerFilter = new BayerFilter();

    private void StartTransfer()
    {
        var rawBufferSize = FrameConfiguration.PixelCount;

        FrameQueue = new FrameQueue(rawBufferSize, MaxFramesInBuffer);

        var streamEndpoint = FindStreamEndpoint();

        var bufferSize = streamEndpoint.MaxPacketSize;

        var endpointAddress = (ReadEndpointID)streamEndpoint.EndpointAddress;

        var reader = UsbDevice.OpenEndpointReader(endpointAddress, rawBufferSize, EndpointType.Bulk);

        ReadStreamData(reader, rawBufferSize);
    }

    private int WholeFrameCounter = 0;
    private int OtherCounter = 0;
    private int Tail = 0;

    public struct ReadFrameConfig
    {
        public uint Stride;
        public int DstBufferSize;
        public int SrcBufferSize;
    }

    private void ReadStreamData(UsbEndpointReader usbEndpointReader, int bufferSize)
    {
        var fcc = FrameConfiguration;
        var size = fcc.VideoSize;
        var fccBufferSize = fcc.FrameBufferSize;
        var dstStride = fcc.Stride;

        while (IsStreaming)
        {
            ReadOne(usbEndpointReader, bufferSize, size, fccBufferSize, dstStride);
        }
    }

    private void ReadOne(UsbEndpointReader usbEndpointReader, int bufferSize, VideoSize size, int fccBufferSize, uint dstStride)
    {
        // I'm using stack-allocked buffers because they do not cause memory corruption errors
        Span<byte> destSpan = stackalloc byte[fccBufferSize];

        // Image data

        //Console.WriteLine("Reading Frame");

        Span<byte> buffer = stackalloc byte[bufferSize];

        var ec = usbEndpointReader.Read(buffer, offset: 0, count: bufferSize, Timeout, out var bytesRead);

        // ??
        //if (bytesRead == 0)
        //{
        //    return;
        //}

        if (ec != Error.Success)
        {
            throw new Exception(string.Format($"Error: '{ec}'. Bytes read: '{bytesRead}"));
        }

        if (bytesRead == bufferSize)
        {
            // Whole Frames?
            WholeFrameCounter++;


            // number of bytes from one row of pixels in memory to the next row of pixels in memory
            // almost there, first and last pixel are not existing in bayern data so we need that to make the stride be correct
            uint srcStride = size.Width + 2;

#warning test if the filter works properly
            BayerFilter.ProcessFilter(size, buffer, srcStride, destSpan, dstStride);

            FrameQueue.GetBufferToWrite(destSpan);
        }
        else
        {
            // Partial Frames ??
            // Control Data ??
            // Possible errors
            OtherCounter++;
        }

        Tail = (Tail + 1) % MaxFramesInBuffer;

        //Console.Write("\r                                                              ");

        Console.Write($"\rStats: WholeFrames:{WholeFrameCounter} Other:{OtherCounter} Tail:{Tail}");
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
