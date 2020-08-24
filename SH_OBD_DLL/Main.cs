using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using SH_OBD_DLL;

namespace SH_OBD_DLL {
    public class SH_OBD_Dll {
        private const int MAX_PID = 0x100;
        private readonly OBDInterface m_OBDIf;
        public Dictionary<string, bool[]> Mode01Support { get; }
        public Dictionary<string, bool[]> Mode09Support { get; }

        public SH_OBD_Dll(string logPath) {
            m_OBDIf = new OBDInterface(logPath);
            Mode01Support = new Dictionary<string, bool[]>();
            Mode09Support = new Dictionary<string, bool[]>();
        }

        public OBDInterface GetOBDInterface() {
            return m_OBDIf;
        }

        public void LogCommSettingInfo() {
            m_OBDIf.Log.TraceInfo(">>>>> Start to connect OBD. DllVersion: " + DllVersion<SH_OBD_Dll>.AssemblyVersion + " <<<<<");
            m_OBDIf.Log.TraceInfo("Connection Procedure Initiated");

            if (m_OBDIf.DllSettings.AutoDetect) {
                m_OBDIf.Log.TraceInfo("   Automatic Hardware Detection: ON");
            } else {
                m_OBDIf.Log.TraceInfo("   Automatic Hardware Detection: OFF");
            }

            m_OBDIf.Log.TraceInfo(string.Format("   Baud Rate: {0}", m_OBDIf.DllSettings.BaudRate));
            m_OBDIf.Log.TraceInfo(string.Format("   Default Port: {0}", m_OBDIf.DllSettings.ComPortName));

            switch (m_OBDIf.DllSettings.HardwareIndex) {
            case HardwareType.Automatic:
                m_OBDIf.Log.TraceInfo("   Interface: Auto-Detect");
                break;
            case HardwareType.ELM327:
                m_OBDIf.Log.TraceInfo("   Interface: ELM327/SH-VCI-302U");
                break;
            default:
                m_OBDIf.Log.TraceInfo("Bad hardware type.");
                throw new Exception("Bad hardware type.");
            }

            m_OBDIf.Log.TraceInfo(string.Format("   Protocol: {0}", m_OBDIf.DllSettings.ProtocolName));
            m_OBDIf.Log.TraceInfo(string.Format("   Application Layer Protocol: {0}", m_OBDIf.DllSettings.StandardName));

            if (m_OBDIf.DllSettings.DoInitialization) {
                m_OBDIf.Log.TraceInfo("   Initialize: YES");
            } else {
                m_OBDIf.Log.TraceInfo("   Initialize: NO");
            }
        }

        public bool ConnectOBD() {
            m_OBDIf.Disconnect();
            LogCommSettingInfo();
            if (m_OBDIf.DllSettings.AutoDetect) {
                if (m_OBDIf.InitDeviceAuto()) {
                    m_OBDIf.Log.TraceInfo("Connection Established!");
                } else {
                    m_OBDIf.Log.TraceWarning("Failed to find a compatible OBD-II interface.");
                    m_OBDIf.Disconnect();
                    return false;
                }
            } else {
                if (m_OBDIf.InitDevice()) {
                    m_OBDIf.Log.TraceInfo("Connection Established!");
                } else {
                    m_OBDIf.Log.TraceWarning("Failed to find a compatible OBD-II interface.");
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
            List<List<OBDParameterValue>> ECUSupportList = new List<List<OBDParameterValue>>();
            List<bool> ECUSupportNext = new List<bool>();
            OBDParameter param = new OBDParameter();
            int HByte = 0;
            if (m_OBDIf.STDType == StandardType.ISO_27145) {
                HByte = (mode << 8) & 0xFF00;
                param = new OBDParameter(0x22, HByte, 0) {
                    ValueTypes = 32
                };
            } else if (m_OBDIf.STDType == StandardType.ISO_15031) {
                param = new OBDParameter(mode, 0, 0) {
                    ValueTypes = 32
                };
            } else if (m_OBDIf.STDType == StandardType.SAE_J1939) {
                param = new OBDParameter(0, mode, 0);
            }
            List<OBDParameterValue> valueList = m_OBDIf.GetValueList(param);
            foreach (OBDParameterValue value in valueList) {
                List<OBDParameterValue> ECUValueList = new List<OBDParameterValue>();
                if (value.ErrorDetected) {
                    return false;
                }
                if (m_OBDIf.STDType == StandardType.SAE_J1939) {
                    supportStatus.Add(value.ECUResponseID, null);
                } else {
                    ECUValueList.Add(value);
                    ECUSupportList.Add(ECUValueList);
                    ECUSupportNext.Add(value.GetBitFlag(31));
                }
            }
            if (m_OBDIf.STDType == StandardType.SAE_J1939) {
                return true;
            }
            bool next = false;
            foreach (bool item in ECUSupportNext) {
                next = next || item;
            }
            if (next) {
                for (int i = 1; (i * 0x20) < MAX_PID; i++) {
                    param.Parameter = HByte + i * 0x20;
                    List<OBDParameterValue> valueList1 = m_OBDIf.GetValueList(param);
                    foreach (OBDParameterValue value in valueList1) {
                        if (value.ErrorDetected) {
                            return false;
                        }
                        for (int j = 0; j < ECUSupportNext.Count; j++) {
                            ECUSupportNext[j] = false;
                        }
                        for (int j = 0; j < ECUSupportList.Count; j++) {
                            if (ECUSupportList[j][0].ECUResponseID == value.ECUResponseID) {
                                ECUSupportList[j].Add(value);
                                ECUSupportNext[j] = value.GetBitFlag(31);
                            }
                        }
                    }
                    next = false;
                    foreach (bool item in ECUSupportNext) {
                        next = next || item;
                    }
                    if (!next) {
                        break;
                    }
                }
            }

            foreach (List<OBDParameterValue> ECUValueList in ECUSupportList) {
                List<bool> bitFlagList = new List<bool>();
                foreach (OBDParameterValue value in ECUValueList) {
                    for (int j = 0; j < 0x20; j++) {
                        bitFlagList.Add(value.GetBitFlag(j));
                    }
                }
                bool[] bitFlag = new bool[MAX_PID];
                for (int i = 0; i < bitFlagList.Count && i < bitFlag.Length; i++) {
                    bitFlag[i] = bitFlagList[i];
                }
                supportStatus.Add(ECUValueList[0].ECUResponseID, bitFlag);
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
                m_OBDIf.Log.TraceInfo(log);
            }
            return true;
        }

        public bool SetSupportStatus() {
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
                    m_OBDIf.Log.TraceError("Get DID F4 Support Status Error!");
                } else if (m_OBDIf.STDType == StandardType.SAE_J1939) {
                    m_OBDIf.Log.TraceError("Get DM5 Support Status Error!");
                } else {
                    m_OBDIf.Log.TraceError("Get Mode01 Support Status Error!");
                }
                return false;
            }

            if (m_OBDIf.STDType != StandardType.SAE_J1939) {
                // J1939只取一个支持状态就够了
                if (!GetSupportStatus(mode09, Mode09Support)) {
                    if (m_OBDIf.STDType == StandardType.ISO_27145) {
                        m_OBDIf.Log.TraceError("Get DID F8 Support Status Error!");
                    } else {
                        m_OBDIf.Log.TraceError("Get Mode09 Support Status Error!");
                    }
                    return false;
                }
            }

            return true;
        }

        public bool TestTCP() {
            bool bRet = Utility.TcpTest(m_OBDIf.DllSettings.RemoteIP, m_OBDIf.DllSettings.RemotePort);
            if (!bRet) {
                m_OBDIf.Log.TraceError("Can't connect to TCP server of OBD VCI device!");
            }
            return bRet;
        }
    }
}
