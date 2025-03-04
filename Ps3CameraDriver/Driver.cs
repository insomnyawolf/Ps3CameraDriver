﻿using LibUsbDotNet.LibUsb;
using Ps3CameraDriver.Models;
using Ps3CameraDriver.Protocol;
using VirtualCameraCommon;

namespace Ps3CameraDriver;

public partial class Ps3CamDriver
{
    // SCCB https://www.waveshare.com/w/upload/1/14/OmniVision_Technologies_Seril_Camera_Control_Bus%28SCCB%29_Specification.pdf
    // ov534 Ps3Cam Hardware https://jim.sh/svn/jim/devl/playstation/ps3/eye/test/

    private readonly IUsbDevice UsbDevice;

    private CameraConfigurations CameraConfiguration = null!;
    private FrameConfiguration FrameConfiguration;
    private SensorConfiguration SensorConfiguration;

    private bool IsInitialized;
    public bool IsStreaming;

    public Ps3CamDriver(IUsbDevice IUsbDevice)
    {
        UsbDevice = IUsbDevice;
    }

    public void Init(FrameConfiguration frameConfiguration)
    {
        if (!UsbDevice.TryOpen())
        {
            throw new Exception("Can not open the device");
        }

        var res = UsbDevice.ClaimInterface(0);

        UpdateCameraConfiguration(frameConfiguration);

        BridgeReset();

        Thread.Sleep(10);

        SensorInitializeAddress();

        SensorReset();

        Thread.Sleep(10);

        // SensorProbe();

        Initialize();
    }

    public void UpdateCameraConfiguration(FrameConfiguration frameConfiguration)
    {
        var configsForTheSelectedResolution = CameraConfigurations[frameConfiguration.VideoSize];

        var validConfig = configsForTheSelectedResolution.GetSensorConfiguration(frameConfiguration.FramesPerSecond);

        var normalized = new FrameConfiguration(frameConfiguration.VideoSize, validConfig.FramesPerSecond, frameConfiguration.ColorFormat);

        if (normalized == FrameConfiguration)
        {
            return;
        }

        CameraConfiguration = configsForTheSelectedResolution;
        FrameConfiguration = normalized;
        SensorConfiguration = validConfig;
    }

    // ov534_set_frame_rate
    // validate frame rate and (if not dry run) set it
    public void ApplyNewFrameConfig()
    {
        SensorConfiguration.WriteTo(this);
    }

    public void Initialize()
    {
        HardwareRegisterWriteArray(Ov534_RegisterInitData);

        SetLed(true);

        SerialCameraControlBusWriteArray(Ov772x_RegisterInitData);

        HardwareRegisterWrite(OperationsOV534.Bridge2, 0x09);

        SetLed(false);

        IsInitialized = true;
    }

    public void Start()
    {
        if (!IsInitialized)
        {
            return;
        }

        if (IsStreaming)
        {
            return;
        }

        InternalStop();

        HardwareRegisterWriteArray(CameraConfiguration.BridgeStart);

        SerialCameraControlBusWriteArray(CameraConfiguration.SensorStart);

        ApplyNewFrameConfig();

#warning image config here

        SetLed(true);

        // Start stream
        HardwareRegisterWrite(OperationsOV534.Bridge2, 0x00);

        StartTransfer();

        IsStreaming = true;
    }

    public void Stop()
    {
        if (!IsStreaming)
        {
            return;
        }

        InternalStop();

        //UsbDevice.ReleaseInterface(0);

        //UsbDevice.Close();

        IsStreaming = false;
    }

    private void InternalStop()
    {
        // Stop stream
        HardwareRegisterWrite(OperationsOV534.Bridge2, 0x09);

        SetLed(false);
    }

    private bool LedStatus;

    // Just because it helps testing, it works \:D/
    public void ToggleLed()
    {
        LedStatus = !LedStatus;
        SetLed(LedStatus);
    }

    // ov534_set_led();
    private const byte LedMask = 0x80;
    private static readonly byte LedMaskInverted = Helpers.AsByte(~LedMask);
    public void SetLed(bool status)
    {
        var data1 = HardwareRegisterRead(OperationsOV534.Led1);

        data1 |= LedMask;

        HardwareRegisterWrite(OperationsOV534.Led1, data1);


        var data2 = HardwareRegisterRead(OperationsOV534.Led2);

        if (status)
        {
            data2 |= LedMask;

            HardwareRegisterWrite(OperationsOV534.Led2, data2);

            return;
        }

        // !on

        data2 &= LedMaskInverted;

        HardwareRegisterWrite(OperationsOV534.Led2, data2);

        var data3 = HardwareRegisterRead(OperationsOV534.Led1);

        data3 &= LedMaskInverted;

        HardwareRegisterWrite(OperationsOV534.Led1, data3);
    }

    public void SensorProbe()
    {
        var data1 = SerialCameraControlBusRegisterRead((RegisterOV534)0x0a);

        var data2 = SerialCameraControlBusRegisterRead((RegisterOV534)0x0a);

        var sensorId = data2 << 8;

        var data3 = SerialCameraControlBusRegisterRead((RegisterOV534)0x0b);

        var data4 = SerialCameraControlBusRegisterRead((RegisterOV534)0x0b);

        var sensorId2 = sensorId | data4;

        Console.WriteLine($"Sensor Id => {sensorId2}");
    }

    public void BridgeReset()
    {
        HardwareRegisterWrite(OperationsOV534.Bridge1, 0x3a);
        HardwareRegisterWrite(OperationsOV534.Bridge2, 0x08);
    }

    public void SensorInitializeAddress()
    {
        HardwareRegisterWrite(OperationsOV534.REG_ADDRESS, 0x42);
    }

    public void SensorReset()
    {
        SerialCameraControlBusRegisterWrite((RegisterOV534)0x12, 0x80);
    }
}