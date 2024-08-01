using Ps3CameraDriver;
using System.Drawing.Drawing2D;
using System.Xml.Linq;
using VirtualCameraCommon;

namespace DiverTestHelper;

public partial class Form1 : Form
{
    private readonly Ps3CamDriver Camera;
    //public readonly FrameConfiguration FrameConfiguration = FrameConfiguration.VGA60;
    public readonly FrameConfiguration FrameConfiguration = FrameConfiguration.QVGA30RGB;
    //public readonly FrameConfiguration FrameConfiguration = FrameConfiguration.QVGA30;
    public uint Width => FrameConfiguration.VideoSize.Width;
    public uint Height => FrameConfiguration.VideoSize.Height;
    public ColorFormat ColorFormat => FrameConfiguration.ColorFormat;

    private Bitmap Image;
    private Bitmap Image2;

    public Form1()
    {
        InitializeComponent();
        Camera = Ps3CamDriverLoader.GetAvailableCameras()[0];
        Camera.Init(FrameConfiguration);
        Camera.Start();

        Image = new Bitmap((int)Width, (int)Height);
        Image2 = new Bitmap((int)Width, (int)Height);

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

        var index = 0;
        var x = 0;
        var y = 0;

        fixed (byte* buffer = frame)
        {
            while (index < bl)
            {
                Color color;

                if (ColorFormat == ColorFormat.Bayer)
                {
                    var grey = buffer[index++];
                    color = Color.FromArgb(grey, grey, grey);
                }
                else if (ColorFormat == ColorFormat.RGB)
                {
                    var r = buffer[index++];
                    var g = buffer[index++];
                    var b = buffer[index++];
                    color = Color.FromArgb(r, g, b);
                }
                else
                {
                    throw new NotImplementedException(nameof(ColorFormat));
                }

                Image.SetPixel(x, y, color);

                x++;

                if (x == Width)
                {
                    x = 0;
                    y++;
                }
            }
        }

        pictureBox1.Invoke(new MethodInvoker(delegate { pictureBox1.Image = Image; }));

        var temp = Image;
        Image = Image2;
        Image2 = temp;

        fq.FinishReadFrame();
    }
}
