using SH_OBD_DLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dyno_OBD_DLL {
    public class XmlHandler {
        private delegate string XmlFunction(Request request);
        private readonly Dictionary<string, XmlFunction> _handlers;
        private readonly SH_OBD_Dll _OBDDll;
        private readonly OBDInterface _OBDIf;
        private readonly Logger _log;
        private readonly OBDTest _obdTest;
        private readonly Dictionary<string, DTC> _dicDTCDef;

        public XmlHandler(SH_OBD_Dll OBDDll, string logPath, Dictionary<string, DTC> dicDTCDef) {
            _OBDDll = OBDDll;
            _log = new Logger(logPath, EnumLogLevel.LogLevelAll, true, 100);
            _dicDTCDef = dicDTCDef;
            _OBDIf = _OBDDll.GetOBDInterface();
            _obdTest = new OBDTest(_OBDDll);
            _handlers = new Dictionary<string, XmlFunction>() {
                { "GetOBDState", GetOBDStateHandler },
                { "GetVCIInfo", GetVCIInfoHandler },
                { "StartTest", StartTestHandler },
                { "GetCarInfo", GetCarInfoHandler },
                { "GetOBDInfo", GetOBDInfoHandler },
                { "GetDTC", GetDTCHandler },
                { "GetReadiness", GetReadinessHandler },
                { "GetIUPR", GetIUPRHandler },
                { "GetRTData", GetRTDataHandler },
                { "GetSpeedInfo", GetSpeedInfoHandler },
                { "GetOilInfo", GetOilInfoHandler }
            };
        }

        public string HandleRequest(string requestXml) {
            string indentChars = string.Empty;
            _log.TraceInfo("Recv request:" + Environment.NewLine + requestXml);
            Request request = Utility.Deserializer<Request>(requestXml);
            string strResponse = string.Empty;
            if (request != null && request.Cmd != null) {
                if (_handlers.ContainsKey(request.Cmd)) {
                    strResponse = _handlers[request.Cmd](request);
                } else {
                    Response response = new Response {
                        Cmd = request.Cmd,
                        Code = 1,
                        Msg = "不支持的请求命令"
                    };
                    strResponse = Utility.XmlSerialize(response, ref indentChars);
                }
            } else {
                Response response = new Response {
                    Cmd = request.Cmd,
                    Code = 1,
                    Msg = "请求命令格式错误"
                };
                strResponse = Utility.XmlSerialize(response, ref indentChars);
            }
            _log.TraceInfo("Send response:" + Environment.NewLine + strResponse);
            return strResponse;
        }

        private string GetOBDStateHandler(Request request) {
            string indentChars = string.Empty;
            GetOBDStateResponse response = new GetOBDStateResponse {
                Cmd = request.Cmd
            };
            try {
                response.Code = 0;
                response.Msg = "成功";
                response.Data = new GetOBDStateResponse.CData {
                    State = _OBDIf.ConnectedStatus ? "已连接车辆OBD" : "未连接车辆OBD"
                };
            } catch (Exception ex) {
                response.Code = 1;
                response.Msg = "失败";
                response.Msg += ex.Message.Length > 0 ? ", " + ex.Message : string.Empty;
                _log.TraceError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return Utility.XmlSerialize(response, ref indentChars).Replace("GetOBDStateResponse", "Response");
        }

        private string GetVCIInfoHandler(Request request) {
            string indentChars = string.Empty;
            GetVCIInfoResponse response = new GetVCIInfoResponse {
                Cmd = request.Cmd
            };
            try {
                string strVCISWVer = _OBDIf.GetDeviceIDString();
                response.Code = 0;
                response.Msg = "成功";
                response.Data = new GetVCIInfoResponse.CData {
                    DllVersion = DllVersion<OBDDiagnosis>.AssemblyVersion + " - " + _OBDDll.GetDllVersion(),
                    VCIModel = _OBDIf.GetDeviceIDString()
                };
            } catch (Exception ex) {
                response.Code = 1;
                response.Msg = "失败";
                response.Msg += ex.Message.Length > 0 ? ", " + ex.Message : string.Empty;
                _log.TraceError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return Utility.XmlSerialize(response, ref indentChars).Replace("GetSoftwareVersionResponse", "Response");
        }

        private string StartTestHandler(Request request) {
            string indentChars = string.Empty;
            bool result = true;
            StartTestResponse response = new StartTestResponse() {
                Cmd = request.Cmd
            };
            try {
                if (!_OBDDll.SetSupportStatus(out string errMsg)) {
                    result = false;
                }
                if (result) {
                    response.Code = 0;
                    response.Msg = "成功";
                    response.Data = new StartTestResponse.CData {
                        Protocol = _OBDIf.GetProtocol().ToString(),
                        AppSTD = _OBDIf.GetStandard().ToString(),
                        FuelType = _obdTest.IsDiesel() ? "柴油" : "汽油"
                    };
                } else {
                    response.Code = 1;
                    response.Msg = "获取ECU支持状态失败";
                    response.Msg += errMsg.Length > 0 ? ", " + errMsg : string.Empty;
                }
            } catch (Exception ex) {
                response.Code = 1;
                response.Msg = "失败";
                response.Msg += ex.Message.Length > 0 ? ", " + ex.Message : string.Empty;
                _log.TraceError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return Utility.XmlSerialize(response, ref indentChars).Replace("StartTestResponse", "Response");
        }

        private string GetCarInfoHandler(Request request) {
            string indentChars = string.Empty;
            GetCarInfoResponse response = new GetCarInfoResponse() {
                Cmd = request.Cmd
            };
            try {
                Dictionary<string, string> dicVIN = _obdTest.GetVIN();
                Dictionary<string, string> dicOBDType = _obdTest.GetPID1C();
                Dictionary<string, double> dicODO = _obdTest.GetPIDDouble(0xA6, "ODO");
                Dictionary<string, string> dicMIL = _obdTest.GetMIL();
                Dictionary<string, double> dicMILDIST = _obdTest.GetPIDDouble(0x21, "MIL_DIST");

                string strECU = dicVIN.First().Key;
                string strVIN = dicVIN.ContainsKey(strECU) ? dicVIN[strECU] : string.Empty;
                string strOBDType = dicOBDType.ContainsKey(strECU) ? dicOBDType[strECU].Split(',').Last() : string.Empty;
                double dODO = dicODO.ContainsKey(strECU) ? dicODO[strECU] : 0;
                string strMIL = dicMIL.ContainsKey(strECU) ? dicMIL[strECU] : string.Empty;
                double dMILDIST = dicMILDIST.ContainsKey(strECU) ? dicMILDIST[strECU] : 0;

                response.Code = 0;
                response.Msg = "成功";
                response.Data = new GetCarInfoResponse.CData {
                    VIN = strVIN,
                    OBDType = strOBDType,
                    ODO = dODO.ToString(),
                    MIL = strMIL,
                    MIL_DIST = dMILDIST.ToString()
                };
            } catch (Exception ex) {
                response.Code = 1;
                response.Msg = "失败";
                response.Msg += ex.Message.Length > 0 ? ", " + ex.Message : string.Empty;
                _log.TraceError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return Utility.XmlSerialize(response, ref indentChars).Replace("GetCarInfoResponse", "Response");
        }

        private string GetOBDInfoHandler(Request request) {
            string indentChars = string.Empty;
            GetOBDInfoResponse response = new GetOBDInfoResponse() {
                Cmd = request.Cmd
            };
            try {
                Dictionary<string, string> dicCVN = _obdTest.GetCVN();
                Dictionary<string, string> dicCALID = _obdTest.GetCALID();
                List<string> ECUs = new List<string>();
                if (_OBDIf.STDType != StandardType.SAE_J1939) {
                    foreach (string item in _OBDDll.Mode09Support.Keys) {
                        ECUs.Add(item);
                    }
                } else {
                    // SAE J1939 只有 Mode01Support，没有 Mode09Support
                    foreach (string item in _OBDDll.Mode01Support.Keys) {
                        ECUs.Add(item);
                    }
                }

                string strECUID = string.Empty;
                string strSCRID = string.Empty;
                string strOtherID = string.Empty;
                string strECUCVN = string.Empty;
                string strSCRCVN = string.Empty;
                string strOtherCVN = string.Empty;
                string strECUCALID = string.Empty;
                string strSCRCALID = string.Empty;
                string strOtherCALID = string.Empty;
                if (ECUs.Count >= 1) {
                    strECUID = "0x" + ECUs[0];
                    strECUCVN = dicCVN[ECUs[0]];
                    strECUCALID = dicCALID[ECUs[0]];
                    if (ECUs.Count >= 2) {
                        strSCRID = "0x" + ECUs[1];
                        strSCRCVN = dicCVN.ContainsKey(ECUs[1]) ? dicCVN[ECUs[1]] : string.Empty;
                        strSCRCALID = dicCALID.ContainsKey(ECUs[1]) ? dicCALID[ECUs[1]] : string.Empty;
                        if (ECUs.Count >= 3) {
                            for (int i = 2; i < ECUs.Count; i++) {
                                strOtherID += "0x" + ECUs[i] + ",";
                                strOtherCVN += dicCVN.ContainsKey(ECUs[i]) ? dicCVN[ECUs[i]] + "," : string.Empty;
                                strOtherCALID += dicCALID.ContainsKey(ECUs[i]) ? dicCALID[ECUs[i]] + "," : string.Empty;
                            }
                            strOtherID = strOtherID.TrimEnd(',');
                            strOtherCVN = strOtherCVN.TrimEnd(',');
                            strOtherCALID = strOtherCALID.TrimEnd(',');
                        }
                    }
                }

                response.Code = 0;
                response.Msg = "成功";
                response.Data = new GetOBDInfoResponse.CData {
                    EngineCVN = strECUCVN,
                    EngineCALID = strECUCALID,
                    Post_ProcCVN = strSCRCVN,
                    Post_ProcCALID = strSCRCALID,
                    OtherCVN = strOtherCVN,
                    OtherCALID = strOtherCALID,
                    MODULEECUID = strECUID,
                    MODULESCRID = strSCRID,
                    MODULEOtherID = strOtherID
                };
            } catch (Exception ex) {
                response.Code = 1;
                response.Msg = "失败";
                response.Msg += ex.Message.Length > 0 ? ", " + ex.Message : string.Empty;
                _log.TraceError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return Utility.XmlSerialize(response, ref indentChars).Replace("GetOBDInfoResponse", "Response");
        }

        private string GetDTCHandler(Request request) {
            string indentChars = string.Empty;
            string strRet;
            GetDTCResponse response = new GetDTCResponse() {
                Cmd = request.Cmd
            };
            try {
                Dictionary<string, string> dicDTC03 = _obdTest.GetDTC03();
                Dictionary<string, string> dicDTC07 = _obdTest.GetDTC07();
                Dictionary<string, string> dicDTC0A = _obdTest.GetDTC0A();
                List<string> lsDTC03 = new List<string>();
                List<string> lsDTC07 = new List<string>();
                List<string> lsDTC0A = new List<string>();
                List<string> lsDTC03Desc = new List<string>();
                List<string> lsDTC07Desc = new List<string>();
                List<string> lsDTC0ADesc = new List<string>();
                int iConfirmed = 0;
                int iPending = 0;
                int iPermanent = 0;
                foreach (string item in dicDTC03.Keys) {
                    string[] DTC03s = dicDTC03[item].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    iConfirmed += DTC03s.Length;
                    for (int i = 0; i < DTC03s.Length; i++) {
                        lsDTC03.Add(DTC03s[i]);
                        if (_dicDTCDef.ContainsKey(DTC03s[i])) {
                            lsDTC03Desc.Add(_dicDTCDef[DTC03s[i]].Description);
                        } else {
                            lsDTC03Desc.Add("无 DTC 描述");
                        }
                    }
                }
                foreach (string item in dicDTC07.Keys) {
                    string[] DTC07s = dicDTC07[item].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    iPending += DTC07s.Length;
                    for (int i = 0; i < DTC07s.Length; i++) {
                        lsDTC07.Add(DTC07s[i]);
                        if (_dicDTCDef.ContainsKey(DTC07s[i])) {
                            lsDTC07Desc.Add(_dicDTCDef[DTC07s[i]].Description);
                        } else {
                            lsDTC07Desc.Add("无 DTC 描述");
                        }
                    }
                }
                foreach (string item in dicDTC0A.Keys) {
                    string[] DTC0As = dicDTC0A[item].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    iPermanent += DTC0As.Length;
                    for (int i = 0; i < DTC0As.Length; i++) {
                        lsDTC0A.Add(DTC0As[i]);
                        if (_dicDTCDef.ContainsKey(DTC0As[i])) {
                            lsDTC0ADesc.Add(_dicDTCDef[DTC0As[i]].Description);
                        } else {
                            lsDTC0ADesc.Add("无 DTC 描述");
                        }
                    }
                }

                response.Code = 0;
                response.Msg = "成功";
                response.Data = new GetDTCResponse.CData {
                    ConfirmedNUM = iConfirmed,
                    ConfirmedDTC = string.Empty,
                    PendingNUM = iPending,
                    PendingDTC = string.Empty,
                    PermanentNUM = iPermanent,
                    PermanentDTC = string.Empty,
                };
                strRet = Utility.XmlSerialize(response, ref indentChars).Replace("GetDTCResponse", "Response");
                indentChars += indentChars;

                string strDTC03 = string.Empty;
                string strDTC07 = string.Empty;
                string strDTC0A = string.Empty;
                for (int i = 0; i < lsDTC03.Count; i++) {
                    strDTC03 += string.Format("{2}<ConfirmedDTC{0}>{1}</ConfirmedDTC{0}>", i + 1, lsDTC03[i], indentChars) + Environment.NewLine;
                    if (lsDTC03Desc.Count > i) {
                        strDTC03 += string.Format("{2}<ConfirmedDesc{0}>{1}</ConfirmedDesc{0}>", i + 1, lsDTC03Desc[i], indentChars) + Environment.NewLine;
                    } else {
                        strDTC03 += string.Format("{2}<ConfirmedDesc{0}>{1}</ConfirmedDesc{0}>", i + 1, "无 DTC 描述", indentChars) + Environment.NewLine;
                    }
                }
                for (int i = 0; i < lsDTC07.Count; i++) {
                    strDTC07 += string.Format("{2}<PendingDTC{0}>{1}</PendingDTC{0}>", i + 1, lsDTC07[i], indentChars) + Environment.NewLine;
                    if (lsDTC07Desc.Count > i) {
                        strDTC07 += string.Format("{2}<PendingDesc{0}>{1}</PendingDesc{0}>", i + 1, lsDTC07Desc[i], indentChars) + Environment.NewLine;
                    } else {
                        strDTC07 += string.Format("{2}<PendingDesc{0}>{1}</PendingDesc{0}>", i + 1, "无 DTC 描述", indentChars) + Environment.NewLine;
                    }
                }
                for (int i = 0; i < lsDTC0A.Count; i++) {
                    strDTC0A += string.Format("{2}<PermanentDTC{0}>{1}</PermanentDTC{0}>", i + 1, lsDTC0A[i], indentChars) + Environment.NewLine;
                    if (lsDTC0ADesc.Count > i) {
                        strDTC0A += string.Format("{2}<PermanentDesc{0}>{1}</PermanentDesc{0}>", i + 1, lsDTC0ADesc[i], indentChars) + Environment.NewLine;
                    } else {
                        strDTC0A += string.Format("{2}<PermanentDesc{0}>{1}</PermanentDesc{0}>", i + 1, "无 DTC 描述", indentChars) + Environment.NewLine;
                    }
                }

                strRet = strRet.Replace(indentChars + "<ConfirmedDTC />" + Environment.NewLine, strDTC03);
                strRet = strRet.Replace(indentChars + "<PendingDTC />" + Environment.NewLine, strDTC07);
                strRet = strRet.Replace(indentChars + "<PermanentDTC />" + Environment.NewLine, strDTC0A);
            } catch (Exception ex) {
                response.Code = 1;
                response.Msg = "失败";
                response.Msg += ex.Message.Length > 0 ? ", " + ex.Message : string.Empty;
                _log.TraceError(ex.Message + Environment.NewLine + ex.StackTrace);
                strRet = Utility.XmlSerialize(response, ref indentChars).Replace("GetDTCResponse", "Response");
            }
            return strRet;
        }

        private string GetReadinessHandler(Request request) {
            string indentChars = string.Empty;
            GetReadinessResponse response = new GetReadinessResponse() {
                Cmd = request.Cmd
            };
            try {
                string strReadiness = string.Empty;
                Dictionary<string, string> dicReadiness = _obdTest.GetReadiness();
                response.Code = 0;
                response.Msg = "成功";
                if (!dicReadiness.Keys.Contains(string.Empty)) {
                    response.Data = new GetReadinessResponse.CData {
                        MIS_RDY = dicReadiness["MIS_RDY"],
                        FUEL_RDY = dicReadiness["FUEL_RDY"],
                        CCM_RDY = dicReadiness["CCM_RDY"]
                    };
                    if (_OBDIf.STDType == StandardType.SAE_J1939) {
                        response.Data.CAT_RDY = dicReadiness["CAT_RDY"];
                        response.Data.HCAT_RDY = dicReadiness["HCAT_RDY"];
                        response.Data.EVAP_RDY = dicReadiness["EVAP_RDY"];
                        response.Data.AIR_RDY = dicReadiness["AIR_RDY"];
                        response.Data.O2S_RDY = dicReadiness["O2S_RDY"];
                        response.Data.HTR_RDY = dicReadiness["HTR_RDY"];
                        response.Data.EGR_RDY = dicReadiness["EGR_RDY_spark"];
                        response.Data.HCCATRDY = dicReadiness["HCCATRDY"];
                        response.Data.NCAT_RDY = dicReadiness["NCAT_RDY"];
                        response.Data.DPF_RDY = dicReadiness["DPF_RDY"];
                        response.Data.BP_RDY = dicReadiness["BP_RDY"];
                        response.Data.CSAS_RDY = dicReadiness["CSAS_RDY"];
                    } else {
                        if (dicReadiness.ContainsKey("EGR_RDY_compression")) {
                            response.Data.HCCATRDY = dicReadiness["HCCATRDY"];
                            response.Data.NCAT_RDY = dicReadiness["NCAT_RDY"];
                            response.Data.BP_RDY = dicReadiness["BP_RDY"];
                            response.Data.EGS_RDY = dicReadiness["EGS_RDY"];
                            response.Data.PM_RDY = dicReadiness["PM_RDY"];
                            response.Data.EGR_RDY = dicReadiness["EGR_RDY_compression"];
                        } else {
                            response.Data.CAT_RDY = dicReadiness["CAT_RDY"];
                            response.Data.HCAT_RDY = dicReadiness["HCAT_RDY"];
                            response.Data.EVAP_RDY = dicReadiness["EVAP_RDY"];
                            response.Data.AIR_RDY = dicReadiness["AIR_RDY"];
                            response.Data.O2S_RDY = dicReadiness["O2S_RDY"];
                            response.Data.HTR_RDY = dicReadiness["HTR_RDY"];
                            response.Data.EGR_RDY = dicReadiness["EGR_RDY_spark"];
                        }
                    }
                }
            } catch (Exception ex) {
                response.Code = 1;
                response.Msg = "失败";
                response.Msg += ex.Message.Length > 0 ? ", " + ex.Message : string.Empty;
                _log.TraceError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return Utility.XmlSerialize(response, ref indentChars).Replace("GetReadinessResponse", "Response");
        }

        private string GetIUPRHandler(Request request) {
            string strRet = string.Empty;
            string indentChars = string.Empty;
            GetIUPRResponse response = new GetIUPRResponse() {
                Cmd = request.Cmd
            };
            try {
                bool bDiesel = _obdTest.IsDiesel();
                Dictionary<string, string> dicIUPR = _obdTest.GetIUPR(bDiesel);

                response.Code = 0;
                response.Msg = "成功";
                if (!dicIUPR.Keys.Contains(string.Empty)) {
                    response.Data = new GetIUPRResponse.CData {
                        OBDCOND = dicIUPR["OBDCOND"],
                        IGNCNTR = dicIUPR["IGNCNTR"],
                    };
                    if (_OBDIf.STDType == StandardType.SAE_J1939) {
                        response.Data.J1939MPR = string.Empty;
                        strRet = Utility.XmlSerialize(response, ref indentChars).Replace("GetIUPRResponse", "Response");
                        indentChars += indentChars;
                        string strMPRData = string.Empty;
                        for (int i = 1; dicIUPR.Keys.Contains("SPN" + i.ToString()); i++) {
                            strMPRData += string.Format("{2}<SPN{0}>{1}</SPN{0}>", i, dicIUPR["SPN" + i.ToString()], indentChars) + Environment.NewLine;
                            strMPRData += string.Format("{2}<COMP{0}>{1}</COMP{0}>", i, dicIUPR["NUM" + i.ToString()], indentChars) + Environment.NewLine;
                            strMPRData += string.Format("{2}<COND{0}>{1}</COND{0}>", i, dicIUPR["DEN" + i.ToString()], indentChars) + Environment.NewLine;
                            strMPRData += string.Format("{2}<MPR{0}>{1}</MPR{0}>", i, dicIUPR["MPR" + i.ToString()], indentChars) + Environment.NewLine;
                        }
                        strRet = strRet.Replace(indentChars + "<J1939MPR />" + Environment.NewLine, strMPRData);
                        return strRet;
                    } else if (bDiesel) {
                        response.Data.HCCATCOMP = dicIUPR["HCCATCOMP"];
                        response.Data.HCCATCOND = dicIUPR["HCCATCOND"];
                        response.Data.HCCATIUPR = dicIUPR["HCCATIUPR"];
                        response.Data.NCATCOMP = dicIUPR["NCATCOMP"];
                        response.Data.NCATCOND = dicIUPR["NCATCOND"];
                        response.Data.NCATIUPR = dicIUPR["NCATIUPR"];
                        response.Data.NADSCOMP = dicIUPR["NADSCOMP"];
                        response.Data.NADSCOND = dicIUPR["NADSCOND"];
                        response.Data.NADSIUPR = dicIUPR["NADSIUPR"];
                        response.Data.PMCOMP = dicIUPR["PMCOMP"];
                        response.Data.PMCOND = dicIUPR["PMCOND"];
                        response.Data.PMIUPR = dicIUPR["PMIUPR"];
                        response.Data.EGSCOMP = dicIUPR["EGSCOMP"];
                        response.Data.EGSCOND = dicIUPR["EGSCOND"];
                        response.Data.EGSIUPR = dicIUPR["EGSIUPR"];
                        response.Data.EGRCOMP_compression = dicIUPR["EGRCOMP_compression"];
                        response.Data.EGRCOND_compression = dicIUPR["EGRCOND_compression"];
                        response.Data.EGRIUPR_compression = dicIUPR["EGRIUPR_compression"];
                        response.Data.BPCOMP = dicIUPR["BPCOMP"];
                        response.Data.BPCOND = dicIUPR["BPCOND"];
                        response.Data.BPIUPR = dicIUPR["BPIUPR"];
                        response.Data.FUELCOMP = dicIUPR["FUELCOMP"];
                        response.Data.FUELCOND = dicIUPR["FUELCOND"];
                        response.Data.FUELIUPR = dicIUPR["FUELIUPR"];
                    } else {
                        response.Data.CATCOMP1 = dicIUPR["CATCOMP1"];
                        response.Data.CATCOND1 = dicIUPR["CATCOND1"];
                        response.Data.CATIUPR1 = dicIUPR["CATIUPR1"];
                        response.Data.CATCOMP2 = dicIUPR["CATCOMP2"];
                        response.Data.CATCOND2 = dicIUPR["CATCOND2"];
                        response.Data.CATIUPR2 = dicIUPR["CATIUPR2"];
                        response.Data.O2SCOMP1 = dicIUPR["O2SCOMP1"];
                        response.Data.O2SCOND1 = dicIUPR["O2SCOND1"];
                        response.Data.O2SIUPR1 = dicIUPR["O2SIUPR1"];
                        response.Data.O2SCOMP2 = dicIUPR["O2SCOMP2"];
                        response.Data.O2SCOND2 = dicIUPR["O2SCOND2"];
                        response.Data.O2SIUPR2 = dicIUPR["O2SIUPR2"];
                        response.Data.EGRCOMP_spark = dicIUPR["EGRCOMP_spark"];
                        response.Data.EGRCOND_spark = dicIUPR["EGRCOND_spark"];
                        response.Data.EGRIUPR_spark = dicIUPR["EGRIUPR_spark"];
                        response.Data.AIRCOMP = dicIUPR["AIRCOMP"];
                        response.Data.AIRCOND = dicIUPR["AIRCOND"];
                        response.Data.AIRIUPR = dicIUPR["AIRIUPR"];
                        response.Data.EVAPCOMP = dicIUPR["EVAPCOMP"];
                        response.Data.EVAPCOND = dicIUPR["EVAPCOND"];
                        response.Data.EVAPIUPR = dicIUPR["EVAPIUPR"];
                        response.Data.SO2SCOMP1 = dicIUPR["SO2SCOMP1"];
                        response.Data.SO2SCOND1 = dicIUPR["SO2SCOND1"];
                        response.Data.SO2SIUPR1 = dicIUPR["SO2SIUPR1"];
                        response.Data.SO2SCOMP2 = dicIUPR["SO2SCOMP2"];
                        response.Data.SO2SCOND2 = dicIUPR["SO2SCOND2"];
                        response.Data.SO2SIUPR2 = dicIUPR["SO2SIUPR2"];
                        response.Data.AFRICOMP1 = dicIUPR["AFRICOMP1"];
                        response.Data.AFRICOND1 = dicIUPR["AFRICOND1"];
                        response.Data.AFRIIUPR1 = dicIUPR["AFRIIUPR1"];
                        response.Data.AFRICOMP2 = dicIUPR["AFRICOMP2"];
                        response.Data.AFRICOND2 = dicIUPR["AFRICOND2"];
                        response.Data.AFRIIUPR2 = dicIUPR["AFRIIUPR2"];
                        response.Data.PFCOMP1 = dicIUPR["PFCOMP1"];
                        response.Data.PFCOND1 = dicIUPR["PFCOND1"];
                        response.Data.PFIUPR1 = dicIUPR["PFIUPR1"];
                        response.Data.PFCOMP2 = dicIUPR["PFCOMP2"];
                        response.Data.PFCOND2 = dicIUPR["PFCOND2"];
                        response.Data.PFIUPR2 = dicIUPR["PFIUPR2"];
                    }
                }
            } catch (Exception ex) {
                response.Code = 1;
                response.Msg = "失败";
                response.Msg += ex.Message.Length > 0 ? ", " + ex.Message : string.Empty;
                _log.TraceError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            strRet = Utility.XmlSerialize(response, ref indentChars).Replace("GetIUPRResponse", "Response");
            strRet = strRet.Replace("_spark", string.Empty).Replace("_compression", string.Empty);
            return strRet;
        }

        private string GetRTDataHandler(Request request) {
            string indentChars = string.Empty;
            GetRTDataResponse response = new GetRTDataResponse() {
                Cmd = request.Cmd
            };
            try {
                bool bDiesel = _obdTest.IsDiesel();
                Dictionary<string, double> dicVSS = _obdTest.GetPIDDouble(0x0D, "VSS");
                Dictionary<string, double> dicRPM = _obdTest.GetPIDDouble(0x0C, "RPM");
                Dictionary<string, double> dicECT = _obdTest.GetPIDDouble(0x05, "ECT");
                Dictionary<string, double> dicEOT = _obdTest.GetPIDDouble(0x5C, "EOT");
                Dictionary<string, double> dicTPR = _obdTest.GetPIDDouble(0x45, "TP_R");
                string strECU = dicVSS.First().Key;
                double dVSS = dicVSS.ContainsKey(strECU) ? dicVSS[strECU] : 0;
                double dRPM = dicRPM.ContainsKey(strECU) ? dicRPM[strECU] : 0;
                double dECT = dicECT.ContainsKey(strECU) ? dicECT[strECU] : 0;
                double dEOT = dicEOT.ContainsKey(strECU) ? dicEOT[strECU] : 0;
                double dTPR = dicTPR.ContainsKey(strECU) ? dicTPR[strECU] : 0;

                Dictionary<string, double> dicMAF = _obdTest.GetPIDDouble(0x10, "MAF");
                Dictionary<string, double> dicScaleAirFlowRatio = _obdTest.GetPIDDouble(0x50, "ScaleAirFlowRatio");
                Dictionary<string, double> dicMAP = _obdTest.GetPIDDouble(0x0B, "MAP");
                Dictionary<string, Dictionary<string, double>> dicPID4Fs = _obdTest.GetPID4F();
                Dictionary<string, double> dicPID4F = dicPID4Fs.ContainsKey(strECU) ? dicPID4Fs[strECU] : null;
                double dMAF = dicMAF.ContainsKey(strECU) ? dicMAF[strECU] : 0;
                double dScaleAirFlowRatio = dicScaleAirFlowRatio.ContainsKey(strECU) ? dicScaleAirFlowRatio[strECU] : 0;
                if (dScaleAirFlowRatio > 0) {
                    dMAF *= dScaleAirFlowRatio * 10 / 65535;
                }
                double dMAP = dicMAP.ContainsKey(strECU) ? dicMAP[strECU] : 0;
                double dScalePressure = dicPID4F != null ? dicPID4F["ScalePressure"] : 0;
                if (dScalePressure > 0) {
                    dMAP *= dScalePressure * 10 / 255;
                }

                response.Code = 0;
                response.Msg = "成功";
                response.Data = new GetRTDataResponse.CData {
                    VSS = dVSS.ToString(),
                    RPM = dRPM.ToString(),
                    ECT = dECT.ToString(),
                    EOT = dEOT.ToString(),
                    TP_R = dTPR.ToString(),
                    MAF = dMAF.ToString(),
                    MAP = dMAP.ToString(),
                };

                if (bDiesel) {
                    Dictionary<string, double> dicTQACT = _obdTest.GetPIDDouble(0x62, "TQ_ACT");
                    Dictionary<string, double> dicTQREF = _obdTest.GetPIDDouble(0x63, "TQ_REF");
                    double dTQACT = dicTQACT.ContainsKey(strECU) ? dicTQACT[strECU] : 0;
                    double dTQREF = dicTQREF.ContainsKey(strECU) ? dicTQREF[strECU] : 0;
                    double dTQ = dTQACT * dTQREF;
                    double dPOWER = dTQ * dRPM / 9550;

                    Dictionary<string, double> dicEGRPCT = _obdTest.GetPIDDouble(0x2C, "EGR_PCT");
                    Dictionary<string, double> dicEGRERR = _obdTest.GetPIDDouble(0x2D, "EGR_ERR");
                    double dEGRPCT = dicEGRPCT.ContainsKey(strECU) ? dicEGRPCT[strECU] : 0;
                    double dEGRERR = dicEGRERR.ContainsKey(strECU) ? dicEGRERR[strECU] : 0;
                    dEGRPCT /= 100; // 从百分比化为实数
                    dEGRERR /= 100; // 从百分比化为实数
                    double dEGRACT = dEGRPCT * (1 + dEGRERR);
                    dEGRACT *= 100;  // 从实数化为百分比

                    Dictionary<string, double> dicBPAACT = _obdTest.GetPIDDouble(0x70, "BP_A_ACT");
                    Dictionary<string, double> dicBPBACT = _obdTest.GetPIDDouble(0x70, "BP_B_ACT");
                    double dBPAACT = dicBPAACT.ContainsKey(strECU) ? dicBPAACT[strECU] : 0;
                    double dBPBACT = dicBPBACT.ContainsKey(strECU) ? dicBPBACT[strECU] : 0;

                    Dictionary<string, double> dicFUELRATE = _obdTest.GetPIDDouble(0x5E, "FUEL_RATE");
                    double dFUELRATE = dicFUELRATE.ContainsKey(strECU) ? dicFUELRATE[strECU] : 0;
                    double dFUELCONSUM = dFUELRATE / (100 * dVSS); // 单位 L/100km

                    Dictionary<string, double> dicNOXC = _obdTest.GetPIDDouble(0xA1, "NOXC11");
                    Dictionary<string, double> dicEGT = _obdTest.GetPIDDouble(0x78, "EGT11");
                    Dictionary<string, double> dicDPFDP = _obdTest.GetPIDDouble(0x7A, "DPF1_DP");
                    double dNOXC = dicNOXC.ContainsKey(strECU) ? dicNOXC[strECU] : 0;
                    double dEGT = dicEGT.ContainsKey(strECU) ? dicEGT[strECU] : 0;
                    double dDPFDP = dicDPFDP.ContainsKey(strECU) ? dicDPFDP[strECU] : 0;

                    Dictionary<string, double> dicREAGRATE = _obdTest.GetPIDDouble(0x85, "REAG_RATE");
                    double dREAGRATE = dicREAGRATE.ContainsKey(strECU) ? dicREAGRATE[strECU] : 0;
                    dREAGRATE = dREAGRATE * 1000 / 3600; // 从 L/h 化为 ml/s

                    Dictionary<string, double> dicFRPG = _obdTest.GetPIDDouble(0x23, "FRP");
                    double dFRPG = dicFRPG.ContainsKey(strECU) ? dicFRPG[strECU] : 0;
                    dFRPG /= 100; // 从 kPa 化为 bar

                    response.Data.ENG_POWER = dPOWER.ToString();
                    response.Data.EGR_ACT = dEGRACT.ToString();
                    response.Data.BPA_ACT = dBPAACT.ToString();
                    response.Data.BPB_ACT = dBPBACT.ToString();
                    response.Data.FUEL_CONSUM = dFUELRATE.ToString();
                    response.Data.NOXC = dNOXC.ToString();
                    response.Data.EGT = dEGT.ToString();
                    response.Data.DPF_DP = dDPFDP.ToString();
                    response.Data.REAG_RATE = dREAGRATE.ToString();
                    response.Data.FRP_G = dFRPG.ToString();
                } else {
                    Dictionary<string, double> dicLOADPCT = _obdTest.GetPIDDouble(0x04, "LOAD_PCT");
                    Dictionary<string, double> dicLAMBDA = _obdTest.GetPIDDouble(0x44, "LAMBDA");
                    double dLOADPCT = dicLOADPCT.ContainsKey(strECU) ? dicLOADPCT[strECU] : 0;
                    double dLAMBDA = dicLAMBDA.ContainsKey(strECU) ? dicLAMBDA[strECU] : 0;

                    Dictionary<string, double> dicSVPOS = _obdTest.GetPIDDouble(0x24, "O2Sxy");
                    Dictionary<string, double> dicSCPOS = _obdTest.GetPIDDouble(0x34, "O2Sxy");
                    double dSVPOS = dicSVPOS.ContainsKey(strECU) ? dicSVPOS[strECU] : 0;
                    double dScaleVoltage = dicPID4F != null ? dicPID4F["ScaleVoltage"] : 0;
                    if (dScaleVoltage > 0) {
                        dSVPOS *= dScaleVoltage / 65535;
                    }
                    dSVPOS *= 1000; // 单位从 V 化为 mV
                    double dSCPOS = dicSCPOS.ContainsKey(strECU) ? dicSCPOS[strECU] : 0;
                    double dScaleCurrent = dicPID4F != null ? dicPID4F["ScaleCurrent"] : 0;
                    if (dScaleCurrent > 0) {
                        dSCPOS *= dScaleCurrent / 32768;
                    }

                    response.Data.LOAD_PCT = dLOADPCT.ToString();
                    response.Data.LAMBDA = dLAMBDA.ToString();
                    response.Data.SVPOS = dSVPOS.ToString();
                    response.Data.SCPOS = dSCPOS.ToString();
                }
            } catch (Exception ex) {
                response.Code = 1;
                response.Msg = "失败";
                response.Msg += ex.Message.Length > 0 ? ", " + ex.Message : string.Empty;
                _log.TraceError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return Utility.XmlSerialize(response, ref indentChars).Replace("GetRTDataResponse", "Response");
        }

        private string GetSpeedInfoHandler(Request request) {
            string indentChars = string.Empty;
            GetSpeedInfoResponse response = new GetSpeedInfoResponse() {
                Cmd = request.Cmd
            };
            try {
                bool bDiesel = _obdTest.IsDiesel();
                Dictionary<string, double> dicVSS = _obdTest.GetPIDDouble(0x0D, "VSS");
                Dictionary<string, double> dicRPM = _obdTest.GetPIDDouble(0x0C, "RPM");
                string strECU = dicVSS.First().Key;
                double dVSS = dicVSS.ContainsKey(strECU) ? dicVSS[strECU] : 0;
                double dRPM = dicRPM.ContainsKey(strECU) ? dicRPM[strECU] : 0;

                response.Code = 0;
                response.Msg = "成功";
                response.Data = new GetSpeedInfoResponse.CData {
                    VSS = dVSS.ToString(),
                    RPM = dRPM.ToString(),
                };
            } catch (Exception ex) {
                response.Code = 1;
                response.Msg = "失败";
                response.Msg += ex.Message.Length > 0 ? ", " + ex.Message : string.Empty;
                _log.TraceError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return Utility.XmlSerialize(response, ref indentChars).Replace("GetSpeedInfoResponse", "Response");
        }

        private string GetOilInfoHandler(Request request) {
            string indentChars = string.Empty;
            GetOilInfoResponse response = new GetOilInfoResponse() {
                Cmd = request.Cmd
            };
            try {
                bool bDiesel = _obdTest.IsDiesel();
                Dictionary<string, double> dicECT = _obdTest.GetPIDDouble(0x05, "ECT");
                Dictionary<string, double> dicEOT = _obdTest.GetPIDDouble(0x5C, "EOT");
                string strECU = dicECT.First().Key;
                double dECT = dicECT.ContainsKey(strECU) ? dicECT[strECU] : 0;
                double dEOT = dicEOT.ContainsKey(strECU) ? dicEOT[strECU] : 0;

                response.Code = 0;
                response.Msg = "成功";
                response.Data = new GetOilInfoResponse.CData {
                    ECT = dECT.ToString(),
                    EOT = dEOT.ToString(),
                };
            } catch (Exception ex) {
                response.Code = 1;
                response.Msg = "失败";
                response.Msg += ex.Message.Length > 0 ? ", " + ex.Message : string.Empty;
                _log.TraceError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return Utility.XmlSerialize(response, ref indentChars).Replace("GetOilInfoResponse", "Response");
        }

    }
}
