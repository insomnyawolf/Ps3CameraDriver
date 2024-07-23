using LibUsbDotNet.Main;

namespace Ps3CameraDriver;

public partial class Ps3CamDriver
{
    // sccb_w_array();
    // output a sensor sequence (reg - val)
    public void SerialCameraControlBusWriteArray(IReadOnlyList<Command> commands)
    {
        for (int i = 0; i < commands.Count; i++)
        {
            var current = commands[i];

            var register = current.Register;
            var value = current.Value;

#warning reverse engineer those values
            if (register == 0xff)
            {
                var data = SerialCameraControlBusRegisterRead((RegisterOV534)value);
                value = 0x00;
            }

            SerialCameraControlBusRegisterWrite((RegisterOV534)register, value);
        }
    }

    // sccb_reg_write();
    public void SerialCameraControlBusRegisterWrite(RegisterOV534 register, byte value)
    {
        HardwareRegisterWrite(OperationsOV534.REG_SUBADDR, (byte)register);
        HardwareRegisterWrite(OperationsOV534.REG_WRITE, value);
        HardwareRegisterWrite(OperationsOV534.REG_OPERATION, (byte)OperationsOV534.OP_WRITE_3);

        SerialCameraControlBusCheckStatus();
    }

    // sccb_reg_read();
    public byte SerialCameraControlBusRegisterRead(RegisterOV534 registerAddress)
    {
        HardwareRegisterWrite(OperationsOV534.REG_SUBADDR, (byte)registerAddress);
        HardwareRegisterWrite(OperationsOV534.REG_OPERATION, (byte)OperationsOV534.OP_WRITE_2);

        SerialCameraControlBusCheckStatus();

        HardwareRegisterWrite(OperationsOV534.REG_OPERATION, (byte)OperationsOV534.OP_READ_2);

        SerialCameraControlBusCheckStatus();

        var data = HardwareRegisterRead(OperationsOV534.REG_READ);

        return data;
    }

    //bool sccb_check_status();
    public void SerialCameraControlBusCheckStatus()
    {
        var i = 0;
        while (i < StatusCheckMaxRetry)
        {
            var data = (CheckStatusResponses)HardwareRegisterRead(OperationsOV534.REG_STATUS);

            Console.WriteLine($"Received Status => {data}");

            if (data == CheckStatusResponses.Ok)
            {
                return;
            }
            else if (data == CheckStatusResponses.Error)
            {
                break;
            }
            else if (data == CheckStatusResponses.Retry)
            {
            }
            else
            {
                Console.WriteLine($"Unknown status => {data}");
            }

            i++;

            Task.Delay(100);
        }

        if (i == StatusCheckMaxRetry)
        {
            Console.WriteLine($"Status check faillure after {StatusCheckMaxRetry} retries");
        }

        throw new Exception();
    }

    // reg_w_array();
    // output a bridge sequence (reg - val)
    public void HardwareRegisterWriteArray(IReadOnlyList<Command> commands)
    {
        for (int i = 0; i < commands.Count; i++)
        {
            var current = commands[i];

            HardwareRegisterWrite((OperationsOV534)current.Register, current.Value);
        }
    }

    // ov534_reg_write();
    public void HardwareRegisterWrite(OperationsOV534 operation, byte value)
    {
        // debug("reg=0x%04x, val=0%02x", reg, val);

        // Can Be Optimized
        var buffer = CopyToBuffer(value, UsbBuffer);

        SendControlPacket(buffer, (short)operation);
    }

    // ov534_reg_read();
    public byte HardwareRegisterRead(OperationsOV534 operation)
    {
        var data = ReadControlPacket((short)operation);

        return data;
    }

    public void SendControlPacket(byte[] buffer, short registerAddress)
    {
        //var packet = new UsbSetupPacket()
        //{
        //    RequestType = OutgoingRequestType,
        //    Request = 1,
        //    Value = registerAddress,
        //    Index = buffer,
        //    Length = 1,
        //};

        var sendDataLength = TransferPacket(OutgoingRequestType, buferSendLength: 1, bufferLength: 1, buffer, registerAddress);
    }

    public byte ReadControlPacket(short registerAddress)
    {
        ClearBuffer(UsbBuffer);
        var readDataLength = TransferPacket(IncomingRequestType, buferSendLength: 0, bufferLength: 1, UsbBuffer, registerAddress);

#warning check if only the first byte is ever used
        // ??????
        return UsbBuffer[0];
    }

    public int TransferPacket(byte requestType, short buferSendLength, short bufferLength, byte[] buffer, short registerAddress)
    {
        var packet = new UsbSetupPacket()
        {
            RequestType = requestType,
            Request = 1,
            Value = 0,
            Index = registerAddress,
            Length = buferSendLength,
        };

        var hexStr = Convert.ToHexString(buffer);

        Console.WriteLine($"Sending Buffer => {hexStr}");

        var bytesTransfered = UsbDevice.ControlTransfer(packet, buffer, offset: 0, length: bufferLength);

        if (bytesTransfered < 0)
        {
            throw new Exception();
        }

        return bytesTransfered;
    }
}