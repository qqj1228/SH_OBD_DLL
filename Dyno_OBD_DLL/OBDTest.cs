using SH_OBD_DLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dyno_OBD_DLL {
    public class OBDTest {
        private readonly SH_OBD_Dll _OBDDll;
        private readonly OBDInterface _OBDIf;

        public OBDTest(SH_OBD_Dll OBDDll) {
            _OBDDll = OBDDll;
            _OBDIf = _OBDDll.GetOBDInterface();
        }

        private string GetParamValue(OBDParameterValue value, int valueType) {
            string strRet = string.Empty;
            if ((valueType & (int)OBDParameter.EnumValueTypes.Bool) != 0) {
                if (value.BoolValue) {
                    strRet = "ON";
                } else {
                    strRet = "OFF";
                }
            } else if ((valueType & (int)OBDParameter.EnumValueTypes.Double) != 0) {
                strRet = value.DoubleValue.ToString();
            } else if ((valueType & (int)OBDParameter.EnumValueTypes.String) != 0) {
                strRet = value.StringValue;
            } else if ((valueType & (int)OBDParameter.EnumValueTypes.ShortString) != 0) {
                strRet = value.ShortStringValue;
            } else if ((valueType & (int)OBDParameter.EnumValueTypes.ListString) != 0) {
                strRet = string.Empty;
                for (int i = 0; i < value.ListStringValue.Count; i++) {
                    strRet += value.ListStringValue[i] + ",";
                }
                strRet = strRet.TrimEnd(',');
            }
            return strRet;
        }

        /// <summary>
        /// 返回车辆是否是柴油
        /// </summary>
        /// <returns></returns>
        public bool IsDiesel() {
            if (_OBDIf.STDType == StandardType.SAE_J1939) {
                return true;
            } else {
                bool compIgn = true;
                OBDParameter param = new OBDParameter();
                if (_OBDIf.STDType == StandardType.ISO_27145) {
                    param.OBDRequest = "22F401";
                    param.Service = 0x22;
                    param.Parameter = 0xF401;
                } else {
                    param.OBDRequest = "0101";
                    param.Service = 1;
                    param.Parameter = 0x01;
                }
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.BitFlags;
                List<OBDParameterValue> valueList = _OBDIf.GetValueList(param);
                foreach (OBDParameterValue value in valueList) {
                    if (value.ErrorDetected) {
                        continue;
                    }
                    if (_OBDDll.Mode01Support.ContainsKey(value.ECUResponseID) && _OBDDll.Mode01Support[value.ECUResponseID][(param.Parameter & 0x00FF) - 1]) {
                        compIgn = value.GetBitFlag(12);
                        break;
                    }
                }
                return compIgn;
            }
        }

        public Dictionary<string, string> GetMIL() {
            Dictionary<string, string> dicRet = new Dictionary<string, string>();
            OBDParameter param = new OBDParameter();
            if (_OBDIf.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "22F491";
                param.Service = 0x22;
                param.Parameter = 0xF491;
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ShortString;
                param.SignalName = "MI_MODE_ECU";
            } else if (_OBDIf.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "0101";
                param.Service = 1;
                param.Parameter = 0x01;
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ShortString;
                param.SignalName = "MIL";
            } else if (_OBDIf.STDType == StandardType.SAE_J1939) {
                dicRet.Add(string.Empty, string.Empty);
                return dicRet;
            }

            List<OBDParameterValue> valueList = _OBDIf.GetValueList(param);
            foreach (OBDParameterValue value in valueList) {
                if (value.ErrorDetected) {
                    continue;
                }
                if (_OBDDll.Mode01Support.ContainsKey(value.ECUResponseID) && _OBDDll.Mode01Support[value.ECUResponseID][(param.Parameter & 0x00FF) - 1]) {
                    dicRet.Add(value.ECUResponseID, GetParamValue(value, param.ValueTypes));
                }
            }

            return dicRet;
        }

        public Dictionary<string, string> GetVIN() {
            Dictionary<string, string> dicRet = new Dictionary<string, string>();
            OBDParameter param = new OBDParameter();
            if (_OBDIf.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "22F802";
                param.Service = 0x22;
                param.Parameter = 0xF802;
            } else if (_OBDIf.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "0902";
                param.Service = 9;
                param.Parameter = 2;
            } else if (_OBDIf.STDType == StandardType.SAE_J1939) {
                param.OBDRequest = "00FEEC";
                param.Service = 0;
                param.Parameter = 0xFEEC;
                _OBDIf.SetTimeout(1000);
            }
            param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
            param.SignalName = "VIN";

            List<OBDParameterValue> valueList = _OBDIf.GetValueList(param);
            foreach (OBDParameterValue value in valueList) {
                if (value.ErrorDetected) {
                    continue;
                }
                bool flag = _OBDDll.Mode09Support.ContainsKey(value.ECUResponseID) && _OBDDll.Mode09Support[value.ECUResponseID][(param.Parameter & 0x00FF) - 1];
                flag = flag || _OBDIf.STDType == StandardType.SAE_J1939;
                if (flag) {
                    dicRet.Add(value.ECUResponseID, GetParamValue(value, param.ValueTypes));
                }
            }
            _OBDIf.SetTimeout();
            return dicRet;
        }

        public Dictionary<string, string> GetPID1C() {
            Dictionary<string, string> dicRet = new Dictionary<string, string>();
            OBDParameter param = new OBDParameter();
            if (_OBDIf.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "22F41C";
                param.Service = 0x22;
                param.Parameter = 0xF41C;
            } else if (_OBDIf.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "011C";
                param.Service = 1;
                param.Parameter = 0x1C;
            } else if (_OBDIf.STDType == StandardType.SAE_J1939) {
                param.OBDRequest = "00FECE";
                param.Service = 0;
                param.Parameter = 0xFECE;
            }
            param.ValueTypes = (int)OBDParameter.EnumValueTypes.ShortString;
            param.SignalName = "OBDSUP";

            List<OBDParameterValue> valueList = _OBDIf.GetValueList(param);
            foreach (OBDParameterValue value in valueList) {
                if (value.ErrorDetected) {
                    continue;
                }
                if (_OBDIf.STDType == StandardType.SAE_J1939) {
                    dicRet.Add(value.ECUResponseID, GetParamValue(value, param.ValueTypes));
                } else if (_OBDDll.Mode01Support.ContainsKey(value.ECUResponseID) && _OBDDll.Mode01Support[value.ECUResponseID][(param.Parameter & 0x00FF) - 1]) {
                    dicRet.Add(value.ECUResponseID, GetParamValue(value, param.ValueTypes));
                }
            }

            return dicRet;
        }

        public Dictionary<string, string> GetCVN() {
            Dictionary<string, string> dicRet = new Dictionary<string, string>();
            OBDParameter param = new OBDParameter();
            if (_OBDIf.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "22F806";
                param.Service = 0x22;
                param.Parameter = 0xF806;
            } else if (_OBDIf.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "0906";
                param.Service = 9;
                param.Parameter = 6;
            } else if (_OBDIf.STDType == StandardType.SAE_J1939) {
                param.OBDRequest = "00D300";
                param.Service = 0;
                param.Parameter = 0xD300;
                _OBDIf.SetTimeout(1000);
            }
            param.SignalName = "CVN";
            param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;

            List<OBDParameterValue> valueList = _OBDIf.GetValueList(param);
            foreach (OBDParameterValue value in valueList) {
                if (value.ErrorDetected) {
                    continue;
                }
                bool flag = _OBDDll.Mode09Support.ContainsKey(value.ECUResponseID) && _OBDDll.Mode09Support[value.ECUResponseID][(param.Parameter & 0x00FF) - 1];
                flag = flag || _OBDIf.STDType == StandardType.SAE_J1939;
                if (flag) {
                    dicRet.Add(value.ECUResponseID, GetParamValue(value, param.ValueTypes));
                }
            }
            _OBDIf.SetTimeout();
            return dicRet;
        }

        public Dictionary<string, string> GetCALID() {
            Dictionary<string, string> dicRet = new Dictionary<string, string>();
            OBDParameter param = new OBDParameter();
            if (_OBDIf.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "22F804";
                param.Service = 0x22;
                param.Parameter = 0xF804;
            } else if (_OBDIf.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "0904";
                param.Service = 9;
                param.Parameter = 4;
            } else if (_OBDIf.STDType == StandardType.SAE_J1939) {
                param.OBDRequest = "00D300";
                param.Service = 0;
                param.Parameter = 0xD300;
                _OBDIf.SetTimeout(1000);
            }
            param.SignalName = "CAL_ID";
            param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;

            List<OBDParameterValue> valueList = _OBDIf.GetValueList(param);
            foreach (OBDParameterValue value in valueList) {
                if (value.ErrorDetected) {
                    continue;
                }
                bool flag = _OBDDll.Mode09Support.ContainsKey(value.ECUResponseID) && _OBDDll.Mode09Support[value.ECUResponseID][(param.Parameter & 0x00FF) - 1];
                flag = flag || _OBDIf.STDType == StandardType.SAE_J1939;
                if (flag) {
                    dicRet.Add(value.ECUResponseID, GetParamValue(value, param.ValueTypes));
                }
            }
            _OBDIf.SetTimeout();
            return dicRet;
        }

        public Dictionary<string, string> GetDTC03() {
            Dictionary<string, string> dicRet = new Dictionary<string, string>();
            OBDParameter param = new OBDParameter();
            if (_OBDIf.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "194233081E";
            } else if (_OBDIf.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "03";
            } else if (_OBDIf.STDType == StandardType.SAE_J1939) {
                dicRet.Add(string.Empty, string.Empty);
                return dicRet;
            }
            param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;

            List<OBDParameterValue> valueList = _OBDIf.GetValueList(param);
            foreach (OBDParameterValue value in valueList) {
                if (value.ErrorDetected) {
                    continue;
                }
                dicRet.Add(value.ECUResponseID, GetParamValue(value, param.ValueTypes));
            }

            return dicRet;
        }

        public Dictionary<string, string> GetDTC07() {
            Dictionary<string, string> dicRet = new Dictionary<string, string>();
            OBDParameter param = new OBDParameter();
            if (_OBDIf.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "194233041E";
            } else if (_OBDIf.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "07";
            } else if (_OBDIf.STDType == StandardType.SAE_J1939) {
                dicRet.Add(string.Empty, string.Empty);
                return dicRet;
            }
            param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;

            List<OBDParameterValue> valueList = _OBDIf.GetValueList(param);
            foreach (OBDParameterValue value in valueList) {
                if (value.ErrorDetected) {
                    continue;
                }
                dicRet.Add(value.ECUResponseID, GetParamValue(value, param.ValueTypes));
            }

            return dicRet;
        }

        public Dictionary<string, string> GetDTC0A() {
            Dictionary<string, string> dicRet = new Dictionary<string, string>();
            OBDParameter param = new OBDParameter();
            if (_OBDIf.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "195533";
            } else if (_OBDIf.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "0A";
            } else if (_OBDIf.STDType == StandardType.SAE_J1939) {
                dicRet.Add(string.Empty, string.Empty);
                return dicRet;
            }
            param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;

            List<OBDParameterValue> valueList = _OBDIf.GetValueList(param);
            foreach (OBDParameterValue value in valueList) {
                if (value.ErrorDetected) {
                    continue;
                }
                dicRet.Add(value.ECUResponseID, GetParamValue(value, param.ValueTypes));
            }

            return dicRet;
        }

        public Dictionary<string, double> GetPIDDouble(byte PID, string signalName) {
            Dictionary<string, double> dicRet = new Dictionary<string, double>();
            OBDParameter param = new OBDParameter();
            if (_OBDIf.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "22F4" + PID.ToString("X2");
                param.Service = 0x22;
                param.Parameter = 0xF400 + PID;
            } else if (_OBDIf.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "01" + PID.ToString("X2");
                param.Service = 1;
                param.Parameter = PID;
            } else if (_OBDIf.STDType == StandardType.SAE_J1939) {
                dicRet.Add(string.Empty, 0);
                return dicRet;
            }
            param.ValueTypes = (int)OBDParameter.EnumValueTypes.Double;
            param.SignalName = signalName;

            List<OBDParameterValue> valueList = _OBDIf.GetValueList(param);
            foreach (OBDParameterValue value in valueList) {
                if (value.ErrorDetected) {
                    continue;
                }
                if (_OBDDll.Mode01Support.ContainsKey(value.ECUResponseID) && _OBDDll.Mode01Support[value.ECUResponseID][(param.Parameter & 0x00FF) - 1]) {
                    dicRet.Add(value.ECUResponseID, value.DoubleValue);
                }
            }

            return dicRet;
        }

        private string GetSignalDisplayString(OBDParameterValue value, string signalName, bool supported) {
            if (supported && value.Message.Signals.ContainsKey(signalName)) {
                return value.Message.Signals[signalName].DisplayString;
            } else {
                return "不适用";
            }
        }

        /// <summary>
        /// 获取车辆的就绪状态，ECM才会返回就绪状态，其他ECU不会返回
        /// 故返回值Dictionary<string, string>为：Dictionary<就绪状态名, 就绪状态值>
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetReadiness() {
            Dictionary<string, string> dicRet = new Dictionary<string, string>();
            OBDParameter param = new OBDParameter();
            if (_OBDIf.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "22F401";
                param.Service = 0x22;
                param.Parameter = 0xF401;
            } else if (_OBDIf.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "0101";
                param.Service = 1;
                param.Parameter = 0x01;
            } else if (_OBDIf.STDType == StandardType.SAE_J1939) {
                dicRet.Add(string.Empty, string.Empty);
                return dicRet;
            }
            param.ValueTypes = (int)OBDParameter.EnumValueTypes.BitFlags;

            bool compIgn = false;
            List<OBDParameterValue> valueList = _OBDIf.GetValueList(param);
            OBDParameterValue value = valueList[0];
            if (value.ErrorDetected) {
                dicRet.Add(string.Empty, string.Empty);
                return dicRet;
            }
            bool supported = false;
            if (_OBDDll.Mode01Support.ContainsKey(value.ECUResponseID) && _OBDDll.Mode01Support[value.ECUResponseID][(param.Parameter & 0x00FF) - 1]) {
                compIgn = value.GetBitFlag(12);
                supported = true;
            }
            dicRet.Add("MIS_RDY", GetSignalDisplayString(value, "MIS_RDY", supported));
            dicRet.Add("FUEL_RDY", GetSignalDisplayString(value, "FUEL_RDY", supported));
            dicRet.Add("CCM_RDY", GetSignalDisplayString(value, "CCM_RDY", supported));
            if (compIgn) {
                dicRet.Add("HCCATRDY", GetSignalDisplayString(value, "HCCATRDY", supported));
                dicRet.Add("NCAT_RDY", GetSignalDisplayString(value, "NCAT_RDY", supported));
                dicRet.Add("BP_RDY", GetSignalDisplayString(value, "BP_RDY", supported));
                dicRet.Add("EGS_RDY", GetSignalDisplayString(value, "EGS_RDY", supported));
                dicRet.Add("PM_RDY", GetSignalDisplayString(value, "PM_RDY", supported));
                dicRet.Add("EGR_RDY_compression", GetSignalDisplayString(value, "EGR_RDY_compression", supported));
            } else {
                dicRet.Add("CAT_RDY", GetSignalDisplayString(value, "CAT_RDY", supported));
                dicRet.Add("HCAT_RDY", GetSignalDisplayString(value, "HCAT_RDY", supported));
                dicRet.Add("EVAP_RDY", GetSignalDisplayString(value, "EVAP_RDY", supported));
                dicRet.Add("AIR_RDY", GetSignalDisplayString(value, "AIR_RDY", supported));
                dicRet.Add("O2S_RDY", GetSignalDisplayString(value, "O2S_RDY", supported));
                dicRet.Add("HTR_RDY", GetSignalDisplayString(value, "HTR_RDY", supported));
                dicRet.Add("EGR_RDY_spark", GetSignalDisplayString(value, "EGR_RDY_spark", supported));
            }
            return dicRet;
        }

        private string GetIUPRRateString(double num, double den, bool supported) {
            if (!supported) {
                return "不适用";
            }
            string strRet;
            if (den == 0) {
                strRet = "7.99527";
            } else {
                double rate = Math.Round(num / den, 6);
                if (rate > 7.99527) {
                    strRet = "7.99527";
                } else {
                    strRet = rate.ToString();
                }
            }
            return strRet;
        }

        /// <summary>
        /// 获取车辆IUPR，ECM才会返回IUPR，其他ECU不会返回
        /// 故返回值Dictionary<string, string>为：Dictionary<IUPR名, IUPR值>
        /// </summary>
        /// <param name="bDiesel"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetIUPR(bool bDiesel) {
            Dictionary<string, string> dicRet = new Dictionary<string, string>();
            OBDParameter param = new OBDParameter();
            if (bDiesel) {
                if (_OBDIf.STDType == StandardType.ISO_27145) {
                    param.OBDRequest = "22F80B";
                    param.Service = 0x22;
                    param.Parameter = 0xF80B;
                } else if (_OBDIf.STDType == StandardType.ISO_15031) {
                    param.OBDRequest = "090B";
                    param.Service = 9;
                    param.Parameter = 0x0B;
                } else if (_OBDIf.STDType == StandardType.SAE_J1939) {
                    dicRet.Add(string.Empty, string.Empty);
                    return dicRet;
                }
            } else {
                if (_OBDIf.STDType == StandardType.ISO_27145) {
                    param.OBDRequest = "22F808";
                    param.Service = 0x22;
                    param.Parameter = 0xF808;
                } else if (_OBDIf.STDType == StandardType.ISO_15031) {
                    param.OBDRequest = "0908";
                    param.Service = 9;
                    param.Parameter = 0x08;
                } else if (_OBDIf.STDType == StandardType.SAE_J1939) {
                    dicRet.Add(string.Empty, string.Empty);
                    return dicRet;
                }
            }
            param.ValueTypes = (int)OBDParameter.EnumValueTypes.BitFlags;
            List<OBDParameterValue> valueList = _OBDIf.GetValueList(param);
            OBDParameterValue value = valueList[0];

            if (value.ErrorDetected) {
                dicRet.Add(string.Empty, string.Empty);
                return dicRet;
            }
            bool supported = _OBDDll.Mode09Support.ContainsKey(value.ECUResponseID);
            supported = supported && _OBDDll.Mode09Support[value.ECUResponseID][(param.Parameter & 0x00FF) - 1];
            dicRet.Add("OBDCOND", GetSignalDisplayString(value, "OBDCOND", supported));
            dicRet.Add("IGNCNTR", GetSignalDisplayString(value, "IGNCNTR", supported));
            if (bDiesel) {
                dicRet.Add("HCCATCOMP", GetSignalDisplayString(value, "HCCATCOMP", supported));
                dicRet.Add("HCCATCOND", GetSignalDisplayString(value, "HCCATCOND", supported));
                dicRet.Add("HCCATIUPR", GetIUPRRateString(value.Message.Signals["HCCATCOMP"].Value, value.Message.Signals["HCCATCOND"].Value, supported));
                dicRet.Add("NCATCOMP", GetSignalDisplayString(value, "NCATCOMP", supported));
                dicRet.Add("NCATCOND", GetSignalDisplayString(value, "NCATCOND", supported));
                dicRet.Add("NCATIUPR", GetIUPRRateString(value.Message.Signals["NCATCOMP"].Value, value.Message.Signals["NCATCOND"].Value, supported));
                dicRet.Add("NADSCOMP", GetSignalDisplayString(value, "NADSCOMP", supported));
                dicRet.Add("NADSCOND", GetSignalDisplayString(value, "NADSCOND", supported));
                dicRet.Add("NADSIUPR", GetIUPRRateString(value.Message.Signals["NADSCOMP"].Value, value.Message.Signals["NADSCOND"].Value, supported));
                dicRet.Add("PMCOMP", GetSignalDisplayString(value, "PMCOMP", supported));
                dicRet.Add("PMCOND", GetSignalDisplayString(value, "PMCOND", supported));
                dicRet.Add("PMIUPR", GetIUPRRateString(value.Message.Signals["PMCOMP"].Value, value.Message.Signals["PMCOND"].Value, supported));
                dicRet.Add("EGSCOMP", GetSignalDisplayString(value, "EGSCOMP", supported));
                dicRet.Add("EGSCOND", GetSignalDisplayString(value, "EGSCOND", supported));
                dicRet.Add("EGSIUPR", GetIUPRRateString(value.Message.Signals["EGSCOMP"].Value, value.Message.Signals["EGSCOND"].Value, supported));
                dicRet.Add("EGRCOMP_compression", GetSignalDisplayString(value, "EGRCOMP", supported));
                dicRet.Add("EGRCOND_compression", GetSignalDisplayString(value, "EGRCOND", supported));
                dicRet.Add("EGRIUPR_compression", GetIUPRRateString(value.Message.Signals["EGRCOMP"].Value, value.Message.Signals["EGRCOND"].Value, supported));
                dicRet.Add("BPCOMP", GetSignalDisplayString(value, "BPCOMP", supported));
                dicRet.Add("BPCOND", GetSignalDisplayString(value, "BPCOND", supported));
                dicRet.Add("BPIUPR", GetIUPRRateString(value.Message.Signals["BPCOMP"].Value, value.Message.Signals["BPCOND"].Value, supported));
                dicRet.Add("FUELCOMP", GetSignalDisplayString(value, "FUELCOMP", supported));
                dicRet.Add("FUELCOND", GetSignalDisplayString(value, "FUELCOND", supported));
                dicRet.Add("FUELIUPR", GetIUPRRateString(value.Message.Signals["FUELCOMP"].Value, value.Message.Signals["FUELCOND"].Value, supported));
            } else {
                dicRet.Add("CATCOMP1", GetSignalDisplayString(value, "CATCOMP1", supported));
                dicRet.Add("CATCOND1", GetSignalDisplayString(value, "CATCOND1", supported));
                dicRet.Add("CATIUPR1", GetIUPRRateString(value.Message.Signals["CATCOMP1"].Value, value.Message.Signals["CATCOND1"].Value, supported));
                dicRet.Add("CATCOMP2", GetSignalDisplayString(value, "CATCOMP2", supported));
                dicRet.Add("CATCOND2", GetSignalDisplayString(value, "CATCOND2", supported));
                dicRet.Add("CATIUPR2", GetIUPRRateString(value.Message.Signals["CATCOMP2"].Value, value.Message.Signals["CATCOND2"].Value, supported));
                dicRet.Add("O2SCOMP1", GetSignalDisplayString(value, "O2SCOMP1", supported));
                dicRet.Add("O2SCOND1", GetSignalDisplayString(value, "O2SCOND1", supported));
                dicRet.Add("O2SIUPR1", GetIUPRRateString(value.Message.Signals["O2SCOMP1"].Value, value.Message.Signals["O2SCOND1"].Value, supported));
                dicRet.Add("O2SCOMP2", GetSignalDisplayString(value, "O2SCOMP2", supported));
                dicRet.Add("O2SCOND2", GetSignalDisplayString(value, "O2SCOND2", supported));
                dicRet.Add("O2SIUPR2", GetIUPRRateString(value.Message.Signals["O2SCOMP2"].Value, value.Message.Signals["O2SCOND2"].Value, supported));
                dicRet.Add("EGRCOMP_spark", GetSignalDisplayString(value, "EGRCOMP", supported));
                dicRet.Add("EGRCOND_spark", GetSignalDisplayString(value, "EGRCOND", supported));
                dicRet.Add("EGRIUPR_spark", GetIUPRRateString(value.Message.Signals["EGRCOMP"].Value, value.Message.Signals["EGRCOND"].Value, supported));
                dicRet.Add("AIRCOMP", GetSignalDisplayString(value, "AIRCOMP", supported));
                dicRet.Add("AIRCOND", GetSignalDisplayString(value, "AIRCOND", supported));
                dicRet.Add("AIRIUPR", GetIUPRRateString(value.Message.Signals["AIRCOMP"].Value, value.Message.Signals["AIRCOND"].Value, supported));
                dicRet.Add("EVAPCOMP", GetSignalDisplayString(value, "EVAPCOMP", supported));
                dicRet.Add("EVAPCOND", GetSignalDisplayString(value, "EVAPCOND", supported));
                dicRet.Add("EVAPIUPR", GetIUPRRateString(value.Message.Signals["EVAPCOMP"].Value, value.Message.Signals["EVAPCOND"].Value, supported));
                dicRet.Add("SO2SCOMP1", GetSignalDisplayString(value, "SO2SCOMP1", supported));
                dicRet.Add("SO2SCOND1", GetSignalDisplayString(value, "SO2SCOND1", supported));
                dicRet.Add("SO2SIUPR1", GetIUPRRateString(value.Message.Signals["SO2SCOMP1"].Value, value.Message.Signals["SO2SCOND1"].Value, supported));
                dicRet.Add("SO2SCOMP2", GetSignalDisplayString(value, "SO2SCOMP2", supported));
                dicRet.Add("SO2SCOND2", GetSignalDisplayString(value, "SO2SCOND2", supported));
                dicRet.Add("SO2SIUPR2", GetIUPRRateString(value.Message.Signals["SO2SCOMP2"].Value, value.Message.Signals["SO2SCOND2"].Value, supported));
                dicRet.Add("AFRICOMP1", GetSignalDisplayString(value, "AFRICOMP1", supported));
                dicRet.Add("AFRICOND1", GetSignalDisplayString(value, "AFRICOND1", supported));
                dicRet.Add("AFRIIUPR1", GetIUPRRateString(value.Message.Signals["AFRICOMP1"].Value, value.Message.Signals["AFRICOND1"].Value, supported));
                dicRet.Add("AFRICOMP2", GetSignalDisplayString(value, "AFRICOMP2", supported));
                dicRet.Add("AFRICOND2", GetSignalDisplayString(value, "AFRICOND2", supported));
                dicRet.Add("AFRIIUPR2", GetIUPRRateString(value.Message.Signals["AFRICOMP2"].Value, value.Message.Signals["AFRICOND2"].Value, supported));
                dicRet.Add("PFCOMP1", GetSignalDisplayString(value, "PFCOMP1", supported));
                dicRet.Add("PFCOND1", GetSignalDisplayString(value, "PFCOND1", supported));
                dicRet.Add("PFIUPR1", GetIUPRRateString(value.Message.Signals["PFCOMP1"].Value, value.Message.Signals["PFCOND1"].Value, supported));
                dicRet.Add("PFCOMP2", GetSignalDisplayString(value, "PFCOMP2", supported));
                dicRet.Add("PFCOND2", GetSignalDisplayString(value, "PFCOND2", supported));
                dicRet.Add("PFIUPR2", GetIUPRRateString(value.Message.Signals["PFCOMP2"].Value, value.Message.Signals["PFCOND2"].Value, supported));
            }
            return dicRet;
        }

        /// <summary>
        /// 返回的Dictionary的value值包含4个信号
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, Dictionary<string, double>> GetPID4F() {
            Dictionary<string, Dictionary<string, double>> dicRet = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<string, double> dicSignal = new Dictionary<string, double>();
            OBDParameter param = new OBDParameter();
            if (_OBDIf.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "22F44F";
                param.Service = 0x22;
                param.Parameter = 0xF44F;
            } else if (_OBDIf.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "014F";
                param.Service = 1;
                param.Parameter = 0x4F;
            } else if (_OBDIf.STDType == StandardType.SAE_J1939) {
                dicRet.Add(string.Empty, null);
                return dicRet;
            }
            param.ValueTypes = (int)OBDParameter.EnumValueTypes.Double;

            List<OBDParameterValue> valueList = _OBDIf.GetValueList(param);
            foreach (OBDParameterValue value in valueList) {
                if (value.ErrorDetected) {
                    continue;
                }
                if (_OBDDll.Mode01Support.ContainsKey(value.ECUResponseID) && _OBDDll.Mode01Support[value.ECUResponseID][(param.Parameter & 0x00FF) - 1]) {
                    dicSignal.Add("ScaleRatio", value.Message.Signals["ScaleRatio"].Value);
                    dicSignal.Add("ScaleVoltage", value.Message.Signals["ScaleVoltage"].Value);
                    dicSignal.Add("ScaleCurrent", value.Message.Signals["ScaleCurrent"].Value);
                    dicSignal.Add("ScalePressure", value.Message.Signals["ScalePressure"].Value);
                }
                dicRet.Add(value.ECUResponseID, dicSignal);
            }

            return dicRet;
        }

    }
}
