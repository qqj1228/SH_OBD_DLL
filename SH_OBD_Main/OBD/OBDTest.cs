using OfficeOpenXml;
using SH_OBD_DLL;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SH_OBD_Main {
    public class OBDTest {
        private readonly OBDIfEx _obdIfEx;
        private readonly DataTable _dtInfo;
        private readonly DataTable _dtECUInfo;
        private readonly DataTable _dtIUPR;
        private bool _compIgn;
        private bool _CN6;
        public event Action OBDTestStart;
        public event Action SetupColumnsDone;
        public event Action WriteDbStart;
        public event Action WriteDbDone;
        public event Action UploadDataStart;
        public event Action UploadDataDone;
        public event Action NotUploadData;
        public event EventHandler<SetDataTableColumnsErrorEventArgs> SetDataTableColumnsError;

        public ModelSQLServer DbNative { get; set; }
        public bool AdvanceMode { get; set; }
        public int AccessAdvanceMode { get; set; }
        public bool OBDResult { get; set; }
        public bool DTCResult { get; set; }
        public bool ReadinessResult { get; set; }
        public bool VINResult { get; set; }
        public bool CALIDCVNResult { get; set; }
        public bool CALIDCVNAllEmpty { get; set; }
        public bool CALIDUnmeaningResult { get; set; }
        public bool OBDSUPResult { get; set; }
        public string StrVIN_IN { get; set; }
        public string StrVIN_ECU { get; set; }

        public OBDTest(OBDIfEx obdIfex) {
            _obdIfEx = obdIfex;
            _dtInfo = new DataTable("Info");
            _dtECUInfo = new DataTable("ECUInfo");
            _dtIUPR = new DataTable("IUPR");
            _compIgn = false;
            _CN6 = false;
            AdvanceMode = false;
            AccessAdvanceMode = 0;
            OBDResult = false;
            DTCResult = true;
            ReadinessResult = true;
            VINResult = true;
            CALIDCVNResult = true;
            CALIDCVNAllEmpty = false;
            CALIDUnmeaningResult = true;
            OBDSUPResult = true;
            DbNative = new ModelSQLServer(_obdIfEx.DBandMES, _obdIfEx.Log);
        }

        private int GetSN(string strNowDate) {
            int iRet;
            string strSN = DbNative.GetSN();
            if (strSN.Length == 0) {
                iRet = _obdIfEx.OBDResultSetting.StartSN;
            } else if (strSN.Split(',')[0] != strNowDate) {
                iRet = _obdIfEx.OBDResultSetting.StartSN;
            } else {
                bool result = int.TryParse(strSN.Split(',')[1], out iRet);
                if (result) {
                    iRet = (iRet % 1000) + _obdIfEx.OBDResultSetting.StartSN;
                } else {
                    iRet = _obdIfEx.OBDResultSetting.StartSN;
                }
            }
            return iRet;
        }

        private void ReduceSN() {
            string strNowDateTime = DateTime.Now.ToLocalTime().ToString("yyyyMMdd");
            int iSN = GetSN(strNowDateTime);
            if (iSN % 1000 != 0) {
                --iSN;
            }
            iSN %= 10000;
            DbNative.SetSN(strNowDateTime + "," + iSN.ToString());
        }

        public DataTable GetDataTable(DataTableType dtType) {
            switch (dtType) {
            case DataTableType.dtInfo:
                return _dtInfo;
            case DataTableType.dtECUInfo:
                return _dtECUInfo;
            case DataTableType.dtIUPR:
                return _dtIUPR;
            default:
                return null;
            }
        }

        private void SetDataTableColumns<T>(DataTable dt, Dictionary<string, bool[]> ECUSupports, bool bIUPR = false) {
            dt.Clear();
            dt.Columns.Clear();
            dt.Columns.Add(new DataColumn("NO", typeof(int)));
            dt.Columns.Add(new DataColumn("Item", typeof(string)));
            foreach (string key in ECUSupports.Keys) {
                if (bIUPR) {
                    if (ECUSupports[key][0xB - 1] || ECUSupports[key][0x8 - 1]) {
                        dt.Columns.Add(new DataColumn(key, typeof(T)));
                    }
                } else {
                    dt.Columns.Add(new DataColumn(key, typeof(T)));
                }
            }
        }

        private void SetDataRow(int lineNO, string strItem, DataTable dt, OBDParameter param) {
            Dictionary<string, bool[]> support = new Dictionary<string, bool[]>();
            if (param.Service == 1 || ((param.Parameter >> 8) & 0x00FF) == 0xF4) {
                support = _obdIfEx.OBDDll.Mode01Support;
            } else if (param.Service == 9 || ((param.Parameter >> 8) & 0x00FF) == 0xF8) {
                support = _obdIfEx.OBDDll.Mode09Support;
            }
            DataRow dr = dt.NewRow();
            dr[0] = lineNO;
            dr[1] = strItem;

            bool bGetData = false;
            if (param.Service == 3 || param.Service == 7 || param.Service == 0x0A || param.Service == 0x19 || _obdIfEx.OBDIf.STDType == StandardType.SAE_J1939) {
                bGetData = true;
            }
            foreach (string key in support.Keys) {
                bGetData = bGetData || support[key][(param.Parameter & 0x00FF) - 1];
            }
            if ((param.Service == 0 && param.Parameter == 0) || !bGetData) {
                for (int i = 2; i < dt.Columns.Count; i++) {
                    if (support.ContainsKey(dt.Columns[i].ColumnName)) {
                        dr[i] = "";
                    }
                }
                dt.Rows.Add(dr);
                return;
            }
            List<OBDParameterValue> valueList = _obdIfEx.OBDIf.GetValueList(param);
            if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.ListString) != 0) {
                int maxLine = 0;
                foreach (OBDParameterValue value in valueList) {
                    if (value.ErrorDetected) {
                        for (int i = 2; i < dt.Columns.Count; i++) {
                            if (dt.Columns[i].ColumnName == value.ECUResponseID) {
                                dr[i] = "";
                            }
                        }
                        continue;
                    }
                    if (value.ListStringValue != null) {
                        if (value.ListStringValue.Count > maxLine) {
                            maxLine = value.ListStringValue.Count;
                        }
                        for (int i = 2; i < dt.Columns.Count; i++) {
                            if (dt.Columns[i].ColumnName == value.ECUResponseID) {
                                if (value.ListStringValue.Count == 0 || value.ListStringValue[0].Length == 0) {
                                    dr[i] = "";
                                } else {
                                    dr[i] = value.ListStringValue[0];
                                    for (int j = 1; j < value.ListStringValue.Count; j++) {
                                        dr[i] += "\n" + value.ListStringValue[j];
                                    }
                                }
                            }
                        }
                    }
                }
                if (param.Service == 1 || param.Service == 9 || param.Service == 0x22) {
                    for (int i = 2; i < dt.Columns.Count; i++) {
                        if (support.ContainsKey(dt.Columns[i].ColumnName) && !support[dt.Columns[i].ColumnName][(param.Parameter & 0x00FF) - 1]) {
                            dr[i] = "不适用";
                        }
                    }
                }
                dt.Rows.Add(dr);
            } else {
                foreach (OBDParameterValue value in valueList) {
                    if (value.ErrorDetected) {
                        for (int i = 2; i < dt.Columns.Count; i++) {
                            dr[i] = "";
                        }
                        break;
                    }
                    for (int i = 2; i < dt.Columns.Count; i++) {
                        if (dt.Columns[i].ColumnName == value.ECUResponseID) {
                            if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.Bool) != 0) {
                                if (value.BoolValue) {
                                    dr[i] = "ON";
                                } else {
                                    dr[i] = "OFF";
                                }
                            } else if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.Double) != 0) {
                                dr[i] = value.DoubleValue.ToString();
                            } else if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.String) != 0) {
                                dr[i] = value.StringValue;
                            } else if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.ShortString) != 0) {
                                dr[i] = value.ShortStringValue;
                            }
                        }
                    }
                }
                if (param.Service == 1 || param.Service == 9 || param.Service == 0x22) {
                    for (int i = 2; i < dt.Columns.Count; i++) {
                        if (support.ContainsKey(dt.Columns[i].ColumnName) && !support[dt.Columns[i].ColumnName][(param.Parameter & 0x00FF) - 1]) {
                            dr[i] = "不适用";
                        }
                    }
                }
                dt.Rows.Add(dr);
            }
        }

        private void SetReadinessDataRow(int lineNO, string strItem, DataTable dt, List<OBDParameterValue> valueList, string sigName, ref int errorCount) {
            DataRow dr = dt.NewRow();
            dr[0] = lineNO;
            dr[1] = strItem;
            foreach (OBDParameterValue value in valueList) {
                if (value.ErrorDetected) {
                    for (int i = 2; i < dt.Columns.Count; i++) {
                        dr[i] = "";
                    }
                    break;
                }
                for (int i = 2; i < dt.Columns.Count; i++) {
                    if (dt.Columns[i].ColumnName == value.ECUResponseID) {
                        if (sigName == "不适用") {
                            dr[i] = sigName;
                        } else {
                            foreach (string name in value.Message.Signals.Keys) {
                                if (name == sigName) {
                                    dr[i] = value.Message.Signals[name].DisplayString;
                                }
                            }
                        }
                    }
                }
            }
            if (_obdIfEx.OBDIf.STDType != StandardType.SAE_J1939) {
                for (int i = 2; i < dt.Columns.Count; i++) {
                    if (_obdIfEx.OBDDll.Mode01Support.ContainsKey(dt.Columns[i].ColumnName) && !_obdIfEx.OBDDll.Mode01Support[dt.Columns[i].ColumnName][0]) {
                        dr[i] = "不适用";
                    }
                }
            }
            dt.Rows.Add(dr);
            for (int i = 2; i < dt.Columns.Count; i++) {
                if (dt.Rows[lineNO - 1][i].ToString() == "未完成") {
                    ++errorCount;
                }
            }
        }

        private void SetIUPRDataRow(int lineNO, string strItem, int padTotal, int padNum, DataTable dt, List<OBDParameterValue> valueList, string sigName, bool supported) {
            double num = 0;
            double den = 0;
            DataRow dr = dt.NewRow();
            dr[0] = lineNO;
            foreach (OBDParameterValue value in valueList) {
                for (int i = 2; i < dt.Columns.Count; i++) {
                    if (dt.Columns[i].ColumnName == value.ECUResponseID) {
                        if (supported) {
                            if (dr[1].ToString().Length == 0) {
                                dr[1] = strItem + ": " + "监测完成次数".PadLeft(padTotal - padNum + 6);
                            }
                            foreach (string name in value.Message.Signals.Keys) {
                                if (name == sigName) {
                                    num = value.Message.Signals[name].Value;
                                    dr[i] = value.Message.Signals[name].DisplayString;
                                }
                            }
                        }
                    }
                }
            }
            if (dr[1].ToString().Length > 0) {
                dt.Rows.Add(dr);
            }

            dr = dt.NewRow();
            foreach (OBDParameterValue value in valueList) {
                for (int i = 2; i < dt.Columns.Count; i++) {
                    if (dt.Columns[i].ColumnName == value.ECUResponseID) {
                        if (supported) {
                            if (dr[1].ToString().Length == 0) {
                                dr[1] = "符合监测条件次数".PadLeft(padTotal + 8);
                            }
                            foreach (string name in value.Message.Signals.Keys) {
                                if (name == sigName.Replace("COMP", "COND")) {
                                    den = value.Message.Signals[name].Value;
                                    dr[i] = value.Message.Signals[name].DisplayString;
                                }
                            }
                        }
                    }
                }
            }
            if (dr[1].ToString().Length > 0) {
                dt.Rows.Add(dr);
            }

            dr = dt.NewRow();
            foreach (OBDParameterValue value in valueList) {
                for (int i = 2; i < dt.Columns.Count; i++) {
                    if (dt.Columns[i].ColumnName == value.ECUResponseID) {
                        if (supported) {
                            if (dr[1].ToString().Length == 0) {
                                dr[1] = "IUPR率".PadLeft(padTotal + 5);
                            }
                            if (den == 0) {
                                dr[i] = "7.99527";
                            } else {
                                double r = Math.Round(num / den, 6);
                                if (r > 7.99527) {
                                    dr[i] = "7.99527";
                                } else {
                                    dr[i] = r.ToString();
                                }
                            }
                        }
                    }
                }
            }
            if (dr[1].ToString().Length > 0) {
                dt.Rows.Add(dr);
            }
        }

        private void SetDataTableInfo() {
            DataTable dt = _dtInfo;
            int NO = 0;
            OBDParameter param = new OBDParameter();
            int HByte = 0;
            if (_obdIfEx.OBDResultSetting.MIL) {
                if (_obdIfEx.OBDIf.STDType == StandardType.ISO_27145) {
                    param = new OBDParameter {
                        OBDRequest = "22F401",
                        Service = 0x22,
                        Parameter = 0xF401,
                        SignalName = "MIL",
                        ValueTypes = (int)OBDParameter.EnumValueTypes.Bool
                    };
                    HByte = 0xF400;
                } else if (_obdIfEx.OBDIf.STDType == StandardType.ISO_15031) {
                    param = new OBDParameter {
                        OBDRequest = "0101",
                        Service = 1,
                        Parameter = 1,
                        SignalName = "MIL",
                        ValueTypes = (int)OBDParameter.EnumValueTypes.Bool
                    };
                } else if (_obdIfEx.OBDIf.STDType == StandardType.SAE_J1939) {
                    param = new OBDParameter();
                }
            } else {
                param = new OBDParameter();
            }
            SetDataRow(++NO, "MIL状态", dt, param);              // 0

            if (_obdIfEx.OBDResultSetting.MIL) {
                if (_obdIfEx.OBDIf.STDType != StandardType.SAE_J1939) {
                    param.Parameter = HByte + 0x21;
                    param.SignalName = "MIL_DIST";
                    param.ValueTypes = (int)OBDParameter.EnumValueTypes.Double;
                } else {
                    param = new OBDParameter();
                }
            } else {
                param = new OBDParameter();
            }
            SetDataRow(++NO, "MIL亮后行驶里程（km）", dt, param); // 1

            if (_obdIfEx.OBDIf.STDType == StandardType.ISO_27145) {
                param = new OBDParameter {
                    OBDRequest = "22F41C",
                    Service = 0x22,
                    Parameter = 0xF41C,
                    SignalName = "OBDSUP",
                ValueTypes = (int)OBDParameter.EnumValueTypes.ShortString
                };
                HByte = 0xF400;
            } else if (_obdIfEx.OBDIf.STDType == StandardType.ISO_15031) {
                param = new OBDParameter {
                    OBDRequest = "011C",
                    Service = 1,
                    Parameter = 0x1C,
                    SignalName = "OBDSUP",
                    ValueTypes = (int)OBDParameter.EnumValueTypes.ShortString
                };
            } else if (_obdIfEx.OBDIf.STDType == StandardType.SAE_J1939) {
                param = new OBDParameter {
                    OBDRequest = "00FECE",
                    Service = 0,
                    Parameter = 0xFECE,
                    SignalName = "OBDSUP",
                    ValueTypes = (int)OBDParameter.EnumValueTypes.ShortString
                };
            }
            SetDataRow(++NO, "OBD型式检验类型", dt, param);      // 2
            string OBD_SUP = dt.Rows[dt.Rows.Count - 1][2].ToString().Split(',')[0];
            string[] CN6_OBD_SUP = _obdIfEx.OBDIf.DllSettings.CN6_OBD_SUP.Split(',');
            foreach (string item in CN6_OBD_SUP) {
                if (OBD_SUP == item) {
                    _CN6 = true;
                    break;
                }
            }
            // 判断ECM的OBD型式是否合法
            if (_obdIfEx.OBDResultSetting.OBD_SUP) {
                if (OBD_SUP.Length == 0 || OBD_SUP.Length > 2 || OBD_SUP == "不适用") {
                    OBDSUPResult = false;
                }
            }

            if (_obdIfEx.OBDIf.STDType != StandardType.SAE_J1939) {
                param.Parameter = HByte + 0xA6;
                param.SignalName = "ODO";
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.Double;
            } else {
                param = new OBDParameter();
            }
            SetDataRow(++NO, "总累积里程ODO（km）", dt, param);  // 3

            if (_obdIfEx.OBDResultSetting.DTC03) {
                if (_obdIfEx.OBDIf.STDType == StandardType.ISO_27145) {
                    param.OBDRequest = "194233081E";
                } else if (_obdIfEx.OBDIf.STDType == StandardType.ISO_15031) {
                    param.OBDRequest = "03";
                } else if (_obdIfEx.OBDIf.STDType == StandardType.SAE_J1939) {
                    param = new OBDParameter();
                }
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
                SetDataRow(++NO, "存储DTC", dt, param);        // 4
                for (int i = 2; i < dt.Columns.Count; i++) {
                    string DTC = dt.Rows[dt.Rows.Count - 1][i].ToString();
                    if (DTC != "--" && DTC != "不适用" && DTC.Length > 0) {
                        DTCResult = false;
                    }
                }
            } else {
                param = new OBDParameter();
                SetDataRow(++NO, "存储DTC", dt, param);
            }

            if (_obdIfEx.OBDResultSetting.DTC07) {
                if (_obdIfEx.OBDIf.STDType == StandardType.ISO_27145) {
                    param.OBDRequest = "194233041E";
                } else if (_obdIfEx.OBDIf.STDType == StandardType.ISO_15031) {
                    param.OBDRequest = "07";
                } else if (_obdIfEx.OBDIf.STDType == StandardType.SAE_J1939) {
                    param = new OBDParameter();
                }
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
                SetDataRow(++NO, "未决DTC", dt, param);         // 5
                for (int i = 2; i < dt.Columns.Count; i++) {
                    string DTC = dt.Rows[dt.Rows.Count - 1][i].ToString();
                    if (DTC != "--" && DTC != "不适用" && DTC.Length > 0) {
                        DTCResult = false;
                    }
                }
            } else {
                param = new OBDParameter();
                SetDataRow(++NO, "未决DTC", dt, param);
            }

            if (_obdIfEx.OBDResultSetting.DTC0A) {
                if (_obdIfEx.OBDIf.STDType == StandardType.ISO_27145) {
                    param.OBDRequest = "195533";
                } else if (_obdIfEx.OBDIf.STDType == StandardType.ISO_15031) {
                    param.OBDRequest = "0A";
                } else if (_obdIfEx.OBDIf.STDType == StandardType.SAE_J1939) {
                    param = new OBDParameter();
                }
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
                SetDataRow(++NO, "永久DTC", dt, param);         // 6
                for (int i = 2; i < dt.Columns.Count; i++) {
                    string DTC = dt.Rows[dt.Rows.Count - 1][i].ToString();
                    if (DTC != "--" && DTC != "不适用" && DTC.Length > 0) {
                        DTCResult = false;
                    }
                }
            } else {
                param = new OBDParameter();
                SetDataRow(++NO, "永久DTC", dt, param);
            }

            int errorCount = 0;
            if (_obdIfEx.OBDResultSetting.Readiness) {
                if (_obdIfEx.OBDIf.STDType == StandardType.ISO_27145) {
                    param.OBDRequest = "22F401";
                } else if (_obdIfEx.OBDIf.STDType == StandardType.ISO_15031) {
                    param.OBDRequest = "0101";
                } else if (_obdIfEx.OBDIf.STDType == StandardType.SAE_J1939) {
                    param = new OBDParameter();
                }
            } else {
                param = new OBDParameter();
            }
            param.ValueTypes = (int)OBDParameter.EnumValueTypes.BitFlags;
            List<OBDParameterValue> valueList = new List<OBDParameterValue>();
            if (param.Service == 0 && param.Parameter == 0) {
                OBDParameterValue value = new OBDParameterValue {
                    ErrorDetected = true
                };
                valueList.Add(value);
            } else {
                valueList = _obdIfEx.OBDIf.GetValueList(param);
            }
            SetReadinessDataRow(++NO, "失火监测", dt, valueList, "MIS_RDY", ref errorCount);      // 7
            SetReadinessDataRow(++NO, "燃油系统监测", dt, valueList, "FUEL_RDY", ref errorCount); // 8
            SetReadinessDataRow(++NO, "综合组件监测", dt, valueList, "CCM_RDY", ref errorCount);  // 9

            if (_obdIfEx.OBDIf.STDType != StandardType.SAE_J1939 && !_obdIfEx.OBDResultSetting.Readiness) {
                foreach (OBDParameterValue value in valueList) {
                    if (value.ECUResponseID != null && _obdIfEx.OBDDll.Mode01Support.ContainsKey(value.ECUResponseID) && _obdIfEx.OBDDll.Mode01Support[value.ECUResponseID][(param.Parameter & 0x00FF) - 1]) {
                        _compIgn = value.GetBitFlag(12);
                        break;
                    }
                }
            }
            if (_compIgn) {
                // 压缩点火
                SetReadinessDataRow(++NO, "NMHC催化剂监测", dt, valueList, "HCCATRDY", ref errorCount);             // 10
                SetReadinessDataRow(++NO, "NOx/SCR后处理监测", dt, valueList, "NCAT_RDY", ref errorCount);          // 11
                SetReadinessDataRow(++NO, "增压系统监测", dt, valueList, "BP_RDY", ref errorCount);                 // 12
                SetReadinessDataRow(++NO, "排气传感器监测", dt, valueList, "EGS_RDY", ref errorCount);              // 13
                SetReadinessDataRow(++NO, "PM过滤器监测", dt, valueList, "PM_RDY", ref errorCount);                 // 14
                SetReadinessDataRow(++NO, "EGR/VVT系统监测", dt, valueList, "EGR_RDY_compression", ref errorCount); // 15
            } else {
                // 火花点火
                SetReadinessDataRow(++NO, "催化剂监测", dt, valueList, "CAT_RDY", ref errorCount);               // 10
                SetReadinessDataRow(++NO, "加热催化剂监测", dt, valueList, "HCAT_RDY", ref errorCount);          // 11
                SetReadinessDataRow(++NO, "燃油蒸发系统监测", dt, valueList, "EVAP_RDY", ref errorCount);        // 12
                SetReadinessDataRow(++NO, "二次空气系统监测", dt, valueList, "AIR_RDY", ref errorCount);         // 13
                SetReadinessDataRow(++NO, "空调系统制冷剂监测", dt, valueList, "不适用", ref errorCount);         // 14
                SetReadinessDataRow(++NO, "氧气传感器监测", dt, valueList, "O2S_RDY", ref errorCount);           // 15
                SetReadinessDataRow(++NO, "加热氧气传感器监测", dt, valueList, "HTR_RDY", ref errorCount);       // 16
                SetReadinessDataRow(++NO, "EGR/VVT系统监测", dt, valueList, "EGR_RDY_spark", ref errorCount);   // 17
            }
            if (_obdIfEx.OBDResultSetting.Readiness && errorCount > 2) {
                ReadinessResult = false;
            }

        }

        private void SetDataTableECUInfo() {
            DataTable dt = _dtECUInfo;
            int NO = 0;
            OBDParameter param = new OBDParameter();
            int HByte = 0;
            if (_obdIfEx.OBDIf.STDType == StandardType.ISO_27145) {
                param = new OBDParameter {
                    OBDRequest = "22F802",
                    Service = 0x22,
                    Parameter = 0xF802,
                    SignalName = "VIN",
                    ValueTypes = (int)OBDParameter.EnumValueTypes.ListString
                };
                HByte = 0xF800;
            } else if (_obdIfEx.OBDIf.STDType == StandardType.ISO_15031) {
                param = new OBDParameter {
                    OBDRequest = "0902",
                    Service = 9,
                    Parameter = 2,
                    SignalName = "VIN",
                    ValueTypes = (int)OBDParameter.EnumValueTypes.ListString
                };
            } else if (_obdIfEx.OBDIf.STDType == StandardType.SAE_J1939) {
                param = new OBDParameter {
                    OBDRequest = "00FEEC",
                    Service = 0,
                    Parameter = 0xFEEC,
                    SignalName = "VIN",
                    ValueTypes = (int)OBDParameter.EnumValueTypes.ListString
                };
            }
            SetDataRow(++NO, "VIN", dt, param);     // 0
            string strVIN = "";
            for (int i = 2; i < dt.Columns.Count; i++) {
                strVIN = dt.Rows[0][i].ToString();
                if (strVIN.Length > 0 || strVIN != "不适用" || strVIN != "--") {
                    break;
                }
            }
            StrVIN_ECU = strVIN;
            if (_obdIfEx.OBDResultSetting.VINError && StrVIN_IN != null && StrVIN_ECU != StrVIN_IN && StrVIN_IN.Length > 0) {
                _obdIfEx.Log.TraceWarning("Scanner VIN[" + StrVIN_IN + "] and ECU VIN[" + StrVIN_ECU + "] are not consistent");
                VINResult = false;
            }

            if (_obdIfEx.OBDIf.STDType != StandardType.SAE_J1939 && _obdIfEx.OBDResultSetting.UseECUAcronym) {
                param.Parameter = HByte + 0x0A;
            } else {
                param = new OBDParameter();
            }
            SetDataRow(++NO, "ECU名称", dt, param); // 1

            if (_obdIfEx.OBDIf.STDType == StandardType.ISO_27145) {
                param.OBDRequest = "22F804";
                param.SignalName = "CAL_ID";
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
            } else if (_obdIfEx.OBDIf.STDType == StandardType.ISO_15031) {
                param.OBDRequest = "0904";
                param.SignalName = "CAL_ID";
                param.ValueTypes = (int)OBDParameter.EnumValueTypes.ListString;
            } else if (_obdIfEx.OBDIf.STDType == StandardType.SAE_J1939) {
                param = new OBDParameter {
                    OBDRequest = "00D300",
                    Service = 0,
                    Parameter = 0xD300,
                    SignalName = "CAL_ID",
                    ValueTypes = (int)OBDParameter.EnumValueTypes.ListString
                };
            }
            SetDataRow(++NO, "CAL_ID", dt, param);  // 2

            if (_obdIfEx.OBDIf.STDType != StandardType.SAE_J1939) {
                param.Parameter = HByte + 6;
                param.SignalName = "CVN";
            } else {
                param = new OBDParameter {
                    OBDRequest = "00D300",
                    Service = 0,
                    Parameter = 0xD300,
                    SignalName = "CVN",
                    ValueTypes = (int)OBDParameter.EnumValueTypes.ListString
                };
            }
            SetDataRow(++NO, "CVN", dt, param);     // 3

            // 根据配置文件，判断CAL_ID和CVN两个值的合法性
            for (int i = 2; i < dt.Columns.Count; i++) {
                string[] CALIDArray = dt.Rows[2][i].ToString().Split('\n');
                string[] CVNArray = dt.Rows[3][i].ToString().Split('\n');
                int length = Math.Max(CALIDArray.Length, CVNArray.Length);
                for (int j = 0; j < length; j++) {
                    string CALID = CALIDArray.Length > j ? CALIDArray[j] : "";
                    string CVN = CVNArray.Length > j ? CVNArray[j] : "";
                    if (_CN6) {
                        if (!_obdIfEx.OBDResultSetting.CALIDCVNEmpty) {
                            if (CALID.Length * CVN.Length == 0) {
                                if (CALID.Length + CVN.Length == 0) {
                                    if (j == 0) {
                                        CALIDCVNResult = false;
                                        CALIDCVNAllEmpty = true;
                                    }
                                } else {
                                    CALIDCVNResult = false;
                                }
                            }
                        }
                        // 国六车型，CALID全部字符均为空格的话也判为乱码
                        if (Utility.IsUnmeaningString(CALID, _obdIfEx.OBDResultSetting.UnmeaningNum, true)) {
                            CALIDUnmeaningResult = false;
                        }
                    } else if (Utility.IsUnmeaningString(CALID, _obdIfEx.OBDResultSetting.UnmeaningNum, false)) {
                        // 国五车型，CALID全部字符均为空格的话不判为乱码
                        CALIDUnmeaningResult = false;
                    }
                }
            }

        }

        private void SetDataTableIUPR() {
            DataTable dt = _dtIUPR;
            int NO = 0;
            OBDParameter param;
            List<OBDParameterValue> valueList;
            int HByte = 0;
            if (_obdIfEx.OBDIf.STDType == StandardType.ISO_27145) {
                param = new OBDParameter {
                    OBDRequest = "22F80B",
                    Service = 0x22,
                    Parameter = 0xF80B,
                    ValueTypes = (int)OBDParameter.EnumValueTypes.ListString
                };
                HByte = 0xF800;
            } else {
                param = new OBDParameter {
                    OBDRequest = "090B",
                    Service = 9,
                    Parameter = 0x0B,
                    ValueTypes = (int)OBDParameter.EnumValueTypes.ListString
                };
            }
            for (int i = 2; i < dt.Columns.Count; i++) {
                // 压缩点火
                bool supported = _obdIfEx.OBDDll.Mode09Support.ContainsKey(dt.Columns[i].ColumnName);
                supported = supported && _obdIfEx.OBDDll.Mode09Support[dt.Columns[i].ColumnName][param.Parameter - HByte - 1];
                if (supported) {
                    valueList = _obdIfEx.OBDIf.GetValueList(param);
                    SetIUPRDataRow(++NO, "NMHC催化器", 18, 12, dt, valueList, "HCCATCOMP", supported);
                    SetIUPRDataRow(++NO, "NOx催化器", 18, 11, dt, valueList, "NCATCOMP", supported);
                    SetIUPRDataRow(++NO, "NOx吸附器", 18, 11, dt, valueList, "NADSCOMP", supported);
                    SetIUPRDataRow(++NO, "PM捕集器", 18, 10, dt, valueList, "PMCOMP", supported);
                    SetIUPRDataRow(++NO, "废气传感器", 18, 12, dt, valueList, "EGSCOMP", supported);
                    SetIUPRDataRow(++NO, "EGR和VVT", 18, 10, dt, valueList, "EGRCOMP", supported);
                    SetIUPRDataRow(++NO, "增压压力", 18, 10, dt, valueList, "BPCOMP", supported);
                }
                // 火花点火
                NO = 0;
                param.Parameter = HByte + 8;
                supported = _obdIfEx.OBDDll.Mode09Support.ContainsKey(dt.Columns[i].ColumnName);
                supported = supported && _obdIfEx.OBDDll.Mode09Support[dt.Columns[i].ColumnName][param.Parameter - HByte - 1];
                if (supported) {
                    valueList = _obdIfEx.OBDIf.GetValueList(param);
                    SetIUPRDataRow(++NO, "催化器 组1", 18, 12, dt, valueList, "CATCOMP1", supported);
                    SetIUPRDataRow(++NO, "催化器 组2", 18, 12, dt, valueList, "CATCOMP2", supported);
                    SetIUPRDataRow(++NO, "前氧传感器 组1", 18, 16, dt, valueList, "O2SCOMP1", supported);
                    SetIUPRDataRow(++NO, "前氧传感器 组2", 18, 16, dt, valueList, "O2SCOMP2", supported);
                    SetIUPRDataRow(++NO, "后氧传感器 组1", 18, 16, dt, valueList, "SO2SCOMP1", supported);
                    SetIUPRDataRow(++NO, "后氧传感器 组2", 18, 16, dt, valueList, "SO2SCOMP2", supported);
                    SetIUPRDataRow(++NO, "EVAP", 18, 6, dt, valueList, "EVAPCOMP", supported);
                    SetIUPRDataRow(++NO, "EGR和VVT", 18, 10, dt, valueList, "EGRCOMP", supported);
                    SetIUPRDataRow(++NO, "GPF 组1", 18, 9, dt, valueList, "PFCOMP1", supported);
                    SetIUPRDataRow(++NO, "GPF 组2", 18, 9, dt, valueList, "PFCOMP2", supported);
                    SetIUPRDataRow(++NO, "二次空气喷射系统", 18, 18, dt, valueList, "AIRCOMP", supported);
                }
            }
        }

        /// <summary>
        /// 返回值代表检测数据上传后是否返回成功信息
        /// </summary>
        /// <param name="errorMsg">错误信息</param>
        /// <returns>是否返回成功信息</returns>
        public void StartOBDTest(out string errorMsg) {
            _obdIfEx.Log.TraceInfo(string.Format(">>>>> Enter StartOBDTest function. Ver(Main / Dll): {0} / {1} <<<<<", MainFileVersion.AssemblyVersion, DllVersion<SH_OBD_Dll>.AssemblyVersion));
            errorMsg = "";
            _dtInfo.Clear();
            _dtInfo.Dispose();
            _dtECUInfo.Clear();
            _dtECUInfo.Dispose();
            _dtIUPR.Clear();
            _dtIUPR.Dispose();
            _compIgn = false;
            _CN6 = false;
            OBDResult = false;
            DTCResult = true;
            ReadinessResult = true;
            VINResult = true;
            CALIDCVNResult = true;
            CALIDCVNAllEmpty = false;
            CALIDUnmeaningResult = true;
            OBDSUPResult = true;

            OBDTestStart?.Invoke();

            if (!_obdIfEx.OBDDll.SetSupportStatus(out errorMsg)) {
                SetupColumnsDone?.Invoke();
                throw new Exception(errorMsg);
            }

            SetDataTableColumns<string>(_dtInfo, _obdIfEx.OBDDll.Mode01Support);
            if (_obdIfEx.OBDIf.STDType == StandardType.SAE_J1939) {
                // J1939用mode01来初始化_dtECUInfo
                SetDataTableColumns<string>(_dtECUInfo, _obdIfEx.OBDDll.Mode01Support);
            } else {
                SetDataTableColumns<string>(_dtECUInfo, _obdIfEx.OBDDll.Mode09Support);
                SetDataTableColumns<string>(_dtIUPR, _obdIfEx.OBDDll.Mode09Support, true);
            }
            SetupColumnsDone?.Invoke();
            SetDataTableInfo();
            SetDataTableECUInfo();
            SetDataTableIUPR();

            WriteDbStart?.Invoke();
            OBDResult = DTCResult && ReadinessResult && VINResult && CALIDCVNResult && CALIDUnmeaningResult && OBDSUPResult;
            string strLog = "OBD Test Result: " + OBDResult.ToString() + " [";
            strLog += "DTCResult: " + DTCResult.ToString();
            strLog += ", ReadinessResult: " + ReadinessResult.ToString();
            strLog += ", CALIDUnmeaningResult: " + CALIDUnmeaningResult.ToString();
            strLog += ", OBDSUPResult: " + OBDSUPResult.ToString();
            strLog += ", VINResult: " + VINResult.ToString();
            strLog += ", CALIDCVNResult: " + CALIDCVNResult.ToString() + "]";
            _obdIfEx.Log.TraceInfo(strLog);

            string strOBDResult = OBDResult ? "1" : "0";

            DataTable dt = new DataTable("OBDData");
            DbNative.GetEmptyTable(dt);
            dt.Columns.Remove("ID");
            dt.Columns.Remove("WriteTime");
            try {
                SetDataTableResult(StrVIN_ECU, strOBDResult, dt);
            } catch (Exception ex) {
                _obdIfEx.Log.TraceError("Result DataTable Error: " + ex.Message);
                dt.Dispose();
                WriteDbDone?.Invoke();
                throw new Exception("生成 Result DataTable 出错: " + ex.Message);
            }

            DataTable dtIUPR = new DataTable("OBDIUPR");
            DbNative.GetEmptyTable(dtIUPR);
            dtIUPR.Columns.Remove("ID");
            dtIUPR.Columns.Remove("WriteTime");
            SetDataTableResultIUPR(StrVIN_ECU, dtIUPR);

            DbNative.ModifyRecords(dt);
            DbNative.ModifyRecords(dtIUPR);
            WriteDbDone?.Invoke();

            try {
                ExportResultFile(dt);
            } catch (Exception ex) {
                _obdIfEx.Log.TraceError("Exporting OBD test result file failed: " + ex.Message);
                dt.Dispose();
                throw new Exception("生成OBD检测结果文件出错");
            }

            // 用“无条件上传”和“OBD检测结果”判断是否需要直接返回不上传MES
            if (!_obdIfEx.OBDResultSetting.UploadWhenever && !OBDResult) {
                _obdIfEx.Log.TraceError("Won't upload data because OBD test result is NOK");
                NotUploadData?.Invoke();
                dt.Dispose();
                return;
            }

            try {
                UploadData(StrVIN_ECU, strOBDResult, dt, ref errorMsg);
            } catch (Exception) {
                dt.Dispose();
                throw;
            }
            dt.Dispose();
        }

        private void UploadData(string strVIN, string strOBDResult, DataTable dtIn, ref string errorMsg, bool bShowMsg = true) {
            // 上传MES接口
            if (bShowMsg) {
                UploadDataStart?.Invoke();
            }
            _obdIfEx.DBandMES.ChangeWebService = false;
            if (!WSHelper.CreateWebService(_obdIfEx.DBandMES, out string error)) {
                _obdIfEx.Log.TraceError("CreateWebService Error: " + error);
                throw new Exception("上传失败：获取 WebService 接口出错, " + error);
            }

            // DataTable必须设置TableName，否则调用方法时会报错“生成 XML 文档时出错”
            DataTable dt1MES = new DataTable("dt1");
            SetDataTable1MES(dt1MES, strVIN, strOBDResult);

            DataTable dt2MES = new DataTable("dt2");
            SetDataTable2MES(dt2MES, dtIn);

            string strMsg = "";
            string strRet = "";
            int count = 0;
            for (count = 0; count < 4; count++) {
                if ((strRet.Contains("OK") && !strRet.Contains("NOK")) || strRet.Contains("true") || strRet.Contains("True")) {
                    // 返回成功信息
                    break;
                } else {
                    // 返回失败信息
                    if (strRet.Length > 0) {
                        _obdIfEx.Log.TraceError(WSHelper.GetMethodName(0) + " return false");
                    }
                    try {
                        strRet = WSHelper.GetResponseOutString(WSHelper.GetMethodName(0), out strMsg, dt1MES, dt2MES);
                    } catch (Exception ex) {
                        _obdIfEx.Log.TraceError("WebService GetResponseString occured Exception: " + ex.Message + (strMsg.Length > 0 ? ", " + strMsg : ""));
                        dt2MES.Dispose();
                        dt1MES.Dispose();
                        if (bShowMsg) {
                            UploadDataDone?.Invoke();
                        }
                        errorMsg = strMsg;
                        ReduceSN();
                        throw new Exception("上传失败：调用 GetResponseString 接口出错，" + ex.Message);
                    }
                }
            }

            dt2MES.Dispose();
            dt1MES.Dispose();
            if (count < 4) {
                // 上传数据接口返回成功信息
                _obdIfEx.Log.TraceInfo("Upload data success, VIN = " + strVIN);
                DbNative.UpdateUpload(strVIN, "1");
                if (bShowMsg) {
                    UploadDataDone?.Invoke();
                }
#if DEBUG
                errorMsg = strMsg;
#endif
            } else {
                // 上传数据接口返回失败信息
                _obdIfEx.Log.TraceError("Upload data function return false, VIN = " + strVIN + (strMsg.Length > 0 ? ", " + strMsg : ""));
                if (bShowMsg) {
                    UploadDataDone?.Invoke();
                }
                errorMsg = strMsg;
                ReduceSN();
                throw new Exception("上传失败：" + WSHelper.GetMethodName(0) + " 返回调用失败");
            }
        }

        private void SetDataTableResult(string strVIN, string strOBDResult, DataTable dtOut) {
            string strDTCTemp;
            for (int i = 2; i < _dtECUInfo.Columns.Count; i++) {
                DataRow dr = dtOut.NewRow();
                dr["VIN"] = strVIN;
                dr["ECU_ID"] = _dtECUInfo.Columns[i].ColumnName;
                for (int j = 2; j < _dtInfo.Columns.Count; j++) {
                    if (_dtInfo.Columns[j].ColumnName == _dtECUInfo.Columns[i].ColumnName) {
                        dr["MIL"] = _dtInfo.Rows[0][j].ToString();
                        dr["MIL_DIST"] = _dtInfo.Rows[1][j].ToString();
                        dr["OBD_SUP"] = _dtInfo.Rows[2][j].ToString();
                        dr["ODO"] = _dtInfo.Rows[3][j].ToString();
                        // DTC03，若大于数据库字段容量则截断
                        strDTCTemp = _dtInfo.Rows[4][j].ToString().Replace("\n", ",");
                        if (strDTCTemp.Length > 100) {
                            dr["DTC03"] = strDTCTemp.Substring(0, 97) + "...";
                        } else {
                            dr["DTC03"] = strDTCTemp;
                        }
                        // DTC07，若大于数据库字段容量则截断
                        strDTCTemp = _dtInfo.Rows[5][j].ToString().Replace("\n", ",");
                        if (strDTCTemp.Length > 100) {
                            dr["DTC07"] = strDTCTemp.Substring(0, 97) + "...";
                        } else {
                            dr["DTC07"] = strDTCTemp;
                        }
                        // DTC0A，若大于数据库字段容量则截断
                        strDTCTemp = _dtInfo.Rows[6][j].ToString().Replace("\n", ",");
                        if (strDTCTemp.Length > 100) {
                            dr["DTC0A"] = strDTCTemp.Substring(0, 97) + "...";
                        } else {
                            dr["DTC0A"] = strDTCTemp;
                        }
                        dr["MIS_RDY"] = _dtInfo.Rows[7][j].ToString();
                        dr["FUEL_RDY"] = _dtInfo.Rows[8][j].ToString();
                        dr["CCM_RDY"] = _dtInfo.Rows[9][j].ToString();
                        if (_compIgn) {
                            dr["CAT_RDY"] = "不适用";
                            dr["HCAT_RDY"] = "不适用";
                            dr["EVAP_RDY"] = "不适用";
                            dr["AIR_RDY"] = "不适用";
                            dr["ACRF_RDY"] = "不适用";
                            dr["O2S_RDY"] = "不适用";
                            dr["HTR_RDY"] = "不适用";
                            dr["EGR_RDY"] = _dtInfo.Rows[15][j].ToString();
                            dr["HCCAT_RDY"] = _dtInfo.Rows[10][j].ToString();
                            dr["NCAT_RDY"] = _dtInfo.Rows[11][j].ToString();
                            dr["BP_RDY"] = _dtInfo.Rows[12][j].ToString();
                            dr["EGS_RDY"] = _dtInfo.Rows[13][j].ToString();
                            dr["PM_RDY"] = _dtInfo.Rows[14][j].ToString();
                        } else {
                            dr["CAT_RDY"] = _dtInfo.Rows[10][j].ToString();
                            dr["HCAT_RDY"] = _dtInfo.Rows[11][j].ToString();
                            dr["EVAP_RDY"] = _dtInfo.Rows[12][j].ToString();
                            dr["AIR_RDY"] = _dtInfo.Rows[13][j].ToString();
                            dr["ACRF_RDY"] = _dtInfo.Rows[14][j].ToString();
                            dr["O2S_RDY"] = _dtInfo.Rows[15][j].ToString();
                            dr["HTR_RDY"] = _dtInfo.Rows[16][j].ToString();
                            dr["EGR_RDY"] = _dtInfo.Rows[17][j].ToString();
                            dr["HCCAT_RDY"] = "不适用";
                            dr["NCAT_RDY"] = "不适用";
                            dr["BP_RDY"] = "不适用";
                            dr["EGS_RDY"] = "不适用";
                            dr["PM_RDY"] = "不适用";
                        }
                        break;
                    }
                }
                dr["ECU_NAME"] = _dtECUInfo.Rows[1][i].ToString();
                dr["CAL_ID"] = _dtECUInfo.Rows[2][i].ToString().Replace("\n", ",");
                dr["CVN"] = _dtECUInfo.Rows[3][i].ToString().Replace("\n", ",");
                dr["Result"] = strOBDResult;
                dr["Upload"] = "0";
                dtOut.Rows.Add(dr);
            }
        }

        private void SetDataTableResultIUPR(string strVIN, DataTable dtOut) {
            for (int i = 2; i < _dtIUPR.Columns.Count; i++) {
                DataRow dr = dtOut.NewRow();
                dr["VIN"] = strVIN;
                dr["ECU_ID"] = _dtIUPR.Columns[i].ColumnName;
                if (_obdIfEx.OBDDll.Mode09Support.ContainsKey(_dtIUPR.Columns[i].ColumnName) && _obdIfEx.OBDDll.Mode09Support[_dtIUPR.Columns[i].ColumnName][0x08 - 1] && _dtIUPR.Rows.Count > 0) {
                    dr["CATCOMP1"] = _dtIUPR.Rows[0][i].ToString();
                    dr["CATCOND1"] = _dtIUPR.Rows[1][i].ToString();
                    dr["CATCOMP2"] = _dtIUPR.Rows[3][i].ToString();
                    dr["CATCOND2"] = _dtIUPR.Rows[4][i].ToString();
                    dr["O2SCOMP1"] = _dtIUPR.Rows[6][i].ToString();
                    dr["O2SCOND1"] = _dtIUPR.Rows[7][i].ToString();
                    dr["O2SCOMP2"] = _dtIUPR.Rows[9][i].ToString();
                    dr["O2SCOND2"] = _dtIUPR.Rows[10][i].ToString();
                    dr["SO2SCOMP1"] = _dtIUPR.Rows[12][i].ToString();
                    dr["SO2SCOND1"] = _dtIUPR.Rows[13][i].ToString();
                    dr["SO2SCOMP2"] = _dtIUPR.Rows[15][i].ToString();
                    dr["SO2SCOND2"] = _dtIUPR.Rows[16][i].ToString();
                    dr["EVAPCOMP"] = _dtIUPR.Rows[18][i].ToString();
                    dr["EVAPCOND"] = _dtIUPR.Rows[19][i].ToString();
                    dr["EGRCOMP_08"] = _dtIUPR.Rows[21][i].ToString();
                    dr["EGRCOND_08"] = _dtIUPR.Rows[22][i].ToString();
                    dr["PFCOMP1"] = _dtIUPR.Rows[24][i].ToString();
                    dr["PFCOND1"] = _dtIUPR.Rows[25][i].ToString();
                    dr["PFCOMP2"] = _dtIUPR.Rows[27][i].ToString();
                    dr["PFCOND2"] = _dtIUPR.Rows[28][i].ToString();
                    dr["AIRCOMP"] = _dtIUPR.Rows[30][i].ToString();
                    dr["AIRCOND"] = _dtIUPR.Rows[31][i].ToString();
                } else {
                    dr["CATCOMP1"] = "-1";
                    dr["CATCOND1"] = "-1";
                    dr["CATCOMP2"] = "-1";
                    dr["CATCOND2"] = "-1";
                    dr["O2SCOMP1"] = "-1";
                    dr["O2SCOND1"] = "-1";
                    dr["O2SCOMP2"] = "-1";
                    dr["O2SCOND2"] = "-1";
                    dr["SO2SCOMP1"] = "-1";
                    dr["SO2SCOND1"] = "-1";
                    dr["SO2SCOMP2"] = "-1";
                    dr["SO2SCOND2"] = "-1";
                    dr["EVAPCOMP"] = "-1";
                    dr["EVAPCOND"] = "-1";
                    dr["EGRCOMP_08"] = "-1";
                    dr["EGRCOND_08"] = "-1";
                    dr["PFCOMP1"] = "-1";
                    dr["PFCOND1"] = "-1";
                    dr["PFCOMP2"] = "-1";
                    dr["PFCOND2"] = "-1";
                    dr["AIRCOMP"] = "-1";
                    dr["AIRCOND"] = "-1";
                }
                if (_obdIfEx.OBDDll.Mode09Support.ContainsKey(_dtIUPR.Columns[i].ColumnName) && _obdIfEx.OBDDll.Mode09Support[_dtIUPR.Columns[i].ColumnName][0x0B - 1] && _dtIUPR.Rows.Count > 0) {
                    dr["HCCATCOMP"] = _dtIUPR.Rows[0][i].ToString();
                    dr["HCCATCOND"] = _dtIUPR.Rows[1][i].ToString();
                    dr["NCATCOMP"] = _dtIUPR.Rows[3][i].ToString();
                    dr["NCATCOND"] = _dtIUPR.Rows[4][i].ToString();
                    dr["NADSCOMP"] = _dtIUPR.Rows[6][i].ToString();
                    dr["NADSCOND"] = _dtIUPR.Rows[7][i].ToString();
                    dr["PMCOMP"] = _dtIUPR.Rows[9][i].ToString();
                    dr["PMCOND"] = _dtIUPR.Rows[10][i].ToString();
                    dr["EGSCOMP"] = _dtIUPR.Rows[12][i].ToString();
                    dr["EGSCOND"] = _dtIUPR.Rows[13][i].ToString();
                    dr["EGRCOMP_0B"] = _dtIUPR.Rows[15][i].ToString();
                    dr["EGRCOND_0B"] = _dtIUPR.Rows[16][i].ToString();
                    dr["BPCOMP"] = _dtIUPR.Rows[18][i].ToString();
                    dr["BPCOND"] = _dtIUPR.Rows[19][i].ToString();
                } else {
                    dr["HCCATCOMP"] = "-1";
                    dr["HCCATCOND"] = "-1";
                    dr["NCATCOMP"] = "-1";
                    dr["NCATCOND"] = "-1";
                    dr["NADSCOMP"] = "-1";
                    dr["NADSCOND"] = "-1";
                    dr["PMCOMP"] = "-1";
                    dr["PMCOND"] = "-1";
                    dr["EGSCOMP"] = "-1";
                    dr["EGSCOND"] = "-1";
                    dr["EGRCOMP_0B"] = "-1";
                    dr["EGRCOND_0B"] = "-1";
                    dr["BPCOMP"] = "-1";
                    dr["BPCOND"] = "-1";
                }
                dtOut.Rows.Add(dr);
            }
        }

        private void SetDataTable1MES(DataTable dt1MES, string strVIN, string strOBDResult) {
            dt1MES.Columns.Add("TestDate");     // 0
            dt1MES.Columns.Add("SBFLAG");       // 1
            dt1MES.Columns.Add("AnalyManuf");   // 2
            dt1MES.Columns.Add("AnalyName");    // 3
            dt1MES.Columns.Add("TEType");       // 4
            dt1MES.Columns.Add("AnalyModel");   // 5
            dt1MES.Columns.Add("analyDate");    // 6
            dt1MES.Columns.Add("VIN");          // 7
            dt1MES.Columns.Add("TestNo");       // 8
            dt1MES.Columns.Add("DynoManuf");    // 9
            dt1MES.Columns.Add("DynoModel");    // 10
            dt1MES.Columns.Add("TestType");     // 11
            dt1MES.Columns.Add("APASS");        // 12
            dt1MES.Columns.Add("OPASS");        // 13
            dt1MES.Columns.Add("EPASS");        // 14
            dt1MES.Columns.Add("Result");       // 15
            dt1MES.Columns.Add("JCJLNO");       // 16
            dt1MES.Columns.Add("JCXTNO");       // 17
            dt1MES.Columns.Add("JCKSSJ");       // 18
            dt1MES.Columns.Add("JCRJ");         // 19
            dt1MES.Columns.Add("DPCGJNO");      // 20
            dt1MES.Columns.Add("CGJXT");        // 21
            dt1MES.Columns.Add("PFCSSJ");       // 22
            dt1MES.Columns.Add("JYLX");         // 23
            dt1MES.Columns.Add("YRFF");         // 24
            dt1MES.Columns.Add("RH");           // 25
            dt1MES.Columns.Add("ET");           // 26
            dt1MES.Columns.Add("AP");           // 27
            dt1MES.Columns.Add("COND");         // 28
            dt1MES.Columns.Add("HCND");         // 29
            dt1MES.Columns.Add("NOXND");        // 30
            dt1MES.Columns.Add("CO2ND");        // 31
            dt1MES.Columns.Add("YND");          // 32
            dt1MES.Columns.Add("REAC");         // 33
            dt1MES.Columns.Add("LRCO");         // 34
            dt1MES.Columns.Add("LLCO");         // 35
            dt1MES.Columns.Add("LRHC");         // 36
            dt1MES.Columns.Add("LLHC");         // 37
            dt1MES.Columns.Add("HRCO");         // 38
            dt1MES.Columns.Add("HLCO");         // 39
            dt1MES.Columns.Add("HRHC");         // 40
            dt1MES.Columns.Add("HLHC");         // 41
            dt1MES.Columns.Add("JYWD");         // 42
            dt1MES.Columns.Add("FDJZS");        // 43
            dt1MES.Columns.Add("SDSFJCSJ");     // 44
            dt1MES.Columns.Add("SDSFGKSJ");     // 45
            dt1MES.Columns.Add("SSZMHCND");     // 46
            dt1MES.Columns.Add("SSZMCOND");     // 47
            dt1MES.Columns.Add("SSZMCO2ND");    // 48
            dt1MES.Columns.Add("SSZMO2ND");     // 49
            dt1MES.Columns.Add("SSZMGDS");      // 50
            dt1MES.Columns.Add("ARHC5025");     // 51
            dt1MES.Columns.Add("ALHC5025");     // 52
            dt1MES.Columns.Add("ARCO5025");     // 53
            dt1MES.Columns.Add("ALCO5025");     // 54
            dt1MES.Columns.Add("ARNOX5025");    // 55
            dt1MES.Columns.Add("ALNOX5025");    // 56
            dt1MES.Columns.Add("ARHC2540");     // 57
            dt1MES.Columns.Add("ALHC2540");     // 58
            dt1MES.Columns.Add("ARCO2540");     // 59
            dt1MES.Columns.Add("ALCO2540");     // 60
            dt1MES.Columns.Add("ARNOX2540");    // 61
            dt1MES.Columns.Add("ALNOX2540");    // 62
            dt1MES.Columns.Add("ZJHC5025");     // 63
            dt1MES.Columns.Add("ZJCO5025");     // 64
            dt1MES.Columns.Add("ZJNO5025");     // 65
            dt1MES.Columns.Add("ZGL5025");      // 66
            dt1MES.Columns.Add("FDJZS5025");    // 67
            dt1MES.Columns.Add("CS5025");       // 68
            dt1MES.Columns.Add("ZJHC2540");     // 69
            dt1MES.Columns.Add("ZJCO2540");     // 70
            dt1MES.Columns.Add("ZJNO2540");     // 71
            dt1MES.Columns.Add("ZGL2540");      // 72
            dt1MES.Columns.Add("FDJZS2540");    // 73
            dt1MES.Columns.Add("CS2540");       // 74
            dt1MES.Columns.Add("WTJCSJ");       // 75
            dt1MES.Columns.Add("WTGKSJ");       // 76
            dt1MES.Columns.Add("WTZMCS");       // 77
            dt1MES.Columns.Add("WTZMFDJZS");    // 78
            dt1MES.Columns.Add("WTZMFZ");       // 79
            dt1MES.Columns.Add("WTZMHCND");     // 80
            dt1MES.Columns.Add("WTZMCOND");     // 81
            dt1MES.Columns.Add("WTZMNOND");     // 82
            dt1MES.Columns.Add("WTZMCO2ND");    // 83
            dt1MES.Columns.Add("WTZMO2ND");     // 84
            dt1MES.Columns.Add("WTZMZ");        // 85
            dt1MES.Columns.Add("WTNOSDXS");     // 86
            dt1MES.Columns.Add("WTZMXSDF");     // 87
            dt1MES.Columns.Add("WTZMHCNDXZ");   // 88
            dt1MES.Columns.Add("WTZMCONDXZ");   // 89
            dt1MES.Columns.Add("WTZMNONDXZ");   // 90
            dt1MES.Columns.Add("VRHC");         // 91
            dt1MES.Columns.Add("VLHC");         // 92
            dt1MES.Columns.Add("VRCO");         // 93
            dt1MES.Columns.Add("VLCO");         // 94
            dt1MES.Columns.Add("VRNOX");        // 95
            dt1MES.Columns.Add("VLNOX");        // 96
            dt1MES.Columns.Add("VRHCNOX");      // 97
            dt1MES.Columns.Add("VLHCNOX");      // 98
            dt1MES.Columns.Add("JYCSSJ");       // 99
            dt1MES.Columns.Add("JYGL");         // 100
            dt1MES.Columns.Add("JYXSJL");       // 101
            dt1MES.Columns.Add("JYHCPF");       // 102
            dt1MES.Columns.Add("JYCOPF");       // 103
            dt1MES.Columns.Add("JYNOXPF");      // 104
            dt1MES.Columns.Add("JYPLCS");       // 105
            dt1MES.Columns.Add("JYGK");         // 106
            dt1MES.Columns.Add("JYZMCS");       // 107
            dt1MES.Columns.Add("JYZMZS");       // 108
            dt1MES.Columns.Add("JYZMZH");       // 109
            dt1MES.Columns.Add("JYZMHCND");     // 110
            dt1MES.Columns.Add("JYZMHCNDXZ");   // 111
            dt1MES.Columns.Add("JYZMCOND");     // 112
            dt1MES.Columns.Add("JYZMCONDXZ");   // 113
            dt1MES.Columns.Add("JYZMNOXND");    // 114
            dt1MES.Columns.Add("JYZMNOXNDXZ");  // 115
            dt1MES.Columns.Add("JYZMCO2ND");    // 116
            dt1MES.Columns.Add("JYZMO2ND");     // 117
            dt1MES.Columns.Add("JYXSO2ND");     // 118
            dt1MES.Columns.Add("JYXSLL");       // 119
            dt1MES.Columns.Add("JYXSXS");       // 120
            dt1MES.Columns.Add("JYNOSDXZ");     // 121
            dt1MES.Columns.Add("JYZMZ");        // 122
            dt1MES.Columns.Add("RateRev");      // 123
            dt1MES.Columns.Add("Rev");          // 124
            dt1MES.Columns.Add("SmokeK1");      // 125
            dt1MES.Columns.Add("SmokeK2");      // 126
            dt1MES.Columns.Add("SmokeK3");      // 127
            dt1MES.Columns.Add("SmokeAvg");     // 128
            dt1MES.Columns.Add("SmokeKLimit");  // 129
            dt1MES.Columns.Add("ZYGXSZ");       // 130
            dt1MES.Columns.Add("ZYJCSSJ");      // 131
            dt1MES.Columns.Add("ZYGKSJ");       // 132
            dt1MES.Columns.Add("ZYZS");         // 133
            dt1MES.Columns.Add("YDJZZC");       // 134
            dt1MES.Columns.Add("YDJMC");        // 135
            dt1MES.Columns.Add("ZYCCRQ");       // 136
            dt1MES.Columns.Add("ZYJDRQ");       // 137
            dt1MES.Columns.Add("ZYJCJL");       // 138
            dt1MES.Columns.Add("ZYBDJL");       // 139
            dt1MES.Columns.Add("RateRevUp");    // 140
            dt1MES.Columns.Add("RateRevDown");  // 141
            dt1MES.Columns.Add("Rev100");       // 142
            dt1MES.Columns.Add("MaxPower");     // 143
            dt1MES.Columns.Add("MaxPowerLimit");// 144
            dt1MES.Columns.Add("Smoke100");     // 145
            dt1MES.Columns.Add("Smoke80");      // 146
            dt1MES.Columns.Add("SmokeLimit");   // 147
            dt1MES.Columns.Add("Nox");          // 148
            dt1MES.Columns.Add("NoxLimit");     // 149
            dt1MES.Columns.Add("JSGXS100");     // 150
            dt1MES.Columns.Add("JSGXS80");      // 151
            dt1MES.Columns.Add("JSLBGL");       // 152
            dt1MES.Columns.Add("JSFDJZS");      // 153
            dt1MES.Columns.Add("JSJCSJ");       // 154
            dt1MES.Columns.Add("JSGKSJ");       // 155
            dt1MES.Columns.Add("JSZMCS");       // 156
            dt1MES.Columns.Add("JSZMZS");       // 157
            dt1MES.Columns.Add("JSZMZH");       // 158
            dt1MES.Columns.Add("JSZMNJ");       // 159
            dt1MES.Columns.Add("JSZMGXS");      // 160
            dt1MES.Columns.Add("JSZMCO2ND");    // 161
            dt1MES.Columns.Add("JSZMNOND");     // 162
            dt1MES.Columns.Add("otestdate");    // 163
            dt1MES.Columns.Add("leacmax");      // 164
            dt1MES.Columns.Add("leacmin");      // 165

            DataRow dr = dt1MES.NewRow();
            dr["SBFLAG"] = "OBD";
            dr["VIN"] = strVIN;
            string strNowDateTime = DateTime.Now.ToLocalTime().ToString("yyyyMMdd");
            int iSN = GetSN(strNowDateTime);
            ++iSN;
            iSN %= 10000;
            dr["TestNo"] = "XC" + NormalizeCompanyCode(_obdIfEx.OBDResultSetting.CompanyCode) + strNowDateTime + iSN.ToString("d4");
            DbNative.SetSN(strNowDateTime + "," + iSN.ToString());
            dr["TestType"] = "0";
            dr["APASS"] = "1";
            dr["OPASS"] = strOBDResult;
            dr["Result"] = strOBDResult;
            dr["otestdate"] = strNowDateTime;
            dt1MES.Rows.Add(dr);
        }

        /// <summary>
        /// 处理企业编号使之长度符合4位：
        /// 1、原始编号长度大于4的话截取前4位
        /// 2、原始编号长度小于4的话在左边补‘0’
        /// </summary>
        /// <param name="strCode"></param>
        /// <returns></returns>
        private string NormalizeCompanyCode(string strCode) {
            string strRet = strCode;
            if (strRet.Length > 4) {
                strRet = strRet.Substring(0, 4);
            } else if (strRet.Length < 4) {
                strRet = new string('0', 4 - strRet.Length) + strRet;
            }
            return strRet;
        }

        private string GetModuleID(string ECUAcronym, string ECUID) {
            string moduleID = ECUAcronym;
            if (_obdIfEx.OBDResultSetting.UseECUAcronym) {
                if (moduleID.Length == 0 || moduleID == "不适用") {
                    moduleID = ECUID;
                }
            } else {
                moduleID = ECUID;
            }
            return moduleID;
        }

        private void DataTable2MESAddRow(DataTable dt2MES, DataTable dtIn, int iRow, string ECUAcronym, string CALID, string CVN) {
            string moduleID = GetModuleID(ECUAcronym, dtIn.Rows[iRow][1].ToString());
            // OBD型式和ODO只用第一个ECU（即7E8，ECM）的数据上传
            string OBD_SUP = dtIn.Rows[0][4].ToString().Split(',')[0].Replace("不适用", "");
            string ODO = dtIn.Rows[0][5].ToString().Replace("不适用", "");
            // 限制上传字段长度，避免上传错误数据导致上传出错
            if (OBD_SUP.Length >= 2) {
                OBD_SUP = OBD_SUP.Substring(0, 2);
            }
            if (ODO.Length >= 8) {
                ODO = ODO.Substring(0, 8);
            }
            if (moduleID.Length >= 11) {
                moduleID = moduleID.Substring(0, 11);
            }
            if (CALID.Length >= 20) {
                CALID = CALID.Substring(0, 20);
            }
            if (CVN.Length >= 20) {
                CVN = CVN.Substring(0, 20);
            }
            dt2MES.Rows.Add(OBD_SUP, ODO, moduleID, CALID, CVN);
        }

        private void SetDataTable2MES(DataTable dt2MES, DataTable dtIn) {
            dt2MES.Columns.Add("obd");
            dt2MES.Columns.Add("odo");
            dt2MES.Columns.Add("ModuleID");
            dt2MES.Columns.Add("CALID");
            dt2MES.Columns.Add("CVN");
            for (int i = 0; i < dtIn.Rows.Count; i++) {
                string ECUAcronym = dtIn.Rows[i][25].ToString().Split('-')[0];
                if (_CN6) {
                    string[] CALIDArray = dtIn.Rows[i][26].ToString().Split(',');
                    string[] CVNArray = dtIn.Rows[i][27].ToString().Split(',');
                    int length = Math.Max(CALIDArray.Length, CVNArray.Length);
                    DataTable2MESAddRow(dt2MES, dtIn, i, ECUAcronym, CALIDArray[0], CVNArray[0]);
                    for (int j = 1; j < length; j++) {
                        string CALID = CALIDArray.Length > j ? CALIDArray[j] : "";
                        string CVN = CVNArray.Length > j ? CVNArray[j] : "";
                        // 多个CALID和CVN使用同一个ModuleID上传
                        DataTable2MESAddRow(dt2MES, dtIn, i, ECUAcronym, CALID, CVN);
                    }
                } else {
                    string CALID = dtIn.Rows[i][26].ToString().Replace(",", "").Replace("不适用", "");
                    if (CALID.Length > 20) {
                        CALID = CALID.Substring(0, 20);
                    }
                    string CVN = dtIn.Rows[i][27].ToString().Replace(",", "").Replace("不适用", "");
                    if (CVN.Length > 20) {
                        CVN = CVN.Substring(0, 20);
                    }
                    DataTable2MESAddRow(dt2MES, dtIn, i, ECUAcronym, CALID, CVN);
                }
            }
        }

        private void SetDataTableColumnsFromDB(DataTable dtDisplay, DataTable dtIn) {
            dtDisplay.Clear();
            dtDisplay.Columns.Clear();
            if (dtIn.Rows.Count > 0) {
                dtDisplay.Columns.Add(new DataColumn("NO", typeof(int)));
                dtDisplay.Columns.Add(new DataColumn("Item", typeof(string)));
                for (int i = 0; i < dtIn.Rows.Count; i++) {
                    dtDisplay.Columns.Add(new DataColumn(dtIn.Rows[i][1].ToString(), typeof(string)));
                }
            } else {
                SetDataTableColumnsErrorEventArgs args = new SetDataTableColumnsErrorEventArgs {
                    ErrorMsg = "从数据库中未获取到数据"
                };
                SetDataTableColumnsError?.Invoke(this, args);
            }
        }

        private void SetDataRowInfoFromDB(int lineNO, string strItem, DataTable dtIn) {
            DataRow dr = _dtInfo.NewRow();
            dr[0] = lineNO;
            dr[1] = strItem;
            for (int i = 0; i < dtIn.Rows.Count; i++) {
                if (GetCompIgn(dtIn) && lineNO > 10) {
                    dr[i + 2] = dtIn.Rows[i][lineNO + (lineNO == 16 ? 3 : 9)];
                } else {
                    if (strItem.Contains("DTC")) {
                        dr[i + 2] = dtIn.Rows[i][lineNO + 1].ToString().Replace(",", "\n");
                    } else {
                        dr[i + 2] = dtIn.Rows[i][lineNO + 1];
                    }
                }
            }
            _dtInfo.Rows.Add(dr);
        }

        private void SetDataRowECUInfoFromDB(int lineNO, string strItem, DataTable dtIn) {
            DataRow dr = _dtECUInfo.NewRow();
            dr[0] = lineNO;
            dr[1] = strItem;
            for (int i = 0; i < dtIn.Rows.Count; i++) {
                if (lineNO == 1) {
                    dr[i + 2] = dtIn.Rows[i][lineNO - 1];
                } else {
                    dr[i + 2] = dtIn.Rows[i][lineNO + 23].ToString().Replace(",", "\n");
                }
            }
            _dtECUInfo.Rows.Add(dr);
        }

        private void SetDataRowIUPRFromDB(int lineNO, string strItem, int padTotal, int padNum, DataTable dtIn, bool bCompIgn) {
            double[] nums = new double[dtIn.Rows.Count];
            double[] dens = new double[dtIn.Rows.Count];
            int colIndex;
            if (bCompIgn) {
                colIndex = (lineNO + 11) * 2;
            } else {
                colIndex = lineNO * 2;
            }
            DataRow dr = _dtIUPR.NewRow();
            dr[0] = lineNO;
            if (dr[1].ToString().Length == 0) {
                dr[1] = strItem + ": " + "监测完成次数".PadLeft(padTotal - padNum + 6);
            }
            for (int i = 0; i < dtIn.Rows.Count; i++) {
                dr[i + 2] = dtIn.Rows[i][colIndex].ToString();
                int.TryParse(dtIn.Rows[i][colIndex].ToString(), out int temp);
                nums[i] = temp;
            }
            _dtIUPR.Rows.Add(dr);

            dr = _dtIUPR.NewRow();
            if (dr[1].ToString().Length == 0) {
                dr[1] = "符合监测条件次数".PadLeft(padTotal + 8);
            }
            for (int i = 0; i < dtIn.Rows.Count; i++) {
                dr[i + 2] = dtIn.Rows[i][colIndex + 1].ToString();
                int.TryParse(dtIn.Rows[i][colIndex + 1].ToString(), out int temp);
                dens[i] = temp;
            }
            _dtIUPR.Rows.Add(dr);

            dr = _dtIUPR.NewRow();
            if (dr[1].ToString().Length == 0) {
                dr[1] = "IUPR率".PadLeft(padTotal + 5);
            }
            for (int i = 0; i < dtIn.Rows.Count; i++) {
                if (dens[i] == 0) {
                    dr[i + 2] = "7.99527";
                } else {
                    double r = Math.Round(nums[i] / dens[i], 6);
                    if (r > 7.99527) {
                        dr[i + 2] = "7.99527";
                    } else {
                        dr[i + 2] = r.ToString();
                    }
                }
            }
            _dtIUPR.Rows.Add(dr);
        }

        private void SetDataTableInfoFromDB(DataTable dtIn) {
            if (_dtInfo.Columns.Count <= 0) {
                return;
            }
            int NO = 0;
            SetDataRowInfoFromDB(++NO, "MIL状态", dtIn);                // 1,2
            SetDataRowInfoFromDB(++NO, "MIL亮后行驶里程（km）", dtIn);   // 2,3
            SetDataRowInfoFromDB(++NO, "OBD型式检验类型", dtIn);         // 3,4
            SetDataRowInfoFromDB(++NO, "总累积里程ODO（km）", dtIn);     // 4,5
            SetDataRowInfoFromDB(++NO, "存储DTC", dtIn);                // 5,6
            SetDataRowInfoFromDB(++NO, "未决DTC", dtIn);                // 6,7
            SetDataRowInfoFromDB(++NO, "永久DTC", dtIn);                // 7,8
            SetDataRowInfoFromDB(++NO, "失火监测", dtIn);               // 8,9
            SetDataRowInfoFromDB(++NO, "燃油系统监测", dtIn);           // 9,10
            SetDataRowInfoFromDB(++NO, "综合组件监测", dtIn);           // 10,11
            if (GetCompIgn(dtIn)) {
                SetDataRowInfoFromDB(++NO, "NMHC催化剂监测", dtIn);     // 11,20
                SetDataRowInfoFromDB(++NO, "NOx/SCR后处理监测", dtIn);  // 12,21
                SetDataRowInfoFromDB(++NO, "增压系统监测", dtIn);       // 13,22
                SetDataRowInfoFromDB(++NO, "排气传感器监测", dtIn);     // 14,23
                SetDataRowInfoFromDB(++NO, "PM过滤器监测", dtIn);       // 15,24
            } else {
                SetDataRowInfoFromDB(++NO, "催化剂监测", dtIn);         // 11,12
                SetDataRowInfoFromDB(++NO, "加热催化剂监测", dtIn);     // 12,13
                SetDataRowInfoFromDB(++NO, "燃油蒸发系统监测", dtIn);   // 13,14
                SetDataRowInfoFromDB(++NO, "二次空气系统监测", dtIn);   // 14,15
                SetDataRowInfoFromDB(++NO, "空调系统制冷剂监测", dtIn); // 15,16
                SetDataRowInfoFromDB(++NO, "氧气传感器监测", dtIn);     // 16,17
                SetDataRowInfoFromDB(++NO, "加热氧气传感器监测", dtIn); // 17,18
            }
            SetDataRowInfoFromDB(++NO, "EGR/VVT系统监测", dtIn);       // 16,19 / 18,19
        }

        private void SetDataTableECUInfoFromDB(DataTable dtIn) {
            if (_dtInfo.Columns.Count <= 0) {
                return;
            }
            int NO = 0;
            SetDataRowECUInfoFromDB(++NO, "VIN", dtIn);               // 1,0
            SetDataRowECUInfoFromDB(++NO, "ECU名称", dtIn);           // 2,25
            SetDataRowECUInfoFromDB(++NO, "CAL_ID", dtIn);            // 3,26
            SetDataRowECUInfoFromDB(++NO, "CVN", dtIn);               // 4,27
        }

        private void SetDataTableIUPRFromDB(DataTable dtIn) {
            if (_dtIUPR.Columns.Count <= 0) {
                return;
            }
            int NO = 0;
            bool bCompIgnIUPR = GetCompIgnIUPR(dtIn);
            if (bCompIgnIUPR) {
                // 压缩点火
                SetDataRowIUPRFromDB(++NO, "NMHC催化器", 18, 12, dtIn, bCompIgnIUPR);
                SetDataRowIUPRFromDB(++NO, "NOx催化器", 18, 11, dtIn, bCompIgnIUPR);
                SetDataRowIUPRFromDB(++NO, "NOx吸附器", 18, 11, dtIn, bCompIgnIUPR);
                SetDataRowIUPRFromDB(++NO, "PM捕集器", 18, 10, dtIn, bCompIgnIUPR);
                SetDataRowIUPRFromDB(++NO, "废气传感器", 18, 12, dtIn, bCompIgnIUPR);
                SetDataRowIUPRFromDB(++NO, "EGR和VVT", 18, 10, dtIn, bCompIgnIUPR);
                SetDataRowIUPRFromDB(++NO, "增压压力", 18, 10, dtIn, bCompIgnIUPR);
            } else {
                // 火花点火
                NO = 0;
                SetDataRowIUPRFromDB(++NO, "催化器 组1", 18, 12, dtIn, bCompIgnIUPR);
                SetDataRowIUPRFromDB(++NO, "催化器 组2", 18, 12, dtIn, bCompIgnIUPR);
                SetDataRowIUPRFromDB(++NO, "前氧传感器 组1", 18, 16, dtIn, bCompIgnIUPR);
                SetDataRowIUPRFromDB(++NO, "前氧传感器 组2", 18, 16, dtIn, bCompIgnIUPR);
                SetDataRowIUPRFromDB(++NO, "后氧传感器 组1", 18, 16, dtIn, bCompIgnIUPR);
                SetDataRowIUPRFromDB(++NO, "后氧传感器 组2", 18, 16, dtIn, bCompIgnIUPR);
                SetDataRowIUPRFromDB(++NO, "EVAP", 18, 6, dtIn, bCompIgnIUPR);
                SetDataRowIUPRFromDB(++NO, "EGR和VVT", 18, 10, dtIn, bCompIgnIUPR);
                SetDataRowIUPRFromDB(++NO, "GPF 组1", 18, 9, dtIn, bCompIgnIUPR);
                SetDataRowIUPRFromDB(++NO, "GPF 组2", 18, 9, dtIn, bCompIgnIUPR);
                SetDataRowIUPRFromDB(++NO, "二次空气喷射系统", 18, 18, dtIn, bCompIgnIUPR);
            }
        }

        public void UploadDataFromDB(string strVIN, out string errorMsg, bool bOnlyShowData) {
            errorMsg = "";
            DataTable dt = new DataTable("OBDData");
            DbNative.GetEmptyTable(dt);
            dt.Columns.Remove("ID");
            dt.Columns.Remove("WriteTime");
            Dictionary<string, string> whereDic = new Dictionary<string, string> { { "VIN", strVIN } };
            DbNative.GetRecords(dt, whereDic);
            SetDataTableColumnsFromDB(_dtInfo, dt);
            SetDataTableColumnsFromDB(_dtECUInfo, dt);

            DataTable dtIUPR = new DataTable("OBDIUPR");
            DbNative.GetEmptyTable(dtIUPR);
            dtIUPR.Columns.Remove("ID");
            dtIUPR.Columns.Remove("WriteTime");
            DbNative.GetRecords(dtIUPR, whereDic);
            SetDataTableColumnsFromDB(_dtIUPR, dtIUPR);

            SetupColumnsDone?.Invoke();
            SetDataTableInfoFromDB(dt);
            SetDataTableECUInfoFromDB(dt);
            SetDataTableIUPRFromDB(dtIUPR);
            if (bOnlyShowData) {
                _obdIfEx.Log.TraceInfo("Only show data from database");
                NotUploadData?.Invoke();
                dt.Dispose();
                return;
            }
            if (dt.Rows.Count > 0) {
                if (!_obdIfEx.OBDResultSetting.UploadWhenever && dt.Rows[0]["Result"].ToString() != "1") {
                    _obdIfEx.Log.TraceWarning("Won't upload data from database because OBD test result is NOK");
                    NotUploadData?.Invoke();
                    dt.Dispose();
                    return;
                }
                try {
                    UploadData(strVIN, dt.Rows[0]["Result"].ToString(), dt, ref errorMsg);
                } catch (Exception ex) {
                    string strMsg = "Wrong record: VIN = " + strVIN + ", OBDResult = " + dt.Rows[0]["Result"].ToString() + ", ";
                    for (int i = 0; i < dt.Rows.Count; i++) {
                        strMsg += "ECU_ID = " + dt.Rows[i]["ECU_ID"] + ", ";
                        strMsg += "OBD_SUP = " + dt.Rows[i]["OBD_SUP"] + ", ";
                        strMsg += "ODO = " + dt.Rows[i]["ODO"] + ", ";
                        strMsg += "ECU_NAME = " + dt.Rows[i]["ECU_NAME"] + ", ";
                        strMsg += "CAL_ID = " + dt.Rows[i]["CAL_ID"] + ", ";
                        strMsg += "CVN = " + dt.Rows[i]["CVN"] + " || ";
                    }
                    strMsg = strMsg.Substring(0, strMsg.Length - 4);
                    _obdIfEx.Log.TraceError("Manual Upload Data Failed: " + ex.Message + (errorMsg.Length > 0 ? ", " + errorMsg : ""));
                    _obdIfEx.Log.TraceError(strMsg);
                    dt.Dispose();
                    throw;
                }
            }
            dt.Dispose();
        }

        private void CopyTableColumns(DataTable dtOut, DataTable dtIn) {
            for (int i = 0; i < dtIn.Columns.Count; i++) {
                dtOut.Columns.Add(dtIn.Columns[i].ColumnName, dtIn.Columns[i].DataType);
            }
        }

        private List<DataTable> SplitResultsPerVIN(DataTable dtIn) {
            List<DataTable> rets = new List<DataTable>();
            DataTable dtTemp = new DataTable("__");
            CopyTableColumns(dtTemp, dtIn);
            string VIN = string.Empty;
            for (int i = 0; i < dtIn.Rows.Count; i++) {
                if (dtIn.Rows[i]["VIN"].ToString() == VIN) {
                    DataRow dr = dtTemp.NewRow();
                    for (int j = 0; j < dtIn.Columns.Count; j++) {
                        dr[j] = dtIn.Rows[i][j];
                    }
                    dtTemp.Rows.Add(dr);
                } else {
                    VIN = dtIn.Rows[i]["VIN"].ToString();
                    if (dtTemp.TableName != "__") {
                        rets.Add(dtTemp);
                    }
                    dtTemp = new DataTable(dtIn.TableName);
                    CopyTableColumns(dtTemp, dtIn);
                    DataRow dr = dtTemp.NewRow();
                    for (int j = 0; j < dtIn.Columns.Count; j++) {
                        dr[j] = dtIn.Rows[i][j];
                    }
                    dtTemp.Rows.Add(dr);
                }
            }
            rets.Add(dtTemp);
            return rets;
        }

        public void UploadDataFromDBOnTime(out string errorMsg) {
            errorMsg = "";
            DataTable dtTemp = new DataTable("OBDData");
            DbNative.GetEmptyTable(dtTemp);
            dtTemp.Columns.Remove("ID");
            dtTemp.Columns.Remove("WriteTime");

            Dictionary<string, string> whereDic = new Dictionary<string, string> { { "Upload", "0" } };
            DbNative.GetRecords(dtTemp, whereDic);
            if (dtTemp.Rows.Count <= 0) {
                dtTemp.Dispose();
                return;
            }
            List<DataTable> dts = SplitResultsPerVIN(dtTemp);
            for (int i = 0; i < dts.Count; i++) {
                if (!_obdIfEx.OBDResultSetting.UploadWhenever && dts[i].Rows[0]["Result"].ToString() != "1") {
                    continue;
                }
                try {
                    UploadData(dts[i].Rows[0]["VIN"].ToString(), dts[i].Rows[0]["Result"].ToString(), dts[i], ref errorMsg, false);
                } catch (Exception ex) {
                    string strMsg = "Wrong record: VIN = " + dts[i].Rows[0][0].ToString() + ", OBDResult = " + dts[i].Rows[0]["Result"].ToString() + " [";
                    for (int j = 0; j < dts[i].Rows.Count; j++) {
                        strMsg += "ECU_ID = " + dts[i].Rows[j]["ECU_ID"] + ", ";
                        strMsg += "OBD_SUP = " + dts[i].Rows[j]["OBD_SUP"] + ", ";
                        strMsg += "ODO = " + dts[i].Rows[j]["ODO"] + ", ";
                        strMsg += "ECU_NAME = " + dts[i].Rows[j]["ECU_NAME"] + ", ";
                        strMsg += "CAL_ID = " + dts[i].Rows[j]["CAL_ID"] + ", ";
                        strMsg += "CVN = " + dts[i].Rows[j]["CVN"] + " || ";
                    }
                    strMsg = strMsg.Substring(0, strMsg.Length - 4) + "]";
                    _obdIfEx.Log.TraceError("Upload Data OnTime Failed: " + ex.Message + (errorMsg.Length > 0 ? ", " + errorMsg : ""));
                    _obdIfEx.Log.TraceError(strMsg);
                    continue;
                }
            }
            dtTemp.Dispose();
        }

        private bool GetCompIgn(DataTable dtIn) {
            bool compIgn = true;
            compIgn = compIgn && dtIn.Rows[0]["CAT_RDY"].ToString() == "不适用";
            compIgn = compIgn && dtIn.Rows[0]["HCAT_RDY"].ToString() == "不适用";
            compIgn = compIgn && dtIn.Rows[0]["EVAP_RDY"].ToString() == "不适用";
            compIgn = compIgn && dtIn.Rows[0]["AIR_RDY"].ToString() == "不适用";
            compIgn = compIgn && dtIn.Rows[0]["ACRF_RDY"].ToString() == "不适用";
            compIgn = compIgn && dtIn.Rows[0]["O2S_RDY"].ToString() == "不适用";
            compIgn = compIgn && dtIn.Rows[0]["HTR_RDY"].ToString() == "不适用";
            return compIgn;
        }

        private bool GetCompIgnIUPR(DataTable dtIn) {
            bool compIgnIUPR = true;
            compIgnIUPR = compIgnIUPR && dtIn.Rows[0]["HCCATCOMP"].ToString() == "-1";
            compIgnIUPR = compIgnIUPR && dtIn.Rows[0]["NCATCOMP"].ToString() == "-1";
            compIgnIUPR = compIgnIUPR && dtIn.Rows[0]["NADSCOMP"].ToString() == "-1";
            compIgnIUPR = compIgnIUPR && dtIn.Rows[0]["PMCOMP"].ToString() == "-1";
            compIgnIUPR = compIgnIUPR && dtIn.Rows[0]["EGSCOMP"].ToString() == "-1";
            compIgnIUPR = compIgnIUPR && dtIn.Rows[0]["EGRCOMP_0B"].ToString() == "-1";
            compIgnIUPR = compIgnIUPR && dtIn.Rows[0]["BPCOMP"].ToString() == "-1";
            return !compIgnIUPR;
        }

        private void ExportResultFile(DataTable dt) {
            string OriPath = ".\\Configs\\OBD_Result.xlsx";
            string ExportPath = ".\\Export\\" + DateTime.Now.ToLocalTime().ToString("yyyy-MM");
            if (!Directory.Exists(ExportPath)) {
                Directory.CreateDirectory(ExportPath);
            }
            ExportPath += "\\" + StrVIN_IN + "_" + DateTime.Now.ToLocalTime().ToString("yyyyMMdd-HHmmss") + ".xlsx";
            FileInfo fileInfo = new FileInfo(OriPath);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (ExcelPackage package = new ExcelPackage(fileInfo, true)) {
                ExcelWorksheet worksheet1 = package.Workbook.Worksheets[0];
                // VIN
                worksheet1.Cells["B2"].Value = dt.Rows[0][0].ToString();

                // CALID, CVN
                if (_CN6) {
                    string[] CALIDArray = dt.Rows[0][26].ToString().Split(',');
                    string[] CVNArray = dt.Rows[0][27].ToString().Split(',');
                    int length = Math.Max(CALIDArray.Length, CVNArray.Length);
                    for (int i = 0; i < length; i++) {
                        worksheet1.Cells[3 + i, 2].Value = CALIDArray.Length > i ? CALIDArray[i] : "";
                        worksheet1.Cells[3 + i, 4].Value = CVNArray.Length > i ? CVNArray[i] : "";
                    }
                    for (int i = 1; i < dt.Rows.Count; i++) {
                        worksheet1.Cells["B5"].Value = dt.Rows[i][26].ToString().Replace(",", "\n");
                        worksheet1.Cells["D5"].Value = dt.Rows[i][27].ToString().Replace(",", "\n");
                    }
                } else {
                    string CALID = dt.Rows[0][26].ToString().Replace(",", "");
                    if (CALID.Length > 20) {
                        CALID = CALID.Substring(0, 20);
                    }
                    string CVN = dt.Rows[0][27].ToString().Replace(",", "");
                    if (CVN.Length > 20) {
                        CVN = CVN.Substring(0, 20);
                    }
                    worksheet1.Cells["B3"].Value = CALID;
                    worksheet1.Cells["D3"].Value = CVN;

                    for (int i = 1; i < dt.Rows.Count; i++) {
                        CALID = dt.Rows[i][26].ToString().Replace(",", "");
                        if (CALID.Length > 20) {
                            CALID = CALID.Substring(0, 20);
                        }
                        CVN = dt.Rows[i][27].ToString().Replace(",", "");
                        if (CVN.Length > 20) {
                            CVN = CVN.Substring(0, 20);
                        }
                        worksheet1.Cells[3 + i, 2].Value = CALID;
                        worksheet1.Cells[3 + i, 4].Value = CVN;
                    }
                }

                // moduleID
                string moduleID = GetModuleID(dt.Rows[0][25].ToString().Split('-')[0], dt.Rows[0][1].ToString());
                worksheet1.Cells["E3"].Value = moduleID;
                worksheet1.Cells["B4"].Value += "";
                worksheet1.Cells["D4"].Value += "";

                string OtherID = "";
                if (_CN6) {
                    if (worksheet1.Cells["B4"].Value.ToString().Length > 0 || worksheet1.Cells["D4"].Value.ToString().Length > 0) {
                        worksheet1.Cells["E4"].Value = moduleID;
                    }
                    for (int i = 1; i < dt.Rows.Count; i++) {
                        moduleID = GetModuleID(dt.Rows[i][25].ToString().Split('-')[0], dt.Rows[i][1].ToString());
                        OtherID += "," + moduleID;
                    }
                    worksheet1.Cells["E5"].Value = OtherID.Trim(',');
                } else {
                    if (dt.Rows.Count > 1) {
                        moduleID = GetModuleID(dt.Rows[1][25].ToString().Split('-')[0], dt.Rows[1][1].ToString());
                        worksheet1.Cells["E4"].Value = moduleID;
                        if (dt.Rows.Count > 2) {
                            for (int i = 2; i < dt.Rows.Count; i++) {
                                moduleID = GetModuleID(dt.Rows[i][25].ToString().Split('-')[0], dt.Rows[i][1].ToString());
                                OtherID += "," + moduleID;
                            }
                            worksheet1.Cells["E5"].Value = OtherID.Trim(',');
                        }
                    }
                }

                // 外观检验结果
                worksheet1.Cells["B7"].Value = "合格";

                // OBD型式检验要求
                worksheet1.Cells["B9"].Value = dt.Rows[0][4].ToString();

                // 总累积里程ODO（km）
                worksheet1.Cells["B10"].Value = dt.Rows[0][5].ToString();

                // 与OBD诊断仪通讯情况
                worksheet1.Cells["B11"].Value = "通讯成功";

                // 检测结果
                string Result = OBDResult ? "合格" : "不合格";
                //Result += DTCResult ? "" : "\n有DTC";
                //Result += ReadinessResult ? "" : "\n就绪状态未完成项超过2项";
                Result += VINResult ? "" : "\nVIN号不匹配";
                Result += CALIDCVNResult ? "" : "\nCALID和CVN数据不完整";
                Result += CALIDUnmeaningResult ? "" : "\nCALID含有乱码";
                Result += OBDSUPResult ? "" : "\nOBD型式不适用或异常";
                worksheet1.Cells["B12"].Value = Result;

                // 检验员
                worksheet1.Cells["E13"].Value = _obdIfEx.MainSettings.TesterName;

                byte[] bin = package.GetAsByteArray();
                FileInfo exportFileInfo = new FileInfo(ExportPath);
                File.WriteAllBytes(exportFileInfo.FullName, bin);
            }
        }

    }

    public class SetDataTableColumnsErrorEventArgs : EventArgs {
        public string ErrorMsg { get; set; }
    }

}
