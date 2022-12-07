using Bridge;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.Text;
using System;
using System.Collections.Generic;
using System.Windows;
using GUI.ViewModels;
using System.Threading;

namespace GUI.Utils;

public class MessagingHelper
{
    public static string? Mechanism { get; private set; }

    private static IModel channel;

    private static UpdateStatusEventHandler updateStatusHandler;
    private static UpdateBestResultEventHandler updateBestResultEventHandler;

    public MessagingHelper(string? mechanism)
    {
        Mechanism = mechanism;
    }

    public static void Setup(UpdateStatusEventHandler receivedStatusUpdate, UpdateBestResultEventHandler receivedBestResult)
    {
        // run rabbittmq with docker: docker run -it --rm --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.11-management
        var factory = new ConnectionFactory() { HostName = "localhost", Port = 5672 };
        var connection = factory.CreateConnection();
        channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: Mechanism + "Start",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueDeclare(
            queue: Mechanism + "Edit", // problem wih initialising with processes
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueDeclare(
            queue: Mechanism + "BestResult",
            durable: false,
            exclusive: false,
        autoDelete: false,
            arguments: null);

        channel.QueueDeclare(
            queue: Mechanism + "StatusInfo",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // handle status updating
        updateStatusHandler = receivedStatusUpdate;
        updateBestResultEventHandler = receivedBestResult;
    }

    public static void ReceiveMessages()
    {
        var consumer = new EventingBasicConsumer(channel);

        var startQueue = $"{Globals.Mechanism}Start";
        var editQueue = $"{Globals.Mechanism}Edit";
        var statusInfoQueue = $"{Globals.Mechanism}StatusInfo";
        var bestResultQueue = $"{Globals.Mechanism}BestResult";

        channel.BasicConsume(queue: statusInfoQueue, autoAck: true, consumer: consumer);
        channel.BasicConsume(queue: bestResultQueue, autoAck: true, consumer: consumer);

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
                        updateStatusHandler?.Invoke(results);
                    }
                }

                if (ea.RoutingKey == bestResultQueue)
                {
                    var results = JsonSerializer.Deserialize<Results>(message);
                    Console.WriteLine(" [x] Received {0}", results);

                    if (results != null)
                    {
                        updateBestResultEventHandler?.Invoke(results);
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

        Globals.Mechanism = mechanism;

        string message = JsonSerializer.Serialize(msg);

        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish("", Globals.Mechanism + "Start", null, body);

        Console.WriteLine("Published initial message!");
    }

    public static void CloseConnection()
    {
        channel.Dispose();
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

        channel.BasicPublish("", Globals.Mechanism + "Edit", null, body);

        Console.WriteLine(" [x] Sent {0}", userChangesMsg);
    }
}
