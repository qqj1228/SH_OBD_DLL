﻿using SH_OBD_DLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test {
    class Program {
        static void Main(string[] args) {
            SH_OBD_Dll obd = new SH_OBD_Dll(".\\log\\OBD");
            OBDInterface OBDIf = obd.GetOBDInterface();
            if (!OBDIf.DllSettingsResult) {
                OBDIf.SaveDllSettings(OBDIf.DllSettings);
            }
            while (true) {
                if (OBDIf.DllSettings.ComPort <= 0 && !obd.TestTCP()) {
                    Console.WriteLine("Connect remote TCP server error!");
                    continue;
                }
                if (!obd.ConnectOBD()) {
                    Console.WriteLine("ConnectOBD() failed");
                    Console.Read();
                    return;
                }
                if (!obd.SetSupportStatus(out string errMsg)) {
                    Console.WriteLine("SetSupportStatus() failed, " + errMsg);
                    Console.Read();
                    return;
                }
                Dictionary<string, string> PID0C = GetPID0C(obd);
                foreach (string key in PID0C.Keys) {
                    Console.WriteLine(string.Format("PRM: {0}/{1}", key, PID0C[key]));
                }
                Dictionary<string, string> VIN = GetVIN(obd);
                foreach (string key in VIN.Keys) {
                    Console.WriteLine(string.Format("VIN: {0}/{1}", key, VIN[key]));
                }
                Dictionary<string, string> CVN = GetCVN(obd);
                foreach (string key in CVN.Keys) {
                    Console.WriteLine(string.Format("CVN: {0}/{1}", key, CVN[key]));
                }
                Dictionary<string, string> ECUNAME = GetECUNAME(obd);
                foreach (string key in ECUNAME.Keys) {
                    Console.WriteLine(string.Format("ECUNAME: {0}/{1}", key, ECUNAME[key]));
                }
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        static Dictionary<string, string> GetPID0C(SH_OBD_Dll obd) {
            OBDInterface OBDIf = obd.GetOBDInterface();
            Dictionary<string, string> dicRet = new Dictionary<string, string>();
            OBDParameter param = new OBDParameter();
            if (OBDIf.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "22F40C";
                param.Service = 0x22;
                param.Parameter = 0xF40C;
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.Double;
            } else if (OBDIf.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "010C";
                param.Service = 1;
                param.Parameter = 0x0C;
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.Double;
            } else if (OBDIf.STDType == StandardType.SAE_J1939) {
                return dicRet;
            }

            List<OBDParameterValue> valueList = OBDIf.GetValueList(param);
            foreach (OBDParameterValue value in valueList) {
                if (value.ErrorDetected) {
                    continue;
                }
                if (obd.Mode01Support.ContainsKey(value.ECUResponseID) && obd.Mode01Support[value.ECUResponseID][(param.Parameter & 0x00FF) - 1]) {
                    if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.Bool) != 0) {
                        if (value.BoolValue) {
                            dicRet.Add(value.ECUResponseID, "ON");
                        } else {
                            dicRet.Add(value.ECUResponseID, "OFF");
                        }
                    } else if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.Double) != 0) {
                        dicRet.Add(value.ECUResponseID, value.DoubleValue.ToString());
                    } else if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.String) != 0) {
                        dicRet.Add(value.ECUResponseID, value.StringValue);
                    } else if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.ShortString) != 0) {
                        dicRet.Add(value.ECUResponseID, value.ShortStringValue);
                    }
                }
            }

            return dicRet;
        }

        static Dictionary<string, string> GetVIN(SH_OBD_Dll obd) {
            OBDInterface OBDIf = obd.GetOBDInterface();
            Dictionary<string, string> dicRet = new Dictionary<string, string>();
            OBDParameter param = new OBDParameter();
            if (OBDIf.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "22F802";
                param.Service = 0x22;
                param.Parameter = 0xF802;
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
            } else if (OBDIf.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "0902";
                param.Service = 9;
                param.Parameter = 2;
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
            } else if (OBDIf.STDType == StandardType.SAE_J1939) {
                param.OBDRequest = "00FEEC";
                param.Service = 0;
                param.Parameter = 0xFEEC;
                param.SignalName = "VIN";
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
                OBDIf.SetTimeout(1000);
            }

            List<OBDParameterValue> valueList = OBDIf.GetValueList(param);
            foreach (OBDParameterValue value in valueList) {
                if (value.ErrorDetected) {
                    continue;
                }
                bool flag = obd.Mode09Support.ContainsKey(value.ECUResponseID) && obd.Mode09Support[value.ECUResponseID][(param.Parameter & 0x00FF) - 1];
                flag = flag || OBDIf.STDType == StandardType.SAE_J1939;
                if (flag) {
                    if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.ListString) != 0) {
                        if (value.ListStringValue.Count == 0 || value.ListStringValue[0].Length == 0) {
                            dicRet.Add(value.ECUResponseID, "");
                        } else {
                            string strVal = value.ListStringValue[0];
                            for (int i = 1; i < value.ListStringValue.Count; i++) {
                                strVal += "," + value.ListStringValue[i];
                            }
                            dicRet.Add(value.ECUResponseID, strVal);
                        }
                    }
                }
            }

            return dicRet;
        }

        static Dictionary<string, string> GetCVN(SH_OBD_Dll obd) {
            OBDInterface OBDIf = obd.GetOBDInterface();
            Dictionary<string, string> dicRet = new Dictionary<string, string>();
            OBDParameter param = new OBDParameter();
            if (OBDIf.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "22F806";
                param.Service = 0x22;
                param.Parameter = 0xF806;
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
            } else if (OBDIf.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "0906";
                param.Service = 9;
                param.Parameter = 6;
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
            } else if (OBDIf.STDType == StandardType.SAE_J1939) {
                param.OBDRequest = "00D300";
                param.Service = 0;
                param.Parameter = 0xD300;
                param.SignalName = "CVN";
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
                OBDIf.SetTimeout(1000);
            }

            List<OBDParameterValue> valueList = OBDIf.GetValueList(param);
            foreach (OBDParameterValue value in valueList) {
                if (value.ErrorDetected) {
                    continue;
                }
                bool flag = obd.Mode09Support.ContainsKey(value.ECUResponseID) && obd.Mode09Support[value.ECUResponseID][(param.Parameter & 0x00FF) - 1];
                flag = flag || OBDIf.STDType == StandardType.SAE_J1939;
                if (flag) {
                    if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.ListString) != 0) {
                        if (value.ListStringValue.Count == 0 || value.ListStringValue[0].Length == 0) {
                            dicRet.Add(value.ECUResponseID, "");
                        } else {
                            string strVal = value.ListStringValue[0];
                            for (int i = 1; i < value.ListStringValue.Count; i++) {
                                strVal += "," + value.ListStringValue[i];
                            }
                            dicRet.Add(value.ECUResponseID, strVal);
                        }
                    }
                }
            }

            return dicRet;
        }

        static Dictionary<string, string> GetECUNAME(SH_OBD_Dll obd) {
            OBDInterface OBDIf = obd.GetOBDInterface();
            Dictionary<string, string> dicRet = new Dictionary<string, string>();
            OBDParameter param = new OBDParameter();
            if (OBDIf.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "22F80A";
                param.Service = 0x22;
                param.Parameter = 0xF80A;
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
            } else if (OBDIf.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "090A";
                param.Service = 9;
                param.Parameter = 0xA;
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
            }

            List<OBDParameterValue> valueList = OBDIf.GetValueList(param);
            foreach (OBDParameterValue value in valueList) {
                if (value.ErrorDetected) {
                    continue;
                }
                bool flag = obd.Mode09Support.ContainsKey(value.ECUResponseID) && obd.Mode09Support[value.ECUResponseID][(param.Parameter & 0x00FF) - 1];
                if (flag) {
                    if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.ListString) != 0) {
                        if (value.ListStringValue.Count == 0 || value.ListStringValue[0].Length == 0) {
                            dicRet.Add(value.ECUResponseID, "");
                        } else {
                            string strVal = value.ListStringValue[0];
                            for (int i = 1; i < value.ListStringValue.Count; i++) {
                                strVal += "," + value.ListStringValue[i];
                            }
                            dicRet.Add(value.ECUResponseID, strVal);
                        }
                    }
                }
            }

            return dicRet;
        }

    }
}
