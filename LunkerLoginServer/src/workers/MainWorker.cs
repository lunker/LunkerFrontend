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
        private ILog logger = Logger.GetLoggerInstance();
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
            await ConnectBEAsync();
            await ConnectFEAsync();
            // request initial FE Info
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

        /// <summary>
        /// <para>Initialize variable</para>
        /// </summary>
        public void Initialize()
        {
            readSocketList = new List<Socket>();
            writeSocketList = new List<Socket>();
            errorSocketList = new List<Socket>();

            // initialiize client socket listener
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, AppConfig.GetInstance().FrontPort);

            clientListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            clientListener.Bind(ep);
            clientListener.Listen(AppConfig.GetInstance().Backlog);
        }

        /// <summary>
        /// Connect to BE Server
        /// </summary>
        public Task ConnectBEAsync()
        {
            return Task.Run(()=> 
            {
                beSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint beEndPoint = new IPEndPoint(IPAddress.Parse("100.100.58.6"), 50010);
                beSocket.Connect(beEndPoint);
            });
        }// end method 
        
        public Task ConnectFEAsync()
        {
            return Task.Run(()=>
            {
                feListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint feEndPoint = new IPEndPoint(IPAddress.Parse(Constants.SocketServer), 43320);
                feListener.Connect(feEndPoint);
            });
        }

        public void MainProcess()
        {
            logger.Debug("[ChatServer][HandleRequest()] start");
            while (threadState)
            {
                // Accept Client Connection Request 
                HandleClientAcceptAsync();
                HandleFEAcceptAsync();

                // 접속한 client가 있을 경우에만 수행.
                if (0 != clientConnection.Count)
                {
                    readSocketList = clientConnection.ToList();
                    

                    // Check Inputs 
                    // select target : client
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
                
                if(0 != feConnectionDic.Count)
                {
                    readSocketList.Concat(feConnectionDic.Values.ToList()); // 
                }
                readSocketList.Clear();
            }// end loop 
        }// end method
        
        public void HandleClientAcceptAsync()
        {
            if ( clientAcceptTask!= null)
            {
                if (clientAcceptTask.IsCompleted)
                {
                    logger.Debug("[ChatServer][HandleRequest()] complete accept task. Restart");

                    // Add accepted connections
                    clientConnection.Add(clientAcceptTask.Result);

                    // 다시 task run 
                    //getAcceptTask = Task.Factory.FromAsync(clientListener.BeginAccept, clientListener.EndAccept, true);
                    clientAcceptTask = Task.Run(()=> {
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

        public async void HandleFEAcceptAsync()
        {
            if (feAcceptTask != null)
            {
                if (feAcceptTask.IsCompleted)
                {
                    logger.Debug("[ChatServer][HandleRequest()] complete accept task. Restart");

                    // Add accepted connections
                    clientConnection.Add(feAcceptTask.Result);

                    // Request FE Server Info 
                    // await 할 필요가 있나 ?
                    RequestFEInfoAsync(feAcceptTask.Result);

                    // FE Info Setup이 끝나면, 다음 accept 수행
                    feAcceptTask = Task.Run(() => {
                        return feListener.Accept();
                    });
                }
            }
            else
            {
                logger.Debug("[ChatServer][HandleRequest()] start accept task ");
                feAcceptTask = Task.Run(() => {
                    return feListener.Accept();
                });
            }
        }

        public Task RequestFEInfoAsync(Socket feSocket)
        {
            return Task.Run(() => {
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
                // 정상 연결상태 
                //CommonHeader header = (CommonHeader)NetworkManager.ReadAsync(peer, 8, typeof(CommonHeader));
                CommonHeader header = (CommonHeader)await NetworkManager.ReadAsync(peer, Constants.HeaderSize, typeof(CommonHeader));
                switch (header.Type)
                {

                    // login에서 request를 날린다.
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
            else
            {
                // peer is disconnected.
                // handling 
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
            CommonHeader feRequestHeader = new CommonHeader(MessageType.Auth, MessageState.Request, Constants.HeaderSize, new Cookie(), new UserInfo());
            LCUserAuthRequestBody feRequestBody = new LCUserAuthRequestBody(cookie, signinUser);

            // select FE Server to connect with client
            await NetworkManager.SendAsync(, feRequestHeader);
            await NetworkManager.SendAsync(, feRequestBody);
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
