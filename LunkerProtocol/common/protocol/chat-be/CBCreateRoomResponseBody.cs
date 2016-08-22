using LunkerLibrary.common.protocol;

public struct CBCreateRoomResponseBody : Body
{
    //UserInfo userInfo;
    ChattingRoom chattingRoom;

    public CBCreateRoomResponseBody(ChattingRoom chattingRoom)
    {
        this.chattingRoom = chattingRoom;
    }

    public ChattingRoom ChattingRoom{
        get { return chattingRoom; }
        set { chattingRoom = value; }
    }
}