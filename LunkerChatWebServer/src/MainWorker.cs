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
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;

namespace LunkerChatWebServer.src
{
    class MainWorker
    {

        private static MainWorker instance = null;
        private ILog logger = Logger.GetLoggerInstance();

        private string hostIP = "";

        //======================================================================================
        //======================================================================================
        //======================================================================================
        //======================================================================================

        private HttpListener server = null;
        private static MainWorker mainWorker = null;
        private bool threadState = Constants.ThreadRun;

        private Socket clientListener = null;

        private WebConnectionManager connectionManager = null;

        private List<Socket> readSocketList = null;
        private List<Socket> writeSocketList = null;
        private List<Socket> errorSocketList = null;

        private List<Socket> tmpClientSocket = null;

        private Socket beServerSocket = null;
        private Socket loginServerSocket = null;

        private Task<Socket> clientAcceptTask = null;

        private Task loginSocketConnectTask = null;
        private Task beSocketConnectTask = null;

        private Dictionary<WebSocket, Task> socketTaskPair = null;

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
           
            MainProcess();

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
            connectionManager = new WebConnectionManager();

            readSocketList = new List<Socket>();
            writeSocketList = new List<Socket>();
            errorSocketList = new List<Socket>();

            socketTaskPair = new Dictionary<WebSocket, Task>();

            // set host id
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (ip.ToString().Split('.')[0].Equals("10"))
                    {
                        hostIP = ip.ToString();
                        logger.Debug("[chatserver][Initialize()] host ip : " + hostIP);
                    }
                }
            }

            server = new HttpListener();
            server.Prefixes.Add("http://+:80/");
            server.Start();

        }// end method 

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

                                //===========================================
                                //===========================================
                                //===========================================
                                //===========================================
                                //===========================================
                            }
                        }
                        else
                        {
                            loginServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        }
                    }
                    catch (InvalidOperationException ioe)
                    {
                        //loginServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        //loginServerSocket = null;

                        Console.WriteLine("[ChatServer][MainProcess()] Disconnected . . . login Server . . . retry");
                        continue;
                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine("[ChatServer][MainProcess()] Disconnected . . . login Server . . . retry");
                        //loginServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        continue;
                    }
                }
            });
        }

        public Task HandleBeServerConnectAsync()
        {
            return Task.Run(() => {

                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(AppConfig.GetInstance().BackendServerIp), AppConfig.GetInstance().BackendServerPort);

                while (true)
                {
                    //Task.Delay(500);
                    try
                    {
                        if (beServerSocket != null)
                        {
                            if (!beServerSocket.Connected)
                            {

                                beServerSocket.Connect(endPoint);
                                /*
                                var result = beServerSocket.BeginConnect(endPoint, null, null);

                                bool success = result.AsyncWaitHandle.WaitOne(1000, true);
                                if (success)
                                {
                                    beServerSocket.EndConnect(result);
                                   
                                    Console.WriteLine("[ChatServer][HandleBeServerConnectAsync()] connect success");
                                }
                                */
                               
                                Console.WriteLine("[ChatServer][HandleBeServerConnectAsync()] Connect Backend Server");
                                logger.Debug("[ChatServer][HandleBeServerConnectAsync()] Connect Backend Server");
                            }
                        }
                        else
                        {
                            beServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        }
                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine("[ChatServer][MainProcess()] Reconnect . . . Backend Server . . .");
                        
                        continue;
                    }
                }
            });
        }

        public  Task HandleClientAcceptAsync()
        {
            
            return Task.Run(async () => {

                IPEndPoint ep = new IPEndPoint(IPAddress.Any, 80);
                WebSocketContext webSocketContext = null;
                
                while (true)
                {
                    try
                    {
                        HttpListenerContext context = await server.GetContextAsync();
                        webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
                        Console.WriteLine("[ChatServer][HandleClientAcceptAsync()] Client connect");
                        //webSocketContext.WebSocket;
                        WebSocket websocket = webSocketContext.WebSocket;

                        socketTaskPair.Add(websocket, Task.Run(() => { }));

                    }
                    catch (SocketException se)
                    {
                        
                        clientListener.Close();
                        continue;
                    }
                }
            });
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
                if (loginServerSocket!=null && loginServerSocket.Poll(0, SelectMode.SelectRead))
                {
                    HandleFERequest(loginServerSocket);
                }

                Task tmp = null;
                if (socketTaskPair.Count != 0)
                {
                    foreach (WebSocket peer in socketTaskPair.Keys.ToArray())
                    {
                        
                        try
                        {
                            tmp = (Task)socketTaskPair[peer];

                            if (tmp != null)
                            {
                                if (tmp.IsCompleted)
                                {
                                    //Console.WriteLine("오ㅓㅏㄴ료ㅕ?");
                                    tmp = Task.Run(() => HandleRequest(peer));
                                    socketTaskPair[peer] = tmp;
                                }
                            }
                            else
                            {
                                tmp = Task.Run(() => HandleRequest(peer));
                                socketTaskPair[peer] = tmp;
                            }

                        }
                        catch (KeyNotFoundException knf)
                        {
                            continue;
                        }
                    }// end loop
                }// endif 
            }// end loop
        }// end method

        public async void HandleFERequest(Socket peer)
        {
            if (peer != null && peer.Connected)
            {
                // 정상 연결상태 
                // 일단 CCHeader로 전체 header 사용 
                try
                {
                    //Console.WriteLine("chatserver: handlerequest");
                    CommonHeader header = (CommonHeader)NetworkManager.Read(peer, Constants.HeaderSize, typeof(CommonHeader));
                    Console.WriteLine($"[chat] type : {header.Type}");
                    Console.WriteLine($"[chat] state : {header.State}");
                    Console.WriteLine($"[chat] remote IP: { ((IPEndPoint)peer.RemoteEndPoint).Address.ToString()}");
                    Console.WriteLine($"[chat] remote port: { ((IPEndPoint)peer.RemoteEndPoint).Port}");
                    Console.WriteLine($"[chat] local port: { ((IPEndPoint)peer.LocalEndPoint).Address.ToString()}");

                    CommonHeader cookieVerifyHeader;
                    switch (header.Type)
                    {
                        // login이 chat에 요청을 보낸다.
                        // 그거에 대한 응답을 보내준다.
                        // chat server의 정보를 담아서 보내준다!!
                        case MessageType.FENotice:
                            // chat->login에게 fe의 정보를 보내준것에 대한 response
                            // 
                            // just Get Response about FE Notice 
                            HandleFEInfoRequeset(peer, header);
                            break;

                        // login -> chat
                        // notice user Auth Info 
                        case MessageType.NoticeUserAuth:
                            HandleNoticeUserAuthAsync(peer, header);
                            break;
                     
                        default:
                            //await HandleErrorAsync(peer, header);
                            break;
                    }// end switch
                    //peer.Blocking = true;
                }
                catch (SocketException se)
                {
                    //peer.Blocking = true;
                    // error
                    // Get rid of client 
                    Console.WriteLine(se.StackTrace);
                    Console.WriteLine(se.SocketErrorCode);
                    if (se.SocketErrorCode == SocketError.WouldBlock)
                    {
                        Console.WriteLine("[chatserver][HandleRequest()] socket exception b b ");
                    }
                    else
                    {
                        Console.WriteLine("[chatserver][HandleRequest()] socket exception b b ");
                        peer.Close();
                    }
                    return;
                }
            }
            else
            {
                // clear connection infos 
                // delete socket in connection list 
                /*
                IPEndPoint endPoint = (IPEndPoint)peer.RemoteEndPoint;
                string ip = endPoint.Address.ToString();
                int port = endPoint.Port;
                string key = ip + ":" + port;

                UserInfo userInfo = connectionManager.GetClientInfo(key);

                if (!userInfo.Equals(default(UserInfo)))
                {
                    connectionManager.LogoutClient(key);
                }
                */
            }
        }

        // 요청을 읽고, 작업을 처리하는 비동기 작업을 만들어야함!!!
        // 여기에서 case나눠서 처리 !!!!
        public async void HandleRequest(WebSocket peer)
        {

            if (peer != null && peer.State != WebSocketState.CloseReceived && peer.State != WebSocketState.Closed)
            {
                // 정상 연결상태 
                // 일단 CCHeader로 전체 header 사용 
                try
                {

                    byte[] headerArr = await WebNetworkManager.ReadAsync(peer, Constants.HeaderSize);
                    //Console.WriteLine("chatserver: handlerequest");
                    //CommonHeader header = (CommonHeader) await WebNetworkManager.ReadAsync(peer, Constants.HeaderSize, typeof(CommonHeader));

                    CommonHeader header = (CommonHeader) WebNetworkManager.ByteToStructure(headerArr, typeof(CommonHeader));

                    Console.WriteLine($"[chat] type : {header.Type}");
                    Console.WriteLine($"[chat] state : {header.State}");

                    //Console.WriteLine($"[chat] remote IP: { ((IPEndPoint)peer.RemoteEndPoint).Address.ToString()}");
                    //Console.WriteLine($"[chat] remote port: { ((IPEndPoint)peer.RemoteEndPoint).Port}");
                    //Console.WriteLine($"[chat] local port: { ((IPEndPoint)peer.LocalEndPoint).Address.ToString()}");

                    CommonHeader cookieVerifyHeader;
                    switch (header.Type)
                    {
                        case MessageType.ConnectionSetup:
                            // 인증된 유저가 들어와야 
                            // connectionDic에 저장된다. 
                            HandleConnectionSetupAsync(peer, header);
                            break;

                        // 200: chatting 
                        // ok 
                        case MessageType.Chatting:
                            HandleChattingRequest(peer, header);
                            break;

                        // room : 400 
                        // check cookie available
                        // ok 
                        case MessageType.CreateRoom:
                            /*
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
                            */
                            HandleCreateRoom(peer, header);
                            break;

                        // ok 
                        case MessageType.JoinRoom:
                            /*
                            cookieVerifyHeader = (CommonHeader)await HandleCookieVerifyAsync(header);
                            if (cookieVerifyHeader.State == MessageState.Success)
                            {
                                await HandleJoinRoomAsync(peer, header);
                            }
                            else
                            {
                                // error
                                // not authenticated user
                            }

                            */
                            HandleJoinRoom(peer, header);
                            break;

                        // ok
                        case MessageType.LeaveRoom:
                            //await HandleCookieVerifyAsync(header);
                            HandleLeaveRoom(peer, header);
                            break;

                        // ok
                        // not yet 
                        case MessageType.ListRoom:
                            //await HandleCookieVerifyAsync(header);
                            HandleListChattingRoom(peer, header);
                            break;
                        // ok
                        // default
                        default:
                            //await HandleErrorAsync(peer, header);
                            break;
                    }// end switch
                    //peer.Blocking = true;
                }
                catch (SocketException se)
                {
                    //peer.Blocking = true;
                    // error
                    // Get rid of client 
                    Console.WriteLine(se.StackTrace);
                    Console.WriteLine(se.SocketErrorCode);
                    if (se.SocketErrorCode == SocketError.WouldBlock)
                    {
                        Console.WriteLine("[chatserver][HandleRequest()] socket exception b b ");


                    }
                    else
                    {
                        Console.WriteLine("[chatserver][HandleRequest()] socket exception b b ");
                        //peer.Close();
                        // ===============================================================================
                        // ================================================================close websocket
                        // ===============================================================================
                    }
                    return;
                }
            }
            else
            {
                // clear connection infos 
                // delete socket in connection list 
                /*
                IPEndPoint endPoint = (IPEndPoint)peer.RemoteEndPoint;
                string ip = endPoint.Address.ToString();
                int port = endPoint.Port;
                string key = ip + ":" + port;

                UserInfo userInfo = connectionManager.GetClientInfo(key);

                if (!userInfo.Equals(default(UserInfo)))
                {
                    connectionManager.LogoutClient(key);
                }
                */
            }
        }// end method  



        //=========================================================================================================//
        //=========================================Handle Request==================================================//
        //=========================================================================================================//

        /*
    /// <summary>
    /// Send FE Info BE
    /// </summary>
    public  Task HandleFEInfoRequeset(Socket peer, CommonHeader header)
    {
        return Task.Run(async () => {
            Console.WriteLine("[chatserver][HandleFEInfoRequeset()] handle");
            CBServerInfoNoticeResponseBody responseBody = new CBServerInfoNoticeResponseBody(new ServerInfo(hostIP, 80));

            CommonHeader responseHeader = new CommonHeader(MessageType.FENotice, MessageState.Response, Marshal.SizeOf(responseBody), new Cookie(), new UserInfo());

            await NetworkManager.SendAsync(peer, responseHeader);
            await NetworkManager.SendAsync(peer, responseBody);

        });
    }
    */

        public void HandleFEInfoRequeset(Socket peer, CommonHeader header)
        {
           
                Console.WriteLine("[chatserver][HandleFEInfoRequeset()] handle");
                CBServerInfoNoticeResponseBody responseBody = new CBServerInfoNoticeResponseBody(new ServerInfo(hostIP, 80));

                CommonHeader responseHeader = new CommonHeader(MessageType.FENotice, MessageState.Response, Marshal.SizeOf(responseBody), new Cookie(), new UserInfo());

                 NetworkManager.Send(peer, responseHeader);
                 NetworkManager.Send(peer, responseBody);

        }


        public async void HandleNoticeUserAuth(Socket peer, CommonHeader header)
        {
            Console.WriteLine("[chatserver][HandleNoticeUserAuth()] start");
            // 1)
            LCUserAuthRequestBody requestBody = (LCUserAuthRequestBody) NetworkManager.Read(peer, header.BodyLength, typeof(LCUserAuthRequestBody));
            Console.WriteLine($"[chatserver][HandleNoticeUserAuth()] cookie : " + header.Cookie);
            Console.WriteLine($"[chatserver][HandleNoticeUserAuth()] userinfo id: " + requestBody.UserInfo.GetPureId());
            // 2) 
            connectionManager.AddAuthInfo(requestBody.UserInfo.GetPureId(), requestBody.Cookie);
            Console.WriteLine("[chatserver][HandleNoticeUserAuth()] end");
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
            return Task.Run(() => HandleNoticeUserAuth(peer, header));
        }

        /// <summary>
        /// <oara>Authorize User Before Chatting</oara>
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public Task HandleConnectionSetupAsync(WebSocket peer, CommonHeader header)
        {
            return Task.Run(() => {
                Console.WriteLine("[chatserver][HandleConnectionSetupAsync()] start");
                Cookie sentCookie = header.Cookie;
                UserInfo userInfo = header.UserInfo;

                Cookie authorizedCookie = connectionManager.GetAuthInfo(header.UserInfo.GetPureId());

                // 인증실패
                if (sentCookie.Value != authorizedCookie.Value)
                {
                    CommonHeader responseHeader = new CommonHeader(MessageType.ConnectionSetup, MessageState.Fail, Constants.None, new Cookie(), userInfo);
                    WebNetworkManager.SendAsync(peer, responseHeader);
                    //socketTaskPair.Remove(peer);

                    //peer.Close();
                }
                else
                {
                    // 인증성공
                    CommonHeader responseHeader = new CommonHeader(MessageType.ConnectionSetup, MessageState.Success, Constants.None, new Cookie(), userInfo);
                    WebNetworkManager.SendAsync(peer, responseHeader);

                    connectionManager.AddClientConnection(header.UserInfo.GetPureId(), peer);
                    //socketTaskPair.Add(peer, Task.Run(()=> { }));
                    //tmpClientSocket.Remove(peer);
                }
                Console.WriteLine("[chatserver][HandleConnectionSetupAsync()] end");
            });
        }

        /// <summary>
        /// <para>handle chatting request from client </para>
        /// <para>be에 보내는 logic 추가해야함 </para>
        /// </summary>
        /// <param name="peer"></param>
        public async void HandleChattingRequest(WebSocket peer, CommonHeader header)
        {
            Console.WriteLine("[chatserver][HandleChattingRequest()] start");
            byte[] messageBuff = new byte[header.BodyLength];
            messageBuff = await WebNetworkManager.ReadAsync(peer, header.BodyLength);
            // read message

            // Get User Entered Room 
            ChattingRoom enteredRoom = connectionManager.GetChattingRoomJoinInfo(header.UserInfo.GetPureId()); // room info ~ user id 

            // broadcast
            WebSocket client = null;

            header.State = MessageState.Success;

            foreach (string user in connectionManager.GetChattingRoomListInfoKey(enteredRoom))
            {
                client = connectionManager.GetClientConnection(user);
              
                // broadcast to each client
                await WebNetworkManager.SendAsync(client, header);
                await WebNetworkManager.SendAsync(client, messageBuff);
            }

            // Send chatting to BE 
            //string sendingUser = new string(header.UserInfo.Id);
            CommonHeader responseHeader = new CommonHeader(MessageType.Chatting, MessageState.Request, Constants.None, new Cookie(), header.UserInfo);
            await NetworkManager.SendAsync(beServerSocket, responseHeader);
            // worker에게 위임? 
            //beWorker.HandleChatting(header);
            Console.WriteLine("[chatserver][HandleChattingRequest()] end");
        }

        public Task HandleChattingRequestAsync(WebSocket peer, CommonHeader header)
        {
            return Task.Run(() => HandleChattingRequest(peer, header));
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
            return Task.Run(() => {

                CommonHeader requestHeader = new CommonHeader(MessageType.VerifyCookie, MessageState.Request, Constants.None, new Cookie(), header.UserInfo);
                NetworkManager.SendAsync(beServerSocket, requestHeader);
            });
        }

        public Task<Object> ResponseCookieVerify()
        {
            return NetworkManager.ReadAsync(beServerSocket, Constants.HeaderSize, typeof(CommonHeader));
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
        public async void HandleCreateRoom(WebSocket peer, CommonHeader header)
        {
            Console.WriteLine("[ChatServer][HandleCreateRoom()] start");
            // 1) send request to BE server
            NetworkManager.Send(beServerSocket, header);

            // 2) read response(header, body) from BE
            CommonHeader responseHeader = (CommonHeader)NetworkManager.Read(beServerSocket, Constants.HeaderSize, typeof(CommonHeader));
            CBCreateRoomResponseBody responseBody = (CBCreateRoomResponseBody)NetworkManager.Read(beServerSocket, responseHeader.BodyLength, typeof(CBCreateRoomResponseBody));

            Console.WriteLine("[ChatServer][HandleCreateRoom()] end");

            // 3) send response(header, body) to client
            await WebNetworkManager.SendAsync(peer, responseHeader);
            await WebNetworkManager.SendAsync(peer, responseBody);

            // 4) 
            connectionManager.AddChattingRoomListInfoKey(responseBody.ChattingRoom);
            Console.WriteLine("[ChatServer][HandleCreateRoom()] end");
        }

        public Task HandleCreateRoomAsync(WebSocket peer, CommonHeader header)
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
        public async void HandleListChattingRoom(WebSocket peer, CommonHeader header)
        {
            Console.WriteLine("[ChatServer][HandleListChattingRoom()] start");
            // 1) 
            await NetworkManager.SendAsync(beServerSocket, header);

            // 2) 
            CommonHeader responseHeader = (CommonHeader)NetworkManager.Read(beServerSocket, Constants.HeaderSize, typeof(CommonHeader));
            byte[] responseBody = NetworkManager.Read(beServerSocket, responseHeader.BodyLength);

            // 3) 

            WebNetworkManager.SendAsync(peer, responseHeader);
            WebNetworkManager.SendAsync(peer, responseBody);
            Console.WriteLine("[ChatServer][HandleListChattingRoom()] end");
        }

        public Task HandleListChattingRoomAsync(WebSocket peer, CommonHeader header)
        {
            return Task.Run(() => HandleListChattingRoom(peer, header));
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
        public async void HandleJoinRoom(WebSocket peer, CommonHeader header)
        {
            Console.WriteLine("[ChatServer][HandleJoinRoom()] start");
            ChattingRoom enteredRoom = default(ChattingRoom);
            string userId = default(string);

            //  1) 
            CCJoinRequestBody requestBody = (CCJoinRequestBody) await WebNetworkManager.ReadAsync(peer, header.BodyLength, typeof(CCJoinRequestBody));
            enteredRoom = requestBody.RoomInfo;
            userId = header.UserInfo.GetPureId();

            // 2) 
            await NetworkManager.SendAsync(beServerSocket, new CommonHeader(header.Type, MessageState.Request, header.BodyLength, new Cookie(), new UserInfo()));
            await NetworkManager.SendAsync(beServerSocket, requestBody);

            // 3) 
            CommonHeader responseHeader = (CommonHeader)NetworkManager.Read(beServerSocket, Constants.HeaderSize, typeof(CommonHeader));

            if (responseHeader.State == MessageState.Fail)
            {
                // fail - room is on the other 
                // read body
                CBJoinRoomResponseBody responseBody = (CBJoinRoomResponseBody)NetworkManager.Read(beServerSocket, responseHeader.BodyLength, typeof(CBJoinRoomResponseBody));

                await WebNetworkManager.SendAsync(peer, responseHeader);
                await WebNetworkManager.SendAsync(peer, responseBody);
            }
            else if (responseHeader.State == MessageState.Success)
            {
                // success 
                // send header to client
                responseHeader.BodyLength = 0;
                connectionManager.AddChattingRoomJoinInfo(userId, enteredRoom);
                connectionManager.AddChattingRoomListInfoValue(enteredRoom, userId);
                await WebNetworkManager.SendAsync(peer, responseHeader);
                // add user info to data structure 
                //connectionManager.AddChattingRoomListInfoValue(enteredRoom, userId);
            }
            else
            {
                // error
                responseHeader.BodyLength = 0;
                await WebNetworkManager.SendAsync(peer, responseHeader);
            }
            Console.WriteLine("[ChatServer][HandleJoinRoom()] end");
        }

        public Task HandleJoinRoomAsync(WebSocket peer, CommonHeader header)
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
        public async void HandleLeaveRoom(WebSocket peer, CommonHeader header)
        {
            ChattingRoom enteredRoom = default(ChattingRoom);
            string userId = new string(header.UserInfo.Id);
            // 1) 
            CCLeaveRequestBody requestBody = (CCLeaveRequestBody)await WebNetworkManager.ReadAsync(peer, header.BodyLength, typeof(CCLeaveRequestBody));
            enteredRoom = requestBody.RoomInfo;

            // 2) send
            await NetworkManager.SendAsync(beServerSocket, header);
            await NetworkManager.SendAsync(beServerSocket, requestBody);

            // 3)
            CommonHeader responseHeader = (CommonHeader)await NetworkManager.ReadAsync(beServerSocket, Constants.HeaderSize, typeof(CommonHeader));

            // 4) 
            await WebNetworkManager.SendAsync(peer, responseHeader);

            // 5)
            connectionManager.DeleteChattingRoomListInfoValue(enteredRoom, userId);
        }

        public Task HandleLeaveRoomAsync(WebSocket peer, CommonHeader header)
        {
            return Task.Run(() => {
                HandleLeaveRoom(peer, header);
            });
        }

        public Task HandleErrorAsync(WebSocket peer, CommonHeader header)
        {
            return Task.Run(() => {
                WebNetworkManager.SendAsync(peer, new CommonHeader(header.Type, MessageState.Error, Constants.None, new Cookie(), header.UserInfo));
            });
        }


    }
}
