﻿using log4net;

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
using LunkerLibrary.common.Utils;
using LunkerLibrary.common.protocol;

namespace LunkerChatServer
{
    /**
     * Socket Listener for Front Component - client 
     */
    class FrontListener
    {
        private delegate void RequestHandler(int bodyLength); // message type에 따라 해당되는 함수를 찾아서, delegate를 통해 호출한다! 

        private ILog logger = Logger.GetLoggerInstance();

        private static FrontListener frontListener = null;
        private bool threadState = Constants.ThreadRun;
        private Socket sockListener = null;

        private ConnectionManager connectionManager = null;
        private List<Socket> readSocketList = null;
        private List<Socket> writeSocketList = null;
        private List<Socket> errorSocketList = null;


        private FrontListener()
        {

        }

        public static FrontListener GetInstance()
        {
            if (frontListener == null)
            {
                frontListener = new FrontListener();
            }
            return frontListener;
        }

        // chat server main thread
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

        // 요청을 읽고, 작업을 처리하는 비동기 작업을 만들어야함!!!
        // 여기에서 case나눠서 처리 !!!!
        public void HandleRequest(Socket peer)
        {
            if(peer!=null && peer.Connected)
            {
                // 정상 연결상태 
                CCHeader header = (CCHeader) NetworkManager.ReadAsync(peer, 8, typeof(CCHeader));
 
                switch (header.Type)
                {
                    // 200: chatting 
                    case MessageType.Chatting:
                        break;

                    // room : 400 
                    case MessageType.CreateRoom:

                        break;

                    case MessageType.JoinRoom:
                        break;

                    case MessageType.LeaveRoom:
                        break;

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

        public void AcceptCallback(IAsyncResult ar)
        {
            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            IPEndPoint ep = (IPEndPoint)handler.RemoteEndPoint;
            StringBuilder idBuilder = new StringBuilder();
            idBuilder.Append(ep.Address);
            idBuilder.Append(":");
            idBuilder.Append(ep.Port);

            //clientSocketDic.Add(idBuilder.ToString(), handler);
            //connectionManager.Add
            //logger.Debug("[ChatServer][AcceptAsync()][AcceptCallback()] aceept client connect request");
            return;
        }// end method
    

        public void GenerateHeader(Task parent)
        {

        }

        public Task<CCHeader> ReadAsyncTask(Socket peer)
        {
            logger.Debug("[ChatServer][ReadAsyncTask()] start");
            int headerLength = 8;

            byte[] buff = new byte[headerLength];

            try
            {
                Task readTask = Task.Factory.FromAsync(peer.BeginReceive(buff, 0, buff.Length, SocketFlags.None, null, peer), peer.EndReceive);
                Task convertTask = null;

                //readTask.ContinueWith(GenerateHeader);
                readTask.ContinueWith((parent) =>
                {
                    string getMsg = Encoding.UTF8.GetString(buff);
                    Console.WriteLine(getMsg);
                });

                readTask.Wait();
            }
            catch (SocketException se)
            {
                Console.WriteLine("disconnected");

                IPEndPoint ep = (IPEndPoint)peer.RemoteEndPoint;
                string key = ep.Address + ":" + ep.Port;

                connectionManager.DeleteClientConnection();
                //clientSocketDic.Remove(key);
                peer.Close();
            }

            //return readTask;
            logger.Debug("[ChatServer][ReadAsyncTask()] end");
            //return;
            return null;
        }

        public void SendAsync()
        {

        }

    }
}
