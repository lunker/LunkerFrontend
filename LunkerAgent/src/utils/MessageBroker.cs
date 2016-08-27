﻿using log4net;
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
        private string webChatQueueName = "WebChatQueue";

        private IModel channel = null;

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

            // =======================================================
            // =========================Declare Queue For Agent App 
            // =======================================================
            channel.QueueDeclare(queue: agentQueueName,
               durable: false,
               exclusive: false,
               autoDelete: false,
               arguments: null);

            // =======================================================
            // =========================Declare Queue For chat server 
            // =======================================================

            channel.QueueDeclare(queue: chatQueueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // =======================================================
            // =================Declare Queue For websocket chat server
            // =======================================================
            channel.QueueDeclare(queue: webChatQueueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);


            // Register Subscriber
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
        public void PublishSocketServer(Object message)
        {
            channel.BasicPublish(exchange: "",
                        routingKey: chatQueueName,
                        basicProperties: null,
                        body: NetworkManager.StructureToByte(message));
            Console.WriteLine($"[MessageBroker][Publish()] publish message : {message}");
        }// end method

        public void PubishWebSocketServer(Object message)
        {
            channel.BasicPublish(exchange: "",
                       routingKey: webChatQueueName,
                       basicProperties: null,
                       body: NetworkManager.StructureToByte(message));
            Console.WriteLine($"[MessageBroker][Publish()] publish message : {message}");
        }

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
                    AdminAgent.GetInstance().HandleResponse(responseHeader);
                }
            };
            
            channel.BasicConsume(queue: agentQueueName,
                                    noAck: true,
                                    consumer: consumer);

            channel.BasicConsume(queue: webChatQueueName,
                                    noAck: true,
                                    consumer: consumer);

        }// end method
       
    }
}
