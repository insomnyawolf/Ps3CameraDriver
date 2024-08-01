using Ps3CameraDriver;
using System.Drawing;
using System;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using VirtualCameraCommon;

namespace DiverTestHelper;

public partial class Form1 : Form
{
    private readonly Ps3CamDriver Camera;
    //public readonly FrameConfiguration FrameConfiguration = FrameConfiguration.VGA60;
    //public readonly FrameConfiguration FrameConfiguration = FrameConfiguration.VGA30;
    //public readonly FrameConfiguration FrameConfiguration = FrameConfiguration.VGA30BGR;
    public readonly FrameConfiguration FrameConfiguration = FrameConfiguration.QVGA30BGR;
    //public readonly FrameConfiguration FrameConfiguration = FrameConfiguration.QVGA30;
    public uint Width => FrameConfiguration.VideoSize.Width;
    public uint Height => FrameConfiguration.VideoSize.Height;
    public ColorFormat ColorFormat => FrameConfiguration.ColorFormat;

    private Bitmap Image;
    private Bitmap Image2;
    private Rectangle Rectangle;

    public Form1()
    {
        InitializeComponent();
        Camera = Ps3CamDriverLoader.GetAvailableCameras()[0];
        Camera.Init(FrameConfiguration);
        Camera.Start();

        var imageSize = new Size((int)Width, (int)Height);

        var origin = new Point(0, 0);

        Rectangle = new Rectangle(origin, imageSize);

        PixelFormat pixelFormat = PixelFormat.Format24bppRgb;

        Image = new Bitmap(imageSize.Width, imageSize.Height, format: pixelFormat);
        Image2 = new Bitmap(imageSize.Width, imageSize.Height, format: pixelFormat);

        DisableAntialiasing(Image);
        DisableAntialiasing(Image2);

        var g = pictureBox1.CreateGraphics();
        DisableAntialiasing(g);

        _ = Task.Run(RenderFrames);
    }

    public void DisableAntialiasing(Bitmap bitmap)
    {
        var g = Graphics.FromImage(bitmap);

        DisableAntialiasing(g);
    }

    public void DisableAntialiasing(Graphics g)
    {
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
    }

    public void RenderFrames()
    {
        while (true)
        {
            RenderFrame();
            Thread.Sleep(25);
        }
    }

    public unsafe void RenderFrame()
    {
        var fq = Camera.FrameQueue;

        var frame = fq.StartReadFrame();

        var bl = frame.Length;
        //_renderTarget.Clear(new _D3DCOLORVALUE(0.5f, 0, 1, 1));

        var driverIndex = 0;
        var bitmapIndex = 0;

        fixed (byte* driverBuffer = frame)
        {
            while (driverIndex < bl)
            {
                Color color;


                BitmapData? bitmapData = Image.LockBits(Rectangle, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                byte* bitmapBuffer = (byte*)bitmapData.Scan0.ToPointer();

                if (ColorFormat == ColorFormat.Bayer)
                {
                    var grey = driverBuffer[driverIndex++];
                    bitmapBuffer[bitmapIndex++] = grey;
                    bitmapBuffer[bitmapIndex++] = grey;
                    bitmapBuffer[bitmapIndex++] = grey;
                }
                else if (ColorFormat == ColorFormat.BGR)
                {
                    var b = driverBuffer[driverIndex++];
                    var g = driverBuffer[driverIndex++];
                    var r = driverBuffer[driverIndex++];
                    bitmapBuffer[bitmapIndex++] = b;
                    bitmapBuffer[bitmapIndex++] = g;
                    bitmapBuffer[bitmapIndex++] = r;
                }
                else
                {
                    throw new NotImplementedException(nameof(ColorFormat));
                }

                Image.UnlockBits(bitmapData);
            }
        }

        pictureBox1.Invoke(new MethodInvoker(delegate { pictureBox1.Image = Image; }));

        var temp = Image;
        Image = Image2;
        Image2 = temp;

        fq.FinishReadFrame();
    }
}
