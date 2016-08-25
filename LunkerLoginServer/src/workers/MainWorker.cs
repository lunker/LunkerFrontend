using log4net;
using LunkerLibrary.common.protocol;
using LunkerLibrary.common.Utils;
using LunkerLoginServer.src.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LunkerLoginServer.src.workers
{
    public class MainWorker
    {
        private static MainWorker instance = null;
        private ILog logger = LoginLogger.GetLoggerInstance();
        private bool threadState = Constants.AppRun;
        private string hostIP = "";

        private List<Socket> clientConnection = null;
        private Dictionary<string, Socket> rawClientSocketDic = null;

        //private Dictionary<ServerInfo, Socket> feConnectionDic = null;
        private Dictionary<Socket, ServerInfo> feConnectionDic = null;

        private List<Socket> readSocketList = null;
        private List<Socket> writeSocketList = null;
        private List<Socket> errorSocketList = null;

        private Task<Socket> clientAcceptTask = null;
        private Task<Socket> feAcceptTask = null;

        private Socket clientListener = null;
        private Socket feListener = null;
        private Socket beSocket = null;

        private Dictionary<Socket, Task> socketTaskPair = null;

        private MainWorker() { }
        public static MainWorker GetInstance()
        {
            if (instance == null)
            {
                instance = new MainWorker();
            }
            return instance;
        }

        public void Start()
        {
            Console.WriteLine("login: start");
            logger.Debug("[LoginServer][MainWorker][Start()] start");

            Initialize();

            MainProcess(); 

            logger.Debug("[LoginServer][MainWorker][Start()] end");
            
        }

        /// <summary>
        /// Stop Thread
        /// </summary>
        public void Stop()
        {
            threadState = Constants.ThreadStop;
        }

        /// <summary>
        /// <para>Initialize variable</para>
        /// </summary>
        public void Initialize()
        {
            Console.WriteLine("login 초기화");

            clientConnection = new List<Socket>();
            rawClientSocketDic = new Dictionary<string, Socket>();


            feConnectionDic = new Dictionary<Socket, ServerInfo>();

            socketTaskPair = new Dictionary<Socket, Task>();

            readSocketList = new List<Socket>();
            writeSocketList = new List<Socket>();
            errorSocketList = new List<Socket>();

            IPEndPoint clientListenEndPoint = new IPEndPoint(IPAddress.Any, 43310);
            clientListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            clientListener.Bind(clientListenEndPoint);
            clientListener.Listen(AppConfig.GetInstance().Backlog);

            IPEndPoint feListenEndPoint = new IPEndPoint(IPAddress.Any, AppConfig.GetInstance().FrontListenEndPoint);

            feListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            feListener.Bind(feListenEndPoint);
            feListener.Listen(AppConfig.GetInstance().Backlog);

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

            Console.WriteLine("loginserver : fe socket listen");
        }

        /// <summary>
        /// Task that connect be async
        /// </summary>
        public Task HandleBEConnectAsnyc()
        {
            return Task.Run(()=> 
            {
                while (true)
                {
                    try
                    {
                        if (beSocket != null)
                        {
                            if (!beSocket.Connected)
                            {
                                beSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                                IPEndPoint beEndPoint = new IPEndPoint(IPAddress.Parse(AppConfig.GetInstance().Backendserverip), AppConfig.GetInstance().Backendserverport);

                                beSocket.Connect(beEndPoint);
                                Console.WriteLine("loginserver : BE connect success");
                                // send 
                                HandleBERequest(beSocket);
                                Console.WriteLine("loginserver : handle be request");
                            }
                        }
                        else
                        {
                            beSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        }
                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine("[ChatServer][MainProcess()] Reconnect . . . Backend Server . . .");
                        continue;
                    }
                }// end loop
            });
        }// end method 

        public void HandleBERequest(Socket beSocket)
        {
            CommonHeader requestHeader = (CommonHeader) NetworkManager.Read(beSocket, Constants.HeaderSize, typeof(CommonHeader));
            Console.WriteLine("read be request");
            requestHeader.State = MessageState.Fail;
            NetworkManager.Send(beSocket, requestHeader);
            Console.WriteLine("send be  res");
        }

        /// <summary>
        /// Task that accept Chatting Client Connect Request async
        /// </summary>
        /// <returns></returns>
        public Task HandleClientAcceptAsync()
        {
            return Task.Run(() => {

                while (true)
                {
                    try
                    {
                        if (clientListener != null)
                        {
                            if (clientListener.IsBound)
                            {
                                Socket client = clientListener.Accept();
                                Console.WriteLine("loginserver : client connected!!");
                                logger.Debug("[ChatServer][HandleClientAcceptAsync()] Accept Client Connect");

                                // Add accepted connections
                                clientConnection.Add(client);
                                //socketTaskPair.Add(client, Task.Run(() => { }));

                                Task.Run(()=> { HandleRequest(client); });
                            }
                        }
                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine("loginserver : client listener disconnected!!");
                        continue;
                    }
                }
            });
        }


        /// <summary>
        /// Task that accept FE Connect Request async
        /// </summary>
        /// <returns></returns>
        public Task HandleFEAcceptAsync()
        {
            return Task.Run(() => {
                //feListenr = new Socket(SocketType.Stream, ProtocolType.Tcp);

                while (true)
                {
                    if (feListener != null)
                    {
                        if (feListener.IsBound)
                        {
                            Socket feSocket = feListener.Accept();
                            logger.Debug("[ChatServer][HandleFEAcceptAsync()] complete accept task. Restart");

                            // Add accepted connections
                            //clientConnection.Add(socket);
                            feConnectionDic.Add(feSocket, default(ServerInfo));


                            //socketTaskPair.Add(socket, Task.Run(() => { }));

                            Task.Run(()=> { HandleChatServer(feSocket); });
                            // Request FE Server Info 
                            // await 할 필요가 있나 ?
                            RequestFEInfoAsync(feSocket);

                            // add fe count
                            LoadBalancer.AddFE();
                        }

                    }
                }
            });
        }

        public void MainProcess()
        {
            Console.WriteLine("[LoginServer][MainProcess()] start");
            logger.Debug("[LoginServer][MainProcess()] start");

            HandleClientAcceptAsync();
            HandleFEAcceptAsync();
            HandleBEConnectAsnyc();

        }// end method

        public void HandleChatServer(Socket feSocket)
        {
            while (true)
            {
                // feSocket : client 
                if (feSocket != null && feSocket.Connected)
                {
                    try
                    {
                        //Console.WriteLine("[LoginServer][HandleRequest()] handle client request start");
                        logger.Debug("[LoginServer][HandleRequest()] handle client request start");
                        // 정상 연결상태 
                        //CommonHeader header = (CommonHeader)NetworkManager.ReadAsync(feSocket, 8, typeof(CommonHeader));

                        CommonHeader header = (CommonHeader)NetworkManager.Read(feSocket, Constants.HeaderSize, typeof(CommonHeader));
                        Console.WriteLine("정말?" + Marshal.SizeOf(header));

                        Console.WriteLine($"type;{header.Type}");
                        Console.WriteLine($"state;{header.State}");

                        Console.WriteLine($"remote ip;{ ((IPEndPoint)feSocket.RemoteEndPoint).Address}");
                        Console.WriteLine($"remote port;{((IPEndPoint)feSocket.RemoteEndPoint).Port}");
                        Console.WriteLine($"local port;{ ((IPEndPoint)feSocket.LocalEndPoint).Port}");

                        switch (header.Type)
                        {
                            case MessageType.FENotice:
                                Console.WriteLine("[LoginServer][HandleRequest()] FENotice.");
                                //await HandleFENoticeAsync(feSocket, header);
                                HandleFENotice(feSocket, header);
                                break;

                            case MessageType.NoticeUserAuth:
                                HandleNoticeUserAuth(feSocket, header);
                                break;

                            case MessageType.Signin:
                                Console.WriteLine("[LoginServer][HandleRequest()] Signin.");
                                HandleSignin(feSocket, header);
                                break;

                            case MessageType.Logout:
                                Console.WriteLine("[LoginServer][HandleRequest()] Logout.");
                                HandleLogout(feSocket, header);
                                break;

                            case MessageType.Signup:
                                Console.WriteLine("[LoginServer][HandleRequest()] Signup.");
                                HandleSignup(feSocket, header);
                                break;

                            case MessageType.Delete:
                                Console.WriteLine("[LoginServer][HandleRequest()] Delete.");
                                HandleDelete(feSocket, header);
                                break;
                                
                            case MessageType.Modify:
                                Console.WriteLine("[LoginServer][HandleRequest()] Modify.");
                                HandleModify(feSocket, header);
                                break;
                            // default
                            default:
                                 HandleError(feSocket, header);
                                break;
                        }
                        feSocket.Blocking = true;
                        logger.Debug("[LoginServer][HandleRequest()] handle client request end");
                        Console.WriteLine("[LoginServer][HandleRequest()] handle client request end");

                    }
                    catch (SocketException e)
                    {
                        // handling .
                        // get rid of socket connection in list 

                        // 1) client connection 이거나 
                        // 2) fe connection 이거나 
                        Console.WriteLine("[LoginServer][HandleRequest()] disconnected . . .");
                        logger.Debug("[LoginServer][HandleRequest()] socket exception . . .");

                        socketTaskPair.Remove(feSocket);

                        if (clientConnection.Contains(feSocket))
                        {
                            clientConnection.Remove(feSocket);
                            feSocket.Dispose();
                            return;
                        }

                        if (feConnectionDic.Keys.ToList().Contains(feSocket))
                        {
                            feConnectionDic.Remove(feSocket);
                            if (feSocket.Connected)
                                feSocket.Close();

                            feSocket.Dispose();

                            LoadBalancer.DeleteFE();

                            return;
                        }

                    }// end try-catch
                }
                else
                {

                }
            }
        }



        /// <summary>
        /// chat
        /// </summary>
        /// <param name="feSocket"></param>
        /// <returns></returns>
        public Task RequestFEInfoAsync(Socket feSocket)
        {
            return Task.Run(() => {
                Console.WriteLine("[LoginServer][RequestFEInfoAsync()] start");
                logger.Debug("[LoginServer][RequestFEInfoAsync()] start");
                CommonHeader requestHeader = new CommonHeader(MessageType.FENotice, MessageState.Request, Constants.None, new Cookie(), new UserInfo());
                NetworkManager.SendAsync(feSocket, requestHeader);
            });
        }
       
        /// <summary>
        /// Handle Chatting Client && ChatServer Request 
        /// </summary>
        /// <param name="peer"></param>
        public async void HandleRequest(Socket peer)
        {
            while (true)
            {
                // peer : client 
                if (peer != null && peer.Connected)
                {
                    try
                    {
                        //Console.WriteLine("[LoginServer][HandleRequest()] handle client request start");
                        logger.Debug("[LoginServer][HandleRequest()] handle client request start");
                        // 정상 연결상태 
                        //CommonHeader header = (CommonHeader)NetworkManager.ReadAsync(peer, 8, typeof(CommonHeader));

                        CommonHeader header = (CommonHeader)NetworkManager.Read(peer, Constants.HeaderSize, typeof(CommonHeader));
                        Console.WriteLine("정말?" + Marshal.SizeOf(header));

                        Console.WriteLine($"type;{header.Type}");
                        Console.WriteLine($"state;{header.State}");

                        Console.WriteLine($"remote ip;{ ((IPEndPoint)peer.RemoteEndPoint).Address}");
                        Console.WriteLine($"remote port;{((IPEndPoint)peer.RemoteEndPoint).Port}");
                        Console.WriteLine($"local port;{ ((IPEndPoint)peer.LocalEndPoint).Port}");

                        switch (header.Type)
                        {

                            case MessageType.ConnectionPassing:
                                HandleConnectionPassing(peer, header);
                                break;

                            case MessageType.FENotice:
                                Console.WriteLine("[LoginServer][HandleRequest()] FENotice.");
                                //await HandleFENoticeAsync(peer, header);
                                HandleFENotice(peer, header);
                                break;

                            case MessageType.NoticeUserAuth:
                                HandleNoticeUserAuth(peer, header);
                                break;

                            case MessageType.Signin:
                                Console.WriteLine("[LoginServer][HandleRequest()] Signin.");
                                HandleSignin(peer, header);
                                break;

                            case MessageType.Logout:
                                Console.WriteLine("[LoginServer][HandleRequest()] Logout.");
                                HandleLogout(peer, header);
                                break;

                            case MessageType.Signup:
                                Console.WriteLine("[LoginServer][HandleRequest()] Signup.");
                                HandleSignup(peer, header);
                                break;

                            case MessageType.Delete:
                                Console.WriteLine("[LoginServer][HandleRequest()] Delete.");
                                HandleDelete(peer, header);
                                break;

                            case MessageType.Modify:
                                Console.WriteLine("[LoginServer][HandleRequest()] Modify.");
                                HandleModify(peer, header);
                                break;
                            // default
                            default:
                                await HandleErrorAsync(peer, header);
                                break;
                        }
                        logger.Debug("[LoginServer][HandleRequest()] handle client request end");
                        Console.WriteLine("[LoginServer][HandleRequest()] handle client request end");

                    }
                    catch (SocketException e)
                    {
                        // handling .
                        // get rid of socket connection in list 

                        // 1) client connection 이거나 
                        // 2) fe connection 이거나 
                        logger.Debug("[LoginServer][HandleRequest()] socket exception . . .");
                        Console.WriteLine("[LoginServer] Client Disconnected . . .");
                        socketTaskPair.Remove(peer);

                        if (clientConnection.Contains(peer))
                        {
                            clientConnection.Remove(peer);
                            peer.Dispose();
                            return;
                        }

                        if (feConnectionDic.Keys.ToList().Contains(peer))
                        {
                            feConnectionDic.Remove(peer);
                            peer.Dispose();
                            return;
                        }

                    }// end try-catch
                }
                else
                {

                }
            }
        }// end method

        public void HandleConnectionPassing(Socket peer, CommonHeader header)
        {
            Console.WriteLine("loginserver[HandleConnectionPassing]  start");
            CLConnectionPassingRequestBody body = (CLConnectionPassingRequestBody) NetworkManager.Read(peer, header.BodyLength, typeof(CLConnectionPassingRequestBody));
            Console.WriteLine("loginserver[HandleConnectionPassing]  read client requeset body");

            Socket connectingChatServerSocket = null;

            for(int idx=0; idx<feConnectionDic.Count; idx++)
            {
                if (feConnectionDic.ElementAt(idx).Value.Equals(body.ServerInfo))
                {
                    connectingChatServerSocket = feConnectionDic.ElementAt(idx).Key;
                }
            }

            CommonHeader chatAuthRequestHeader = new CommonHeader(MessageType.ConnectionSetup, MessageState.Request, Constants.None, header.Cookie, header.UserInfo);

            NetworkManager.Send(connectingChatServerSocket, chatAuthRequestHeader);
            Console.WriteLine("loginserver[HandleConnectionPassing] send user auth info to chatting server");


            CommonHeader responseHeader = (CommonHeader) NetworkManager.Read(connectingChatServerSocket, Constants.HeaderSize, typeof(CommonHeader));
            Console.WriteLine("loginserver[HandleConnectionPassing] read user auth info response from chatting server");
            responseHeader.Type = MessageType.ConnectionPassing;

            NetworkManager.Send(peer, responseHeader);
            Console.WriteLine("loginserver[HandleConnectionPassing]  end");
        }

        /// <summary>
        /// <para>Handle Chat Service Info</para>
        /// <para>1) read body from chat server info</para>
        /// <para>2) save </para>
        /// <para>3) send response to fe</para>
        /// <para></para>
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="header"></param>
        public async void HandleFENotice(Socket feSocket, CommonHeader header)
        {

            Console.WriteLine("[LoginServer][HandleFENotice()] fenotice start");
            logger.Debug("[LoginServer][HandleFENotice()] fenotice start");

            // 1) 
            CBServerInfoNoticeResponseBody responseBody = (CBServerInfoNoticeResponseBody) NetworkManager.Read(feSocket, header.BodyLength, typeof(CBServerInfoNoticeResponseBody));

            // 2) 
            // 
            if (feConnectionDic.ContainsKey(feSocket))
            {
                feConnectionDic.Remove(feSocket);
                feConnectionDic.Add(feSocket, responseBody.ServerInfo);
            }

            
            logger.Debug("[LoginServer][HandleFENotice()] fenotice end");
            Console.WriteLine("[LoginServer][HandleFENotice()] fenotice end");
        }

        public Task HandleFENoticeAsync(Socket peer, CommonHeader header)
        {
            return Task.Run(()=>HandleFENotice(peer, header));
        }

        /// <summary>
        /// FE로 부터 사용자 인증 정보를 받았다는걸 받음.
        /// </summary>
        /// <param name="feSocket"></param>
        /// <param name="header"></param>
        public  void HandleNoticeUserAuth(Socket feSocket, CommonHeader header)
        {
            Console.WriteLine("[loginserver][HandleNoticeUserAuth()] start");
            // find client socket from dictionary
            Socket client = null;
            
            rawClientSocketDic.TryGetValue(header.UserInfo.GetPureId(),out client);

            if (client != null && feSocket !=null )
            {
                // LoadBalancing
                // pick FE 
                // client에게 login 성공을 보낸다.

                ServerInfo serverInfo = default(ServerInfo);
                if(feConnectionDic.TryGetValue(feSocket, out serverInfo))
                {
                    
                    CLSigninResponseBody body = new CLSigninResponseBody(header.Cookie, serverInfo);
                    CommonHeader responseHeader = new CommonHeader(MessageType.Signin, MessageState.Success, Marshal.SizeOf(body), header.Cookie, header.UserInfo);

                    NetworkManager.Send(client, responseHeader);
                    NetworkManager.Send(client, body);

                    //NetworkManager.Send(client, responseHeader, body);

                    Console.WriteLine("[loginserver][HandleNoticeUserAuth()] auth success.");
                    Console.WriteLine("[loginserver][HandleNoticeUserAuth()] Login Finally Success.");

                }
            }
            Console.WriteLine("[loginserver][HandleNoticeUserAuth()] end");
        }

        /// <summary>
        /// Handle Login 
        /// <para>1) read signin info from client</para>
        /// <para>2) send signin request to be</para>
        /// <para>3) read signin response from be</para>
        /// <para>4) send signin response to client</para>
        /// <para>5) send auth info to Chat Server</para>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="header"></param>
        public async void HandleSignin(Socket client, CommonHeader header)
        {
            Console.WriteLine("[LoginServer][HandleSignin()] signin start");
            logger.Debug("[LoginServer][HandleSignin()] signin start");
            UserInfo signinUser;
            Cookie cookie;

            // read body from client
            //CLSigninRequestBody body = (CLSigninRequestBody) await NetworkManager.ReadAsync(client, header.BodyLength, typeof(CLSigninRequestBody));
            signinUser = header.UserInfo;
            Console.WriteLine($"[LoginServer][HandleSignin()] signin : {signinUser.Id} : {signinUser.Pwd}" );

            CommonHeader requestHeader = new CommonHeader(header.Type, header.State, Constants.None , new Cookie(), header.UserInfo);

            /*
                묶어야함!!
             */
            // send header+body to be
            NetworkManager.Send(beSocket, requestHeader);
            //await NetworkManager.SendAsync(beSocket, body);

            /***
             * 위험요소! 
             */
            // read header from be 
            //CommonHeader responseHeader = (CommonHeader) await NetworkManager.ReadAsync(beSocket, Constants.HeaderSize, typeof(CommonHeader) );
            CommonHeader responseHeader = (CommonHeader) NetworkManager.Read(beSocket, Constants.HeaderSize, typeof(CommonHeader));

            if (responseHeader.State == MessageState.Success)
            {
                // =====================READ COOKIE FROM BE
                // cookie in body 
                //LBSigninResponseBody responseBody = (LBSigninResponseBody) await NetworkManager.ReadAsync(beSocket, responseHeader.BodyLength, typeof(LBSigninResponseBody));
                LBSigninResponseBody responseBody = (LBSigninResponseBody)NetworkManager.Read(beSocket, responseHeader.BodyLength, typeof(LBSigninResponseBody));
                cookie = responseBody.Cookie;

                int index = LoadBalancer.RoundRobin();

                responseBody.ServerInfo = feConnectionDic.ElementAt(index).Value;

                Console.WriteLine("client에게 보내지는 FE의 정보 : " + responseBody.ServerInfo.GetPureIp() + ":" + responseBody.ServerInfo.Port);

                /****
                 * 
                 * 
                 * 
                 */
                rawClientSocketDic.Add(signinUser.GetPureId(), client);

                // !!!!!! loadbalancing 
                // send auth to Chat Server

                LCUserAuthRequestBody feRequestBody = new LCUserAuthRequestBody(cookie, signinUser);
                //byte[] bodyArr = NetworkManager.StructureToByte(feRequestBody);
                CommonHeader feRequestHeader = new CommonHeader(MessageType.NoticeUserAuth, MessageState.Request, Marshal.SizeOf(feRequestBody), cookie, responseHeader.UserInfo);


                //await NetworkManager.SendAsync(feConnectionDic.ElementAt(index).Key, feRequestHeader);
                //await NetworkManager.SendAsync(feConnectionDic.ElementAt(index).Key, feRequestBody);

                NetworkManager.Send(feConnectionDic.ElementAt(index).Key, feRequestHeader, feRequestBody);

                //NetworkManager.Send(feConnectionDic.ElementAt(index).Key, feRequestHeader);
                //NetworkManager.Send(feConnectionDic.ElementAt(index).Key, feRequestBody);

                // select FE Server to connect with client
                Console.WriteLine("[LoginServer][HandleSignin()] signin success");
                Console.WriteLine("[LoginServer][HandleSignin()] send user auth info to Chat Server");
            }
            else
            {
                Console.WriteLine("[LoginServer][HandleSignin()] signin fail");
                NetworkManager.Send(client, responseHeader);
            }

            logger.Debug("[LoginServer][HandleSignin()] signin end");
            Console.WriteLine("[LoginServer][HandleSignin()] signin end");
        }
        
        public Task HandleSigninAsync(Socket client, CommonHeader header)
        {
            return Task.Run(()=> HandleSignin(client, header));
        }

        public void HandleLogout(Socket client, CommonHeader header)
        {

            NetworkManager.Send(beSocket, header);

            CommonHeader resonseHeader = (CommonHeader)NetworkManager.Read(beSocket, Constants.HeaderSize, typeof(CommonHeader));

            //NetworkManager.Send(beSocket, header);
            // ++ login server에서 해당 유저 delete
            NetworkManager.Send(client, resonseHeader);
            //rawClientSocketDic.Add(signinUser.GetPureId(), client);
            rawClientSocketDic.Remove(header.UserInfo.GetPureId());
        }

        /// <summary>
        /// Client의 Logout처리.
        /// <para>Connection은 그대로 두고, 그냥 BE와 정보만 주고 받는다.</para>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public Task HandleLogoutAsnyc(Socket client, CommonHeader header)
        {
            return Task.Run(()=> {
                NetworkManager.Send(beSocket, header);

                CommonHeader resonseHeader = (CommonHeader) NetworkManager.Read(beSocket, Constants.HeaderSize, typeof(CommonHeader));

                NetworkManager.Send(client, resonseHeader);
            });
        }

        public async void HandleSignup(Socket client, CommonHeader header)
        {
            Console.WriteLine("[LoginServer][HandleSignup()] signup start");
            logger.Debug("[LoginServer][HandleSignup()] signup start");
            // read body from client
            //CLSignupRequestBody requestBody = (CLSignupRequestBody) NetworkManager.Read(client, header.BodyLength, typeof(CLSignupRequestBody));

            // create request header
            // send request header to be
            await NetworkManager.SendAsync(beSocket, header);

            // send request body to be 
            //await NetworkManager.SendAsync(beSocket, requestBody);

            // read header from be
            CommonHeader responseHeader = (CommonHeader) NetworkManager.Read(beSocket, Constants.HeaderSize, typeof(CommonHeader));

            // send result(header) to client 
            await NetworkManager.SendAsync(client, responseHeader);
            Console.WriteLine("[LoginServer][HandleSignup()] signup end");
            logger.Debug("[LoginServer][HandleSignup()] signup end");
        }

        public Task HandleSignupAsync(Socket client, CommonHeader header)
        {
            return Task.Run(()=> { HandleSignup(client, header); });
        }

        /// <summary>
        /// <para>1) read body from client</para>
        /// <para>2) send delete request to be</para>
        /// <para>3) read delete response(header) from be</para>
        /// <para>4) send delete response(header) to client</para>
        /// <para>### Chat Server의 Logout 처리?는  </para>
        /// <para></para>
        /// <para></para>
        /// <para></para>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="header"></param>
        public async void HandleDelete(Socket client, CommonHeader header)
        {
            Console.WriteLine("[LoginServer][HandleDelete()] delete start");

            // 2)
            await NetworkManager.SendAsync(beSocket, header );
            
            // 3)
            CommonHeader responseHeader =(CommonHeader)await NetworkManager.ReadAsync(beSocket, Constants.HeaderSize, typeof(CommonHeader));

            // 4) 
            await NetworkManager.SendAsync(client, responseHeader);
            Console.WriteLine("[LoginServer][HandleDelete()] delete end");
        }

        public Task HandleDeleteAsync(Socket client, CommonHeader header)
        {
            return Task.Run(()=> HandleDelete(client, header));
        }

        /// <summary>
        /// <para>1) read body from client</para>
        /// <para>2) send modify request to be</para>
        /// <para>3) read modify response(header) from be</para>
        /// <para>4) send modify response(header) to client</para>
        /// <para></para>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="header"></param>
        public async void HandleModify(Socket client, CommonHeader header)
        {
            Console.WriteLine("[LoginServer][HandleModify()] modify start");
            logger.Debug("[LoginServer][HandleModify()] modify start");
            // 1) 
            CLModifyRequestBody clRequestBody = (CLModifyRequestBody)  NetworkManager.Read(client, header.BodyLength, typeof(CLModifyRequestBody));

            // 2)
            NetworkManager.Send(beSocket, header, clRequestBody);

            //NetworkManager.Send(beSocket, header);

            //NetworkManager.Send(beSocket, clRequestBody);

            // 3)
            CommonHeader responseHeader = (CommonHeader) NetworkManager.Read(beSocket, Constants.HeaderSize, typeof(CommonHeader));

            // 4) 
            await NetworkManager.SendAsync(client, responseHeader);
            logger.Debug("[LoginServer][HandleModify()] modify end");
            Console.WriteLine("[LoginServer][HandleModify()] modify end");
        }
        
        public Task HandleModifyAsync(Socket client, CommonHeader header)
        {
            return Task.Run(() => HandleModify(client, header));
        }

        public async void HandleError(Socket client, CommonHeader header)
        {
            CommonHeader responseHeader = new CommonHeader(header.Type, MessageState.Error,Constants.None, new Cookie(), new UserInfo() );
            await NetworkManager.SendAsync(client, responseHeader);
        }
        public Task HandleErrorAsync(Socket client, CommonHeader header)
        {
            return Task.Run(()=> HandleError(client, header));
        }
    }
}
