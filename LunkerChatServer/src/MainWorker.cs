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

        private Socket sockListener = null;

        private ConnectionManager connectionManager = null;
        private List<Socket> readSocketList = null;
        private List<Socket> writeSocketList = null;
        private List<Socket> errorSocketList = null;

        private Socket beSocket = null;
        private Socket loginSocket = null;

        private Task<Socket> getAcceptTask = null;

        //private ChatWorker chatWorker;
        private BEWorker beWorker = BEWorker.GetInstance();

        private MainWorker()
        {

        }

        public static MainWorker GetInstance()
        {
            if (mainWorker == null)
            {
                mainWorker = new MainWorker();
            }
            return mainWorker;
        }

        // chat server main thread
        public void Start()
        {
            logger.Debug("[ChatServer][MainWorker][Start()] start");
            Initialize();
            InitializebeSocket();
            InitializeLoginSocket();

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
                HandleAccept();

                // 접속한 client가 있을 경우에만 수행.
                if (0 != connectionManager.GetClientConnectionCount())
                {
                    // select client connection
                    readSocketList = connectionManager.GetClientConnectionDic().Values.ToList();
                    // select login coinnection
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

        public void HandleAccept()
        {
            if (getAcceptTask != null)
            {
                if (getAcceptTask.IsCompleted)
                {
                    logger.Debug("[ChatServer][HandleRequest()] complete accept task. Restart");

                    // Add accepted connections
                    // getAcceptTask.Result;
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
            if(peer!=null && peer.Connected)
            {
                // 정상 연결상태 
                // 일단 CCHeader로 전체 header 사용 
                CommonHeader header = (CommonHeader) NetworkManager.ReadAsync(peer, Constants.HeaderSize, typeof(CommonHeader));
 
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

                        /*
                        if(header.State == MessageState.Request)
                        {
                            // send create request
                            //ChatWorker.HandleCreateRoomRequest();
                            await HandleCreateRoomRequestAsync(peer, header);
                            break;
                        }
                        else
                        {
                            //connectionManager.GetClientConnection();
                            await HandleCreateRoomResponseAsync(peer, header);
                            break;
                        }
                        */

                    /*
                    case MessageType.JoinRoom:
                        if (header.State == MessageState.Request)
                        {
                            break;
                        }
                        else
                        {
                            break;
                        }
                    */

                    case MessageType.LeaveRoom:
                        /*
                        if (header.State == MessageState.Request)
                        {

                            break;
                        }
                        else
                        {
                            break;
                        }
                        */

                    // not yet 
                    case MessageType.ListRoom:
                        break;

                        // default
                    default:
                        break;
                }
            }
            else
            {

            }
        }

        public void Initialize()
        {
            connectionManager = ConnectionManager.GetInstance();

            readSocketList = new List<Socket>();
            writeSocketList = new List<Socket>();
            errorSocketList = new List<Socket>();

            // initialiize client socket listener
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, AppConfig.GetInstance().FrontPort);

            sockListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sockListener.Bind(ep);
            sockListener.Listen(AppConfig.GetInstance().Backlog);

        }// end method 

        public void InitializebeSocket()
        {
            beSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(AppConfig.GetInstance().BackendServerIp), AppConfig.GetInstance().BackendServerPort);

            beSocket.Connect(ep);
        }

        public void InitializeLoginSocket()
        {
            loginSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(AppConfig.GetInstance().LoginServerIp), AppConfig.GetInstance().LoginServerPort);

            loginSocket.Connect(ep);
        }

        public async void GetClientRequest()
        {
            //logger.Debug("[ChatServer][GetClientRequest()] start");
            Socket handler = await AcceptAsync();
            logger.Debug("[ChatServer][GetClientRequest()] get client conenction ");

            IPEndPoint ep = (IPEndPoint)handler.RemoteEndPoint;
            StringBuilder idBuilder = new StringBuilder();
            idBuilder.Append(ep.Address);
            idBuilder.Append(":");
            idBuilder.Append(ep.Port);

            //clientSocketDic.Add(idBuilder.ToString(), handler);

            logger.Debug("[ChatServer][AcceptAsync()] add handler to list");
            return;
        }

        public async Task<Socket> AcceptAsync()
        {
            //logger.Debug("[ChatServer][GetClientRequest()][AcceptAsync()] start accept");
            Socket handler = await Task.Factory.FromAsync(sockListener.BeginAccept, sockListener.EndAccept, true);

            logger.Debug("[ChatServer][GetClientRequest()][AcceptAsync()] accept client request");
            return handler;

            /*
            Task delay = Task.Delay(TimeSpan.FromSeconds(5));
            var result = await Task.WhenAny(delay, Task.Factory.FromAsync(sockListener.BeginAccept, sockListener.EndAccept, true)).ConfigureAwait(false);

       
            if (result == delay)
            {
                // timeout
                return null;
            }
            else
            {
                return (Task)result.;
            }
            */
            /*
            Socket handler = await Task.Factory.FromAsync(sockListener.BeginAccept, sockListener.EndAccept, true);

            logger.Debug("[ChatServer][GetClientRequest()][AcceptAsync()] accept client request");
            return handler;
            */
            /*
            IPEndPoint ep = (IPEndPoint)handler.RemoteEndPoint;
            StringBuilder idBuilder = new StringBuilder();
            idBuilder.Append(ep.Address);
            idBuilder.Append(":");
            idBuilder.Append(ep.Port);

            clientSocketDic.Add(idBuilder.ToString(), handler);

            logger.Debug("[ChatServer][AcceptAsync()] accept client request");
            return;
            */
        }// end method

        /// <summary>
        /// <para>1) read body from login server</para>
        /// <para>2) save auth info in structure</para>
        /// <para></para>
        /// <para></para>
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="header"></param>
        public void HandleConnectionSetup(Socket peer, CommonHeader header)
        {
            // 1)
            LCNotifyUserRequestBody requestBody = (LCNotifyUserRequestBody) NetworkManager.ReadAsync(peer, header.BodyLength, typeof(LCNotifyUserRequestBody));
            
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
            
            // read message
            await NetworkManager.ReadAsyncTask(peer, header.BodyLength, ref messageBuff);
                
            // Get User Entered Room 
            ChattingRoom enteredRoom = connectionManager.GetChattingRoomJoinInfo(new string(header.UserInfo.Id)); // room info ~ user id 

            // broadcast
            Socket client = null;
            foreach (string user in connectionManager.GetChattingRoomListInfo(enteredRoom))
            {
                client = connectionManager.GetClientConnection(user);

                // broadcast to each client
                await NetworkManager.SendAsyncTask(client, messageBuff);
            }

            // Send chatting to BE 
            string sendingUser = new string(header.UserInfo.Id);

            // worker에게 위임? 
            beWorker.HandleChatting(header);
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
            await NetworkManager.SendAsyncTask(beSocket,header);

            // 2) read response(header, body) from BE
            CommonHeader responseHeader = (CommonHeader) NetworkManager.ReadAsync(beSocket, Constants.HeaderSize, typeof(CommonHeader));
            CBCreateRoomResponseBody responseBody = (CBCreateRoomResponseBody) NetworkManager.ReadAsync(beSocket, responseHeader.BodyLength, typeof(CBCreateRoomResponseBody));

            // 3) send response(header, body) to client
            await NetworkManager.SendAsyncTask(peer, responseHeader);
            await NetworkManager.SendAsyncTask(peer, responseBody);
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
            await NetworkManager.SendAsyncTask(beSocket, header);

            // 2) 
            CommonHeader responseHeader = (CommonHeader) NetworkManager.ReadAsync(beSocket, Constants.HeaderSize, typeof(CommonHeader));

            //// 여기 에러 밭 ㅠㅠㅠㅠ 
            CBListRoomResponseBody responseBody = (CBListRoomResponseBody) NetworkManager.ReadAsync(beSocket, responseHeader.BodyLength, typeof(CBListRoomResponseBody));


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
