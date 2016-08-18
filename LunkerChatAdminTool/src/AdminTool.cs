using LunkerLibrary.common.protocol;
using LunkerLibrary.common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LunkerChatAdminTool.src
{
    public class AdminTool
    {
        private bool appState = Constants.AppRun;
        private Socket agentListener = null;


        private Task<Socket> acceptAgentTask = null; // socket listen
        private Task<string> consoleInputTask = null;
        private Task acceptRequestTask = null;

        private Dictionary<Socket, ServerInfo> agentSocketList = null;

        private List<Socket> readSocketList = null;
        private List<Socket> writeSocketList = null;
        private List<Socket> errorSocketList = null;

        private static AdminTool instance = null;


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
            agentSocketList = new Dictionary<Socket, ServerInfo>();

            readSocketList = new List<Socket>();
            writeSocketList = new List<Socket>();
            errorSocketList = new List<Socket>();


            agentListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 43330); 

            agentListener.Bind(endPoint);
            agentListener.Listen(100);
        }

        public Task BindAgentListener()
        {

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

                    // socket을 저장해놓고, 나중에 serverinfo를 받아서 초기화 시킨다.
                    agentSocketList.Add(acceptAgentTask.Result, default(ServerInfo));


                    acceptAgentTask = Task.Run(() => {
                        return agentListener.Accept();
                    });
                }
            }
            else
            {
                acceptAgentTask = Task.Run(() => {
                    return agentListener.Accept();
                });
            }
        }// end method

        
        public void PrintAgentInfo()
        {
            // index, state, ip, port 
            const string format = "[{0,-3}][{1,-5}] {2, -20} : {3,-5}";
            int idx = 0;
            Console.Clear();
            Console.WriteLine("---------------------------------------------------------");
            foreach(ServerInfo agent in agentSocketList.Values.ToList()){
                Console.WriteLine(format, idx++, agent.Ip, agent.Port);
            }
            Console.WriteLine("---------------------------------------------------------");
        }

        /// <summary>
        /// Print User Interface
        /// </summary>
        public void PrintUI()
        {
            const string format = "{0,-32} : {1} ";

            Console.Clear();
            Console.WriteLine(format, "Key", "Value");
        }

        public void PrintErrorUI()
        {
            const string format = "{0,-32} : {1} ";
            
            Console.WriteLine(format, "Error", "다시 입력하세요.");
        }

        public Task ConsoleInputTask()
        {
            PrintUI();

            if (consoleInputTask != null)
            {
                if (consoleInputTask.IsCompleted)
                {

                    string request = consoleInputTask.Result;
                    short type = 0;
                    if(short.TryParse(request, out type))
                    {
                        HandleAdminRequest(type);
                    }
                    else
                    {
                        // print error
                        Task.Delay();
                    }
                
                    


                    consoleInputTask = Task.Run(() => {
                       return Console.ReadLine();
                    });
                }

            }
            else
            {
                consoleInputTask = Task.Run(() => {
                    return Console.ReadLine();
                });
            }
        }

        /// <summary>
        /// <para>Get Agent Request</para>
        /// </summary>
        /// <returns></returns>
        public async void HandleAdminRequest(short type)
        {
            //CommonHeader header = (CommonHeader) await NetworkManager.ReadAsync(agent, Constants.HeaderSize, typeof(CommonHeader));

            switch (type)
            {
                case (short) MessageType.AgentInfo:
                    await HandleAgentInfoAsync();
                    break;
                case (short) MessageType.StartApp:
                    await HandleStartAppAsync();
                    break;

                case (short) MessageType.ShutdownApp:
                    await HandleShutdownAppAsync();
                    break;

                case (short) MessageType.RestartApp:
                    await HandleReStartAppAsync();
                    break;

                default:
                    break;
            }

        }

        public Task HandleAgentInfoAsync()
        {
            return Task.Run(() => { });
        }

        public Task HandleStartAppAsync()
        {
            return Task.Run(()=> { });
        }
        public Task HandleShutdownAppAsync()
        {
            return Task.Run(() => { });
        }
        public Task HandleReStartAppAsync()
        {
            return Task.Run(() => { });
        }

        public void MainProcess()
        {
            while (appState)
            {
                AcceptAgentAsync();

                ConsoleInputTask();

                /*
                if (0 != agentSocketList.Count)
                {
                    readSocketList = agentSocketList.Keys.ToList();

                    Socket.Select(readSocketList, writeSocketList, errorSocketList, 0);

                    if(0 != readSocketList.Count)
                    {
                        foreach(Socket agent in readSocketList)
                        {
                            HandleAgentRequest(agent);
                        }
                    }
                }
                */


                //AcceptRequestTask();

            }// end loop 
        }// end method


        

    }
}
