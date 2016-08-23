using log4net;
using LunkerChatWebServer.src.utils;
using LunkerLibrary.common.protocol;
using LunkerLibrary.common.utils;
using LunkerLibrary.common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;

namespace LunkerChatWebServer.src
{
    class MainWorker
    {

        private static MainWorker instance = null;
        private ILog logger = Logger.GetLoggerInstance();
        private bool threadState = Constants.AppRun;

        private List<Socket> clientConnection = null;

        private ConnectionManager connectionManager = null;

        private Dictionary<ServerInfo, Socket> feConnectionDic = null;

        private List<Socket> readSocketList = null;
        private List<Socket> writeSocketList = null;
        private List<Socket> errorSocketList = null;

        private Task<HttpListenerContext> clientAcceptTask = null;
        private Task<Socket> feAcceptTask = null;

        private Socket feListener = null;

        private Socket beServerSocket = null;
        private Socket loginServerSocket = null;


        private HttpListener server = null;

        private MainWorker() { }
        public static MainWorker GetInstance()
        {
            if (instance == null)
            {
                instance = new MainWorker();
            }
            return instance;
        }

        public async void Start()
        {
            logger.Debug("[ChatServer][MainWorker][Start()] start");

            Initialize();
            try
            {
                server = new HttpListener();
                server.Prefixes.Add("http://+:80/");
                server.Start();

                MainProcess();
                //Console.WriteLine("complete222");
            } 
            catch(ArgumentException ae)
            {
                Console.WriteLine("ae error");
            }
            catch(HttpListenerException hle)
            {
                Console.WriteLine("hle error");
            }
            catch (Exception e)
            {
                // error
                Console.WriteLine("error");
            }


            logger.Debug("[ChatServer][MainWorker][Start()] end");
        }

        /// <summary>
        /// Stop Thread
        /// </summary>
        public void Stop()
        {
            threadState = Constants.ThreadStop;
        }

        public void Initialize()
        {
            connectionManager = ConnectionManager.GetInstance();

            readSocketList = new List<Socket>();
            writeSocketList = new List<Socket>();
            errorSocketList = new List<Socket>();
        }// end method 

        /// <summary>
        /// Listen 80 port 
        /// </summary>
        /// <returns></returns>
       

        public Task HandleLoginServerConnectAsync()
        {
            return Task.Run(() => {

                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(AppConfig.GetInstance().LoginServerIp), AppConfig.GetInstance().LoginServerPort);

                while (true)
                {
                    try
                    {
                        if (loginServerSocket != null)
                        {
                            if (!loginServerSocket.Connected)
                            {
                                Console.WriteLine("[ChatServer][MainProcess()] 설마....");


                                loginServerSocket.Connect(ep);
                                Console.WriteLine("[ChatServer][MainProcess()] Connect login Server");
                                logger.Debug("[ChatServer][MainProcess()] Connect login Server");

                                //socketTaskPair.Add(loginServerSocket, Task.Run(() => { }));
                            }
                        }
                    }
                    catch (InvalidOperationException ioe)
                    {
                        loginServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        Console.WriteLine("[ChatServer][MainProcess()] Disconnected . . . login Server . . . retry");
                        continue;
                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine("[ChatServer][MainProcess()] Disconnected . . . login Server . . . retry");
                        loginServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        continue;
                    }
                }
            });
        }

        public Task HandleBeServerConnectAsync()
        {
            return Task.Run(() => {

                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(AppConfig.GetInstance().BackendServerIp), AppConfig.GetInstance().BackendServerPort);

                while (true)
                {
                    //Task.Delay(500);
                    try
                    {
                        if (beServerSocket != null)
                        {
                            if (!beServerSocket.Connected)
                            {
                                beServerSocket.Connect(ep);
                                Console.WriteLine("[ChatServer][MainProcess()] Connect Backend Server");
                                logger.Debug("[ChatServer][MainProcess()] Connect Backend Server");
                            }
                        }
                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine("[ChatServer][MainProcess()] Reconnect . . . Backend Server . . .");
                        beServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        continue;
                    }
                }
            });
        }

        public void HandleClientAcceptAsync()
        {
            /*
            return Task.Run(() => {
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, 80);

                while (true)
                {
                    try
                    {

                        HttpListenerContext context = await server.GetContextAsync();
                        context.AcceptWebSocketAsync();

                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine("loginserver : client listener disconnected!!");
                        clientListener.Close();
                        continue;
                    }
                }
            });
            */
        }

        public void MainProcess()
        {
            logger.Debug("[ChatServer][MainProcess()] start");
            Console.WriteLine("[ChatServer][MainProcess()] start");

            HandleLoginServerConnectAsync();

            HandleBeServerConnectAsync();

            HandleClientAcceptAsync();

            while (true)
            {

               //
            }
        }// end method

        public async void HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            //logger.Debug("[ChatServer][HandleRequest()] start accept task ");
            Console.WriteLine("[ChatServer][HandleRequest()] start");
            // 정상 연결상태 
            // 일단 CCHeader로 전체 header 사용 
            try
            {
                //CommonHeader header = (CommonHeader)await NetworkManager.ReadAsync(peer, Constants.HeaderSize, typeof(CommonHeader));

                CommonHeader header = (CommonHeader) await WebNetworkManager.ReadAsync(request, Constants.HeaderSize, typeof(CommonHeader));

                switch (header.Type)
                {
                    // Login Server
                    // request from login server
                    // ok 
                    case MessageType.ConnectionSetup:
                        // 인증된 유저가 들어와야 
                        // connectionDic에 저장된다. 
                        //await HandleConnectionSetupAsync(peer, header);
                        break;

                    // 200: chatting 
                    // ok 
                    case MessageType.Chatting:
                        //await HandleChattingRequestAsync(peer, header);
                        break;

                    // room : 400 
                    // ok 
                    case MessageType.CreateRoom:
                        //await HandleCreateRoomAsync(peer, header);
                        break;

                    case MessageType.JoinRoom:
                        break;

                    case MessageType.LeaveRoom:
                        break;

                    // not yet 
                    case MessageType.ListRoom:
                        break;

                    // default
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                // errorr
            }
            
        }// end method
    }
}
