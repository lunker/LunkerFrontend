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
    public class MainWorker
    {
        private delegate void RequestHandler(int bodyLength); // message type에 따라 해당되는 함수를 찾아서, delegate를 통해 호출한다! 

        private ILog logger = Logger.GetLoggerInstance();

        private static MainWorker mainWorker = null;
        private bool threadState = Constants.ThreadRun;

        private Socket clientListener = null;

        private ConnectionManager connectionManager = null;

        private List<Socket> readSocketList = null;
        private List<Socket> writeSocketList = null;
        private List<Socket> errorSocketList = null;

        private Socket beSocket = null;
        private Socket loginSocket = null;

        private Task<Socket> clientAcceptTask = null;

        //private ChatWorker chatWorker;
        private BEWorker beWorker = BEWorker.GetInstance();

        private MainWorker(){ }

        public static MainWorker GetInstance()
        {
            if (mainWorker == null)
            {
                mainWorker = new MainWorker();
            }
            return mainWorker;
        }

        // chat server main thread
        public async void Start()
        {
            logger.Debug("[ChatServer][MainWorker][Start()] start");
            Initialize();
            try
            {
                await Task.WhenAll(BindClientSocketListenerAsync(), ConnectBESocketAsync(), ConnectLoginSocketAsync());

            }
            catch (Exception e)
            {
                // error

            }
 
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
        }

        public void HandleClientAcceptAsync()
        {

            if (clientAcceptTask != null)
            {
                if (clientAcceptTask.IsCompleted)
                {
                    logger.Debug("[ChatServer][HandleRequest()] complete accept task. Restart");
                    
                    // 나중에 auth 인증을 거친 후 해당 사용자의 connection을 저장시킨다.  

                    // 다시 task run 
                    //getAcceptTask = Task.Factory.FromAsync(clientListener.BeginAccept, clientListener.EndAccept, true);
                    clientAcceptTask = Task.Run(() => {
                        return clientListener.Accept();
                    });
                }
            }
            else
            {
                logger.Debug("[ChatServer][HandleRequest()] start accept task ");
                //clientAcceptTask = Task.Factory.FromAsync(clientListener.BeginAccept, clientListener.EndAccept, true);
                clientAcceptTask = Task.Run(() => {
                    return clientListener.Accept();
                });
            }
        }

        // 요청을 읽고, 작업을 처리하는 비동기 작업을 만들어야함!!!
        // 여기에서 case나눠서 처리 !!!!
        public async void HandleRequest(Socket peer)
        {
            if(peer!=null && peer.Connected)
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
                            await HandleConnectionSetupAsync(peer, header);
                            break;

                        // 200: chatting 
                        // ok 
                        case MessageType.Chatting:
                            await HandleChattingRequestAsync(peer, header);
                            break;

                        // room : 400 
                        // ok 
                        case MessageType.CreateRoom:
                            await HandleCreateRoomAsync(peer, header);
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

                IPEndPoint endPoint = (IPEndPoint) peer.RemoteEndPoint;
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

        /// <summary>
        /// <para>Initialize variable </para>
        /// <para></para>
        /// <para></para>
        /// </summary>
        public void Initialize()
        {
            connectionManager = ConnectionManager.GetInstance();

            readSocketList = new List<Socket>();
            writeSocketList = new List<Socket>();
            errorSocketList = new List<Socket>();

        }// end method 

        public Task BindClientSocketListenerAsync()
        {
            return Task.Run(()=> {
                // initialiize client socket listener
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, AppConfig.GetInstance().FrontPort);

                clientListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
                clientListener.Bind(ep);
                clientListener.Listen(AppConfig.GetInstance().Backlog);
            });
        }

        public Task ConnectBESocketAsync()
        {
            return Task.Run(()=> {
                beSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                IPEndPoint ep = new IPEndPoint(Dns.GetHostEntry(Constants.BeServer).AddressList[0], AppConfig.GetInstance().BackendServerPort);

                beSocket.Connect(ep);
            });
        }

        public Task ConnectLoginSocketAsync()
        {
            return Task.Run(()=> {
                loginSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                IPEndPoint ep = new IPEndPoint(Dns.GetHostEntry(Constants.LoginServer).AddressList[0], AppConfig.GetInstance().LoginServerPort);

                loginSocket.Connect(ep);
            });
        }

        /// <summary>
        /// <para>1) read body from login server</para>
        /// <para>2) save auth info in structure</para>
        /// <para></para>
        /// <para></para>
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="header"></param>
        public async void HandleConnectionSetup(Socket peer, CommonHeader header)
        {
            // 1)
            LCUserAuthRequestBody requestBody = (LCUserAuthRequestBody) await NetworkManager.ReadAsync(peer, header.BodyLength, typeof(LCUserAuthRequestBody));
            
            // 2) 
            connectionManager.AddAuthInfo(new string(requestBody.UserInfo.Id), requestBody.Cookie);
        }

        public Task HandleConnectionSetupAsync(Socket peer, CommonHeader header)
        {
            return Task.Run(()=>HandleConnectionSetup(peer, header));
        }

        /// <summary>
        /// handle chatting request from client 
        /// be에 보내는 logic 추가해야함 
        /// </summary>
        /// <param name="peer"></param>
        public async void HandleChattingRequest(Socket peer, CommonHeader header)
        {
            byte[] messageBuff = new byte[header.BodyLength];
            messageBuff = await NetworkManager.ReadAsync(peer, header.BodyLength);
            // read message

            // Get User Entered Room 
            ChattingRoom enteredRoom = connectionManager.GetChattingRoomJoinInfo(new string(header.UserInfo.Id)); // room info ~ user id 

            // broadcast
            Socket client = null;
            foreach (string user in connectionManager.GetChattingRoomListInfoKey(enteredRoom))
            {
                client = connectionManager.GetClientConnection(user);

                // broadcast to each client
                await NetworkManager.SendAsync(client, messageBuff);
            }

            // Send chatting to BE 
            //string sendingUser = new string(header.UserInfo.Id);
            CommonHeader responseHeader = new CommonHeader(MessageType.Chatting, MessageState.Request, Constants.None, new Cookie(), header.UserInfo);
            await NetworkManager.SendAsync(beSocket, responseHeader);
            // worker에게 위임? 
            //beWorker.HandleChatting(header);
        }

        public Task HandleChattingRequestAsync(Socket peer, CommonHeader header)
        {
            return Task.Run(()=> HandleChattingRequest(peer, header));
        }

        /// <summary>
        /// <para>handle create chatting room request</para>
        /// <para>1) send request to BE</para>
        /// <para>2) read respoonse(header, body) from BE</para>
        /// <para>3) send response(header, body) to client</para>
        /// <para></para>
        /// </summary>
        /// <param name="peer"></param>
        public async void HandleCreateRoom(Socket peer, CommonHeader header)
        {
            // 1) send request to BE server
            //beWorker.HandleCreateRoomRequest(header);
            await NetworkManager.SendAsync(beSocket,header);

            // 2) read response(header, body) from BE
            CommonHeader responseHeader = (CommonHeader) await NetworkManager.ReadAsync(beSocket, Constants.HeaderSize, typeof(CommonHeader));
            CBCreateRoomResponseBody responseBody = (CBCreateRoomResponseBody) await NetworkManager.ReadAsync(beSocket, responseHeader.BodyLength, typeof(CBCreateRoomResponseBody));

            // 3) send response(header, body) to client
            await NetworkManager.SendAsync(peer, responseHeader);
            await NetworkManager.SendAsync(peer, responseBody);
        }

        public Task HandleCreateRoomAsync(Socket peer, CommonHeader header)
        {
            return Task.Run(() => HandleCreateRoom(peer, header));
        }

        /// <summary>
        /// <para>handle list chatting room request</para>
        /// <para>1) send request to be</para>
        /// <para>2) read response(header, body) from be</para>
        /// <para>3) send response(header, body) to client</para>
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="header"></param>
        public async void HandleListChattingRoom(Socket peer, CommonHeader header)
        {
            // 1) 
            await NetworkManager.SendAsync(beSocket, header);

            // 2) 
            CommonHeader responseHeader = (CommonHeader) await NetworkManager.ReadAsync(beSocket, Constants.HeaderSize, typeof(CommonHeader));

            //// 여기 에러 밭 ㅠㅠㅠㅠ 
            CBListRoomResponseBody responseBody = (CBListRoomResponseBody)await NetworkManager.ReadAsync(beSocket, responseHeader.BodyLength, typeof(CBListRoomResponseBody));
        }

        public Task HandleListChattingRoomAsync(Socket peer, CommonHeader header)
        {
            return Task.Run(()=>HandleChattingRequest(peer, header));
        }

        /*
        public void HandleCreateRoomRequest(Socket peer, CommonHeader header)
        {
            // send request to BE server
            beWorker.HandleCreateRoomRequest(header);
        }

        public Task HandleCreateRoomRequestAsync(Socket peer, CommonHeader header)
        {
            return Task.Run(()=> HandleCreateRoomRequest(peer,header));
        }

        // Message From BE Server
        public async void HandleCreateRoomResponse(Socket peer, CommonHeader header)
        {
            // read from be socket
            CBCreateRoomResponseBody body = (CBCreateRoomResponseBody) NetworkManager.ReadAsync(peer, header.BodyLength, typeof(CBCreateRoomResponseBody)); // Get ResponseBody

            // get requested client socket 
            Socket client = connectionManager.GetClientConnection(new string(header.UserInfo.Id));

            // Send Response To Client 
            // send header
            Task sendHeaderTask = NetworkManager.SendAsyncTask(client, header);
            // send body
            await sendHeaderTask.ContinueWith((parent)=> 
            {
                 NetworkManager.SendAsyncTask(client, body);
            });
        }
        public Task HandleCreateRoomResponseAsync(Socket peer, CommonHeader header)
        {
            return Task.Run( ()=> HandleCreateRoomResponse(peer, header) );
        }
        */




    }
}
