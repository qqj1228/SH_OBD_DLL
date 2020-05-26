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
                Console.WriteLine(GetPID0C(obd));
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        static string GetPID0C(SH_OBD_Main obd) {
            string strRet = "";
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
                            strRet = "ON";
                        } else {
                            strRet = "OFF";
                        }
                    } else if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.Double) != 0) {
                        strRet = value.DoubleValue.ToString();
                    } else if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.String) != 0) {
                        strRet = value.StringValue;
                    } else if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.ShortString) != 0) {
                        strRet = value.ShortStringValue;
                    }
                    break;
                }
            }
            return strRet;
        }
    }
}
