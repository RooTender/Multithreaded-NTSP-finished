using System.Collections.Generic;
using Bridge;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.Text;
using GUI.ViewModels;

namespace GUI.Utils;

public class MessagingHelper
{
    private readonly IModel _channel;
    private readonly StatusNotifier _statusNotifier;
    private readonly NewResultNotifier _newResultNotifier;

    public MessagingHelper(StatusNotifier statusNotifier, NewResultNotifier newResultNotifier)
    {
        _statusNotifier = statusNotifier;
        _newResultNotifier = newResultNotifier;
        _channel = RabbitQueue.SetupChannel();

        ReactToIncomingMessages();
    }

    public void StartCalculations(CalculationDTO calculationData)
    {
        _channel.BasicPublish("", RabbitQueue.QueueTypes.Start, null, SerializeMessage(calculationData));
    }

    public void AbortCalculations()
    {
        _channel.BasicPublish("", RabbitQueue.QueueTypes.Stop, null, null);
    }

    private void ReactToIncomingMessages()
    {
        var consumer = new EventingBasicConsumer(_channel);

        _channel.BasicConsume(queue: RabbitQueue.QueueTypes.Status, autoAck: true, consumer: consumer);
        _channel.BasicConsume(queue: RabbitQueue.QueueTypes.UpdateBest, autoAck: true, consumer: consumer);

        consumer.Received += (_, sender) =>
        {
            var body = sender.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            switch (sender.RoutingKey)
            {
                case RabbitQueue.QueueTypes.Status:
                {
                    var results = JsonSerializer.Deserialize<CalculationStatusDTO>(message);
                    if (results != null) _statusNotifier(results);
                    break;
                }

                case RabbitQueue.QueueTypes.UpdateBest:
                {
                    var results = JsonSerializer.Deserialize<List<Point>>(message);
                    if (results != null) _newResultNotifier(results);
                    break;
                }
            }
        };
    }

    private static byte[] SerializeMessage(object message)
    {
        var serializedMessage = JsonSerializer.Serialize(message);
        return Encoding.UTF8.GetBytes(serializedMessage);
    }
}
