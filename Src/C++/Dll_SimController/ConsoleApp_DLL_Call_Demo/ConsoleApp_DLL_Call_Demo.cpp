#include <windows.h>
#include <cstdio>
#include <cstdint>
#include <string>
#include <thread>
#include <atomic>
#include <chrono>
#include <cmath>
#include <iostream>  

#include "SimController.h"  // 你的DLL头文件

using namespace std::chrono;

// ======== 正弦运动参数（可按需调整）========
static constexpr float kRollAmp = 5.0f;   // 振幅（度）或弧度，按你的协议约定
static constexpr float kRollFreq = 0.5f;   // 频率(Hz)
static constexpr float kTwoPi = 6.28318530718f;

// ======== 运行标志 ========
std::atomic<bool> g_running{ true };
std::atomic<bool> g_rollSine{ false };

int main() {
    printf("=== Motion Console Demo ===\n");
    printf("数字+回车: 设置 Command_word\n");
    printf("A/a +回车: 切换 ROLL 正弦运动\n");
    printf("Q/q +回车: 退出\n\n");

    // 连接（IP/端口在DLL中已固定为常量）
    if (Sim_Connect() <= 0) {
        printf("连接失败（Sim_Connect）\n");
        return 0;
    }
    Sim_SetSendPeriodMs(10);    // 10ms 周期（DLL内本身就按这个周期发）
    printf("已连接，发送周期=10ms\n");

    // 工作者线程：周期设置目标姿态 & 读回包、打印（每秒一次）
    std::thread worker([] {
        auto t0 = steady_clock::now();
        auto nextTick = t0;
        auto nextPrint = t0;

        while (g_running.load()) {
            nextTick += milliseconds(10);  // 10ms 应和 DLL 周期一致

            Sim_SetMessageId(100);

            // ROLL 正弦
            float roll = 0.0f;
            if (g_rollSine.load()) {
                auto now = steady_clock::now();
                float t = duration<float>(now - t0).count();
                roll = kRollAmp * std::sinf(kTwoPi * kRollFreq * t);
            }

            // 只改 Roll，其余保持 0（或也可保留上次目标）
            Sim_SetTargetPose(
                /*pitch*/ 0.0f,
                /*roll*/  roll,
                /*yaw*/   0.0f,
                /*sway*/  0.0f,
                /*surge*/ 0.0f,
                /*heave*/ 0.0f
            );

            // 每秒打印一次回传（避免刷屏）
            auto now = steady_clock::now();
            if (now >= nextPrint) {
                MOTION_TO_HOST rx{};
                int got = Sim_GetMotionToHost(&rx);
                if (got >= 0) {
                    printf("[RX] id=%u, status=0x%04X, roll=%.3f, heave=%.3f\n",
                        rx.Message_ID, rx.Status_word,
                        rx.Roll_actual_position, rx.Heave_actual_position);
                }
                else {
                    printf("[RX] 读取失败 got=%d\n", got);
                }
                nextPrint = now + seconds(1);
            }

            std::this_thread::sleep_until(nextTick);
        }
        });

    // 主线程：阻塞式读取一行指令
    std::string line;
    while (true) {
        printf("> ");
        if (!std::getline(std::cin, line)) break; // EOF 退出

        // 去掉前后空白
        while (!line.empty() && (line.back() == '\r' || line.back() == '\n' || line.back() == ' ' || line.back() == '\t')) line.pop_back();
        size_t p = 0; while (p < line.size() && (line[p] == ' ' || line[p] == '\t')) ++p;
        std::string cmd = line.substr(p);

        if (cmd.empty()) continue;

        // Q/q 退出
        if (cmd == "Q" || cmd == "q") {
            printf("退出...\n");
            break;
        }

        // A/a 切换正弦
        if (cmd == "A" || cmd == "a") {
            bool on = !g_rollSine.load();
            g_rollSine.store(on);
            printf("ROLL 正弦运动：%s\n", on ? "开启" : "关闭");
            continue;
        }

        // 其他：尝试解析为数字 → 设置 Command_word
        try {
            // 支持十进制或 0x 开头十六进制
            uint32_t val = 0;
            if (cmd.rfind("0x", 0) == 0 || cmd.rfind("0X", 0) == 0) {
                val = std::stoul(cmd, nullptr, 16);
            }
            else {
                val = std::stoul(cmd, nullptr, 10);
            }
            Sim_SetControlWord(static_cast<uint16_t>(val));
            printf("已设置 Command_word = %u (0x%04X)\n", (unsigned)val, (unsigned)val & 0xFFFF);
        }
        catch (...) {
            printf("无效输入：请输入数字、A/a 或 Q/q\n");
        }
    }

    // 收尾
    g_running.store(false);
    if (worker.joinable()) worker.join();

    Sim_Disconnect();
    printf("已断开。\n");
    return 0;
}
