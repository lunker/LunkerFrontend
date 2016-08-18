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

        private Dictionary<ServerInfo, Socket> agentSocketList = null;

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
            agentSocketList = new Dictionary<ServerInfo, Socket>();
            agentListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 43330); 

            agentListener.Bind(endPoint);
            agentListener.Listen(100);
        }

        public Task BindAgentListener()
        {
            ;
        }

        public void AcceptAgentAsync()
        {
            if (acceptAgentTask != null)
            {
                if (acceptAgentTask.IsCompleted)
                {

                    // serverinfo를 받고서 저장시킨다.

                    acceptAgentTask = Task.Run(() => {
                        return agentListener.Accept();
                    });
                }
            }
            else
            {

            }
        }

        public Task ConsoleInputTask()
        {

        }

        public Task AcceptRequestTask()
        {

        }

        public void HandleRequest()
        {

        }

        public void MainProcess()
        {
            while (appState)
            {
                AcceptAgentAsync();

                ConsoleInputTask();

                AcceptRequestTask();

            }// end loop 
        }// end method


        

    }
}
