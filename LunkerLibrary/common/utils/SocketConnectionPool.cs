using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LunkerLibrary.common.Utils
{
    public class SocketConnectionPool
    {
        private static List<Socket> connectionList = null;

        private SocketConnectionPool() { }
        
        public static List<Socket> GetAllConnection()
        {
            return connectionList;
        }

        public static Socket GetConnection()
        {
            return null; 
        }

        
    }
}
