#include "pch.h" 

#define WIN32_LEAN_AND_MEAN
#define SIMCTRL_EXPORTS
#include "SimController.h"

#include <winsock2.h>
#include <ws2tcpip.h>
#include <atomic>
#include <thread>
#include <mutex>
#include <chrono>
#include <cstring>

#pragma comment(lib, "Ws2_32.lib")

namespace {

    struct UdpClient {
        SOCKET      sock = INVALID_SOCKET;
        sockaddr_in remote{};
        bool        connected = false;
    };

    class SimCore {
    public:
        int connect(const char* ip, uint16_t rPort, uint16_t lPort) {
            std::lock_guard<std::mutex> lk(mtx_);

            if (!wsaInited_) {
                WSADATA w;
                if (WSAStartup(MAKEWORD(2, 2), &w) != 0) return -100 - WSAGetLastError();
                wsaInited_ = true;
            }
            if (udp_.connected) return 1;

            udp_.sock = ::socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
            if (udp_.sock == INVALID_SOCKET) return -200 - WSAGetLastError();

            sockaddr_in local{};
            local.sin_family = AF_INET;
            local.sin_addr.s_addr = htonl(INADDR_ANY);
            local.sin_port = htons(lPort);
            if (bind(udp_.sock, reinterpret_cast<sockaddr*>(&local), sizeof(local)) == SOCKET_ERROR) {
                closesocket(udp_.sock); udp_.sock = INVALID_SOCKET; return -300 - WSAGetLastError();
            }

            // 非阻塞
            u_long nb = 1;
            ioctlsocket(udp_.sock, FIONBIO, &nb);

            // 目标地址并 connect
            udp_.remote.sin_family = AF_INET;
            udp_.remote.sin_port = htons(rPort);
            if (inet_pton(AF_INET, ip, &udp_.remote.sin_addr) != 1) {
                closesocket(udp_.sock); udp_.sock = INVALID_SOCKET; return -400 - WSAGetLastError();
            }
            if (::connect(udp_.sock, reinterpret_cast<sockaddr*>(&udp_.remote), sizeof(udp_.remote)) == SOCKET_ERROR) {
                closesocket(udp_.sock); udp_.sock = INVALID_SOCKET; return -500 - WSAGetLastError();
            }

            // 初始化发送帧固定字段
            std::memset(&tx_, 0, sizeof(tx_));
            tx_.Packet_length = sizeof(HOST_TO_MOTION); // 固定长度
            tx_.Packet_sequence_count = 0;
            // 其余 Reserved 均保持 0

            udp_.connected = true;
            stopFlag_ = false;
            worker_ = std::thread(&SimCore::loop, this);
            return 1;
        }

        void disconnect() {
            {
                std::lock_guard<std::mutex> lk(mtx_);
                if (!udp_.connected) return;
                stopFlag_ = true;
            }
            if (worker_.joinable()) worker_.join();

            std::lock_guard<std::mutex> lk2(mtx_);
            if (udp_.sock != INVALID_SOCKET) { closesocket(udp_.sock); udp_.sock = INVALID_SOCKET; }
            udp_.connected = false;

            if (wsaInited_) { WSACleanup(); wsaInited_ = false; }
        }

        void setControlWord(uint16_t cw) {
            std::lock_guard<std::mutex> lk(mtx_);
            tx_.Command_word = cw;           // 对应你的字段
        }

        void setMessageId(uint32_t id) {
            std::lock_guard<std::mutex> lk(mtx_);
            tx_.Message_ID = id;             // 对应你的字段
        }

        void setTargetPose(float pitch, float roll, float yaw, float sway, float surge, float heave) {
            std::lock_guard<std::mutex> lk(mtx_);
            tx_.Pitch = pitch;
            tx_.Roll = roll;
            tx_.Yaw = yaw;
            tx_.sway = sway;
            tx_.surge = surge;
            tx_.heave = heave;
        }

        void setHostToMotion(const HOST_TO_MOTION* in) {
            if (!in) return;
            std::lock_guard<std::mutex> lk(mtx_);
            tx_ = *in;

            // 强制修正长度与自增序号，由 DLL 维护，避免上层忘记填
            tx_.Packet_length = sizeof(HOST_TO_MOTION);
            // Packet_sequence_count 在发送线程里再统一自增，避免并发状态错位
        }

        int getMotionToHost(MOTION_TO_HOST* out) {
            if (!out) return -1;
            bool hasNew = newRx_.exchange(false);
            std::lock_guard<std::mutex> lk(mtx_);
            *out = rx_;
            return hasNew ? 1 : 0;
        }

        void setPeriod(uint32_t ms) {
            if (ms < 1) ms = 1;
            periodMs_.store(ms);
        }

        bool isConnected() const { return udp_.connected; }

        ~SimCore() { disconnect(); }

    private:
        void loop() {
            using namespace std::chrono;
            auto next = steady_clock::now();

            while (true) {
                {
                    std::lock_guard<std::mutex> lk(mtx_);

                    // 发送前维护长度/序号
                    tx_.Packet_length = sizeof(HOST_TO_MOTION);
                    tx_.Packet_sequence_count++;

                    int sent = ::send(udp_.sock, reinterpret_cast<const char*>(&tx_), (int)sizeof(tx_), 0);
                    (void)sent; // 可按需统计与错误处理
                }

                // 非阻塞接收：尽量把缓冲读干净，保留最新一帧
                for (;;) {
                    MOTION_TO_HOST tmp{};
                    int r = ::recv(udp_.sock, reinterpret_cast<char*>(&tmp), (int)sizeof(tmp), 0);
                    if (r == SOCKET_ERROR) {
                        int e = WSAGetLastError();
                        if (e == WSAEWOULDBLOCK) break;
                        // 其他错误可记录日志
                        break;
                    }
                    if (r == sizeof(MOTION_TO_HOST)) {
                        std::lock_guard<std::mutex> lk(mtx_);
                        rx_ = tmp;
                        newRx_.store(true);
                    }
                    else {
                        // 如果对端可能发变长数据，这里可改为缓冲/解析
                    }
                }

                next += milliseconds(periodMs_.load());
                std::this_thread::sleep_until(next);
            }
        }

    private:
        mutable std::mutex mtx_;
        UdpClient          udp_;
        std::thread        worker_;
        std::atomic<bool>  newRx_{ false };
        std::atomic<uint32_t> periodMs_{ 10 };
        bool               stopFlag_{ false };
        bool               wsaInited_{ false };

        HOST_TO_MOTION     tx_{};
        MOTION_TO_HOST     rx_{};
    };

    // 进程内单例
    SimCore& core() { static SimCore inst; return inst; }

} // namespace

// ============ 导出接口实现 ============
SIMCTRL_API int SIMCTRL_CALL Sim_Connect() {
    return core().connect(SIM_REMOTE_IP, SIM_REMOTE_PORT, SIM_LOCAL_PORT);
}
SIMCTRL_API void SIMCTRL_CALL Sim_Disconnect() { core().disconnect(); }

SIMCTRL_API void SIMCTRL_CALL Sim_SetControlWord(uint16_t controlWord) { core().setControlWord(controlWord); }
SIMCTRL_API void SIMCTRL_CALL Sim_SetMessageId(uint32_t messageId) { core().setMessageId(messageId); }

SIMCTRL_API void SIMCTRL_CALL Sim_SetTargetPose(float pitch, float roll, float yaw,
    float sway, float surge, float heave) {
    core().setTargetPose(pitch, roll, yaw, sway, surge, heave);
}

SIMCTRL_API void SIMCTRL_CALL Sim_SetHostToMotion(const HOST_TO_MOTION* p) { core().setHostToMotion(p); }
SIMCTRL_API int  SIMCTRL_CALL Sim_GetMotionToHost(MOTION_TO_HOST* out) { return core().getMotionToHost(out); }

SIMCTRL_API void SIMCTRL_CALL Sim_SetSendPeriodMs(uint32_t periodMs) { core().setPeriod(periodMs); }
SIMCTRL_API int  SIMCTRL_CALL Sim_IsConnected() { return core().isConnected() ? 1 : 0; }


