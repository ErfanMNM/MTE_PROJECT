using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace DMS_Project.Communications.WebSockets
{
    public class C1WebSocketHandler
    {
        private readonly WebSocket _webSocket;

        public C1WebSocketHandler(WebSocket webSocket)
        {
            _webSocket = webSocket;
        }

        public async Task HandleAsync()
        {
            var buffer = new byte[4096];
            Console.WriteLine($"[WS-C1] Client connected");

            try
            {
                while (_webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result;
                    try
                    {
                        result = await _webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            CancellationToken.None);
                    }
                    catch (WebSocketException)
                    {
                        Console.WriteLine($"[WS-C1] Receive failed - connection closed");
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine($"[WS-C1] Client requested close");
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "",
                            CancellationToken.None);
                        break;
                    }

                    string receivedText = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"[WS-C1] Received: {receivedText}");
                    
                    string response = ProcessClientMessage(receivedText);

                    if (!string.IsNullOrEmpty(response))
                    {
                        await SendTextAsync(response);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WS-C1] Error: {ex.GetType().Name} - {ex.Message}");
            }
            
            Console.WriteLine($"[WS-C1] Connection closed. State: {_webSocket.State}");
        }

        /// <summary>
        /// HÀM NHẬN JSON TỪ CLIENT - XỬ LÝ Ở ĐÂY
        /// </summary>
        public static string ProcessClientMessage(string jsonMessage)
        {
            try
            {
                Console.WriteLine($"[WS-C1] Received: {jsonMessage}");

                // Parse JSON
                using var doc = JsonDocument.Parse(jsonMessage);
                var root = doc.RootElement;

                // TODO: Xử lý JSON theo logic của bạn
                // Ví dụ:
                // string action = root.GetProperty("action").GetString() ?? "";
                // switch (action) { ... }

                // Trả về JSON response
                return JsonSerializer.Serialize(new
                {
                    status = "ok",
                    received = jsonMessage
                });
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[WS-C1] JSON Parse Error: {ex.Message}");
                return JsonSerializer.Serialize(new
                {
                    status = "error",
                    message = "Invalid JSON"
                });
            }
        }

        /// <summary>
        /// HÀM GỬI DATA VỀ CLIENT
        /// </summary>
        public async Task SendTextAsync(string text)
        {
            if (_webSocket.State != WebSocketState.Open) return;

            var bytes = Encoding.UTF8.GetBytes(text);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        /// <summary>
        /// Gửi JSON response về client
        /// </summary>
        public async Task SendJsonAsync(object data)
        {
            string json = JsonSerializer.Serialize(data);
            await SendTextAsync(json);
        }

        /// <summary>
        /// Gửi data về client (static version)
        /// </summary>
        public static async Task SendTextToClientAsync(WebSocket webSocket, string text)
        {
            if (webSocket.State != WebSocketState.Open) return;

            var bytes = Encoding.UTF8.GetBytes(text);
            await webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
    }
}
