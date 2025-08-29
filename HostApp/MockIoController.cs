using System;
using System.Collections.Generic;

public class MockIoController : IIoController
{
    private readonly Dictionary<int, bool> inputStates = new();

    public void SetOutput(int channel, bool state)
    {
        Console.WriteLine($"[MockIO] SetOutput({channel}, {state})");
        // Optional: simulate effect on input
        inputStates[channel] = state;
    }

    public bool GetInput(int channel)
    {
        Console.WriteLine($"[MockIO] GetInput({channel})");
        return inputStates.ContainsKey(channel) ? inputStates[channel] : false;
    }
}

