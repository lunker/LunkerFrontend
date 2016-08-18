public struct CBListRoomResponseBody : Body
{
    ChattingRoom[] chattingRoomList;

 
    public CBListRoomResponseBody(ChattingRoom[] chattingRoomList)
    {
        this.chattingRoomList = chattingRoomList;
    }

    public ChattingRoom[] ChattingRoomList
    {
        get { return chattingRoomList; }
        set { chattingRoomList = value; }
    }
}