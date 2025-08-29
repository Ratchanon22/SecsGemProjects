using Microsoft.Extensions.Configuration;
using System;
using System.IO;

public static class IoControllerFactory
{
    public static IIoController Create()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var config = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: true)
            .Build();

        bool useMock = config.GetValue<bool>("IOSettings:UseMockIO");
        string devicePort = config.GetValue<string>("IOSettings:DevicePort");

        if (useMock)
        {
            Console.WriteLine("[Factory] Using MockIoController");
            return new MockIoController();
        }
        else
        {
            Console.WriteLine($"[Factory] Using NavigatorIO with port: {devicePort}");
            return new NavigatorIO(devicePort); // ถ้า NavigatorIO รับพอร์ต
        }
    }
}
