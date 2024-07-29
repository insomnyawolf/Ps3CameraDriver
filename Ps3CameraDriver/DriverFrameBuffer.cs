using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System;
using VirtualCameraCommon;

namespace Ps3CameraDriver;

public partial class Ps3CamDriver
{
    public FrameQueue FrameQueue = null!;
    private static readonly BayerFilter BayerFilter = new BayerFilter();

    private void StartTransfer()
    {
        var rawBufferSize = FrameConfiguration.PixelCount;

        var decodedSize = FrameConfiguration.FrameBufferSize;

        RawBuffer = new byte[rawBufferSize + 2048];

        DecodedBuffer = new byte[decodedSize];

        FrameQueue = new FrameQueue(decodedSize);

        var streamEndpoint = FindStreamEndpoint();

        var deviceBufferSize = streamEndpoint.MaxPacketSize;

        var endpointAddress = (ReadEndpointID)streamEndpoint.EndpointAddress;

        var reader = UsbDevice.OpenEndpointReader(endpointAddress, deviceBufferSize, EndpointType.Bulk);

        ReadStreamData(reader, deviceBufferSize);
    }

    private int FrameCounter = 0;
    private int Block456ByteCounter = 0;
    private int OtherCounter = 0;

    private void ReadStreamData(UsbEndpointReader usbEndpointReader, int deviceBufferSize)
    {
        var fcc = FrameConfiguration;
        var size = fcc.VideoSize;
        var fccBufferSize = fcc.FrameBufferSize;
        var dstStride = fcc.Stride;

        while (IsStreaming)
        {
            ReadOne(usbEndpointReader, deviceBufferSize, size, fccBufferSize, dstStride);
        }
    }

    private byte[] RawBuffer;
    private byte[] DecodedBuffer;

    private int Frame = 0;
    private int Other = 0;
    private void ReadOne(UsbEndpointReader usbEndpointReader, int deviceBufferSize, VideoSize size, int fccBufferSize, uint dstStride)
    {
        // I'm using stack-allocked buffers because they do not cause memory corruption errors
        Span<byte> readBuffer = stackalloc byte[deviceBufferSize];

        var discard = 8;

        if (discard > 0)
        {
            var ec = usbEndpointReader.Read(readBuffer, offset: 0, count: discard, Timeout, out var bytesRead);
        }

        int readIndex = 0;

        while (true)
        {
            var ec = usbEndpointReader.Read(readBuffer, offset: 0, count: deviceBufferSize, Timeout, out var bytesRead);

            if (bytesRead < 1)
            {
                return;
            }

            if (ec != Error.Success)
            {
                throw new Exception(string.Format($"Error: '{ec}'. Bytes read: '{bytesRead}"));
            }

            var slice = readBuffer.Slice(0, bytesRead);

            slice.CopyTo(new Span<byte>(RawBuffer, readIndex, bytesRead));

            readIndex += bytesRead;

            if (bytesRead < deviceBufferSize)
            {
                break;
            }
        }

        if (readIndex < 8192)
        {
            // Other data ?
            Other++;
        }
        else
        {
            Frame++;
            // number of bytes from one row of pixels in memory to the next row of pixels in memory
            // almost there, first and last pixel are not existing in bayern data so we need that to make the stride be correct
            uint srcStride = size.Width + 2;

#warning test if the filter works properly

            BayerFilter.ProcessFilter(size, RawBuffer, srcStride, DecodedBuffer, dstStride);

            var dstBuffer = FrameQueue.WriteFrame();

            DecodedBuffer.CopyTo(dstBuffer, 0);
        }

        Console.Write($"\rStats: Frame:{Frame} Other:{Other}");
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
