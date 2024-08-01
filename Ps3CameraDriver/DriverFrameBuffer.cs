using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System.Drawing;
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

        RawBuffer = new byte[rawBufferSize];

        DecodedBuffer = new byte[decodedSize];

        FrameQueue = new FrameQueue(decodedSize, this);

        var streamEndpoint = FindStreamEndpoint();

        var deviceBufferSize = streamEndpoint.MaxPacketSize;

        var endpointAddress = (ReadEndpointID)streamEndpoint.EndpointAddress;

        UsbEndpointReader = UsbDevice.OpenEndpointReader(endpointAddress, deviceBufferSize, EndpointType.Bulk);

        _ = Task.Run(() =>
        {
            try
            {
                ReadStreamData(UsbEndpointReader, deviceBufferSize);
            }
            catch
            {

            }
            finally
            {

                Stop();
            }
        });
    }

    UsbEndpointReader UsbEndpointReader;

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
    private int DesyncFrame = 0;

    private void ReadOne(UsbEndpointReader usbEndpointReader, int deviceBufferSize, VideoSize size, int fccBufferSize, uint dstStride)
    {
        // I'm using stack-allocked buffers because they do not cause memory corruption errors
        var rawBufferLength = RawBuffer.Length;

        // If you don't read thre data like that the driver insta crashes
        var dstIndex = 0;

        // 12 bytes of unknown data each 2kb
        const int paddingBytes = 12;
        const int unknownPaddingSpacing = 2048;

        var usbReadBufferSize = unknownPaddingSpacing;

        Span<byte> buffer = stackalloc byte[usbReadBufferSize];

        //var readToPadding = false;

        while (true)
        {
            Console.Write($"\rStats: Frame:{Frame} Other:{DesyncFrame}");

            var ec = usbEndpointReader.Read(buffer, offset: 0, count: usbReadBufferSize, Timeout, out var bytesRead);

            if (ec != Error.Success)
            {
                throw new Exception(string.Format($"Error: '{ec}'. Bytes read: '{bytesRead}"));
            }

            //#warning not correct
            //            if (bytesRead == StreamPadding)
            //            {
            //                dstIndex = 0;
            //            }

            var srcIndex = 0;

            var remeaningBytes = bytesRead;

            // Sync Byte?
            if (buffer[0] != 0x0C)
            {
                DesyncFrame++;
                break;
            }

            while (remeaningBytes > 0)
            {
                // Discard what we don't want
                srcIndex += paddingBytes;
                remeaningBytes -= paddingBytes;

                var maxPossibleRead = rawBufferLength - dstIndex;

                var readSize = remeaningBytes;

                if (readSize > maxPossibleRead)
                {
                    readSize = maxPossibleRead;
                }

                var srcBuffer = buffer.Slice(srcIndex, readSize);

                var destBuffer = new Span<byte>(RawBuffer, dstIndex, readSize);

                srcBuffer.CopyTo(destBuffer);

                // Advance pointers
                remeaningBytes -= readSize;
                srcIndex += readSize;
                dstIndex += readSize;

                if (dstIndex == rawBufferLength)
                {
                    //var b64 = Convert.ToBase64String(RawBuffer);
                    //head = (head + 1) % MaxFramesInBuffer;
                    //var ptr = head * rawBufferLength;
                    dstIndex = 0;
                    ProcessFrame();
                    break;
                }
            }
        }
    }

    private void ProcessFrame()
    {
        var frameBuffer = FrameQueue.WriteFrame();

        if (FrameConfiguration.ColorFormat == ColorFormat.Bayer)
        {
            RawBuffer.CopyTo(frameBuffer, 0);
        }
        else if (FrameConfiguration.ColorFormat == ColorFormat.RGB)
        {
            // number of bytes from one row of pixels in memory to the next row of pixels in memory
            // almost there, first and last pixel are not existing in bayern data so we need that to make the stride be correct
            var fc = FrameConfiguration;
            var size = fc.VideoSize;
            uint srcStride = size.Width;

#warning test if the filter works properly

            BayerFilter.ProcessFilter(size, RawBuffer, srcStride, DecodedBuffer);

            DecodedBuffer.CopyTo(frameBuffer, 0);
        }
        else
        {
            throw new NotImplementedException();
        }

        Frame++;
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
}
