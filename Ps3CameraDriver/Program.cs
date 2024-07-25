using LibUsbDotNet.LibUsb;
using Ps3CameraDriver;

internal class Program
{
    private static readonly UsbContext UsbContext = new UsbContext();
    //private static readonly DeviceManager DeviceManager = new DeviceManager(UsbContext);
    static async Task Main(string[] args)
    {
        var device = UsbContext.Find(Ps3CamDriver.IsTargetDevice);

        var ps3cam = new Ps3CamDriver(device);

        var frameConfig = new FrameConfiguration()
        {
            Resolution = VideoResolution.QVGA,
            FramesPerSecond = 1,
        };

        ps3cam.Init(frameConfig);

        //while (true)
        //{
        //    ps3cam.ToggleLed();

        //    await Task.Delay(1000);
        //}

        ps3cam.Start();

        while (true)
        {
            await Task.Delay(1000);
        }

        ps3cam.Stop();

        ps3cam.ToggleLed();
    }
}
