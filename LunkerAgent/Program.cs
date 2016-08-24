using log4net;
using LunkerAgent.src;
using LunkerAgent.src.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunkerAgent
{
    class Program
    {
        private static ILog logger = AgentLogger.GetLoggerInstance();
        static void Main(string[] args)
        {
            Console.Title = "Agent";
            logger.Debug("----------------------------------------------start-------------------");
            AdminAgent agent = AdminAgent.GetInstance();
            agent.Start();
        }
    }
}
