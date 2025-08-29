
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimulatorApp
{
    class Simulator
    {
        static async Task Main()
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var config = new ConfigurationBuilder()
                .AddJsonFile(configPath, optional: false, reloadOnChange: true)
                .Build();

            var simulatorConnection = new SimulatorConnectionOptions();
            config.GetSection("SimulatorConnection").Bind(simulatorConnection);

            IPAddress ipAddress;
            if (!string.IsNullOrWhiteSpace(simulatorConnection.IpAddress) &&
                IPAddress.TryParse(simulatorConnection.IpAddress, out var parsedIp))
            {
                try
                {
                    var testSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    testSocket.Bind(new IPEndPoint(parsedIp, 0));
                    testSocket.Close();
                    ipAddress = parsedIp;
                }
                catch
                {
                    Console.WriteLine($"[Simulator] Cannot bind to IP {parsedIp}. Using IPAddress.Any instead.");
                    ipAddress = IPAddress.Any;
                }
            }
            else
            {
                Console.WriteLine("[Simulator] Invalid or missing IP in config. Using IPAddress.Any");
                ipAddress = IPAddress.Any;
            }

            var port = simulatorConnection.Port;
            if (port <= 0 || port > 65535)
            {
                Console.WriteLine("[Simulator] Invalid port number in config. Using default port 5000.");
                port = 5000;
            }

            var listener = new TcpListener(ipAddress, port);
            listener.Start();
            Console.WriteLine($"[Simulator] Listening on {ipAddress} at port {port}");

            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("[Simulator] Shutting down...");
                listener.Stop();
                Environment.Exit(0);
            };

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                var remoteEndPoint = client.Client.RemoteEndPoint?.ToString();
                Console.WriteLine($"[Simulator] Connected from {remoteEndPoint}");

                _ = Task.Run(async () =>
                {
                    using var stream = client.GetStream();
                    var buffer = new byte[1024];
                    try
                    {
                        while (true)
                        {
                            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                            if (bytesRead == 0)
                            {
                                Console.WriteLine($"[Simulator] Client disconnected: {remoteEndPoint}");
                                break;
                            }

                            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                            Console.WriteLine($"[Simulator] Received: {message}");

                            byte[] response = Encoding.UTF8.GetBytes("ACK");
                            await stream.WriteAsync(response, 0, response.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Simulator] Error with client {remoteEndPoint}: {ex.Message}");
                    }
                    finally
                    {
                        client.Close();
                        Console.WriteLine($"[Simulator] Disconnected from {remoteEndPoint}");
                    }
                });
            }
        }
    }

    public class SimulatorConnectionOptions
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
    }
}
