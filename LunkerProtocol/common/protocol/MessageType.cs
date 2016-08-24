
public enum MessageType : short
{
    Basic = 0,

    // connection setup between client ~ chat server 
    ConnectionSetup = 100,

    // chatting 
    Chatting = 200,

    // Membership : 300번대 
    Signup = 310,
    Signin = 320,
    Logout = 330,
    Modify = 340,
    Delete = 350,

    // Chatting Room : 400번대
    ListRoom = 400,
    JoinRoom = 410,
    LeaveRoom = 420,
    CreateRoom = 430,

    // Admin tool : 500번대 
    StartApp = 500,
    ShutdownApp = 510,
    RestartApp = 520,

    Total_Room_Count = 530, // type for request total room number in application
    FE_User_Status = 540, // type for request FE' user number
    Chat_Ranking = 550, // type for request chatting ranking 

    /*
    Monitoring 
    */
    // chat -> login 
    FENotice = 610,
    // chat -> BE
    BENotice = 650,

    // login -> chat 
    NoticeUserAuth = 640,
    // Chat -> BE
    VerifyCookie = 620,
    
    // send agent info to admin
    AgentInfo = 630,

    
    // Heartbeat 
    Heartbeat = 800
}