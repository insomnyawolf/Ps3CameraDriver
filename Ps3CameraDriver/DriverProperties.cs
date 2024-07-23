namespace Ps3CameraDriver;

public partial class Ps3CamDriver
{

    private bool LedStatus;

    // Just because it helps testing, it works \:D/
    public void ToggleLed()
    {
        LedStatus = !LedStatus;
        SetLed(LedStatus);
    }

    // ov534_set_led();
    private const byte LedMask = 0x80;
    private static readonly byte LedMaskInverted = AsByte(~LedMask);
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

    private const byte AutoGainMask1 = 0x04;
    private const byte AutoGainMask2 = 0x03;
    private static readonly byte AutoGainMaskInverted1 = AsByte(~AutoGainMask1);
    private static readonly byte AutoGainMaskInverted2 = AsByte(~AutoGainMask2);
    public void SetAutoGain(bool status)
    {
        byte val1 = SerialCameraControlBusRegisterRead(RegisterOV534.Settings);
        byte val2 = SerialCameraControlBusRegisterRead(RegisterOV534.AutoGain);
        if (status)
        {
            val1 |= AutoGainMask1;
            val2 |= AutoGainMask2;
        }
        else
        {
            val1 &= AutoGainMaskInverted1;
            val2 &= AutoGainMaskInverted2;
        }

        // AGC enable
        SerialCameraControlBusRegisterWrite(RegisterOV534.Settings, val1);
        // Gamma function ON/OFF selection
        SerialCameraControlBusRegisterWrite(RegisterOV534.AutoGain, val2);
    }

    private const byte AutoWhiteBalanceMask1 = 0x02;
    private const byte AutoWhiteBalanceMask2 = 0x40;
    private static readonly byte AutoWhiteBalanceMaskInverted1 = AsByte(~AutoWhiteBalanceMask1);
    private static readonly byte AutoWhiteBalanceMaskInverted2 = AsByte(~AutoWhiteBalanceMask2);
    public void SetAutoWhiteBalance(bool status)
    {
        byte val1 = SerialCameraControlBusRegisterRead(RegisterOV534.Settings);
        byte val2 = SerialCameraControlBusRegisterRead(RegisterOV534.AutoWhiteBalance);
        if (status)
        {
            val1 |= AutoWhiteBalanceMask1;
            val2 |= AutoWhiteBalanceMask2;
        }
        else
        {
            val1 &= AutoWhiteBalanceMaskInverted1;
            val2 &= AutoWhiteBalanceMaskInverted2;
        }

        // AWB enable
        SerialCameraControlBusRegisterWrite(RegisterOV534.Settings, val1);
        // AWB calculate enable
        SerialCameraControlBusRegisterWrite(RegisterOV534.AutoWhiteBalance, val2);
    }

    private const byte AutomaticExposureControlMask1 = 0x01;
    private static readonly byte AutomaticExposureControlMaskInverted1 = AsByte(~AutoWhiteBalanceMask1);
    public void SetAutomaticExposureControl(bool status)
    {
        byte val1 = SerialCameraControlBusRegisterRead(RegisterOV534.Settings);
        if (status)
        {
            val1 |= AutomaticExposureControlMask1;
        }
        else
        {
            val1 &= AutomaticExposureControlMaskInverted1;
        }

        SerialCameraControlBusRegisterWrite(RegisterOV534.Settings, val1);
    }

    public void SetFramerate(int val)
    {
        if (IsStreaming)
        {
            // Bufer resizing is scary D:
            throw new Exception("Can not change framerate while streaming");
        }
       
        FrameConfigurationCache.FramesPerSecond = val;
        NormalizedFrameConfigurationCache = InternalFrameConfigurationCache.GetNormalizedFrameConfig(FrameConfigurationCache.FramesPerSecond);
    }

    private const byte TestPatternMask1 = 0b00000001;
    private static readonly byte TestPatternMaskInverted1 = AsByte(~TestPatternMask1);
    public void SetTestPattern(bool status)
    {
        byte val1 = SerialCameraControlBusRegisterRead(RegisterOV534.UnknownFrameBufferRelated);

        val1 &= TestPatternMaskInverted1;

        if (status) 
        {
            // 0x80;
            val1 |= TestPatternMask1;
        }

        SerialCameraControlBusRegisterWrite(RegisterOV534.UnknownFrameBufferRelated, val1);
    }

    public void SetExposue(int val)
    {
        var val1 = AsByte(val >> 7);
        var val2 = AsByte(val << 1);

        SerialCameraControlBusRegisterWrite(RegisterOV534.Exposure1, val1);
        SerialCameraControlBusRegisterWrite(RegisterOV534.Exposure2, val2);
    }

    public void SetSharpness(int val)
    {
        var val1 = AsByte(val);

        SerialCameraControlBusRegisterWrite(RegisterOV534.Sharpness1, val1);
        SerialCameraControlBusRegisterWrite(RegisterOV534.Sharpness2, val1);
    }

    public void SetContrast(int val)
    {
        var val1 = AsByte(val);

        SerialCameraControlBusRegisterWrite(RegisterOV534.Contrast, val1);
    }

    public void SetBrightness(int val)
    {
        var val1 = AsByte(val);

        SerialCameraControlBusRegisterWrite(RegisterOV534.Brightness, val1);
    }

    // void camera::set_hue(int val)
    public void SetHue(int val)
    {
        // Sorry i'm lazy atm
        throw new NotImplementedException();
    }

    public void SetBalanceBlue(int val)
    {
        var val1 = AsByte(val);

        SerialCameraControlBusRegisterWrite(RegisterOV534.BlueChannelGain, val1);
        //SerialCameraControlBusRegisterWrite(RegisterOV534.BlcBlueChannelTarget, val1);
    }

    public void SetBalanceRed(int val)
    {
        var val1 = AsByte(val);

        SerialCameraControlBusRegisterWrite(RegisterOV534.RedChannelGain, val1);
        //SerialCameraControlBusRegisterWrite(RegisterOV534.BlcRedChannelTarget, val1);
    }

    public void SetBalanceGreen(int val)
    {
        var val1 = AsByte(val);

        SerialCameraControlBusRegisterWrite(RegisterOV534.GreenChannelGain, val1);
        //SerialCameraControlBusRegisterWrite(RegisterOV534.BlcGrenChannelTarget, val1);
    }
}