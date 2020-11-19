using System;
using System.IO.Ports;
using System.Threading;

namespace SH_OBD_DLL {
    public class OBDDeviceELM327 : OBDDevice {
        private ProtocolType _iProtocol;
        private StandardType _iStandard;
        private int _iBaudRateIndex;

        public OBDDeviceELM327(DllSettings settings, Logger log, int[] xattr) : base(settings, log, xattr) {
            _iProtocol = ProtocolType.Unknown;
            _iStandard = StandardType.Automatic;
        }

        private bool InternalInitialize() {
            if (!_CommELM.Open()) {
                return false;
            }
            if (!ConfirmAT("ATWS") || !ConfirmAT("ATE0") || !ConfirmAT("ATL0") || !ConfirmAT("ATH1") || !ConfirmAT("ATCAF1")) {
                _CommELM.Close();
                return false;
            }

            base._DeviceDes = GetDeviceDes().Trim();
            base._DeviceID = GetDeviceID().Trim().Replace("ELM327", "SH-VCI-302U");
            if (_iProtocol != ProtocolType.Unknown) {
                if (!ConfirmAT("ATSP" + ((int)_iProtocol).ToString("X1"))) {
                    _CommELM.Close();
                    return false;
                }

                int originalTimeout = _CommELM.Timeout;
                _CommELM.Timeout = 5000;
                _iStandard = SetStandardType(_iProtocol);
                if (_iStandard != StandardType.Automatic) {
                    if (_Parser == null) {
                        string strDPN = GetOBDResponse("ATDPN").Replace("A", "");
                        if (strDPN.Length == 0) {
                            strDPN = "A";
                        }
                        SetProtocol((ProtocolType)Convert.ToInt32(strDPN, 16));
                    }
                }

                _CommELM.Timeout = originalTimeout;
                return _iStandard != StandardType.Automatic;
            } else {
                if (!ConfirmAT("ATM0")) {
                    _CommELM.Close();
                    return false;
                }

                int originalTimeout = _CommELM.Timeout;
                _CommELM.Timeout = 5000;
                for (int idx = 0; idx < _xattr.Length; idx++) {
                    if (!ConfirmAT("ATTP" + _xattr[idx].ToString("X1"))) {
                        _CommELM.Close();
                        return false;
                    }
                    _iStandard = SetStandardType((ProtocolType)_xattr[idx]);
                    if (_iStandard != StandardType.Automatic) {
                        if (_Parser == null) {
                            SetProtocol((ProtocolType)_xattr[idx]);
                        }
                        _CommELM.Timeout = originalTimeout;
                        ConfirmAT("ATM1");
                        return true;
                    }
                }
                // 每个协议都无法连接的话就关闭端口准备退出
                if (_CommELM.Online) {
                    _CommELM.Close();
                }
                return false;
            }
        }

        public override bool Initialize(int iPort, int iBaud) {
            try {
                if (_CommELM.Online) {
                    return true;
                }
                _CommELM.Port = iPort;
                _CommELM.BaudRate = iBaud;
                if (InternalInitialize()) {
                    SetBaudRateIndex(iBaud);
                    return true;
                }
            } catch (Exception ex) {
                if (_CommELM.Online) {
                    _CommELM.Close();
                }
                _log.TraceError("Initialize occur error: " + ex.Message);
            }
            return false;
        }

        public override bool Initialize(string strRemoteIP, int iRemotePort) {
            try {
                if (_CommELM.Online) {
                    return true;
                }
                _CommELM.Port = 0;
                _CommELM.RemoteIP = strRemoteIP;
                _CommELM.RemotePort = iRemotePort;
                if (InternalInitialize()) {
                    return true;
                }
            } catch (Exception ex) {
                if (_CommELM.Online) {
                    _CommELM.Close();
                }
                _log.TraceError("Initialize occur error: " + ex.Message);
            }
            return false;
        }

        public override bool Initialize(DllSettings settings) {
            SetProtocol(settings.ProtocolIndex);
            _iStandard = settings.StandardIndex;
            if (settings.ComPort > 0) {
                // 使用串口连接ELM327
                _log.TraceInfo(string.Format("Attempting initialization on port {0}", settings.HardwareIndex.ToString()));
                return Initialize(settings.ComPort, settings.BaudRate);
            } else {
                // 使用TCP连接ELM327
                _log.TraceInfo(string.Format("Attempting initialization on remote server {0}:{1}", settings.RemoteIP, settings.RemotePort.ToString()));
                return Initialize(settings.RemoteIP, settings.RemotePort);
            }
        }

        public override bool InitializeAuto(DllSettings settings) {
            try {
                if (_CommELM.Online) {
                    return true;
                }
                _iStandard = settings.StandardIndex;
                if (settings.ComPort > 0) {
                    // 使用串口连接ELM327
                    if (CommBase.GetPortAvailable(settings.ComPort) == CommBase.PortStatus.Available && Initialize(settings.ComPort, settings.BaudRate)) {
                        settings.FirstRun = false;
                        return true;
                    }
                    if (settings.FirstRun) {
                        string[] serials = SerialPort.GetPortNames();
                        for (int i = 0; i < serials.Length; i++) {
                            if (int.TryParse(serials[i].Substring(3), out int iPort)) {
                                if (iPort != settings.ComPort) {
                                    if (CommBase.GetPortAvailable(iPort) == CommBase.PortStatus.Available && Initialize(iPort, 38400)) {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                } else {
                    // 使用TCP连接ELM327
                    if (Initialize(settings.RemoteIP, settings.RemotePort)) {
                        return true;
                    }
                }
            } catch (Exception ex) {
                if (_CommELM.Online) {
                    _CommELM.Close();
                }
                _log.TraceError("Initialize occur error: " + ex.Message);
            }
            return false;
        }

        private StandardType SetStandardType(ProtocolType protocol) {
            if (_iStandard != StandardType.Automatic) {
                return _iStandard;
            }
            bool bflag = false;
            StandardType standard = StandardType.Automatic;
            switch (protocol) {
            case ProtocolType.J1850_PWM:
            case ProtocolType.J1850_VPW:
            case ProtocolType.ISO9141_2:
            case ProtocolType.ISO_14230_4_KWP_5BAUDINIT:
            case ProtocolType.ISO_14230_4_KWP_FASTINIT:
                for (int i = 3; i > 0 && !bflag; i--) {
                    if (GetOBDResponse("0100").Replace(" ", "").Contains("4100")) {
                        bflag = true;
                        standard = StandardType.ISO_15031;
                    }
                }
                break;
            case ProtocolType.ISO_15765_4_CAN_11BIT_500KBAUD:
            case ProtocolType.ISO_15765_4_CAN_11BIT_250KBAUD:
                ConfirmAT("ATCF700");
                ConfirmAT("ATCMF00");
                for (int i = 3; i > 0 && !bflag; i--) {
                    if (GetOBDResponse("22F810").Replace(" ", "").Contains("62F810")) {
                        bflag = bflag || true;
                        standard = StandardType.ISO_27145;
                    }
                }
                for (int i = 3; i > 0 && !bflag; i--) {
                    if (GetOBDResponse("0100").Replace(" ", "").Contains("4100")) {
                        bflag = true;
                        standard = StandardType.ISO_15031;
                    }
                }
                if (standard == StandardType.Automatic) {
                    ConfirmAT("ATAR");
                }
                break;
            case ProtocolType.ISO_15765_4_CAN_29BIT_500KBAUD:
            case ProtocolType.ISO_15765_4_CAN_29BIT_250KBAUD:
                ConfirmAT("ATCF18DAF100");
                ConfirmAT("ATCM1FFFFF00");
                for (int i = 3; i > 0 && !bflag; i--) {
                    if (GetOBDResponse("22F810").Replace(" ", "").Contains("62F810")) {
                        bflag = bflag || true;
                        standard = StandardType.ISO_27145;
                    }
                }
                for (int i = 3; i > 0 && !bflag; i--) {
                    if (GetOBDResponse("0100").Replace(" ", "").Contains("4100")) {
                        bflag = true;
                        standard = StandardType.ISO_15031;
                    }
                }
                if (standard == StandardType.Automatic) {
                    ConfirmAT("ATAR");
                }
                break;
            case ProtocolType.SAE_J1939_CAN_29BIT_250KBAUD:
                for (int i = 3; i > 0 && !bflag; i--) {
                    if (GetOBDResponse("00FECE").Replace(" ", "").Contains("60FECE")) {
                        bflag = bflag || true;
                        standard = StandardType.SAE_J1939;
                    }
                }
                break;
            default:
                for (int i = 3; i > 0 && !bflag; i--) {
                    if (GetOBDResponse("22F810").Replace(" ", "").Contains("62F810")) {
                        bflag = bflag || true;
                        standard = StandardType.ISO_27145;
                    }
                }
                for (int i = 3; i > 0 && !bflag; i--) {
                    if (GetOBDResponse("0100").Replace(" ", "").Contains("4100")) {
                        bflag = true;
                        standard = StandardType.ISO_15031;
                    }
                }
                for (int i = 3; i > 0 && !bflag; i--) {
                    if (GetOBDResponse("00FECE").Replace(" ", "").Contains("60FECE")) {
                        bflag = bflag || true;
                        standard = StandardType.SAE_J1939;
                    }
                }
                break;
            }
            _log.TraceInfo("SetStandardType: " + standard.ToString());
            return standard;
        }

        public override OBDResponseList Query(OBDParameter param) {
            OBDResponseList orl = _Parser.Parse(param, GetOBDResponse(param.OBDRequest));
            int errorQty;
            for (errorQty = 0; errorQty < 2; errorQty++) {
                if (!orl.ErrorDetected) {
                    break;
                }
                if (Online) {
                    Thread.Sleep(500);
                }
                orl = _Parser.Parse(param, GetOBDResponse(param.OBDRequest));
            }
            // 重试3次后还是出错的话，软重启ELM327后再重试3次
            if (errorQty >= 2) {
                ConfirmAT("ATWS");
                InitELM327Format();
                ConfirmAT("ATSP" + ((int)_iProtocol).ToString("X1"));
                for (errorQty = 0; errorQty < 3; errorQty++) {
                    if (!orl.ErrorDetected) {
                        break;
                    }
                    if (Online && errorQty != 0) {
                        Thread.Sleep(500);
                    }
                    orl = _Parser.Parse(param, GetOBDResponse(param.OBDRequest));
                }
            }
            if (orl.RawResponse == "PENDING") {
                int waittingTime = 60; // 重试总时间，单位秒
                int interval = 6; // 重试间隔时间，单位秒
                // NRC=78 处理
                _log.TraceWarning("Receive only NRC78, handle pending message");
                switch (_iProtocol) {
                case ProtocolType.J1850_PWM:
                case ProtocolType.J1850_VPW:
                    // 间隔30秒重复发送一次请求，直到有正响应消息返回
                    interval = 30;
                    for (int i = waittingTime / interval; i > 0; i--) {
                        Thread.Sleep(interval * 1000);
                        orl = _Parser.Parse(param, GetOBDResponse(param.OBDRequest));
                        if (!(orl.RawResponse == "PENDING" || orl.ErrorDetected)) {
                            break;
                        }
                    }
                    break;
                case ProtocolType.ISO9141_2:
                    // 间隔4秒重复发送一次请求，直到有正响应消息返回
                    interval = 4;
                    for (int i = waittingTime / interval; i > 0; i--) {
                        Thread.Sleep(interval * 1000);
                        orl = _Parser.Parse(param, GetOBDResponse(param.OBDRequest));
                        if (!(orl.RawResponse == "PENDING" || orl.ErrorDetected)) {
                            break;
                        }
                    }
                    break;
                default:
                    // ISO14230-4 会在50ms内发送NRC78负响应直到发送正响应，v2.1以下的ELM327一般可收到所需要的消息，只需要过滤NRC78的负响应即可
                    // ISO15765-4 会在5s内发送NRC78负响应直到发送正响应，v2.1以下的ELM327无法收到所需要的消息，需要上层应用自己处理
                    // 间隔6s(5s加上传输延时)检测一次是否有正响应消息返回，如果没有则继续
                    for (int i = waittingTime / interval; i > 0; i--) {
                        Thread.Sleep(interval * 1000);
                        string RxLine = _CommELM.GetRxLine();
                        if (RxLine != null && RxLine.Length > 0) {
                            _log.TraceInfo("RX: " + RxLine.Replace("\r", "\\r"));
                            orl = _Parser.Parse(param, RxLine);
                            if (!(orl.RawResponse == "PENDING" || orl.ErrorDetected)) {
                                break;
                            }
                        }
                    }
                    break;
                }
            }
            return orl;
        }

        public override string Query(string command) {
            if (_CommELM.Online) {
                return _CommELM.GetResponse(command);
            }
            return "";
        }

        override public void SetTimeout(int iTimeout) {
            _CommELM.Timeout = iTimeout;
        }

        public override void Disconnect() {
            if (_CommELM.Online) {
                _CommELM.Close();
            }
        }

        public void SetProtocol(ProtocolType iProtocol) {
            _iProtocol = iProtocol;
            _log.TraceInfo(string.Format("Protocol switched to: {0}", DllSettings.ProtocolNames[(int)iProtocol]));
            switch (iProtocol) {
            case ProtocolType.J1850_PWM:
                _Parser = new OBDParser_J1850_PWM();
                break;
            case ProtocolType.J1850_VPW:
                _Parser = new OBDParser_J1850_VPW();
                break;
            case ProtocolType.ISO9141_2:
                _Parser = new OBDParser_ISO9141_2();
                break;
            case ProtocolType.ISO_14230_4_KWP_5BAUDINIT:
                _Parser = new OBDParser_ISO14230_4_KWP();
                break;
            case ProtocolType.ISO_14230_4_KWP_FASTINIT:
                _Parser = new OBDParser_ISO14230_4_KWP();
                break;
            case ProtocolType.ISO_15765_4_CAN_11BIT_500KBAUD:
                _Parser = new OBDParser_ISO15765_4_CAN11();
                break;
            case ProtocolType.ISO_15765_4_CAN_29BIT_500KBAUD:
                _Parser = new OBDParser_ISO15765_4_CAN29();
                break;
            case ProtocolType.ISO_15765_4_CAN_11BIT_250KBAUD:
                _Parser = new OBDParser_ISO15765_4_CAN11();
                break;
            case ProtocolType.ISO_15765_4_CAN_29BIT_250KBAUD:
                _Parser = new OBDParser_ISO15765_4_CAN29();
                break;
            case ProtocolType.SAE_J1939_CAN_29BIT_250KBAUD:
                _Parser = new OBDParser_SAE_J1939_CAN29();
                break;
            }
        }

        public bool ConfirmAT(string command, int attempts = 3) {
            if (!_CommELM.Online) {
                return false;
            }
            for (int i = attempts; i > 0; i--) {
                string response = _CommELM.GetResponse(command);
                if (response.IndexOf("OK") >= 0 || response.IndexOf("ELM") >= 0) {
                    return true;
                } else if (response.Contains("STOPPED")) {
                    Thread.Sleep(500);
                }
            }
            _log.TraceWarning("Current device can't support command \"" + command + "\"!");
            return false;
        }

        public string GetOBDResponse(string command) {
            string strRet = "";
            if (_CommELM.Online) {
                strRet = _CommELM.GetResponse(command);
            }
            // 返回"ERR94"说明发生CAN网络错误，ELM327会返回出厂设置
            // 如需继续的话，需要重新初始化ELM327
            if (strRet.Contains("ERR94")) {
                InitELM327Format();
            }
            return strRet;
        }

        public string GetDeviceDes() {
            if (_CommELM.Online) {
                return _CommELM.GetResponse("AT@1");
            }
            return "";
        }

        public string GetDeviceID() {
            if (_CommELM.Online) {
                return _CommELM.GetResponse("ATI");
            }
            return "";
        }

        private void InitELM327Format() {
            ConfirmAT("ATE0");
            ConfirmAT("ATL0");
            ConfirmAT("ATH1");
            ConfirmAT("ATCAF1");
        }

        public override ProtocolType GetProtocolType() { return _iProtocol; }
        public override int GetBaudRateIndex() { return _iBaudRateIndex; }
        public void SetBaudRateIndex(int iBaud) {
            switch (iBaud) {
            case 9600:
                _iBaudRateIndex = 0;
                break;
            case 38400:
                _iBaudRateIndex = 1;
                break;
            case 115200:
                _iBaudRateIndex = 2;
                break;
            default:
                _iBaudRateIndex = -1;
                break;
            }
        }

        public override int GetComPortIndex() { return _CommELM.Port; }

        public override StandardType GetStandardType() { return _iStandard; }

    }
}
