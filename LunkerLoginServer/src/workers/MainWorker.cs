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
        private List<Socket> readSocketList = null;
        private List<Socket> writeSocketList = null;
        private List<Socket> errorSocketList = null;

        private Task<Socket> getAcceptTask = null;
        private Socket sockListener = null;

        private Socket beSocket = null;
        private Socket feSocket = null;

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
            logger.Debug("[ChatServer][MainWorker][Start()] start");

            Initialize();
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
        public void Initialize()
        {
            readSocketList = new List<Socket>();
            writeSocketList = new List<Socket>();
            errorSocketList = new List<Socket>();

            // initialiize client socket listener
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, AppConfig.GetInstance().FrontPort);

            sockListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sockListener.Bind(ep);
            sockListener.Listen(AppConfig.GetInstance().Backlog);

            ConnectBEAsync();
            ConnectFEAsync();
        }

        /// <summary>
        /// 
        /// Connect to BE Server
        /// </summary>
        public void ConnectBEAsync()
        {
            beSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint beEndPoint = new IPEndPoint(IPAddress.Parse("100.100.58.6"), 50010);

            Task connectTask = Task.Factory.FromAsync(beSocket.BeginConnect(beEndPoint, null, beSocket), beSocket.EndConnect);
            connectTask.Wait();
        }// end method 


        public void ConnectFEAsync()
        {
            feSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint feEndPoint = new IPEndPoint(IPAddress.Parse("10.10.0.157"), 43320);

            Task connectTask = Task.Factory.FromAsync(beSocket.BeginConnect(feEndPoint, null, beSocket), beSocket.EndConnect);
            connectTask.Wait();
        }

        public void MainProcess()
        {
            logger.Debug("[ChatServer][HandleRequest()] start");
            while (threadState)
            {
                // Accept Client Connection Request 
                HandleAccept();
                // 접속한 client가 있을 경우에만 수행.
                if (0 != clientConnection.Count)
                {
                    readSocketList = clientConnection.ToList();
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
                readSocketList.Clear();
            }// end loop 
        }// end method

        public void HandleAccept()
        {
            if (getAcceptTask != null)
            {
                if (getAcceptTask.IsCompleted)
                {
                    logger.Debug("[ChatServer][HandleRequest()] complete accept task. Restart");

                    // Add accepted connections
                    clientConnection.Add(getAcceptTask.Result);
                 
                    // 다시 task run 
                    getAcceptTask = Task.Factory.FromAsync(sockListener.BeginAccept, sockListener.EndAccept, true);
                }
            }
            else
            {
                logger.Debug("[ChatServer][HandleRequest()] start accept task ");
                getAcceptTask = Task.Factory.FromAsync(sockListener.BeginAccept, sockListener.EndAccept, true);
                //getAcceptTask.Start();
            }
        }

        // 요청을 읽고, 작업을 처리하는 비동기 작업을 만들어야함!!!
        // 여기에서 case나눠서 처리 !!!!
        public async void HandleRequest(Socket peer)
        {
            // peer : client 
            if (peer != null && peer.Connected)
            {
                // 정상 연결상태 
                // 일단 CCHeader로 전체 header 사용 
                //CommonHeader header = (CommonHeader)NetworkManager.ReadAsync(peer, 8, typeof(CommonHeader));
                CommonHeader header = (CommonHeader)await NetworkManager.ReadAsync(peer, Constants.HeaderSize, typeof(CommonHeader));
                switch (header.Type)
                {
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
            LCNotifyUserRequestBody feRequestBody = new LCNotifyUserRequestBody(cookie, signinUser);

            await NetworkManager.SendAsync(feSocket, feRequestHeader);
            await NetworkManager.SendAsync(feSocket, feRequestBody);
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
