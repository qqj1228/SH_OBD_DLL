﻿using SH_OBD_DLL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace SH_OBD_Main {
    public class Model {
        public string StrConn { get; set; }
        public readonly Logger _log;
        public DBandMES _dbandMES;

        public Model(DBandMES dbandMES, Logger log) {
            _log = log;
            _dbandMES = dbandMES;
            this.StrConn = "";
            ReadConfig();
        }

        void ReadConfig() {
            StrConn = "user id=" + _dbandMES.UserName + ";";
            StrConn += "password=" + _dbandMES.PassWord + ";";
            StrConn += "database=" + _dbandMES.DBName + ";";
            StrConn += "data source=" + _dbandMES.IP + "," + _dbandMES.Port;
        }

        public void ShowDB(string strTable) {
            string strSQL = "select * from " + strTable;

            using (SqlConnection sqlConn = new SqlConnection(StrConn)) {
                sqlConn.Open();
                SqlCommand sqlCmd = new SqlCommand(strSQL, sqlConn);
                SqlDataReader sqlData = sqlCmd.ExecuteReader();
                string str = "";
                int c = sqlData.FieldCount;
                while (sqlData.Read()) {
                    for (int i = 0; i < c; i++) {
                        object obj = sqlData.GetValue(i);
                        if (obj.GetType() == typeof(DateTime)) {
                            str += ((DateTime)obj).ToString("yyyy-MM-dd HH:mm:ss") + "\t";
                        } else {
                            str += obj.ToString() + "\t";
                        }
                    }
                    str += "\n";
                }
                str = str.Trim('\n');
                _log.TraceInfo(str);
                sqlCmd.Dispose();
                sqlConn.Close();
            }
        }

        public string[] GetTableColumns(string strTable) {
            using (SqlConnection sqlConn = new SqlConnection(StrConn)) {
                try {
                    sqlConn.Open();
                    DataTable schema = sqlConn.GetSchema("Columns", new string[] { null, null, strTable });
                    schema.DefaultView.Sort = "ORDINAL_POSITION";
                    schema = schema.DefaultView.ToTable();
                    int count = schema.Rows.Count;
                    string[] columns = new string[count];
                    for (int i = 0; i < count; i++) {
                        DataRow row = schema.Rows[i];
                        foreach (DataColumn col in schema.Columns) {
                            if (col.Caption == "COLUMN_NAME") {
                                if (col.DataType.Equals(typeof(DateTime))) {
                                    columns[i] = string.Format("{0:d}", row[col]);
                                } else if (col.DataType.Equals(typeof(decimal))) {
                                    columns[i] = string.Format("{0:C}", row[col]);
                                } else {
                                    columns[i] = string.Format("{0}", row[col]);
                                }
                            }
                        }
                    }
                    return columns;
                } catch (Exception ex) {
                    _log.TraceError("==> SQL ERROR: " + ex.Message);
                } finally {
                    sqlConn.Close();
                }
            }
            return new string[] { };
        }

        public Dictionary<string, int> GetTableColumnsDic(string strTable) {
            Dictionary<string, int> colDic = new Dictionary<string, int>();
            string[] cols = GetTableColumns(strTable);
            for (int i = 0; i < cols.Length; i++) {
                colDic.Add(cols[i], i);
            }
            return colDic;
        }

        public void InsertDB(DataTable dt) {
            string columns = " (";
            for (int i = 0; i < dt.Columns.Count; i++) {
                columns += dt.Columns[i].ColumnName + ",";
            }
            columns = columns.Substring(0, columns.Length - 1) + ")";

            for (int i = 0; i < dt.Rows.Count; i++) {
                string row = " values ('";
                for (int j = 0; j < dt.Columns.Count; j++) {
                    row += dt.Rows[i][j].ToString() + "','";
                }
                row = row.Substring(0, row.Length - 2) + ")";
                string strSQL = "insert into " + dt.TableName + columns + row;

                using (SqlConnection sqlConn = new SqlConnection(StrConn)) {
                    SqlCommand sqlCmd = new SqlCommand(strSQL, sqlConn);
                    try {
                        sqlConn.Open();
                        _log.TraceInfo(string.Format("==> T-SQL: {0}", strSQL));
                        _log.TraceInfo(string.Format("==> Insert {0} record(s)", sqlCmd.ExecuteNonQuery()));
                    } catch (Exception ex) {
                        _log.TraceError("==> SQL ERROR: " + ex.Message);
                    } finally {
                        sqlCmd.Dispose();
                        sqlConn.Close();
                    }
                }
            }
        }

        public void UpdateDB(DataTable dt, Dictionary<string, string> whereDic) {
            for (int i = 0; i < dt.Rows.Count; i++) {
                string strSQL = "update " + dt.TableName + " set ";
                for (int j = 0; j < dt.Columns.Count; j++) {
                    strSQL += dt.Columns[j].ColumnName + " = '" + dt.Rows[i][j].ToString() + "', ";
                }
                strSQL = strSQL.Substring(0, strSQL.Length - 2);
                strSQL += " where ";
                foreach (string key in whereDic.Keys) {
                    strSQL += key + " = '" + whereDic[key] + "' and ";
                }
                strSQL = strSQL.Substring(0, strSQL.Length - 5);

                using (SqlConnection sqlConn = new SqlConnection(StrConn)) {
                    SqlCommand sqlCmd = new SqlCommand(strSQL, sqlConn);
                    try {
                        sqlConn.Open();
                        _log.TraceInfo(string.Format("==> T-SQL: {0}", strSQL));
                        _log.TraceInfo(string.Format("==> Update {0} record(s)", sqlCmd.ExecuteNonQuery()));
                    } catch (Exception ex) {
                        _log.TraceError("==> SQL ERROR: " + ex.Message);
                    } finally {
                        sqlCmd.Dispose();
                        sqlConn.Close();
                    }
                }

            }
        }

        int RunSQL(string strSQL) {
            int count = 0;
            if (strSQL.Length == 0) {
                return -1;
            }
            try {
                using (SqlConnection sqlConn = new SqlConnection(StrConn)) {
                    SqlCommand sqlCmd = new SqlCommand(strSQL, sqlConn);
                    try {
                        sqlConn.Open();
                        _log.TraceInfo(string.Format("==> T-SQL: {0}", strSQL));
                        count = sqlCmd.ExecuteNonQuery();
                        _log.TraceInfo(string.Format("==> {0} record(s) affected", count));
                    } catch (Exception ex) {
                        _log.TraceError("==> SQL ERROR: " + ex.Message);
                    } finally {
                        sqlCmd.Dispose();
                        sqlConn.Close();
                    }
                }
            } catch (Exception ex) {
                _log.TraceError("==> SQL ERROR: " + ex.Message);
            }
            return count;
        }

        string[,] SelectDB(string strSQL) {
            string[,] records = null;
            try {
                int count = 0;
                List<string[]> rowList;
                using (SqlConnection sqlConn = new SqlConnection(StrConn)) {
                    SqlCommand sqlCmd = new SqlCommand(strSQL, sqlConn);
                    sqlConn.Open();
                    SqlDataReader sqlData = sqlCmd.ExecuteReader();
                    count = sqlData.FieldCount;
                    rowList = new List<string[]>();
                    while (sqlData.Read()) {
                        string[] items = new string[count];
                        for (int i = 0; i < count; i++) {
                            object obj = sqlData.GetValue(i);
                            if (obj.GetType() == typeof(DateTime)) {
                                items[i] = ((DateTime)obj).ToString("yyyy-MM-dd HH:mm:ss");
                            } else {
                                items[i] = obj.ToString();
                            }
                        }
                        rowList.Add(items);
                    }
                    sqlCmd.Dispose();
                    sqlConn.Close();
                }
                records = new string[rowList.Count, count];
                for (int i = 0; i < rowList.Count; i++) {
                    for (int j = 0; j < count; j++) {
                        records[i, j] = rowList[i][j];
                    }
                }
                return records;
            } catch (Exception ex) {
                _log.TraceError("==> SQL ERROR: " + ex.Message);
            }
            return records;
        }

        public int GetRecordCount(string strTable, Dictionary<string, string> whereDic) {
            string strSQL = "select * from " + strTable + " where ";
            foreach (string key in whereDic.Keys) {
                strSQL += key + " = '" + whereDic[key] + "' and ";
            }
            strSQL = strSQL.Substring(0, strSQL.Length - 5);
            _log.TraceInfo("==> T-SQL: " + strSQL);
            string[,] strArr = SelectDB(strSQL);
            if (strArr != null) {
                return strArr.GetLength(0);
            } else {
                return -1;
            }
        }

        public string[,] GetRecords(string strTable, Dictionary<string, string> whereDic) {
            string strSQL;
            if (whereDic == null) {
                strSQL = "select * from " + strTable;
            } else {
                strSQL = "select * from " + strTable + " where ";
                foreach (string key in whereDic.Keys) {
                    strSQL += key + " = '" + whereDic[key] + "' and ";
                }
                strSQL = strSQL.Substring(0, strSQL.Length - 5);
            }
            _log.TraceInfo("==> T-SQL: " + strSQL);
            return SelectDB(strSQL);
        }

        public enum FilterTime : int {
            NoFilter = 0,
            Day = 1,
            Week = 2,
            Month = 3
        }

        public string[,] GetRecords(string strTable, string[] columns, Dictionary<string, string> whereDic, FilterTime time, int pageNum, int pageSize) {
            string strSQL = "select ";
            foreach (string col in columns) {
                strSQL += col + ", ";
            }
            strSQL = strSQL.Substring(0, strSQL.Length - 2);
            strSQL += " from " + strTable + " where ";
            foreach (string key in whereDic.Keys) {
                strSQL += key + " = '" + whereDic[key] + "' and ";
            }
            string strTimeStart = DateTime.Now.ToLocalTime().ToString("yyyyMMdd");
            switch (time) {
            case FilterTime.Week:
                strTimeStart = DateTime.Now.AddDays(-6).ToLocalTime().ToString("yyyyMMdd");
                break;
            case FilterTime.Month:
                strTimeStart = DateTime.Now.AddDays(-29).ToLocalTime().ToString("yyyyMMdd");
                break;
            }
            string strTimeEnd = DateTime.Now.AddDays(1).ToLocalTime().ToString("yyyyMMdd");
            strSQL += "WriteTime > '" + strTimeStart + "' and WriteTime < '" + strTimeEnd + "' order by ID ";
            strSQL += "offset " + ((pageNum - 1) * pageSize).ToString() + " rows fetch next " + pageSize.ToString() + " rows only";
            _log.TraceInfo("==> T-SQL: " + strSQL);
            return SelectDB(strSQL);
        }

        public string[,] GetRecordsCount(string strTable, string[] columns, Dictionary<string, string> whereDic, FilterTime time) {
            string strSQL = "select count(distinct ";
            foreach (string col in columns) {
                strSQL += col + ", ";
            }
            strSQL = strSQL.Substring(0, strSQL.Length - 2);
            strSQL += ") from " + strTable + " where ";
            foreach (string key in whereDic.Keys) {
                strSQL += key + " = '" + whereDic[key] + "' and ";
            }
            string strTimeStart = DateTime.Now.ToLocalTime().ToString("yyyyMMdd");
            switch (time) {
            case FilterTime.Week:
                strTimeStart = DateTime.Now.AddDays(-6).ToLocalTime().ToString("yyyyMMdd");
                break;
            case FilterTime.Month:
                strTimeStart = DateTime.Now.AddDays(-29).ToLocalTime().ToString("yyyyMMdd");
                break;
            }
            string strTimeEnd = DateTime.Now.AddDays(1).ToLocalTime().ToString("yyyyMMdd");
            strSQL += "WriteTime > '" + strTimeStart + "' and WriteTime < '" + strTimeEnd + "'";
            _log.TraceInfo("==> T-SQL: " + strSQL);
            return SelectDB(strSQL);
        }

        public bool ModifyDB(DataTable dt) {
            for (int i = 0; i < dt.Rows.Count; i++) {
                Dictionary<string, string> whereDic = new Dictionary<string, string> {
                    { "VIN", dt.Rows[i][0].ToString() },
                    { "ECU_ID", dt.Rows[i][1].ToString() }
                };
                string strSQL = "";
                int count = GetRecordCount(dt.TableName, whereDic);
                if (count > 0) {
                    strSQL = "update " + dt.TableName + " set ";
                    for (int j = 0; j < dt.Columns.Count; j++) {
                        strSQL += dt.Columns[j].ColumnName + " = '" + dt.Rows[i][j].ToString() + "', ";
                    }
                    strSQL += "WriteTime = '" + DateTime.Now.ToLocalTime().ToString() + "' where ";
                    foreach (string key in whereDic.Keys) {
                        strSQL += key + " = '" + whereDic[key] + "' and ";
                    }
                    strSQL = strSQL.Substring(0, strSQL.Length - 5);
                } else if (count == 0) {
                    strSQL = "insert " + dt.TableName + " (";
                    for (int j = 0; j < dt.Columns.Count; j++) {
                        strSQL += dt.Columns[j].ColumnName + ", ";
                    }
                    strSQL = strSQL.Substring(0, strSQL.Length - 2) + ") values ('";

                    for (int j = 0; j < dt.Columns.Count; j++) {
                        strSQL += dt.Rows[i][j].ToString() + "', '";
                    }
                    strSQL = strSQL.Substring(0, strSQL.Length - 3) + ")";
                } else if (count < 0) {
                    return false;
                }
                RunSQL(strSQL);
            }
            return true;
        }

        public int UpdateUpload(string strVIN, string strUpload) {
            string strSQL = "update OBDData set Upload = '" + strUpload + "' where VIN = '" + strVIN + "'";
            return RunSQL(strSQL);
        }

        public string GetPassWord() {
            string strSQL = "select PassWord from OBDUser where UserName = 'admin'";
            _log.TraceInfo("==> T-SQL: " + strSQL);
            string[,] strArr = SelectDB(strSQL);
            if (strArr != null) {
                return strArr[0, 0];
            } else {
                return "";
            }
        }

        public int SetPassWord(string strPwd) {
            string strSQL = "update OBDUser set PassWord = '" + strPwd + "' where UserName = 'admin'";
            return RunSQL(strSQL);
        }

        public void AddUploadField() {
            string strSQL = "select Upload from OBDData where ID = '1'";
            string[,] rets = SelectDB(strSQL);
            if (rets == null || rets.GetLength(0) < 1) {
                strSQL = "alter table OBDData add Upload int not null default(0)";
                RunSQL(strSQL);
                strSQL = "update OBDData set Upload = '1' where VIN = 'testvincode012345'";
                RunSQL(strSQL);
            }
        }

        public void AddSNField() {
            string strSQL = "select SN from OBDUser where ID = '1'";
            string[,] rets = SelectDB(strSQL);
            if (rets == null || rets.GetLength(0) < 1) {
                strSQL = "alter table OBDUser add SN varchar(20) null";
                RunSQL(strSQL);
            }
        }

        public void AddOBDProtocol() {
            string strSQL = "IF OBJECT_ID(N'SH_OBD.dbo.OBDProtocol') IS NULL ";
            strSQL += "Create TABLE SH_OBD.dbo.OBDProtocol (ID int IDENTITY PRIMARY KEY NOT NULL, CAR_CODE varchar(20) NOT NULL, ";
            strSQL += "Engine varchar(20), Stage varchar(20), Fuel varchar(20), Model varchar(20), Protocol varchar(50) NOT NULL)";
            RunSQL(strSQL);
        }

        public string GetSN() {
            string strSQL = "select SN from OBDUser where ID = '1'";
            string[,] rets = SelectDB(strSQL);
            string strRet;
            if (rets == null || rets.GetLength(0) < 1) {
                strRet = "";
            } else {
                strRet = rets[0, 0];
            }
            return strRet;
        }

        public int SetSN(string strSN) {
            string strSQL = "update OBDUser set SN = '" + strSN + "' where ID = '1'";
            return RunSQL(strSQL);
        }

        public bool AddProtocol(DataTable dt) {
            for (int i = 0; i < dt.Rows.Count; i++) {
                Dictionary<string, string> whereDic = new Dictionary<string, string> {
                    { "CAR_CODE", dt.Rows[i]["CAR_CODE"].ToString() },
                };
                string strSQL = "";
                int count = GetRecordCount(dt.TableName, whereDic);
                if (count > 0) {
                    strSQL = "update " + dt.TableName + " set ";
                    for (int j = 0; j < dt.Columns.Count; j++) {
                        strSQL += dt.Columns[j].ColumnName + " = '" + dt.Rows[i][j].ToString() + "', ";
                    }
                    strSQL = strSQL.Substring(0, strSQL.Length - 2);
                    strSQL += " where ";
                    foreach (string key in whereDic.Keys) {
                        strSQL += key + " = '" + whereDic[key] + "' and ";
                    }
                    strSQL = strSQL.Substring(0, strSQL.Length - 5);
                } else if (count == 0) {
                    strSQL = "insert " + dt.TableName + " (";
                    for (int j = 0; j < dt.Columns.Count; j++) {
                        strSQL += dt.Columns[j].ColumnName + ", ";
                    }
                    strSQL = strSQL.Substring(0, strSQL.Length - 2) + ") values ('";

                    for (int j = 0; j < dt.Columns.Count; j++) {
                        strSQL += dt.Rows[i][j].ToString() + "', '";
                    }
                    strSQL = strSQL.Substring(0, strSQL.Length - 3) + ")";
                } else if (count < 0) {
                    return false;
                }
                RunSQL(strSQL);
            }
            return true;
        }

        public int InsertProtocol(string CAR_CODE, string Protocol) {
            string strSQL = "insert into OBDProtocol (CAR_CODE, Protocol) values ('";
            strSQL += CAR_CODE + "', '" + Protocol + "')";
            _log.TraceInfo("==> T-SQL: " + strSQL);
            return RunSQL(strSQL);
        }

        public int UpdateProtocol(string CAR_CODE, string Protocol, string strID) {
            string strSQL = "update OBDProtocol set CAR_CODE = '";
            strSQL += CAR_CODE + "', Protocol = '" + Protocol + "' ";
            strSQL += "where ID = '" + strID + "'";
            _log.TraceInfo("==> T-SQL: " + strSQL);
            return RunSQL(strSQL);
        }

        public int DeleteProtocol(string strID) {
            string strSQL = "delete from OBDProtocol where ID = '" + strID + "'";
            _log.TraceInfo("==> T-SQL: " + strSQL);
            return RunSQL(strSQL);
        }

        public string GetProtocol(string carCode) {
            string strSQL = "select Protocol from OBDProtocol where CAR_CODE = '" + carCode + "'";
            string[,] rets = SelectDB(strSQL);
            string strRet;
            if (rets == null || rets.GetLength(0) < 1) {
                strRet = "";
            } else {
                strRet = rets[0, 0];
            }
            return strRet;
        }
    }
}