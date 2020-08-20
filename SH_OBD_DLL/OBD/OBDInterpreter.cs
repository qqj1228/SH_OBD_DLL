using DBCParser;
using DBCParser.DBCObj;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SH_OBD {
    public partial class OBDInterpreter {
        private readonly NetWork m_netWork;
        private readonly List<SignalDisplay> m_sigDisplays;
        private readonly List<ValueDisplay> m_valDispalys;

        public OBDInterpreter(NetWork netWork, List<SignalDisplay> sigDisplays, List<ValueDisplay> valDispalys) {
            m_netWork = netWork;
            m_sigDisplays = sigDisplays;
            m_valDispalys = valDispalys;
        }

        public OBDParameterValue GetPIDSupport(OBDResponse response) {
            OBDParameterValue value2 = new OBDParameterValue();
            if (response.GetDataByteCount() < 4) {
                value2.ErrorDetected = true;
                return value2;
            }
            int dataA = Utility.Hex2Int(response.GetDataByte(0));
            int dataB = Utility.Hex2Int(response.GetDataByte(1));
            int dataC = Utility.Hex2Int(response.GetDataByte(2));
            int dataD = Utility.Hex2Int(response.GetDataByte(3));
            value2.SetBitFlagBAT(dataA, dataB, dataC, dataD);
            return value2;
        }

        public OBDParameterValue GetPIDValue(uint ID, string strData) {
            OBDParameterValue value2 = new OBDParameterValue();
            Message msg = m_netWork.GetMessage(ID);
            if (msg == null && ID < 0x80000000) {
                msg = m_netWork.GetMessage(ID + 0x80000000);
            }
            if (msg == null) {
                value2.ErrorDetected = true;
            } else {
                if (msg.SetSignalRawValue(strData)) {
                    foreach (Signal signal in msg.Signals.Values) {
                        int iUsed = signal.TestSignalUesed();
                        if (iUsed > 0) {
                            value2.DoubleValue = Math.Round(signal.Value, 2);
                            value2.BoolValue = signal.RawValue > 0;
                            foreach (string item in signal.ListString) {
                                value2.ListStringValue.Add(item);
                            }
                            value2.ShortStringValue = GetDisplayString(signal);
                            value2.StringValue = value2.ShortStringValue;
                        }
                    }
                } else {
                    value2.ErrorDetected = true;
                }
            }
            return value2;
        }

        public OBDParameterValue GetMode010209Value(OBDParameter param, OBDResponse response) {
            OBDParameterValue value2;
            if (param.Parameter % 0x20 == 0) {
                value2 = GetPIDSupport(response);
            } else {
                uint ID = (uint)((param.Service == 2 ? param.Service - 1 : param.Service << 8) + param.Parameter);
                value2 = GetPIDValue(ID, response.Data);
            }
            return value2;
        }

        public OBDParameterValue GetMode03070AValue(OBDResponse response) {
            OBDParameterValue value2 = new OBDParameterValue();
            List<string> strings = new List<string>();
            for (int i = 0; i <= response.Data.Length - 4; i += 4) {
                string str = GetDTCName(response.Data.Substring(i, 4));
                if (str.CompareTo("P0000") != 0) {
                    strings.Add(str);
                }
            }
            value2.ListStringValue = strings;
            return value2;
        }

        public OBDParameterValue GetMode05Value(OBDParameter param, OBDResponse response) {
            OBDParameterValue value2 = new OBDParameterValue();
            switch (param.Parameter) {
            case 0x00:
                value2 = GetPIDSupport(response);
                break;
            case 0x01:
            case 0x02:
            case 0x03:
            case 0x04:
                if (response.GetDataByteCount() < 1) {
                    value2.ErrorDetected = true;
                    break;
                }
                value2.DoubleValue = Utility.Hex2Int(response.GetDataByte(0)) * 0.005;
                value2.DoubleValue = Math.Round(value2.DoubleValue, 2);
                break;
            case 0x05:
            case 0x06:
                if (response.GetDataByteCount() < 3) {
                    value2.ErrorDetected = true;
                    break;
                }
                if (param.SubParameter == 0) {
                    // 取计算值
                    value2.DoubleValue = Utility.Hex2Int(response.GetDataByte(0)) * 0.004;
                } else if (param.SubParameter == 1) {
                    // 取最小值
                    value2.DoubleValue = Utility.Hex2Int(response.GetDataByte(1)) * 0.004;
                } else if (param.SubParameter == 2) {
                    // 取最大值
                    value2.DoubleValue = Utility.Hex2Int(response.GetDataByte(2)) * 0.004;
                }
                value2.DoubleValue = Math.Round(value2.DoubleValue, 2);
                break;
            case 0x07:
            case 0x08:
                if (response.GetDataByteCount() < 3) {
                    value2.ErrorDetected = true;
                    break;
                }
                if (param.SubParameter == 0) {
                    // 取计算值
                    value2.DoubleValue = Utility.Hex2Int(response.GetDataByte(0)) * 0.005;
                } else if (param.SubParameter == 1) {
                    // 取最小值
                    value2.DoubleValue = Utility.Hex2Int(response.GetDataByte(1)) * 0.005;
                } else if (param.SubParameter == 2) {
                    // 取最大值
                    value2.DoubleValue = Utility.Hex2Int(response.GetDataByte(2)) * 0.005;
                }
                value2.DoubleValue = Math.Round(value2.DoubleValue, 2);
                break;
            case 0x09:
            case 0x0A:
                if (response.GetDataByteCount() < 3) {
                    value2.ErrorDetected = true;
                    break;
                }
                if (param.SubParameter == 0) {
                    // 取计算值
                    value2.DoubleValue = Utility.Hex2Int(response.GetDataByte(0)) * 0.04;
                } else if (param.SubParameter == 1) {
                    // 取最小值
                    value2.DoubleValue = Utility.Hex2Int(response.GetDataByte(1)) * 0.04;
                } else if (param.SubParameter == 2) {
                    // 取最大值
                    value2.DoubleValue = Utility.Hex2Int(response.GetDataByte(2)) * 0.04;
                }
                value2.DoubleValue = Math.Round(value2.DoubleValue, 2);
                break;
            default:
                value2.ErrorDetected = true;
                break;
            }
            return value2;
        }

        private List<string> SetMode09ASCII(int DataOffset, OBDResponse response) {
            List<string> strings = new List<string>();
            int num = response.Data.Length / DataOffset;
            for (int i = 0; i < num; i++) {
                strings.Add(Utility.HexStrToASCIIStr(response.Data.Substring(i * DataOffset, DataOffset)));
            }
            return strings;
        }

        private OBDParameterValue Get42DTCValue(OBDResponse response) {
            return Get19DTCValue(response, 10);
        }

        private OBDParameterValue Get55DTCValue(OBDResponse response) {
            return Get19DTCValue(response, 8);
        }

        private OBDParameterValue Get19DTCValue(OBDResponse response, int offset, int WholeDTCLenInByte = 4) {
            if (offset - WholeDTCLenInByte * 2 < 0) {
                return null;
            }
            OBDParameterValue value2 = new OBDParameterValue();
            List<string> strings = new List<string>();
            for (int i = 0; i <= response.Data.Length - offset; i += offset) {
                string str = GetDTCName(response.Data.Substring(i + offset - WholeDTCLenInByte * 2, 6));
                if (!str.StartsWith("P0000")) {
                    strings.Add(str);
                }
            }
            value2.ListStringValue = strings;
            return value2;
        }

        private OBDParameterValue GetDM5Value(OBDParameter param, OBDResponse response) {
            OBDParameterValue value2 = new OBDParameterValue();
            if (response.GetDataByteCount() < 8) {
                value2.ErrorDetected = true;
                return value2;
            }

            switch (param.SubParameter) {
            case 0:
                // OBD型式
                response.Data = response.GetDataByte(2);
                value2 = GetPIDValue(0x11C, response.Data);
                break;
            case 1:
                // 激活的故障代码
                response.Data = response.GetDataByte(0);
                break;
            case 2:
                // 先前激活的诊断故障代码
                response.Data = response.GetDataByte(1);
                break;
            case 3:
                // 持续监视系统支持／状态
                response.Data = response.GetDataByte(3);
                break;
            case 4:
                // 非持续监视系统支持
                response.Data = response.GetDataByte(4) + response.GetDataByte(5);
                break;
            case 5:
                // 非持续监视系统状态
                response.Data = response.GetDataByte(6) + response.GetDataByte(7);
                break;
            default:
                value2.ErrorDetected = true;
                break;
            }
            return value2;
        }

        private OBDParameterValue GetDM19Value(OBDParameter param, OBDResponse response) {
            OBDParameterValue value2 = new OBDParameterValue();
            if (response.GetDataByteCount() < 20) {
                value2.ErrorDetected = true;
                return value2;
            }

            int qty = response.Data.Length / (20 * 2);
            string strData = "";
            int OriginalParam = param.Parameter;
            int OriginalService = param.Service;
            param.Service = 9;
            switch (param.SubParameter) {
            case 0:
                // CVN
                param.Parameter = 0x06;
                for (int i = 0; i < qty; i++) {
                    strData += response.Data.Substring(i * 20 * 2, 4 * 2);
                }
                response.Data = strData;
                value2 = GetMode010209Value(param, response);
                for (int i = 0; i < value2.ListStringValue.Count; i++) {
                    string strVal = value2.ListStringValue[i];
                    value2.ListStringValue[i] = strVal.Substring(6, 2) + strVal.Substring(4, 2) + strVal.Substring(2, 2) + strVal.Substring(0, 2);
                }
                break;
            case 1:
                // CAL_ID
                param.Parameter = 0x04;
                for (int i = 0; i < qty; i++) {
                    strData += response.Data.Substring(4 * 2 + i * 20 * 2, 16 * 2);
                }
                response.Data = strData;
                value2 = GetMode010209Value(param, response);
                break;
            default:
                value2.ErrorDetected = true;
                break;
            }
            param.Parameter = OriginalParam;
            param.Service = OriginalService;
            return value2;
        }

        private OBDParameterValue GetJ1939Value(OBDParameter param, OBDResponse response) {
            OBDParameterValue value2 = new OBDParameterValue();
            if (response.Header.Substring(2, 2) == "E8") {
                // J1939的确认消息，非正确的返回值
                value2.ErrorDetected = true;
                return value2;
            }
            switch (param.Parameter) {
            case 0xFECE:
                value2 = GetDM5Value(param, response);
                break;
            case 0xD300:
                value2 = GetDM19Value(param, response);
                break;
            case 0xFEEC:
                // VIN
                value2.ListStringValue = SetMode09ASCII(17 * 2, response);
                break;
            }
            return value2;
        }

        public OBDParameterValue GetValue(OBDParameter param, OBDResponse response) {
            OBDParameterValue value2 = new OBDParameterValue();
            if (response == null) {
                value2.ErrorDetected = true;
                return value2;
            }
            switch (param.Service) {
            case 0:
                // SAE J1939
                value2 = GetJ1939Value(param, response);
                break;
            case 1:
            case 2:
                value2 = GetMode010209Value(param, response);
                break;
            case 3:
            case 7:
            case 0x0A:
                value2 = GetMode03070AValue(response);
                break;
            case 5:
                value2 = GetMode05Value(param, response);
                break;
            case 9:
                value2 = GetMode010209Value(param, response);
                break;
            case 0x19:
                // ISO 27145 ReadDTCInformation
                string reportType = param.OBDRequest.Substring(2, 2);
                if (reportType == "42") {
                    value2 = Get42DTCValue(response);
                } else if (reportType == "55") {
                    value2 = Get55DTCValue(response);
                }
                break;
            case 0x22:
                // ISO 27145 ReadDataByIdentifer
                int HByte = (param.Parameter >> 8) & 0xFF;
                int LByte = param.Parameter & 0x00FF;
                int OriginalParam = param.Parameter;
                int OriginalService = param.Service;
                param.Parameter = LByte;
                if (HByte == 0xF4) {
                    param.Service = 1;
                    value2 = GetMode010209Value(param, response);
                } else if (HByte == 0xF8) {
                    param.Service = 9;
                    value2 = GetMode010209Value(param, response);
                }
                param.Parameter = OriginalParam;
                param.Service = OriginalService;
                break;
            default:
                value2.ErrorDetected = true;
                break;
            }
            value2.ECUResponseID = response.Header;
            if (value2.ECUResponseID.Length == 6) {
                // 如果是K线协议的话ECUResponseID取最后2个字节
                value2.ECUResponseID = value2.ECUResponseID.Substring(2);
            } else if (value2.ECUResponseID.Length == 8 && param.Service == 0) {
                // 如果是J1939协议的话ECUResponseID取最后1个字节
                value2.ECUResponseID = value2.ECUResponseID.Substring(6);
            }
            return value2;
        }

        public OBDParameterValue GetValue(OBDParameter param, OBDResponseList responses) {
            if (responses.ResponseCount == 1) {
                return GetValue(param, responses.GetOBDResponse(0));
            }
            if ((param.Service == 1) || (param.Service == 2)) {
                if (((param.Parameter == 0) || (param.Parameter == 0x20)) || ((param.Parameter == 0x40) || (param.Parameter == 0x60))) {
                    OBDParameterValue value7 = new OBDParameterValue();
                    for (int i = 0; i < responses.ResponseCount; i++) {
                        OBDParameterValue value4 = GetValue(param, responses.GetOBDResponse(i));
                        if (value4.ErrorDetected) {
                            return value4;
                        }
                        for (int j = 0; j < 0x20; j++) {
                            value7.SetBitFlag(j, value4.GetBitFlag(j));
                        }
                    }
                    return value7;
                }
                if ((param.Parameter == 1) && (param.SubParameter == 0)) {
                    OBDParameterValue value2 = new OBDParameterValue {
                        BoolValue = false
                    };
                    for (int i = 0; i < responses.ResponseCount; i++) {
                        OBDParameterValue value6 = GetValue(param, responses.GetOBDResponse(i));
                        if (value6.ErrorDetected) {
                            return value6;
                        }
                        if (value6.BoolValue) {
                            value2.BoolValue = true;
                        }
                    }
                    if (value2.BoolValue) {
                        value2.DoubleValue = 1.0;
                        value2.StringValue = "ON";
                        value2.ShortStringValue = "ON";
                        return value2;
                    } else {
                        value2.DoubleValue = 0.0;
                        value2.StringValue = "OFF";
                        value2.ShortStringValue = "OFF";
                        return value2;
                    }
                }
                if ((param.Parameter == 1) && (param.SubParameter == 1)) {
                    OBDParameterValue value3 = new OBDParameterValue {
                        DoubleValue = 0.0
                    };
                    for (int i = 0; i < responses.ResponseCount; i++) {
                        OBDParameterValue value5 = GetValue(param, responses.GetOBDResponse(i));
                        if (value5.ErrorDetected) {
                            return value5;
                        }
                        value3.DoubleValue = value5.DoubleValue + value3.DoubleValue;
                    }
                    return value3;
                }
            }
            if ((param.Service != 3) && (param.Service != 7)) {
                return GetValue(param, responses.GetOBDResponse(0));
            }
            OBDParameterValue value8 = new OBDParameterValue();
            List<string> strings = new List<string>();
            for (int i = 0; i < responses.ResponseCount; i++) {
                foreach (string str in GetValue(param, responses.GetOBDResponse(i)).ListStringValue) {
                    strings.Add(str);
                }
            }
            value8.ListStringValue = strings;
            return value8;
        }

        public string GetDTCName(string strHexDTC) {
            if (strHexDTC.Length < 4) {
                return "P0000";
            } else {
                return GetDTCSystem(strHexDTC.Substring(0, 1)) + strHexDTC.Substring(1);
            }
        }

        private string GetDTCSystem(string strSysId) {
            string strSys;
            switch (strSysId) {
            case "0":
                strSys = "P0";
                break;
            case "1":
                strSys = "P1";
                break;
            case "2":
                strSys = "P2";
                break;
            case "3":
                strSys = "P3";
                break;
            case "4":
                strSys = "C0";
                break;
            case "5":
                strSys = "C1";
                break;
            case "6":
                strSys = "C2";
                break;
            case "7":
                strSys = "C3";
                break;
            case "8":
                strSys = "B0";
                break;
            case "9":
                strSys = "B1";
                break;
            case "A":
                strSys = "B2";
                break;
            case "B":
                strSys = "B3";
                break;
            case "C":
                strSys = "U0";
                break;
            case "D":
                strSys = "U1";
                break;
            case "E":
                strSys = "U2";
                break;
            case "F":
                strSys = "U3";
                break;
            default:
                strSys = "ER";
                break;
            }
            return strSys;
        }

        string GetDisplayString(Signal sigIn) {
            string ret = string.Empty;
            int iUsed = sigIn.TestSignalUesed();
            if (iUsed > 0) {
                if (sigIn.ValueDescs.ContainsKey((uint)sigIn.RawValue)) {
                    ret = sigIn.ValueDescs[(uint)sigIn.RawValue];
                    foreach (ValueDisplay vd in m_valDispalys) {
                        if (sigIn.Parent is Message msg) {
                            if (vd.ID == msg.ID && vd.Name == sigIn.Name) {
                                ret = vd.Values[(uint)sigIn.RawValue];
                            }
                        }
                    }
                } else {
                    if (sigIn.Unit == "ASCII" || sigIn.Unit == "HEX") {
                        foreach (string item in sigIn.ListString) {
                            ret += item + ",";
                        }
                        ret = ret.TrimEnd(',');
                    } else {
                        ret = sigIn.Value.ToString();
                    }
                }
            } else if (iUsed == 0) {
                ret = "不适用";
            }
            return ret;
        }

    }
}