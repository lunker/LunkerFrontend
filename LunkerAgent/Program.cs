using LunkerAgent.src;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunkerAgent
{
    class Program
    { 
        static void Main(string[] args)
        {
            AdminAgent agent = AdminAgent.GetInstance();
            agent.Start();
        }
    }
}
