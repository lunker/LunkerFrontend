public struct CBLeaveRequestBody
{
    ChattingRoom roomInfo;
    public CBLeaveRequestBody(ChattingRoom roomInfo)
    {
        this.roomInfo = roomInfo;
    }

    public ChattingRoom RoomInfo
    {
        get { return this.roomInfo; }
        set { roomInfo = value; }
    }


}