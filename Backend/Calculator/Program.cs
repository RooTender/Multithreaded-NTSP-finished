using Bridge;
using Calculator;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

var factory = new ConnectionFactory() { HostName = "localhost", Port = 5672 };

using var connection = factory.CreateConnection();

using var channel = connection.CreateModel();

var startQueue = $"{Mechanisms.Tasks}Start";
var editQueue = $"{Mechanisms.Tasks}Edit";
var bestResultQueue = $"{Mechanisms.Tasks}BestResult";
var statusInfoQueue = $"{Mechanisms.Tasks}StatusInfo";

var startQueueThreads = $"{Mechanisms.Processes}Start";
var editQueueThreads = $"{Mechanisms.Processes}Edit";
var bestResultQueueThreads = $"{Mechanisms.Processes}BestResult";
var statusInfoQueueThreads = $"{Mechanisms.Processes}StatusInfo";

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


// threads
channel.QueueDeclare(
	queue: startQueueThreads,
	durable: false,
	exclusive: false,
	autoDelete: false,
	arguments: null);

channel.QueueDeclare(
	queue: editQueueThreads,
	durable: false,
	exclusive: false,
	autoDelete: false,
	arguments: null);

channel.QueueDeclare(
	queue: bestResultQueueThreads,
	durable: false,
	exclusive: false,
	autoDelete: false,
	arguments: null);

channel.QueueDeclare(
	queue: statusInfoQueueThreads,
	durable: false,
	exclusive: false,
	autoDelete: false,
	arguments: null);


var consumer = new EventingBasicConsumer(channel);

channel.BasicConsume(queue: startQueue, autoAck: true, consumer: consumer);
channel.BasicConsume(queue: editQueue, autoAck: true, consumer: consumer);
channel.BasicConsume(queue: startQueueThreads, autoAck: true, consumer: consumer);
channel.BasicConsume(queue: editQueueThreads, autoAck: true, consumer: consumer);

MultiTaskNTSP? multiTaskNTSP = null;
MultiThreadNTSP? multiThreadNTSP = null;

consumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);

    if(ea.RoutingKey == startQueue) {
        Task.Run(() => ProcessTask(message));
    }

    if (ea.RoutingKey == editQueue)
    {
        var msgUserChanges = JsonSerializer.Deserialize<UserChanges>(message);
        if(multiTaskNTSP != null)
        {
            multiTaskNTSP.phase1TimeOut = msgUserChanges.NewTimeA;
            multiTaskNTSP.phase2TimeOut = msgUserChanges.NewTimeB;

            if(msgUserChanges.Stop) 
            {
                multiTaskNTSP._cts.Cancel();
            }
        }
    }

    // threads
	if (ea.RoutingKey == startQueueThreads)
	{
		Task.Run(() => ProcessThread(message));
	}

	if (ea.RoutingKey == editQueueThreads)
	{
		var msgUserChanges = JsonSerializer.Deserialize<UserChanges>(message);
		if (multiThreadNTSP != null)
		{
			multiThreadNTSP.phase1TimeOut = msgUserChanges.NewTimeA;
			multiThreadNTSP.phase2TimeOut = msgUserChanges.NewTimeB;

			if (msgUserChanges.Stop)
			{
				multiThreadNTSP._cts.Cancel();
			}
		}
	}
};

void ProcessTask(string message)
{
    var msgFromClient = JsonSerializer.Deserialize<MessageFromClient>(message) ?? throw new ArgumentNullException();
    Globals.Mechanism = msgFromClient.Mechanism;
    multiTaskNTSP = new MultiTaskNTSP(msgFromClient.NumberOfTasks, msgFromClient.TimePhase1, msgFromClient.TimePhase2,
        msgFromClient.NumberOfEpochs, channel);
    multiTaskNTSP.Run(msgFromClient.Points);
}

void ProcessThread(string message)
{
	var msgFromClient = JsonSerializer.Deserialize<MessageFromClient>(message) ?? throw new ArgumentNullException();
	Globals.Mechanism = msgFromClient.Mechanism;
	multiThreadNTSP = new MultiThreadNTSP(msgFromClient.NumberOfTasks, msgFromClient.TimePhase1, msgFromClient.TimePhase2,
		msgFromClient.NumberOfEpochs, channel);
	multiThreadNTSP.Run(msgFromClient.Points);
}

Console.ReadKey();

