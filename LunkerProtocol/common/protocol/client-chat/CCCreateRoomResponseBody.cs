public struct CCCreateRoomResponseBody : Body
{
    ChattingRoom chattingRoom;

    public CCCreateRoomResponseBody(ChattingRoom chattingRoom)
    {
        this.chattingRoom = chattingRoom;
    }

    public ChattingRoom ChattingRoom
    {
        get { return chattingRoom; }
        set { chattingRoom = value; }
    }
}
