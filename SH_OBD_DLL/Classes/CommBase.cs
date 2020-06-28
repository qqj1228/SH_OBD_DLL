using SH_OBD_DLL;
using System;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Activation;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace SH_OBD {
    public abstract class CommBase {
        private SerialPortClass m_serial = null;
        private TCPClientImp m_TCP = null;
        private readonly Settings m_mainSettings; // 传入的主配置
        protected readonly Logger m_log;
        private bool m_online = false;
        private bool m_auto = false;
        private int m_writeCount = 0;

        protected CommBase(Settings settings, Logger log) {
            m_mainSettings = settings;
            m_log = log;
        }

        ~CommBase() {
            Close();
        }

        public bool Online {
            get {
                if (m_online) {
                    return CheckOnline();
                } else {
                    return false;
                }
            }
        }

        public static bool IsPortAvailable(int iComPort) {
            return GetPortAvailable(iComPort) > CommBase.PortStatus.Unavailable;
        }

        public static PortStatus GetPortAvailable(int iComPort) {
            string comPort = "COM" + iComPort.ToString();
            SerialPort sp = new SerialPort(comPort);
            try {
                sp.Open();
            } catch (Exception ex) {
                if (ex is UnauthorizedAccessException uaex) {
                    return PortStatus.Absent;
                } else {
                    return PortStatus.Unavailable;
                }
            } finally {
                sp.Close();
            }
            return PortStatus.Available;
        }

        public bool Open() {
            bool bRet;
            if (m_mainSettings.ComPort > 0) {
                bRet = OpenSerialPort();
            } else {
                bRet = OpenTCPClient();
            }
            return bRet;
        }

        protected bool OpenSerialPort() {
            if (m_online) {
                return false;
            }
            CommBase.CommBaseSettings commBaseSettings = CommSettings();
            m_serial = new SerialPortClass(commBaseSettings.Port, commBaseSettings.BaudRate, (Parity)commBaseSettings.Parity, commBaseSettings.DataBits, (StopBits)commBaseSettings.StopBits) {
                WriteTimeout = 5000 // 发送超时设为5s
            };
            m_writeCount = 0;
            m_auto = false;
            m_serial.DataReceived += SerialDataReceived;
            try {
                if (m_serial.OpenPort()) {
                    m_online = true;
                    if (AfterOpen()) {
                        m_auto = commBaseSettings.AutoReopen;
                        return true;
                    } else {
                        Close();
                        return false;
                    }
                } else {
                    return false;
                }
            } catch (Exception ex) {
                m_log.TraceFatal(string.Format("Can't open {0}! Reason: {1}", commBaseSettings.Port, ex.Message));
                return false;
            }
        }

        protected bool OpenTCPClient() {
            if (m_online) {
                return false;
            }
            CommBase.CommBaseSettings commBaseSettings = CommSettings();
            commBaseSettings.Port = m_mainSettings.ComPortName;
            m_TCP = new TCPClientImp(m_mainSettings.RemoteIP, m_mainSettings.RemotePort, m_log);
            m_auto = false;
            m_TCP.RecvedMsg += OnRecvedMsg;
            try {
                m_TCP.ConnectServer();
            } catch (Exception ex) {
                m_log.TraceFatal(string.Format("Can't open {0}:{1}! Reason: {2}", m_mainSettings.RemoteIP, m_mainSettings.RemotePort, ex.Message));
                return false;
            }
            m_online = true;
            if (AfterOpen()) {
                m_auto = commBaseSettings.AutoReopen;
                return true;
            } else {
                Close();
                return false;
            }
        }

        void SerialDataReceived(object sender, SerialDataReceivedEventArgs e, byte[] bits) {
            string RxString = Encoding.ASCII.GetString(bits);
            OnRxString(RxString);
            //foreach (byte item in bits) {
            //    OnRxChar(item);
            //}
        }

        private void OnRecvedMsg(object sender, RecvMsgEventArgs e) {
            foreach (byte item in e.RecvBytes) {
                OnRxChar(item);
            }
        }

        public void Close() {
            if (!m_online) {
                return;
            }
            m_auto = false;
            BeforeClose(false);
            InternalClose();
            m_online = false;
        }

        private void InternalClose() {
            if (m_mainSettings.ComPort > 0) {
                m_serial.DataReceived -= SerialDataReceived;
                m_serial.ClosePort();
            } else {
                m_TCP.RecvedMsg -= OnRecvedMsg;
                m_TCP.Close();
            }
        }

        protected void ThrowException(string reason) {
            if (m_online && reason != "Timeout") {
                BeforeClose(true);
                InternalClose();
            }
            throw new CommPortException(reason);
        }

        protected void Send(byte[] tosend) {
            if (CheckOnline()) {
                m_writeCount = tosend.GetLength(0);
                try {
                    if (m_mainSettings.ComPort > 0) {
                        m_serial.SendData(tosend, 0, m_writeCount);
                    } else {
                        m_TCP.SendData(tosend, 0, m_writeCount);
                    }
                    m_writeCount = 0;
                } catch (Exception ex) {
                    m_log.TraceError("Send failed: " + ex.Message);
                    ThrowException("Send failed: " + ex.Message);
                }
            }
        }

        protected virtual CommBase.CommBaseSettings CommSettings() {
            return new CommBase.CommBaseSettings();
        }

        protected virtual bool AfterOpen() {
            return true;
        }

        protected virtual void BeforeClose(bool error) { }

        protected virtual void OnRxChar(byte ch) { }

        protected virtual void OnRxString(string strRx) { }

        protected virtual void OnTxDone() { }

        protected virtual void OnBreak() { }

        protected virtual void OnRxException(Exception e) { }

        private bool CheckOnline() {
            if (m_mainSettings.ComPort > 0) {
                if (m_online) {
                    return true;
                } else {
                    if (m_auto && Open()) {
                        return true;
                    }
                    m_log.TraceError("CheckOnline: Offline");
                    return false;
                }
            } else {
                return m_TCP.TestConnect();
            }
        }

        protected class CommBaseSettings {
            public string Port = "COM1";
            public int BaudRate = 2400;
            public int Parity = 0;
            public int DataBits = 8;
            public int StopBits = 1;
            public bool AutoReopen = false;
            public string RemoteIP = "127.0.0.1";
            public int RemotePort = 60001;

            public void SetStandard(string port, int baudrate) {
                DataBits = 8;
                StopBits = 1;
                Parity = 0;
                Port = port;
                BaudRate = baudrate;
            }

            public void SetServerAddress(string strRemoteIP, int iRemotePort) {
                RemoteIP = strRemoteIP;
                RemotePort = iRemotePort;
            }
        }

        public enum PortStatus {
            Absent = -1,
            Unavailable = 0,
            Available = 1,
        }

    }

    public class CommPortException : ApplicationException {
        public CommPortException(string desc) : base(desc) { }

        public CommPortException(Exception ex) : base("Receive Thread Exception", ex) { }
    }
}
