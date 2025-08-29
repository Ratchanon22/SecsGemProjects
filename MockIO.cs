using System;

public class MockIO : IIoController
{
    public void SetOutput(int channel, bool state)
    {
        Console.WriteLine($"[MockIO] SetOutput: Channel {channel}, State {state}");
    }

    public bool GetInput(int channel)
    {
        Console.WriteLine($"[MockIO] GetInput: Channel {channel}");
        return false;
    }
}
