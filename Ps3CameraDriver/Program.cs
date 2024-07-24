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

        ps3cam.Init(FrameConfiguration.Default);

        ps3cam.ToggleLed();

        await ps3cam.StartTransfer();

        ps3cam.ToggleLed();
    }
}
