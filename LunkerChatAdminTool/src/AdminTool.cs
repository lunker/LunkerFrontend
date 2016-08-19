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
        private Task<int> consoleInputTask = null; // agent + request

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
        public Task<Socket> AcceptAgentAsync()
        {
            /*
            //logger.Debug("[Admin][AcceptAgentAsync()] start");
            if (acceptAgentTask != null)
            {
                if (acceptAgentTask.IsCompleted)
                {
                    logger.Debug("[Admin][AcceptAgentAsync()] complete");

                    // socket을 저장해놓고, 나중에 serverinfo를 받아서 초기화 시킨다.
                    agentSocketList.Add(acceptAgentTask.Result, default(AgentInfo));

                    acceptAgentTask = Task.Run(() => {
                        return agentListener.Accept();
                    });
                }
            }
            else
            {
                acceptAgentTask = Task.Run(() => {
                    
                });
            }
            */
            return Task.Run(()=> {  return agentListener.Accept(); });
            
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
       
        public void PrintErrorUI()
        {
            const string format = "{0,-32} : {1} ";
            
            Console.WriteLine(format, "Error", "다시 입력하세요.");
        }

        public Task<string> ConsoleAgentSelectTask()
        {
            return Task.Run(() => {
                Console.Write("Agent를 선택하세요 : ");
                return Console.ReadLine();
            });
        }
        public Task<string> ConsoleRequestSelectTask()
        {

            return Task.Run(() => {
                Console.Write("Enter Command : ");
                
                return Console.ReadLine();
            });
        }

        public Task<int> ConsoleInputTask()
        {
            
            return Task.Run( ()=> {

                Console.Write("Enter Command : ");

                string request = Console.ReadLine();

                Console.Write("Agent를 선택하세요 : ");
                string agent = Console.ReadLine();

                logger.Debug("[Admin][ConsoleInputTask()] send request!");
                if (int.TryParse(agent, out selectedAgent) && int.TryParse(request, out selectedRequest))
                {
                    return 1; 
                }
                else
                {
                    return 0;
                }
                
                
                /*
                string request = await ConsoleRequestSelectTask();
                string agent = await ConsoleAgentSelectTask();
                logger.Debug("[Admin][ConsoleInputTask()] send request!");

                if (int.TryParse(agent, out selectedAgent))
                {
                    logger.Debug("[Admin][ConsoleInputTask()] send request!");
                    SendAdminRequest(selectedRequest, agentSocketList.ElementAt(selectedAgent).Key);
                }
                */

            });
            

        }

        /// <summary>
        /// <para>Send Admin request </para>
        /// </summary>
        /// <returns></returns>
        public async void SendAdminRequest(int type, Socket agentSocket)
        {
            Console.WriteLine( "asdfawsdfadsfdasfasdfdasfadsf");
            logger.Debug("[Admin][SendAdminRequest()] start");
            //CommonHeader header = (CommonHeader) await NetworkManager.ReadAsync(agent, Constants.HeaderSize, typeof(CommonHeader));
            try
            {
                switch (type)
                {
                    case (short)MessageType.StartApp:
                        HandleStartAppRequestAsync(agentSocket);
                        break;
                    case (short)MessageType.ShutdownApp:
                        HandleShutdownAppRequestAsync(agentSocket);
                        break;

                    case (short)MessageType.RestartApp:
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

        public async void HandleAgentResponse(Socket agentSocket)
        {
            logger.Debug("[Admin][HandleAgentResponse()] start");
            // read response form agent
            AAHeader responseHeader = (AAHeader) await NetworkManager.ReadAsync(agentSocket, Constants.AdminHeaderSize, typeof(AAHeader));

            switch (responseHeader.Type)
            {
                case MessageType.Basic:
                    // 공통작업
                    // UI Refresh
                case MessageType.AgentInfo:
                    await HandleAgentInfoResponseAsync(agentSocket, responseHeader);
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

        public Task HandleStartAppRequestAsync(Socket agentSocket)
        {
            return Task.Run(()=> {

                AAHeader requestHeader = new AAHeader(MessageType.StartApp, MessageState.Request, Constants.None);
                NetworkManager.Send(agentSocket, requestHeader);
            });
        }
        public Task HandleShutdownAppRequestAsync(Socket agentSocket)
        {
            return Task.Run(() => { });
        }
        public Task HandleReStartAppRequestAsync(Socket agentSocket)
        {
            return Task.Run(() => { });
        }
        ////---------------------------------------------Response---------------------------------------------/////
        
        public Task HandleStartAppResponseAsync(Socket agentSocket)
        {
            return Task.Run(() => {
            });
        }
        public Task HandleShutdownAppResponseAsync(Socket agentSocket)
        {
            return Task.Run(() => { });
        }
        public Task HandleReStartAppResponseAsync(Socket agentSocket)
        {
            return Task.Run(() => { });
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

        public async void MainProcess()
        {
            while (appState)
            {

                if (acceptAgentTask != null)
                {
                    if (acceptAgentTask.IsCompleted)
                    {
                        logger.Debug("[Admin][AcceptAgentAsync()] complete");

                        // socket을 저장해놓고, 나중에 serverinfo를 받아서 초기화 시킨다.
                        agentSocketList.Add(acceptAgentTask.Result, default(AgentInfo));

                        acceptAgentTask = AcceptAgentAsync();
                    }
                }
                else
                {
                    acceptAgentTask = AcceptAgentAsync();
                }



                //PrintMainUI();
                if (printUITask != null)
                {
                    if (printUITask.IsCompleted)
                    {
                        printUITask = PrintMainUIAsync();
                    }
                }
                else
                {
                    printUITask = PrintMainUIAsync();
                }

                // accept agent connect request 
                
               

                // Get User Input 
                if (consoleInputTask != null)
                {
                    if (consoleInputTask.IsCompleted )
                    {
                        logger.Debug("[Admin][AcceptAgentAsync()] consoleInputTask complete??");
                        int result = consoleInputTask.Result;

                        Console.WriteLine("result :"+result);
                        SendAdminRequest(selectedRequest, agentSocketList.ElementAt(selectedAgent).Key);
                        consoleInputTask = ConsoleInputTask();
                    }
                    else if (consoleInputTask.IsCanceled)
                    {
                        // printError 
                        PrintErrorUI();
                        consoleInputTask = ConsoleInputTask();
                    }
                }
                else
                {
                    consoleInputTask = ConsoleInputTask();
                }

                // select read ㅠㅠㅠㅠ 

                if(0 != agentSocketList.Count)
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
              
            }// end loop 
        }// end method
    }
}
