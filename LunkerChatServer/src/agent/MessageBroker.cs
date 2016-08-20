using LunkerLibrary.common.Utils;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunkerChatServer.src.agent
{
    /// <summary>
    /// Publish & Subscribe Message between agent & chat server
    /// </summary>
    class MessageBroker
    {
        private static MessageBroker instance = null;
        private string chatQueueName = "ChatQueue"; // to chatqueue.subscribe. request from agent
        private string agentQueueName = "AgentQueue"; // to agent queue. publish. response to agent.
        
        private IModel channel = null;

        private MessageBroker() { Setup(); }

        public static MessageBroker GetInstance()
        {   
            if(instance == null)
            {
                instance = new MessageBroker();
            }
            return instance;
        }

        /// <summary>
        /// Setup Queue
        /// </summary>
        private void Setup()
        {
            ConnectionFactory factory = new ConnectionFactory() { HostName = "localhost" };
            channel = factory.CreateConnection().CreateModel();

            channel.QueueDeclare(queue: agentQueueName,
               durable: false,
               exclusive: false,
               autoDelete: false,
               arguments: null);

            channel.QueueDeclare(queue: chatQueueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            
            factory = null;

            RegisterSubscribe();

        }// end method

        public void Release()
        {
            channel.Dispose();
            instance = null;
        }

        public void Publish(Object message)
        {
            if(message is string)
            {
                channel.BasicPublish(exchange: "",
                        routingKey: agentQueueName,
                        basicProperties: null,
                        body: NetworkManager.StructureToByte(message));
            }
        }// end method

        public void RegisterSubscribe()
        {
            Console.WriteLine("RegisterSubscribe");
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;

                AAHeader message = (AAHeader) NetworkManager.ByteToStructure(body,typeof(AAHeader));

                // 
                HandleRequest(message.Type);
            };
            channel.BasicConsume(queue: chatQueueName,
                                    noAck: true,
                                    consumer: consumer);
        }// end method

        public void HandleRequest(MessageType type)
        {
            Console.WriteLine("HandleRequest");
            switch (type)
            {
                case MessageType.RestartApp:
                case MessageType.ShutdownApp:

                    Power.Off(type);
                    break;
            }
        }// end method
    }
}
