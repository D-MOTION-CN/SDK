#pragma once
#include <cstdint>

#ifdef SIMCTRL_EXPORTS
#  define SIMCTRL_API extern "C" __declspec(dllexport)
#else
#  define SIMCTRL_API extern "C" __declspec(dllimport)
#endif

#ifndef SIMCTRL_CALL
#  define SIMCTRL_CALL __stdcall
#endif

#pragma pack(push, 1)

// ====== 来自你的定义 ======
struct HOST_TO_MOTION {
    unsigned int Packet_length;
    unsigned int Packet_sequence_count;
    unsigned int Reserved;
    unsigned int Message_ID;

    unsigned short Command_word;
    unsigned short Playback_File_ID;
    unsigned short SubCmd;
    unsigned short Reserved2;
    unsigned int Reserved3;
    unsigned int Reserved4;

    float Pitch; float Roll; float Yaw;
    float sway; float surge; float heave;

    float var11; float var12; float var13; float var14; float var15; float var16;
    float var21; float var22; float var23; float var24; float var25; float var26;
    float var31; float var32; float var33; float var34; float var35; float var36;
};

struct MOTION_TO_HOST {
    unsigned int Packet_length;
    unsigned int Packet_sequence_count;
    unsigned int Reserved;
    unsigned int Message_ID;

    unsigned short Status_word;
    unsigned short Motion_state;
    unsigned short IO_word;
    unsigned short Reserved2;
    unsigned int Error_code;
    unsigned int Warnning_code;

    float Pitch_actual_position;
    float Roll_actual_position;
    float Yaw_actual_position;
    float Sway_actual_position;
    float Surge_actual_position;
    float Heave_actual_position;

    float stroke1; float stroke2; float stroke3;
    float stroke4; float stroke5; float stroke6;

    unsigned int driver1_errorCode; unsigned int driver2_errorCode; unsigned int driver3_errorCode;
    unsigned int driver4_errorCode; unsigned int driver5_errorCode; unsigned int driver6_errorCode;

    float motor1_torque; float motor2_torque; float motor3_torque;
    float motor4_torque; float motor5_torque; float motor6_torque;
};
#pragma pack(pop)

// ================== 常量定义 ==================
constexpr const char* SIM_REMOTE_IP = "127.0.0.1";
//constexpr const char* SIM_REMOTE_IP = "192.168.1.150";
constexpr uint16_t    SIM_REMOTE_PORT = 10000;
constexpr const char* SIM_LOCAL_IP = "127.0.0.1";
//constexpr const char* SIM_LOCAL_IP = "192.168.1.200";
constexpr uint16_t    SIM_LOCAL_PORT = 10010;

// ================== DLL 接口 ==================
SIMCTRL_API int  SIMCTRL_CALL Sim_Connect();   // 不再需要参数，直接用常量
SIMCTRL_API void SIMCTRL_CALL Sim_Disconnect();

SIMCTRL_API void SIMCTRL_CALL Sim_SetControlWord(uint16_t controlWord);
SIMCTRL_API void SIMCTRL_CALL Sim_SetMessageId(uint32_t messageId);

SIMCTRL_API void SIMCTRL_CALL Sim_SetTargetPose(float pitch, float roll, float yaw,
    float sway, float surge, float heave);

SIMCTRL_API void SIMCTRL_CALL Sim_SetHostToMotion(const HOST_TO_MOTION* p);
SIMCTRL_API int  SIMCTRL_CALL Sim_GetMotionToHost(MOTION_TO_HOST* out);

SIMCTRL_API void SIMCTRL_CALL Sim_SetSendPeriodMs(uint32_t periodMs);
SIMCTRL_API int  SIMCTRL_CALL Sim_IsConnected();
