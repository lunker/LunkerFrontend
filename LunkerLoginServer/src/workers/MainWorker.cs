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

        private List<Socket> clientConnection = null;

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
                            }
                        }
                        else
                        {
                            beSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                            IPEndPoint beEndPoint = new IPEndPoint(IPAddress.Parse(AppConfig.GetInstance().Backendserverip), AppConfig.GetInstance().Backendserverport);
                            beSocket.Connect(beEndPoint);
                            Console.WriteLine("loginserver : BE connect success");
                        }
                        
                    }
                    catch (SocketException se)
                    {
                        continue;
                    }
                }// end loop
 
            });
        }// end method 

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
                                socketTaskPair.Add(client, Task.Run(() => { }));
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
                            Socket socket = feListener.Accept();
                            logger.Debug("[ChatServer][HandleFEAcceptAsync()] complete accept task. Restart");

                            // Add accepted connections
                            //clientConnection.Add(socket);
                            feConnectionDic.Add(socket, default(ServerInfo));
                            socketTaskPair.Add(socket, Task.Run(() => { }));
                            // Request FE Server Info 
                            // await 할 필요가 있나 ?
                            RequestFEInfoAsync(socket);

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

            while (threadState)
            {
                try
                {
                    if (socketTaskPair.Count != 0)
                    {
                        foreach (Socket peer in socketTaskPair.Keys.ToList())
                        {
                            readSocketList.Add(peer);
                        }

                        Task tmp = null;
                        foreach (Socket peer in readSocketList)
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
                        }
                    }// end if
                    readSocketList.Clear();
                }
                catch (SocketException se)
                {
                    Console.WriteLine("socket select error~!");
                    continue;
                }
            }// end loop 
        }// end method

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
            // peer : client 
            if (peer != null && peer.Connected)
            {
                try
                {
                    
                    //Console.WriteLine("[LoginServer][HandleRequest()] handle client request start");
                    logger.Debug("[LoginServer][HandleRequest()] handle client request start");
                    // 정상 연결상태 
                    //CommonHeader header = (CommonHeader)NetworkManager.ReadAsync(peer, 8, typeof(CommonHeader));

                    CommonHeader header = (CommonHeader) NetworkManager.Read(peer, Constants.HeaderSize, typeof(CommonHeader));
                    Console.WriteLine("정말?"+Marshal.SizeOf(header));

                    Console.WriteLine($"type;{header.Type}");
                    Console.WriteLine($"state;{header.State}");
                    
                    Console.WriteLine($"remote ip;{ ((IPEndPoint)peer.RemoteEndPoint).Address}");
                    Console.WriteLine($"remote port;{((IPEndPoint)peer.RemoteEndPoint).Port}");
                    Console.WriteLine($"local port;{ ((IPEndPoint)peer.LocalEndPoint).Port}");

                    switch (header.Type)
                    {
                        case MessageType.FENotice:
                            Console.WriteLine("[LoginServer][HandleRequest()] FENotice.");
                            //await HandleFENoticeAsync(peer, header);
                             HandleFENotice(peer, header);
                            break;

                        case MessageType.Signin:
                            Console.WriteLine("[LoginServer][HandleRequest()] Signin.");
                            await HandleSigninAsync(peer, header);
                            break;

                        case MessageType.Logout:
                            Console.WriteLine("[LoginServer][HandleRequest()] Logout.");
                            await HandleLogoutAsnyc(peer, header);
                            break;
                        case MessageType.Signup:
                            Console.WriteLine("[LoginServer][HandleRequest()] Signup.");
                            await HandleSignupAsync(peer, header);
                            break;
                        case MessageType.Delete:
                            Console.WriteLine("[LoginServer][HandleRequest()] Delete.");
                            await HandleDeleteAsync(peer, header);
                            break;
                        case MessageType.Modify:
                            Console.WriteLine("[LoginServer][HandleRequest()] Modify.");
                            await HandleModifyAsync(peer, header);
                            break;
                        // default
                        default:
                            await HandleErrorAsync(peer, header);
                            break;
                    }
                    peer.Blocking = true;
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
        }// end method

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

            // 3) 
            /*
            CBServerInfoNoticeResponseBody info = new CBServerInfoNoticeResponseBody(new ServerInfo("10.100.58.3", 43330));
            CommonHeader responseHeader = new CommonHeader(MessageType.FENotice, MessageState.Response, Marshal.SizeOf(info), new Cookie(), new UserInfo());
            NetworkManager.Send(feSocket, responseHeader);
            NetworkManager.Send(feSocket, info);
            */
            
            logger.Debug("[LoginServer][HandleFENotice()] fenotice end");
            Console.WriteLine("[LoginServer][HandleFENotice()] fenotice end");
        }

        public Task HandleFENoticeAsync(Socket peer, CommonHeader header)
        {
            return Task.Run(()=>HandleFENotice(peer, header));
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

            // =====================READ COOKIE FROM BE
            // cookie in body 
            //LBSigninResponseBody responseBody = (LBSigninResponseBody) await NetworkManager.ReadAsync(beSocket, responseHeader.BodyLength, typeof(LBSigninResponseBody));
            LBSigninResponseBody responseBody = (LBSigninResponseBody) NetworkManager.Read(beSocket, responseHeader.BodyLength, typeof(LBSigninResponseBody));
            cookie = responseBody.Cookie;

            // LoadBalancing
            // pick FE 
            int index = LoadBalancer.RoundRobin();

            responseBody.ServerInfo = feConnectionDic.ElementAt(index).Value;

            Console.WriteLine("client에게 보내지는 FE의 정보 : " + responseBody.ServerInfo.GetPureIp() + ":" + responseBody.ServerInfo.Port);
            // send result to client
             NetworkManager.Send(client, responseHeader);
             NetworkManager.Send(client, responseBody);

            // !!!!!! loadbalancing 
            // send auth to Chat Server
            
            LCUserAuthRequestBody feRequestBody = new LCUserAuthRequestBody(cookie, signinUser);
            //byte[] bodyArr = NetworkManager.StructureToByte(feRequestBody);
            CommonHeader feRequestHeader = new CommonHeader(MessageType.NoticeUserAuth, MessageState.Request, Marshal.SizeOf(feRequestBody) , cookie, responseHeader.UserInfo);
            

            //await NetworkManager.SendAsync(feConnectionDic.ElementAt(index).Key, feRequestHeader);
            //await NetworkManager.SendAsync(feConnectionDic.ElementAt(index).Key, feRequestBody);

             NetworkManager.Send(feConnectionDic.ElementAt(index).Key, feRequestHeader);
             NetworkManager.Send(feConnectionDic.ElementAt(index).Key, feRequestBody);

            // select FE Server to connect with client

            logger.Debug("[LoginServer][HandleSignin()] signin end");
            Console.WriteLine("[LoginServer][HandleSignin()] signin end");
        }
        
        public Task HandleSigninAsync(Socket client, CommonHeader header)
        {
            return Task.Run(()=> HandleSignin(client, header));
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
             NetworkManager.Send(beSocket, header);
             NetworkManager.Send(beSocket, clRequestBody);

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
