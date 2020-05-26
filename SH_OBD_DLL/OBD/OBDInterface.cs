using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace SH_OBD {
    public class OBDInterface {
        private const string m_settings_xml = ".\\Configs\\dllsetting.xml";

        public delegate void __Delegate_OnConnect();
        public delegate void __Delegate_OnDisconnect();
        public event __Delegate_OnDisconnect OnDisconnect;
        public event __Delegate_OnConnect OnConnect;

        private OBDDevice m_obdDevice;
        private readonly OBDInterpreter m_obdInterpreter;
        private List<DTC> m_listDTC;
        private readonly List<OBDParameter> m_listAllParameters;
        private readonly List<OBDParameter> m_listSupportedParameters;
        private readonly int[] m_xattr;

        public Logger Log { get; }
        public LoadConfigResult ConfigResult { get; private set; }
        public Settings CommSettings { get; private set; }
        public StandardType STDType { get; set; }

        public OBDInterface() {
            Log = new Logger("./log/OBD", EnumLogLevel.LogLevelAll, true, 100);
            Log.TraceInfo("==================================================================");
            Log.TraceInfo("==================== START DllVersion: " + DllVersion<OBDInterface>.AssemblyVersion + " ====================");
            m_listAllParameters = new List<OBDParameter>();
            m_listSupportedParameters = new List<OBDParameter>();
            m_obdInterpreter = new OBDInterpreter();
            ConfigResult = LoadConfigResult.Success;
            CommSettings = LoadCommSettings();
            LoadDTCDefinitions(".\\Configs\\dtc.xml");
            string[] strAttr = CommSettings.AutoProtocolOrder.Split(',');
            m_xattr = new int[strAttr.Length];
            for (int i = 0; i < strAttr.Length; i++) {
                int.TryParse(strAttr[i], out m_xattr[i]);
            }
            SetDevice(HardwareType.ELM327);
            Disconnect();
            STDType = StandardType.Automatic;
        }

        public bool ConnectedStatus {
            get { return m_obdDevice.GetConnected(); }
        }

        public HardwareType GetDevice() {
            return CommSettings.HardwareIndex;
        }

        public string GetDeviceDesString() {
            return m_obdDevice.DeviceDesString();
        }

        public string GetDeviceIDString() {
            return m_obdDevice.DeviceIDString();
        }

        public ProtocolType GetProtocol() {
            return CommSettings.ProtocolIndex;
        }

        public StandardType GetStandard() {
            return CommSettings.StandardIndex;
        }

        public void SetTimeout(int iTimeout = 500) {
            m_obdDevice.SetTimeout(iTimeout);
        }

        public bool InitDevice(bool bGetPIDStatus = true) {
            bool flag;
            SetDevice(CommSettings.HardwareIndex);
            if (bGetPIDStatus) {
                flag = m_obdDevice.Initialize(CommSettings) && InitOBD();
            } else {
                flag = m_obdDevice.Initialize(CommSettings);
            }
            STDType = m_obdDevice.GetStandardType();
            if (flag) {
                m_obdDevice.SetConnected(true);
                OnConnect?.Invoke();
                Log.TraceInfo("Connection Established!");
                return true;
            } else {
                m_obdDevice.SetConnected(false);
                Log.TraceWarning("Failed to find a compatible OBD-II interface.");
                return false;
            }
        }

        public bool InitDeviceAuto(bool bGetPIDStatus = true) {
            Log.TraceInfo("Beginning AUTO initialization...");
            string order = "";
            foreach (int item in m_xattr) {
                order += "," + item.ToString();
            }
            order = order.TrimStart(',');
            Log.TraceInfo("Auto Protocol Order: " + order);
            Log.TraceInfo("Application Layer Protocol: " + STDType.ToString());

            SetDevice(HardwareType.ELM327);
            bool flag;
            CommSettings.StandardIndex = STDType;
            if (bGetPIDStatus) {
                flag = m_obdDevice.InitializeAuto(CommSettings) && InitOBD();
            } else {
                flag = m_obdDevice.InitializeAuto(CommSettings);
            }
            STDType = m_obdDevice.GetStandardType();
            if (flag) {
                CommSettings.ProtocolIndex = m_obdDevice.GetProtocolType();
                CommSettings.ComPort = m_obdDevice.GetComPortIndex();
                SaveCommSettings(CommSettings);
                m_obdDevice.SetConnected(true);
                OnConnect?.Invoke();
                return true;
            }
            m_obdDevice.SetConnected(false);
            return false;
        }

        public bool InitOBD() {
            bool bRet = true;
            // 获取ISO15031 Mode01 PID支持情况
            OBDParameter param = new OBDParameter(1, 0, 0) {
                ValueTypes = 32
            };
            m_listSupportedParameters.Clear();

            for (int i = 0; (i * 0x20) < 0x100; i++) {
                param.Parameter = i * 0x20;
                OBDParameterValue value = GetValue(param);
                if (value.ErrorDetected) {
                    bRet = false;
                    break;
                }
                foreach (OBDParameter param2 in m_listAllParameters) {
                    if (param2.Parameter > 2 && param2.Parameter > (i * 0x20) && param2.Parameter < ((i + 1) * 0x20) && value.GetBitFlag(param2.Parameter - param.Parameter - 1)) {
                        m_listSupportedParameters.Add(param2);
                    }
                }
                if (!value.GetBitFlag(31)) {
                    break;
                }
            }

            if (!bRet) {
                // 获取ISO27145 PID支持情况
                bRet = true;
                param = new OBDParameter(0x22, 0, 0) {
                    ValueTypes = 32
                };
                m_listSupportedParameters.Clear();

                for (int i = 0; (i * 0x20) < 0x100; i++) {
                    param.Parameter = 0xF400 + i * 0x20;
                    OBDParameterValue value = GetValue(param);
                    if (value.ErrorDetected) {
                        bRet = false;
                        break;
                    }
                    foreach (OBDParameter param2 in m_listAllParameters) {
                        if (param2.Parameter > 2 && param2.Parameter > (i * 0x20) && param2.Parameter < ((i + 1) * 0x20) && value.GetBitFlag(param2.Parameter - param.Parameter - 1)) {
                            m_listSupportedParameters.Add(param2);
                        }
                    }
                    if (!value.GetBitFlag(31)) {
                        break;
                    }
                }
            }

            if (!bRet) {
                // 获取J1939 DM5支持情况，J1939只测试能否连接车辆，不获取PID信息
                bRet = true;
                param = new OBDParameter(0, 0xFECE, 0);
                OBDParameterValue value = GetValue(param);
                if (value.ErrorDetected) {
                    bRet = false;
                }
            }
            return bRet;
        }

        public bool IsParameterSupported(string strPID) {
            foreach (OBDParameter param in m_listSupportedParameters) {
                if (param.PID.CompareTo(strPID) == 0) {
                    return true;
                }
            }
            return false;
        }

        public OBDParameterValue GetValue(string strPID, bool bEnglishUnits = false) {
            OBDParameter obdParameter = LookupParameter(strPID);
            if (obdParameter != null) {
                return GetValue(obdParameter, bEnglishUnits);
            }

            OBDParameterValue value = new OBDParameterValue {
                ErrorDetected = true
            };
            return value;
        }

        public OBDParameterValue GetValue(OBDParameter param, bool bEnglishUnits = false) {
            if (param.PID.Length > 0) {
                Log.TraceInfo("Requesting: " + param.PID);
            } else {
                Log.TraceInfo("Requesting: " + param.OBDRequest);
            }
            if (param.Service == 0 && param.Parameter == 0) {
                return SpecialValue(param);
            }

            OBDResponseList responses = m_obdDevice.Query(param);
            string strItem = "Responses: ";
            if (responses.ErrorDetected) {
                strItem += "Error Detected!";
                Log.TraceInfo(strItem);
                return new OBDParameterValue { ErrorDetected = true };
            } else {
                for (int i = 0; i < responses.ResponseCount; i++) {
                    strItem += string.Format("[{0}] ", Utility.GetReadableHexString(0, responses.GetOBDResponse(i).Data));
                }
            }
            strItem = strItem.TrimEnd();
            Log.TraceInfo(strItem);
            OBDParameterValue obdValue = m_obdInterpreter.GetValue(param, responses, bEnglishUnits);
            if (obdValue.ErrorDetected) {
                Log.TraceError("Error Detected in OBDParameterValue!");
            } else {
                Log.TraceInfo(GetLogString(param, obdValue));
            }
            return obdValue;
        }

        public List<OBDParameterValue> GetValueList(OBDParameter param, bool bEnglishUnits = false) {
            List<OBDParameterValue> ValueList = new List<OBDParameterValue>();

            if (param.PID.Length > 0) {
                Log.TraceInfo("Requesting: " + param.PID);
            } else {
                Log.TraceInfo("Requesting: " + param.OBDRequest);
            }
            OBDResponseList responses = m_obdDevice.Query(param);
            string strItem = "Responses: ";
            if (responses.ErrorDetected) {
                strItem += "Error Detected!";
                OBDParameterValue value = new OBDParameterValue {
                    ErrorDetected = true,
                    StringValue = "Error Detected in OBDResponseList!",
                    ShortStringValue = "ERROR_RESP"
                };
                ValueList.Add(value);
                Log.TraceInfo(strItem);
                return ValueList;
            } else {
                for (int i = 0; i < responses.ResponseCount; i++) {
                    strItem += string.Format("[{0}] ", Utility.GetReadableHexString(0, responses.GetOBDResponse(i).Data));
                }
                strItem = strItem.TrimEnd();
                Log.TraceInfo(strItem);
            }

            for (int i = 0; i < responses.ResponseCount; i++) {
                OBDParameterValue obdValue = m_obdInterpreter.GetValue(param, responses.GetOBDResponse(i), bEnglishUnits);
                if (obdValue.ErrorDetected) {
                    Log.TraceError("Error Detected in OBDParameterValue!");
                } else {
                    Log.TraceInfo(GetLogString(param, obdValue));
                }
                ValueList.Add(obdValue);
            }
            return ValueList;
        }

        private string GetLogString(OBDParameter param, OBDParameterValue obdValue) {
            string strRet = "Values: ";
            if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.Double) == (int)OBDParameter.EnumValueTypes.Double) {
                strRet += string.Format("[Double: {0}] ", obdValue.DoubleValue.ToString());
            }
            if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.Bool) == (int)OBDParameter.EnumValueTypes.Bool) {
                strRet += string.Format("[Bool: {0}] ", obdValue.BoolValue.ToString());
            }
            if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.String) == (int)OBDParameter.EnumValueTypes.String) {
                strRet += string.Format("[String: {0}] ", obdValue.StringValue);
            }
            if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.ListString) == (int)OBDParameter.EnumValueTypes.ListString) {
                strRet += "[ListString: ";
                foreach (string strx in obdValue.ListStringValue) {
                    strRet = string.Concat(strRet, strx + ", ");
                }
                strRet = strRet.Substring(0, strRet.Length - 2);
                strRet += "]";
            }
            if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.ShortString) == (int)OBDParameter.EnumValueTypes.ShortString) {
                strRet += string.Format("[ShortString: {0}] ", obdValue.ShortStringValue);
            }
            if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.BitFlags) == (int)OBDParameter.EnumValueTypes.BitFlags) {
                strRet += "[BitFlags: ";
                for (int idx = 0; idx < 32; idx++) {
                    strRet += (obdValue.GetBitFlag(idx) ? "1" : "0");
                    if (idx % 8 == 7) {
                        strRet += " ";
                    }
                }
                strRet = strRet.TrimEnd() + "]";
            }
            strRet = strRet.TrimEnd();
            return strRet;
        }

        public OBDParameterValue SpecialValue(OBDParameter param) {
            if (param.Parameter != 0) {
                return null;
            }

            OBDParameterValue value = new OBDParameterValue();
            string respopnse = GetRawResponse("ATRV");
            Log.TraceInfo("Response of \"ATRV\": " + respopnse);
            if (respopnse != null) {
                respopnse = respopnse.Replace("V", "");
                value.DoubleValue = Utility.Text2Double(respopnse);
            }
            return value;
        }

        public string GetRawResponse(string strCmd) {
            return m_obdDevice.Query(strCmd);
        }

        public OBDResponseList GetResponseList(OBDParameter param) {
            OBDResponseList responses = m_obdDevice.Query(param);
            string strItem = "Responses: ";
            for (int i = 0; i < responses.ResponseCount; i++) {
                strItem += string.Format("[{0}] ", Utility.GetReadableHexString(0, responses.GetOBDResponse(i).Data));
            }
            strItem = strItem.TrimEnd();
            Log.TraceInfo(strItem);
            return responses;
        }

        public bool ClearCodes() {
            return (m_obdDevice.Query("04").IndexOf("44") >= 0);
        }

        public void Disconnect() {
            m_obdDevice.Disconnect();
            m_obdDevice.SetConnected(false);
            STDType = StandardType.Automatic;
            OnDisconnect?.Invoke();
        }

        public DTC GetDTC(string code) {
            foreach (DTC dtc in m_listDTC) {
                if (dtc.Name.CompareTo(code) == 0) {
                    return dtc;
                }
            }
            return new DTC(code, "", "");
        }

        public OBDParameter LookupParameter(string pid) {
            foreach (OBDParameter param in m_listAllParameters) {
                if (param.PID.CompareTo(pid) == 0) {
                    return param;
                }
            }
            return null;
        }

        public List<OBDParameter> SupportedParameterList(int valueTypes) {
            List<OBDParameter> list = new List<OBDParameter>(m_listSupportedParameters.Count);
            foreach (OBDParameter param in m_listSupportedParameters) {
                if ((param.ValueTypes & valueTypes) == valueTypes) {
                    list.Add(param);
                }
            }
            return list;
        }

        private void SetDevice(HardwareType device) {
            CommSettings.HardwareIndex = device;
            switch (device) {
            case HardwareType.ELM327:
                Log.TraceInfo("Set device to ELM327");
                m_obdDevice = new OBDDeviceELM327(CommSettings, Log, m_xattr);
                break;
            default:
                Log.TraceInfo("Set device to ELM327");
                m_obdDevice = new OBDDeviceELM327(CommSettings, Log, m_xattr);
                break;
            }
        }

        public int LoadDTCDefinitions(string fileName) {
            try {
                if (File.Exists(fileName)) {
                    Type[] extraTypes = new Type[] { typeof(DTC) };
                    m_listDTC = new XmlSerializer(typeof(List<DTC>), extraTypes).Deserialize(new FileStream(fileName, FileMode.Open)) as List<DTC>;
                    return m_listDTC.Count;
                } else {
                    Log.TraceError("Failed to locate DTC definitions file: " + fileName + "because it doesn't exist.");
                    return 0;
                }
            } catch (Exception ex) {
                Log.TraceError("Failed to load parameters from: " + fileName + ", reason: " + ex.Message);
                return -1;
            }
        }

        public void SaveCommSettings(Settings settings) {
            CommSettings = settings;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Settings));
            using (TextWriter writer = new StreamWriter(m_settings_xml)) {
                xmlSerializer.Serialize(writer, CommSettings);
                writer.Close();
            }
        }

        public Settings LoadCommSettings() {
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                using (FileStream reader = new FileStream(m_settings_xml, FileMode.Open)) {
                    CommSettings = (Settings)serializer.Deserialize(reader);
                    reader.Close();
                }
            } catch (Exception ex) {
                Log.TraceError("Using default communication settings because of failed to load them, reason: " + ex.Message);
                CommSettings = new Settings();
                ConfigResult |= LoadConfigResult.CommSettings;
            }
            return CommSettings;
        }

    }
}