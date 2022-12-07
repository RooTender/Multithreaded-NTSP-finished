using Bridge;
using Calculator;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

var factory = new ConnectionFactory() { HostName = "localhost", Port = 5672 };

using var connection = factory.CreateConnection();

using var channel = connection.CreateModel();

var startQueue = $"{Globals.Mechanism}Start";
var editQueue = $"{Globals.Mechanism}Edit";
var bestResultQueue = $"{Globals.Mechanism}BestResult";
var statusInfoQueue = $"{Globals.Mechanism}StatusInfo";

channel.QueueDeclare(
    queue: startQueue,
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null);

channel.QueueDeclare(
    queue: editQueue,
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null);

channel.QueueDeclare(
    queue: bestResultQueue,
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null);

channel.QueueDeclare(
    queue: statusInfoQueue,
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null);


var consumer = new EventingBasicConsumer(channel);

channel.BasicConsume(queue: startQueue, autoAck: true, consumer: consumer);
channel.BasicConsume(queue: editQueue, autoAck: true, consumer: consumer);

MultiThreadNTSP? ntsp = null;

consumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);

    if(ea.RoutingKey == startQueue) {
        Task.Run(() => ProcessClientAsync(message));
    }

    if (ea.RoutingKey == editQueue)
    {
        var msgUserChanges = JsonSerializer.Deserialize<UserChanges>(message);
        if(ntsp != null)
        {
            ntsp.phase1TimeOut = msgUserChanges.NewTimeA;
            ntsp.phase2TimeOut = msgUserChanges.NewTimeB;

            if(msgUserChanges.Stop) 
            {
                ntsp._cts.Cancel();
            }
        }
    }
};

void ProcessClientAsync(string message)
{
    var msgFromClient = JsonSerializer.Deserialize<MessageFromClient>(message) ?? throw new ArgumentNullException();
    Globals.Mechanism = msgFromClient.Mechanism;
    ntsp = new MultiThreadNTSP(msgFromClient.NumberOfTasks, msgFromClient.TimePhase1, msgFromClient.TimePhase2,
        msgFromClient.NumberOfEpochs, channel);
    ntsp.Run(msgFromClient.Points);
}

Console.ReadKey();

