
public struct CBJoinRoomRequestBody : Body
{
    ChattingRoom roomInfo;
    public CBJoinRoomRequestBody(ChattingRoom roomInfo)
    {
        this.roomInfo = roomInfo;
    }

    public ChattingRoom RoomInfo
    {
        get { return this.roomInfo; }
        set { roomInfo = value; }
    }
}

