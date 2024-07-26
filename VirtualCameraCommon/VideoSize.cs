namespace VirtualCameraCommon;

public readonly record struct VideoSize
{
    public readonly int Width;
    public readonly int Height;
    public readonly int PixelCount;

    public VideoSize(int Width, int Height)
    {
        this.Width = Width;
        this.Height = Height;
        this.PixelCount = Width * Height;
    }

    public static VideoSize VGA => new VideoSize(Width: 640, Height: 480);
    public static VideoSize QVGA => new VideoSize(Width: 320, Height: 240);
};