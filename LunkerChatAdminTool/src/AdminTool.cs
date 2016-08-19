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

            // print ui
            PrintAgentInfo();
            PrintRequest();
            while (true)
            {
        

                Console.Write("Enter Command : ");

                string request = Console.ReadLine();

                Console.Write("Agent를 선택하세요 : ");
                string agent = Console.ReadLine();

                logger.Debug("[Admin][ConsoleInputTask()] send request!");
                Console.WriteLine();
                if (int.TryParse(agent, out selectedAgent) && int.TryParse(request, out selectedRequest))
                {
                    
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

                    
                }
                else
                {

                }
            }
        }// end method

        public void Stop()
        {
            appState = Constants.AppStop;
        }
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

        /// <summary>
        /// Accept Agent Connect request
        /// </summary>
        public void AcceptAgentAsync()
        {
            if (acceptAgentTask != null)
            {
                if (acceptAgentTask.IsCompleted)
                {
                    logger.Debug("[ChatServer][HandleRequest()] complete accept task. Restart");

                    // Add accepted connections
                    agentSocketList.Add(acceptAgentTask.Result, default(AgentInfo));

                    // 다시 task run 
                    //getAcceptTask = Task.Factory.FromAsync(clientListener.BeginAccept, clientListener.EndAccept, true);
                    acceptAgentTask = Task.Run(() => {
                        return agentListener.Accept();
                    });
                }
            }
            else
            {
                logger.Debug("[ChatServer][HandleRequest()] start accept task ");
                //clientAcceptTask = Task.Factory.FromAsync(clientListener.BeginAccept, clientListener.EndAccept, true);
                acceptAgentTask = Task.Run(() => {
                    return agentListener.Accept();
                });
            }

            //return Task.Run(()=> {  return agentListener.Accept(); });

        }// end method

        public void PrintAgentInfo()
        {
            // index, state, ip, port 
            const string format = "[{0}][{1}] {2} : {3}";
            int idx = 0;
            Console.Clear();
            Console.WriteLine("---------------------------------------------------------");
            Console.WriteLine(format, "index", "state", "ip", "port");
            foreach (AgentInfo agent in agentSocketList.Values.ToList()){
                currentLine++;
                Console.WriteLine(format, idx++, agent.ServerState, new string(agent.ServerInfo.Ip), agent.ServerInfo.Port);
            }
            Console.WriteLine("---------------------------------------------------------");
        }

        public void PrintRequest()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("[1] : Start Application [2] : Shutdown Application [3] : Restart Application");
        }
        public Task PrintMainUIAsync()
        {
            
            return Task.WhenAll(
                Task.Run(()=> {
                    PrintAgentInfo();
                    PrintRequest();
                }),
                Task.Delay(2000)
                );
        }

        /// <summary>
        /// <para>Send Admin request </para>
        /// </summary>
        /// <returns></returns>
        public void SendAdminRequest(MessageType type, Socket agentSocket)
        {
            logger.Debug("[Admin][SendAdminRequest()] start");
            //CommonHeader header = (CommonHeader) await NetworkManager.ReadAsync(agent, Constants.HeaderSize, typeof(CommonHeader));
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
            }
            logger.Debug("[Admin][SendAdminRequest()] start");
        }// end method 

        /// <summary>
        /// start, shutdown, restart의 resonse = header만 존재 
        /// </summary>
        /// <param name="agentSocket"></param>
        public async void HandleAgentResponse(Socket agentSocket)
        {
            logger.Debug("[Admin][HandleAgentResponse()] start");
            // read response form agent
            AAHeader responseHeader = (AAHeader) await NetworkManager.ReadAsync(agentSocket, Constants.AdminHeaderSize, typeof(AAHeader));

            switch (responseHeader.Type)
            {
                /*
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
                */
                case MessageType.AgentInfo:
                    break;
                case MessageType.StartApp:
                    break;
                case MessageType.ShutdownApp:
                    break;
                case MessageType.RestartApp:
                    break;
                default:
                    break;
            }// end switch

            logger.Debug("[Admin][HandleAgentResponse()] end");
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
        
        public Task HandleStartAppResponseAsync(Socket agentSocket)
        {
            return Task.Run(() => {
                
            });
        }
        public Task HandleShutdownAppResponseAsync(Socket agentSocket)
        {
            return Task.Run(() => {

            });
        }
        public Task HandleReStartAppResponseAsync(Socket agentSocket)
        {
            return Task.Run(() => {

            });
        }

        /// <summary>
        /// ok
        /// </summary>
        /// <param name="agentSocket"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public Task HandleAgentInfoResponseAsync(Socket agentSocket, AAHeader header)
        {
            return Task.Run(()=> {
                logger.Debug("[Admin][HandleAgentInfoResponseAsync()] start");
                AAAgentInfoRequestBody requestBody = (AAAgentInfoRequestBody)NetworkManager.Read(agentSocket, header.BodyLength, typeof(AAAgentInfoRequestBody));
                logger.Debug("[Admin][HandleAgentInfoResponseAsync()] ip : " + new string(requestBody.AgentInfo.ServerInfo.Ip).Split('\0')[0]);
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
