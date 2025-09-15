namespace Connect;

public unsafe struct MotionToHost
{
    public uint PacketLength;
    public uint SequenceCount;
    public uint Reserved;
    public uint MessageID;

    public eState StatusWord;
    public ushort MotionState;
    public ushort IOword;
    public ushort Reserved2;
    public uint   ErrorCode;
    public uint   WarningCode;


    public float DemandPitch;
    public float DemandRoll;
    public float DemandYaw;
    public float DemandSway;
    public float DemandSurge;
    public float DemandHeave;

    public fixed float Stroke[6];

    public fixed uint DriverErrorCode[6];

    public fixed float DriverTorque[6];
}