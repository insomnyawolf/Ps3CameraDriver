namespace VirtualCameraCommon;

public readonly record struct VideoSize
{
    public readonly uint Width;
    public readonly uint Height;
    public readonly uint PixelCount;

    public VideoSize(uint Width, uint Height)
    {
        this.Width = Width;
        this.Height = Height;
        this.PixelCount = Width * Height;
    }

    public static VideoSize VGA => new VideoSize(Width: 640, Height: 480);
    public static VideoSize QVGA => new VideoSize(Width: 320, Height: 240);
};