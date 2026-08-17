using DMS_App.Controllers;
using DMS_App.DataPool;
using DMS_Library.MQTT;
using DMS_Library.TCP;
using MQTTnet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;

Queue<MQTT_Pub_Payload> _MQTT_Pub_Queue = new Queue<MQTT_Pub_Payload>();
MqttClient _MQTTclient = new MqttClient(new MqttClientConfig
{
    BrokerHost = "127.0.0.1",
    BrokerPort = 1883,
    ClientId = "backend",
    AutoReconnect = true,
    ReconnectDelaySeconds = 5
});

BackgroundWorker WK_Dequeue_MQTT = new BackgroundWorker
{
    WorkerSupportsCancellation = true
};

WK_Dequeue_MQTT.DoWork += WK_Dequeue_MQTT_DoWork;
WK_Dequeue_MQTT.RunWorkerAsync();

void WK_Dequeue_MQTT_DoWork(object? sender, DoWorkEventArgs e)
{
    while (!WK_Dequeue_MQTT.CancellationPending)
    {

        if (_MQTT_Pub_Queue.TryDequeue(out MQTT_Pub_Payload? payload))
        {
            _MQTTclient.PublishAsync(message);
        }



        Thread.Sleep(50);
    }
}

TCPClient cameraTCP = new TCPClient
{
    IP = "127.0.0.1",
    Port = 12345
};

_MQTTclient.ClientCallBack += MQTTclient_ClientCallBack;
_MQTTclient.OnMessageReceived += MQTTclient_OnMessageReceived;
_MQTTclient.Connect();

void MQTTclient_OnMessageReceived(object? sender, MqttMessageReceivedEventArgs e)
{
    switch (e.Topic)
    {
        case "datapool/rq":
            dynamic dyn = JObject.Parse(e.Payload);
            string _action = dyn.action;
            //chuyển string thành enum
            if (Enum.TryParse(_action, out e_DataPool_Query_Payload_Action action))
            {
                switch (action)
                {
                    case e_DataPool_Query_Payload_Action.GetPoolPath:
                        string _poolname = dyn.poolname;
                        DataPool _dataPool = new DataPool();
                        DataPoolResultString dataPoolPathRs = _dataPool.GetPoolPath(_poolname);

                        DataPool_Query_Payload_Res_GetPoolPatch res = new DataPool_Query_Payload_Res_GetPoolPatch
                        {
                            id = dyn.id,
                            timestamp = DateTime.Now.ToString("o"),
                            poolname = _poolname,
                            poolpath = dataPoolPathRs.Data,
                            status = dataPoolPathRs.Success?200:403,
                            message = dataPoolPathRs.Message,
                        };

                        string payloadrs = JsonConvert.SerializeObject(res);
                        MQTT_Pub_Payload _Pub_Payload = new MQTT_Pub_Payload("datapool/rq", payloadrs);
                        _MQTT_Pub_Queue.Enqueue(_Pub_Payload);
                        break;
                     default:
                        DataPool_Query_Payload_Res_GetPoolPatch res_01 = new DataPool_Query_Payload_Res_GetPoolPatch
                        {
                            id = dyn.id,
                            timestamp = DateTime.Now.ToString("o"),
                            poolname = "",
                            poolpath = "",
                            status = 404,
                            message = "Không tồn tại mã hành động",
                        };

                        string payloadrs_01 = JsonConvert.SerializeObject(res_01);
                        MQTT_Pub_Payload _Pub_Payload_01 = new MQTT_Pub_Payload("datapool/rq", payloadrs_01);
                        _MQTT_Pub_Queue.Enqueue(_Pub_Payload_01);
                        break;
                }
            }
            else
            {
                DataPool_Query_Payload_Res_GetPoolPatch res_01 = new DataPool_Query_Payload_Res_GetPoolPatch
                {
                    id = dyn.id,
                    timestamp = DateTime.Now.ToString("o"),
                    poolname = "",
                    poolpath = "",
                    status = 404,
                    message = "Không tồn tại mã hành động",
                };

                string payloadrs_01 = JsonConvert.SerializeObject(res_01);
                MQTT_Pub_Payload _Pub_Payload_01 = new MQTT_Pub_Payload("datapool/rq", payloadrs_01);
                _MQTT_Pub_Queue.Enqueue(_Pub_Payload_01);
            }
            break;
        default:
            Console.WriteLine($"Unknown Topic Received: {e.Topic}, Payload={e.Payload}");
            break;
    }
}

void MQTTclient_ClientCallBack(enumMqttStatus status, string message)
{
    switch (status)
    {
        case enumMqttStatus.CONNECTED:
            Console.WriteLine($"MQTT Connected: {message}");
            _MQTTclient.Subscribe("datapool/rq", 0);
            break;
        case enumMqttStatus.DISCONNECTED:
            Console.WriteLine($"MQTT Disconnected: {message}");
            break;
        case enumMqttStatus.RECONNECT:
            Console.WriteLine($"MQTT Reconnecting: {message}");
            break;
        case enumMqttStatus.PUB_OK:
            Console.WriteLine("MQTT Publish OK" );
            break;
        case enumMqttStatus.PUB_FAILED:
            Console.WriteLine("MQTT Publish Failed");
            break;
        case enumMqttStatus.SUB_OK:
            Console.WriteLine("MQTT Subscribe OK");
            break;
        case enumMqttStatus.SUB_FAILED:
            Console.WriteLine("MQTT Subscribe Failed");
            break;
    }
}

cameraTCP.ClientCallBack += CameraTCP_ClientCallBack;
cameraTCP.Connect();
void CameraTCP_ClientCallBack(enumClient state, string data)
{
    switch (state)
    {
        case enumClient.CONNECTED:
            Console.WriteLine($"Camera TCP Connected: {data}");
            break;
        case enumClient.DISCONNECTED:
            Console.WriteLine($"Camera TCP Disconnected: {data}");
            break;
        case enumClient.RECEIVED:
            Console.WriteLine($"Camera TCP Received: {data}");
            break;
        case enumClient.RECONNECT:
            Console.WriteLine($"Camera TCP Reconnecting: {data}");
            break;
    }
}

while (true);

public class MQTT_Pub_Payload
{
    public string Topic { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public MQTT_Pub_Payload(string topic, string payload)
    {
        Topic = topic;
        Payload = payload;
    }
}