using DBCParser;
using DBCParser.DBCObj;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SH_OBD_DLL {
    public partial class OBDInterpreter {
        private readonly Parser _dbc;
        private readonly NetWork _netWork;

        public OBDInterpreter(NetWork netWork, Parser dbc) {
            _dbc = dbc;
            _netWork = netWork;
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

        public OBDParameterValue GetPIDValue(uint ID, string strData, string signalName) {
            OBDParameterValue value2 = new OBDParameterValue();
            Message msg = _netWork.GetMessage(ID);
            if (msg == null && ID < 0x80000000) {
                msg = _netWork.GetMessage(ID + 0x80000000);
            }
            if (msg == null) {
                value2.ErrorDetected = true;
            } else {
                if (msg.SetSignalRawValue(strData)) {
                    value2.Message = msg;
                    foreach (Signal signal in msg.Signals.Values) {
                        signal.DisplayString = _dbc.GetDisplayString(signal, "不适用");
                        if (signalName == signal.Name || signalName.Length == 0) {
                            int iUsed = signal.TestSignalUesed();
                            if (iUsed > 0) {
                                value2.DoubleValue = Math.Round(signal.Value, 2);
                                value2.BoolValue = signal.RawValue > 0;
                                foreach (string item in signal.ListString) {
                                    value2.ListStringValue.Add(item);
                                }
                                value2.ShortStringValue = signal.DisplayString;
                                value2.StringValue = value2.ShortStringValue;
                            }
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
                value2 = GetPIDValue(ID, response.Data, param.SignalName);
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

        /// <summary>
        /// 返回J1939 DTC，返回的字符串格式："FMI/SPN/OC"
        /// </summary>
        /// <param name="strDTCData"></param>
        /// <param name="bOC"></param>
        /// <returns></returns>
        private string GetJ1939DTCValue(string strDTCData, bool bOC) {
            string strRet = string.Empty;
            if (bOC) {
                if (strDTCData.Length < 4) {
                    return string.Empty;
                }
            } else {
                if (strDTCData.Length < 3) {
                    return string.Empty;
                }
            }
            string strLByte = strDTCData.Substring(0, 2);
            string strMByte = strDTCData.Substring(2, 2);
            string strHByte = strDTCData.Substring(4, 2);
            int LByte = Convert.ToByte(strLByte, 16);
            int MByte = Convert.ToByte(strMByte, 16);
            int HByte = Convert.ToByte(strHByte, 16);
            int FMI = HByte & 0x1F;
            int SPN = (HByte >> 5) * 0x10000 + MByte * 0x100 + LByte;
            if (bOC) {
                string strOC = strDTCData.Substring(6, 2);
                int OC = Convert.ToByte(strOC, 16) & 0x7F;
                strRet = FMI.ToString() + "/" + SPN.ToString() + "/" + OC.ToString();
            } else {
                strRet = SPN.ToString();
            }
            return strRet;
        }

        private OBDParameterValue GetDM1_12_6_23Value(OBDParameter param, OBDResponse response) {
            OBDParameterValue value2 = new OBDParameterValue();
            if (response.GetDataByteCount() < 6) {
                value2.ErrorDetected = true;
                return value2;
            }
            int OriginalParam = param.Parameter;
            int OriginalService = param.Service;

            switch (param.SignalName) {
            case "":
            case "MIL":
                // Malfunction Indicator Lamp Status
                byte A = Convert.ToByte(response.GetDataByte(0), 16);
                A = (byte)((A << 1) & 0x80);
                response.Data = A.ToString("X2") + "000000";
                value2 = GetPIDValue(0x101, response.Data, param.SignalName);
                break;
            case "DTC":
                List<string> strings = new List<string>();
                string strDTCData = response.Data.Substring(2 * 2);
                if (response.GetDataByteCount() > 8) {
                    int count = strDTCData.Length / (4 * 2);
                    for (int i = 0; i < count; i++) {
                        strings.Add(GetJ1939DTCValue(strDTCData.Substring(i * 4 * 2, 4 * 2), true));
                    }
                } else {
                    strings.Add(GetJ1939DTCValue(strDTCData, true));
                }
                value2.ListStringValue = strings;
                break;
            default:
                value2.ErrorDetected = true;
                break;
            }

            param.Parameter = OriginalParam;
            param.Service = OriginalService;
            return value2;
        }

        private byte ConvertJ1939RDYByte(byte byteIn) {
            byte byteOut = (byte)((byteIn >> 4) & 0x01);
            byteOut |= (byte)((byteIn >> 2) & 0x02);
            byteOut |= (byte)((byteIn << 4) & 0x40);
            byteOut |= (byte)((byteIn << 2) & 0x08);
            byteOut |= (byte)((byteIn << 5) & 0x20);
            return byteOut;
        }

        private OBDParameterValue GetDM5Value(OBDParameter param, OBDResponse response) {
            OBDParameterValue value2 = new OBDParameterValue();
            if (response.GetDataByteCount() < 8) {
                value2.ErrorDetected = true;
                return value2;
            }
            int OriginalParam = param.Parameter;
            int OriginalService = param.Service;
            byte temp, dataB;
            switch (param.SignalName) {
            case "":
            case "OBDSUP":
                // OBD型式
                response.Data = response.GetDataByte(2);
                value2 = GetPIDValue(0x11C, response.Data, param.SignalName);
                break;
            case "SPARK_RDY":
                param.Service = 1;
                param.Parameter = 0x01;
                temp = Convert.ToByte(response.GetDataByte(3), 16);
                dataB = (byte)(temp & ~0x08);
                response.Data = "00" + dataB.ToString("X2") + response.GetDataByte(4) + response.GetDataByte(6);
                value2 = GetMode010209Value(param, response);
                break;
            case "COMP_RDY":
                param.Service = 1;
                param.Parameter = 0x01;
                temp = Convert.ToByte(response.GetDataByte(3), 16);
                dataB = (byte)(temp | 0x08);
                temp = Convert.ToByte(response.GetDataByte(5), 16);
                byte dataC = ConvertJ1939RDYByte(temp);
                temp = Convert.ToByte(response.GetDataByte(7), 16);
                byte dataD = ConvertJ1939RDYByte(temp);
                response.Data = "00" + dataB.ToString("X2") + dataC.ToString("X2") + dataD.ToString("X2");
                value2 = GetMode010209Value(param, response);
                param.Parameter = OriginalParam;
                param.Service = OriginalService;
                break;
            default:
                value2.ErrorDetected = true;
                break;
            }
            param.Parameter = OriginalParam;
            param.Service = OriginalService;
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
            switch (param.SignalName) {
            case "CVN":
                param.Parameter = 0x06;
                for (int i = 0; i < qty; i++) {
                    strData += response.Data.Substring(i * 20 * 2, 4 * 2);
                }
                response.Data = strData;
                value2 = GetMode010209Value(param, response);
                for (int i = 0; i < value2.ListStringValue.Count; i++) {
                    string strVal = value2.ListStringValue[i];
                    value2.ListStringValue[i] = Utility.ReverseString(strVal, 2, true);
                }
                break;
            case "CAL_ID":
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

        private double GetMPRRate(double num, double den) {
            double dRet;
            if (den == 0) {
                dRet = 7.99527;
            } else {
                dRet = Math.Round(num / den, 6);
                if (dRet > 7.99527) {
                    dRet = 7.99527;
                }
            }
            return dRet;
        }

        private OBDParameterValue GetDM20Value(OBDParameter param, OBDResponse response) {
            OBDParameterValue value2 = new OBDParameterValue();
            if (response.GetDataByteCount() < 4) {
                value2.ErrorDetected = true;
                return value2;
            }
            List<string> lsMPR = new List<string>();
            string strMPRData = Utility.ReverseString(response.Data.Substring(0 * 2, 2 * 2), 2, true);
            string IGNCNTR = Convert.ToInt32(strMPRData, 16).ToString();
            lsMPR.Add(IGNCNTR);
            strMPRData = Utility.ReverseString(response.Data.Substring(2 * 2, 2 * 2), 2, true);
            string OBDCOND = Convert.ToInt32(strMPRData, 16).ToString();
            lsMPR.Add(OBDCOND);
            for (int i = 0; (9 + i * 7 + 2) <= response.GetDataByteCount(); i++) {
                strMPRData = response.Data.Substring((4 + i * 7) * 2, 3 * 2);
                string strSPN = GetJ1939DTCValue(strMPRData, false);
                strMPRData = Utility.ReverseString(response.Data.Substring((7 + i * 7) * 2, 2 * 2), 2, true);
                int iNumerator = Convert.ToInt32(strMPRData, 16);
                strMPRData = Utility.ReverseString(response.Data.Substring((9 + i * 7) * 2, 2 * 2), 2, true);
                int iDenominator = Convert.ToInt32(strMPRData, 16);
                double dMPR = GetMPRRate(iNumerator, iDenominator);
                strMPRData = strSPN + "/" + iNumerator.ToString() + "/" + iDenominator.ToString() + "/" + dMPR.ToString();
                lsMPR.Add(strMPRData);
            }
            value2.ListStringValue = lsMPR;
            return value2;
        }

        private OBDParameterValue GetPGNDoubleValue(OBDParameter param, OBDResponse response) {
            OBDParameterValue value2 = new OBDParameterValue();
            if (response.GetDataByteCount() < 8) {
                value2.ErrorDetected = true;
                return value2;
            }
            int PGN = param.Parameter;
            int dataA, dataB;
            int OriginalParam = param.Parameter;
            int OriginalService = param.Service;
            param.Service = 1;
            switch (PGN) {
            case 0xC100:
                switch (param.SignalName) {
                case "MIL_DIST":
                    param.Parameter = 0x21;
                    response.Data = Utility.ReverseString(response.Data.Substring(0 * 2, 2 * 2), 2, true);
                    value2 = GetMode010209Value(param, response);
                    break;
                default:
                    value2.ErrorDetected = true;
                    break;
                }
                break;
            case 0xF004:
                switch (param.SignalName) {
                case "RPM":
                    param.Parameter = 0x0C;
                    response.Data = Utility.ReverseString(response.Data.Substring(3 * 2, 2 * 2), 2, true);
                    value2 = GetMode010209Value(param, response);
                    value2.DoubleValue *= 0.5;
                    break;
                case "TQ_ACT":
                    param.Parameter = 0x62;
                    response.Data = response.GetDataByte(2);
                    value2 = GetMode010209Value(param, response);
                    break;
                default:
                    value2.ErrorDetected = true;
                    break;
                }
                break;
            case 0xF00A:
                switch (param.SignalName) {
                case "MAF":
                    param.Parameter = 0x10;
                    response.Data = Utility.ReverseString(response.Data.Substring(2 * 2, 2 * 2), 2, true);
                    value2 = GetMode010209Value(param, response);
                    value2.DoubleValue *= 25 / 18;
                    break;
                default:
                    value2.ErrorDetected = true;
                    break;
                }
                break;
            case 0xF00E:
                switch (param.SignalName) {
                case "NOXC11":
                    param.Parameter = 0xA1;
                    response.Data = Utility.ReverseString(response.Data.Substring(0 * 2, 2 * 2), 2, true);
                    value2 = GetMode010209Value(param, response);
                    value2.DoubleValue = value2.DoubleValue * 0.05 - 200;
                    break;
                default:
                    value2.ErrorDetected = true;
                    break;
                }
                break;
            case 0xFDD5:
                switch (param.SignalName) {
                case "EGR_PCT":
                    param.Parameter = 0x2C;
                    dataA = Utility.Hex2Int(response.GetDataByte(4));
                    dataB = Utility.Hex2Int(response.GetDataByte(5));
                    value2.DoubleValue = ((dataB * 0x100) + dataA) * 0.0025;
                    value2.DoubleValue = Math.Round(value2.DoubleValue, 1);
                    break;
                default:
                    value2.ErrorDetected = true;
                    break;
                }
                break;
            case 0xFEDB:
                switch (param.SignalName) {
                case "FRP":
                    param.Parameter = 0x23;
                    response.Data = Utility.ReverseString(response.Data.Substring(0 * 2, 2 * 2), 2, true);
                    value2 = GetMode010209Value(param, response);
                    value2.DoubleValue *= 25 / 64;
                    break;
                default:
                    value2.ErrorDetected = true;
                    break;
                }
                break;
            case 0xFEE0:
                switch (param.SignalName) {
                case "ODO":
                    param.Parameter = 0xA6;
                    response.Data = Utility.ReverseString(response.Data.Substring(4 * 2, 4 * 2), 2, true);
                    value2 = GetMode010209Value(param, response);
                    value2.DoubleValue *= 1.25;
                    break;
                default:
                    value2.ErrorDetected = true;
                    break;
                }
                break;
            case 0xFEE3:
                if (response.GetDataByteCount() < 39) {
                    value2.ErrorDetected = true;
                    return value2;
                }
                switch (param.SignalName) {
                case "TQ_REF":
                    param.Parameter = 0x63;
                    response.Data = Utility.ReverseString(response.Data.Substring(19 * 2, 2 * 2), 2, true);
                    value2 = GetMode010209Value(param, response);
                    break;
                default:
                    value2.ErrorDetected = true;
                    break;
                }
                break;
            case 0xFEEE:
                switch (param.SignalName) {
                case "ECT":
                    param.Parameter = 0x05;
                    response.Data = response.GetDataByte(0);
                    value2 = GetMode010209Value(param, response);
                    break;
                case "EOT":
                    dataA = Utility.Hex2Int(response.GetDataByte(2));
                    dataB = Utility.Hex2Int(response.GetDataByte(3));
                    value2.DoubleValue = ((dataB * 0x100) + dataA) * 0.03125 - 273;
                    value2.DoubleValue = Math.Round(value2.DoubleValue, 1);
                    break;
                default:
                    value2.ErrorDetected = true;
                    break;
                }
                break;
            case 0xFEF1:
                switch (param.SignalName) {
                case "VSS":
                    dataA = Utility.Hex2Int(response.GetDataByte(1));
                    dataB = Utility.Hex2Int(response.GetDataByte(2));
                    value2.DoubleValue = ((dataB * 0x100) + dataA) / 256;
                    value2.DoubleValue = Math.Round(value2.DoubleValue, 1);
                    break;
                default:
                    value2.ErrorDetected = true;
                    break;
                }
                break;
            case 0xFEF2:
                switch (param.SignalName) {
                case "TP_R":
                    param.Parameter = 0x45;
                    response.Data = response.GetDataByte(6);
                    value2 = GetMode010209Value(param, response);
                    break;
                case "FUEL_RATE":
                    param.Parameter = 0x5E;
                    response.Data = Utility.ReverseString(response.Data.Substring(0 * 2, 2 * 2), 2, true);
                    value2 = GetMode010209Value(param, response);
                    break;
                default:
                    value2.ErrorDetected = true;
                    break;
                }
                break;
            case 0xFEF6:
                switch (param.SignalName) {
                case "MAP":
                    param.Parameter = 0x0B;
                    response.Data = response.GetDataByte(3);
                    value2 = GetMode010209Value(param, response);
                    value2.DoubleValue *= 2;
                    break;
                case "BP_A_ACT":
                    param.Parameter = 0x70;
                    value2.DoubleValue = Utility.Hex2Int(response.GetDataByte(1)) * 2;
                    break;
                case "EGT11":
                    param.Parameter = 0x78;
                    dataA = Utility.Hex2Int(response.GetDataByte(5));
                    dataB = Utility.Hex2Int(response.GetDataByte(6));
                    value2.DoubleValue = ((dataB * 0x100) + dataA) * 0.03125 - 273;
                    value2.DoubleValue = Math.Round(value2.DoubleValue, 1);
                    break;
                default:
                    value2.ErrorDetected = true;
                    break;
                }
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
            case 0xFECA:
            case 0xFED4:
            case 0xFECF:
            case 0xFDB5:
            case 0xFD82:
            case 0xFD80:
            case 0xFD5F:
            case 0xFD5E:
            case 0xFD5D:
            case 0xFD5C:
            case 0xFD5B:
            case 0xFD5A:
            case 0xFD59:
            case 0xFD58:
            case 0xFD57:
            case 0xFD56:
            case 0xFD55:
            case 0xFD54:
                value2 = GetDM1_12_6_23Value(param, response);
                break;
            case 0xFECE:
                value2 = GetDM5Value(param, response);
                break;
            case 0xD300:
                value2 = GetDM19Value(param, response);
                break;
            case 0xC200:
                value2 = GetDM20Value(param, response);
                break;
            case 0xFEEC:
                // VIN
                value2.ListStringValue = SetMode09ASCII(17 * 2, response);
                break;
            default:
                value2 = GetPGNDoubleValue(param, response);
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

    }
}