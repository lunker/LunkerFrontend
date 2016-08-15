using LunkerLibrary.common.protocol;
using LunkerLibrary.common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LunkerChatServer.src.workers
{
    class BEWorker
    {

        private static BEWorker worker = null;
        private Socket peer;// connection for 


        private BEWorker() { }
        public static BEWorker GetInstance()
        {
            if (worker == null)
            {
                worker = new BEWorker();
            }
            return worker;
        }

        public void HandleCreateRoomRequest()
        {
            CCHeader header = new CCHeader(MessageType.CreateRoom, MessageState.Request, 0);

            Task sendTask = NetworkManager.SendAsyncTask(peer, header);

            sendTask.ContinueWith((parent) =>
            {
                Task readTask = NetworkManager.ReadAsyncTask(peer, );
            });
        }
        public void HandleCreateRoomResponse()
        {

        }

        public void HandleJoinRoomRequest()
        {

        }

        public void HandleJoinRoomResponse()
        {

        }
    }
}
