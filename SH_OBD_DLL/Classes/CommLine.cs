using System;
using System.IO;
using System.Text;
using System.Threading;

namespace SH_OBD_DLL {
    public abstract class CommLine : CommBase {
        private const int BUFF_SIZE = 512;
        private static readonly object locker = new object();
        private int _RxIndex = 0;
        private string _RxString = "";
        private readonly ManualResetEvent _TransFlag = new ManualResetEvent(true);
        private byte[] _RxBuffer;
        private byte _RxTerm;
        private byte[] _TxTerm;
        private byte[] _RxFilter;
        private string _RxLine; // 单独接收到的ELM327发来的消息
        private bool _bReceivedMsg = false; // 表示给ELM327发送命令后是否收到了返回数据
        private bool _bRxEnd = false; // 表示已把串口返回的一包数据处理完毕
        protected int TransTimeout { get; set; }

        protected CommLine(DllSettings settings, Logger log) : base(settings, log) { }

        protected void SetRxFilter(byte[] RxFilter) {
            _RxFilter = RxFilter;
        }

        protected void Send(string data) {
            int len_data = Encoding.ASCII.GetByteCount(data);

            int len_term = 0;
            if (_TxTerm != null) {
                len_term = _TxTerm.Length;
            }

            byte[] sending = new byte[len_data + len_term];
            Encoding.ASCII.GetBytes(data).CopyTo(sending, 0);

            if (_TxTerm != null) {
                _TxTerm.CopyTo(sending, len_data);
            }
            base.Send(sending);
        }

        protected string Transact(string data) {
            _RxString = "";
            Send(data);
            _bReceivedMsg = false;
            _TransFlag.Reset();
            if (!_TransFlag.WaitOne(TransTimeout, false)) {
                if (!_bReceivedMsg) {
                    ThrowException("Timeout");
                }
                while (!_bRxEnd) {
                    Thread.Sleep(10);
                }
            }
            lock (locker) {
                _RxLine = "";
                return _RxString;
            }
        }

        protected void Setup(CommLine.CommLineSettings settings) {
            _RxBuffer = new byte[settings.RxStringBufferSize];
            _RxTerm = settings.RxTerminator;
            _RxFilter = settings.RxFilter;
            TransTimeout = settings.TransactTimeout;
            _TxTerm = settings.TxTerminator;
        }

        /// <summary>
        /// 获取单独接收到的ELM327发来的消息，函数返回后会将单独接收缓冲区清空
        /// </summary>
        /// <returns></returns>
        public string GetRxLine() {
            string strRet = _RxLine;
            _RxLine = "";
            return strRet;
        }

        protected virtual void OnRxLine() {
            _RxLine += _RxString;
        }

        protected string StringFilter(string strOld) {
            char[] chars = strOld.ToCharArray();
            int offset = 0;
            char[] result = new char[chars.Length];
            bool bSkip;
            for (int i = 0; i < chars.Length; i++) {
                bSkip = false;
                foreach (byte item in _RxFilter) {
                    if (chars[i] == item) {
                        bSkip = true;
                        break;
                    }
                }
                if (bSkip) {
                    continue;
                }
                result[offset] = chars[i];
                offset++;
            }
            return new string(result, 0, offset);
        }

        protected override void OnRxString(string strRx) {
            _bReceivedMsg = true;
            int index;
            int startIndex = 0;
            string strRxFilter = strRx;
            if (_RxFilter != null) {
                strRxFilter = StringFilter(strRxFilter);
            }
            _bRxEnd = false;
            while (startIndex < strRxFilter.Length) {
                index = strRxFilter.IndexOf((char)_RxTerm, startIndex);
                if (index > -1) {
                    lock (locker) {
                        _RxString += strRxFilter.Substring(startIndex, index);
                    }
                    // 检测是否已经m_TransFlag.Set()了
                    if (_TransFlag.WaitOne(0, false)) {
                        // 已经m_TransFlag.Set()了，表示是单独接收ELM327发来的消息，即startIndex > 0
                        OnRxLine();
                    } else {
                        // 没有m_TransFlag.Set()，表示是给ELM327发送命令后返回的消息，即startIndex == 0
                        _TransFlag.Set();
                    }
                    startIndex = index + 1;
                } else {
                    _RxString += strRxFilter.Substring(startIndex);
                    if (_RxString.Length > BUFF_SIZE) {
                        _RxString = _RxString.Substring(0, BUFF_SIZE);
                        // 检测是否已经m_TransFlag.Set()了
                        if (_TransFlag.WaitOne(0, false)) {
                            // 已经m_TransFlag.Set()了，表示是单独接收ELM327发来的消息，即startIndex > 0
                            OnRxLine();
                        } else {
                            // 没有m_TransFlag.Set()，表示是给ELM327发送命令后返回的消息，即startIndex == 0
                            _TransFlag.Set();
                        }
                    }
                    break;
                }
            }
            _bRxEnd = true;
        }

        protected override void OnRxChar(byte ch) {
            _bReceivedMsg = true;
            _bRxEnd = false;
            if (ch == _RxTerm || _RxIndex >= _RxBuffer.Length) {
                lock (locker) {
                    _RxString = Encoding.ASCII.GetString(_RxBuffer, 0, _RxIndex);
                }
                _RxIndex = 0;
                _bRxEnd = true;
                // 检测是否已经m_TransFlag.Set()了
                if (_TransFlag.WaitOne(0, false)) {
                    // 已经m_TransFlag.Set()了，表示是单独接收ELM327发来的消息
                    OnRxLine();
                } else {
                    // 没有m_TransFlag.Set()，表示是给ELM327发送命令后返回的消息
                    _TransFlag.Set();
                }
                _TransFlag.Set();
            } else {
                if (_RxFilter != null) {
                    for (int idx = 0; idx < _RxFilter.Length; ++idx) {
                        if (_RxFilter[idx] == ch) {
                            return;
                        }
                    }
                }
                _RxBuffer[_RxIndex] = ch;
                ++_RxIndex;
            }
        }

        protected class CommLineSettings : CommBase.CommBaseSettings {
            public int RxStringBufferSize = 256;
            public byte RxTerminator = 0x0D;
            public int TransactTimeout = 1000;
            public byte[] RxFilter;
            public byte[] TxTerminator;
        }
    }
}
