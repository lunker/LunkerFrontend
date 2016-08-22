using log4net;
using LunkerChatAdminTool.src.utils;
using LunkerLibrary.common.protocol;
using LunkerLibrary.common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace LunkerChatAdminTool.src
{
    public class AdminTool
    {
        private ILog logger = AdminLogger.GetLoggerInstance();
        private bool appState = Constants.AppRun;
        private Socket agentListener = null;

        private Task<Socket> acceptAgentTask = null; // socket listen

        private Task<string> consoleAgentSelectTask = null; // agent
        private Task<string> consoleRequestSelectTask = null; // request 
        private Task consoleInputTask = null; // agent + request

        private Task printUITask = null;
        private CancellationTokenSource source = new CancellationTokenSource();

        private Dictionary<Socket, AgentInfo> agentSocketList = null;

        private List<Socket> readSocketList = null;
        private List<Socket> writeSocketList = null;
        private List<Socket> errorSocketList = null;

        private static AdminTool instance = null;

        private int selectedAgent = -1;
        private int selectedRequest = -1;

        private int currentLine = 0;
        private int maxLine = 30;

        private int uiState = Constants.InitialState; // 초기 state
        private bool isBlocked = Constants.ConsoleNonBlock;

        private int AdminMode = Constants.Admin;

        private Task adminModeTask = null;

        private AdminTool() { }
        public static AdminTool GetInstance()
        {
            if(instance == null)
            {
                instance = new AdminTool();
            }
            return instance;
        }

        public void Start()
        {
            Initialize();
            MainProcess();
        }
        public void Stop()
        {
            appState = Constants.AppStop;
        }

        public Task AdminModeTask()
        {
            /*
            return Task.Delay(2000).ContinueWith((parent) => {
                PrintAdminModeUI();
            });
            */
            return Task.WhenAll(
                Task.Delay(2000),
                Task.Run(()=> PrintAdminModeUI())
                );
        }

        /// <summary>
        /// 1) send admin request // UI 
        /// 2) handle response  // select read 
        /// 3) accept 
        /// </summary>
        public async void MainProcess()
        {
            Task.Run(()=> {
                while (true)
                {
                    Socket tmp = agentListener.Accept();
                    agentSocketList.Add(tmp, default(AgentInfo));
                }
            });

            Task.Run(()=> {
                while (true)
                {
                    if (0 != agentSocketList.Count)
                    {
                        readSocketList = agentSocketList.Keys.ToList();

                        Socket.Select(readSocketList, writeSocketList, errorSocketList, 0);
                        // data is ready
                        if (0 != readSocketList.Count)
                        {
                            logger.Debug("[Admin][MainProcess()] socket.select success ");
                            foreach (Socket agent in readSocketList)
                            {
                                try
                                {
                                    HandleAgentResponse(agent);
                                }
                                catch (SocketException se)
                                {
                                    logger.Debug("[Admin][MainProcess()]HandleAgentResponse error");
                                    agentSocketList.Remove(agent);
                                }
                            }
                        }
                    }// end select read if 
                }
            });

            // Print UI
            while (true)
            {
                // print admin mode UI 
                // mode for admin
                if (AdminMode == Constants.Admin)
                {
                    /*
                    if (adminModeTask != null)
                    {
                        if (adminModeTask.IsCompleted)
                        {
                            adminModeTask = AdminModeTask();
                        }
                    }
                    else
                    {
                        adminModeTask = AdminModeTask();
                    }
                    */
                    PrintAdminModeUI();
                }
                // mode for monitoring 
                else if(AdminMode == Constants.Monitoring)
                {
                    Console.WriteLine("wow monitoring! ");
                }
                else
                {
                    // lobby
                    Console.WriteLine("\n1. Admin 2. Monitoring");
                    Console.Write("Select Menu :");
                    string menu = "";
                    if (TryReadLine(out menu) != KeyType.Exit)
                    {
                        AdminMode = int.Parse(menu);
                    }
                    Console.Clear();
                }
            }// end while
        }// end method
        
        /// 예외처리해야함
        /// </summary>
        public void Initialize()
        {
            agentSocketList = new Dictionary<Socket, AgentInfo>();

            readSocketList = new List<Socket>();
            writeSocketList = new List<Socket>();
            errorSocketList = new List<Socket>();


            agentListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 43330); 

            agentListener.Bind(endPoint);
            agentListener.Listen(100);
        }

        public void PrintAgentInfo()
        {
            // index, state, ip, port 
            const string format = "[{0}] [{1}] {2} : {3}";
            int idx = 0;
            Console.Clear();
            Console.WriteLine("---------------------------------------------------------");
            Console.WriteLine(format, "index", "state", "ip", "port");
            foreach (AgentInfo agent in agentSocketList.Values.ToList()){
                currentLine++;
                Console.WriteLine(format, idx++, agent.ServerState, agent.ServerInfo.GetPureIp(), agent.ServerInfo.Port);
            }
            Console.WriteLine("---------------------------------------------------------");
        }

        public void PrintRequest()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("[1] : Start Application [2] : Shutdown Application [3] : Restart Application");
        }

        public void PrintAdminModeUI()
        {
            for (int moveState = 1; moveState <= uiState; moveState = moveState << 1)
            {
                switch (moveState)
                {
                    // 1이면, 1을 출력해야한다! 
                    case (int)UIState.PrintAgentInfo:
                        PrintAgentInfo();
                        break;
                    case (int)UIState.PrintCommandInfo:
                        PrintRequest();
                        break;
                    case (int)UIState.PrintSelectCommandInfo:
                        Console.Write("command : ");
                        if (moveState != uiState && selectedRequest != -1)
                        {
                            Console.Write(selectedRequest);
                        }
                        break;
                    case (int)UIState.GetUserCommandInfo:
                        if (moveState == uiState && !isBlocked)
                        {

                            string request = "";
                            isBlocked = Constants.ConsoleBlock;
                            // Exit
                            // GOTO Lobby
                            if (TryReadLine(out request) == KeyType.Exit)
                            {
                                Console.Clear();
                                AdminMode = Constants.Lobby;
                                isBlocked = Constants.ConsoleNonBlock;
                                break;
                            }
                            else
                            {
                                if (int.TryParse(request, out selectedRequest) && selectedRequest <= 3)
                                {
                                    // Success
                                    // Move to Next State
                                    isBlocked = Constants.ConsoleNonBlock;
                                    uiState = uiState << 2;
                                }
                                else
                                {
                                    // retry
                                    ResetVariable();
                                    isBlocked = Constants.ConsoleNonBlock;
                                }
                            }
                        }// end if
                        break;
                    case (int)UIState.PrintSelectAgentInfo:
                        Console.Write("agent : ");
                        if (moveState != uiState && selectedAgent != -1)
                        {
                            Console.Write(selectedAgent);
                        }
                        break;
                    case (int)UIState.GetUserAgnetInfo:
                        if (moveState == uiState && !isBlocked)
                        {
                            string agent = "";
                            isBlocked = Constants.ConsoleBlock;

                            // Exit
                            // GOTO Lobby
                            if (TryReadLine(out agent) == KeyType.Exit)
                            {
                                Console.Clear();
                                AdminMode = Constants.Lobby;
                                isBlocked = Constants.ConsoleNonBlock;
                                break;
                            }
                            else
                            {
                                // get right input from user
                                if (int.TryParse(agent, out selectedAgent) && selectedAgent < agentSocketList.Count)
                                {
                                    isBlocked = Constants.ConsoleNonBlock;
                                    uiState = Constants.InitialState;
                                    Console.WriteLine(selectedRequest + ":" + selectedAgent); // 0 : -1 ? 

                                    switch (selectedRequest)
                                    {
                                        case 1:
                                            SendAdminRequest(MessageType.StartApp, agentSocketList.ElementAt(selectedAgent).Key);
                                            break;
                                        case 2:
                                            SendAdminRequest(MessageType.ShutdownApp, agentSocketList.ElementAt(selectedAgent).Key);
                                            break;
                                        case 3:
                                            SendAdminRequest(MessageType.RestartApp, agentSocketList.ElementAt(selectedAgent).Key);
                                            break;
                                    }
                                    ResetVariable();
                                }
                                else
                                {
                                    ResetVariable();

                                    // get input retry
                                    isBlocked = Constants.ConsoleNonBlock;
                                }
                            }
                        }
                        // end break;
                        break;
                }// end switch
            }// end loop
        }

        public void PrintLobbyUI()
        {

        }

        public void PrintMonitoringUI()
        {

        }

        public Task<string> ConsoleInputTask()
        {
            return Task.Run(()=>{
                string input = Console.ReadLine();
                isBlocked = Constants.ConsoleNonBlock;
                return input;
            });
        }

        public KeyType TryReadLine(out string result)
        {
            var buf = new StringBuilder();
            for (;;)
            {
                //exit
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                {
                    result = "";
                    return KeyType.Exit;
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    result = buf.ToString();
                    return KeyType.Success;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (buf.Length > 0)
                    {
                        buf.Remove(buf.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else if (key.KeyChar != 0)
                {
                    buf.Append(key.KeyChar);
                    Console.Write(key.KeyChar);
                }
            }
        }

        public void ResetVariable()
        {
            selectedAgent = -1;
            selectedRequest = -1;
        }

        /// <summary>
        /// <para>Send Admin request </para>
        /// </summary>
        /// <returns></returns>
        public void SendAdminRequest(MessageType type, Socket agentSocket)
        {
            logger.Debug("[Admin][SendAdminRequest()] start");
            try
            {
                switch (type)
                {
                    case MessageType.StartApp:
                        HandleStartAppRequestAsync(agentSocket);
                        break;
                    case MessageType.ShutdownApp:
                        HandleShutdownAppRequestAsync(agentSocket);
                        break;

                    case MessageType.RestartApp:
                        HandleReStartAppRequestAsync(agentSocket);
                        break;
                    default:
                        break;
                }// end switch
            }
            catch (SocketException se)
            {
                // disconnect handlign
                agentSocketList.Remove(agentSocket);
                logger.Debug("[Admin][SendAdminRequest()] agent disconnected");
            }
            logger.Debug("[Admin][SendAdminRequest()] end");
        }// end method 

        /// <summary>
        /// start, shutdown, restart의 resonse = header만 존재 
        /// </summary>
        /// <param name="agentSocket"></param>
        public async void HandleAgentResponse(Socket agentSocket)
        {
            logger.Debug("[Admin][HandleAgentResponse()] start");
            // read response form agent
            try
            {
                AAHeader responseHeader = (AAHeader)await NetworkManager.ReadAsync(agentSocket, Constants.AdminHeaderSize, typeof(AAHeader));

                switch (responseHeader.Type)
                {
                    // agent
                    case MessageType.AgentInfo:
                        await HandleAgentInfoResponseAsync(agentSocket, responseHeader);
                        //PrintAdminModeUI();
                        break;
                    case MessageType.StartApp:
                        await HandleStartAppResponseAsync(agentSocket, responseHeader);
                        //PrintAdminModeUI();
                        break;
                    case MessageType.ShutdownApp:
                        await HandleShutdownAppResponseAsync(agentSocket, responseHeader);
                        //PrintAdminModeUI();
                        break;

                    case MessageType.RestartApp:
                        await HandleReStartAppResponseAsync(agentSocket, responseHeader);
                        //PrintAdminModeUI();
                        break;

                    // monitoring
                    case MessageType.Total_Room_Count:
                        break;

                    case MessageType.FE_User_Status:
                        break;
                    case MessageType.Chat_Ranking:
                        break;

                    default:
                        break;
                }// end switch
                PrintAdminModeUI();
                logger.Debug("[Admin][HandleAgentResponse()] end");
            }
            catch (SocketException se)
            {
                // connection disconnected.
                return;
            }

        }

        public void HandleStartAppRequestAsync(Socket agentSocket)
        {
            Console.WriteLine("start");
            //logger.Debug("[Admin][HandleStartAppRequestAsync()] start");
            AAHeader requestHeader = new AAHeader(MessageType.StartApp, MessageState.Request, Constants.None);
            NetworkManager.Send(agentSocket, requestHeader);
        }

        public void HandleShutdownAppRequestAsync(Socket agentSocket)
        {
            Console.WriteLine("shutdown");
            //logger.Debug("[Admin][HandleStartAppRequestAsync()] start");
            AAHeader requestHeader = new AAHeader(MessageType.ShutdownApp, MessageState.Request, Constants.None);
            NetworkManager.Send(agentSocket, requestHeader);
        }
        public void HandleReStartAppRequestAsync(Socket agentSocket)
        {
            Console.WriteLine("restart");
            //logger.Debug("[Admin][HandleStartAppRequestAsync()] start");
            AAHeader requestHeader = new AAHeader(MessageType.RestartApp, MessageState.Request, Constants.None);
            NetworkManager.Send(agentSocket, requestHeader);
        }
        ////---------------------------------------------Response---------------------------------------------/////
        
            /// <summary>
            ///  update Server State
            /// </summary>
            /// <param name="agentSocket"></param>
            /// <param name="header"></param>
            /// <returns></returns>
        public Task HandleStartAppResponseAsync(Socket agentSocket, AAHeader header)
        {
            return Task.Run(() => {
                AgentInfo resultAgentInfo = default(AgentInfo);

                if(agentSocketList.TryGetValue(agentSocket, out resultAgentInfo))
                {
                    if (header.State == MessageState.Success)
                    {
                        resultAgentInfo.ServerState = ServerState.Running;
                    }
                    else
                        resultAgentInfo.ServerState = ServerState.Stopped;
                }
                else
                {
                    resultAgentInfo.ServerState = ServerState.Stopped;
                }
                agentSocketList.Remove(agentSocket);
                agentSocketList.Add(agentSocket, resultAgentInfo);
                
            });
        }
        public Task HandleShutdownAppResponseAsync(Socket agentSocket, AAHeader header)
        {
            return Task.Run(() => {
                logger.Debug("[Admin][HandleShutdownAppResponseAsync()] start");
                logger.Debug("[Admin][HandleShutdownAppResponseAsync()] state : " + header.State);

                AgentInfo resultAgentInfo = default(AgentInfo);

                if (agentSocketList.TryGetValue(agentSocket, out resultAgentInfo))
                {
                    if (header.State == MessageState.Success)
                    {
                        resultAgentInfo.ServerState = ServerState.Stopped;
                    }
                    else
                    {

                    }
                }
                else
                {
                    // error . . .
                }
                agentSocketList.Remove(agentSocket);
                agentSocketList.Add(agentSocket, resultAgentInfo);
                logger.Debug("[Admin][HandleShutdownAppResponseAsync()] end");
            });
        }
        public Task HandleReStartAppResponseAsync(Socket agentSocket, AAHeader header)
        {
            return Task.Run(() => {

                AgentInfo resultAgentInfo = default(AgentInfo);

                if (agentSocketList.TryGetValue(agentSocket, out resultAgentInfo))
                {
                    if (header.State == MessageState.Success)
                    {
                        resultAgentInfo.ServerState = ServerState.Running;
                    }
                    else
                    {

                    }
                }
                else
                {
                    // error . . .
                }
                agentSocketList.Remove(agentSocket);
                agentSocketList.Add(agentSocket, resultAgentInfo);
            });
        }


        /// <summary>
        /// Agent가 접속후, 자신의 정보를 보내서 초기화시킨다.
        /// </summary>
        /// <param name="agentSocket"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public Task HandleAgentInfoResponseAsync(Socket agentSocket, AAHeader header)
        {
            return Task.Run(()=> {
                logger.Debug("[Admin][HandleAgentInfoResponseAsync()] start");
                AAAgentInfoRequestBody requestBody = (AAAgentInfoRequestBody)NetworkManager.Read(agentSocket, header.BodyLength, typeof(AAAgentInfoRequestBody));
                logger.Debug("[Admin][HandleAgentInfoResponseAsync()] ip : " + requestBody.AgentInfo.ServerInfo.GetPureIp());
                logger.Debug("[Admin][HandleAgentInfoResponseAsync()] port :" + requestBody.AgentInfo.ServerInfo.Port);

                if (agentSocketList.ContainsKey(agentSocket))
                {
                    agentSocketList.Remove(agentSocket);
                    agentSocketList.Add(agentSocket, requestBody.AgentInfo);
                    logger.Debug("[Admin][HandleAgentInfoResponseAsync()] key contains");
                }
                else
                {
                    agentSocketList.Add(agentSocket, requestBody.AgentInfo);
                    logger.Debug("[Admin][HandleAgentInfoResponseAsync()] no keys");
                }
                logger.Debug("[Admin][HandleAgentInfoResponseAsync()] end");
            });
            
            
        }
      
    }
}
