#region USAGE_EXAMPLE
/*
 * ==================== MQTT CLIENT USAGE EXAMPLE ====================
 * 
 * // 1. Create client with configuration
 * var client = new MqttClient(new MqttClientConfig
 * {
 *     BrokerHost = "broker.example.com",
 *     BrokerPort = 1883,
 *     ClientId = "MyDevice001",
 *     Username = "user",
 *     Password = "pass",
 *     AutoReconnect = true,
 *     ReconnectDelaySeconds = 5
 * });
 * 
 * // 2. Setup event handlers (TCPClient-style callbacks)
 * client.ClientCallBack += (status, message) =>
 * {
 *     switch (status)
 *     {
 *         case enumMqttStatus.CONNECTED:
 *             Console.WriteLine($"Connected: {message}");
 *             break;
 *         case enumMqttStatus.DISCONNECTED:
 *             Console.WriteLine($"Disconnected: {message}");
 *             break;
 *         case enumMqttStatus.RECONNECT:
 *             Console.WriteLine($"Reconnecting: {message}");
 *             break;
 *     }
 * };
 * 
 * client.PubSubStatus += (status, topic) =>
 * {
 *     Console.WriteLine($"PubSub {status} on {topic}");
 * };
 * 
 * client.OnMessageReceived += (s, msg) =>
 * {
 *     Console.WriteLine($"Received on {msg.Topic}: {msg.Payload}");
 * };
 * 
 * // 3. Connect
 * await client.ConnectAsync();
 * 
 * // 4. Subscribe to topics
 * await client.SubscribeAsync("sensors/#", MqttQualityOfServiceLevel.AtLeastOnce);
 * 
 * // 5. Publish messages
 * await client.PublishAsync("sensors/temperature", "25.5", MqttQualityOfServiceLevel.AtLeastOnce);
 * 
 * // 6. Disconnect when done
 * await client.DisconnectAsync();
 * 
 * ==================== END USAGE EXAMPLE ====================
 */
#endregion

using System.Threading.Channels;
using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace DMS_Library.MQTT
{
    /// <summary>
    /// Quality of Service levels for MQTT message delivery.
    /// </summary>
    public enum MqttQualityOfServiceLevel
    {
        /// <summary>At most once delivery (fire and forget)</summary>
        AtMostOnce = 0,
        /// <summary>At least once delivery (acknowledged delivery)</summary>
        AtLeastOnce = 1,
        /// <summary>Exactly once delivery (assured delivery)</summary>
        ExactlyOnce = 2
    }

    /// <summary>
    /// MQTT client status for TCPClient-style callbacks.
    /// </summary>
    public enum enumMqttStatus
    {
        CONNECTED,
        DISCONNECTED,
        RECONNECT,
        PUB_OK,
        PUB_FAILED,
        SUB_OK,
        SUB_FAILED
    }

    /// <summary>
    /// TCPClient-style callback delegate for connection status.
    /// </summary>
    /// <param name="status">Connection status enum</param>
    /// <param name="message">Status message or details</param>
    public delegate void ConnectionCallback(enumMqttStatus status, string message);

    /// <summary>
    /// TCPClient-style callback delegate for Pub/Sub status.
    /// </summary>
    /// <param name="status">Pub or Sub status</param>
    /// <param name="topic">The topic involved</param>
    public delegate void PubSubStatusCallback(enumMqttStatus status, string topic);

    /// <summary>
    /// MQTT message received event arguments.
    /// </summary>
    public class MqttMessageReceivedEventArgs : EventArgs
    {
        /// <summary>Topic the message was received on</summary>
        public string Topic { get; init; } = string.Empty;
        /// <summary>Message payload as string</summary>
        public string Payload { get; init; } = string.Empty;
        /// <summary>Raw payload bytes</summary>
        public byte[] PayloadBytes { get; init; } = Array.Empty<byte>();
        /// <summary>Quality of Service level</summary>
        public MqttQualityOfServiceLevel QoS { get; init; }
        /// <summary>Whether the message is retained</summary>
        public bool Retain { get; init; }
        /// <summary>Timestamp when message was received</summary>
        public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Connection state changed event arguments.
    /// </summary>
    public class MqttConnectionEventArgs : EventArgs
    {
        /// <summary>Connection timestamp</summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        /// <summary>Reason message if available</summary>
        public string? Reason { get; init; }
    }

    /// <summary>
    /// Configuration options for MQTT client connection.
    /// </summary>
    public class MqttClientConfig
    {
        /// <summary>MQTT broker hostname or IP address</summary>
        public string BrokerHost { get; set; } = "localhost";
        
        /// <summary>MQTT broker port (default: 1883 for non-TLS, 8883 for TLS)</summary>
        public int BrokerPort { get; set; } = 1883;
        
        /// <summary>Unique client identifier. Auto-generated if empty.</summary>
        public string ClientId { get; set; } = string.Empty;
        
        /// <summary>Username for authentication (optional)</summary>
        public string? Username { get; set; }
        
        /// <summary>Password for authentication (optional)</summary>
        public string? Password { get; set; }
        
        /// <summary>Enable automatic reconnection on disconnect</summary>
        public bool AutoReconnect { get; set; } = true;
        
        /// <summary>Delay in seconds before attempting to reconnect</summary>
        public int ReconnectDelaySeconds { get; set; } = 5;
        
        /// <summary>Keep-alive interval in seconds</summary>
        public int KeepAliveSeconds { get; set; } = 60;
        
        /// <summary>Use clean session on connect</summary>
        public bool CleanSession { get; set; } = true;
        
        /// <summary>Use TLS/SSL encryption</summary>
        public bool UseTls { get; set; } = false;
        
        /// <summary>Topic to publish Last Will and Testament message</summary>
        public string? LastWillTopic { get; set; }
        
        /// <summary>Last Will message payload</summary>
        public string? LastWillPayload { get; set; }
        
        /// <summary>Last Will QoS level</summary>
        public MqttQualityOfServiceLevel LastWillQoS { get; set; } = MqttQualityOfServiceLevel.AtLeastOnce;
        
        /// <summary>Retain Last Will message</summary>
        public bool LastWillRetain { get; set; } = false;
    }

    /// <summary>
    /// MQTT client interface for publishing and subscribing to messages.
    /// </summary>
    public interface IMqttClientWrapper : IDisposable
    {
        /// <summary>Event raised when client successfully connects</summary>
        event EventHandler<MqttConnectionEventArgs>? OnConnected;
        
        /// <summary>Event raised when client disconnects</summary>
        event EventHandler<MqttConnectionEventArgs>? OnDisconnected;
        
        /// <summary>Event raised when a message is received</summary>
        event EventHandler<MqttMessageReceivedEventArgs>? OnMessageReceived;
        
        /// <summary>Event raised when an error occurs</summary>
        event EventHandler<Exception>? OnError;
        
        /// <summary>Whether client is currently connected</summary>
        bool IsConnected { get; }
        
        /// <summary>Connect to the MQTT broker</summary>
        Task ConnectAsync(CancellationToken cancellationToken = default);
        
        /// <summary>Disconnect from the MQTT broker</summary>
        Task DisconnectAsync(CancellationToken cancellationToken = default);
        
        /// <summary>Subscribe to a topic with QoS level</summary>
        Task SubscribeAsync(string topic, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, CancellationToken cancellationToken = default);
        
        /// <summary>Unsubscribe from a topic</summary>
        Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default);
        
        /// <summary>Publish a string message to a topic</summary>
        Task PublishAsync(string topic, string payload, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false, CancellationToken cancellationToken = default);
        
        /// <summary>Publish a byte array message to a topic</summary>
        Task PublishAsync(string topic, byte[] payload, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false, CancellationToken cancellationToken = default);
        
        /// <summary>Publish an object as JSON to a topic</summary>
        Task PublishJsonAsync<T>(string topic, T data, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// MQTT client implementation using MQTTnet library.
    /// Supports publishing and subscribing with auto-reconnect capability.
    /// Follows TCPClient pattern with delegate-based callbacks.
    /// </summary>
    public class MqttClient : IMqttClientWrapper
    {
        private readonly MqttClientConfig _config;
        private IMqttClient? _mqttClient;
        private readonly MqttFactory _factory;
        private CancellationTokenSource? _reconnectCts;
        private bool _disposed;

        // Channel for thread-safe concurrent message handling
        private readonly Channel<(string Topic, string Payload, byte[] PayloadBytes, MqttQualityOfServiceLevel QoS, bool Retain)> _messageChannel;
        private Task? _messageProcessorTask;

        #region TCPClient-Style Callbacks

        /// <summary>
        /// TCPClient-style callback for connection status (CONNECTED, DISCONNECTED, RECONNECT).
        /// </summary>
        public event ConnectionCallback? ClientCallBack;

        /// <summary>
        /// TCPClient-style callback for Pub/Sub status (PUB_OK, PUB_FAILED, SUB_OK, SUB_FAILED).
        /// </summary>
        public event PubSubStatusCallback? PubSubStatus;

        #endregion

        #region Events

        /// <inheritdoc />
        public event EventHandler<MqttConnectionEventArgs>? OnConnected;

        /// <inheritdoc />
        public event EventHandler<MqttConnectionEventArgs>? OnDisconnected;

        /// <inheritdoc />
        public event EventHandler<MqttMessageReceivedEventArgs>? OnMessageReceived;

        /// <inheritdoc />
        public event EventHandler<Exception>? OnError;

        #endregion

        #region Properties

        /// <inheritdoc />
        public bool IsConnected => _mqttClient?.IsConnected ?? false;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new MQTT client with the specified configuration.
        /// </summary>
        /// <param name="config">MQTT connection configuration</param>
        public MqttClient(MqttClientConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _factory = new MqttFactory();

            // Initialize Channel for concurrent message handling
            _messageChannel = Channel.CreateBounded<(string, string, byte[], MqttQualityOfServiceLevel, bool)>(
                new BoundedChannelOptions(10000)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropOldest
                });

            StartMessageProcessor();
        }

        #endregion

        #region Connection

        /// <inheritdoc />
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_mqttClient?.IsConnected == true)
                return;

            try
            {
                var clientOptions = BuildClientOptions();
                _mqttClient = _factory.CreateMqttClient();

                SetupClientEvents();

                var response = await _mqttClient.ConnectAsync(clientOptions, cancellationToken);

                if (response.ResultCode == MqttClientConnectResultCode.Success)
                {
                    RaiseConnectedEvent("Connected successfully");
                    ClientCallBack?.Invoke(enumMqttStatus.CONNECTED, "Connected successfully");
                    StartAutoReconnectLoop();
                }
                else
                {
                    var errorMsg = $"Connection failed: {response.ResultCode} - {response.ReasonString}";
                    RaiseError(new Exception(errorMsg));
                    ClientCallBack?.Invoke(enumMqttStatus.DISCONNECTED, errorMsg);
                    if (_config.AutoReconnect)
                        StartAutoReconnectLoop();
                }
            }
            catch (Exception ex)
            {
                RaiseError(ex);
                ClientCallBack?.Invoke(enumMqttStatus.DISCONNECTED, $"Connection error: {ex.Message}");
                if (_config.AutoReconnect)
                    StartAutoReconnectLoop();
                throw;
            }
        }

        /// <summary>
        /// Connect to the MQTT broker (fire-and-forget, TCPClient-style).
        /// No need to await - connection status is reported via ClientCallBack event.
        /// </summary>
        public void Connect()
        {
            _ = ConnectAsync();
        }

        /// <inheritdoc />
        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            StopAutoReconnectLoop();

            if (_mqttClient == null)
                return;

            try
            {
                var options = new MqttClientDisconnectOptionsBuilder().Build();
                await _mqttClient.DisconnectAsync(options, cancellationToken);
                RaiseDisconnectedEvent("Disconnected by user");
                ClientCallBack?.Invoke(enumMqttStatus.DISCONNECTED, "Disconnected by user");
            }
            catch (Exception ex)
            {
                RaiseError(ex);
                ClientCallBack?.Invoke(enumMqttStatus.DISCONNECTED, $"Disconnect error: {ex.Message}");
            }
        }

        /// <summary>
        /// Disconnect from the MQTT broker (fire-and-forget, TCPClient-style).
        /// </summary>
        public void Disconnect()
        {
            _ = DisconnectAsync();
        }

        private MqttClientOptions BuildClientOptions()
        {
            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(_config.BrokerHost, _config.BrokerPort)
                .WithTimeout(TimeSpan.FromSeconds(30))
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(_config.KeepAliveSeconds))
                .WithCleanSession(_config.CleanSession);

            // Client ID - auto-generate if not provided
            if (!string.IsNullOrWhiteSpace(_config.ClientId))
                builder.WithClientId(_config.ClientId);
            else
                builder.WithClientId($"DMS_Client_{Guid.NewGuid():N}");

            // Authentication
            if (!string.IsNullOrWhiteSpace(_config.Username))
            {
                builder.WithCredentials(_config.Username, _config.Password);
            }

            // TLS
            if (_config.UseTls)
            {
                builder.WithTlsOptions(tls =>
                {
                    tls.UseTls();
                });
            }

            // Last Will and Testament
            if (!string.IsNullOrWhiteSpace(_config.LastWillTopic))
            {
                builder.WithWillTopic(_config.LastWillTopic)
                       .WithWillPayload(_config.LastWillPayload ?? string.Empty)
                       .WithWillQualityOfServiceLevel(ConvertQoS(_config.LastWillQoS))
                       .WithWillRetain(_config.LastWillRetain);
            }

            return builder.Build();
        }

        #endregion

        #region Subscribe/Unsubscribe

        /// <inheritdoc />
        public async Task SubscribeAsync(string topic, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, CancellationToken cancellationToken = default)
        {
            EnsureConnected();

            var options = _factory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(topic, ConvertQoS(qos))
                .Build();

            try
            {
                await _mqttClient!.SubscribeAsync(options, cancellationToken);
                PubSubStatus?.Invoke(enumMqttStatus.SUB_OK, topic);
            }
            catch (Exception ex)
            {
                PubSubStatus?.Invoke(enumMqttStatus.SUB_FAILED, $"{topic}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Subscribe to a topic (fire-and-forget, TCPClient-style).
        /// </summary>
        public void Subscribe(string topic, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce)
        {
            _ = SubscribeAsync(topic, qos);
        }

        /// <inheritdoc />
        public async Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default)
        {
            EnsureConnected();

            var unsubscribeOptions = _factory.CreateUnsubscribeOptionsBuilder()
                .WithTopicFilter(topic)
                .Build();

            await _mqttClient!.UnsubscribeAsync(unsubscribeOptions, cancellationToken);
        }

        #endregion

        #region Publish

        /// <inheritdoc />
        public async Task PublishAsync(string topic, string payload, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false, CancellationToken cancellationToken = default)
        {
            await PublishAsync(topic, Encoding.UTF8.GetBytes(payload), qos, retain, cancellationToken);
        }

        /// <inheritdoc />
        public async Task PublishAsync(string topic, byte[] payload, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false, CancellationToken cancellationToken = default)
        {
            EnsureConnected();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(ConvertQoS(qos))
                .WithRetainFlag(retain)
                .Build();

            try
            {
                await _mqttClient!.PublishAsync(message, cancellationToken);
                PubSubStatus?.Invoke(enumMqttStatus.PUB_OK, topic);
            }
            catch (Exception ex)
            {
                PubSubStatus?.Invoke(enumMqttStatus.PUB_FAILED, $"{topic}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Publish a string message (fire-and-forget, TCPClient-style).
        /// </summary>
        public void Publish(string topic, string payload, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false)
        {
            _ = PublishAsync(topic, payload, qos, retain);
        }

        /// <summary>
        /// Publish a byte array message (fire-and-forget, TCPClient-style).
        /// </summary>
        public void Publish(string topic, byte[] payload, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false)
        {
            _ = PublishAsync(topic, payload, qos, retain);
        }

        /// <inheritdoc />
        public async Task PublishJsonAsync<T>(string topic, T data, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false, CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            await PublishAsync(topic, json, qos, retain, cancellationToken);
        }

        /// <summary>
        /// Publish an object as JSON (fire-and-forget, TCPClient-style).
        /// </summary>
        public void PublishJson<T>(string topic, T data, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce, bool retain = false)
        {
            _ = PublishJsonAsync(topic, data, qos, retain);
        }

        #endregion

        #region Event Handlers

        private void SetupClientEvents()
        {
            if (_mqttClient == null) return;

            _mqttClient.ConnectedAsync += args =>
            {
                RaiseConnectedEvent("Connected successfully");
                ClientCallBack?.Invoke(enumMqttStatus.CONNECTED, "Connected successfully");
                return Task.CompletedTask;
            };

            _mqttClient.DisconnectedAsync += args =>
            {
                RaiseDisconnectedEvent($"Disconnected: {args.Reason}");
                ClientCallBack?.Invoke(enumMqttStatus.DISCONNECTED, $"Disconnected: {args.Reason}");

                if (_config.AutoReconnect && !_disposed)
                {
                    StartAutoReconnectLoop();
                }

                return Task.CompletedTask;
            };

            _mqttClient.ApplicationMessageReceivedAsync += args =>
            {
                // Enqueue message to Channel for thread-safe concurrent handling
                var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
                var payloadBytes = args.ApplicationMessage.PayloadSegment.ToArray();
                var qos = ConvertFromQoS(args.ApplicationMessage.QualityOfServiceLevel);

                // TryWrite is non-blocking; if channel is full, oldest messages are dropped
                _messageChannel.Writer.TryWrite((
                    args.ApplicationMessage.Topic,
                    payload,
                    payloadBytes,
                    qos,
                    args.ApplicationMessage.Retain
                ));

                return Task.CompletedTask;
            };
        }

        #endregion

        #region Message Processor

        /// <summary>
        /// Starts the background task that processes messages from the channel.
        /// Messages are processed sequentially to avoid race conditions.
        /// </summary>
        private void StartMessageProcessor()
        {
            _messageProcessorTask = Task.Run(async () =>
            {
                try
                {
                    await foreach (var (topic, payload, payloadBytes, qos, retain) in
                                  _messageChannel.Reader.ReadAllAsync())
                    {
                        if (_disposed) break;

                        try
                        {
                            var eventArgs = new MqttMessageReceivedEventArgs
                            {
                                Topic = topic,
                                Payload = payload,
                                PayloadBytes = payloadBytes,
                                QoS = qos,
                                Retain = retain,
                                ReceivedAt = DateTime.UtcNow
                            };

                            OnMessageReceived?.Invoke(this, eventArgs);
                        }
                        catch (Exception ex)
                        {
                            RaiseError(ex);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                }
            });
        }

        #endregion

        #region Auto Reconnect

        private void StartAutoReconnectLoop()
        {
            if (!_config.AutoReconnect || _disposed)
                return;

            StopAutoReconnectLoop();
            _reconnectCts = new CancellationTokenSource();

            ClientCallBack?.Invoke(enumMqttStatus.RECONNECT, "Auto-reconnect started");

            Task.Run(async () =>
            {
                while (!_reconnectCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(_config.ReconnectDelaySeconds), _reconnectCts.Token);

                        if (!IsConnected && !_disposed)
                        {
                            ClientCallBack?.Invoke(enumMqttStatus.RECONNECT, "Attempting to reconnect...");
                            await ConnectAsync(_reconnectCts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        RaiseError(ex);
                    }
                }
            }, _reconnectCts.Token);
        }

        private void StopAutoReconnectLoop()
        {
            _reconnectCts?.Cancel();
            _reconnectCts?.Dispose();
            _reconnectCts = null;
        }

        #endregion

        #region Event Raisers

        private void RaiseConnectedEvent(string? reason = null)
        {
            var args = new MqttConnectionEventArgs { Reason = reason };
            OnConnected?.Invoke(this, args);
        }

        private void RaiseDisconnectedEvent(string? reason = null)
        {
            var args = new MqttConnectionEventArgs { Reason = reason };
            OnDisconnected?.Invoke(this, args);
        }

        private void RaiseError(Exception ex)
        {
            OnError?.Invoke(this, ex);
        }

        #endregion

        #region Helpers

        private void EnsureConnected()
        {
            if (_mqttClient?.IsConnected != true)
                throw new InvalidOperationException("Client is not connected. Call ConnectAsync first.");
        }

        private static MQTTnet.Protocol.MqttQualityOfServiceLevel ConvertQoS(MqttQualityOfServiceLevel qos) => qos switch
        {
            MqttQualityOfServiceLevel.AtMostOnce => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce,
            MqttQualityOfServiceLevel.AtLeastOnce => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
            MqttQualityOfServiceLevel.ExactlyOnce => MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce,
            _ => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce
        };

        private static MqttQualityOfServiceLevel ConvertFromQoS(MQTTnet.Protocol.MqttQualityOfServiceLevel qos) => qos switch
        {
            MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce => MqttQualityOfServiceLevel.AtMostOnce,
            MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce => MqttQualityOfServiceLevel.AtLeastOnce,
            MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce => MqttQualityOfServiceLevel.ExactlyOnce,
            _ => MqttQualityOfServiceLevel.AtMostOnce
        };

        #endregion

        #region IDisposable

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            StopAutoReconnectLoop();

            // Complete and cleanup the message channel
            _messageChannel.Writer.TryComplete();

            // Wait for message processor to finish (with timeout)
            if (_messageProcessorTask != null)
            {
                try
                {
                    _messageProcessorTask.Wait(TimeSpan.FromSeconds(2));
                }
                catch (AggregateException)
                {
                    // Ignore - task may have already completed or been cancelled
                }
            }

            _mqttClient?.Dispose();
            GC.SuppressFinalize(this);
        }


        #endregion
    }
}
