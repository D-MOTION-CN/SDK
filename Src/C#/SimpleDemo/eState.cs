// ReSharper disable InconsistentNaming

namespace Connect;

public enum eState : ushort
{
    STATE_OFF               = 0,  //回底位后，下使能状态
    STATE_PoweredUp         = 1,  //初始上电状态
    STATE_Zeroing           = 2,  //寻零位
    STATE_Origin            = 3,  //底位状态
    STATE_Ascending         = 4,  //从零位到中立位
    STATE_Neutral           = 5,  //中立位状态
    STATE_Running           = 6,  //运行中
    STATE_MovingToNeutral   = 7,  //停止运行，到中立位过程状态
    STATE_ActuatorToNeutral = 8,  //从缸长运动状态到中位过程
    STATE_Descending        = 9,  //到底位过程状态
    STATE_Holding           = 10, //姿态保持
    STATE_Fault             = 11, //报错状态
    STATE_Emergency         = 12, //紧急停止状态

    STATE_PoweringUp   = 13, //正在启动
    STATE_PoweringDown = 14, //正在下使能
    STATE_Initialized  = 15, //已完成初始化

    STATE_ActuatorFollowUp  = 16, //行程随动模式
    STATE_ActuatorToSettled = 17, //行程随动模式回底位

    STATE_Frozen         = 18, //冻结(舱门打开,不能动)
    STATE_FaultResetting = 19, // 从故障中复位
    STATE_Maintenance    = 20, // 维护状态
}