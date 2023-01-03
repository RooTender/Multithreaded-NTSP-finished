using Bridge;
using Calculator;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using RQueue = Bridge.RabbitQueue.QueueTypes;

var channel = RabbitQueue.SetupChannel();
var consumer = new EventingBasicConsumer(channel);
ParallelNTSP? calculations = null;

foreach (var queueName in new[] { RQueue.Start, RQueue.Stop,  RQueue.Status, RQueue.UpdateBest })
{                               
    channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
}

consumer.Received += (_, sender) =>
{
    var body = sender.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);

    switch (sender.RoutingKey)
    {
        case RQueue.Start:
            StartCalculations(message);
            break;

        case RQueue.Stop:
            AbortCalculations();
            break;
    }
};

void StartCalculations(string encodedMessage)
{
    var message = JsonSerializer.Deserialize<CalculationDTO>(encodedMessage);
    if (message == null) return;

    calculations = message.Mechanism switch
    {
        Mechanism.Type.Tasks => new MultiTaskNTSP(message, channel),
        Mechanism.Type.Threads => new MultiThreadNTSP(message, channel),
        _ => calculations
    };

    Task.Run(() => calculations?.Run(message.Points));
}

void AbortCalculations()
{
    calculations?.Abort();
}

Console.ReadKey();
