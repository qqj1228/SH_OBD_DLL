using System;

namespace SH_OBD_DLL {
    /// <summary>
    /// 用于发送命令的OBD参数
    /// </summary>
    public class OBDParameter {
        private int _parameter;
        public int Parameter {
            get { return _parameter; }
            set {
                _parameter = value;
                string strParam;
                if (_parameter > 0xFF) {
                    strParam = _parameter.ToString("X4");
                } else {
                    strParam = _parameter.ToString("X2");
                }
                if (_OBDRequest.Length > 2) {
                    _OBDRequest = _OBDRequest.Substring(0, 2) + strParam;
                } else {
                    _OBDRequest += strParam;
                }
            }
        }
        private string _OBDRequest;
        public string OBDRequest {
            get { return _OBDRequest; }
            set {
                _OBDRequest = value;
                if (_OBDRequest.Length <= 2) {
                    try {
                        Service = Convert.ToInt32(_OBDRequest, 16);
                    } catch (Exception) {
                        Service = 0;
                    }
                    _parameter = 0;
                } else {
                    try {
                        Service = Convert.ToInt32(_OBDRequest.Substring(0, 2), 16);
                    } catch (Exception) {
                        Service = 0;
                    }
                    try {
                        _parameter = Convert.ToInt32(_OBDRequest.Substring(2), 16);
                    } catch (Exception) {
                        _parameter = 0;
                    }
                }
            }
        }
        public int Service { get; set; }
        public string SignalName { get; set; }
        public int ValueTypes { get; set; }

        public OBDParameter(int service, int parameter, string signalName, int valueTypes) {
            if (parameter > 0xFF) {
                _OBDRequest = service.ToString("X2") + parameter.ToString("X4");
            } else {
                _OBDRequest = service.ToString("X2") + parameter.ToString("X2");
            }
            Service = service;
            _parameter = parameter;
            SignalName = signalName;
            ValueTypes = valueTypes;
        }

        public OBDParameter() {
            _OBDRequest = "";
            Service = 0;
            _parameter = 0;
            SignalName = "";
            ValueTypes = 0;
        }

        public OBDParameter GetCopy() {
            OBDParameter p = new OBDParameter {
                ValueTypes = ValueTypes,
                _OBDRequest = OBDRequest,
                _parameter = Parameter,
                Service = Service,
                SignalName = SignalName,
            };
            return p;
        }

        public OBDParameter GetFreezeFrameCopy(int iFrame) {
            OBDParameter copy = GetCopy();
            copy.Service = 2;
            copy.OBDRequest = "02" + copy.OBDRequest.Substring(2, 2) + iFrame.ToString("D2");
            return copy;
        }

        [Flags]
        public enum EnumValueTypes {
            Double = 0x01,
            Bool = 0x02,
            String = 0x04,
            ListString = 0x08,
            ShortString = 0x10,
            BitFlags = 0x20
        }

    }
}
