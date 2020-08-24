using System;

namespace SH_OBD_DLL {
    /// <summary>
    /// 用于发送命令的OBD参数
    /// </summary>
    public class OBDParameter {
        private int m_parameter;
        public int Parameter {
            get { return m_parameter; }
            set {
                m_parameter = value;
                string strParam;
                if (m_parameter > 0xFF) {
                    strParam = m_parameter.ToString("X4");
                } else {
                    strParam = m_parameter.ToString("X2");
                }
                if (m_OBDRequest.Length > 2) {
                    m_OBDRequest = m_OBDRequest.Substring(0, 2) + strParam;
                } else {
                    m_OBDRequest += strParam;
                }
            }
        }
        private string m_OBDRequest;
        public string OBDRequest {
            get { return m_OBDRequest; }
            set {
                m_OBDRequest = value;
                if (m_OBDRequest.Length <= 2) {
                    try {
                        Service = Convert.ToInt32(m_OBDRequest, 16);
                    } catch (Exception) {
                        Service = 0;
                    }
                    m_parameter = 0;
                } else {
                    try {
                        Service = Convert.ToInt32(m_OBDRequest.Substring(0, 2), 16);
                    } catch (Exception) {
                        Service = 0;
                    }
                    try {
                        m_parameter = Convert.ToInt32(m_OBDRequest.Substring(2), 16);
                    } catch (Exception) {
                        m_parameter = 0;
                    }
                }
            }
        }
        public int Service { get; set; }
        public int SubParameter { get; set; }
        public int ValueTypes { get; set; }

        public OBDParameter(int service, int parameter, int subParameter, int valueTypes) {
            if (parameter > 0xFF) {
                m_OBDRequest = service.ToString("X2") + parameter.ToString("X4");
            } else {
                m_OBDRequest = service.ToString("X2") + parameter.ToString("X2");
            }
            Service = service;
            m_parameter = parameter;
            SubParameter = subParameter;
            ValueTypes = valueTypes;
        }

        public OBDParameter() {
            m_OBDRequest = "";
            Service = 0;
            m_parameter = 0;
            SubParameter = 0;
            ValueTypes = 0;
        }

        public OBDParameter GetCopy() {
            OBDParameter p = new OBDParameter {
                ValueTypes = ValueTypes,
                m_OBDRequest = OBDRequest,
                m_parameter = Parameter,
                Service = Service,
                SubParameter = SubParameter,
            };
            return p;
        }

        public OBDParameter GetFreezeFrameCopy(int iFrame) {
            OBDParameter copy = GetCopy();
            copy.Service = 2;
            copy.OBDRequest = "02" + copy.OBDRequest.Substring(2, 2) + iFrame.ToString("D2");
            return copy;
        }

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
