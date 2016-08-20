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

        private Socket beServerSocket = null;
        private Socket loginServerSocket = null;

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
                await Task.WhenAll(BindClientSocketListenerAsync());

                

            }
            catch (SocketException se)
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
            logger.Debug("[ChatServer][MainProcess()] start");

            while (threadState)
            {
                // Accept Client Connection Request 
                HandleClientAcceptAsync();

                //ConnectLoginServerAsync()
                Task.Run( async ()=> {
                    while (true)
                    {
                        Task.Delay(500);
                        try
                        {
                            if (loginServerSocket != null)
                            {
                                if (!loginServerSocket.Connected)
                                {
                                    IPEndPoint ep = new IPEndPoint(IPAddress.Parse(AppConfig.GetInstance().LoginServerIp), AppConfig.GetInstance().LoginServerPort);
                                    loginServerSocket.Connect(ep);

                                    // send Chatting Server Info to LoginServer
                                    // messagetype.FENOtice
                                    CommonHeader requestHeader = new CommonHeader(MessageType.FENotice, MessageState.Request, Constants.None, new Cookie(), new UserInfo());
                                    await NetworkManager.SendAsync(beServerSocket, requestHeader);

                                    logger.Debug("[ChatServer][MainProcess()] Connect Login Server");
                                }
                            }
                        }
                        catch (SocketException se)
                        {
                            continue;
                        }
                    }
                });

                // Connect BEServer 
                Task.Run( ()=> {
                    while (true)
                    {
                        Task.Delay(500);
                        try
                        {
                            if (beServerSocket != null)
                            {
                                if (!beServerSocket.Connected)
                                {
                                    IPEndPoint ep = new IPEndPoint(IPAddress.Parse(AppConfig.GetInstance().BackendServerIp), AppConfig.GetInstance().BackendServerPort);
                                    beServerSocket.Connect(ep);
                                    logger.Debug("[ChatServer][MainProcess()] Connect Backend Server");
                                }
                            }
                        }
                        catch (SocketException se)
                        {
                            continue;
                        }
                        
                    }
                });                

                // 접속한 client가 있을 경우에만 수행.
                // client의 요청 수행 
                if (0 != connectionManager.GetClientConnectionCount())
                {
                    // select CLIENT 
                    readSocketList = connectionManager.GetClientConnectionDic().Values.ToList();

                    // select LOGIN
                    // 사용자의 Auth정보를 받기 위함.
                    readSocketList.Add(loginServerSocket);
                   
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
        /// <summary>
        /// <para>Handle Client Connect Request</para>
        /// <para>나중에 auth 인증을 거친 후 해당 사용자의 connection을 저장시킨다.  </para>
        /// </summary>
        public void HandleClientAcceptAsync()
        {
            try
            {
                if (clientAcceptTask != null)
                {
                    if (clientAcceptTask.IsCompleted)
                    {
                        logger.Debug("[ChatServer][HandleRequest()] complete accept task. Restart");

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
            catch(SocketException se)
            {
                // clientListener error 
                // reconnect.
                return;
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
                    CommonHeader cookieVerifyHeader;
                    switch (header.Type)
                    {
                        case MessageType.FENotice:
                            // chat->login에게 fe의 정보를 보내준것에 대한 response
                            // 
                            // just Get Response about FE Notice 
                            break;
                        
                            // login -> chat
                            // notice user Auth Info 
                        case MessageType.NoticeUserAuth:
                            await HandleNoticeUserAuthAsync(peer, header);
                            break;
                        
                            // client -> chatting server. 
                            // send user auth info
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
                            cookieVerifyHeader = (CommonHeader) await HandleCookieVerifyAsync(header);
                            if(cookieVerifyHeader.State == MessageState.Success)
                            {
                                await HandleCreateRoomAsync(peer, header);
                            }
                            else
                            {
                                // error
                                // not authenticated user
                            }
                            break;

                            // ok 
                        case MessageType.JoinRoom:
                            cookieVerifyHeader = (CommonHeader)await HandleCookieVerifyAsync(header);
                            if (cookieVerifyHeader.State == MessageState.Success)
                            {
                                await HandleCreateRoomAsync(peer, header);
                            }
                            else
                            {
                                // error
                                // not authenticated user
                            }
                            await HandleJoinRoomAsync(peer, header);
                            break;

                            // ok
                        case MessageType.LeaveRoom:
                            await HandleCookieVerifyAsync(header);
                            await HandleLeaveRoomAsync(peer, header);
                            break;

                            // ok
                        // not yet 
                        case MessageType.ListRoom:
                            await HandleCookieVerifyAsync(header);
                            await HandleListChattingRoomAsync(peer, header);
                            break;
                            // ok
                        // default
                        default:
                            await HandleErrorAsync(peer, header);
                            break;
                    }
                }
                catch (SocketException se)
                {
                    // error
                    // Get rid of client 

                    connectionManager.


                    return;
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

            loginServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            beServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint ep = new IPEndPoint(IPAddress.Any, AppConfig.GetInstance().ClientListenPort);

            clientListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            clientListener.Bind(ep);
            clientListener.Listen(AppConfig.GetInstance().Backlog);

        }// end method 

        public Task BindClientSocketListenerAsync()
        {
            return Task.Run(()=> {
                // initialiize client socket listener
                
            });
        }



        public async void HandleNoticeUserAuth(Socket peer, CommonHeader header)
        {
            // 1)
            LCUserAuthRequestBody requestBody = (LCUserAuthRequestBody)await NetworkManager.ReadAsync(peer, header.BodyLength, typeof(LCUserAuthRequestBody));

            // 2) 
            connectionManager.AddAuthInfo(new string(requestBody.UserInfo.Id), requestBody.Cookie);
        }

        /// <summary>
        /// <para>접속할 유저의 정보를 받아온다.</para>
        /// <para>1) read body from login server</para>
        /// <para>2) save auth info in structure</para>
        /// <para></para>
        /// <para></para>
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="header"></param>
        public Task HandleNoticeUserAuthAsync(Socket peer, CommonHeader header)
        {
            return Task.Run(()=> HandleNoticeUserAuth(peer, header));
        }

        /// <summary>
        /// <oara>Authorize User Before Chatting</oara>
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public Task HandleConnectionSetupAsync(Socket peer, CommonHeader header)
        {
            return Task.Run(()=> {
                Cookie sentCookie = header.Cookie;
                UserInfo userInfo = header.UserInfo;

                Cookie authorizedCookie = connectionManager.GetAuthInfo(new string(userInfo.Id).Split('\0')[0]);

                // 인증실패
                if(sentCookie.Value != authorizedCookie.Value)
                {
                    CommonHeader responseHeader = new CommonHeader(MessageType.ConnectionSetup, MessageState.Fail, Constants.None, new Cookie(), userInfo);
                    NetworkManager.SendAsync(peer, responseHeader);
                }
                else
                {
                    // 인증성공
                    CommonHeader responseHeader = new CommonHeader(MessageType.ConnectionSetup, MessageState.Success, Constants.None, new Cookie(), userInfo);
                    NetworkManager.SendAsync(peer, responseHeader);
                }
            });
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
            await NetworkManager.SendAsync(beServerSocket, responseHeader);
            // worker에게 위임? 
            //beWorker.HandleChatting(header);
        }

        public Task HandleChattingRequestAsync(Socket peer, CommonHeader header)
        {
            return Task.Run(()=> HandleChattingRequest(peer, header));
        }

        /// <summary>
        /// <para></para>
        /// <para>1) read response</para>
        /// <para></para>
        /// <para></para>
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="header"></param>
        public Task SendCookieVerify(CommonHeader header)
        {  
            return Task.Run(()=> {

                CommonHeader requestHeader = new CommonHeader(MessageType.VerifyCookie, MessageState.Request, Constants.None, new Cookie(), header.UserInfo);
                NetworkManager.SendAsync(beServerSocket, requestHeader);
            });
        }

        public Task<Object> ResponseCookieVerify()
        {
            return  NetworkManager.ReadAsync(beServerSocket, Constants.HeaderSize, typeof(CommonHeader));
        }

        public async Task<CommonHeader> HandleCookieVerifyAsync(CommonHeader header)
        {
            //Task sendTask = SendCookieVerify(header);
            SendCookieVerify(header).ContinueWith((parent) =>
            {
               return NetworkManager.ReadAsync(beServerSocket, Constants.HeaderSize, typeof(CommonHeader));
            });

            return default(CommonHeader);
        }// end method 

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
            await NetworkManager.SendAsync(beServerSocket,header);

            // 2) read response(header, body) from BE
            CommonHeader responseHeader = (CommonHeader) await NetworkManager.ReadAsync(beServerSocket, Constants.HeaderSize, typeof(CommonHeader));
            CBCreateRoomResponseBody responseBody = (CBCreateRoomResponseBody) await NetworkManager.ReadAsync(beServerSocket, responseHeader.BodyLength, typeof(CBCreateRoomResponseBody));

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
            await NetworkManager.SendAsync(beServerSocket, header);

            // 2) 
            CommonHeader responseHeader = (CommonHeader) await NetworkManager.ReadAsync(beServerSocket, Constants.HeaderSize, typeof(CommonHeader));
            byte[] responseBody = await NetworkManager.ReadAsync(beServerSocket, responseHeader.BodyLength);

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
            await NetworkManager.SendAsync(beServerSocket, header);
            await NetworkManager.SendAsync(beServerSocket, requestBody);

            // 3) 
            CommonHeader responseHeader = (CommonHeader) await NetworkManager.ReadAsync(beServerSocket, Constants.HeaderSize, typeof(CommonHeader));

            
            if (header.State == MessageState.Fail)
            {
                // fail - room is on the other 
                // read body
                CBJoinRoomResponseBody responseBody = (CBJoinRoomResponseBody) await NetworkManager.ReadAsync(beServerSocket, responseHeader.BodyLength, typeof(CBJoinRoomResponseBody));

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
            await NetworkManager.SendAsync(beServerSocket, header);
            await NetworkManager.SendAsync(beServerSocket, requestBody);

            // 3)
            CommonHeader responseHeader = (CommonHeader) await NetworkManager.ReadAsync(beServerSocket, Constants.HeaderSize, typeof(CommonHeader));

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
      
        public Task HandleErrorAsync(Socket peer, CommonHeader header)
        {
            return Task.Run(()=> {
                NetworkManager.SendAsync(peer, new CommonHeader(header.Type, MessageState.Error, Constants.None, new Cookie(), header.UserInfo));
            });
        }

    }
}
