using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using SH_OBD_DLL;

namespace SH_OBD_DLL {
    public class SH_OBD_Dll {
        private const int MAX_PID = 0x100;
        private readonly Logger _log;
        private readonly OBDInterface _OBDIf;
        public Dictionary<string, bool[]> Mode01Support { get; }
        public Dictionary<string, bool[]> Mode09Support { get; }

        public SH_OBD_Dll(string logPath) {
            _log = new Logger(logPath, EnumLogLevel.LogLevelAll, true, 100);
            _OBDIf = new OBDInterface(_log);
            Mode01Support = new Dictionary<string, bool[]>();
            Mode09Support = new Dictionary<string, bool[]>();
        }

        public Logger GetLogger() {
            return _log;
        }

        public OBDInterface GetOBDInterface() {
            return _OBDIf;
        }

        public void LogCommSettingInfo() {
            _log.TraceInfo(">>>>> Start to connect OBD. DllVersion: " + DllVersion<SH_OBD_Dll>.AssemblyVersion + " <<<<<");
            _log.TraceInfo("Connection Procedure Initiated");

            if (_OBDIf.DllSettings.AutoDetect) {
                _log.TraceInfo("   Automatic Hardware Detection: ON");
            } else {
                _log.TraceInfo("   Automatic Hardware Detection: OFF");
            }

            _log.TraceInfo(string.Format("   Baud Rate: {0}", _OBDIf.DllSettings.BaudRate));
            _log.TraceInfo(string.Format("   Default Port: {0}", _OBDIf.DllSettings.ComPortName));

            switch (_OBDIf.DllSettings.HardwareIndex) {
            case HardwareType.Automatic:
                _log.TraceInfo("   Interface: Auto-Detect");
                break;
            case HardwareType.ELM327:
                _log.TraceInfo("   Interface: ELM327/SH-VCI-302U");
                break;
            default:
                _log.TraceInfo("Bad hardware type.");
                throw new Exception("Bad hardware type.");
            }

            _log.TraceInfo(string.Format("   Protocol: {0}", _OBDIf.DllSettings.ProtocolName));
            _log.TraceInfo(string.Format("   Application Layer Protocol: {0}", _OBDIf.DllSettings.StandardName));

            if (_OBDIf.DllSettings.DoInitialization) {
                _log.TraceInfo("   Initialize: YES");
            } else {
                _log.TraceInfo("   Initialize: NO");
            }
        }

        public bool ConnectOBD() {
            _OBDIf.Disconnect();
            LogCommSettingInfo();
            if (_OBDIf.DllSettings.AutoDetect) {
                if (_OBDIf.InitDeviceAuto()) {
                    _log.TraceInfo("Connection Established!");
                } else {
                    _log.TraceWarning("Failed to find a compatible OBD-II interface.");
                    _OBDIf.Disconnect();
                    return false;
                }
            } else {
                if (_OBDIf.InitDevice()) {
                    _log.TraceInfo("Connection Established!");
                } else {
                    _log.TraceWarning("Failed to find a compatible OBD-II interface.");
                    _OBDIf.Disconnect();
                    return false;
                }
            }
            return true;
        }

        private bool GetSupportStatus(int mode, Dictionary<string, bool[]> supportStatus) {
            supportStatus.Clear();
            OBDParameter param = new OBDParameter();
            int HByte = 0;
            if (_OBDIf.STDType == StandardType.ISO_27145) {
                HByte = (mode << 8) & 0xFF00;
                param = new OBDParameter(0x22, HByte, "", 32);
            } else if (_OBDIf.STDType == StandardType.ISO_15031) {
                param = new OBDParameter(mode, 0, "", 32);
            } else if (_OBDIf.STDType == StandardType.SAE_J1939) {
                param = new OBDParameter(0, mode, "", 0);
                List<OBDParameterValue> valueList = _OBDIf.GetValueList(param);
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
                List<OBDParameterValue> valueList = _OBDIf.GetValueList(param);
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
                if (_OBDIf.STDType == StandardType.ISO_27145) {
                    log = "DID " + mode.ToString("X2") + " Support: [" + key + "], [";
                } else if (_OBDIf.STDType == StandardType.ISO_15031) {
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
                _log.TraceInfo(log);
            }
            return true;
        }

        public bool SetSupportStatus(out string errorMsg) {
            errorMsg = string.Empty;
            int mode01 = 1;
            int mode09 = 9;
            if (_OBDIf.STDType == StandardType.ISO_27145) {
                mode01 = 0xF4;
                mode09 = 0xF8;
            } else if (_OBDIf.STDType == StandardType.SAE_J1939) {
                // J1939只取一个支持状态就够了
                mode01 = 0xFECE;
            }

            if (!GetSupportStatus(mode01, Mode01Support)) {
                if (_OBDIf.STDType == StandardType.ISO_27145) {
                    errorMsg = "获取 DID F4 支持状态出错！";
                    _log.TraceError("Get DID F4 Support Status Error!");
                } else if (_OBDIf.STDType == StandardType.SAE_J1939) {
                    errorMsg = "获取 DM5 支持状态出错！";
                    _log.TraceError("Get DM5 Support Status Error!");
                } else {
                    errorMsg = "获取 Mode01 支持状态出错！";
                    _log.TraceError("Get Mode01 Support Status Error!");
                }
                return false;
            }

            if (_OBDIf.STDType != StandardType.SAE_J1939) {
                // J1939只取一个支持状态就够了
                if (!GetSupportStatus(mode09, Mode09Support)) {
                    if (_OBDIf.STDType == StandardType.ISO_27145) {
                        errorMsg = "获取 DID F8 支持状态出错！";
                        _log.TraceError("Get DID F8 Support Status Error!");
                    } else {
                        errorMsg = "获取 Mode09 支持状态出错！";
                        _log.TraceError("Get Mode09 Support Status Error!");
                    }
                    return false;
                }
            }

            return true;
        }

        public bool TestTCP() {
            bool bRet = Utility.TcpTest(_OBDIf.DllSettings.RemoteIP, _OBDIf.DllSettings.RemotePort);
            if (!bRet) {
                _log.TraceError("Can't connect to TCP server of OBD VCI device!");
            }
            return bRet;
        }

        public string GetDllVersion() {
            return DllVersion<SH_OBD_Dll>.AssemblyVersion.ToString();
        }
    }
}
