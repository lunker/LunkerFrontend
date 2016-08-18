public struct CCJoinRequestBody : Body
{
    ChattingRoom roomInfo;
    public CCJoinRequestBody(ChattingRoom roomInfo)
    {
        this.roomInfo = roomInfo;
    }

    public ChattingRoom RoomInfo
    {
        get { return this.roomInfo; }
        set { roomInfo = value; }
    }
}
