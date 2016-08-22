using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LunkerLoginServer.src.utils
{
    public static class LoadBalancer
    {
        private static int turn = 0;
        private static int FECount = 0;

        public static void AddFE()
        {
            Interlocked.Increment(ref FECount);
        }

        public static void DeleteFE()
        {
            Interlocked.Decrement(ref FECount);
        }

        /// <summary>
        /// Get Next FE 
        /// </summary>
        /// <returns></returns>
        public static int RoundRobin()
        {
            return (++turn) % FECount;
        }

    }
}
