namespace Connect;

public enum eCommandWord : ushort
{
    COMMAND_NULL             = 0,
    COMMAND_Neutral          = 1, //初始化，完成寻零位、到中立位状态
    COMMAND_Run              = 2, //使准备好，完成寻零位，到中立位，到运行状态
    COMMAND_Descend          = 3, //停止运动，回底位
    COMMAND_Off              = 4,
    COMMAND_Enable           = 5,
    COMMAND_FilePlay         = 6,
    COMMAND_Hold             = 7,
    COMMAND_FaultReset       = 8,
    COMMAND_Emergency        = 9, //急停指令
    COMMAND_ActuatorFollowUp = 10,
    COMMAND_Home             = 11,
    COMMAND_HomeRecord       = 12,
    COMMAND_Maintain         = 13, //进入维护模式
}