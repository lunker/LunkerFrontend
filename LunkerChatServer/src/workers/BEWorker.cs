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
        private Socket beServer;// connection for BE Server

        private BEWorker() { }
        public static BEWorker GetInstance()
        {
            if (worker == null)
            {
                worker = new BEWorker();
            }
            return worker;
        }

        public async void HandleChatting(CommonHeader header)
        {
            CommonHeader requestHeader = new CommonHeader(MessageType.Chatting, MessageState.Request, 0, new Cookie(0), header.UserInfo);
            await NetworkManager.SendAsyncTask(beServer, requestHeader);
        }

        public async void HandleCreateRoomRequest(CommonHeader header)
        {
            CommonHeader requestHeader = new CommonHeader(MessageType.CreateRoom, MessageState.Request, 0, new Cookie(), new UserInfo());
            await NetworkManager.SendAsyncTask(beServer, requestHeader);

            /*
            CCHeader header = new CCHeader(MessageType.CreateRoom, MessageState.Request, 0);

            Task sendTask = NetworkManager.SendAsyncTask(peer, header);

            sendTask.ContinueWith((parent) =>
            {
                Task readTask = NetworkManager.ReadAsyncTask(peer, );
            });
            */

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
