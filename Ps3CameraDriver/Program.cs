using LibUsbDotNet.LibUsb;
using Ps3CameraDriver;

internal class Program
{
    private static readonly UsbContext UsbContext = new UsbContext();
    //private static readonly DeviceManager DeviceManager = new DeviceManager(UsbContext);
    static Task Main(string[] args)
    {
        var device = UsbContext.Find(Ps3CamDriver.IsTargetDevice);

        var ps3cam = new Ps3CamDriver(device);

        ps3cam.Init(VideoResolution.VGA, 30, VideoFormat.RGB);

        ps3cam.ToggleLed();

        return Task.CompletedTask;
    }
}
