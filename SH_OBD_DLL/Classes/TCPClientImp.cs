using SH_OBD_DLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SH_OBD_DLL {
    public class TCPClientImp {
        private const int BUF_SIZE = 256;
        private readonly string _strHostName;
        private readonly int _iPort;
        private readonly Logger _log;
        private TcpClient _client;
        private NetworkStream _clientStream;
        private byte[] _recvBuf;
        public event EventHandler<RecvMsgEventArgs> RecvedMsg;

        public TCPClientImp(string strHostName, int iPort, Logger log) {
            _strHostName = strHostName;
            _iPort = iPort;
            _log = log;
        }

        ~TCPClientImp() {
            Close();
        }

        public void ConnectServer() {
            try {
                _client = new TcpClient(_strHostName, _iPort);
                _clientStream = _client.GetStream();
                _recvBuf = new byte[BUF_SIZE];
                RecvMsgEventArgs args = new RecvMsgEventArgs();
                _clientStream.BeginRead(_recvBuf, 0, BUF_SIZE, AsyncRecvMsg, args);
            } catch (Exception) {
                Close();
                throw;
            }
        }

        public void Close() {
            if (_clientStream != null) {
                _clientStream.Close();
            }
            if (_client != null) {
                _client.Close();
            }
        }

        public void SendData(byte[] data, int offset, int count) {
            _clientStream.Write(data, offset, count);
        }

        public void SendData(string strMsg) {
            byte[] sendMessage = Encoding.UTF8.GetBytes(strMsg);
            _clientStream.Write(sendMessage, 0, sendMessage.Length);
        }

        private void AsyncRecvMsg(IAsyncResult ar) {
            RecvMsgEventArgs args = (RecvMsgEventArgs)ar.AsyncState;
            args.RecvBytes.Clear();
            args.Message = "";
            try {
                if (_clientStream.CanRead) {
                    // CanRead为false，说明m_clientStream流不可读，可能是m_clientStream流已关闭或发生错误，退出本次读取处理
                    // 为true，则说明m_clientStream流可读，可继续执行后续代码
                    int bytesRead = _clientStream.EndRead(ar);
                    // 读取缓冲区大小为256字节，用于OBD诊断命令已足够，无需使用NetworkStream.DataAvailable属性
                    // 来判断是否还有数据没有读取完，需要分段多次读取
                    for (int i = 0; i < bytesRead; i++) {
                        args.RecvBytes.Add(_recvBuf[i]);
                    }
                    args.Message += Encoding.UTF8.GetString(_recvBuf, 0, bytesRead);
                    // 继续准备读取可能会传进来的数据
                    _clientStream.BeginRead(_recvBuf, 0, BUF_SIZE, AsyncRecvMsg, args);
                }
            } catch (Exception ex) {
                _log.TraceError(ex.Message);
                Close();
            } finally {
                RecvedMsg?.Invoke(this, args);
            }
        }

        public bool TestConnect() {
            Socket cltSocket = _client.Client;
            bool bRet = false;
            // This is how you can determine whether a socket is still connected.
            bool blockingState = cltSocket.Blocking;
            try {
                byte[] tmp = new byte[1];
                cltSocket.Blocking = false;
                cltSocket.Send(tmp, 0, 0);
                bRet = true;
            } catch (SocketException ex) {
                // 10035 == WSAEWOULDBLOCK
                if (ex.NativeErrorCode.Equals(10035)) {
                    bRet = true;
                    _log.TraceWarning(string.Format("Still Connected, but the Send would block[{0}]", ex.NativeErrorCode));
                } else {
                    bRet = false;
                    _log.TraceError(string.Format("Disconnected: {0}[{1}]", ex.Message, ex.NativeErrorCode));
                }
            } finally {
                cltSocket.Blocking = blockingState;
            }
            return bRet;
        }

    }

    public class RecvMsgEventArgs : EventArgs {
        public List<byte> RecvBytes;
        public string Message;

        public RecvMsgEventArgs() {
            RecvBytes = new List<byte>();
            Message = "";
        }
    }

}
