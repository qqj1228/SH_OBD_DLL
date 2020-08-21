using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace SH_OBD_DLL {
    public abstract class OBDDevice {
        protected string m_DeviceDes;
        protected string m_DeviceID;
        protected Logger m_log;
        protected OBDParser m_Parser;
        protected OBDCommELM m_CommELM;
        protected int[] m_xattr;
        public bool Online {
            get {
                return m_CommELM.Online;
            }
        }
        public string DeviceDesString {
            get {
                return m_DeviceDes;
            }
        }
        public string DeviceIDString {
            get {
                return m_DeviceID;
            }
        }

        protected OBDDevice(DllSettings settings, Logger log, int[] xattr) {
            m_log = log;
            m_CommELM = new OBDCommELM(settings, log);
            m_xattr = xattr;
        }

        public abstract bool Initialize(DllSettings settings);
        public abstract bool InitializeAuto(DllSettings settings);
        public abstract bool Initialize(int iPort, int iBaud);
        public abstract bool Initialize(string strRemoteIP, int iRemotePort);
        public abstract void Disconnect();
        public abstract OBDResponseList Query(OBDParameter param);
        public abstract string Query(string cmd);
        public abstract ProtocolType GetProtocolType();
        public abstract int GetComPortIndex();
        public abstract int GetBaudRateIndex();
        public abstract void SetTimeout(int iTimeout);
        public abstract StandardType GetStandardType();
    }
}