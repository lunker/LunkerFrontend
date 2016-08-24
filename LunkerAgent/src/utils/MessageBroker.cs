using log4net;
using LunkerLibrary.common.Utils;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LunkerAgent.src.utils
{
    class MessageBroker
    {
        private static MessageBroker instance = null;
        private ILog logger = AgentLogger.GetLoggerInstance();
        public delegate void MessageConumer(Object model, BasicDeliverEventArgs events, Socket admin); // subscriber

        private string chatQueueName = "ChatQueue"; // to chatqueue.subscribe. request from agent
        private string agentQueueName = "AgentQueue"; // to agent queue. publish. response to agent.

        
        private IModel channel = null;

        private IModel chatQueueChannel = null;
        private IModel agentQueueChannel = null;
       

        private MessageBroker() { Setup(); }

        public static MessageBroker GetInstance()
        {
            if (instance == null)
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

            
            RegisterSubscribe();
            factory = null;
            
        }// end method

        public void Release()
        {
            channel.Dispose();
            instance = null;
        }
        
        /// <summary>
        /// publish message to agentQueue
        /// </summary>
        /// <param name="message"></param>
        public void Publish(Object message)
        {
            channel.BasicPublish(exchange: "",
                        routingKey: chatQueueName,
                        basicProperties: null,
                        body: NetworkManager.StructureToByte(message));

            logger.Debug($"[MessageBroker][Publish()] publish message : {message}");
        }// end method

        
        public void RegisterSubscribe()
        {
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;

                AAHeader responseHeader = (AAHeader) NetworkManager.ByteToStructure(body, typeof(AAHeader));

                logger.Debug($"[MessageBroker][Publish()] Subscribe message~! type : " + nameof(responseHeader.Type));
                logger.Debug($"[MessageBroker][Publish()] Subscribe message~! state : " + nameof(responseHeader.State));

                if (responseHeader.Type == MessageType.RestartApp)
                {
                    ;
                }
                else
                {
                    // send result to admin tool 
                    // call send method
                    AdminAgent.GetInstance().HandleResponse(responseHeader);
                }
            };
            
            channel.BasicConsume(queue: agentQueueName,
                                    noAck: true,
                                    consumer: consumer);
        }// end method
       
    }
}
