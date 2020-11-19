using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace SH_OBD_DLL {
    public abstract class OBDDevice {
        protected string _DeviceDes;
        protected string _DeviceID;
        protected Logger _log;
        protected OBDParser _Parser;
        protected OBDCommELM _CommELM;
        protected int[] _xattr;
        public bool Online {
            get {
                return _CommELM.Online;
            }
        }
        public string DeviceDesString {
            get {
                return _DeviceDes;
            }
        }
        public string DeviceIDString {
            get {
                return _DeviceID;
            }
        }

        protected OBDDevice(DllSettings settings, Logger log, int[] xattr) {
            _log = log;
            _CommELM = new OBDCommELM(settings, log);
            _xattr = xattr;
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