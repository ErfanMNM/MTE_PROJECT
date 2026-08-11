
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;


namespace DMS_Project.Communications.TCP
{
    public enum enumClient
    {
        CONNECTED,
        DISCONNECTED,
        RECEIVED,
        RECONNECT
    }

    public class TCPClient
    {
        public delegate void EventForClient(enumClient state, string data);
        public event EventForClient? ClientCallBack;

        public required string IP { get; set; }
        public int Port { get; set; }
        public bool Connected { get; private set; } = false;

        private Socket? client;
        private CancellationTokenSource? cts;
        private Task? reconnectTask;

        public void Connect()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }
            cts = new CancellationTokenSource();

            reconnectTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (!Connected)
                    {
                        try
                        {
                            ClientCallBack?.Invoke(enumClient.RECONNECT, "Reconnecting...");

                            if (!PingHost(IP))
                            {
                                await Task.Delay(2000, cts.Token);
                                continue;
                            }

                            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            client.Connect(IPAddress.Parse(IP), Port);
                            Connected = true;
                            ClientCallBack?.Invoke(enumClient.CONNECTED, "Connected");

                            StartReceiveLoop();
                        }
                        catch (Exception ex)
                        {
                            Connected = false;
                            ClientCallBack?.Invoke(enumClient.DISCONNECTED, $"Connect failed: {ex.Message}");
                            await Task.Delay(3000, cts.Token);
                        }
                    }
                    await Task.Delay(1000, cts.Token);
                }
            }, cts.Token);
        }

        private bool PingHost(string ip)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send(ip, 1000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private void StartReceiveLoop()
        {
            Task.Run(async () =>
            {
                var buffer = new byte[512];

                while (Connected && client != null && client.Connected)
                {
                    try
                    {
                        int received = await client.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                        if (received > 0)
                        {
                            var data = Encoding.UTF8.GetString(buffer, 0, received);
                            ClientCallBack?.Invoke(enumClient.RECEIVED, data);
                        }
                        else
                        {
                            HandleDisconnect("Server closed connection");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        HandleDisconnect($"Receive error: {ex.Message}");
                        break;
                    }
                }
            });
        }

        public void Send(string data)
        {
            try
            {
                if (client != null && client.Connected)
                {
                    var bytes = Encoding.UTF8.GetBytes(data);
                    client.Send(bytes);
                }
                else
                {
                    HandleDisconnect("Send failed: Not connected");
                }
            }
            catch (Exception ex)
            {
                HandleDisconnect($"Send error: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            cts?.Cancel();

            if (client != null)
            {
                try
                {
                    if (client.Connected)
                        client.Shutdown(SocketShutdown.Both);
                    client.Close();
                }
                catch { }
                finally
                {
                    client = null;
                }
            }
            Connected = false;
            ClientCallBack?.Invoke(enumClient.DISCONNECTED, "Disconnected");
        }

        private void HandleDisconnect(string message)
        {
            Connected = false;
            try
            {
                client?.Close();
            }
            catch { }
            ClientCallBack?.Invoke(enumClient.DISCONNECTED, message);
        }

        public void Dispose()
        {
            Disconnect();
            cts?.Dispose();
        }
    }
}
