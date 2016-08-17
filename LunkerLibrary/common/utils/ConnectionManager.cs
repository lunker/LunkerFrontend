
using LunkerLibrary.common.protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LunkerLibrary.common.Utils
{

    /// <summary>
    /// Connection Manager for Socket Connection.
    /// connections : client, 
    /// </summary>
    public class ConnectionManager
    {

        private static ConnectionManager instance = null;

        private Dictionary<string, UserInfo> clientInfos = null;// ip:port - user Id 

        // fe에 접속중인 client user의 정보 
        //ip:port -> socket    
        // Chat server에는 이미 로그인 된 유저가 Cookie를 들고 오므로,
        // 최초 connection맺은 후 
        // client가 id, cookie를 보내어 인증을 거친다.
        // 그 후에 완료된 client를 자료구조에 저장!!! 
        // 저장할때에는 사용자id, socket으로!
        private Dictionary<string, Socket> clientConnections = null;// user id - socket

        private Dictionary<string, Cookie> authInfos = null;// user id - cookie

        private Dictionary<string, ChattingRoom> chattingRoomJoinInfo = null; // user id - chatting room info 

        private Dictionary<ChattingRoom, HashSet<string>> chattingRoomListInfo = null;// roominfo ~ entered user info 

        private ConnectionManager()
        {
            clientConnections = new Dictionary<string, Socket>();
            clientInfos = new Dictionary<string, UserInfo>();
            authInfos = new Dictionary<string, Cookie>();

            chattingRoomJoinInfo = new Dictionary<string, ChattingRoom>();
            chattingRoomListInfo = new Dictionary<ChattingRoom, HashSet<string>>();
        }

        public static ConnectionManager GetInstance()
        {
            if(instance == null)
            {
                instance = new ConnectionManager();
            }
            return instance;
        }

        public void ReleaseAll()
        {
            /*
             string release?
            foreach (string key in clientConnections.Keys)
            {
                key = null;
            }
            */

            foreach (Socket key in clientConnections.Values)
            {
                key.Disconnect(false);
                key.Dispose();
            }
            clientConnections.Clear();
            clientConnections = null;


            GC.Collect();
        }


        /**
         * Client Info
         */

        public Dictionary<string, UserInfo>  GetClientInfo()
        {
            return clientInfos;
        }

        public void AddClientInfo(string endpoint, UserInfo userInfo)
        {
            clientInfos.Add(endpoint, userInfo);
        }

        public void DeleteClientInfo(string endpoint)
        {
            clientInfos.Remove(endpoint);
        }

        /*
         * Client Connection 
         * 
         */
        public Dictionary<string, Socket> GetClientConnectionDic()
        {
            return clientConnections;
        }

        public void AddClientConnection(string id, Socket peer)
        {
            clientConnections.Add(id, peer);
        }

        public void DeleteClientConnection(string id)
        {
            clientConnections.Remove(id);
        }

        public Socket GetClientConnection(string id)
        {
            Socket tmp = null;
            clientConnections.TryGetValue(id, out tmp);
            return tmp;
        }

        public int GetClientConnectionCount()
        {
            return clientConnections.Count;
        }

        //-------------------------------------------------------------------------------------//
        public Dictionary<string, Cookie> GetAuthInfoDic()
        {
            return authInfos;
        }
        /***
         * Auth Info
         * 
         */
        public void AddAuthInfo(string id, Cookie cookie)
        {
            authInfos.Add(id,cookie);
        }


        //-------------------------------------------------------------------------------------//
        //chattingRoomJoinInfo
        public Dictionary<string, ChattingRoom> GetChattingRoomJoinInfoDic()
        {
            return chattingRoomJoinInfo;
        }

        public void AddChattingRoomJoinInfo(string id, ChattingRoom room)
        {
            chattingRoomJoinInfo.Add(id, room);
        }

        public void DeleteChattingRoomJoinInfo(string id)
        {
            chattingRoomJoinInfo.Remove(id);
        }

        public ChattingRoom GetChattingRoomJoinInfo(string id)
        {
            ChattingRoom tmp = default(ChattingRoom);
            chattingRoomJoinInfo.TryGetValue(id, out tmp );

            return tmp;
        }

        public void ReleaseChattingRoomJoinInfo()
        {
            /*
            foreach (ChattingRoom chatRoom in chattingRoomJoinInfo.Values)
            {
                chatRoom = null;
            }
            */

        }

        //-------------------------------------------------------------------------------------//
        //chattingRoomListInfo
        public Dictionary<ChattingRoom, HashSet<string>> GetChattingRoomListInfoDic()
        {
            return chattingRoomListInfo;
        }

        public void AddChattingRoomListInfo(ChattingRoom chatRoom, string id)
        {
            HashSet<string> userInfo = null;

            chattingRoomListInfo.TryGetValue(chatRoom, out userInfo);
            userInfo.Add(id);
        }

        public void DeleteChattingRoomListInfo(ChattingRoom chatRoom, string id)
        {
            HashSet<string> userInfo = null;

            chattingRoomListInfo.TryGetValue(chatRoom, out userInfo);
            userInfo.Remove(id);
        }

        public HashSet<string> GetChattingRoomListInfo(ChattingRoom chatRoom)
        {
            HashSet<string> userInfo = null;
            chattingRoomListInfo.TryGetValue(chatRoom, out userInfo);

            return userInfo;
        }
    }
}
