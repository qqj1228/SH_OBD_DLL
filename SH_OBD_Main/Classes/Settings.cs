using System;
using System.Xml.Serialization;

namespace SH_OBD_Main {
    [Serializable]
    public class MainSettings {
        public int ScannerBaudRateIndex { get; set; }
        public int ScannerPort { get; set; }
        public bool UseSerialScanner { get; set; }
        public string TesterName { get; set; }


        public MainSettings() {
            ScannerPort = 2;
            ScannerBaudRateIndex = 0;
            UseSerialScanner = false;
            TesterName = "tester";
        }

        public int ScannerBaudRate {
            get {
                switch (ScannerBaudRateIndex) {
                case 0: return 9600;
                case 1: return 38400;
                case 2: return 115200;
                default: return 9600;
                }
            }
        }

        public string ScannerPortName {
            get { return "COM" + Convert.ToString(ScannerPort); }
        }
    }

    [Flags]
    public enum LoadConfigResult : int {
        Success = 0,
        DllSettings = 1,
        DBandMES = 2,
        OBDResultSetting = 4,
        MainSettings = 8,
    }

    [Serializable]
    public class DBandMES {
        public string UserName { get; set; }
        public string PassWord { get; set; }
        public string DBName { get; set; }
        public string IP { get; set; }
        public string Port { get; set; }
        public string DateSN { get; set; }
        public WebServiceMES WSMES { get; set; }
        public OracleMES OraMES { get; set; }
        [XmlIgnore]
        public bool ChangeWebService { get; set; }

        public DBandMES() {
            UserName = "sa";
            PassWord = "sh49";
            DBName = "SH_OBD";
            IP = "127.0.0.1";
            Port = "1433";
            DateSN = DateTime.Now.ToLocalTime().ToString("yyyyMMdd") + ",0";
            WSMES = new WebServiceMES();
            OraMES = new OracleMES();
            ChangeWebService = true;
        }

        public string[] GetMethodArray() {
            return WSMES.Methods.Split(',');
        }
    }

    [Serializable]
    public class WebServiceMES {
        public bool Enable { get; set; }
        public string Address { get; set; }
        public string Name { get; set; }
        public string Methods { get; set; }
        public string WSDL { get; set; }
        public bool UseURL { get; set; }

        public WebServiceMES() {
            Enable = false;
            Address = "http://localhost:53827/webservicedemo.asmx?wsdl";
            Name = "WebServiceDemo";
            Methods = "WriteDataToMes";
            WSDL = "";
            UseURL = true;
        }
    }

    [Serializable]
    public class OracleMES {
        public bool Enable { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        public string ServiceName { get; set; }
        public string UserID { get; set; }
        public string PassWord { get; set; }

        public OracleMES() {
            Enable = false;
            Host = "192.168.1.49";
            Port = "1521";
            ServiceName = "XE";
            UserID = "c##scott";
            PassWord = "tiger";
        }
    }

    [Serializable]
    public class OBDResultSetting {
        public bool UploadWhenever { get; set; }
        public bool UseECUAcronym { get; set; }
        public bool UseSCRName { get; set; }
        public bool MIL { get; set; }
        public bool DTC03 { get; set; }
        public bool DTC07 { get; set; }
        public bool DTC0A { get; set; }
        public bool Readiness { get; set; }
        public bool VINError { get; set; }
        public int UploadInterval { get; set; }
        public bool CALIDCVNEmpty { get; set; }
        public bool OBD_SUP { get; set; }
        public int StartSN { get; set; }
        public int UnmeaningNum { get; set; }
        public bool SpecifiedProtocol { get; set; }
        public bool KMSSpecified { get; set; }
        public string CompanyCode { get; set; }

        public OBDResultSetting() {
            UploadWhenever = false;
            UseECUAcronym = false;
            UseSCRName = false;
            MIL = false;
            DTC03 = false;
            DTC07 = false;
            DTC0A = false;
            Readiness = false;
            VINError = true;
            CALIDCVNEmpty = false;
            OBD_SUP = true;
            UploadInterval = 15;
            StartSN = 0;
            UnmeaningNum = 5;
            SpecifiedProtocol = false;
            KMSSpecified = true;
            CompanyCode = "0079";
        }
    }

}
