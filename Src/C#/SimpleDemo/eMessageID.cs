namespace Connect;

public enum eMessageID
{
    msgid_connect_request    = 1,
    msgid_disconnect_request = 2,
    msgid_request_ack        = 11,
    msgid_request_nack       = 12,

    DOF_CommandData      = 100,
    Actuator_CommandData = 101,
    Washout_CommandData  = 102,
    Playback_CommandData = 103,

    PTP          = 106,
    Sine         = 107,
    ActuatorSine = 108,

    ReadParas          = 201,
    WriteParasToRAM    = 202,
    WriteParasToFLASH  = 203,
    msgid_SetIPAddress = 204,
    SetHomeMotorCode   = 211,

    ReadCalibrationData         = 301,
    WriteCalibrationDataToRAM   = 302,
    WriteCalibrationDataToFLASH = 303,

    msgid_SystemReset  = 99,
    msgid_SetHomePulse = 211,
    msgid_broadcast    = 255,
}