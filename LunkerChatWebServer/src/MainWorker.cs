using log4net;
using LunkerChatWebServer.src.utils;
using LunkerLibrary.common.protocol;
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

        private Task<Socket> clientAcceptTask = null;
        private Task<Socket> feAcceptTask = null;

        private Socket clientListener = null;
        private Socket feListener = null;

        private Socket beSocket = null;
        private Socket loginSocket = null;

        private TcpListener clientTCPListener = null;
        private TcpListener server = null;

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
                /*
                await Task.WhenAll(BindClientSocketListenerAsync(), ConnectBESocketAsync(), ConnectLoginSocketAsync()).ContinueWith((parent) => {

                    Console.WriteLine("complete"); });
                    */

                server = new TcpListener(IPAddress.Parse("127.0.0.1"), 9999);

                server.Start();
                //server.AcceptSocketAsync();
                //BindClientSocketListenerAsync().ContinueWith((parent)=> { MainProcess(); });
                MainProcess();
                Console.WriteLine("complete222");
            } 
            catch (Exception e)
            {
                // error
                Console.WriteLine("error");
            }
            // request initial FE Info
           

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
        public Task BindClientSocketListenerAsync()
        {
            return Task.Run(() => {
                logger.Debug("[ChatServer][BindClientSocketListenerAsync()] start");
                // initialiize client socket listener
                //IPEndPoint ep = new IPEndPoint(IPAddress.Parse("10.100.58.3"), 80);
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, 80);


                clientListener = new Socket(SocketType.Stream, ProtocolType.IP);
                clientListener.Bind(ep);
                clientListener.Listen(1000);
                logger.Debug("[ChatServer][BindClientSocketListenerAsync()] end");
            });
        }

        public Task ConnectBESocketAsync()
        {

            return Task.Run(() => {
                logger.Debug("[ChatServer][ConnectBESocketAsync()] start");
                beSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                //IPEndPoint ep = new IPEndPoint(IPAddress.Parse(AppConfig.GetInstance().BackendServerIp), AppConfig.GetInstance().BackendServerPort);
                IPEndPoint ep = new IPEndPoint(Dns.GetHostEntry(Constants.BeServer).AddressList[0], AppConfig.GetInstance().BackendServerPort);

                beSocket.Connect(ep);
                logger.Debug("[ChatServer][ConnectBESocketAsync()] end");
            });
        }

        public Task ConnectLoginSocketAsync()
        {
            return Task.Run(() => {
                logger.Debug("[ChatServer][ConnectLoginSocketAsync()] start");
                loginSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                ;
                IPEndPoint ep = new IPEndPoint(Dns.GetHostEntry(Constants.LoginServer).AddressList[0], AppConfig.GetInstance().LoginServerPort);

                loginSocket.Connect(ep);
                logger.Debug("[ChatServer][ConnectLoginSocketAsync()] end");
            });
        }


        public void MainProcess()
        {
            logger.Debug("[ChatServer][HandleRequest()] start");

            while (threadState)
            {
                // Accept Client Connection Request 
                HandleClientAcceptAsync();

                // 접속한 client가 있을 경우에만 수행.
                if (0 != connectionManager.GetClientConnectionCount())
                {
                    // select client connection
                    readSocketList = connectionManager.GetClientConnectionDic().Values.ToList();
                    // select login connection
                    readSocketList.Add(loginSocket);
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
                }// end if
            }// end loop 
        }// end method

        public async void HandleClientAcceptAsync()
        {


            if (clientAcceptTask != null)
            {
                if ( clientAcceptTask.IsCompleted)
                {
                    logger.Debug("[ChatServer][HandleRequest()] complete accept task. Restart");

                    // 나중에 auth 인증을 거친 후 해당 사용자의 connection을 저장시킨다.  
                    Console.WriteLine("whow~!~!~!~!~!~!~!~!");
                    // 다시 task run 
                    //Task.Factory.FromAsync(clientListener.BeginAccept, clientListener.EndAccept, true);

                    //getAcceptTask = Task.Factory.FromAsync(clientListener.BeginAccept, clientListener.EndAccept, true);
                    /*
                    Socket tmp = await (clientAcceptTask = Task.Run(() =>
                    {
                        return clientListener.Accept();
                    }));
                    */

                    clientAcceptTask = server.AcceptSocketAsync();
                }
            }
            else
            {
                logger.Debug("[ChatServer][HandleRequest()] start accept task ");
                //clientAcceptTask = Task.Factory.FromAsync(clientListener.BeginAccept, clientListener.EndAccept, true);
                /*
                Socket tmp = await (clientAcceptTask = Task.Run(() =>
                {
                    return clientListener.Accept();
                }));
                */
                clientAcceptTask = server.AcceptSocketAsync();
            }
        }

        public async void HandleRequest(Socket peer)
        {
            if (peer != null && peer.Connected)
            {
                // 정상 연결상태 
                // 일단 CCHeader로 전체 header 사용 
                try
                {
                    CommonHeader header = (CommonHeader)await NetworkManager.ReadAsync(peer, Constants.HeaderSize, typeof(CommonHeader));
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

            }
            else
            {
                // clear connection infos 
                // delete socket in connection list 

                IPEndPoint endPoint = (IPEndPoint)peer.RemoteEndPoint;
                string ip = endPoint.Address.ToString();
                int port = endPoint.Port;
                string key = ip + ":" + port;

                UserInfo userInfo = connectionManager.GetClientInfo(key);

                if (!userInfo.Equals(default(UserInfo)))
                {
                    connectionManager.LogoutClient(key);

                }

            }
        }// end method

        public void HandleConnectionSetupAsync()
        {

        }


    }
}
