namespace VirtualCameraCommon;

public readonly record struct FrameConfiguration
{
    public readonly ColorFormat ColorFormat;
    public readonly VideoSize VideoSize;
    public readonly int FramesPerSecond;

    public readonly byte BytesPerPixel;

    /// <summary>
    /// The stride is the number of bytes from one row of pixels in memory to the next row of pixels in memory.
    /// </summary>
    public readonly int Stride;
    public readonly int FrameBufferSize;
    
    public int PixelCount => VideoSize.PixelCount;

    public FrameConfiguration(VideoSize VideoSize, int FramesPerSecond, ColorFormat ColorFormat)
    {
        this.VideoSize = VideoSize;
        this.FramesPerSecond = FramesPerSecond;
        this.ColorFormat = ColorFormat;
        this.BytesPerPixel = GetBytesPerPixel(ColorFormat);
        this.Stride = GetStride(BytesPerPixel, VideoSize.Width);
        this.FrameBufferSize = GetBufferSize(VideoSize.PixelCount, BytesPerPixel);
    }

    public static FrameConfiguration Default => new FrameConfiguration(
        VideoSize: VideoSize.VGA,
        FramesPerSecond: 60,
        ColorFormat: ColorFormat.RGB
    );

    public static FrameConfiguration LowResLowFramerate => new FrameConfiguration(
        VideoSize: VideoSize.QVGA,
        FramesPerSecond: 30,
        ColorFormat: ColorFormat.RGB
    );

    private static int GetStride(int BytesPerPixel, int Width)
    {
        return BytesPerPixel * Width;
    }

    private static int GetBufferSize(int PixelCount, int BytesPerPixel)
    {
        var size = PixelCount * BytesPerPixel;

        return size;
    }

    private static byte GetBytesPerPixel(ColorFormat colorFormat)
    {
        if (colorFormat == ColorFormat.Bayer)
        {
            return 1;
        }

        if (colorFormat == ColorFormat.Gray)
        {
            return 1;
        }

        if (colorFormat == ColorFormat.RGB)
        {
            return 3;
        }

        if (colorFormat == ColorFormat.BGR)
        {
            return 3;
        }

        //if (colorFormat == ColorFormat.RGBA)
        //{
        //    return 4;
        //}

        //if (colorFormat == ColorFormat.BGRA)
        //{
        //    return 4;
        //}

        throw new NotImplementedException();
    }
}