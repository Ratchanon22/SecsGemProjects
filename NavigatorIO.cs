
using System;
using Automation.BDaq;

public class NavigatorIO : IIoController
{
    private InstantDoCtrl _doCtrl;
    private InstantDiCtrl _diCtrl;
    private const string DeviceDescription = "USB-4750,BID#0"; // ตรงกับที่เห็นใน Navigator

    public NavigatorIO(string devicePort)
    {
        try
        {
            _doCtrl = new InstantDoCtrl();
            _diCtrl = new InstantDiCtrl();

            _doCtrl.SelectedDevice = new DeviceInformation(devicePort);
            _diCtrl.SelectedDevice = new DeviceInformation(devicePort);

            Console.WriteLine("[NavigatorIO] Initialized with device: " + devicePort);
        }
        catch (DllNotFoundException dllEx)
        {
            Console.WriteLine("[NavigatorIO] DLL not found: " + dllEx.Message);
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[NavigatorIO] Initialization failed: " + ex.Message);
            throw;
        }
    }


    public void SetOutput(int channel, bool state)
    {
        try
        {
            byte port = (byte)(channel / 8);
            byte bit = (byte)(channel % 8);
            byte mask = (byte)(1 << bit);

            byte currentValue;
            _doCtrl.Read(port, out currentValue);

            byte newValue = state ? (byte)(currentValue | mask) : (byte)(currentValue & ~mask);

            _doCtrl.Write(port, newValue);
            Console.WriteLine($"[NavigatorIO] SetOutput: Channel {channel} => Port {port}, Bit {bit}, State {state}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[NavigatorIO] Error setting output: " + ex.Message);
        }
    }

    public bool GetInput(int channel)
    {
        try
        {
            byte port = (byte)(channel / 8);
            byte bit = (byte)(channel % 8);
            byte mask = (byte)(1 << bit);

            byte value;
            _diCtrl.Read(port, out value);

            bool result = (value & mask) != 0;
            Console.WriteLine($"[NavigatorIO] GetInput: Channel {channel} => Port {port}, Bit {bit}, Value {result}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[NavigatorIO] Error getting input: " + ex.Message);
            return false;
        }
    }
    public NavigatorIO() : this("USB-4750,BID#0") { }

}
