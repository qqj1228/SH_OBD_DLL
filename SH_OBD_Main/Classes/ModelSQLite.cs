using LibBase;
using SH_OBD_DLL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace SH_OBD_Main {
    public class ModelSQLite : ModelBase {
        private readonly DBandMES _dbandMES;

        public ModelSQLite(DBandMES dbandMES, Logger log) {
            _dbandMES = dbandMES;
            ModelParameter dbParam = new ModelParameter {
                DataBaseType = DataBaseType.SQLite,
                DBorService = _dbandMES.DBName
            };
            InitDataBase(dbParam, log);
        }

        public enum FilterTime : int {
            NoFilter = 0,
            Day = 1,
            Week = 2,
            Month = 3
        }

        public void GetRecordsFilterTime(DataTable dt, Dictionary<string, string> whereDic, FilterTime time, int pageNum, int pageSize) {
            string strSQL = "select distinct ";
            int lenBefore = strSQL.Length;
            for (int i = 0; i < dt.Columns.Count; i++) {
                strSQL += dt.Columns[i] + ", ";
            }
            if (strSQL.Length == lenBefore) {
                strSQL += "*";
            } else {
                strSQL = strSQL.Substring(0, strSQL.Length - 2);
            }
            strSQL += " from " + dt.TableName + " where ";
            foreach (string key in whereDic.Keys) {
                strSQL += key + " = '" + whereDic[key] + "' and ";
            }
            string strTimeStart = DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd");
            switch (time) {
            case FilterTime.Week:
                strTimeStart = DateTime.Now.AddDays(-6).ToLocalTime().ToString("yyyy-MM-dd");
                break;
            case FilterTime.Month:
                strTimeStart = DateTime.Now.AddDays(-29).ToLocalTime().ToString("yyyy-MM-dd");
                break;
            }
            string strTimeEnd = DateTime.Now.AddDays(1).ToLocalTime().ToString("yyyy-MM-dd");
            strSQL += "WriteTime > '" + strTimeStart + "' and WriteTime < '" + strTimeEnd + "' order by WriteTime ";
            strSQL += "limit " + pageSize.ToString() + " offset " + ((pageNum - 1) * pageSize).ToString();
            Query(strSQL, dt);
        }

        public object GetRecordsCount(string strTable, string[] columns, Dictionary<string, string> whereDic, FilterTime time) {
            string strSQL = "select count(distinct ";
            foreach (string col in columns) {
                strSQL += col + ", ";
            }
            strSQL = strSQL.Substring(0, strSQL.Length - 2);
            strSQL += ") from " + strTable + " where ";
            foreach (string key in whereDic.Keys) {
                strSQL += key + " = '" + whereDic[key] + "' and ";
            }
            string strTimeStart = DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd");
            switch (time) {
            case FilterTime.Week:
                strTimeStart = DateTime.Now.AddDays(-6).ToLocalTime().ToString("yyyy-MM-dd");
                break;
            case FilterTime.Month:
                strTimeStart = DateTime.Now.AddDays(-29).ToLocalTime().ToString("yyyy-MM-dd");
                break;
            }
            string strTimeEnd = DateTime.Now.AddDays(1).ToLocalTime().ToString("yyyy-MM-dd");
            strSQL += "WriteTime > '" + strTimeStart + "' and WriteTime < '" + strTimeEnd + "'";
            return QueryOne(strSQL);
        }

        public bool ModifyRecords(DataTable dt) {
            for (int i = 0; i < dt.Rows.Count; i++) {
                Dictionary<string, string> whereDic = new Dictionary<string, string> {
                    { "VIN", dt.Rows[i]["VIN"].ToString() },
                    { "ECU_ID", dt.Rows[i]["ECU_ID"].ToString() }
                };
                string strSQL = "";
                DataTable dtTemp = new DataTable(dt.TableName);
                GetRecords(dtTemp, whereDic);
                if (dtTemp.Rows.Count > 0) {
                    strSQL = "update " + dt.TableName + " set ";
                    for (int j = 0; j < dt.Columns.Count; j++) {
                        strSQL += dt.Columns[j].ColumnName + " = '" + dt.Rows[i][j].ToString() + "', ";
                    }
                    strSQL += "WriteTime = '" + DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") + "' where ";
                    foreach (string key in whereDic.Keys) {
                        strSQL += key + " = '" + whereDic[key] + "' and ";
                    }
                    strSQL = strSQL.Substring(0, strSQL.Length - 5);
                } else if (dtTemp.Rows.Count == 0) {
                    strSQL = "insert into " + dt.TableName + " (";
                    for (int j = 0; j < dt.Columns.Count; j++) {
                        strSQL += dt.Columns[j].ColumnName + ", ";
                    }
                    strSQL = strSQL.Substring(0, strSQL.Length - 2) + ") values ('";

                    for (int j = 0; j < dt.Columns.Count; j++) {
                        strSQL += dt.Rows[i][j].ToString() + "', '";
                    }
                    strSQL = strSQL.Substring(0, strSQL.Length - 3) + ")";
                } else if (dtTemp.Rows.Count < 0) {
                    return false;
                }
                ExecuteNonQuery(strSQL);
            }
            return true;
        }

        public int UpdateUpload(string strVIN, string strUpload) {
            string strSQL = "update OBDData set Upload = '" + strUpload + "' where VIN = '" + strVIN + "'";
            return ExecuteNonQuery(strSQL);
        }

        public string GetPassWord() {
            string strSQL = "select PassWord from OBDUser where UserName = 'admin'";
            object result = QueryOne(strSQL);
            if (result != null) {
                return result.ToString();
            } else {
                return "";
            }
        }

        public int SetPassWord(string strPwd) {
            string strSQL = "update OBDUser set PassWord = '" + strPwd + "' where UserName = 'admin'";
            return ExecuteNonQuery(strSQL);
        }

        public string GetSN() {
            string strSQL = "select SN from OBDUser where ID = '1'";
            object result = QueryOne(strSQL);
            if (result != null) {
                return result.ToString();
            } else {
                return "";
            }
        }

        public int SetSN(string strSN) {
            string strSQL = "update OBDUser set SN = '" + strSN + "' where ID = '1'";
            return ExecuteNonQuery(strSQL);
        }

        public int DeleteAllRecords(string strTable) {
            return DeleteRecord(strTable, null);
        }

        public int DeleteRecordByID(string strTable, string strID) {
            return DeleteRecords(strTable, "ID", new List<string>() { strID });
        }

        public int ResetTableID(string strTable, int iStart = 0) {
            string strSQL = "UPDATE sqlite_sequence SET seq ='" + iStart.ToString() + "' WHERE name = '" + strTable + "'";
            return ExecuteNonQuery(strSQL);
        }

    }
}
