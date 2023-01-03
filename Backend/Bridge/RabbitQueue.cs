using RabbitMQ.Client;

namespace Bridge
{
    public static class RabbitQueue
    {
        public static class QueueTypes
        {
            public const string Start = "Start";
            public const string Stop = "Stop";
            public const string UpdateBest = "UpdateBest";
            public const string Status = "Status";
        }

        public static class RabbitMQConfig
        {
            public const string HostName = "localhost";
            public const int Port = 5672;
        }

        public static IModel SetupChannel()
        {
            var factory = new ConnectionFactory { HostName = RabbitMQConfig.HostName, Port = RabbitMQConfig.Port };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            var queueTypes = new[] { QueueTypes.Start, QueueTypes.Stop, QueueTypes.UpdateBest, QueueTypes.Status };
            foreach (var queueType in queueTypes)
            {
                channel.QueueDeclare(
                    queue: queueType,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
            }
            
            return channel;
        }
    }
}
