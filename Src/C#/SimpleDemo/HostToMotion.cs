namespace Connect;

public struct HostToMotion
{
    public uint PacketLength = 128;
    public uint SequenceCount;
    public uint Reversed;
    public uint MessageID;

    public eCommandWord CommandWord;
    public ushort       PlaybackFileID;
    public ushort       SubCmd;
    public ushort       Reversed3;
    public uint         Reversed4;
    public uint         Reversed5;

    public float Pitch;
    public float Roll;
    public float Yaw;
    public float Sway;
    public float Surge;
    public float Heave;

    public float Var11;
    public float Var12;
    public float Var13;
    public float Var14;
    public float Var15;
    public float Var16;

    public float Var21;
    public float Var22;
    public float Var23;
    public float Var24;
    public float Var25;
    public float Var26;

    public float Var31;
    public float Var32;
    public float Var33;
    public float Var34;
    public float Var35;
    public float Var36;

    public HostToMotion()
    {
        SequenceCount  = 0;
        Reversed       = 0;
        MessageID      = 0;
        CommandWord    = eCommandWord.COMMAND_NULL;
        PlaybackFileID = 0;
        SubCmd         = 0;
        Reversed3      = 0;
        Reversed4      = 0;
        Reversed5      = 0;
        Pitch          = 0;
        Roll           = 0;
        Yaw            = 0;
        Sway           = 0;
        Surge          = 0;
        Heave          = 0;
        Var11          = 0;
        Var12          = 0;
        Var13          = 0;
        Var14          = 0;
        Var15          = 0;
        Var16          = 0;
        Var21          = 0;
        Var22          = 0;
        Var23          = 0;
        Var24          = 0;
        Var25          = 0;
        Var26          = 0;
        Var31          = 0;
        Var32          = 0;
        Var33          = 0;
        Var34          = 0;
        Var35          = 0;
        Var36          = 0;
    }
}