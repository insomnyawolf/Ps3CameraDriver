using LibUsbDotNet.LibUsb;
using Ps3CameraDriver;
using VirtualCameraCommon;

internal class Program
{

    //private static readonly DeviceManager DeviceManager = new DeviceManager(UsbContext);
    static async Task Main(string[] args)
    {
        var cameras = Ps3CamDriverLoader.GetAvailableCameras();

        foreach (var cam in cameras) 
        {
            Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) =>
            {
                e.Cancel = true;
                cam.Stop();
            };

            //cam.Init(FrameConfiguration.VGA60);
            cam.Init(FrameConfiguration.QVGA30);

            //while (true)
            //{
            //    ps3cam.ToggleLed();

            //    await Task.Delay(1000);
            //}

            cam.Start();

            while (true && cam.IsStreaming)
            {
                await Task.Delay(1000);

                //var fq = cam.FrameQueue;

                //var frame = fq.StartReadFrame();

                //fq.FinishReadFrame();
            }

           

            //ps3cam.ToggleLed();
        }
    }
}

public class Ps3CamDriverLoader
{
    private const ushort VendorId = 0x1415;
    private const ushort ProductId = 0x2000;

    private static readonly UsbContext UsbContext = new UsbContext();

    private static bool IsTargetDevice(IUsbDevice usbDevice)
    {
        var info = usbDevice.Info;

        if (info.VendorId != VendorId)
        {
            return false;
        }

        if (info.ProductId != ProductId)
        {
            return false;
        }

        return true;
    }

    public static List<Ps3CamDriver> GetAvailableCameras()
    {
        var deviceCollection = UsbContext.FindAll(IsTargetDevice);

        var drivers = new List<Ps3CamDriver>(deviceCollection.Count);

        foreach (var device in deviceCollection)
        {
            var ps3cam = new Ps3CamDriver(device);

            drivers.Add(ps3cam);
        }

        return drivers;
    }
}