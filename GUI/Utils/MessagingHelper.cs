using Bridge;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.Text;
using System;
using System.Collections.Generic;
using GUI.ViewModels;

namespace GUI.Utils;

public class MessagingHelper
{
    private static IModel _channel;
    private static string _startQueue;
	private static string _editQueue;
	private static string _bestResultQueue;
	private static string _statusInfoQueue;

	private static string _startQueueThreads;
	private static string _editQueueThreads;
	private static string _bestResultQueueThreads;
	private static string _statusInfoQueueThreads;
	
    private static UpdateStatusEventHandler _updateStatusHandler;
    private static UpdateBestResultEventHandler _updateBestResultEventHandler;

    public MessagingHelper()
    {
    }

    public static void Setup(UpdateStatusEventHandler receivedStatusUpdate, UpdateBestResultEventHandler receivedBestResult, string mechanism)
    {
        // run rabbittmq with docker: docker run -it --rm --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.11-management
        var factory = new ConnectionFactory() { HostName = "localhost", Port = 5672 };
        var connection = factory.CreateConnection();
        _channel = connection.CreateModel();

        Globals.Mechanism = mechanism;


		_startQueue = $"{Mechanisms.Tasks}Start";
		_editQueue = $"{Mechanisms.Tasks}Edit";
		_bestResultQueue = $"{Mechanisms.Tasks}BestResult";
		_statusInfoQueue = $"{Mechanisms.Tasks}StatusInfo";

		_startQueueThreads = $"{Mechanisms.Processes}Start";
		_editQueueThreads = $"{Mechanisms.Processes}Edit";
		_bestResultQueueThreads = $"{Mechanisms.Processes}BestResult";
		_statusInfoQueueThreads = $"{Mechanisms.Processes}StatusInfo";

		_channel.QueueDeclare(
			queue: _startQueue,
			durable: false,
			exclusive: false,
			autoDelete: false,
			arguments: null);

		_channel.QueueDeclare(
			queue: _editQueue,
			durable: false,
			exclusive: false,
			autoDelete: false,
			arguments: null);

		_channel.QueueDeclare(
			queue: _bestResultQueue,
			durable: false,
			exclusive: false,
			autoDelete: false,
			arguments: null);

		_channel.QueueDeclare(
			queue: _statusInfoQueue,
			durable: false,
			exclusive: false,
			autoDelete: false,
			arguments: null);


		// threads
		_channel.QueueDeclare(
			queue: _startQueueThreads,
			durable: false,
			exclusive: false,
			autoDelete: false,
			arguments: null);

		_channel.QueueDeclare(
			queue: _editQueueThreads,
			durable: false,
			exclusive: false,
			autoDelete: false,
			arguments: null);

		_channel.QueueDeclare(
			queue: _bestResultQueueThreads,
			durable: false,
			exclusive: false,
			autoDelete: false,
			arguments: null);

		_channel.QueueDeclare(
			queue: _statusInfoQueueThreads,
			durable: false,
			exclusive: false,
			autoDelete: false,
			arguments: null);



		// handle status updating
		_updateStatusHandler = receivedStatusUpdate;
        _updateBestResultEventHandler = receivedBestResult;
    }

    public static void ReceiveMessages()
    {
        var consumer = new EventingBasicConsumer(_channel);

        var startQueue = $"{Globals.Mechanism}Start";
        var editQueue = $"{Globals.Mechanism}Edit";
        var statusInfoQueue = $"{Globals.Mechanism}StatusInfo";
        var bestResultQueue = $"{Globals.Mechanism}BestResult";

        _channel.BasicConsume(queue: statusInfoQueue, autoAck: true, consumer: consumer);
        _channel.BasicConsume(queue: bestResultQueue, autoAck: true, consumer: consumer);

        consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                if (ea.RoutingKey == statusInfoQueue)
                {
                    var results = JsonSerializer.Deserialize<StatusInfo>(message);
                    Console.WriteLine(" [x] Received {0}", results);

                    if (results != null)
                    {
                        _updateStatusHandler?.Invoke(results);
                    }
                }

                if (ea.RoutingKey == bestResultQueue)
                {
                    var results = JsonSerializer.Deserialize<Results>(message);
                    Console.WriteLine(" [x] Received {0}", results);

                    if (results != null)
                    {
                        _updateBestResultEventHandler?.Invoke(results);
                    }
                }
            };
    }

    public static void SendInitialMessageFromClient(
        int phaseOneDurationInMs, 
        int phaseTwoDurationInMs, 
        int quantityForMechanism, 
        int numberOfEpochs, 
        List<Bridge.Point> points,
        string mechanism)
    {
        MessageFromClient msg = new()
        {
            NumberOfTasks = quantityForMechanism,
            TimePhase1 = phaseOneDurationInMs,
            TimePhase2 = phaseTwoDurationInMs,
            NumberOfEpochs = numberOfEpochs,
            Points = points,
            Mechanism = mechanism
        };

        string message = JsonSerializer.Serialize(msg);

        var body = Encoding.UTF8.GetBytes(message);

        _channel.BasicPublish("", Globals.Mechanism + "Start", null, body);

        Console.WriteLine("Published initial message!");
    }

    public static void CloseConnection()
    {
        _channel.Dispose();
    }

    public static void SendEditMessage(bool stop, int phaseOneDurationInMs, int phaseTwoDurationInMs)
    {
        UserChanges userChanges = new()
        {
            Stop = stop,
            NewTimeA = phaseOneDurationInMs,
            NewTimeB = phaseTwoDurationInMs
        };

        string userChangesMsg = JsonSerializer.Serialize(userChanges);

        var body = Encoding.UTF8.GetBytes(userChangesMsg);

        _channel.BasicPublish("", Globals.Mechanism + "Edit", null, body);

        Console.WriteLine(" [x] Sent {0}", userChangesMsg);
    }
}
