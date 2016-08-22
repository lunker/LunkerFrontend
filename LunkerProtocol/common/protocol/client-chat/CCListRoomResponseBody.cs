
public struct CCListRoomResponseBody : Body
{
    ChattingRoom[] chattingRoomList;


    public CCListRoomResponseBody(ChattingRoom[] chattingRoomList)
    {
        this.chattingRoomList = chattingRoomList;
    }

    public ChattingRoom[] ChattingRoomList
    {
        get { return chattingRoomList; }
        set { chattingRoomList = value; }
    }

}