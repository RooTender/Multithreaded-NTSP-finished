//using System;
//using RabbitMQ.Client;
//using System.Text;
//using System.Text.Json;
//using Communicator;
//using RabbitMQ.Client.Events;


//class Send
//{
//    public static void Main()
//    {

//        string operationType = "thread";


//        var factory = new ConnectionFactory() { HostName = "localhost" };

//        using var connection = factory.CreateConnection();

//        using var channel = connection.CreateModel();



//        channel.QueueDeclare(
//            queue: operationType + "Start",
//            durable: false,
//            exclusive: false,
//            autoDelete: false,
//            arguments: null);

//        channel.QueueDeclare(
//            queue: operationType + "Edit",
//            durable: false,
//            exclusive: false,
//            autoDelete: false,
//            arguments: null);

//        channel.QueueDeclare(
//            queue: operationType + "bestResult",
//            durable: false,
//            exclusive: false,
//            autoDelete: false,
//            arguments: null);

//        channel.QueueDeclare(
//            queue: operationType + "Results",
//            durable: false,
//            exclusive: false,
//            autoDelete: false,
//            arguments: null);


//        //wysyłanie wiadomości startowej

//        MessageFromClient msg = CreateExampleMessage();

//        string message = JsonSerializer.Serialize(msg);

//        var body = Encoding.UTF8.GetBytes(message);

//        channel.BasicPublish("", operationType + "Start", null, body);

//        Console.WriteLine(" [x] Sent {0}", message);

//        //wysyałanie zmiany przez użytkownika
//        UserChanges userChanges = CreateExampleUserChanges();

//        string userChangesMsg = JsonSerializer.Serialize(userChanges);

//        var userChangesBody = Encoding.UTF8.GetBytes(userChangesMsg);

//        channel.BasicPublish("", operationType + "Edit", null, userChangesBody);

//        Console.WriteLine(" [x] Sent {0}", userChangesMsg);



//        ///////////////////////////////////

//        //czekanie na wiadomości

//        var consumer = new EventingBasicConsumer(channel);

//        consumer.Received += (model, ea) =>
//        {
//            var body = ea.Body.ToArray();
//            var message = Encoding.UTF8.GetString(body);
            
            
//            if (ea.RoutingKey == "bestResult")
//            {
//                Results? results = JsonSerializer.Deserialize<Results>(message);
//                Console.WriteLine(" [x] Received {0}", results);
//            }


//        };



//        channel.BasicConsume(queue: "bestResult", autoAck: true, consumer: consumer);
//        channel.BasicConsume(queue: "Results", autoAck: true, consumer: consumer);

//        Console.ReadLine();

//    }

//    public static MessageFromClient CreateExampleMessage()
//    {
//        MessageFromClient msg = new MessageFromClient();
//        msg.NumberOfTasks = 1;
//        msg.TimePhase1 = 30;
//        msg.TimePhase2 = 60;
//        msg.NumberOfEpoch = 100;

//        msg.Points.AddRange(new[]
//        {
//            new Point(10, 20),
//            new Point(10, 30),
//            new Point(9, 40),
//            new Point(8, 50),
//            new Point(7, 60),
//            new Point(4, 70),
//            new Point(3, 80),
//            new Point(2, 90),
//            new Point(1, 100),
//        });


//        return msg;
//    }

//    public static UserChanges CreateExampleUserChanges()
//    {
//        UserChanges msg = new UserChanges();

//        // true jezeli uzytkownik kliknal stop
//        // false jezeli nie kliknal
//        msg.Stop = false;

//        // nowe czasy wpisane przez uzytkownika
//        msg.NewTimeA = 30;
//        msg.NewTimeB = 60;

//        return msg;
//    }
//}
//internal class MessageFromClient
//{
//    public List<Point> Points { get; set; } = new List<Point>();
//    public int NumberOfTasks { get; set; }
//    public int NumberOfEpoch { get; set; }
//    public int TimePhase1 { get; set; }
//    public int TimePhase2 { get; set; }
//}

//public class UserChanges
//{
//    public bool Stop { get; set; }
//    public int NewTimeA { get; set; }
//    public int NewTimeB { get; set; }
//}

//public class Results
//{
//    public List<Point> points { get; set; } = new List<Point>();
//}