using LibBase;
using Oracle.ManagedDataAccess.Client;
using SH_OBD_DLL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace SH_OBD_Main {
    public class ModelOracle : ModelBase {
        public OracleMES _oracleMESSetting;
        public bool Connected { get; set; }
        public string IDValue { get; set; }

        public ModelOracle(OracleMES oracleMESSetting, Logger log) {
            _oracleMESSetting = oracleMESSetting;
            ModelParameter dbParam = new ModelParameter {
                DataBaseType = DataBaseType.Oracle,
                UserName = _oracleMESSetting.UserID,
                PassWord = _oracleMESSetting.PassWord,
                Host = _oracleMESSetting.Host,
                Port = _oracleMESSetting.Port,
                DBorService = _oracleMESSetting.ServiceName
            };
            InitDataBase(dbParam, log);
            Connected = false;
            IDValue = "SEQ_EM_WQPF_ID.NEXTVAL";
        }

        public bool ConnectOracle() {
            Connected = false;
            try {
                OracleConnection con = new OracleConnection(_strConn);
                con.Open();
                Connected = true;
                con.Close();
                con.Dispose();
            } catch (Exception ex) {
                _log.TraceError("Connection error: " + ex.Message);
                throw ex;
            }
            return Connected;
        }

    }
}
