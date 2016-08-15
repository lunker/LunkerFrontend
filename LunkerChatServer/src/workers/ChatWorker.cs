using LunkerLibrary.common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LunkerChatServer.src.workers
{
    public class ChatWorker
    {
        private static BEWorker beWorker = BEWorker.GetInstance();
        private ConnectionManager connectionManager = ConnectionManager.GetInstance();


        public static void HandleCreateRoomRequest()
        {
            beWorker.HandleCreateRoomRequest();
        }

        // message from BE 
        public static void HandleCreateRoomResponse(int bodyLength)
        {
            NetworkManager.ReadAsync();        
        }
    }
}
