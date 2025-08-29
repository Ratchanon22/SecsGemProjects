using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

enum DisconnectReason
{
    DeviceClosed,
    PortBlocked,
    EthernetUnplugged,
    Timeout,
    Unknown
}

class Host
{
    static CancellationTokenSource cts = new CancellationTokenSource();
    static bool isConnected = false;
    static IIoController ioController;

    static void Main()
    {
        MainSync();
    }

    static void MainSync()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var config = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: true)
            .Build();

        ioController = IoControllerFactory.Create();

        var connectionOptions = new DeviceConnection();
        config.GetSection("DeviceConnection").Bind(connectionOptions);
        string ip = connectionOptions.IpAddress;
        int port = connectionOptions.Port;

        Console.WriteLine($"[Host] Using IO: {ioController.GetType().Name}");
        Console.WriteLine($"[Host] Target IP: {ip}, Port: {port}");

        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("[Host] Shutting down gracefully...");
            e.Cancel = true;
            cts.Cancel();
        };

        if (port <= 0 || port > 65535)
        {
            Console.WriteLine("[Host] Invalid port number in config. Using default port 5000.");
            port = 5000;
        }

        while (!cts.IsCancellationRequested)
        {
            try
            {
                using var client = new TcpClient();
                Console.WriteLine($"[Host] Attempting to connect to {ip}:{port}...");

                using var connectCts = new CancellationTokenSource(5000);
                client.ConnectAsync(ip, port, connectCts.Token).GetAwaiter().GetResult();

                Console.WriteLine("[Host] Connected to device");
                isConnected = true;
                ioController.SetOutput(0, false);

                using var stream = client.GetStream();

                while (client.Connected && !cts.IsCancellationRequested)
                {
                    try
                    {
                        using var operationCts = new CancellationTokenSource(30000);

                        byte[] message = Encoding.UTF8.GetBytes("Hello Device");
                        stream.WriteAsync(message, 0, message.Length, operationCts.Token).GetAwaiter().GetResult();
                        Console.WriteLine("[Host] Sent: Hello Device");

                        byte[] buffer = new byte[4096];
                        int bytesRead = stream.ReadAsync(buffer, 0, buffer.Length, operationCts.Token).GetAwaiter().GetResult();

                        if (bytesRead == 0)
                        {
                            Console.WriteLine("[Host] Device closed the connection.");
                            break;
                        }

                        string rawBytes = BitConverter.ToString(buffer, 0, bytesRead);
                        string receivedText = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                        Console.WriteLine($"[Host] Received Raw Bytes: {rawBytes}");
                        Console.WriteLine($"[Host] Received UTF-8 String: {receivedText}");

                        Task.Delay(10000, cts.Token).GetAwaiter().GetResult();
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("[Host] An operation timed out. Continuing...");
                    }
                    catch (IOException ioEx) when (ioEx.InnerException is SocketException sockEx &&
                                                   (sockEx.SocketErrorCode == SocketError.ConnectionAborted ||
                                                    sockEx.SocketErrorCode == SocketError.ConnectionReset))
                    {
                        Console.WriteLine("[Host] Connection was reset by the device during an active operation. Attempting to reconnect.");
                        break;
                    }
                    catch (Exception innerEx)
                    {
                        Console.WriteLine($"[Host] Inner loop error: {innerEx.Message}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                var reason = DetectDisconnectReason(ex);

                if (isConnected)
                {
                    Console.WriteLine($"[Host] Connection lost due to: {reason}");
                    LogDisconnect(reason);
                    ioController.SetOutput(0, true);
                    isConnected = false;
                }
                else
                {
                    Console.WriteLine($"[Host] Retry failed: {ex.Message}");
                    Console.WriteLine($"[Host] Connection lost due to: {reason}");
                    LogDisconnect(reason);
                }

                Task.Delay(10000, cts.Token).GetAwaiter().GetResult();
            }
        }
    }

    static DisconnectReason DetectDisconnectReason(Exception ex)
    {
        if (ex is TimeoutException || ex is OperationCanceledException)
            return DisconnectReason.Timeout;

        if (ex is SocketException sockEx)
        {
            switch (sockEx.SocketErrorCode)
            {
                case SocketError.ConnectionRefused:
                    return DisconnectReason.PortBlocked;
                case SocketError.ConnectionReset:
                case SocketError.ConnectionAborted:
                    return DisconnectReason.DeviceClosed;
                case SocketError.NetworkDown:
                case SocketError.NetworkUnreachable:
                    return DisconnectReason.EthernetUnplugged;
                default:
                    return DisconnectReason.Unknown;
            }
        }
        if (ex.InnerException is SocketException innerSockEx)
        {
            switch (innerSockEx.SocketErrorCode)
            {
                case SocketError.ConnectionReset:
                case SocketError.ConnectionAborted:
                    return DisconnectReason.DeviceClosed;
                case SocketError.ConnectionRefused:
                    return DisconnectReason.PortBlocked;
                case SocketError.NetworkDown:
                case SocketError.NetworkUnreachable:
                    return DisconnectReason.EthernetUnplugged;
                default:
                    return DisconnectReason.Unknown;
            }
        }
        if (ex.Message.Contains("Disconnected") ||
            ex.Message.Contains("socket closed") ||
            ex.Message.Contains("connection was closed"))
            return DisconnectReason.DeviceClosed;

        if (!NetworkInterface.GetIsNetworkAvailable())
            return DisconnectReason.EthernetUnplugged;

        return DisconnectReason.Unknown;
    }

    static void LogDisconnect(DisconnectReason reason)
    {
        string log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Disconnect Reason: {reason}";
        Console.WriteLine($"[LOG] {log}");
        File.AppendAllText("disconnect_log.txt", log + Environment.NewLine);
    }
}

public class DeviceConnection
{
    public string IpAddress { get; set; }
    public int Port { get; set; }
}
