using DBCParser;
using DBCParser.DBCObj;
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

namespace SH_OBD_DLL {
    public class OBDInterface {
        private const string _dllSettings_xml = ".\\Configs\\dllsetting.xml";

        public delegate void __Delegate_OnConnect();
        public delegate void __Delegate_OnDisconnect();
        public event __Delegate_OnDisconnect OnDisconnect;
        public event __Delegate_OnConnect OnConnect;

        private OBDDevice _obdDevice;
        private readonly Logger _log;
        private readonly Parser _dbc;
        private readonly NetWork _netWork;
        private readonly OBDInterpreter _obdInterpreter;
        private readonly int[] _xattr;

        public bool DllSettingsResult { get; private set; }
        public DllSettings DllSettings { get; private set; }
        public StandardType STDType { get; set; }

        public OBDInterface(Logger log) {
            _log = log;
            _log.TraceInfo("=======================================================================");
            _log.TraceInfo("==================== START DllVersion: " + DllVersion<OBDInterface>.AssemblyVersion + " ====================");
            _dbc = new Parser(".\\Configs\\value.xml", ".\\Configs\\signal.xml");
            _netWork = _dbc.ParseFile(".\\Configs\\OBD_CMD.dbc");
            _obdInterpreter = new OBDInterpreter(_netWork, _dbc);
            DllSettingsResult = true;
            DllSettings = LoadDllSettings();
            string[] strAttr = DllSettings.AutoProtocolOrder.Split(',');
            _xattr = new int[strAttr.Length];
            for (int i = 0; i < strAttr.Length; i++) {
                int.TryParse(strAttr[i], out _xattr[i]);
            }
            SetDevice(HardwareType.ELM327);
            Disconnect();
            STDType = StandardType.Automatic;
        }

        public bool ConnectedStatus {
            get { return _obdDevice.Online; }
        }

        public HardwareType GetDevice() {
            return DllSettings.HardwareIndex;
        }

        public string GetDeviceDesString() {
            return _obdDevice.DeviceDesString;
        }

        public string GetDeviceIDString() {
            return _obdDevice.DeviceIDString;
        }

        public ProtocolType GetProtocol() {
            return DllSettings.ProtocolIndex;
        }

        public StandardType GetStandard() {
            return DllSettings.StandardIndex;
        }

        public void SetTimeout(int iTimeout = 500) {
            _obdDevice.SetTimeout(iTimeout);
        }

        public bool InitDevice() {
            bool flag;
            SetDevice(DllSettings.HardwareIndex);
            flag = _obdDevice.Initialize(DllSettings);
            STDType = _obdDevice.GetStandardType();
            if (flag) {
                OnConnect?.Invoke();
                _log.TraceInfo("Connection Established!");
                return true;
            } else {
                _log.TraceWarning("Failed to find a compatible OBD-II interface.");
                return false;
            }
        }

        public bool InitDeviceAuto() {
            _log.TraceInfo("Beginning AUTO initialization...");
            string order = "";
            foreach (int item in _xattr) {
                order += "," + item.ToString();
            }
            order = order.TrimStart(',');
            _log.TraceInfo("Auto Protocol Order: " + order);
            _log.TraceInfo("Application Layer Protocol: " + STDType.ToString());

            SetDevice(HardwareType.ELM327);
            bool flag;
            DllSettings.StandardIndex = STDType;
            flag = _obdDevice.InitializeAuto(DllSettings);
            STDType = _obdDevice.GetStandardType();
            if (flag) {
                DllSettings.ProtocolIndex = _obdDevice.GetProtocolType();
                DllSettings.ComPort = _obdDevice.GetComPortIndex();
                SaveDllSettings(DllSettings);
                OnConnect?.Invoke();
                return true;
            }
            return false;
        }

        public List<OBDParameterValue> GetValueList(OBDParameter param) {
            List<OBDParameterValue> ValueList = new List<OBDParameterValue>();

            _log.TraceInfo("Requesting: " + param.OBDRequest);
            OBDResponseList responses = _obdDevice.Query(param);
            string strItem = "Responses: ";
            if (responses.ErrorDetected) {
                strItem += "Error Detected!";
                OBDParameterValue value = new OBDParameterValue {
                    ErrorDetected = true,
                    StringValue = "Error Detected in OBDResponseList!",
                    ShortStringValue = "ERROR_RESP"
                };
                ValueList.Add(value);
                _log.TraceInfo(strItem);
                return ValueList;
            } else {
                for (int i = 0; i < responses.ResponseCount; i++) {
                    strItem += string.Format("[{0}] ", Utility.GetReadableHexString(0, responses.GetOBDResponse(i).Data));
                }
                strItem = strItem.TrimEnd();
                _log.TraceInfo(strItem);
            }

            for (int i = 0; i < responses.ResponseCount; i++) {
                OBDParameterValue obdValue = _obdInterpreter.GetValue(param, responses.GetOBDResponse(i));
                if (obdValue.ErrorDetected) {
                    _log.TraceError(string.Format("Values: [ECU: {0}, Error Detected!]", obdValue.ECUResponseID));
                } else {
                    _log.TraceInfo(GetLogString(param, obdValue));
                }
                ValueList.Add(obdValue);
            }
            return ValueList;
        }

        private string GetLogString(OBDParameter param, OBDParameterValue obdValue) {
            string strRet = "Values: ";
            if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.Double) == (int)OBDParameter.EnumValueTypes.Double) {
                strRet += string.Format("[ECU: {1}, Double: {0}] ", obdValue.DoubleValue.ToString(), obdValue.ECUResponseID);
            }
            if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.Bool) == (int)OBDParameter.EnumValueTypes.Bool) {
                strRet += string.Format("[ECU: {1}, Bool: {0}] ", obdValue.BoolValue.ToString(), obdValue.ECUResponseID);
            }
            if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.String) == (int)OBDParameter.EnumValueTypes.String) {
                strRet += string.Format("[ECU: {1}, String: {0}] ", obdValue.StringValue, obdValue.ECUResponseID);
            }
            if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.ListString) == (int)OBDParameter.EnumValueTypes.ListString) {
                strRet += string.Format("[ECU: {0}, ListString: ", obdValue.ECUResponseID);
                if (obdValue.ListStringValue != null && obdValue.ListStringValue.Count > 0) {
                    foreach (string strx in obdValue.ListStringValue) {
                        strRet = string.Concat(strRet, strx + ", ");
                    }
                    strRet = strRet.Substring(0, strRet.Length - 2);
                }
                strRet += "]";
            }
            if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.ShortString) == (int)OBDParameter.EnumValueTypes.ShortString) {
                strRet += string.Format("[ECU: {1}, ShortString: {0}] ", obdValue.ShortStringValue, obdValue.ECUResponseID);
            }
            if ((param.ValueTypes & (int)OBDParameter.EnumValueTypes.BitFlags) == (int)OBDParameter.EnumValueTypes.BitFlags) {
                strRet += string.Format("[ECU: {0}, BitFlags: ", obdValue.ECUResponseID);
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

        public string GetRawResponse(string strCmd) {
            return _obdDevice.Query(strCmd);
        }

        public OBDResponseList GetResponseList(OBDParameter param) {
            OBDResponseList responses = _obdDevice.Query(param);
            string strItem = "Responses: ";
            for (int i = 0; i < responses.ResponseCount; i++) {
                strItem += string.Format("[{0}] ", Utility.GetReadableHexString(0, responses.GetOBDResponse(i).Data));
            }
            strItem = strItem.TrimEnd();
            _log.TraceInfo(strItem);
            return responses;
        }

        public void Disconnect() {
            _obdDevice.Disconnect();
            STDType = StandardType.Automatic;
            OnDisconnect?.Invoke();
        }

        private void SetDevice(HardwareType device) {
            DllSettings.HardwareIndex = device;
            switch (device) {
            case HardwareType.ELM327:
                _log.TraceInfo("Set device to ELM327");
                _obdDevice = new OBDDeviceELM327(DllSettings, _log, _xattr);
                break;
            default:
                _log.TraceInfo("Set device to ELM327");
                _obdDevice = new OBDDeviceELM327(DllSettings, _log, _xattr);
                break;
            }
        }

        public List<T> LoadDisplayFile<T>(string fileName) {
            List<T> displays = null;
            try {
                if (File.Exists(fileName)) {
                    Type[] extraTypes = new Type[] { typeof(T) };
                    displays = new XmlSerializer(typeof(List<T>), extraTypes).Deserialize(new FileStream(fileName, FileMode.Open)) as List<T>;
                    return displays;
                } else {
                    Console.WriteLine("Failed to locate the file: " + fileName + ", because it doesn't exist.");
                    return displays;
                }
            } catch (Exception ex) {
                Console.WriteLine("Failed to load parameters from: " + fileName + ", reason: " + ex.Message);
                return displays;
            }
        }

        public void SaveDllSettings(DllSettings settings) {
            DllSettings = settings;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(DllSettings));
            using (TextWriter writer = new StreamWriter(_dllSettings_xml)) {
                xmlSerializer.Serialize(writer, DllSettings);
                writer.Close();
            }
        }

        public DllSettings LoadDllSettings() {
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(DllSettings));
                using (FileStream reader = new FileStream(_dllSettings_xml, FileMode.Open)) {
                    DllSettings = (DllSettings)serializer.Deserialize(reader);
                    reader.Close();
                }
            } catch (Exception ex) {
                _log.TraceError("Using default dll settings because of failed to load them, reason: " + ex.Message);
                DllSettings = new DllSettings();
                DllSettingsResult = false;
            }
            return DllSettings;
        }

    }
}