using SH_OBD_DLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dyno_OBD_DLL {
    public class OBDDiagnosis {
        private readonly SH_OBD_Dll _OBDDll;
        private readonly Logger _log;
        private XmlHandler _handler;

        public OBDDiagnosis() {
            _OBDDll = new SH_OBD_Dll(".\\log\\OBD");
            _log = _OBDDll.GetLogger();
        }

        public bool OBDDiagnosisInit() {
            try {
                List<DTC> DTCDefs = Utility.LoadXmlFile<DTC>(".\\Configs\\dtc.xml");
                Dictionary<string, DTC> dicDTCDef = new Dictionary<string, DTC>();
                foreach (DTC item in DTCDefs) {
                    dicDTCDef.Add(item.Name, item);
                }
                _handler = new XmlHandler(_OBDDll, ".\\log\\XML", dicDTCDef);
            } catch (ApplicationException ex) {
                _log.TraceError(ex.Message);
                return false;
            }
            OBDInterface OBDIf = _OBDDll.GetOBDInterface();
            if (!OBDIf.DllSettingsResult) {
                OBDIf.SaveDllSettings(OBDIf.DllSettings);
                return false;
            }
            return true;
        }

        public bool ConnectOBD() {
            if (!_OBDDll.ConnectOBD()) {
                return false;
            }
            return true;
        }

        public string RequestByXMLString(string requestXml) {
            return _handler.HandleRequest(requestXml);
        }

    }

}
