using log4net;
using LunkerAgent.src.utils;
using LunkerLibrary.common.protocol;
using LunkerLibrary.common.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LunkerAgent.src
{
    public class AdminAgent
    {
        private ILog logger = AgentLogger.GetLoggerInstance();
        private static AdminAgent instance = null;

        private Socket adminSocket = null;

        private String hostIP = "";
        private bool appState = Constants.AppRun;
        private List<Socket> readSocket = null;
        private Process chatProcess = null;

        private Task socketConnectTask = null;
        private CancellationTokenSource source = new CancellationTokenSource();
         
        private MessageBroker broker = MessageBroker.GetInstance();
        private AdminAgent() { }

        public static AdminAgent GetInstance()
        {
            if(instance == null)
            {
                instance = new AdminAgent();
            }
            return instance;
        }

        public void Start()
        {
            logger.Debug("\n\n------------------------------------start------------------------------");
            Initialize();
            MainProcess();
        }

        public void Stop()
        {
            appState = Constants.AppStop;
        }

        /// <summary>
        /// 예외처리해야함
        /// </summary>
        public void Initialize()
        {
            Console.WriteLine(Marshal.SizeOf(new AAHeader()));
            // set host IP Address
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (ip.ToString().Split('.')[0].Equals("10"))
                    {
                        hostIP = ip.ToString();
                    }
                }
            }
            logger.Debug("[AdminAgent][Initialize()] IP :  " + hostIP);

            //adminSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            readSocket = new List<Socket>();
            //broker.RegisterSubscribe();
        }

        public Task ConnectTask()
        {
            return Task.WhenAll(Task.Run(() => {
                try
                {
                    if(adminSocket!=null && adminSocket.Connected)
                    {

                    }
                    else
                    {
                        adminSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(AppConfig.GetInstance().Ip), AppConfig.GetInstance().Port);

                        adminSocket.Connect(endPoint);
                        SendServerInfo();
                        logger.Debug("[AdminAgent][Initialize()] connect success");

                    }
                }
                catch (SocketException se)
                {
                    source.Cancel();
                }
            }),
            Task.Delay(2000));
        }

        public async void MainProcess()
        {
            
            while (appState)
            {
                // admin socket connection
                try
                {
                    if (adminSocket != null)
                    {
                        if (!adminSocket.Connected)
                        {
                            if (socketConnectTask.IsCompleted)
                            {
                                socketConnectTask = ConnectTask();
                            }
                        }
                    }
                    else
                    {
                        if (socketConnectTask==null)
                        {
                            socketConnectTask = ConnectTask();
                        }
                    }

                }
                catch (SocketException se)
                {
                    // admin과 연결이끊어짐. 계속해서 연결 시도...
                    logger.Debug("[AdminAgent][Initialize()] connect fail . . .");
                    continue;
                }

                // read 
                if (adminSocket!=null && adminSocket.Connected)
                {
                    if (adminSocket.Poll(0, SelectMode.SelectRead))
                    {
                        // read
                        try
                        {
                            logger.Debug("[AdminAgent][Initialize()] before call HandleRequestAsync. .");
                            Task.Run(() => HandleRequestAsync(adminSocket));
                        }
                        catch (SocketException se)
                        {
                            continue;
                        }
                    }
                }
                // poll admin request
                
            }// end loop 
        }// end method

        public async void SendServerInfo()
        {
            logger.Debug("[AdminAgent][SendServerInfo()] start");
            ServerInfo serverInfo = new ServerInfo(hostIP);
            AgentInfo agentInfo = new AgentInfo(serverInfo, new ServerState());

            AAAgentInfoRequestBody requestBody = new AAAgentInfoRequestBody(agentInfo);
            byte[] bodyArr = NetworkManager.StructureToByte(requestBody); 
            AAHeader requestHeader = new AAHeader(MessageType.AgentInfo, MessageState.Request, bodyArr.Length);

            await NetworkManager.SendAsync(adminSocket, requestHeader);
            await NetworkManager.SendAsync(adminSocket, bodyArr);
            logger.Debug("[AdminAgent][SendServerInfo()] end");
        }

        /// <summary>
        /// handle admin request
        /// </summary>
        /// <param name="peer"></param>
        public async void HandleRequestAsync(Socket peer)
        {
            //AAHeader requestHeader = (AAHeader)NetworkManager.ReadAsync(peer, Constants.AdminHeaderSize, typeof(AAHeader));
            try
            {
                AAHeader requestHeader = (AAHeader) NetworkManager.Read(peer, Constants.AdminHeaderSize, typeof(AAHeader));
                switch (requestHeader.Type)
                {
                    case MessageType.RestartApp:
                        HandleRestartApp();
                        break;
                    case MessageType.ShutdownApp:
                        HandleShutdownApp();
                        break;
                    case MessageType.StartApp:
                        HandleStartApp();
                        break;

                    default:
                        break;
                }
            }
            catch (ObjectDisposedException ode)
            {
                return;
            }
            catch (SocketException se)
            {
                logger.Debug("[AdminAgent][HandleRequestAsync] socket disconnected");
                peer.Close();

                return;
            }
        }// end method 

        public async void HandleResponse(AAHeader responseHeader)
        {
            //await NetworkManager.SendAsyncTask(adminSocket, responseHeader);
            await NetworkManager.SendAsync(adminSocket, responseHeader);
        }

        /// <summary>
        /// <para></para>
        /// <para></para>
        /// <para></para>
        /// </summary>
        /// <param name="peer"></param>

        public async void HandleStartApp()
        {
            if (chatProcess == null)
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.CreateNoWindow = false;
                info.FileName = "D:\\workspace\\LunkerFrontend\\LunkerChatServer\\bin\\Debug\\LunkerChatServer.exe";

                chatProcess = Process.Start(info);

                // send result
                AAHeader responseHeader = new AAHeader(MessageType.StartApp, MessageState.Success, Constants.None);
                //await NetworkManager.SendAsyncTask(adminSocket, responseHeader);
                await NetworkManager.SendAsync(adminSocket, responseHeader);
            }
            else
            {
                if (!chatProcess.HasExited)
                {
                    // already start
                    AAHeader responseHeader = new AAHeader(MessageType.StartApp, MessageState.Fail, Constants.None);
                    //await NetworkManager.SendAsyncTask(adminSocket, responseHeader);
                    await NetworkManager.SendAsync(adminSocket, responseHeader);
                }
            }
        }

        /// <summary>
        /// message queue를 통해서 죽여야함. - gracefull shutdown을 위하여
        /// </summary>
        /// <param name="peer"></param>
        public void HandleShutdownApp()
        {
            if (chatProcess != null && !chatProcess.HasExited)
            {
                // publish message
                AAHeader requestHeader = new AAHeader(MessageType.ShutdownApp, MessageState.Request, Constants.None);
                broker.Publish(requestHeader);
            }
        }

        /// <summary>
        /// <para></para>
        /// <para></para>
        /// <para></para>
        /// </summary>
        /// <param name="peer"></param>
        public void HandleRestartApp()
        {
            if (chatProcess == null)
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.CreateNoWindow = true;
                info.FileName = "D:\\workspace\\LunkerFrontend\\LunkerChatServer\\bin\\Debug\\LunkerChatServer.exe";

                chatProcess = Process.Start(info);

            }
        }// end 
    }
}
