using SH_OBD_DLL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace SH_OBD_Main {
    public class ModelSQLite {
        public string StrConn { get; set; }
        private readonly Logger _log;
        private readonly DBandMES _dbandMES;

        public ModelSQLite(DBandMES dbandMES, Logger log) {
            _log = log;
            _dbandMES = dbandMES;
            StrConn = "";
            ReadConfig();
        }

        void ReadConfig() {
            StrConn = "data source=" + _dbandMES.DBName;
        }

        public void ShowDB(string strTable) {
            string strSQL = "select * from " + strTable;
            using (SQLiteConnection sqliteConn = new SQLiteConnection(StrConn)) {
                sqliteConn.Open();
                SQLiteCommand sqliteCmd = new SQLiteCommand(strSQL, sqliteConn);
                SQLiteDataReader sqliteData = sqliteCmd.ExecuteReader();
                string str = "";
                int c = sqliteData.FieldCount;
                while (sqliteData.Read()) {
                    for (int i = 0; i < c; i++) {
                        object obj = sqliteData.GetValue(i);
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
                sqliteCmd.Dispose();
                sqliteConn.Close();
            }
        }

        private DataTable GetTableColumnsSchema(string strTable) {
            DataTable schema = new DataTable();
            using (SQLiteConnection sqliteConn = new SQLiteConnection(StrConn)) {
                try {
                    sqliteConn.Open();
                    schema = sqliteConn.GetSchema("Columns", new string[] { null, null, strTable });
                    schema.DefaultView.Sort = "ORDINAL_POSITION";
                    schema = schema.DefaultView.ToTable();
                } catch (Exception ex) {
                    _log.TraceError("==> SQL ERROR: " + ex.Message);
                    throw;
                } finally {
                    if (sqliteConn.State != ConnectionState.Closed) {
                        sqliteConn.Close();
                    }
                }
            }
            return schema;
        }

        public string[] GetColumnsName(string strTable) {
            DataTable schema = GetTableColumnsSchema(strTable);
            string[] columns = new string[schema.Rows.Count];
            for (int i = 0; i < schema.Rows.Count; i++) {
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
        }

        public Dictionary<string, int> GetTableColumnsDic(string strTable) {
            Dictionary<string, int> colDic = new Dictionary<string, int>();
            string[] cols = GetColumnsName(strTable);
            for (int i = 0; i < cols.Length; i++) {
                colDic.Add(cols[i], i);
            }
            return colDic;
        }

        public void GetEmptyTable(DataTable dt) {
            string strSQL = "select * from " + dt.TableName + " limit 1";
            using (SQLiteConnection sqliteConn = new SQLiteConnection(StrConn)) {
                try {
                    sqliteConn.Open();
                    using (SQLiteTransaction sqliteTrans = sqliteConn.BeginTransaction()) {
                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(strSQL, sqliteConn)) {
                            adapter.Fill(dt);
                        }
                        sqliteTrans.Commit();
                    }
                    dt.Clear();
                } catch (Exception ex) {
                    _log.TraceError("==> Error SQL: " + strSQL);
                    _log.TraceError("==> SQL ERROR: " + ex.Message);
                    throw;
                } finally {
                    if (sqliteConn.State != ConnectionState.Closed) {
                        sqliteConn.Close();
                    }
                }
            }
        }

        public void InsertDB(DataTable dt, string primaryKey = "ID") {
            string columns = " (";
            string row = " values (";
            for (int i = 0; i < dt.Columns.Count; i++) {
                if (primaryKey != dt.Columns[i].ColumnName) {
                    columns += dt.Columns[i].ColumnName + ",";
                    row += "@" + dt.Columns[i].ColumnName + ",";
                }
            }
            columns = columns.Substring(0, columns.Length - 1) + ")";
            row = row.Substring(0, row.Length - 1) + ")";
            string strSQL = "insert into " + dt.TableName + columns + row;

            DataTable schema = GetTableColumnsSchema(dt.TableName);
            using (SQLiteConnection sqliteConn = new SQLiteConnection(StrConn)) {
                try {
                    sqliteConn.Open();
                    using (SQLiteTransaction sqliteTrans = sqliteConn.BeginTransaction()) {
                        using (SQLiteCommand sqliteCmd = new SQLiteCommand(strSQL, sqliteConn)) {
                            sqliteCmd.CommandText = strSQL;
                            for (int i = 0; i < dt.Rows.Count; i++) {
                                string strDisplaySQL = strSQL;
                                for (int j = 0; j < dt.Columns.Count; j++) {
                                    if (primaryKey != dt.Columns[j].ColumnName) {
                                        DbType dbType = SqlDbTypeToDbType(schema.Rows[j]["DATA_TYPE"].ToString());
                                        int length = Convert.ToInt32(schema.Rows[j]["CHARACTER_MAXIMUM_LENGTH"]);
                                        string strParaName = "@" + dt.Columns[j].ColumnName;
                                        sqliteCmd.Parameters.Add(strParaName, dbType, length);
                                        sqliteCmd.Parameters[strParaName].Value = dt.Rows[i][j].ToString();
                                        strDisplaySQL = strDisplaySQL.Replace(strParaName, sqliteCmd.Parameters[strParaName].Value.ToString());
                                    }
                                }
                                _log.TraceInfo(string.Format("==> SQL: {0}", strDisplaySQL));
                                _log.TraceInfo(string.Format("==> Insert {0} record(s)", sqliteCmd.ExecuteNonQuery()));
                            }
                        }
                        sqliteTrans.Commit();
                    }
                } catch (Exception ex) {
                    _log.TraceError("==> SQL ERROR: " + ex.Message);
                    throw;
                } finally {
                    if (sqliteConn.State != ConnectionState.Closed) {
                        sqliteConn.Close();
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

                using (SQLiteConnection sqliteConn = new SQLiteConnection(StrConn)) {
                    SQLiteCommand sqliteCmd = new SQLiteCommand(strSQL, sqliteConn);
                    try {
                        sqliteConn.Open();
                        _log.TraceInfo(string.Format("==> SQL: {0}", strSQL));
                        _log.TraceInfo(string.Format("==> Update {0} record(s)", sqliteCmd.ExecuteNonQuery()));
                    } catch (Exception ex) {
                        _log.TraceError("==> SQL ERROR: " + ex.Message);
                        throw;
                    } finally {
                        sqliteCmd.Dispose();
                        sqliteConn.Close();
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
                using (SQLiteConnection sqliteConn = new SQLiteConnection(StrConn)) {
                    SQLiteCommand sqliteCmd = new SQLiteCommand(strSQL, sqliteConn);
                    try {
                        sqliteConn.Open();
                        _log.TraceInfo(string.Format("==> SQL: {0}", strSQL));
                        count = sqliteCmd.ExecuteNonQuery();
                        _log.TraceInfo(string.Format("==> {0} record(s) affected", count));
                    } catch (Exception ex) {
                        _log.TraceError("==> SQL ERROR: " + ex.Message);
                    } finally {
                        sqliteCmd.Dispose();
                        sqliteConn.Close();
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
                using (SQLiteConnection sqliteConn = new SQLiteConnection(StrConn)) {
                    SQLiteCommand sqliteCmd = new SQLiteCommand(strSQL, sqliteConn);
                    sqliteConn.Open();
                    SQLiteDataReader sqliteData = sqliteCmd.ExecuteReader();
                    count = sqliteData.FieldCount;
                    rowList = new List<string[]>();
                    while (sqliteData.Read()) {
                        string[] items = new string[count];
                        for (int i = 0; i < count; i++) {
                            object obj = sqliteData.GetValue(i);
                            if (obj.GetType() == typeof(DateTime)) {
                                items[i] = ((DateTime)obj).ToString("yyyy-MM-dd HH:mm:ss");
                            } else {
                                items[i] = obj.ToString();
                            }
                        }
                        rowList.Add(items);
                    }
                    sqliteCmd.Dispose();
                    sqliteConn.Close();
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
            _log.TraceInfo("==> SQL: " + strSQL);
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
            _log.TraceInfo("==> SQL: " + strSQL);
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
            _log.TraceInfo("==> SQL: " + strSQL);
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
            _log.TraceInfo("==> SQL: " + strSQL);
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
                    strSQL += "WriteTime = '" + DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") + "' where ";
                    foreach (string key in whereDic.Keys) {
                        strSQL += key + " = '" + whereDic[key] + "' and ";
                    }
                    strSQL = strSQL.Substring(0, strSQL.Length - 5);
                } else if (count == 0) {
                    strSQL = "insert into " + dt.TableName + " (";
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
            _log.TraceInfo("==> SQL: " + strSQL);
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

        public int DeleteDB(string strTable, string strID) {
            string strSQL;
            if (strID == null) {
                strSQL = "delete from " + strTable;
            } else {
                strSQL = "delete from " + strTable + " where ID = '" + strID + "'";
            }
            _log.TraceInfo("==> SQL: " + strSQL);
            return RunSQL(strSQL);
        }

        public int ResetTableID(string strTable, int iStart = 0) {
            string strSQL = "UPDATE sqlite_sequence SET seq ='" + iStart.ToString() + "' WHERE name = '" + strTable + "'";
            _log.TraceInfo("==> SQL: " + strSQL);
            return RunSQL(strSQL);
        }

        /// <summary>
        /// sql数据类型（如：varchar）转换为DbType类型
        /// </summary>
        /// <param name="sqlTypeString"></param>
        /// <returns></returns>
        public static DbType SqlDbTypeToDbType(string sqlTypeString) {
            DbType dbType;
            switch (sqlTypeString.ToLower()) {
            case "bigint":
                dbType = DbType.Int64;
                break;
            case "binary":
                dbType = DbType.Binary;
                break;
            case "bit":
                dbType = DbType.Byte;
                break;
            case "char":
                dbType = DbType.String;
                break;
            case "datetime":
                dbType = DbType.DateTime;
                break;
            case "decimal":
                dbType = DbType.Decimal;
                break;
            case "double":
            case "float":
                dbType = DbType.Double;
                break;
            case "image":
                dbType = DbType.Binary;
                break;
            case "int":
            case "integer":
                dbType = DbType.Int32;
                break;
            case "money":
                dbType = DbType.Currency;
                break;
            case "nchar":
                dbType = DbType.String;
                break;
            case "ntext":
                dbType = DbType.String;
                break;
            case "numeric":
                dbType = DbType.Decimal;
                break;
            case "nvarchar":
                dbType = DbType.String;
                break;
            case "smalldatetime":
                dbType = DbType.DateTime;
                break;
            case "real":
                dbType = DbType.Single;
                break;
            case "smallint":
                dbType = DbType.Int16;
                break;
            case "smallmoney":
                dbType = DbType.Currency;
                break;
            case "sql_variant":
                dbType = DbType.Object;
                break;
            case "text":
                dbType = DbType.String;
                break;
            case "timestamp":
                dbType = DbType.Binary;
                break;
            case "tinyint":
                dbType = DbType.Byte;
                break;
            case "uniqueidentifier":
                dbType = DbType.Guid;
                break;
            case "varbinary":
                dbType = DbType.Binary;
                break;
            case "varchar":
                dbType = DbType.String;
                break;
            case "xml":
                dbType = DbType.Xml;
                break;
            default:
                dbType = DbType.Object;
                break;
            }
            return dbType;
        }

        /// <summary>
        /// sql数据类型（如：varchar）转换为SqlDbType类型
        /// </summary>
        /// <param name="sqlTypeString"></param>
        /// <returns></returns>
        public static SqlDbType SqlTypeToSqlDbType(string sqlTypeString) {
            SqlDbType dbType;
            switch (sqlTypeString.ToLower()) {
            case "int":
                dbType = SqlDbType.Int;
                break;
            case "varchar":
                dbType = SqlDbType.VarChar;
                break;
            case "bit":
                dbType = SqlDbType.Bit;
                break;
            case "datetime":
                dbType = SqlDbType.DateTime;
                break;
            case "decimal":
                dbType = SqlDbType.Decimal;
                break;
            case "float":
                dbType = SqlDbType.Float;
                break;
            case "image":
                dbType = SqlDbType.Image;
                break;
            case "money":
                dbType = SqlDbType.Money;
                break;
            case "ntext":
                dbType = SqlDbType.NText;
                break;
            case "nvarchar":
                dbType = SqlDbType.NVarChar;
                break;
            case "smalldatetime":
                dbType = SqlDbType.SmallDateTime;
                break;
            case "smallint":
                dbType = SqlDbType.SmallInt;
                break;
            case "text":
                dbType = SqlDbType.Text;
                break;
            case "bigint":
                dbType = SqlDbType.BigInt;
                break;
            case "binary":
                dbType = SqlDbType.Binary;
                break;
            case "char":
                dbType = SqlDbType.Char;
                break;
            case "nchar":
                dbType = SqlDbType.NChar;
                break;
            case "numeric":
                dbType = SqlDbType.Decimal;
                break;
            case "real":
                dbType = SqlDbType.Real;
                break;
            case "smallmoney":
                dbType = SqlDbType.SmallMoney;
                break;
            case "sql_variant":
                dbType = SqlDbType.Variant;
                break;
            case "timestamp":
                dbType = SqlDbType.Timestamp;
                break;
            case "tinyint":
                dbType = SqlDbType.TinyInt;
                break;
            case "uniqueidentifier":
                dbType = SqlDbType.UniqueIdentifier;
                break;
            case "varbinary":
                dbType = SqlDbType.VarBinary;
                break;
            case "xml":
                dbType = SqlDbType.Xml;
                break;
            default:
                // 默认为Object
                dbType = SqlDbType.Variant;
                break;
            }
            return dbType;
        }

        /// <summary>
        /// SqlDbType转换为C#数据类型
        /// </summary>
        /// <param name="sqlType"></param>
        /// <returns></returns>
        public static Type SqlType2CsharpType(SqlDbType sqlType) {
            switch (sqlType) {
            case SqlDbType.BigInt:
                return typeof(Int64);
            case SqlDbType.Binary:
                return typeof(Object);
            case SqlDbType.Bit:
                return typeof(Boolean);
            case SqlDbType.Char:
                return typeof(String);
            case SqlDbType.DateTime:
                return typeof(DateTime);
            case SqlDbType.Decimal:
                return typeof(Decimal);
            case SqlDbType.Float:
                return typeof(Double);
            case SqlDbType.Image:
                return typeof(Object);
            case SqlDbType.Int:
                return typeof(Int32);
            case SqlDbType.Money:
                return typeof(Decimal);
            case SqlDbType.NChar:
                return typeof(String);
            case SqlDbType.NText:
                return typeof(String);
            case SqlDbType.NVarChar:
                return typeof(String);
            case SqlDbType.Real:
                return typeof(Single);
            case SqlDbType.SmallDateTime:
                return typeof(DateTime);
            case SqlDbType.SmallInt:
                return typeof(Int16);
            case SqlDbType.SmallMoney:
                return typeof(Decimal);
            case SqlDbType.Text:
                return typeof(String);
            case SqlDbType.Timestamp:
                return typeof(Object);
            case SqlDbType.TinyInt:
                return typeof(Byte);
            case SqlDbType.Udt: // 自定义的数据类型
                return typeof(Object);
            case SqlDbType.UniqueIdentifier:
                return typeof(Object);
            case SqlDbType.VarBinary:
                return typeof(Object);
            case SqlDbType.VarChar:
                return typeof(String);
            case SqlDbType.Variant:
                return typeof(Object);
            case SqlDbType.Xml:
                return typeof(Object);
            default:
                return null;
            }
        }
    }
}
