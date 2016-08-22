
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
            if (instance == null)
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

        /// <summary>
        /// Logout Client User Connection
        /// </summary>
        /// <param name="endpoint"></param>
        public void LogoutClient(string endpoint)
        {
            // 1)
            UserInfo userInfo = GetClientInfo(endpoint);

            string userId = new string(userInfo.Id);

            // 2) 
            Socket clientSocket = null;
            clientConnections.TryGetValue(userId, out clientSocket);
            if (clientSocket.Connected)
                clientSocket.Close();
            clientSocket.Dispose();
            clientSocket = null;
            //clientConnections.Remove(userId);        
            DeleteClientConnection(userId);

            // 3) 
            DeleteClientConnection(userId);

            // 4)
            ChattingRoom enteredRoom = GetChattingRoomJoinInfo(userId);

            DeleteChattingRoomListInfoValue(enteredRoom, userId);
            // delete room 
            if (GetChattingRoomListInfoCount(enteredRoom) == 0)
            {
                DeleteChattingRoomListInfoKey(enteredRoom);
            }

            DeleteChattingRoomJoinInfo(userId);
   
        }

    /**
     * Client Info
     */

    public Dictionary<string, UserInfo>  GetClientInfoDic()
        {
            return clientInfos;
        }

        public void AddClientInfo(string endpoint, UserInfo userInfo)
        {
            clientInfos.Add(endpoint, userInfo);
        }

        public UserInfo GetClientInfo(string endpoint)
        {
            UserInfo userInfo = default(UserInfo);

            clientInfos.TryGetValue(endpoint, out userInfo);

            return userInfo;
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
        public void DeleteAuthInfo(string id)
        {
            authInfos.Remove(id);
        }

        public Cookie GetAuthInfo(string id)
        {
            Cookie cookie = default(Cookie);

            authInfos.TryGetValue(id, out cookie);
            return cookie;
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
        /// <summary>
        /// 해당 채팅방에 접속해 있는 유저의 수 
        /// </summary>
        /// <param name="chatRoom"></param>
        /// <returns></returns>
        public int GetChattingRoomListInfoCount(ChattingRoom chatRoom)
        {
            HashSet<string> userInfo = null;

            if(chattingRoomListInfo.TryGetValue(chatRoom, out userInfo))
            {
                return Constants.NonExistedRoom; // 해당 채팅방이 없음 .
            }
            else
                return userInfo.Count;
        }

        
        /// <summary>
        /// user Enter to chatting room
        /// </summary>
        /// <param name="chatRoom">chatting room </param>
        /// <param name="id">entered user id</param>
        public void AddChattingRoomListInfoValue(ChattingRoom chatRoom, string id)
        {
            HashSet<string> userInfo = null;

            chattingRoomListInfo.TryGetValue(chatRoom, out userInfo);
            userInfo.Add(id);
        }

        /// <summary>
        /// create new chatting room
        /// </summary>
        /// <param name="chatRoom"></param>
        public void AddChattingRoomListInfoKey(ChattingRoom chatRoom)
        {
            HashSet<string> userInfo = new HashSet<string>();

            chattingRoomListInfo.Add(chatRoom, userInfo);// create new chatting room
        }

        /// <summary>
        /// 해당 chattingroom에서 user를 나가기 시킨다.
        /// <para>remove된것이 dic에도 반영이 될지는 모르겠다. . . .</para>
        /// </summary>
        /// <param name="chatRoom"></param>
        /// <param name="id"></param>
        public void DeleteChattingRoomListInfoValue(ChattingRoom chatRoom, string id)
        {
            HashSet<string> userInfo = null;

            chattingRoomListInfo.TryGetValue(chatRoom, out userInfo);
            userInfo.Remove(id);
            
        }

        public void DeleteChattingRoomListInfoKey(ChattingRoom chatRoom)
        {
            chattingRoomListInfo.Remove(chatRoom);
        }

        public HashSet<string> GetChattingRoomListInfoKey(ChattingRoom chatRoom)
        {
            HashSet<string> userInfo = null;
            chattingRoomListInfo.TryGetValue(chatRoom, out userInfo);

            return userInfo;
        }
    }
}
