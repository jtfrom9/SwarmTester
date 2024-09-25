#nullable enable

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Cysharp.Threading.Tasks;
using UniRx;

using MQTTnet;
using MQTTnet.Client;

public class MQTTTest : MonoBehaviour
{
    [SerializeField]
    Button? connectButton;

    [SerializeField]
    Button? publishButton;

    [SerializeField]
    TMP_InputField? inputField;


    void Start()
    {
        if(connectButton==null || publishButton==null || inputField==null) {
            Debug.LogError("fail");
            return;
        }

        IMqttClient? client = null;

        void UpdateState(bool v)
        {
            publishButton.interactable = v;
            inputField.interactable = v;
        }
        UpdateState(false);

        var factory = new MqttFactory();
        // MqttTopicTemplate sampleTemplate = new("mqttnet/samples/topic/{id}");

        connectButton.OnClickAsObservable().Subscribe(async _ => {
            if (client == null)
            {
                var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("broker.hivemq.com").Build();
                client = factory.CreateMqttClient();

                client.ApplicationMessageReceivedAsync += e => {
                    Debug.Log("Recved");
                    Debug.Log($"{JsonConvert.SerializeObject(e, Formatting.Indented)}");

                    var segment = e.ApplicationMessage.PayloadSegment;
                    // Debug.Log(e.ApplicationMessage.PayloadSegment.ToString());
                    var str = Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);
                    Debug.Log(str);

                    return Task.CompletedTask;
                };

                Debug.Log(">> start connecting...");
                var response = await client.ConnectAsync(mqttClientOptions, CancellationToken.None);

                Console.WriteLine("The MQTT client is connected.");

                Debug.Log("Connected");
                Debug.Log($"{JsonConvert.SerializeObject(response, Formatting.Indented)}");

                var mqttSubscribeOptions = factory.CreateSubscribeOptionsBuilder().WithTopicFilter("swarnTester/hoge").Build();

                await client.SubscribeAsync(mqttSubscribeOptions);
                Debug.Log("Subscribed");
            }
            else
            {
                var mqttClientDisconnectOptions = factory.CreateClientDisconnectOptionsBuilder().Build();
                await client.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);
                client = null;
            }
            UpdateState(client != null);
        }).AddTo(this);

        publishButton.OnClickAsObservable().Subscribe(async _ => {
            if (client == null)
                return;
            var result = await client.PublishStringAsync("swarnTester/hoge2", "foo");
            Debug.Log("published");
            Debug.Log($"{JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }).AddTo(this);
    }
}
