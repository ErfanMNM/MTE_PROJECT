using DMS_Library.TCP;
using System.ComponentModel;

Queue<MQTT_Pub_Payload> _MQTT_Pub_Queue = new Queue<MQTT_Pub_Payload>();

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
            //_MQTTclient.PublishAsync(message);
        }
        Thread.Sleep(50);
    }
}

TCPClient cameraTCP = new TCPClient
{
    IP = "127.0.0.1",
    Port = 12345
};

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

Task task = Task.Run(() =>
{
    while (true)
    {
       Console.WriteLine("HIIIIIIIIIIIIIIIIIIIIIIIIIIIIII");
        Thread.Sleep(1000);
    }
});

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