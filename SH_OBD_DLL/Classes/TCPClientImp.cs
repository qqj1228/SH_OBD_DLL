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
        private TcpClient m_client;
        private NetworkStream m_clientStream;
        private byte[] m_recvBuf;
        public event EventHandler<RecvMsgEventArgs> RecvedMsg;
        private Task m_taskRecv;

        public TCPClientImp(string strHostName, int iPort) {
            m_strHostName = strHostName;
            m_iPort = iPort;
        }

        ~TCPClientImp() {
            Close();
        }

        public void ConnectServer() {
            try {
                m_client = new TcpClient(m_strHostName, m_iPort);
                m_clientStream = m_client.GetStream();
                m_recvBuf = new byte[BUF_SIZE];
                //m_taskRecv = Task.Factory.StartNew(RecvMsg);
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
            m_taskRecv = Task.Factory.StartNew(RecvMsg);
            m_clientStream.Write(data, offset, count);
            m_clientStream.Flush();
        }

        public void SendData(string strMsg) {
            m_taskRecv = Task.Factory.StartNew(RecvMsg);
            byte[] sendMessage = Encoding.UTF8.GetBytes(strMsg);
            m_clientStream.Write(sendMessage, 0, sendMessage.Length);
            m_clientStream.Flush();
        }

        private void RecvMsg() {
            RecvMsgEventArgs args = new RecvMsgEventArgs(BUF_SIZE);
            int iStart = 0;
            int bytesRead;
            args.Message = "";
            args.RecvBuf = new byte[BUF_SIZE];
            try {
                do {
                    bytesRead = m_clientStream.Read(m_recvBuf, 0, BUF_SIZE);
                    for (int i = iStart; i < iStart + bytesRead; i++) {
                        args.RecvBuf[i] = m_recvBuf[i];
                    }
                    iStart += bytesRead;
                    args.Message += Encoding.UTF8.GetString(m_recvBuf, 0, bytesRead);
                } while (m_clientStream.DataAvailable);
            } catch (Exception) {
                RecvedMsg?.Invoke(this, args);
                throw;
            }
            RecvedMsg?.Invoke(this, args);
        }

    }

    public class RecvMsgEventArgs : EventArgs {
        public byte[] RecvBuf;
        public string Message;

        public RecvMsgEventArgs(int iSize) {
            RecvBuf = new byte[iSize];
        }
    }

}
