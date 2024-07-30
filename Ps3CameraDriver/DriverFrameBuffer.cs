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
    const int StreamPadding = 456;

    private void StartTransfer()
    {
        var rawBufferSize = FrameConfiguration.PixelCount;

        var decodedSize = FrameConfiguration.FrameBufferSize;

        RawBufferClean = new byte[rawBufferSize];

        RawBuffer = new byte[rawBufferSize + StreamPadding];

        DecodedBuffer = new byte[decodedSize];

        FrameQueue = new FrameQueue(decodedSize, this);

        var streamEndpoint = FindStreamEndpoint();

        var deviceBufferSize = streamEndpoint.MaxPacketSize;

        var endpointAddress = (ReadEndpointID)streamEndpoint.EndpointAddress;

        UsbEndpointReader = UsbDevice.OpenEndpointReader(endpointAddress, deviceBufferSize, EndpointType.Bulk);

        ReadStreamData(UsbEndpointReader, deviceBufferSize);
    }

    UsbEndpointReader UsbEndpointReader;

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
    private byte[] RawBufferClean;
    private byte[] DecodedBuffer;

    private int Frame = 0;
    private int Other = 0;
    private void ReadOne(UsbEndpointReader usbEndpointReader, int deviceBufferSize, VideoSize size, int fccBufferSize, uint dstStride)
    {
        // I'm using stack-allocked buffers because they do not cause memory corruption errors
        var rbl = RawBuffer.Length;

        // If you don't read thre data like that the driver insta crashes
        var idx = 0;
        Span<byte> buffer = stackalloc byte[deviceBufferSize];
        while (true)
        {
            var ec = usbEndpointReader.Read(buffer, offset: 0, count: deviceBufferSize, Timeout, out var bytesRead);

            if (ec != Error.Success)
            {
                throw new Exception(string.Format($"Error: '{ec}'. Bytes read: '{bytesRead}"));
            }

            var srcBuffer = buffer.Slice(0, bytesRead);

            var dstBuffer = new Span<byte>(RawBuffer, idx, bytesRead);

            idx += bytesRead;

            srcBuffer.CopyTo(dstBuffer);

            if (bytesRead != deviceBufferSize)
            {
                break;
            }
        }

        //if (bytesRead == 0)
        //{
        //    break;
        //}

        if (idx != rbl)
        {
            // Other data ?
            Other++;
        }
        else
        {
            Frame++;
        }

        // padding is 456 => 38 * 12 BYTES
        const int ratio = 6 * 2;

        // Magic number
        const int batchSizeBase = 160 * ratio;

        var indexSrc = 0;
        var indexDst = 0;

        var cbl = RawBufferClean.Length;
        var maxLength = cbl - batchSizeBase;

        while (true)
        {
            var batchSize = batchSizeBase;

            //const int totalDiscard = 1 * ratio;
            //const int discardStart = totalDiscard;

            //indexSrc += discardStart;
            //batchSize -= discardStart;

            if (indexDst >= maxLength)
            {
                break;
            }

            if (indexSrc + batchSize > rbl)
            {
                // Ignore ending padding
                batchSize = rbl - indexSrc;
            }

            var srcBuffer = new Span<byte>(RawBuffer, indexSrc, batchSize);

            // Advance pointer over read bytes
            indexSrc += batchSizeBase;

            var copyLength = srcBuffer.Length;

            var dstBuffer = new Span<byte>(RawBufferClean, indexDst, copyLength);

            srcBuffer.CopyTo(dstBuffer);

            indexDst += copyLength;

        }

        // number of bytes from one row of pixels in memory to the next row of pixels in memory
        // almost there, first and last pixel are not existing in bayern data so we need that to make the stride be correct
        //uint srcStride = size.Width;
        //uint srcStride = size.Width + 2;

#warning test if the filter works properly

        //BayerFilter.ProcessFilter(size, RawBuffer, srcStride, DecodedBuffer, dstStride);

        var frameBuffer = FrameQueue.WriteFrame();

        RawBufferClean.CopyTo(frameBuffer, 0);

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
}
