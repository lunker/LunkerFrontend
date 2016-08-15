
using log4net;
using LunkerLibrary.common.protocol.login_chat;
using LunkerLibrary.common.Utils;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace LunkerChatServer
{
    /**
     * Socket Listener for Backend Component - login server, other chat server agent, Backend Server  
     */
    class BackendListener
    {
        private static BackendListener backListener = null;
        private bool threadState = Constants.ThreadRun;

        private ILog logger = Logger.GetLoggerInstance();
        private static FrontListener frontListener = null;
        private Socket sockListener = null;

        private BackendListener()
        {

        }
        public static BackendListener GetInstance()
        {
            if(backListener == null)
            {
                backListener = new BackendListener();
            }
            return backListener;
        }

        public void Start()
        {

            logger.Debug("[ChatServer][FrontListener][Start()] start");
            MainProcess();

            logger.Debug("[ChatServer][FrontListener][Start()] end");
            Console.ReadKey();
        }

        /// <summary>
        /// Stop Thread
        /// </summary>
        public void Stop()
        {
            threadState = Constants.ThreadStop;
        }

        public void MainProcess()
        {
            logger.Debug("[ChatServer][HandleRequest()] start");
            Task<Socket> getAcceptTask = null;

            while (threadState)
            {
                //GetClientRequest(); // isCOmplete를 사용하자!!!! 메모리터진다지금 
                // Accept Client Connection Request 
                if (getAcceptTask != null)
                {
                    if (getAcceptTask.IsCompleted)
                    {
                        logger.Debug("[ChatServer][HandleRequest()] complete accept task. Restart");
                        // 다시 task run 
                        getAcceptTask = Task.Factory.FromAsync(sockListener.BeginAccept, sockListener.EndAccept, true);
                    }
                }
                else
                {
                    logger.Debug("[ChatServer][HandleRequest()] start accept task ");
                    getAcceptTask = Task.Factory.FromAsync(sockListener.BeginAccept, sockListener.EndAccept, true);
                    //getAcceptTask.Start();
                }

            }// end loop 
        }// end method 

        // 요청을 읽고, 작업을 처리하는 비동기 작업을 만들어야함!!!
        // 여기에서 case나눠서 처리 !!!!
        public void HandleRequest(Socket peer)
        {
            if (peer != null && peer.Connected)
            {
                // 정상 연결상태 
                //CCHeader header = (CCHeader)ReadAsync(peer, 8, typeof(CCHeader));
                CBHeader header = (CBHeader)NetworkManager.ReadAsync(peer, 8, typeof(CBHeader));

                switch (header.Type)
                {
                    // Read Auth Info From Login Server
                    case MessageType.Auth:

                        // async read body 
                        LCNotifyUserRequestBody body = (LCNotifyUserRequestBody) NetworkManager.ReadAsync(peer, header.BodyLength, typeof(LCNotifyUserRequestBody));

                        // RPC to Front Thread
                        // add user auth info to structure
                        string authInfo = new string(body.UserInfo.Id) + ":" + body.Cookie;

                        var factory = new ConnectionFactory() { HostName = "localhost" };
                        using (var connection = factory.CreateConnection())
                        using (var channel = connection.CreateModel())
                        {
                            channel.QueueDeclare(queue: "hello",
                                                 durable: false,
                                                 exclusive: false,
                                                 autoDelete: false,
                                                 arguments: null);

                            var bytes = Encoding.UTF8.GetBytes(authInfo);

                            channel.BasicPublish(exchange: "",
                                                 routingKey: "hello",
                                                 basicProperties: null,
                                                 body: bytes);
                        }

                break;

                    // 300 : Membership 
                    case MessageType.Signup:
                        break;
                    case MessageType.Signin:
                        break;
                    case MessageType.Modify:
                        break;
                    case MessageType.Delete:
                        break;


                    // 500: Admin tool
                    case MessageType.RestartApp:

                        break;

                    case MessageType.ShutdownApp:

                        break;
                    case MessageType.StartApp:

                        break;

                    // default
                    default:
                        break;
                }

            }
            else
            {

            }
        }
    }
}
