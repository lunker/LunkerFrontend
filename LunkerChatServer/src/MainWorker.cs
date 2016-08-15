using log4net;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using LunkerChatServer.src.utils;
using LunkerLibrary.common.protocol;
using LunkerLibrary.common.Utils;
using LunkerChatServer.src.workers;

namespace LunkerChatServer
{
    /**
     * Socket Listener for Front Component - client 
     */
    class MainWorker
    {
        private delegate void RequestHandler(int bodyLength); // message type에 따라 해당되는 함수를 찾아서, delegate를 통해 호출한다! 

        private ILog logger = Logger.GetLoggerInstance();

        private static MainWorker mainWorker = null;
        private bool threadState = Constants.ThreadRun;

        private Socket sockListener = null;

        private ConnectionManager connectionManager = null;
        private List<Socket> readSocketList = null;
        private List<Socket> writeSocketList = null;
        private List<Socket> errorSocketList = null;

        private Socket beConnection = null;
        Task<Socket> getAcceptTask = null;

        //private ChatWorker chatWorker;

        private MainWorker()
        {

        }

        public static MainWorker GetInstance()
        {
            if (mainWorker == null)
            {
                mainWorker = new MainWorker();
            }
            return mainWorker;
        }

        // chat server main thread
        public void Start()
        {

            logger.Debug("[ChatServer][MainWorker][Start()] start");
            Initialize();
            InitializeBEConnection();

            MainProcess();

            logger.Debug("[ChatServer][MainWorker][Start()] end");
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
           

            while (threadState)
            {
                // Accept Client Connection Request 
                HandleAccept();

                // 접속한 client가 있을 경우에만 수행.
                if (0 != connectionManager.GetClientConnectionCount())
                {
                    readSocketList = connectionManager.GetClientConnectionDic().Values.ToList();
                    //writeSocketList = clientSocketDic.Values.ToList();
                    //errorSocketList = clientSocketDic.Values.ToList();

                    // Check Inputs 
                    Socket.Select(readSocketList, writeSocketList, errorSocketList, 0);

                    // Request가 들어왔을 경우 
                    if (readSocketList.Count != 0)
                    {
                        foreach (Socket peer in readSocketList)
                        {
                            HandleRequest(peer);
                        }
                    }
                }
              
            }// end loop 
        }

        public void HandleAccept()
        {
            if (getAcceptTask != null)
            {
                if (getAcceptTask.IsCompleted)
                {
                    logger.Debug("[ChatServer][HandleRequest()] complete accept task. Restart");

                    // Add accepted connections
                    // getAcceptTask.Result;
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
        }

        // 요청을 읽고, 작업을 처리하는 비동기 작업을 만들어야함!!!
        // 여기에서 case나눠서 처리 !!!!
        public void HandleRequest(Socket peer)
        {
            if(peer!=null && peer.Connected)
            {
                // 정상 연결상태 
                // 일단 CCHeader로 전체 header 사용 
                CCHeader header = (CCHeader) NetworkManager.ReadAsync(peer, 8, typeof(CCHeader));
 
                switch (header.Type)
                {
                    // 200: chatting 
                    case MessageType.Chatting:

                        break;
                    // room : 400 
                    case MessageType.CreateRoom:
                        if(header.State == MessageState.Request)
                        {
                            // send create request
                            ChatWorker.HandleCreateRoomRequest();
                            
                            break;
                        }
                        else
                        {
                            //connectionManager.GetClientConnection();
                            ChatWorker.HandleCreateRoomResponse();
                            
                            break;
                        }

                    case MessageType.JoinRoom:
                        if (header.State == MessageState.Request)
                        {

                            // send create request
                            // read 
                            //beConnection.Send
                            HandleCreateRoomRequest(peer);
                            break;
                        }
                        else
                        {
                            break;
                        }

                    case MessageType.LeaveRoom:
                        if (header.State == MessageState.Request)
                        {

                            // send create request
                            // read 
                            //beConnection.Send
                            HandleCreateRoomRequest(peer);
                            break;
                        }
                        else
                        {
                            break;
                        }

                    case MessageType.ListRoom:
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

        public void Initialize()
        {
            connectionManager = ConnectionManager.GetInstance();

            readSocketList = new List<Socket>();
            writeSocketList = new List<Socket>();
            errorSocketList = new List<Socket>();

            // initialiize client socket listener
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, AppConfig.GetInstance().FrontPort);

            sockListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sockListener.Bind(ep);
            sockListener.Listen(AppConfig.GetInstance().Backlog);

            // Set MessageQueue
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "hello",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);


                    string id = message.Split(':')[0];
                    Cookie cookie = new Cookie(message.Split(':')[1]);

                    //authInfo.Add(id,cookie);
                    connectionManager.AddAuthInfo(id, cookie);

                    //Console.WriteLine(" [x] Received {0}", message);
                };
                channel.BasicConsume(queue: "hello",
                                     noAck: true,
                                     consumer: consumer);

            }
        }// end method 

        public void InitializeBEConnection()
        {
            beConnection = new Socket(SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(AppConfig.GetInstance().Backendserverip), AppConfig.GetInstance().Backendserverport);

            beConnection.Connect(ep);

        }

        public async void GetClientRequest()
        {
            //logger.Debug("[ChatServer][GetClientRequest()] start");
            Socket handler = await AcceptAsync();
            logger.Debug("[ChatServer][GetClientRequest()] get client conenction ");

            IPEndPoint ep = (IPEndPoint)handler.RemoteEndPoint;
            StringBuilder idBuilder = new StringBuilder();
            idBuilder.Append(ep.Address);
            idBuilder.Append(":");
            idBuilder.Append(ep.Port);

            //clientSocketDic.Add(idBuilder.ToString(), handler);

            logger.Debug("[ChatServer][AcceptAsync()] add handler to list");
            return;
        }

        public async Task<Socket> AcceptAsync()
        {

            //logger.Debug("[ChatServer][GetClientRequest()][AcceptAsync()] start accept");
            Socket handler = await Task.Factory.FromAsync(sockListener.BeginAccept, sockListener.EndAccept, true);

            logger.Debug("[ChatServer][GetClientRequest()][AcceptAsync()] accept client request");
            return handler;
            /*
            Task delay = Task.Delay(TimeSpan.FromSeconds(5));
            var result = await Task.WhenAny(delay, Task.Factory.FromAsync(sockListener.BeginAccept, sockListener.EndAccept, true)).ConfigureAwait(false);

       
            if (result == delay)
            {
                // timeout
                return null;
            }
            else
            {
                return (Task)result.;
            }
            */
            /*
            Socket handler = await Task.Factory.FromAsync(sockListener.BeginAccept, sockListener.EndAccept, true);

            logger.Debug("[ChatServer][GetClientRequest()][AcceptAsync()] accept client request");
            return handler;
            */
            /*
            IPEndPoint ep = (IPEndPoint)handler.RemoteEndPoint;
            StringBuilder idBuilder = new StringBuilder();
            idBuilder.Append(ep.Address);
            idBuilder.Append(":");
            idBuilder.Append(ep.Port);

            clientSocketDic.Add(idBuilder.ToString(), handler);

            logger.Debug("[ChatServer][AcceptAsync()] accept client request");
            return;
            */

        }


        public void HandleCreateRoomRequest(Socket peer)
        {
            // 1) request to be 
            // 2) read response 

            // 3) 
        }

    }
}
