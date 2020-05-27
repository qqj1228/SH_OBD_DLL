using SH_OBD;
using SH_OBD_DLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test {
    class Program {
        static void Main(string[] args) {
            SH_OBD_Main obd = new SH_OBD_Main();
            while (true) {
                if (!obd.ConnectOBD()) {
                    Console.WriteLine("ConnectOBD() failed");
                    Console.Read();
                    return;
                }
                if (!obd.SetSupportStatus()) {
                    Console.WriteLine("SetSupportStatus() failed");
                    Console.Read();
                    return;
                }
                Dictionary<string, string> PID0C = GetPID0C(obd);
                foreach (string key in PID0C.Keys) {
                    Console.WriteLine(string.Format("PRM: {0}/{1}", key, PID0C[key]));
                }
                Dictionary<string, string> CVN = GetCVN(obd);
                foreach (string key in CVN.Keys) {
                    Console.WriteLine(string.Format("CVN: {0}/{1}", key, CVN[key]));
                }

                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        static Dictionary<string, string> GetPID0C(SH_OBD_Main obd) {
            Dictionary<string, string> dicRet = new Dictionary<string, string>();
            OBDParameter param = new OBDParameter();
            if (obd.OBDif.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "22F40C";
                param.Service = 0x22;
                param.Parameter = 0xF40C;
                param.SubParameter = 0;
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.Double;
            } else if (obd.OBDif.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "010C";
                param.Service = 1;
                param.Parameter = 0x0C;
                param.SubParameter = 0;
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.Double;
            }

            List<OBDParameterValue> valueList = obd.OBDif.GetValueList(param);
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

        static Dictionary<string, string> GetCVN(SH_OBD_Main obd) {
            Dictionary<string, string> dicRet = new Dictionary<string, string>();
            OBDParameter param = new OBDParameter();
            if (obd.OBDif.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "22F806";
                param.Service = 0x22;
                param.Parameter = 0xF806;
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
            } else if (obd.OBDif.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "0906";
                param.Service = 9;
                param.Parameter = 6;
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
            } else if (obd.OBDif.STDType == StandardType.SAE_J1939) {
                param.OBDRequest = "00D300";
                param.Service = 0;
                param.Parameter = 0xD300;
                param.SubParameter = 0;
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
                obd.OBDif.SetTimeout(1000);
            }

            List<OBDParameterValue> valueList = obd.OBDif.GetValueList(param);
            foreach (OBDParameterValue value in valueList) {
                if (value.ErrorDetected) {
                    continue;
                }
                if (obd.Mode09Support.ContainsKey(value.ECUResponseID) && obd.Mode09Support[value.ECUResponseID][(param.Parameter & 0x00FF) - 1]) {
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
