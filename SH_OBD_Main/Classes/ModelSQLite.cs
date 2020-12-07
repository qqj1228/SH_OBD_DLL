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

        private DataTable GetTableColumnsSchema(string strTable) {
            DataTable schema = new DataTable();
            using (SQLiteConnection sqliteConn = new SQLiteConnection(StrConn)) {
                try {
                    sqliteConn.Open();
                    schema = sqliteConn.GetSchema("Columns", new string[] { null, null, strTable });
                    schema.DefaultView.Sort = "ORDINAL_POSITION";
                    schema = schema.DefaultView.ToTable();
                } catch (Exception ex) {
                    _log.TraceError("Get columns schema from table: " + strTable + " error");
                    _log.TraceError(ex.Message);
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
                    _log.TraceError("Error SQL: " + strSQL);
                    _log.TraceError(ex.Message);
                    throw;
                } finally {
                    if (sqliteConn.State != ConnectionState.Closed) {
                        sqliteConn.Close();
                    }
                }
            }
        }

        /// <summary>
        /// 执行update insert delete语句，失败了返回-1，成功了返回影响的行数，自动commit
        /// </summary>
        /// <param name="strSQL"></param>
        /// <returns></returns>
        private int ExecuteNonQuery(string strSQL) {
            using (SQLiteConnection sqliteConn = new SQLiteConnection(StrConn)) {
                int val = -1;
                try {
                    sqliteConn.Open();
                    SQLiteCommand sqliteCmd = new SQLiteCommand(strSQL, sqliteConn);
                    val = sqliteCmd.ExecuteNonQuery();
                    sqliteCmd.Parameters.Clear();
                } catch (Exception ex) {
                    _log.TraceError("Error SQL: " + strSQL);
                    _log.TraceError(ex.Message);
                    throw;
                } finally {
                    if (sqliteConn.State != ConnectionState.Closed) {
                        sqliteConn.Close();
                    }
                }
                return val;
            }
        }

        /// <summary>
        /// 执行select语句，自动commit
        /// </summary>
        /// <param name="strSQL"></param>
        /// <param name="dt"></param>
        private void Query(string strSQL, DataTable dt) {
            using (SQLiteConnection sqliteConn = new SQLiteConnection(StrConn)) {
                try {
                    sqliteConn.Open();
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(strSQL, sqliteConn);
                    adapter.Fill(dt);
                } catch (Exception ex) {
                    _log.TraceError("Error SQL: " + strSQL);
                    _log.TraceError(ex.Message);
                    throw;
                } finally {
                    if (sqliteConn.State != ConnectionState.Closed) {
                        sqliteConn.Close();
                    }
                }
            }
        }

        /// <summary>
        /// 执行select语句，返回第一个数据对象，自动commit
        /// </summary>
        /// <param name="strSQL"></param>
        /// <returns></returns>
        private object QueryOne(string strSQL) {
            using (SQLiteConnection sqliteConn = new SQLiteConnection(StrConn)) {
                using (SQLiteCommand sqliteCmd = new SQLiteCommand(strSQL, sqliteConn)) {
                    try {
                        sqliteConn.Open();
                        object obj = sqliteCmd.ExecuteScalar();
                        if ((Object.Equals(obj, null)) || (Object.Equals(obj, System.DBNull.Value))) {
                            return null;
                        } else {
                            return obj;
                        }
                    } catch (Exception ex) {
                        _log.TraceError("Error SQL: " + strSQL);
                        _log.TraceError(ex.Message);
                        throw;
                    } finally {
                        if (sqliteConn.State != ConnectionState.Closed) {
                            sqliteConn.Close();
                        }
                    }
                }
            }
        }

        public void InsertRecords(DataTable dt, string primaryKey = "ID") {
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
                string strDisplaySQL = string.Empty;
                try {
                    sqliteConn.Open();
                    using (SQLiteTransaction sqliteTrans = sqliteConn.BeginTransaction()) {
                        using (SQLiteCommand sqliteCmd = new SQLiteCommand(strSQL, sqliteConn)) {
                            sqliteCmd.CommandText = strSQL;
                            for (int i = 0; i < dt.Rows.Count; i++) {
                                strDisplaySQL = strSQL;
                                for (int j = 0; j < schema.Rows.Count; j++) {
                                    string strColumnsName = schema.Rows[j]["COLUMN_NAME"].ToString();
                                    if (primaryKey != strColumnsName) {
                                        DbType dbType = SqlDbTypeToDbType(schema.Rows[j]["DATA_TYPE"].ToString());
                                        int length = Convert.ToInt32(schema.Rows[j]["CHARACTER_MAXIMUM_LENGTH"]);
                                        string strParaName = "@" + strColumnsName;
                                        sqliteCmd.Parameters.Add(strParaName, dbType, length);
                                        sqliteCmd.Parameters[strParaName].Value = dt.Rows[i][strColumnsName].ToString();
                                        strDisplaySQL = strDisplaySQL.Replace(strParaName, sqliteCmd.Parameters[strParaName].Value.ToString());
                                    }
                                }
                                sqliteCmd.ExecuteNonQuery();
                            }
                        }
                        sqliteTrans.Commit();
                    }
                } catch (Exception ex) {
                    _log.TraceError("Error SQL: " + strDisplaySQL);
                    _log.TraceError(ex.Message);
                    throw;
                } finally {
                    if (sqliteConn.State != ConnectionState.Closed) {
                        sqliteConn.Close();
                    }
                }
            }
        }

        public void UpdateRecords(DataTable dt, Dictionary<string, string> whereDic) {
            string strSQL = "update " + dt.TableName + " set ";
            for (int j = 0; j < dt.Columns.Count; j++) {
                strSQL += dt.Columns[j].ColumnName + " = @" + dt.Columns[j].ColumnName + ", ";
            }
            strSQL = strSQL.Substring(0, strSQL.Length - 2);
            strSQL += " where ";
            foreach (string key in whereDic.Keys) {
                strSQL += key + " = '" + whereDic[key] + "' and ";
            }
            strSQL += "1 = 1";

            DataTable schema = GetTableColumnsSchema(dt.TableName);
            using (SQLiteConnection sqliteConn = new SQLiteConnection(StrConn)) {
                string strDisplaySQL = string.Empty;
                try {
                    sqliteConn.Open();
                    using (SQLiteTransaction sqliteTrans = sqliteConn.BeginTransaction()) {
                        using (SQLiteCommand sqliteCmd = new SQLiteCommand(strSQL, sqliteConn)) {
                            sqliteCmd.CommandText = strSQL;
                            for (int i = 0; i < dt.Rows.Count; i++) {
                                strDisplaySQL = strSQL;
                                for (int j = 0; j < dt.Columns.Count; j++) {
                                    for (int k = 0; k < schema.Rows.Count; k++) {
                                        if (dt.Columns[j].ColumnName == schema.Rows[k]["COLUMN_NAME"].ToString()) {
                                            DbType dbType = SqlDbTypeToDbType(schema.Rows[k]["DATA_TYPE"].ToString());
                                            int length = Convert.ToInt32(schema.Rows[k]["CHARACTER_MAXIMUM_LENGTH"]);
                                            string strParaName = "@" + dt.Columns[j].ColumnName;
                                            sqliteCmd.Parameters.Add(strParaName, dbType, length);
                                            sqliteCmd.Parameters[strParaName].Value = dt.Rows[i][j].ToString();
                                            strDisplaySQL = strDisplaySQL.Replace(strParaName, sqliteCmd.Parameters[strParaName].Value.ToString());
                                        }
                                    }
                                }
                                sqliteCmd.ExecuteNonQuery();
                            }
                        }
                        sqliteTrans.Commit();
                    }
                } catch (Exception ex) {
                    _log.TraceError("Error SQL: " + strDisplaySQL);
                    _log.TraceError(ex.Message);
                    throw;
                } finally {
                    if (sqliteConn.State != ConnectionState.Closed) {
                        sqliteConn.Close();
                    }
                }
            }
        }

        public int DeleteRecords(string strTable, string whereCol, List<string> whereVals) {
            string strSQL = "delete from " + strTable + " where " + whereCol + " = @value";

            int count = 0;
            DataTable schema = GetTableColumnsSchema(strTable);
            using (SQLiteConnection sqliteConn = new SQLiteConnection(StrConn)) {
                string strDisplaySQL = string.Empty;
                try {
                    sqliteConn.Open();
                    using (SQLiteTransaction sqliteTrans = sqliteConn.BeginTransaction()) {
                        using (SQLiteCommand sqliteCmd = new SQLiteCommand(strSQL, sqliteConn)) {
                            sqliteCmd.CommandText = strSQL;
                            for (int i = 0; i < whereVals.Count; i++) {
                                strDisplaySQL = strSQL;
                                int index = 0;
                                for (int k = 0; k < schema.Rows.Count; k++) {
                                    if (whereCol == schema.Rows[k]["COLUMN_NAME"].ToString()) {
                                        index = k;
                                    }
                                    DbType dbType = SqlDbTypeToDbType(schema.Rows[index]["DATA_TYPE"].ToString());
                                    int length = Convert.ToInt32(schema.Rows[index]["CHARACTER_MAXIMUM_LENGTH"]);
                                    sqliteCmd.Parameters.Add("@value", dbType, length);
                                    sqliteCmd.Parameters["@value"].Value = whereVals[i].ToString();
                                    strDisplaySQL = strDisplaySQL.Replace("@value", sqliteCmd.Parameters["@value"].Value.ToString());
                                }
                                count += sqliteCmd.ExecuteNonQuery();
                            }
                        }
                        sqliteTrans.Commit();
                    }
                } catch (Exception ex) {
                    _log.TraceError("Error SQL: " + strDisplaySQL);
                    _log.TraceError(ex.Message);
                    throw;
                } finally {
                    if (sqliteConn.State != ConnectionState.Closed) {
                        sqliteConn.Close();
                    }
                }
            }
            return count;
        }

        public int DeleteRecord(string strTable, Dictionary<string, string> whereDic) {
            string strSQL = "delete from " + strTable + " where ";
            if (whereDic != null) {
                foreach (string key in whereDic.Keys) {
                    strSQL += key + " = '" + whereDic[key] + "' and ";
                }
            }
            strSQL += "1 = 1";
            return ExecuteNonQuery(strSQL);
        }

        public void GetRecords(DataTable dt, Dictionary<string, string> whereDic) {
            string strSQL = "select ";
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
            if (whereDic != null) {
                foreach (string key in whereDic.Keys) {
                    strSQL += key + " = '" + whereDic[key] + "' and ";
                }
            }
            strSQL += "1 = 1";
            Query(strSQL, dt);
        }

        public enum FilterTime : int {
            NoFilter = 0,
            Day = 1,
            Week = 2,
            Month = 3
        }

        public void GetRecordsFilterTime(DataTable dt, Dictionary<string, string> whereDic, FilterTime time, int pageNum, int pageSize) {
            string strSQL = "select ";
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
            strSQL += "WriteTime > '" + strTimeStart + "' and WriteTime < '" + strTimeEnd + "' order by ID ";
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
                    { "VIN", dt.Rows[i][0].ToString() },
                    { "ECU_ID", dt.Rows[i][1].ToString() }
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
