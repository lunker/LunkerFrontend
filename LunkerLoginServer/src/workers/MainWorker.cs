using log4net;
using LunkerLibrary.common.protocol;
using LunkerLibrary.common.Utils;
using LunkerLoginServer.src.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        private Dictionary<ServerInfo, Socket> feConnectionDic = null;

        private List<Socket> readSocketList = null;
        private List<Socket> writeSocketList = null;
        private List<Socket> errorSocketList = null;

        private Task<Socket> clientAcceptTask = null;
        private Task<Socket> feAcceptTask = null;

        private Socket clientListener = null;
        private Socket feListener = null;
        private Socket beSocket = null;
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

            // parallel
            ConnectBEAsync();
            AcceptFEConnectAsync();


            // request initial FE Info
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

        /// <summary>
        /// <para>Initialize variable</para>
        /// </summary>
        public void Initialize()
        {
            clientConnection = new List<Socket>();

            feConnectionDic = new Dictionary<ServerInfo, Socket>();

            readSocketList = new List<Socket>();
            writeSocketList = new List<Socket>();
            errorSocketList = new List<Socket>();

            // initialiize client socket listener
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, AppConfig.GetInstance().FrontPort);
            IPEndPoint feListenEndPoint = new IPEndPoint(IPAddress.Any, 43340);


            clientListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            clientListener.Bind(ep);
            clientListener.Listen(AppConfig.GetInstance().Backlog);

            feListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            feListener.Bind(feListenEndPoint);
            feListener.Listen(AppConfig.GetInstance().Backlog);
        }

        /// <summary>
        /// Connect to BE Server
        /// </summary>
        public Task ConnectBEAsync()
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
                                IPEndPoint beEndPoint = new IPEndPoint(Dns.GetHostEntry(Constants.BeServer).AddressList[0], 50010);
                                beSocket.Connect(beEndPoint);
                            }
                        }
                        else
                        {
                            beSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                            IPEndPoint beEndPoint = new IPEndPoint(Dns.GetHostEntry(Constants.BeServer).AddressList[0], 50010);
                            beSocket.Connect(beEndPoint);
                        }
                    }
                    catch (SocketException se)
                    {
                        continue;
                    }
                }// end loop
 
            });
        }// end method 
        
        public Task AcceptFEConnectAsync()
        {
            return Task.Run(  async ()=>
            {
                /*
                feListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint feEndPoint = new IPEndPoint(Dns.GetHostEntry(Constants.SocketServer).AddressList[0], 43320);
                feListener.Connect(feEndPoint);
                */

                while (true)
                {
                    Socket tmp = feListener.Accept();

                    // Add accepted connections
                    clientConnection.Add(tmp);

                    // Request FE Server Info 
                    // await 할 필요가 있나 ?
                    await RequestFEInfoAsync(tmp);

                    // add fe count
                    LoadBalancer.AddFE();

                    // FE Info Setup이 끝나면, 다음 accept 수행
                    feAcceptTask = Task.Run(() => {
                        return feListener.Accept();
                    });


                }
               
            });
        }

        public void MainProcess()
        {
            logger.Debug("[ChatServer][MainProcess()] start");

            HandleClientAcceptAsync();
            HandleFEAcceptAsync();

            while (threadState)
            {
                // Accept Client Connection Request 
                // Listen . . .

                // 접속한 client가 있을 경우에만 수행.
                if (0 != clientConnection.Count)
                {
                    //logger.Debug("[ChatServer][HandleRequest()] 0 != ");

                    readSocketList = clientConnection.ToList();
                    
                    // Check Inputs 
                    // select target : client
                    Socket.Select(readSocketList, writeSocketList, errorSocketList, 0);

                    // Request가 들어왔을 경우 
                    if (readSocketList.Count != 0)
                    {
                        foreach (Socket peer in readSocketList)
                        {
                            logger.Debug("[ChatServer][HandleRequest()] in socket.select");
                            HandleRequest(peer);
                        }
                    }
                }// end if
                
                // check fe socket connection for read
                if(0 != feConnectionDic.Count)
                {
                    readSocketList.Concat(feConnectionDic.Values.ToList()); // 
                }

                readSocketList.Clear();
            }// end loop 
        }// end method
        
        public Task HandleClientAcceptAsync()
        {
            return Task.Run(()=> {

                while (true)
                {
                    Socket client = clientListener.Accept();

                    logger.Debug("[ChatServer][HandleClientAcceptAsync()] complete accept task. Restart");

                    // Add accepted connections
                    clientConnection.Add(client);
                }
            });
        }

        public Task HandleFEAcceptAsync()
        {
            return Task.Run(()=> {
                //feListenr = new Socket(SocketType.Stream, ProtocolType.Tcp);

                while (true)
                {

                    Socket socket = feListener.Accept();
                    logger.Debug("[ChatServer][HandleFEAcceptAsync()] complete accept task. Restart");

                    // Add accepted connections
                    clientConnection.Add(socket);

                    // Request FE Server Info 
                    // await 할 필요가 있나 ?
                    RequestFEInfoAsync(socket);

                    // add fe count
                    LoadBalancer.AddFE();
                }

            });
        }

        public Task RequestFEInfoAsync(Socket feSocket)
        {
            return Task.Run(() => {
                logger.Debug("[LoginServer][RequestFEInfoAsync()] start");
                CommonHeader requestHeader = new CommonHeader(MessageType.FENotice, MessageState.Request, Constants.None, new Cookie(), new UserInfo());
                NetworkManager.SendAsync(feSocket, requestHeader);
            });
        }
        // 요청을 읽고, 작업을 처리하는 비동기 작업을 만들어야함!!!
        // 여기에서 case나눠서 처리 !!!!
        public async void HandleRequest(Socket peer)
        {
            // peer : client 
            if (peer != null && peer.Connected)
            {
                try
                {
                    logger.Debug("[ChatServer][HandleRequest()] handle client request start");
                    // 정상 연결상태 
                    //CommonHeader header = (CommonHeader)NetworkManager.ReadAsync(peer, 8, typeof(CommonHeader));
                    CommonHeader header = (CommonHeader)await NetworkManager.ReadAsync(peer, Constants.HeaderSize, typeof(CommonHeader));
                    switch (header.Type)
                    {
                        
                        case MessageType.FENotice:
                            await HandleFENoticeAsync(peer, header);
                            break;

                        case MessageType.Signin:
                            await HandleSigninAsync(peer, header);
                            break;
                        case MessageType.Signup:
                            await HandleSignupAsync(peer, header);
                            break;
                        case MessageType.Delete:
                            await HandleDeleteAsync(peer, header);
                            break;
                        case MessageType.Modify:
                            await HandleModifyAsync(peer, header);
                            break;
                        // default
                        default:
                            await HandleErrorAsync(peer, header);
                            break;
                    }
                }
                catch (Exception e)
                {
                    // handling .
                }
               
            }
            else
            {
                
            }
        }// end method

        /// <summary>
        /// <para>Handle FE Service Info</para>
        /// <para>1) read body from fe</para>
        /// <para>2) save </para>
        /// <para>3) send response to fe</para>
        /// <para></para>
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="header"></param>
        public async void HandleFENotice(Socket feSocket, CommonHeader header)
        {
            // 1) 
            LCFENoticeResponseBody responseBody =  (LCFENoticeResponseBody)await NetworkManager.ReadAsync(feSocket, header.BodyLength, typeof(LCFENoticeResponseBody));

            // 2) 
            feConnectionDic.Add(responseBody.ServerInfo, feSocket);

            // 3) 
            CommonHeader responseHeader = new CommonHeader(MessageType.FENotice, MessageState.Response, Constants.None, new Cookie(), new UserInfo());
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
            UserInfo signinUser;
            Cookie cookie;

            // read body from client
            CLSigninRequestBody body = (CLSigninRequestBody) await NetworkManager.ReadAsync(client, header.BodyLength, typeof(CLSigninRequestBody));
            signinUser = body.UserInfo;
           
            CommonHeader requestHeader = new CommonHeader();
            /*
                묶어야함!!
             */
            // send header+body to be
            await NetworkManager.SendAsync(beSocket, requestHeader);
            await NetworkManager.SendAsync(beSocket, body);

            /***
             * 위험요소! 
             */
            // read header from be 
            CommonHeader responseHeader = (CommonHeader) await NetworkManager.ReadAsync(beSocket, Constants.HeaderSize, typeof(CommonHeader) );

            // read body from be
            // cookie in body 
            LBSigninResponseBody responseBody = (LBSigninResponseBody) await NetworkManager.ReadAsync(beSocket, responseHeader.BodyLength, typeof(LBSigninResponseBody));
            cookie = responseBody.Cookie;
            
            // send result to client
            await NetworkManager.SendAsync(client, responseHeader);
            await NetworkManager.SendAsync(client, responseBody);

            // !!!!!! loadbalancing 
            
            // send auth to Chat Server
            CommonHeader feRequestHeader = new CommonHeader(MessageType.SendAuthToChatServer, MessageState.Request, Constants.HeaderSize, new Cookie(), new UserInfo());
            LCUserAuthRequestBody feRequestBody = new LCUserAuthRequestBody(cookie, signinUser);

            // LoadBalancing
            // pick FE 
            int index = LoadBalancer.RoundRobin();
  
            CLSigninResponseBody clientResponseBdoy = new CLSigninResponseBody();
            byte[] bodyArr = NetworkManager.StructureToByte(clientResponseBdoy);
            CommonHeader clientResponseHeader = new CommonHeader(MessageType.Signin, MessageState.Response, bodyArr.Length, new Cookie(), new UserInfo());

            // select FE Server to connect with client
            await NetworkManager.SendAsync(feConnectionDic.ElementAt(index).Value, clientResponseHeader);
            await NetworkManager.SendAsync(feConnectionDic.ElementAt(index).Value, bodyArr);
        }
        
        public Task HandleSigninAsync(Socket client, CommonHeader header)
        {
            return Task.Run(()=> HandleSignin(client, header));
        }

        public async void HandleSignup(Socket client, CommonHeader header)
        {
            // read body from client
            CLSignupRequestBody requestBody = (CLSignupRequestBody) await NetworkManager.ReadAsync(client, header.BodyLength, typeof(CLSignupRequestBody));

            // create request header
            // send request header to be
            await NetworkManager.SendAsync(beSocket, header);

            // send request body to be 
            await NetworkManager.SendAsync(beSocket, requestBody);

            // read header from be
            CommonHeader responseHeader = (CommonHeader) await NetworkManager.ReadAsync(beSocket, Constants.HeaderSize, typeof(CommonHeader));

            // send result(header) to client 
            await NetworkManager.SendAsync(client, responseHeader);
        }

        public Task HandleSignupAsync(Socket client, CommonHeader header)
        {
            return Task.Run(()=>HandleSignup(client, header));
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
            // 1) 
            CLDeleteRequestBody clRequestBody = (CLDeleteRequestBody) await NetworkManager.ReadAsync(client, header.BodyLength, typeof(CLDeleteRequestBody));

            // 2)
            await NetworkManager.SendAsync(beSocket, header );
            await NetworkManager.SendAsync(beSocket, clRequestBody);

            // 3)
            CommonHeader responseHeader =(CommonHeader)await NetworkManager.ReadAsync(beSocket, Constants.HeaderSize, typeof(CommonHeader));

            // 4) 
            await NetworkManager.SendAsync(client, responseHeader);
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
            // 1) 
            CLModifyRequestBody clRequestBody = (CLModifyRequestBody) await NetworkManager.ReadAsync(client, header.BodyLength, typeof(CLModifyRequestBody));

            // 2)
            await NetworkManager.SendAsync(beSocket, header);
            await NetworkManager.SendAsync(beSocket, clRequestBody);

            // 3)
            CommonHeader responseHeader = (CommonHeader)await NetworkManager.ReadAsync(beSocket, Constants.HeaderSize, typeof(CommonHeader));

            // 4) 
            await NetworkManager.SendAsync(client, responseHeader);
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
