using System;
using System.Xml.Serialization;

namespace SH_OBD {
    [Serializable]
    public class Settings {
        public bool AutoDetect { get; set; }
        public int ComPort { get; set; }
        public string RemoteIP { get; set; }
        public int RemotePort { get; set; }
        public int BaudRateIndex { get; set; }
        public bool DoInitialization { get; set; }
        public string CN6_OBD_SUP { get; set; }
        public string AutoProtocolOrder { get; set; }
        [XmlIgnore]
        public bool FirstRun { get; set; }


        public Settings() {
            AutoDetect = true;
            ComPort = 0;
            BaudRateIndex = 0;
            RemoteIP = "127.0.0.1";
            RemotePort = 60001;
            HardwareIndex = HardwareType.Automatic;
            ProtocolIndex = ProtocolType.Automatic;
            StandardIndex = StandardType.Automatic;
            DoInitialization = true;
            FirstRun = true;
            CN6_OBD_SUP = "29,2B";
            AutoProtocolOrder = "6,7,8,9,10,5,4,3,2,1";
        }

        public int BaudRate {
            get {
                switch (BaudRateIndex) {
                    case 0: return 9600;
                    case 1: return 38400;
                    case 2: return 115200;
                    default: return 9600;
                }
            }
        }

        public string ComPortName {
            get { return "COM" + Convert.ToString(ComPort); }
        }

        [XmlIgnore]
        public static string[] ProtocolNames = new string[] {
            "自动",
            "SAE J1850 PWM (41.6K 波特率)",
            "SAE J1850 VPW (10.4K 波特率)",
            "ISO 9141-2 (5 波特率初始化, 10.4K 波特率)",
            "ISO 14230-4 KWP (5 波特率初始化, 10.4K 波特率)",
            "ISO 14230-4 KWP (快速初始化, 10.4K 波特率)",
            "ISO 15765-4 CAN (11 位 CAN ID, 500K 波特率)",
            "ISO 15765-4 CAN (29 位 CAN ID, 500K 波特率)",
            "ISO 15765-4 CAN (11 位 CAN ID, 250K 波特率)",
            "ISO 15765-4 CAN (29 位 CAN ID, 250K 波特率)",
            "SAE J1939 CAN (29 位 CAN ID, 250K 波特率)"
        };

        [XmlIgnore]
        public string ProtocolName {
            get {
                if (ProtocolIndexInt >= 0 && ProtocolIndexInt < ProtocolNames.Length) {
                    return ProtocolNames[ProtocolIndexInt];
                }
                return "Unknown";
            }
        }

        [XmlIgnore]
        public ProtocolType ProtocolIndex { get; set; }

        [XmlElement("ProtocolIndex")]
        public int ProtocolIndexInt {
            get { return (int)ProtocolIndex; }
            set { ProtocolIndex = (ProtocolType)value; }
        }

        [XmlIgnore]
        public HardwareType HardwareIndex { get; set; }

        [XmlElement("HardwareIndex")]
        public int HardwareIndexInt {
            get { return (int)HardwareIndex; }
            set { HardwareIndex = (HardwareType)value; }
        }

        [XmlIgnore]
        public static string[] StandardNames = new string[] {
            "自动",
            "ISO 15031",
            "ISO 27145",
            "SAE J1939",
        };

        [XmlIgnore]
        public string StandardName {
            get {
                if (StandardIndexInt >= 0 && StandardIndexInt < StandardNames.Length) {
                    return StandardNames[StandardIndexInt];
                }
                return "Automatic";
            }
        }

        [XmlIgnore]
        public StandardType StandardIndex { get; set; }

        [XmlElement("StandardIndex")]
        public int StandardIndexInt {
            get { return (int)StandardIndex; }
            set { StandardIndex = (StandardType)value; }
        }
    }

    public enum ProtocolType : int {
        Unknown = -1,
        Automatic = 0,
        J1850_PWM = 1,
        J1850_VPW = 2,
        ISO9141_2 = 3,
        ISO_14230_4_KWP_5BAUDINIT = 4,
        ISO_14230_4_KWP_FASTINIT = 5,
        ISO_15765_4_CAN_11BIT_500KBAUD = 6,
        ISO_15765_4_CAN_29BIT_500KBAUD = 7,
        ISO_15765_4_CAN_11BIT_250KBAUD = 8,
        ISO_15765_4_CAN_29BIT_250KBAUD = 9,
        SAE_J1939_CAN_29BIT_250KBAUD = 0xA
    }

    public enum StandardType : int {
        Automatic = 0,
        ISO_15031 = 1,
        ISO_27145 = 2,
        SAE_J1939 = 3
    }

    public enum HardwareType : int {
        Automatic = 0,
        ELM327 = 1,
        ELM320 = 2,
        ELM322 = 3,
        ELM323 = 4,
        CANtact = 5
    }

    [Flags]
    public enum LoadConfigResult : int {
        Success = 0,
        CommSettings = 1
    }

    public enum ConnectionType : int {
        SerialPort = 0,
        TCPClient = 1,
        UDPClient = 2
    }

}
