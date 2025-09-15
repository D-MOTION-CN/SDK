// UdpMotionClient.cpp
// 固定IP与端口：本地 127.0.0.1:10010，远端 127.0.0.1:10000
// 使用 WinSock + Timer Queue，每 10ms 周期发送；控制台输入修改 Command_word / 切换 ROLL 正弦 / 退出。

#include <winsock2.h>
#include <ws2tcpip.h>
#include <windows.h>
#pragma comment(lib, "ws2_32.lib")

#include <atomic>
#include <chrono>
#include <cstdint>
#include <cstring>
#include <iostream>
#include <string>
#include <thread>
#include <cmath>

static const char* kRemoteIp = "127.0.0.1";
static const uint16_t kRemotePort = 10000;
static const char* kLocalIp = "127.0.0.1";
static const uint16_t kLocalPort = 10010;

#pragma pack(push, 1)
struct HOST_TO_MOTION
{
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

    float Pitch, Roll, Yaw;
    float sway, surge, heave;

    float var11, var12, var13, var14, var15, var16;
    float var21, var22, var23, var24, var25, var26;
    float var31, var32, var33, var34, var35, var36;
};

struct MOTION_TO_HOST
{
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

    float stroke1, stroke2, stroke3, stroke4, stroke5, stroke6;

    unsigned int driver1_errorCode, driver2_errorCode, driver3_errorCode;
    unsigned int driver4_errorCode, driver5_errorCode, driver6_errorCode;

    float motor1_torque, motor2_torque, motor3_torque;
    float motor4_torque, motor5_torque, motor6_torque;
};
#pragma pack(pop)

// ---------------- 全局状态 ----------------
struct TxContext {
    SOCKET sock{};
    sockaddr_in remote{};
    std::chrono::steady_clock::time_point t0;
    std::atomic<bool> running{ true };
    std::atomic<bool> rollSine{ false };
    std::atomic<uint32_t> seq{ 1 };
    std::atomic<uint16_t> commandWord{ 0x0001 };
};

static void FillFrame(HOST_TO_MOTION& pkt, const TxContext& cx)
{
    std::memset(&pkt, 0, sizeof(pkt));
    pkt.Packet_length = sizeof(HOST_TO_MOTION);
    pkt.Packet_sequence_count = cx.seq.load();
    pkt.Message_ID = 0x1001;                // 按需调整
    pkt.Command_word = cx.commandWord.load(); // 来自控制台输入

    // 仅演示 ROLL 正弦；其余姿态/位移置零（按需映射）
    if (cx.rollSine.load()) {
        using clock = std::chrono::steady_clock;
        float t = std::chrono::duration<float>(clock::now() - cx.t0).count();
        // 设定：幅值 5.0（单位自定：度或弧度，按你系统约定），频率 0.5 Hz
        float amplitude = 5.0f;
        float freq = 0.5f;
        pkt.Roll = amplitude * std::sin(2.0f * 3.1415926f * freq * t);
        pkt.Message_ID = 100;  // 必须
        pkt.Command_word = 2;
    }
    else {
        pkt.Roll = 0.0f;
    }
}

// Timer Queue 回调（每 10ms 调用一次）
VOID CALLBACK TxTimerCallback(PVOID lpParameter, BOOLEAN /*TimerOrWaitFired*/)
{
    TxContext* cx = reinterpret_cast<TxContext*>(lpParameter);
    if (!cx->running.load()) return;

    HOST_TO_MOTION tx{};
    FillFrame(tx, *cx);

    int sent = sendto(cx->sock, reinterpret_cast<const char*>(&tx), sizeof(tx), 0,
        reinterpret_cast<sockaddr*>(&cx->remote), sizeof(cx->remote));
    if (sent == sizeof(tx)) {
        cx->seq.fetch_add(1, std::memory_order_relaxed);
    }
    else {
        std::cerr << "[TX] sendto failed, code=" << WSAGetLastError() << "\n";
    }
}

// 接收线程：持续接收，但只每秒打印一次
static void RxLoop(SOCKET sock, std::atomic<bool>& running)
{
    DWORD tv = 200; // 200ms timeout，便于退出
    setsockopt(sock, SOL_SOCKET, SO_RCVTIMEO, (const char*)&tv, sizeof(tv));

    char buf[2048];
    sockaddr_in from{};
    int fromlen = sizeof(from);

    MOTION_TO_HOST lastRsp{}; // 保存最新的一帧
    auto lastPrint = std::chrono::steady_clock::now();

    while (running.load()) {
        int n = recvfrom(sock, buf, sizeof(buf), 0, (sockaddr*)&from, &fromlen);
        if (n == sizeof(MOTION_TO_HOST)) {
            std::memcpy(&lastRsp, buf, sizeof(lastRsp));
        }

        // 每秒打印一次
        auto now = std::chrono::steady_clock::now();
        if (std::chrono::duration_cast<std::chrono::seconds>(now - lastPrint).count() >= 1) {
            lastPrint = now;
            std::cout << "[RX] seq=" << lastRsp.Packet_sequence_count
                << " status=" << lastRsp.Status_word
                << " Heave=" << lastRsp.Heave_actual_position
                << " Roll=" << lastRsp.Roll_actual_position
                << "\n";
        }
    }
}

// 控制台输入线程：数字→Command_word；A/a→切换ROLL正弦；Q/q→退出
static void InputLoop(TxContext& cx)
{
    std::cout << "输入说明：\n"
        << "  - 输入整数并回车：修改 Command_word（例如 3 回车）\n"
        << "  - 输入 A 并回车：切换 ROLL 正弦运动开/关\n"
        << "  - 输入 Q 并回车：退出程序\n";
    std::string line;
    while (cx.running.load()) {
        if (!std::getline(std::cin, line)) {
            // 管道/控制台关闭时也退出
            cx.running.store(false);
            break;
        }
        if (line.empty()) continue;

        if (line.size() == 1) {
            char c = (char)std::toupper((unsigned char)line[0]);
            if (c == 'Q') {
                cx.running.store(false);
                break;
            }
            else if (c == 'A') {
                bool newState = !cx.rollSine.load();
                cx.rollSine.store(newState);
                std::cout << "[Input] ROLL 正弦运动: " << (newState ? "开启" : "关闭") << "\n";
                continue;
            }
        }

        try {
            unsigned long v = std::stoul(line);
            if (v > 0xFFFFul) v = 0xFFFFul;
            cx.commandWord.store(static_cast<uint16_t>(v));
            std::cout << "[Input] Command_word = " << v << "\n";
        }
        catch (...) {
            std::cout << "[Input] 无法解析为整数或命令。请输入数字 / A / Q。\n";
        }
    }
}

int main()
{
    // WinSock 初始化
    WSADATA wsa{};
    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0) {
        std::cerr << "WSAStartup failed\n";
        return 1;
    }

    SOCKET sock = ::socket(AF_INET, SOCK_DGRAM, 0);
    if (sock == INVALID_SOCKET) {
        std::cerr << "socket() failed\n";
        WSACleanup();
        return 2;
    }

    // 绑定本地 127.0.0.1:10010
    sockaddr_in local{};
    local.sin_family = AF_INET;
    local.sin_port = htons(kLocalPort);
    inet_pton(AF_INET, kLocalIp, &local.sin_addr);
    if (::bind(sock, (sockaddr*)&local, sizeof(local)) != 0) {
        std::cerr << "bind(" << kLocalIp << ":" << kLocalPort << ") failed, code=" << WSAGetLastError() << "\n";
        closesocket(sock);
        WSACleanup();
        return 3;
    }

    // 远端 127.0.0.1:10000
    sockaddr_in remote{};
    remote.sin_family = AF_INET;
    remote.sin_port = htons(kRemotePort);
    inet_pton(AF_INET, kRemoteIp, &remote.sin_addr);

    TxContext cx;
    cx.sock = sock;
    cx.remote = remote;
    cx.t0 = std::chrono::steady_clock::now();

    std::cout << "[Info] Local  = " << kLocalIp << ":" << kLocalPort << "\n";
    std::cout << "[Info] Remote = " << kRemoteIp << ":" << kRemotePort << "\n";
    std::cout << "[Info] 周期发送 = 10 ms\n";

    // 启动接收线程
    std::thread rxThr(RxLoop, sock, std::ref(cx.running));

    // 启动输入线程
    std::thread inThr(InputLoop, std::ref(cx));

    // 创建定时器队列 + 10ms 周期定时器
    HANDLE timerQueue = CreateTimerQueue();
    if (!timerQueue) {
        std::cerr << "CreateTimerQueue failed, code=" << GetLastError() << "\n";
        cx.running.store(false);
    }

    HANDLE txTimer = nullptr;
    if (timerQueue) {
        // 初始延迟 10ms，周期 10ms
        if (!CreateTimerQueueTimer(
            &txTimer, timerQueue, TxTimerCallback, &cx,
            /*DueTime*/10, /*Period*/10, WT_EXECUTEDEFAULT)) {
            std::cerr << "CreateTimerQueueTimer failed, code=" << GetLastError() << "\n";
            cx.running.store(false);
        }
    }

    // 等待退出
    while (cx.running.load()) {
        std::this_thread::sleep_for(std::chrono::milliseconds(50));
    }

    // 停止定时器与线程
    if (txTimer) {
        DeleteTimerQueueTimer(timerQueue, txTimer, INVALID_HANDLE_VALUE);
    }
    if (timerQueue) {
        DeleteTimerQueueEx(timerQueue, INVALID_HANDLE_VALUE);
    }

    if (inThr.joinable()) inThr.join();
    if (rxThr.joinable()) rxThr.join();

    closesocket(sock);
    WSACleanup();
    std::cout << "[Info] 已退出。\n";
    return 0;
}
