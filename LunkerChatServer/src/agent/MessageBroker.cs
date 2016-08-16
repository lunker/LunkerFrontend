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

        
        //private IModel chatQueue = null;
        //private IModel agentQueue = null;

        private IModel channel = null;



        private MessageBroker() { }

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

            /*
            chatQueue = factory.CreateConnection().CreateModel();
            agentQueue = factory.CreateConnection().CreateModel();

            
            chatQueue.QueueDeclare(queue: chatQueueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

            agentQueue.QueueDeclare(queue: agentQueueName,
                         durable: false,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);
            */
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
                        body: Encoding.UTF8.GetBytes((string)message));

            }
        }// end method
    

        public void Subscribe()
        {
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Received {0}", message);
            };
            channel.BasicConsume(queue: "hello",
                                    noAck: true,
                                    consumer: consumer);
        }// end method


    }
}
