using System.Net;
using System.Net.Sockets;
using Timer = System.Timers.Timer;

namespace Connect
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            _timerConnectChecker.Elapsed += TimerConnectChecker_Elapsed;
        }

        private void ButtonConnect_Click(object sender, EventArgs e)
        {
            IsConnecting          = true;
            IsDisconnectRequested = false;
            Task.Run(StartUDP);
        }

        private void ButtonDisconnect_Click(object sender, EventArgs e)
        {
            if (!IsDisconnectRequested)
            {
                IsDisconnectRequested = true;
            }
        }


        private bool _isConnected;

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                _isConnected = value;

                UpdateUI();
            }
        }

        private bool _isConnecting;

        public bool IsConnecting
        {
            get => _isConnecting;
            private set
            {
                _isConnecting = value;
                UpdateUI();
            }
        }


        public bool IsDisconnectRequested;

        private void UpdateUI()
        {
            if (InvokeRequired)
            {
                Invoke(UpdateUI);
                return;
            }

            ButtonConnect.Enabled    = !IsConnected                  && !IsConnecting;
            ButtonDisconnect.Enabled = (IsConnected || IsConnecting) && !IsDisconnectRequested;
            labelIsConnected.Text    = IsConnected.ToString();
            ButtonSend.Enabled       = IsConnected;

            TextBoxLocalIP.Enabled        = !IsConnected && !IsConnecting;
            TextBoxLocalPort.Enabled      = !IsConnected && !IsConnecting;
            TextBoxMotionBaseIP.Enabled   = !IsConnected && !IsConnecting;
            TextBoxMotionBasePort.Enabled = !IsConnected && !IsConnecting;
        }


        private readonly Timer _timerConnectChecker = new(1000);

        private void TimerConnectChecker_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            //持续1秒钟没有收到有效信息了,判定连接已经断开
            IsConnected = false;
        }

        private UdpClient? udpClient;

        // ReSharper disable once InconsistentNaming
        private unsafe void StartUDP()
        {
            // 创建UDP客户端并绑定端口
            udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse(TextBoxLocalIP.Text), int.Parse(TextBoxLocalPort.Text)));
            udpClient.Connect(IPAddress.Parse(TextBoxMotionBaseIP.Text), int.Parse(TextBoxMotionBasePort.Text));


            //////发送连接请求,
            //////对于 TwinCat 版本的运动控制器是必须的;
            //////stm32 版本的运动控制器不会理会此请求.
            ////udpClient.Send(new HostToMotion()
            ////                   {
            ////                       MessageID = (uint)eMessageID.msgid_connect_request
            ////                   }
            ////                  .StructToBytes()
            ////              );

            try
            {
                IPEndPoint? remoteEP = null;
                while (!IsDisconnectRequested) // 持续接收数据
                {
                    // 接收数据
                    var receivedBytes = udpClient.Receive(ref remoteEP);

                    if (receivedBytes.Length == 128 && receivedBytes[0] == 128)
                    {
                        if (!IsConnected)
                        {
                            //由于UDP协议没有连接状态,所以我们以收到运动控制器的信号为依据判断是否连接成功了
                            IsConnected  = true;
                            IsConnecting = false;
                        }

                        //重新开始计时
                        _timerConnectChecker.Enabled = false;
                        _timerConnectChecker.Enabled = true;


                        //将字节数组解析为 MotionToHost 结构体,以便读取其中内容
                        var mth = receivedBytes.BytesToStruct<MotionToHost>();

                        var stroke1 = mth.Stroke[0].ToString("F2");
                        var stroke2 = mth.Stroke[1].ToString("F2");
                        var stroke3 = mth.Stroke[2].ToString("F2");
                        var stroke4 = mth.Stroke[3].ToString("F2");
                        var stroke5 = mth.Stroke[4].ToString("F2");
                        var stroke6 = mth.Stroke[5].ToString("F2");

                        var driverTorque1 = mth.DriverTorque[0].ToString("F2");
                        var driverTorque2 = mth.DriverTorque[1].ToString("F2");
                        var driverTorque3 = mth.DriverTorque[2].ToString("F2");
                        var driverTorque4 = mth.DriverTorque[3].ToString("F2");
                        var driverTorque5 = mth.DriverTorque[4].ToString("F2");
                        var driverTorque6 = mth.DriverTorque[5].ToString("F2");


                        var driverErrorCode1 = mth.DriverErrorCode[0].ToString();
                        var driverErrorCode2 = mth.DriverErrorCode[1].ToString();
                        var driverErrorCode3 = mth.DriverErrorCode[2].ToString();
                        var driverErrorCode4 = mth.DriverErrorCode[3].ToString();
                        var driverErrorCode5 = mth.DriverErrorCode[4].ToString();
                        var driverErrorCode6 = mth.DriverErrorCode[5].ToString();

                        var statusWord    = Enum.GetName(typeof(eState),       mth.StatusWord);
                        var errorCode     = Enum.GetName(typeof(ErrorCodes),   mth.ErrorCode);
                        var warningCode   = Enum.GetName(typeof(WarningCodes), mth.WarningCode);
                        var motionState   = mth.MotionState.ToString();
                        var sequenceCount = mth.SequenceCount.ToString();


                        var diWord = (byte)mth.IOword;
                        var di1    = ((diWord & 0x01) != 0).ToString();
                        var di2    = ((diWord & 0x02) != 0).ToString();
                        var di3    = ((diWord & 0x04) != 0).ToString();
                        var di4    = ((diWord & 0x08) != 0).ToString();
                        var di5    = ((diWord & 0x10) != 0).ToString();
                        var di6    = ((diWord & 0x20) != 0).ToString();
                        var di7    = ((diWord & 0x40) != 0).ToString();
                        var di8    = ((diWord & 0x80) != 0).ToString();

                        var dOWord = (byte)(mth.IOword >> 8);
                        var do1    = ((dOWord & 0x01) != 0).ToString();
                        var do2    = ((dOWord & 0x02) != 0).ToString();
                        var do3    = ((dOWord & 0x04) != 0).ToString();
                        var do4    = ((dOWord & 0x08) != 0).ToString();
                        var do5    = ((dOWord & 0x10) != 0).ToString();
                        var do6    = ((dOWord & 0x20) != 0).ToString();
                        var do7    = ((dOWord & 0x40) != 0).ToString();
                        var do8    = ((dOWord & 0x80) != 0).ToString();

                        Invoke((Delegate)(() =>
                                          {
                                              labelStroke1.Text = stroke1;
                                              labelStroke2.Text = stroke2;
                                              labelStroke3.Text = stroke3;
                                              labelStroke4.Text = stroke4;
                                              labelStroke5.Text = stroke5;
                                              labelStroke6.Text = stroke6;

                                              labelDriverTorque1.Text = driverTorque1;
                                              labelDriverTorque2.Text = driverTorque2;
                                              labelDriverTorque3.Text = driverTorque3;
                                              labelDriverTorque4.Text = driverTorque4;
                                              labelDriverTorque5.Text = driverTorque5;
                                              labelDriverTorque6.Text = driverTorque6;

                                              labelDriverErrorCode1.Text = driverErrorCode1;
                                              labelDriverErrorCode2.Text = driverErrorCode2;
                                              labelDriverErrorCode3.Text = driverErrorCode3;
                                              labelDriverErrorCode4.Text = driverErrorCode4;
                                              labelDriverErrorCode5.Text = driverErrorCode5;
                                              labelDriverErrorCode6.Text = driverErrorCode6;

                                              labelStatusWord.Text  = statusWord;
                                              labelErrorCode.Text   = errorCode;
                                              labelWarningCode.Text = warningCode;
                                              labelMotionState.Text = motionState;
                                              LabelPacketCount.Text = sequenceCount;

                                              labelDI1.Text = di1;
                                              labelDI2.Text = di2;
                                              labelDI3.Text = di3;
                                              labelDI4.Text = di4;
                                              labelDI5.Text = di5;
                                              labelDI6.Text = di6;
                                              labelDI7.Text = di7;
                                              labelDI8.Text = di8;

                                              labelDO1.Text = do1;
                                              labelDO2.Text = do2;
                                              labelDO3.Text = do3;
                                              labelDO4.Text = do4;
                                              labelDO5.Text = do5;
                                              labelDO6.Text = do6;
                                              labelDO7.Text = do7;
                                              labelDO8.Text = do8;
                                          }));
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Runtime error: {ex.Message}");
            }
            finally
            {
                udpClient?.Dispose();
                udpClient = null;
            }
        }

        private void ButtonCreateRunMessage_Click(object sender, EventArgs e)
        {
            HostToMotion = new HostToMotion()
                           {
                               CommandWord = eCommandWord.COMMAND_Run
                           };
        }

        private void ButtonCreateNeutralMessage_Click(object sender, EventArgs e)
        {
            HostToMotion = new HostToMotion()
                           {
                               CommandWord = eCommandWord.COMMAND_Neutral
                           };
        }

        private void ButtonCreateDescendMessage_Click(object sender, EventArgs e)
        {
            HostToMotion = new HostToMotion()
                           {
                               CommandWord = eCommandWord.COMMAND_Descend
                           };
        }

        private void ButtonCreateEmergMessage_Click(object sender, EventArgs e)
        {
            HostToMotion = new HostToMotion()
                           {
                               CommandWord = eCommandWord.COMMAND_Emergency
                           };
        }

        private void ButtonCreatePTPStartMessage_Click(object sender, EventArgs e)
        {
            HostToMotion = new HostToMotion
                           {
                               CommandWord = eCommandWord.COMMAND_Run,
                               MessageID   = (uint)eMessageID.PTP,
                               SubCmd      = 0x01,

                               Pitch = float.Parse(TextBoxPTPPitchPosition.Text),
                               Roll  = float.Parse(TextBoxPTPRollPosition.Text),
                               Yaw   = float.Parse(TextBoxPTPYawPosition.Text),
                               Sway  = float.Parse(TextBoxPTPSwayPosition.Text),
                               Surge = float.Parse(TextBoxPTPSurgePosition.Text),
                               Heave = float.Parse(TextBoxPTPHeavePosition.Text),

                               Var11 = float.Parse(TextBoxPTPPitchVelocity.Text),
                               Var12 = float.Parse(TextBoxPTPRollVelocity.Text),
                               Var13 = float.Parse(TextBoxPTPYawVelocity.Text),
                               Var14 = float.Parse(TextBoxPTPSwayVelocity.Text),
                               Var15 = float.Parse(TextBoxPTPSurgeVelocity.Text),
                               Var16 = float.Parse(TextBoxPTPHeaveVelocity.Text)
                           };
        }

        private void ButtonCreatePTPStopMessage_Click(object sender, EventArgs e)
        {
            HostToMotion = new HostToMotion()
                           {
                               CommandWord = eCommandWord.COMMAND_Run,
                               MessageID   = (uint)eMessageID.PTP,
                               SubCmd      = 0x80,
                           };
        }

        private void ButtonCreateSineStartMessage_Click(object sender, EventArgs e)
        {
            HostToMotion = new HostToMotion
                           {
                               CommandWord = eCommandWord.COMMAND_Run,
                               MessageID   = (uint)eMessageID.Sine,
                               SubCmd      = 0x01,

                               Pitch = float.Parse(TextBoxSinePitchPosition.Text),
                               Roll  = float.Parse(TextBoxSineRollPosition.Text),
                               Yaw   = float.Parse(TextBoxSineYawPosition.Text),
                               Sway  = float.Parse(TextBoxSineSwayPosition.Text),
                               Surge = float.Parse(TextBoxSineSurgePosition.Text),
                               Heave = float.Parse(TextBoxSineHeavePosition.Text),

                               Var11 = float.Parse(TextBoxSinePitchVelocity.Text),
                               Var12 = float.Parse(TextBoxSineRollVelocity.Text),
                               Var13 = float.Parse(TextBoxSineYawVelocity.Text),
                               Var14 = float.Parse(TextBoxSineSwayVelocity.Text),
                               Var15 = float.Parse(TextBoxSineSurgeVelocity.Text),
                               Var16 = float.Parse(TextBoxSineHeaveVelocity.Text),

                               Var21 = float.Parse(TextBoxSinePitchPhase.Text),
                               Var22 = float.Parse(TextBoxSineRollPhase.Text),
                               Var23 = float.Parse(TextBoxSineYawPhase.Text),
                               Var24 = float.Parse(TextBoxSineSwayPhase.Text),
                               Var25 = float.Parse(TextBoxSineSurgePhase.Text),
                               Var26 = float.Parse(TextBoxSineHeavePhase.Text)
                           };
        }

        private void ButtonCreateSineStopMessage_Click(object sender, EventArgs e)
        {
            HostToMotion = new HostToMotion()
                           {
                               CommandWord = eCommandWord.COMMAND_Run,
                               MessageID   = (uint)eMessageID.Sine,
                               SubCmd      = 0x00,
                           };
        }


        private HostToMotion _hostToMotion = new();

        private HostToMotion HostToMotion
        {
            get => _hostToMotion;
            set
            {
                _hostToMotion = value;

                ShowHostToMotion();
            }
        }

        private void ShowHostToMotion()
        {
            if (InvokeRequired)
            {
                Invoke(ShowHostToMotion);
                return;
            }

            TextBoxHTMLength.Text   = HostToMotion.PacketLength.ToString();
            TextBoxHTMSC.Text       = HostToMotion.SequenceCount.ToString();
            TextBoxHTMReversed.Text = HostToMotion.Reversed.ToString();
            TextBoxHTMMsgID.Text    = HostToMotion.MessageID.ToString();

            TextBoxHTMCW.Text        = ((ushort)HostToMotion.CommandWord).ToString();
            TextBoxHTMFileID.Text    = HostToMotion.PlaybackFileID.ToString();
            TextBoxHTMSubCmd.Text    = HostToMotion.SubCmd.ToString();
            TextBoxHTMReversed3.Text = HostToMotion.Reversed3.ToString();
            TextBoxHTMReversed4.Text = HostToMotion.Reversed4.ToString();
            TextBoxHTMReversed5.Text = HostToMotion.Reversed5.ToString();

            TextBoxHTMPitch.Text = HostToMotion.Pitch.ToString();
            TextBoxHTMRoll.Text  = HostToMotion.Roll.ToString();
            TextBoxHTMYaw.Text   = HostToMotion.Yaw.ToString();
            TextBoxHTMSway.Text  = HostToMotion.Sway.ToString();
            TextBoxHTMSurge.Text = HostToMotion.Surge.ToString();
            TextBoxHTMHeave.Text = HostToMotion.Heave.ToString();

            TextBoxHTM11.Text = HostToMotion.Var11.ToString();
            TextBoxHTM12.Text = HostToMotion.Var12.ToString();
            TextBoxHTM13.Text = HostToMotion.Var13.ToString();
            TextBoxHTM14.Text = HostToMotion.Var14.ToString();
            TextBoxHTM15.Text = HostToMotion.Var15.ToString();
            TextBoxHTM16.Text = HostToMotion.Var16.ToString();

            TextBoxHTM21.Text = HostToMotion.Var21.ToString();
            TextBoxHTM22.Text = HostToMotion.Var22.ToString();
            TextBoxHTM23.Text = HostToMotion.Var23.ToString();
            TextBoxHTM24.Text = HostToMotion.Var24.ToString();
            TextBoxHTM25.Text = HostToMotion.Var25.ToString();
            TextBoxHTM26.Text = HostToMotion.Var26.ToString();

            TextBoxHTM31.Text = HostToMotion.Var31.ToString();
            TextBoxHTM32.Text = HostToMotion.Var32.ToString();
            TextBoxHTM33.Text = HostToMotion.Var33.ToString();
            TextBoxHTM34.Text = HostToMotion.Var34.ToString();
            TextBoxHTM35.Text = HostToMotion.Var35.ToString();
            TextBoxHTM36.Text = HostToMotion.Var36.ToString();

            TextBoxMsgHex.Text = BitConverter.ToString(HostToMotion.StructToBytes());
        }

        private void ButtonSend_Click(object sender, EventArgs e)
        {
            var data = HostToMotion.StructToBytes();
            udpClient?.Send(data, data.Length);
        }
    }
}