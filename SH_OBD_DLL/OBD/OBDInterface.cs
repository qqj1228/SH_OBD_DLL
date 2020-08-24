﻿using DBCParser;
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
        private const string m_dllSettings_xml = ".\\Configs\\dllsetting.xml";

        public delegate void __Delegate_OnConnect();
        public delegate void __Delegate_OnDisconnect();
        public event __Delegate_OnDisconnect OnDisconnect;
        public event __Delegate_OnConnect OnConnect;

        private OBDDevice m_obdDevice;
        private readonly Parser m_dbc;
        private readonly NetWork m_netWork;
        private readonly OBDInterpreter m_obdInterpreter;
        private readonly List<SignalDisplay> m_sigDisplays;
        private readonly List<ValueDisplay> m_valDispalys;
        private readonly int[] m_xattr;

        public Logger Log { get; }
        public bool DllSettingsResult { get; private set; }
        public DllSettings DllSettings { get; private set; }
        public StandardType STDType { get; set; }

        public OBDInterface(string logPath) {
            Log = new Logger(logPath, EnumLogLevel.LogLevelAll, true, 100);
            Log.TraceInfo("=======================================================================");
            Log.TraceInfo("==================== START DllVersion: " + DllVersion<OBDInterface>.AssemblyVersion + " ====================");
            m_sigDisplays = LoadDisplayFile<SignalDisplay>(".\\Configs\\signal.xml");
            m_valDispalys = LoadDisplayFile<ValueDisplay>(".\\Configs\\value.xml");
            m_dbc = new Parser();
            m_netWork = m_dbc.ParseFile(".\\Configs\\OBD_CMD.dbc");
            m_obdInterpreter = new OBDInterpreter(m_netWork, m_sigDisplays, m_valDispalys);
            DllSettingsResult = true;
            DllSettings = LoadDllSettings();
            string[] strAttr = DllSettings.AutoProtocolOrder.Split(',');
            m_xattr = new int[strAttr.Length];
            for (int i = 0; i < strAttr.Length; i++) {
                int.TryParse(strAttr[i], out m_xattr[i]);
            }
            SetDevice(HardwareType.ELM327);
            Disconnect();
            STDType = StandardType.Automatic;
        }

        public bool ConnectedStatus {
            get { return m_obdDevice.Online; }
        }

        public HardwareType GetDevice() {
            return DllSettings.HardwareIndex;
        }

        public string GetDeviceDesString() {
            return m_obdDevice.DeviceDesString;
        }

        public string GetDeviceIDString() {
            return m_obdDevice.DeviceIDString;
        }

        public ProtocolType GetProtocol() {
            return DllSettings.ProtocolIndex;
        }

        public StandardType GetStandard() {
            return DllSettings.StandardIndex;
        }

        public void SetTimeout(int iTimeout = 500) {
            m_obdDevice.SetTimeout(iTimeout);
        }

        public bool InitDevice() {
            bool flag;
            SetDevice(DllSettings.HardwareIndex);
            flag = m_obdDevice.Initialize(DllSettings);
            STDType = m_obdDevice.GetStandardType();
            if (flag) {
                OnConnect?.Invoke();
                Log.TraceInfo("Connection Established!");
                return true;
            } else {
                Log.TraceWarning("Failed to find a compatible OBD-II interface.");
                return false;
            }
        }

        public bool InitDeviceAuto() {
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
            DllSettings.StandardIndex = STDType;
            flag = m_obdDevice.InitializeAuto(DllSettings);
            STDType = m_obdDevice.GetStandardType();
            if (flag) {
                DllSettings.ProtocolIndex = m_obdDevice.GetProtocolType();
                DllSettings.ComPort = m_obdDevice.GetComPortIndex();
                SaveDllSettings(DllSettings);
                OnConnect?.Invoke();
                return true;
            }
            return false;
        }

        public List<OBDParameterValue> GetValueList(OBDParameter param) {
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
                OBDParameterValue obdValue = m_obdInterpreter.GetValue(param, responses.GetOBDResponse(i));
                if (obdValue.ErrorDetected) {
                    Log.TraceError(string.Format("Values: [ECU: {0}, Error Detected!]", obdValue.ECUResponseID));
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

        public void Disconnect() {
            m_obdDevice.Disconnect();
            STDType = StandardType.Automatic;
            OnDisconnect?.Invoke();
        }

        private void SetDevice(HardwareType device) {
            DllSettings.HardwareIndex = device;
            switch (device) {
            case HardwareType.ELM327:
                Log.TraceInfo("Set device to ELM327");
                m_obdDevice = new OBDDeviceELM327(DllSettings, Log, m_xattr);
                break;
            default:
                Log.TraceInfo("Set device to ELM327");
                m_obdDevice = new OBDDeviceELM327(DllSettings, Log, m_xattr);
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
            using (TextWriter writer = new StreamWriter(m_dllSettings_xml)) {
                xmlSerializer.Serialize(writer, DllSettings);
                writer.Close();
            }
        }

        public DllSettings LoadDllSettings() {
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(DllSettings));
                using (FileStream reader = new FileStream(m_dllSettings_xml, FileMode.Open)) {
                    DllSettings = (DllSettings)serializer.Deserialize(reader);
                    reader.Close();
                }
            } catch (Exception ex) {
                Log.TraceError("Using default dll settings because of failed to load them, reason: " + ex.Message);
                DllSettings = new DllSettings();
                DllSettingsResult = false;
            }
            return DllSettings;
        }

    }
}