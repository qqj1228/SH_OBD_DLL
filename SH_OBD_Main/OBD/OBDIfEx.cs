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
        private const string m_mainSettings_xml = ".\\Configs\\mainsettings.xml";
        private const string m_userprefs_xml = ".\\Configs\\userprefs.xml";
        private const string m_dbandMES_xml = ".\\Configs\\dbandMES.xml";
        private const string m_obdResultSetting_xml = ".\\Configs\\obdResultSetting.xml";
        private int m_configResult;
        public readonly SerialPortClass m_sp;
        public SH_OBD_Dll OBDDll { get; private set; }
        public OBDInterface OBDIf { get; private set; }
        public Logger Log { get; private set; }
        public UserPreferences UserPreferences { get; private set; }
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
            m_configResult = (int)LoadConfigResult.Success;
            if (!OBDIf.DllSettingsResult) {
                m_configResult |= (int)LoadConfigResult.DllSettings;
            }
            UserPreferences = LoadSettings<UserPreferences>(m_userprefs_xml);
            MainSettings = LoadSettings<MainSettings>(m_mainSettings_xml);
            DBandMES = LoadSettings<DBandMES>(m_dbandMES_xml);
            OBDResultSetting = LoadSettings<OBDResultSetting>(m_obdResultSetting_xml);
            if (m_configResult != (int)LoadConfigResult.Success) {
                if ((m_configResult & (int)LoadConfigResult.UserPreferences) == (int)LoadConfigResult.UserPreferences) {
                    StrLoadConfigResult += "用户设置读取错误\n";
                } else if ((m_configResult & (int)LoadConfigResult.DllSettings) == (int)LoadConfigResult.DllSettings) {
                    StrLoadConfigResult += "Dll设置读取错误\n";
                } else if ((m_configResult & (int)LoadConfigResult.DBandMES) == (int)LoadConfigResult.DBandMES) {
                    StrLoadConfigResult += "数据库和MES设置读取错误\n";
                } else if ((m_configResult & (int)LoadConfigResult.OBDResultSetting) == (int)LoadConfigResult.OBDResultSetting) {
                    StrLoadConfigResult += "OBD检测结果设置读取错误\n";
                } else if ((m_configResult & (int)LoadConfigResult.MainSettings) == (int)LoadConfigResult.MainSettings) {
                    StrLoadConfigResult += "主程序设置读取错误\n";
                }
            }
            ScannerPortOpened = false;
            if (MainSettings.UseSerialScanner) {
                m_sp = new SerialPortClass(
                    MainSettings.ScannerPortName,
                    MainSettings.ScannerBaudRate,
                    Parity.None,
                    8,
                    StopBits.One
                );
                try {
                    m_sp.OpenPort();
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
                if (settings.GetType() == typeof(UserPreferences)) {
                    m_configResult |= (int)LoadConfigResult.UserPreferences;
                } else if (settings.GetType() == typeof(MainSettings)) {
                    m_configResult |= (int)LoadConfigResult.MainSettings;
                } else if (settings.GetType() == typeof(DBandMES)) {
                    m_configResult |= (int)LoadConfigResult.DBandMES;
                } else if (settings.GetType() == typeof(OBDResultSetting)) {
                    m_configResult |= (int)LoadConfigResult.OBDResultSetting;
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

        public void SaveUserPreferences(UserPreferences userPreferences) {
            UserPreferences = userPreferences;
            SaveSetting(userPreferences, m_userprefs_xml);
        }

        public void SaveDBandMES(DBandMES dBandMES) {
            DBandMES = dBandMES;
            SaveSetting(dBandMES, m_dbandMES_xml);
        }

        public void SaveMainSettings(MainSettings mainSettings) {
            MainSettings = mainSettings;
            SaveSetting(mainSettings, m_mainSettings_xml);
        }

        public void SaveDllSettings(DllSettings settings) {
            OBDIf.SaveDllSettings(settings);
        }
    }
}
