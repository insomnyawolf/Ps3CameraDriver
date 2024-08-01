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

        RawBuffer = new byte[rawBufferSize + StreamPadding];

        RawBufferClean = new byte[rawBufferSize];

        DecodedBuffer = new byte[decodedSize];

        FrameQueue = new FrameQueue(decodedSize, this);

        var streamEndpoint = FindStreamEndpoint();

        var deviceBufferSize = streamEndpoint.MaxPacketSize;

        var endpointAddress = (ReadEndpointID)streamEndpoint.EndpointAddress;

        UsbEndpointReader = UsbDevice.OpenEndpointReader(endpointAddress, deviceBufferSize, EndpointType.Bulk);

        ReadStreamData(UsbEndpointReader, deviceBufferSize);
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
    private byte[] RawBufferClean;
    private byte[] DecodedBuffer;

    private int Frame = 0;
    private int Other = 0;

    private int[] UnknownPacketSizes = new int[]
    {
        12,
        40,
        64,
        72,
    };

    private void ReadOne(UsbEndpointReader usbEndpointReader, int deviceBufferSize, VideoSize size, int fccBufferSize, uint dstStride)
    {
        // I'm using stack-allocked buffers because they do not cause memory corruption errors
        var rawBufferLength = RawBuffer.Length;

        // If you don't read thre data like that the driver insta crashes
        var rawBufferIndex = 0;

        Span<byte> buffer = stackalloc byte[deviceBufferSize];

        var readToPadding = false;

        while (true)
        {
            Console.Write($"\rStats: Frame:{Frame} Other:{Other}");

            var ec = usbEndpointReader.Read(buffer, offset: 0, count: deviceBufferSize, Timeout, out var bytesRead);

            if (ec != Error.Success)
            {
                throw new Exception(string.Format($"Error: '{ec}'. Bytes read: '{bytesRead}"));
            }

            if (bytesRead == 0)
            {
                // sanity check but it should not happen (?)
                continue;
            }

            //if (UnknownPacketSizes.Contains(bytesRead))
            //{
            //    Other++;
            //    // sanity check but it should not happen (?)
            //    continue;
            //}

            var bytesRemeaning = bytesRead;

            var srcBufferIndex = 0;

            while (bytesRemeaning > 0)
            {
                var toReadNow = bytesRemeaning;

                var possibleMaxAddress = rawBufferIndex + bytesRemeaning;

                var addressDiff = rawBufferLength - possibleMaxAddress;

                if (addressDiff < 0)
                {
                    // Will count enough to fill the buffer
                    toReadNow += addressDiff;
                }

                var srcBuffer = buffer.Slice(srcBufferIndex, toReadNow);

                var dstBuffer = new Span<byte>(RawBuffer, rawBufferIndex, toReadNow);

                srcBuffer.CopyTo(dstBuffer);

                bytesRemeaning -= toReadNow;
                rawBufferIndex += toReadNow;

                if (rawBufferIndex == rawBufferLength)
                {
                    rawBufferIndex = 0;
                    ProcessFrame();
                }
            }
        }
    }

    private void ProcessFrame()
    {
        // I'm using stack-allocked buffers because they do not cause memory corruption errors
        var rawBufferLength = RawBuffer.Length;

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

            if (indexSrc + batchSize > rawBufferLength)
            {
                // Ignore ending padding
                batchSize = rawBufferLength - indexSrc;
            }

            var srcBuffer = new Span<byte>(RawBuffer, indexSrc, batchSize);

            // Advance pointer over read bytes
            indexSrc += batchSizeBase;

            var copyLength = srcBuffer.Length;

            var dstBuffer = new Span<byte>(RawBufferClean, indexDst, copyLength);

            srcBuffer.CopyTo(dstBuffer);

            indexDst += copyLength;

            if (indexDst >= maxLength)
            {
                break;
            }
        }

        // number of bytes from one row of pixels in memory to the next row of pixels in memory
        // almost there, first and last pixel are not existing in bayern data so we need that to make the stride be correct
        //uint srcStride = size.Width;
        //uint srcStride = size.Width + 2;

#warning test if the filter works properly

        //BayerFilter.ProcessFilter(size, RawBuffer, srcStride, DecodedBuffer, dstStride);

        var frameBuffer = FrameQueue.WriteFrame();

        RawBufferClean.CopyTo(frameBuffer, 0);

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
