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
                        // check cookie available
                        // ok 
                        
                        case MessageType.CreateRoom:
                            await HandleCreateRoomAsync(peer, header);
                            break;

                            // ok 
                        case MessageType.JoinRoom:
                            await HandleJoinRoomAsync(peer, header);
                            break;

                            // ok
                        case MessageType.LeaveRoom:
                            await HandleLeaveRoomAsync(peer, header);
                            break;

                            // ok
                        // not yet 
                        case MessageType.ListRoom:
                            await HandleListChattingRoomAsync(peer, header);
                            break;
                            // ok
                        // default
                        default:
                            //await HandleErrorAsync();
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
        /// <para>handle chatting request from client </para>
        /// <para>be에 보내는 logic 추가해야함 </para>
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



        public Task RequestCookieVerifyAsync()
        {
            return Task.Run(()=> {
                CommonHeader requestHeader = new CommonHeader(MessageType.VerifyCookie, MessageState.Request, Constants.None, new Cookie(), header.UserInfo);
                return NetworkManager.SendAsync(beSocket, requestHeader);
            });
        }


        /// <summary>
        /// <para></para>
        /// <para>1) read response</para>
        /// <para></para>
        /// <para></para>
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="header"></param>
        public async void HandleCookieVerify(Socket peer, CommonHeader header)
        {
            // 1) 
            CommonHeader requestHeader = new CommonHeader(MessageType.VerifyCookie, MessageState.Request, Constants.None, new Cookie(), header.UserInfo);
            await NetworkManager.SendAsync(beSocket, requestHeader);   
        }

        public Task HandleCookieVerifyAsync( CommonHeader header)
        {
            return Task.Run(()=> { HandleCookieVerify(header); });
        }

        /// <summary>
        /// <para>handle create chatting room request</para>
        /// <para>1) send request to BE</para>
        /// <para>2) read respoonse(header, body) from BE</para>
        /// <para>3) send response(header, body) to client</para>
        /// <para>4) save data </para>
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

            // 4) 
            connectionManager.AddChattingRoomListInfoKey(responseBody.ChattingRoom);
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
            byte[] responseBody = await NetworkManager.ReadAsync(beSocket, responseHeader.BodyLength);

            // 3) 

            await NetworkManager.SendAsync(peer,responseHeader);
            await NetworkManager.SendAsync(peer, responseBody);
        }

        public Task HandleListChattingRoomAsync(Socket peer, CommonHeader header)
        {
            return Task.Run(()=> HandleListChattingRoom(peer, header));
        }

        /// <summary>
        /// <para>handle join room request</para>
        /// <para>1) read request body from client</para>
        /// <para>2) send request to be</para>
        /// <para>3) read reseponse from be</para>
        /// <para>4) send response to client </para>
        /// <para>5) save data </para>
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="header"></param>
        public async void HandleJoinRoom(Socket peer, CommonHeader header)
        {
            ChattingRoom enteredRoom = default(ChattingRoom);
            string userId = default(string);

            //  1) 
            CCJoinRequestBody requestBody = (CCJoinRequestBody) await NetworkManager.ReadAsync(peer, header.BodyLength, typeof(CCJoinRequestBody));
            enteredRoom = requestBody.RoomInfo;
            userId = new string(header.UserInfo.Id);
            // 2) 
            await NetworkManager.SendAsync(beSocket, header);
            await NetworkManager.SendAsync(beSocket, requestBody);

            // 3) 
            CommonHeader responseHeader = (CommonHeader) await NetworkManager.ReadAsync(beSocket, Constants.HeaderSize, typeof(CommonHeader));

            
            if (header.State == MessageState.Fail)
            {
                // fail - room is on the other 
                // read body
                CBJoinRoomResponseBody responseBody = (CBJoinRoomResponseBody) await NetworkManager.ReadAsync(beSocket, responseHeader.BodyLength, typeof(CBJoinRoomResponseBody));

                await NetworkManager.SendAsync(peer, responseHeader);
                await NetworkManager.SendAsync(peer, responseBody);
            }
            else if(header.State == MessageState.Success)
            {
                // success 
                // send header to client
                await NetworkManager.SendAsync(peer, responseHeader);
                // add user info to data structure 
                connectionManager.AddChattingRoomListInfoValue(enteredRoom, userId);
            }
            else
            {
                // error
                await NetworkManager.SendAsync(peer, responseHeader);

            }
        }

        public Task HandleJoinRoomAsync(Socket peer, CommonHeader header)
        {
            return Task.Run(() => {
                HandleJoinRoom(peer, header);
            });
        }

        /// <summary>
        /// <para>1) read body from client</para>
        /// <para>2) send request to be</para>
        /// <para>3) read body from be</para>
        /// <para>4) send response to client </para>
        /// <para>5) save data</para>
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="header"></param>
        public async void HandleLeaveRoom(Socket peer, CommonHeader header)
        {
            ChattingRoom enteredRoom = default(ChattingRoom);
            string userId = new string(header.UserInfo.Id);
            // 1) 
            CCLeaveRequestBody requestBody = (CCLeaveRequestBody) await NetworkManager.ReadAsync(peer, header.BodyLength, typeof(CCLeaveRequestBody));
            enteredRoom = requestBody.RoomInfo;

            // 2) send
            await NetworkManager.SendAsync(beSocket, header);
            await NetworkManager.SendAsync(beSocket, requestBody);

            // 3)
            CommonHeader responseHeader = (CommonHeader) await NetworkManager.ReadAsync(beSocket, Constants.HeaderSize, typeof(CommonHeader));

            // 4) 
            await NetworkManager.SendAsync(peer, responseHeader);

            // 5)
            connectionManager.DeleteChattingRoomListInfoValue(enteredRoom, userId);
        }

        public Task HandleLeaveRoomAsync(Socket peer, CommonHeader header)
        {
            return Task.Run(() => {
                HandleLeaveRoom(peer, header);
            });
        }
      
        public void HandleErrorAsync()
        {

        }



    }
}
