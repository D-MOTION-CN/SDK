namespace Connect;

[Flags]
public enum WarningCodes
{
    DriverWarning        = 0x00000001, //驱动器警告      
    EStopActive          = 0x00000002, //急停被按下
    TargetOverStroke     = 0x00000004, //目标位置超过最大行程 
    TargetOverMaxPose    = 0x00000008, //目标位置超过最大幅值 
    OverSpeed            = 0x00000010, //位置指令超速度限制  
    OverMotorTorque      = 0x00000020, //超过电机力矩限制   
    OpenFileFailed       = 0x00000040, //打开数据文件错误   
    CabinetAcMalfunction = 0x00000080, //机柜空调故障     
    TheHatchIsOpened     = 0x00000100, //舱门被打开      
}