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

        private ILog logger = Logger.GetLoggerInstance();
        private Socket sockListener = null;

        public void Start()
        {
            Initialize();
            AcceptAsync();
        }

        public void Initialize()
        {
            sockListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, AppConfig.GetInstance().Port);
            sockListener.Bind(ep);
            sockListener.Listen(AppConfig.GetInstance().Backlog);
        }
        public void AcceptAsync() {
            IAsyncResult ar = sockListener.BeginAccept( new AsyncCallback(AcceptCallback), sockListener);

            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            logger.Debug("[ChatServer][AcceptAsync()] in acceptAsync() ");


        }
        
        public void AcceptCallback(IAsyncResult ar)
        {
            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            logger.Debug("[ChatServer][AcceptCallback()] in accpet callback");
            // Create the state object.
            /*
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(AsynchronousSocketListener.readCallback), state);
            */

            //return handler;
        }

    }
}
