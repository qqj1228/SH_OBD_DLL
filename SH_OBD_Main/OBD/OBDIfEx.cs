using SH_OBD_DLL;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SH_OBD_Main {
    public class OBDIfEx {
        private const string _mainSettings_xml = ".\\Configs\\mainsettings.xml";
        private const string _dbandMES_xml = ".\\Configs\\dbandMES.xml";
        private const string _obdResultSetting_xml = ".\\Configs\\obdResultSetting.xml";
        private int _configResult;
        public readonly SerialPortClass _sp;
        public SH_OBD_Dll OBDDll { get; private set; }
        public OBDInterface OBDIf { get; private set; }
        public Logger Log { get; private set; }
        public MainSettings MainSettings { get; private set; }
        public DBandMES DBandMES { get; private set; }
        public OBDResultSetting OBDResultSetting { get; set; }
        public bool ScannerPortOpened { get; set; }
        public string StrLoadConfigResult { get; set; }

        public OBDIfEx() {
            StrLoadConfigResult = "";
            OBDDll = new SH_OBD_Dll(".\\log");
            OBDIf = OBDDll.GetOBDInterface();
            Log = OBDDll.GetLogger();
            _configResult = (int)LoadConfigResult.Success;
            if (!OBDIf.DllSettingsResult) {
                _configResult |= (int)LoadConfigResult.DllSettings;
            }
            MainSettings = LoadSettings<MainSettings>(_mainSettings_xml);
            DBandMES = LoadSettings<DBandMES>(_dbandMES_xml);
            OBDResultSetting = LoadSettings<OBDResultSetting>(_obdResultSetting_xml);
            if (_configResult != (int)LoadConfigResult.Success) {
                if ((_configResult & (int)LoadConfigResult.DllSettings) == (int)LoadConfigResult.DllSettings) {
                    StrLoadConfigResult += "Dll设置读取错误\n";
                } else if ((_configResult & (int)LoadConfigResult.DBandMES) == (int)LoadConfigResult.DBandMES) {
                    StrLoadConfigResult += "数据库和MES设置读取错误\n";
                } else if ((_configResult & (int)LoadConfigResult.OBDResultSetting) == (int)LoadConfigResult.OBDResultSetting) {
                    StrLoadConfigResult += "OBD检测结果设置读取错误\n";
                } else if ((_configResult & (int)LoadConfigResult.MainSettings) == (int)LoadConfigResult.MainSettings) {
                    StrLoadConfigResult += "主程序设置读取错误\n";
                }
            }
            ScannerPortOpened = false;
            if (MainSettings.UseSerialScanner) {
                _sp = new SerialPortClass(
                    MainSettings.ScannerPortName,
                    MainSettings.ScannerBaudRate,
                    Parity.None,
                    8,
                    StopBits.One
                );
                try {
                    _sp.OpenPort();
                    ScannerPortOpened = true;
                } catch (Exception ex) {
                    Log.TraceError("打开扫码枪串口出错: " + ex.Message);
                }
            }
        }

        public T LoadSettings<T>(string strXmlFile) where T : new() {
            T settings;
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (FileStream reader = new FileStream(strXmlFile, FileMode.Open)) {
                    settings = (T)serializer.Deserialize(reader);
                    reader.Close();
                }
            } catch (Exception ex) {
                Log.TraceError("Using default settings because of failed to load " + strXmlFile + ", reason: " + ex.Message);
                settings = new T();
                if (settings.GetType() == typeof(MainSettings)) {
                    _configResult |= (int)LoadConfigResult.MainSettings;
                } else if (settings.GetType() == typeof(DBandMES)) {
                    _configResult |= (int)LoadConfigResult.DBandMES;
                } else if (settings.GetType() == typeof(OBDResultSetting)) {
                    _configResult |= (int)LoadConfigResult.OBDResultSetting;
                }
            }
            return settings;
        }

        public void SaveSetting<T>(T TSetting, string strXmlFile) where T : new() {
            if (TSetting == null) {
                throw new ArgumentNullException(nameof(TSetting));
            }
            try {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                using (TextWriter writer = new StreamWriter(strXmlFile)) {
                    xmlSerializer.Serialize(writer, TSetting);
                    writer.Close();
                }
            } catch (Exception ex) {
                Log.TraceError("Save " + strXmlFile + " error, reason: " + ex.Message);
                throw new ApplicationException(strXmlFile + ": saving error");
            }
        }

        public void SaveDBandMES(DBandMES dBandMES) {
            DBandMES = dBandMES;
            SaveSetting(dBandMES, _dbandMES_xml);
        }

        public void SaveMainSettings(MainSettings mainSettings) {
            MainSettings = mainSettings;
            SaveSetting(mainSettings, _mainSettings_xml);
        }

        public void SaveDllSettings(DllSettings settings) {
            OBDIf.SaveDllSettings(settings);
        }
    }
}
