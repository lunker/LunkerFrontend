using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunkerStarter
{
    class Program
    {
        static void Main(string[] args)
        {
            ProcessStartInfo login = new ProcessStartInfo();
            login.CreateNoWindow = false;
            login.FileName = "..\\..\\..\\LunkerLoginServer\\bin\\Debug\\LunkerLoginServer.exe";


            ProcessStartInfo socket = new ProcessStartInfo();
            socket.CreateNoWindow = false;
            socket.FileName = "..\\..\\..\\LunkerChatServer\\bin\\Debug\\LunkerChatServer.exe";

            ProcessStartInfo websocket = new ProcessStartInfo();
            websocket.CreateNoWindow = false;
            websocket.FileName = "..\\..\\..\\LunkerChatWebServer\\bin\\Debug\\LunkerChatWebServer.exe";

            Process.Start(login);
            Process.Start(socket);
            Process.Start(websocket);
        }
    }
}
