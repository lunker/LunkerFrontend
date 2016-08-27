using log4net;
using LunkerChatAdminTool.src.utils;
using LunkerLibrary.common.protocol;
using LunkerLibrary.common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

        private CancellationTokenSource source = new CancellationTokenSource();

        private Dictionary<Socket, List<AgentInfo>> agentSocketList = null;

        private List<Socket> readSocketList = null;
        private List<Socket> writeSocketList = null;
        private List<Socket> errorSocketList = null;

        private static AdminTool instance = null;

        private int selectedAgent = -1;
        private int selectedRequest = -1;

        private int uiState = Constants.InitialState; // 초기 state
        private bool isBlocked = Constants.ConsoleNonBlock;

        private int AdminMode = Constants.Admin;

        private int selectedIndex = 0;
        private AdminTool() { }
        public static AdminTool GetInstance()
        {
            if (instance == null)
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

        public Task HandleAgentAccpetAsync()
        {
            return Task.Run(() => {
                while (true)
                {
                    Socket tmp = agentListener.Accept();
                    //tmp.Blocking = false;
                    Console.WriteLine("[Admin] Agent Connected");

                    Task.Run(() => {
                        HandleAgentResponse(tmp);
                    });

                    agentSocketList.Add(tmp, new List<AgentInfo>());
                }
            });
        }

        public async void MainProcess()
        {
            HandleAgentAccpetAsync();


            //=======================================================================
            //================================================== Print Initaial UI
            //=======================================================================
            while (true)
            {
                // print admin mode UI 
                // mode for admin
                if (AdminMode == Constants.Admin)
                {
                    PrintAdminModeUI();
                }
                // mode for monitoring 
                else if (AdminMode == Constants.Monitoring)
                {
                    Console.WriteLine("wow monitoring! ");
                }
                else
                {
                    PrintLobbyUI();
                }
            }// end while
        }// end method

        /// <summary>
        /// Initialize Variable, data Structure
        /// </summary>
        public void Initialize()
        {
            agentSocketList = new Dictionary<Socket, List<AgentInfo>>();

            readSocketList = new List<Socket>();
            writeSocketList = new List<Socket>();
            errorSocketList = new List<Socket>();

            agentListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 43330);

            agentListener.Bind(endPoint);
            agentListener.Listen(100);
        }


        ////====================================================================================================////
        ////====================================================================================================////
        ////============================================Print UI====================================================////
        ////====================================================================================================////
        ////====================================================================================================////
        public void PrintAgentInfo()
        {
            // index, state, ip, port 
            const string format = "| [{0}] [{1}] {2} : {3} |";
            int idx = 0;
            Console.Clear();
            Console.WriteLine("----------------------------------------------------------------");
            Console.WriteLine(format, "index", "state", "ip", "port");
            for (int i = 0; i < agentSocketList.Count; i++)
            {
                List<AgentInfo> agentList = agentSocketList.ElementAt(i).Value;

                foreach (AgentInfo agent in agentList)
                {
                    Console.WriteLine(format, idx++, agent.ServerState, agent.ServerInfo.GetPureIp(), agent.ServerType);
                }
            }
            Console.WriteLine("----------------------------------------------------------------");
        }

        public void PrintRequest()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("----------------------------------------------------------------");
            Console.WriteLine("|            [1] : Start  [2] : Shutdown  [3] : Restart         |");
            Console.WriteLine("----------------------------------------------------------------");
        }

        public void PrintAdminModeUI()
        {
            Console.Clear();
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
                                if (int.TryParse(agent, out selectedAgent) && selectedAgent < agentSocketList.Count * 2)
                                {
                                    isBlocked = Constants.ConsoleNonBlock;
                                    uiState = Constants.InitialState;
                                    Console.WriteLine(selectedRequest + ":" + selectedAgent); // 0 : -1 ? 

                                    int host = 0;
                                    int server = 0;

                                    selectedIndex = selectedAgent;

                                    host = selectedAgent / 2;
                                    server = selectedAgent % 2;

                                    ServerType serverType;
                                    serverType = agentSocketList.ElementAt(host).Value.ElementAt(server).ServerType;

                                    switch (selectedRequest)
                                    {
                                        case 1:
                                            SendAdminRequest(MessageType.StartApp, agentSocketList.ElementAt(host).Key, serverType);
                                            break;
                                        case 2:
                                            SendAdminRequest(MessageType.ShutdownApp, agentSocketList.ElementAt(host).Key, serverType);
                                            break;
                                        case 3:
                                            SendAdminRequest(MessageType.RestartApp, agentSocketList.ElementAt(host).Key, serverType);
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

        public void PrintMonitoringUI()
        {

        }

        public Task<string> ConsoleInputTask()
        {
            return Task.Run(() => {
                string input = Console.ReadLine();
                isBlocked = Constants.ConsoleNonBlock;
                return input;
            });
        }

        public void RefreshUI()
        {
            if (AdminMode == Constants.Admin)
                PrintAdminModeUI();
            else if (AdminMode == Constants.Lobby)
            {
                PrintLobbyUI();
            }
        }

        /// <summary>
        /// ReadLine with key event
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
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

        ////====================================================================================================////
        ////====================================================================================================////
        ////============================================Request====================================================////
        ////====================================================================================================////
        ////====================================================================================================////
        /// <summary>
        /// <para>Send Admin request </para>
        /// </summary>
        /// <returns></returns>
        public void SendAdminRequest(MessageType type, Socket agentSocket, ServerType server)
        {
            logger.Debug("[Admin][SendAdminRequest()] start");
            try
            {
                switch (type)
                {
                    case MessageType.StartApp:
                        Console.WriteLine("start");
                        HandleStartAppRequestAsync(agentSocket, server);
                        break;
                    case MessageType.ShutdownApp:
                        Console.WriteLine("shutdown");
                        HandleShutdownAppRequestAsync(agentSocket, server);
                        break;

                    case MessageType.RestartApp:
                        Console.WriteLine("restart");
                        HandleReStartAppRequestAsync(agentSocket, server);
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
        /// <para>Handle Agent Response</para>
        /// <para>RefereshUI</para>
        /// </summary>
        /// <param name="agentSocket"></param>
        public async void HandleAgentResponse(Socket agentSocket)
        {
            while (true)
            {

                try
                {
                    AAHeader responseHeader = (AAHeader)NetworkManager.Read(agentSocket, Constants.AdminHeaderSize, typeof(AAHeader));

                    switch (responseHeader.Type)
                    {
                        // agent
                        case MessageType.AgentInfo:
                            await HandleAgentInfoResponseAsync(agentSocket, responseHeader);
                            break;
                        case MessageType.StartApp:
                            await HandleStartAppResponseAsync(agentSocket, responseHeader);
                            break;
                        case MessageType.ShutdownApp:
                            await HandleShutdownAppResponseAsync(agentSocket, responseHeader);
                            break;

                        case MessageType.RestartApp:
                            await HandleReStartAppResponseAsync(agentSocket, responseHeader);
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
                    Console.WriteLine("[Admin][HandleAgentResponse()] end");
                }
                catch (NoMessageException nme)
                {
                    Console.WriteLine("no message");
                    continue;
                }
                catch (SocketException se)
                {
                    agentSocketList.Remove(agentSocket);
                    agentSocket.Close();

                    Console.WriteLine("[Admin][HandleAgentResponse()] agent disconnected . . . ");
                    return;
                }
                finally
                {
                    RefreshUI();
                }
            }
        }// end method

        public void HandleStartAppRequestAsync(Socket agentSocket, ServerType server)
        {
            Console.WriteLine("start");
            AACommandRequestBody body = new AACommandRequestBody(server);
            AAHeader requestHeader = new AAHeader(MessageType.StartApp, MessageState.Request, Marshal.SizeOf(body));
            NetworkManager.Send(agentSocket, requestHeader, body);

        }

        public void HandleShutdownAppRequestAsync(Socket agentSocket, ServerType server)
        {
            Console.WriteLine("shutdown");
            AACommandRequestBody body = new AACommandRequestBody(server);
            AAHeader requestHeader = new AAHeader(MessageType.ShutdownApp, MessageState.Request, Marshal.SizeOf(body));


            NetworkManager.Send(agentSocket, requestHeader, body);
        }
        public void HandleReStartAppRequestAsync(Socket agentSocket, ServerType server)
        {
            Console.WriteLine("restart");
            //logger.Debug("[Admin][HandleStartAppRequestAsync()] start");
            AACommandRequestBody body = new AACommandRequestBody(server);
            AAHeader requestHeader = new AAHeader(MessageType.RestartApp, MessageState.Request, Marshal.SizeOf(body));


            NetworkManager.Send(agentSocket, requestHeader, body);
        }


        ////====================================================================================================////
        ////====================================================================================================////
        ////============================================Response====================================================////
        ////====================================================================================================////
        ////====================================================================================================////

        public void HandleStartAppResponse(Socket agentSocket, AAHeader header)
        {
            AgentInfo resultAgentInfo = default(AgentInfo);

            int host = selectedIndex / 2;
            int server = selectedIndex % 2;

            resultAgentInfo = agentSocketList.ElementAt(host).Value.ElementAt(server);
            if (header.State == MessageState.Success)
            {
                resultAgentInfo.ServerState = ServerState.Running;
            }
            else
                resultAgentInfo.ServerState = ServerState.Stopped;


            agentSocketList.ElementAt(host).Value.RemoveAt(server);
            agentSocketList.ElementAt(host).Value.Add(resultAgentInfo);
            selectedIndex = -1;
        }

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

                int host = selectedIndex / 2;
                int server = selectedIndex % 2;

                resultAgentInfo = agentSocketList.ElementAt(host).Value.ElementAt(server);
                if (header.State == MessageState.Success)
                {
                    resultAgentInfo.ServerState = ServerState.Running;
                }
                else
                    resultAgentInfo.ServerState = ServerState.Stopped;

                agentSocketList.ElementAt(host).Value.RemoveAt(server);
                agentSocketList.ElementAt(host).Value.Add(resultAgentInfo);
                selectedIndex = -1;

            });
        }
        public Task HandleShutdownAppResponseAsync(Socket agentSocket, AAHeader header)
        {
            return Task.Run(() => {
                logger.Debug("[Admin][HandleShutdownAppResponseAsync()] start");
                logger.Debug("[Admin][HandleShutdownAppResponseAsync()] state : " + header.State);

                AgentInfo resultAgentInfo = default(AgentInfo);

                int host = selectedIndex / 2;
                int server = selectedIndex % 2;


                resultAgentInfo = agentSocketList.ElementAt(host).Value.ElementAt(server);
                if (header.State == MessageState.Success)
                {
                    resultAgentInfo.ServerState = ServerState.Stopped;
                }
                else
                {

                }

                agentSocketList.ElementAt(host).Value.RemoveAt(server);
                agentSocketList.ElementAt(host).Value.Add(resultAgentInfo);
                selectedIndex = -1;

            });
        }
        public Task HandleReStartAppResponseAsync(Socket agentSocket, AAHeader header)
        {
            return Task.Run(() => {

                AgentInfo resultAgentInfo = default(AgentInfo);

                int host = selectedIndex / 2;
                int server = selectedIndex % 2;


                resultAgentInfo = agentSocketList.ElementAt(host).Value.ElementAt(server);

                if (header.State == MessageState.Success)
                {
                    resultAgentInfo.ServerState = ServerState.Running;
                }
                else
                {

                }

                agentSocketList.ElementAt(host).Value.RemoveAt(server);
                agentSocketList.ElementAt(host).Value.Add(resultAgentInfo);
                selectedIndex = -1;

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
            return Task.Run(() => {

                //logger.Debug("[Admin][HandleAgentInfoResponseAsync()] start");
                Console.WriteLine("[Admin][HandleAgentInfoResponseAsync()] start");
                AAAgentInfoRequestBody requestBody = (AAAgentInfoRequestBody)NetworkManager.Read(agentSocket, header.BodyLength, typeof(AAAgentInfoRequestBody));
                Console.WriteLine("[Admin][HandleAgentInfoResponseAsync()] ip : " + requestBody.AgentInfo.ServerInfo.GetPureIp());
                Console.WriteLine("[Admin][HandleAgentInfoResponseAsync()] port :" + requestBody.AgentInfo.ServerInfo.Port);

                List<AgentInfo> tmp = null;

                if (agentSocketList.ContainsKey(agentSocket))
                {
                    agentSocketList.TryGetValue(agentSocket, out tmp);
                    tmp.Add(requestBody.AgentInfo);

                    //agentSocketList.Add(agentSocket, requestBody.AgentInfo);

                    agentSocketList.Remove(agentSocket);
                    agentSocketList.Add(agentSocket, tmp);
                    logger.Debug("[Admin][HandleAgentInfoResponseAsync()] key contains");
                }
                else
                {
                    tmp = new List<AgentInfo>();
                    tmp.Add(requestBody.AgentInfo);
                    agentSocketList.Add(agentSocket, tmp);
                    //agentSocketList.Add(agentSocket, requestBody.AgentInfo);
                    logger.Debug("[Admin][HandleAgentInfoResponseAsync()] no keys");
                }
                logger.Debug("[Admin][HandleAgentInfoResponseAsync()] end");
                Console.WriteLine("[Admin] Connected Agent Info IP: " + requestBody.AgentInfo.ServerInfo.GetPureIp());
                Console.WriteLine("[Admin] Connected Agent Info port: " + requestBody.AgentInfo.ServerInfo.Port);
            });
        }// end method
    }
}