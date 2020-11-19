using System;

namespace SH_OBD_DLL {
    public class OBDCommELM : CommLine {
        protected string _Port = "COM1";
        public int Port {
            get {
                return int.Parse(_Port.Substring(3));
            }
            set {
                _Port = "COM" + value.ToString();
                _log.TraceInfo(string.Format("Port set to {0}", _Port));
            }
        }

        protected int _BaudRate = 38400;
        public int BaudRate {
            get { return _BaudRate; }
            set {
                _BaudRate = value;
                _log.TraceInfo(string.Format("Baud rate set to {0}", _BaudRate.ToString()));
            }
        }

        public int Timeout {
            get { return TransTimeout; }
            set {
                TransTimeout = value;
                _log.TraceInfo(string.Format("Timeout set to {0} ms", TransTimeout.ToString()));
            }
        }

        protected byte _asciiRxTerm = (byte)'>';
        public byte RxTerminator {
            get { return _asciiRxTerm; }
            set { _asciiRxTerm = value; }
        }

        protected string _RemoteIP = "127.0.0.1";
        public string RemoteIP {
            get { return _RemoteIP; }
            set {
                _RemoteIP = value;
                _log.TraceInfo(string.Format("RemoteIP set to {0}", _RemoteIP));
            }
        }

        protected int _RemotePort = 60001;
        public int RemotePort {
            get { return RemotePort; }
            set {
                _RemotePort = value;
                _log.TraceInfo(string.Format("RemotePort set to {0}", _RemotePort.ToString()));
            }
        }

        protected byte[] _RxFilterWithSpace;
        protected byte[] _RxFilterNoSpace;

        public OBDCommELM(DllSettings settings, Logger log) : base(settings, log) {
            _RxFilterWithSpace = new byte[] { 0x0A, 0x20, 0 };
            _RxFilterNoSpace = new byte[] { 0x0A, 0 };
        }

        public string GetResponse(string command) {
            string response;
            bool bRxFilterNoSpace = false;
            if (command.Contains("AT")) {
                // 如果是发送AT命令的话，返回值就不过滤空格
                SetRxFilter(_RxFilterNoSpace);
                bRxFilterNoSpace = true;
            }
            try {
                _log.TraceInfo(string.Format("TX: {0}", command));
                response = Transact(command);
                _log.TraceInfo(string.Format("RX: {0}", response.Replace("\r", @"\r").Replace("\n", @"\n")));
            } catch (Exception ex) {
                _log.TraceError("Transact() occur exception: " + ex.Message);
                if (string.Compare(ex.Message, "Timeout") == 0) {
                    Open();
                    _log.TraceError("RX: COMM TIMED OUT!");
                    response = "TIMEOUT";
                }
                response = ex.Message;
            } finally {
                if (bRxFilterNoSpace) {
                    // 将返回值重设为过滤空格
                    SetRxFilter(_RxFilterWithSpace);
                }
            }
            return response;
        }

        protected override CommBaseSettings CommSettings() {
            CommLine.CommLineSettings settings = new CommLine.CommLineSettings();
            settings.SetStandard(_Port, _BaudRate);
            settings.RxTerminator = _asciiRxTerm;
            settings.RxFilter = _RxFilterWithSpace;
            settings.TxTerminator = new byte[] { 0x0D };
            base.Setup(settings);
            return settings;
        }
    }
}