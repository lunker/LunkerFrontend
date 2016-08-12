using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using log4net;
using LunkerRedis.src.Utils;
using System.Net;
using LunkerChatServer.src.Utils;

namespace LunkerChatServer
{
    public class ChatServer
    {
        private delegate void RequestHandler(int bodyLength); // message type에 따라 해당되는 함수를 찾아서, delegate를 통해 호출한다! 

        private ILog logger = Logger.GetLoggerInstance();
        private Socket sockListener = null;
        //ip:port -> socket 
        private Dictionary<string, Socket> clientSocketDic = null;
        private List<Socket> readSocketList = null;
        private List<Socket> writeSocketList = null;
        private List<Socket> errorSocketList = null;
        
        // chat server main thread
        public void Start()
        {
            
            logger.Debug("[ChatServer][Start()] start");
            Initialize();
           
            while (true)
            {
                GetClientRequest(); // isCOmplete를 사용하자!!!! 메모리터진다지금 

                if(clientSocketDic.Count != 0)
                {
                    readSocketList = clientSocketDic.Values.ToList();
                    writeSocketList = clientSocketDic.Values.ToList();
                    errorSocketList = clientSocketDic.Values.ToList();
                }
                    
                // 접속한 client가 있을 경우에만 수행.
                if(clientSocketDic.Count != 0)
                {
                    Socket.Select(readSocketList, writeSocketList, errorSocketList, 0);

                    /**
                     * 
                     * Read, Write를 하나로 묶을까???
                     */

                    // read work 
                    if (readSocketList.Count == 0)
                    {
                        
                    }
                    else
                    {
                        // readSocketList에 남아있는 socket에 대해서만 read 수행.
                        foreach (Socket client in readSocketList)
                        {
                            ReadAsync(client);
                        }
                    }

                    // write work 
                }

            }// end loop 
            

            logger.Debug("[ChatServer][Start()] end");
            Console.ReadKey();
        }

        public void Initialize()
        {
            clientSocketDic = new Dictionary<string, Socket>();

            readSocketList = new List<Socket>();
            writeSocketList = new List<Socket>();
            errorSocketList = new List<Socket>();

            // initialiize client socket listener
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, AppConfig.GetInstance().Port);

            sockListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sockListener.Bind(ep);
            sockListener.Listen(AppConfig.GetInstance().Backlog);

        }// end method 

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

            clientSocketDic.Add(idBuilder.ToString(), handler);

            logger.Debug("[ChatServer][AcceptAsync()] add handler to list");
            return;

        }

        public async Task<Socket> AcceptAsync() {

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

        }
        
        public void AcceptCallback(IAsyncResult ar)
        {
            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            
            // Create the state object.
            IPEndPoint ep = (IPEndPoint) handler.RemoteEndPoint;
            StringBuilder idBuilder = new StringBuilder();
            idBuilder.Append(ep.Address);
            idBuilder.Append(":");
            idBuilder.Append(ep.Port);

            clientSocketDic.Add(idBuilder.ToString(), handler);
            //logger.Debug("[ChatServer][AcceptAsync()][AcceptCallback()] aceept client connect request");
            return; 
        }// end method

        public async void ReadAsync(Socket peer)
        {
            logger.Debug("[ChatServer][ReadAsync()] start");
            byte[] buff = new byte[1024];

            try
            {
                Task readTask = Task.Factory.FromAsync(peer.BeginReceive(buff, 0, buff.Length, SocketFlags.None, null, peer), peer.EndReceive);
                Task convertTask = null;

                //readTask.ContinueWith(GenerateHeader);
                readTask.ContinueWith((parent) =>
                {
                    string getMsg = Encoding.UTF8.GetString(buff);
                    Console.WriteLine(getMsg);
                });

                readTask.Wait();
            }
            catch (SocketException se)
            {
                Console.WriteLine("disconnected");

                IPEndPoint ep = (IPEndPoint)peer.RemoteEndPoint;
                string key = ep.Address + ":" + ep.Port;

                clientSocketDic.Remove(key);
                peer.Close();
            }

            //return readTask;
            logger.Debug("[ChatServer][ReadAsync()] end");
            return;
        }

        public void GenerateHeader(Task parent)
        {

        }

        public void ReadAsyncTask()
        {

        }

        public void SendAsync()
        {

        }

    }
}
