using LunkerAgent.src.utils;
using LunkerLibrary.common.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LunkerAgent.src
{
    public class AdminAgent
    {
        private static AdminAgent instance = null;

        private Socket adminSocket = null;
        private bool appState = Constants.AppRun;
        private List<Socket> readSocket = null;
        private Process chatProcess = null;

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
            adminSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(AppConfig.GetInstance().Ip), AppConfig.GetInstance().Port);
            adminSocket.Connect(endPoint);

            readSocket = new List<Socket>();
            //broker.RegisterSubscribe();
        }

        public void MainProcess()
        {
            while (appState)
            {
                // check connection 
                if (adminSocket != null && !adminSocket.Connected)
                {

                    adminSocket.Close();
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(AppConfig.GetInstance().Ip), AppConfig.GetInstance().Port);
                    adminSocket.Connect(endPoint);
                }

                if (adminSocket.Poll(0, SelectMode.SelectRead))
                {
                    // read
                    HandleRequestAsync(adminSocket);
                }
            }// end loop 
        }// end method

        public void HandleRequestAsync(Socket peer)
        {
            AAHeader requestHeader = (AAHeader)NetworkManager.ReadAsync(peer, Constants.AdminHeaderSize, typeof(AAHeader));

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

        public async void HandleResponse(AAHeader responseHeader)
        {
            await NetworkManager.SendAsyncTask(adminSocket, responseHeader);
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
                AAHeader responseHeader = new AAHeader(MessageType.StartApp, MessageState.Success);
                await NetworkManager.SendAsyncTask(adminSocket, responseHeader);
            }
            else
            {
                if (!chatProcess.HasExited)
                {
                    // already start
                    AAHeader responseHeader = new AAHeader(MessageType.StartApp, MessageState.Fail);
                    await NetworkManager.SendAsyncTask(adminSocket, responseHeader);
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
                AAHeader requestHeader = new AAHeader(MessageType.ShutdownApp, MessageState.Request);
                broker.Publish(requestHeader);
            }
        }

        /// <summary>
        /// <para></para>
        /// <para></para>
        /// <para></para>
        /// </summary>
        /// <param name="peer"></param>
        public async void HandleRestartApp()
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
