using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using SH_OBD_DLL;

namespace SH_OBD_DLL {
    public class SH_OBD_Dll {
        private const int MAX_PID = 0x100;
        private readonly Logger m_log;
        private readonly OBDInterface m_OBDIf;
        public Dictionary<string, bool[]> Mode01Support { get; }
        public Dictionary<string, bool[]> Mode09Support { get; }

        public SH_OBD_Dll(string logPath) {
            m_log = new Logger(logPath, EnumLogLevel.LogLevelAll, true, 100);
            m_OBDIf = new OBDInterface(m_log);
            Mode01Support = new Dictionary<string, bool[]>();
            Mode09Support = new Dictionary<string, bool[]>();
        }

        public Logger GetLogger() {
            return m_log;
        }

        public OBDInterface GetOBDInterface() {
            return m_OBDIf;
        }

        public void LogCommSettingInfo() {
            m_log.TraceInfo(">>>>> Start to connect OBD. DllVersion: " + DllVersion<SH_OBD_Dll>.AssemblyVersion + " <<<<<");
            m_log.TraceInfo("Connection Procedure Initiated");

            if (m_OBDIf.DllSettings.AutoDetect) {
                m_log.TraceInfo("   Automatic Hardware Detection: ON");
            } else {
                m_log.TraceInfo("   Automatic Hardware Detection: OFF");
            }

            m_log.TraceInfo(string.Format("   Baud Rate: {0}", m_OBDIf.DllSettings.BaudRate));
            m_log.TraceInfo(string.Format("   Default Port: {0}", m_OBDIf.DllSettings.ComPortName));

            switch (m_OBDIf.DllSettings.HardwareIndex) {
            case HardwareType.Automatic:
                m_log.TraceInfo("   Interface: Auto-Detect");
                break;
            case HardwareType.ELM327:
                m_log.TraceInfo("   Interface: ELM327/SH-VCI-302U");
                break;
            default:
                m_log.TraceInfo("Bad hardware type.");
                throw new Exception("Bad hardware type.");
            }

            m_log.TraceInfo(string.Format("   Protocol: {0}", m_OBDIf.DllSettings.ProtocolName));
            m_log.TraceInfo(string.Format("   Application Layer Protocol: {0}", m_OBDIf.DllSettings.StandardName));

            if (m_OBDIf.DllSettings.DoInitialization) {
                m_log.TraceInfo("   Initialize: YES");
            } else {
                m_log.TraceInfo("   Initialize: NO");
            }
        }

        public bool ConnectOBD() {
            m_OBDIf.Disconnect();
            LogCommSettingInfo();
            if (m_OBDIf.DllSettings.AutoDetect) {
                if (m_OBDIf.InitDeviceAuto()) {
                    m_log.TraceInfo("Connection Established!");
                } else {
                    m_log.TraceWarning("Failed to find a compatible OBD-II interface.");
                    m_OBDIf.Disconnect();
                    return false;
                }
            } else {
                if (m_OBDIf.InitDevice()) {
                    m_log.TraceInfo("Connection Established!");
                } else {
                    m_log.TraceWarning("Failed to find a compatible OBD-II interface.");
                    m_OBDIf.Disconnect();
                    return false;
                }
            }

            // 初始化用到的变量
            Mode01Support.Clear();
            Mode09Support.Clear();

            return true;
        }

        private bool GetSupportStatus(int mode, Dictionary<string, bool[]> supportStatus) {
            OBDParameter param = new OBDParameter();
            int HByte = 0;
            if (m_OBDIf.STDType == StandardType.ISO_27145) {
                HByte = (mode << 8) & 0xFF00;
                param = new OBDParameter(0x22, HByte, "", 32);
            } else if (m_OBDIf.STDType == StandardType.ISO_15031) {
                param = new OBDParameter(mode, 0, "", 32);
            } else if (m_OBDIf.STDType == StandardType.SAE_J1939) {
                param = new OBDParameter(0, mode, "", 0);
                List<OBDParameterValue> valueList = m_OBDIf.GetValueList(param);
                foreach (OBDParameterValue value in valueList) {
                    if (value.ErrorDetected) {
                        return false;
                    }
                    supportStatus.Add(value.ECUResponseID, null);
                }
                return true;
            }

            for (int i = 0; (i * 0x20) < MAX_PID; i++) {
                param.Parameter = HByte + i * 0x20;
                List<OBDParameterValue> valueList = m_OBDIf.GetValueList(param);
                bool next = false;
                foreach (OBDParameterValue value in valueList) {
                    if (value.ErrorDetected) {
                        return false;
                    }
                    if (!supportStatus.ContainsKey(value.ECUResponseID)) {
                        bool[] bitFlag = new bool[MAX_PID];
                        supportStatus.Add(value.ECUResponseID, bitFlag);
                    }

                    foreach (string key in supportStatus.Keys) {
                        if (value.ECUResponseID == key) {
                            for (int j = 0; j < 0x20; j++) {
                                supportStatus[key][j + i * 0x20] = value.GetBitFlag(j);
                            }
                        }
                    }
                    next = next || value.GetBitFlag(31);
                }
                if (!next) {
                    break;
                }
            }

            foreach (string key in supportStatus.Keys) {
                string log = "";
                if (m_OBDIf.STDType == StandardType.ISO_27145) {
                    log = "DID " + mode.ToString("X2") + " Support: [" + key + "], [";
                } else if (m_OBDIf.STDType == StandardType.ISO_15031) {
                    log = "Mode" + mode.ToString("X2") + " Support: [" + key + "], [";
                }
                for (int i = 0; i * 8 < MAX_PID; i++) {
                    for (int j = 0; j < 8; j++) {
                        log += supportStatus[key][i * 8 + j] ? "1" : "0";
                    }
                    log += " ";
                }
                log = log.TrimEnd();
                log += "]";
                m_log.TraceInfo(log);
            }
            return true;
        }

        public bool SetSupportStatus(out string errorMsg) {
            errorMsg = string.Empty;
            int mode01 = 1;
            int mode09 = 9;
            if (m_OBDIf.STDType == StandardType.ISO_27145) {
                mode01 = 0xF4;
                mode09 = 0xF8;
            } else if (m_OBDIf.STDType == StandardType.SAE_J1939) {
                // J1939只取一个支持状态就够了
                mode01 = 0xFECE;
            }

            if (!GetSupportStatus(mode01, Mode01Support)) {
                if (m_OBDIf.STDType == StandardType.ISO_27145) {
                    errorMsg = "获取 DID F4 支持状态出错！";
                    m_log.TraceError("Get DID F4 Support Status Error!");
                } else if (m_OBDIf.STDType == StandardType.SAE_J1939) {
                    errorMsg = "获取 DM5 支持状态出错！";
                    m_log.TraceError("Get DM5 Support Status Error!");
                } else {
                    errorMsg = "获取 Mode01 支持状态出错！";
                    m_log.TraceError("Get Mode01 Support Status Error!");
                }
                return false;
            }

            if (m_OBDIf.STDType != StandardType.SAE_J1939) {
                // J1939只取一个支持状态就够了
                if (!GetSupportStatus(mode09, Mode09Support)) {
                    if (m_OBDIf.STDType == StandardType.ISO_27145) {
                        errorMsg = "获取 DID F8 支持状态出错！";
                        m_log.TraceError("Get DID F8 Support Status Error!");
                    } else {
                        errorMsg = "获取 Mode09 支持状态出错！";
                        m_log.TraceError("Get Mode09 Support Status Error!");
                    }
                    return false;
                }
            }

            return true;
        }

        public bool TestTCP() {
            bool bRet = Utility.TcpTest(m_OBDIf.DllSettings.RemoteIP, m_OBDIf.DllSettings.RemotePort);
            if (!bRet) {
                m_log.TraceError("Can't connect to TCP server of OBD VCI device!");
            }
            return bRet;
        }
    }
}
