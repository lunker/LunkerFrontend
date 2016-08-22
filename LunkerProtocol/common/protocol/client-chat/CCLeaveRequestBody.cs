public struct CCLeaveRequestBody : Body
{ 
    ChattingRoom roomInfo;
    public CCLeaveRequestBody(ChattingRoom roomInfo)
    {
        this.roomInfo = roomInfo;
    }

    public ChattingRoom RoomInfo
    {
        get { return this.roomInfo; }
        set { roomInfo = value; }
    }


}