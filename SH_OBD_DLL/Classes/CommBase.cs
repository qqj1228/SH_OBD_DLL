using SH_OBD_DLL;
using System;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Activation;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace SH_OBD_DLL {
    public abstract class CommBase {
        private SerialPortClass _serial = null;
        private TCPClientImp _TCP = null;
        private readonly DllSettings _mainSettings; // 传入的主配置
        protected readonly Logger _log;
        private bool _online = false;
        private bool _auto = false;
        private int _writeCount = 0;

        protected CommBase(DllSettings settings, Logger log) {
            _mainSettings = settings;
            _log = log;
        }

        ~CommBase() {
            Close();
        }

        public bool Online {
            get {
                if (_online) {
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
            if (_mainSettings.ComPort > 0) {
                bRet = OpenSerialPort();
            } else {
                bRet = OpenTCPClient();
            }
            return bRet;
        }

        protected bool OpenSerialPort() {
            if (_online) {
                return false;
            }
            CommBase.CommBaseSettings commBaseSettings = CommSettings();
            _serial = new SerialPortClass(commBaseSettings.Port, commBaseSettings.BaudRate, (Parity)commBaseSettings.Parity, commBaseSettings.DataBits, (StopBits)commBaseSettings.StopBits) {
                WriteTimeout = 5000 // 发送超时设为5s
            };
            _writeCount = 0;
            _auto = false;
            _serial.DataReceived += SerialDataReceived;
            try {
                if (_serial.OpenPort()) {
                    _online = true;
                    if (AfterOpen()) {
                        _auto = commBaseSettings.AutoReopen;
                        return true;
                    } else {
                        Close();
                        return false;
                    }
                } else {
                    return false;
                }
            } catch (Exception ex) {
                _log.TraceFatal(string.Format("Can't open {0}! Reason: {1}", commBaseSettings.Port, ex.Message));
                return false;
            }
        }

        protected bool OpenTCPClient() {
            if (_online) {
                return false;
            }
            CommBase.CommBaseSettings commBaseSettings = CommSettings();
            commBaseSettings.Port = _mainSettings.ComPortName;
            _TCP = new TCPClientImp(_mainSettings.RemoteIP, _mainSettings.RemotePort, _log);
            _auto = false;
            _TCP.RecvedMsg += OnRecvedMsg;
            try {
                _TCP.ConnectServer();
            } catch (Exception ex) {
                _log.TraceFatal(string.Format("Can't open {0}:{1}! Reason: {2}", _mainSettings.RemoteIP, _mainSettings.RemotePort, ex.Message));
                return false;
            }
            _online = true;
            if (AfterOpen()) {
                _auto = commBaseSettings.AutoReopen;
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
            if (!_online) {
                return;
            }
            _auto = false;
            BeforeClose(false);
            InternalClose();
            _online = false;
        }

        private void InternalClose() {
            if (_mainSettings.ComPort > 0) {
                _serial.DataReceived -= SerialDataReceived;
                _serial.ClosePort();
            } else {
                _TCP.RecvedMsg -= OnRecvedMsg;
                _TCP.Close();
            }
        }

        protected void ThrowException(string reason) {
            if (_online && reason != "Timeout") {
                BeforeClose(true);
                InternalClose();
            }
            throw new CommPortException(reason);
        }

        protected void Send(byte[] tosend) {
            if (CheckOnline()) {
                _writeCount = tosend.GetLength(0);
                try {
                    if (_mainSettings.ComPort > 0) {
                        _serial.SendData(tosend, 0, _writeCount);
                    } else {
                        _TCP.SendData(tosend, 0, _writeCount);
                    }
                    _writeCount = 0;
                } catch (Exception ex) {
                    _log.TraceError("Send failed: " + ex.Message);
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
            if (_mainSettings.ComPort > 0) {
                if (_online) {
                    return true;
                } else {
                    if (_auto && Open()) {
                        return true;
                    }
                    _log.TraceError("CheckOnline: Offline");
                    return false;
                }
            } else {
                return _TCP.TestConnect();
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
