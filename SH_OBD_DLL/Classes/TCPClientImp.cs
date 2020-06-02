using SH_OBD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SH_OBD_DLL {
    public class TCPClientImp {
        private const int BUF_SIZE = 256;
        private readonly string m_strHostName;
        private readonly int m_iPort;
        private readonly Logger m_log;
        private TcpClient m_client;
        private NetworkStream m_clientStream;
        private byte[] m_recvBuf;
        public event EventHandler<RecvMsgEventArgs> RecvedMsg;

        public TCPClientImp(string strHostName, int iPort, Logger log) {
            m_strHostName = strHostName;
            m_iPort = iPort;
            m_log = log;
        }

        ~TCPClientImp() {
            Close();
        }

        public void ConnectServer() {
            try {
                m_client = new TcpClient(m_strHostName, m_iPort);
                m_clientStream = m_client.GetStream();
                m_recvBuf = new byte[BUF_SIZE];
                RecvMsgEventArgs args = new RecvMsgEventArgs();
                m_clientStream.BeginRead(m_recvBuf, 0, BUF_SIZE, AsyncRecvMsg, args);
            } catch (Exception) {
                Close();
                throw;
            }
        }

        public void Close() {
            if (m_clientStream != null) {
                m_clientStream.Close();
            }
            if (m_client != null) {
                m_client.Close();
            }
        }

        public void SendData(byte[] data, int offset, int count) {
            m_clientStream.Write(data, offset, count);
        }

        public void SendData(string strMsg) {
            byte[] sendMessage = Encoding.UTF8.GetBytes(strMsg);
            m_clientStream.Write(sendMessage, 0, sendMessage.Length);
        }

        private void AsyncRecvMsg(IAsyncResult ar) {
            RecvMsgEventArgs args = (RecvMsgEventArgs)ar.AsyncState;
            args.RecvBytes.Clear();
            args.Message = "";
            try {
                if (m_clientStream.CanRead) {
                    // CanRead为false，说明m_clientStream流不可读，可能是m_clientStream流已关闭或发生错误，退出本次读取处理
                    // 为true，则说明m_clientStream流可读，可继续执行后续代码
                    int bytesRead = m_clientStream.EndRead(ar);
                    // 读取缓冲区大小为256字节，用于OBD诊断命令已足够，无需使用NetworkStream.DataAvailable属性
                    // 来判断是否还有数据没有读取完，需要分段多次读取
                    for (int i = 0; i < bytesRead; i++) {
                        args.RecvBytes.Add(m_recvBuf[i]);
                    }
                    args.Message += Encoding.UTF8.GetString(m_recvBuf, 0, bytesRead);
                    // 继续准备读取可能会传进来的数据
                    m_clientStream.BeginRead(m_recvBuf, 0, BUF_SIZE, AsyncRecvMsg, args);
                }
            } catch (Exception ex) {
                m_log.TraceError(ex.Message);
                Close();
            } finally {
                RecvedMsg?.Invoke(this, args);
            }
        }

        public bool TestConnect() {
            Socket cltSocket = m_client.Client;
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
                    m_log.TraceWarning(string.Format("Still Connected, but the Send would block[{0}]", ex.NativeErrorCode));
                } else {
                    bRet = false;
                    m_log.TraceError(string.Format("Disconnected: {0}[{1}]", ex.Message, ex.NativeErrorCode));
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
