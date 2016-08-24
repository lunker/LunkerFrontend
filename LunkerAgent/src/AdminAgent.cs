﻿using log4net;
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

        
        private ProcessStartInfo socketChatServer = new ProcessStartInfo();
        private ProcessStartInfo websocketChatServer = new ProcessStartInfo();


        private AdminAgent() { }

        public static AdminAgent GetInstance()
        {
            if (instance == null)
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

            /*
            info.CreateNoWindow = false;
            info.FileName = "C:\\chatserver(socket)\\LunkerChatServer.exe";
            */

            socketChatServer.CreateNoWindow = false;
            socketChatServer.FileName = "..\\..\\..\\LunkerChatServer\\bin\\Release\\LunkerChatServer.exe";

            websocketChatServer.CreateNoWindow = false;
            websocketChatServer.FileName = "..\\..\\..\\LunkerChatWebServer\\bin\\Release\\LunkerChatWebServer.exe";
        }
        public Task HandleAdminConnectAsync()
        {
            return Task.Run(() => {

                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(AppConfig.GetInstance().Ip), AppConfig.GetInstance().Port);

                while (true)
                {
                    try
                    {
                        if (adminSocket != null)
                        {
                            if (!adminSocket.Connected)
                            {
                                Task.Delay(1000);
                                adminSocket.Connect(endPoint);
                                //SendServerInfo();
                                SendSocketServerInfo();
                                SendWebSocketServerInfo();
                                Console.WriteLine("[AdminAgent][HandleAdminConnectAsync()] connect success");
                                Task.Run(() => { HandleRequestAsync(adminSocket); });
                                Console.WriteLine("[AdminAgent][HandleAdminConnectAsync()] start agent handler task ");
                            }
                        }
                        else
                        {
                            adminSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        }
                    }
                    catch (InvalidOperationException ioe)
                    {
                        //adminSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        adminSocket = null;
                        //adminSocket.Blocking = false;
                        Console.WriteLine("[AdminAgent][HandleAdminConnectAsync()] Disconnected . . . admin tool  . . . retry");
                        continue;
                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine("[AdminAgent][HandleAdminConnectAsync()] socketexception");
                        //adminSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        //adminSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        //adminSocket.Blocking = false;
                        continue;
                    }
                }// end loop
            });
        }

        public async void MainProcess()
        {
            HandleAdminConnectAsync();

            while (appState)
            {


                /*
                //logger.Debug("[AdminAgent][SendServerInfo()] 1");
                // read 
                if (adminSocket!=null && adminSocket.Connected)
                {
                    if (adminSocket.Poll(500, SelectMode.SelectRead))
                    {
                        // read
                        try
                        {
                            //Console.WriteLine("[AdminAgent][Initialize()] before call HandleRequestAsync. .");
                            //logger.Debug("[AdminAgent][Initialize()] before call HandleRequestAsync. .");
                            HandleRequestAsync(adminSocket);
                        }
                        catch (SocketException se)
                        {
                            continue;
                        }
                    }
                }// end if 
                */
                // poll admin request
            }// end loop 
        }// end method
        public void SendSocketServerInfo()
        {
            Console.WriteLine("[AdminAgent][SendSocketServerInfo()]" + hostIP);

            ServerInfo serverInfo = new ServerInfo(hostIP, 0);
            AgentInfo agentInfo = new AgentInfo(serverInfo, ServerState.Stopped, ServerType.Socket);

            AAAgentInfoRequestBody requestBody = new AAAgentInfoRequestBody(agentInfo);
            byte[] bodyArr = NetworkManager.StructureToByte(requestBody);
            AAHeader requestHeader = new AAHeader(MessageType.AgentInfo, MessageState.Request, bodyArr.Length);

            //logger.Debug("[AdminAgent][SendServerInfo()] IP By GetPureIP() : "+ agentInfo.ServerInfo.GetPureIp());
            Console.WriteLine("[AdminAgent][SendSocketServerInfo()] IP By GetPureIP() : " + agentInfo.ServerInfo.GetPureIp());

            if (adminSocket != null && adminSocket.Connected)
            {
                byte[] headerArr = NetworkManager.StructureToByte(requestHeader);
                byte[] resBodyArr = NetworkManager.StructureToByte(requestBody);

                byte[] packetArr = new byte[headerArr.Length + resBodyArr.Length];
                Buffer.BlockCopy(headerArr, 0, packetArr, 0, headerArr.Length);
                Buffer.BlockCopy(resBodyArr, 0, packetArr, headerArr.Length, resBodyArr.Length);

                NetworkManager.Send(adminSocket, packetArr);

                //NetworkManager.Send(adminSocket, requestHeader);
                //NetworkManager.Send(adminSocket, bodyArr);
            }
            //logger.Debug("[AdminAgent][SendServerInfo()] end");
            Console.WriteLine("[AdminAgent][SendSocketServerInfo()] end");
        }
        public void SendWebSocketServerInfo()
        {
            Console.WriteLine("[AdminAgent][SendWebSocketServerInfo()]" + hostIP);

            ServerInfo serverInfo = new ServerInfo(hostIP, 0);
            AgentInfo agentInfo = new AgentInfo(serverInfo, ServerState.Stopped, ServerType.Websocket);

            AAAgentInfoRequestBody requestBody = new AAAgentInfoRequestBody(agentInfo);
            byte[] bodyArr = NetworkManager.StructureToByte(requestBody);
            AAHeader requestHeader = new AAHeader(MessageType.AgentInfo, MessageState.Request, bodyArr.Length);

            //logger.Debug("[AdminAgent][SendServerInfo()] IP By GetPureIP() : "+ agentInfo.ServerInfo.GetPureIp());
            Console.WriteLine("[AdminAgent][SendWebSocketServerInfo()] IP By GetPureIP() : " + agentInfo.ServerInfo.GetPureIp());

            if (adminSocket != null && adminSocket.Connected)
            {
                byte[] headerArr = NetworkManager.StructureToByte(requestHeader);
                byte[] resBodyArr = NetworkManager.StructureToByte(requestBody);

                byte[] packetArr = new byte[headerArr.Length + resBodyArr.Length];
                Buffer.BlockCopy(headerArr, 0, packetArr, 0, headerArr.Length);
                Buffer.BlockCopy(resBodyArr, 0, packetArr, headerArr.Length, resBodyArr.Length);

                NetworkManager.Send(adminSocket, packetArr);
            }
            //logger.Debug("[AdminAgent][SendServerInfo()] end");
            Console.WriteLine("[AdminAgent][SendWebSocketServerInfo()] end");
        }
        public async void SendServerInfo()
        {
            Console.WriteLine("[AdminAgent][SendServerInfo()]" + hostIP);

            ServerInfo serverInfo = new ServerInfo(hostIP, 43320);
            AgentInfo agentInfo = new AgentInfo(serverInfo, ServerState.Stopped, ServerType.Socket);

            AAAgentInfoRequestBody requestBody = new AAAgentInfoRequestBody(agentInfo);
            byte[] bodyArr = NetworkManager.StructureToByte(requestBody);
            AAHeader requestHeader = new AAHeader(MessageType.AgentInfo, MessageState.Request, bodyArr.Length);

            //logger.Debug("[AdminAgent][SendServerInfo()] IP By GetPureIP() : "+ agentInfo.ServerInfo.GetPureIp());
            Console.WriteLine("[AdminAgent][SendServerInfo()] IP By GetPureIP() : " + agentInfo.ServerInfo.GetPureIp());

            if (adminSocket != null && adminSocket.Connected)
            {
                byte[] headerArr = NetworkManager.StructureToByte(requestHeader);
                byte[] resBodyArr = NetworkManager.StructureToByte(requestBody);

                byte[] packetArr = new byte[headerArr.Length + resBodyArr.Length];
                Buffer.BlockCopy(headerArr, 0, packetArr, 0, headerArr.Length);
                Buffer.BlockCopy(resBodyArr, 0, packetArr, headerArr.Length, resBodyArr.Length);

                NetworkManager.Send(adminSocket, packetArr);




                //NetworkManager.Send(adminSocket, requestHeader);
                //NetworkManager.Send(adminSocket, bodyArr);
            }
            //logger.Debug("[AdminAgent][SendServerInfo()] end");
            Console.WriteLine("[AdminAgent][SendServerInfo()] end");
        }

        /// <summary>
        /// handle admin request
        /// </summary>
        /// <param name="peer"></param>
        public void HandleRequestAsync(Socket peer)
        {
            while (true)
            {
                logger.Debug("[AdminAgent][SendServerInfo()] end");
                //AAHeader requestHeader = (AAHeader)NetworkManager.ReadAsync(peer, Constants.AdminHeaderSize, typeof(AAHeader));
                try
                {
                    AAHeader requestHeader = (AAHeader)NetworkManager.Read(peer, Constants.AdminHeaderSize, typeof(AAHeader));
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
                catch (NoMessageException ne)
                {
                    Console.WriteLine("no message read");

                    return;
                }
                catch (ObjectDisposedException ode)
                {
                    return;
                }
                catch (SocketException se)
                {
                    logger.Debug("[AdminAgent][HandleRequestAsync] socket disconnected");

                    if (peer != null)
                        peer.Close();

                    return;
                }
            }
        }// end method 

        /// <summary>
        /// Send ResponseHeader to Admin Tool 
        /// </summary>
        /// <param name="responseHeader"></param>
        public async void HandleResponse(AAHeader responseHeader)
        {
            //await NetworkManager.SendAsyncTask(adminSocket, responseHeader);
            NetworkManager.Send(adminSocket, responseHeader);
            //await NetworkManager.SendAsync(adminSocket, responseHeader);
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
                Console.WriteLine("in null");

                //info.FileName = "D:\\workspace\\feature-async-without-beginxxxx\\LunkerFrontend\\LunkerChatServer\\bin\\Debug\\LunkerChatServer.exe";
                chatProcess = Process.Start(socketChatServer);

                // send result
                AAHeader responseHeader = new AAHeader(MessageType.StartApp, MessageState.Success, Constants.None);
                //await NetworkManager.SendAsyncTask(adminSocket, responseHeader);
                //await NetworkManager.SendAsync(adminSocket, responseHeader);
                NetworkManager.Send(adminSocket, responseHeader);
            }
            else
            {
                Console.WriteLine("not null");
                if (chatProcess.HasExited || !chatProcess.Responding)
                {
                    try
                    {
                        Console.WriteLine("in null");
                        //ProcessStartInfo info = new ProcessStartInfo();
                        //info.CreateNoWindow = false;
                        //info.FileName = "D:\\workspace\\feature-async-without-beginxxxx\\LunkerFrontend\\LunkerChatServer\\bin\\Debug\\LunkerChatServer.exe";
                        chatProcess = Process.Start(socketChatServer);

                        Console.WriteLine("not null start!!!");

                        // already start
                        AAHeader responseHeader = new AAHeader(MessageType.StartApp, MessageState.Success, Constants.None);
                        //await NetworkManager.SendAsyncTask(adminSocket, responseHeader);
                        //await NetworkManager.SendAsync(adminSocket, responseHeader);
                        NetworkManager.Send(adminSocket, responseHeader);
                    }
                    catch (Exception e)
                    {
                        AAHeader responseHeader = new AAHeader(MessageType.StartApp, MessageState.Fail, Constants.None);
                        //await NetworkManager.SendAsyncTask(adminSocket, responseHeader);
                        //await NetworkManager.SendAsync(adminSocket, responseHeader);
                        NetworkManager.Send(adminSocket, responseHeader);
                    }

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
                //chatProcess.Kill();
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
            logger.Debug("[AdminAgent][HandleRestartApp()] start");

            // 1) shutdown
            if (chatProcess != null && !chatProcess.HasExited)
            {
                // publish message
                AAHeader requestHeader = new AAHeader(MessageType.RestartApp, MessageState.Request, Constants.None);
                broker.Publish(requestHeader);
                chatProcess.Kill();
            }
            if (chatProcess == null)
            {
                //ProcessStartInfo info = new ProcessStartInfo();
                //info.CreateNoWindow = true;
                //info.FileName = "D:\\workspace\\LunkerFrontend\\LunkerChatServer\\bin\\Debug\\LunkerChatServer.exe";

                chatProcess = Process.Start(socketChatServer);
            }
            else
            {
                if (chatProcess.HasExited || chatProcess.Responding)
                {
                    //ProcessStartInfo info = new ProcessStartInfo();
                    //info.CreateNoWindow = true;
                    //info.FileName = "D:\\workspace\\LunkerFrontend\\LunkerChatServer\\bin\\Debug\\LunkerChatServer.exe";

                    chatProcess = Process.Start(socketChatServer);
                }
            }

            AAHeader responseHeader = new AAHeader(MessageType.RestartApp, MessageState.Success, Constants.None);
            //await NetworkManager.SendAsyncTask(adminSocket, responseHeader);
            //await NetworkManager.SendAsync(adminSocket, responseHeader);
            NetworkManager.Send(adminSocket, responseHeader);

        }// end 
    }
}