using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;
using SH_OBD_DLL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace LibBase {
    public abstract class ModelBase {
        protected Logger _log;
        protected ModelParameter _param;
        protected string _strConn;
        protected string _prefixParam;

        /// <summary>
        /// 读取数据库配置，由该配置生成连接字符串和参数化查询前缀
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public void InitDataBase(ModelParameter param, Logger log) {
            _log = log;
            _param = param;
            switch (_param.DataBaseType) {
            case DataBaseType.SQLServer:
                _strConn = "user id=" + _param.UserName + ";";
                _strConn += "password=" + _param.PassWord + ";";
                _strConn += "database=" + _param.DBorService + ";";
                _strConn += "data source=" + _param.Host + "," + _param.Port;
                _prefixParam = "@";
                break;
            case DataBaseType.Oracle:
                _strConn = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=";
                _strConn += _param.Host + ")(PORT=";
                _strConn += _param.Port + "))(CONNECT_DATA=(SERVICE_NAME=";
                _strConn += _param.DBorService + ")));";
                _strConn += "Persist Security Info=True;";
                _strConn += "User ID=" + _param.UserName + ";";
                _strConn += "Password=" + _param.PassWord + ";";
                _prefixParam = ":";
                break;
            case DataBaseType.MySQL:
                _strConn = "server=" + _param.Host + ";" + "port=" + _param.Port + ";";
                _strConn += "uid=" + _param.UserName + ";";
                _strConn += "pwd=" + _param.PassWord + ";";
                _strConn += "database=" + _param.DBorService + ";";
                _strConn += "charset=utf8mb4;";
                _prefixParam = "@";
                break;
            case DataBaseType.SQLite:
                _strConn = "data source=" + _param.DBorService;
                _prefixParam = "@";
                break;
            }
        }

        protected DataTable GetTableColumnsSchema(string strTable) {
            DataTable schema = new DataTable();
            using (DbConnection_IT dbConn = new DbConnection_IT(_param.DataBaseType, _strConn)) {
                try {
                    dbConn.DbConnection.Open();
                    string[] restrictions = new string[] { null, null, strTable };
                    switch (_param.DataBaseType) {
                    case DataBaseType.SQLServer:
                        // SQLServer restrictions(database, owner, tablename)
                        schema = dbConn.DbConnection.GetSchema("Columns", restrictions);
                        schema.DefaultView.Sort = "ORDINAL_POSITION";
                        break;
                    case DataBaseType.Oracle:
                        // Oracle restrictions (owner, tablename, columnname)
                        restrictions[1] = strTable;
                        restrictions[2] = null;
                        schema = dbConn.DbConnection.GetSchema("Columns", restrictions);
                        schema.DefaultView.Sort = "ID";
                        break;
                    case DataBaseType.MySQL:
                        // MySQL restrictions(database, owner, tablename)
                        schema = dbConn.DbConnection.GetSchema("Columns", restrictions);
                        schema.DefaultView.Sort = "ORDINAL_POSITION";
                        break;
                    case DataBaseType.SQLite:
                        // SQLite restrictions(database, owner, tablename)
                        schema = dbConn.DbConnection.GetSchema("Columns", restrictions);
                        schema.DefaultView.Sort = "ORDINAL_POSITION";
                        break;
                    default:
                        break;
                    }
                    schema = schema.DefaultView.ToTable();
                } catch (Exception ex) {
                    _log.TraceError("Get columns schema from table: " + strTable + " error");
                    _log.TraceError(ex.Message);
                    throw;
                } finally {
                    if (dbConn.DbConnection.State != ConnectionState.Closed) {
                        dbConn.DbConnection.Close();
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

        public void GetEmptyTable(DataTable dt) {
            string strSQL = "select * from " + dt.TableName;
            switch (_param.DataBaseType) {
            case DataBaseType.SQLServer:
                strSQL += " top 1";
                break;
            case DataBaseType.Oracle:
                strSQL += " where rownum <= 1";
                break;
            case DataBaseType.MySQL:
                strSQL += " limit 1";
                break;
            case DataBaseType.SQLite:
                strSQL += " limit 1";
                break;
            default:
                break;
            }
            using (DbConnection_IT dbConn = new DbConnection_IT(_param.DataBaseType, _strConn)) {
                try {
                    dbConn.DbConnection.Open();
                    using (DbTransaction dbTrans = dbConn.DbConnection.BeginTransaction()) {
                        using (DbDataAdapter_IT adapter = new DbDataAdapter_IT(_param.DataBaseType, strSQL, dbConn)) {
                            adapter.DbDataAdapter.Fill(dt);
                        }
                        dbTrans.Commit();
                    }
                    dt.Clear();
                } catch (Exception ex) {
                    _log.TraceError("Error SQL: " + strSQL);
                    _log.TraceError(ex.Message);
                    throw;
                } finally {
                    if (dbConn.DbConnection.State != ConnectionState.Closed) {
                        dbConn.DbConnection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// 执行update insert delete语句，返回受影响的行数，自动commit
        /// </summary>
        /// <param name="strSQL"></param>
        /// <returns></returns>
        protected int ExecuteNonQuery(string strSQL) {
            using (DbConnection_IT dbConn = new DbConnection_IT(_param.DataBaseType, _strConn)) {
                int val = 0;
                try {
                    dbConn.DbConnection.Open();
                    DbCommand_IT dbCmd = new DbCommand_IT(_param.DataBaseType, strSQL, dbConn);
                    val = dbCmd.DbCommand.ExecuteNonQuery();
                    dbCmd.DbCommand.Parameters.Clear();
                } catch (Exception ex) {
                    _log.TraceError("Error SQL: " + strSQL);
                    _log.TraceError(ex.Message);
                    throw;
                } finally {
                    if (dbConn.DbConnection.State != ConnectionState.Closed) {
                        dbConn.DbConnection.Close();
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
        protected void Query(string strSQL, DataTable dt) {
            using (DbConnection_IT dbConn = new DbConnection_IT(_param.DataBaseType, _strConn)) {
                try {
                    dbConn.DbConnection.Open();
                    DbDataAdapter_IT adapter = new DbDataAdapter_IT(_param.DataBaseType, strSQL, dbConn);
                    adapter.DbDataAdapter.Fill(dt);
                } catch (Exception ex) {
                    _log.TraceError("Error SQL: " + strSQL);
                    _log.TraceError(ex.Message);
                    throw;
                } finally {
                    if (dbConn.DbConnection.State != ConnectionState.Closed) {
                        dbConn.DbConnection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// 执行select语句，返回第一个数据对象，自动commit
        /// </summary>
        /// <param name="strSQL"></param>
        /// <returns></returns>
        protected object QueryOne(string strSQL) {
            using (DbConnection_IT dbConn = new DbConnection_IT(_param.DataBaseType, _strConn)) {
                using (DbCommand_IT dbCmd = new DbCommand_IT(_param.DataBaseType, strSQL, dbConn)) {
                    try {
                        dbConn.DbConnection.Open();
                        object obj = dbCmd.DbCommand.ExecuteScalar();
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
                        if (dbConn.DbConnection.State != ConnectionState.Closed) {
                            dbConn.DbConnection.Close();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 新记录插入数据库表中，返回受影响的行数。
        /// 参数dt的名称表示欲插入的表名，dt的columns和rows表示欲插入的新记录。
        /// 参数bProcessPK表示是否对主键特殊处理，例如Oracle使用序列器生成主键值。
        /// 参数primaryKey表示该表的主键名称，插入新记录时不处理主键（因为主键一般是自动生成的）。
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="bProcessPK"></param>
        /// <param name="primaryKey"></param>
        public int InsertRecords(DataTable dt, bool bProcessPK = false, string primaryKey = "ID") {
            string columns = " (";
            string row = " values (";
            for (int i = 0; i < dt.Columns.Count; i++) {
                if (primaryKey != dt.Columns[i].ColumnName || bProcessPK) {
                    columns += dt.Columns[i].ColumnName + ",";
                    row += _prefixParam + dt.Columns[i].ColumnName + ",";
                }
            }
            columns = columns.Substring(0, columns.Length - 1) + ")";
            row = row.Substring(0, row.Length - 1) + ")";
            string strSQL = "insert into " + dt.TableName + columns + row;

            int count = 0;
            DataTable schema = GetTableColumnsSchema(dt.TableName);
            using (DbConnection_IT dbConn = new DbConnection_IT(_param.DataBaseType, _strConn)) {
                string strDisplaySQL = string.Empty;
                try {
                    dbConn.DbConnection.Open();
                    using (DbTransaction dbTrans = dbConn.DbConnection.BeginTransaction()) {
                        for (int i = 0; i < dt.Rows.Count; i++) {
                            using (DbCommand_IT dbCmd = new DbCommand_IT(_param.DataBaseType, strSQL, dbConn)) {
                                strDisplaySQL = strSQL;
                                for (int j = 0; j < dt.Columns.Count; j++) {
                                    for (int k = 0; k < schema.Rows.Count; k++) {
                                        if (dt.Columns[j].ColumnName == schema.Rows[k]["COLUMN_NAME"].ToString()) {
                                            int length = 0;
                                            string strParaName = _prefixParam + dt.Columns[j].ColumnName;
                                            if (primaryKey != dt.Columns[j].ColumnName) {
                                                switch (_param.DataBaseType) {
                                                case DataBaseType.SQLServer:
                                                    length = Convert.ToInt32(schema.Rows[k]["CHARACTER_MAXIMUM_LENGTH"]);
                                                    SqlCommand sqlCmd = dbCmd.DbCommand as SqlCommand;
                                                    SqlDbType sqldbType = SqlTypeToSqlDbType(schema.Rows[k]["DATA_TYPE"].ToString());
                                                    sqlCmd.Parameters.Add(strParaName, sqldbType, length);
                                                    sqlCmd.Parameters[strParaName].Value = dt.Rows[i][j];
                                                    strDisplaySQL = strDisplaySQL.Replace(strParaName, "'" + sqlCmd.Parameters[strParaName].Value.ToString() + "'");
                                                    break;
                                                case DataBaseType.Oracle:
                                                    length = Convert.ToInt32(schema.Rows[k]["LENGTH"]);
                                                    OracleCommand oraCmd = dbCmd.DbCommand as OracleCommand;
                                                    OracleDbType oraDbType = SqlTypeToOracleDbType(schema.Rows[k]["DATATYPE"].ToString());
                                                    oraCmd.Parameters.Add(strParaName, oraDbType, length);
                                                    oraCmd.Parameters[strParaName].Value = dt.Rows[i][j];
                                                    strDisplaySQL = strDisplaySQL.Replace(strParaName, "'" + oraCmd.Parameters[strParaName].Value.ToString() + "'");
                                                    break;
                                                case DataBaseType.MySQL:
                                                    length = Convert.ToInt32(schema.Rows[k]["CHARACTER_MAXIMUM_LENGTH"]);
                                                    MySqlCommand mySqlCmd = dbCmd.DbCommand as MySqlCommand;
                                                    MySqlDbType mySqlDbType = SqlTypeToMySqlDbType(schema.Rows[k]["DATA_TYPE"].ToString());
                                                    mySqlCmd.Parameters.Add(strParaName, mySqlDbType, length);
                                                    mySqlCmd.Parameters[strParaName].Value = dt.Rows[i][j];
                                                    strDisplaySQL = strDisplaySQL.Replace(strParaName, "'" + mySqlCmd.Parameters[strParaName].Value.ToString() + "'");
                                                    break;
                                                case DataBaseType.SQLite:
                                                    length = Convert.ToInt32(schema.Rows[k]["CHARACTER_MAXIMUM_LENGTH"]);
                                                    SQLiteCommand sqliteCmd = dbCmd.DbCommand as SQLiteCommand;
                                                    DbType dbType = SqlTypeToDbType(schema.Rows[k]["DATA_TYPE"].ToString());
                                                    sqliteCmd.Parameters.Add(strParaName, dbType, length);
                                                    sqliteCmd.Parameters[strParaName].Value = dt.Rows[i][j];
                                                    strDisplaySQL = strDisplaySQL.Replace(strParaName, "'" + sqliteCmd.Parameters[strParaName].Value.ToString() + "'");
                                                    break;
                                                }
                                            } else {
                                                dbCmd.DbCommand.CommandText = dbCmd.DbCommand.CommandText.Replace(strParaName, dt.Rows[i][j].ToString());
                                                strDisplaySQL = strDisplaySQL.Replace(strParaName, dt.Rows[i][j].ToString());
                                            }
                                        }
                                    }
                                }
                                count += dbCmd.DbCommand.ExecuteNonQuery();
                            }
                        }
                        dbTrans.Commit();
                    }
                } catch (Exception ex) {
                    _log.TraceError("Error SQL: " + strDisplaySQL);
                    _log.TraceError(ex.Message);
                    throw;
                } finally {
                    if (dbConn.DbConnection.State != ConnectionState.Closed) {
                        dbConn.DbConnection.Close();
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// 更新数据库表中的旧记录，返回受影响的行数。
        /// 参数dt的名称表示欲更新的表名，dt的columns和rows表示欲更新的记录数据。
        /// 参数wherePair表示更新哪几条记录的where条件，key为列名，value为值列表，值列表的数量不大于dt记录数量，且一条dt记录对应一个where值列表。
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="whereCol"></param>
        /// <param name="whereVals"></param>
        public int UpdateRecords(DataTable dt, string whereCol, List<string> whereVals) {
            string strSQL = "update " + dt.TableName + " set ";
            for (int j = 0; j < dt.Columns.Count; j++) {
                strSQL += dt.Columns[j].ColumnName + " = " + _prefixParam + dt.Columns[j].ColumnName + ", ";
            }
            strSQL = strSQL.Substring(0, strSQL.Length - 2);
            string strSQLSet = strSQL;

            int count = 0;
            DataTable schema = GetTableColumnsSchema(dt.TableName);
            using (DbConnection_IT dbConn = new DbConnection_IT(_param.DataBaseType, _strConn)) {
                string strDisplaySQL = string.Empty;
                try {
                    dbConn.DbConnection.Open();
                    using (DbTransaction dbTrans = dbConn.DbConnection.BeginTransaction()) {
                        for (int i = 0; i < whereVals.Count; i++) {
                            strSQL = strSQLSet + " where " + whereCol + " = '" + whereVals[i] + "'";
                            using (DbCommand_IT dbCmd = new DbCommand_IT(_param.DataBaseType, strSQL, dbConn)) {
                                strDisplaySQL = dbCmd.DbCommand.CommandText;
                                for (int j = 0; j < dt.Columns.Count; j++) {
                                    for (int k = 0; k < schema.Rows.Count; k++) {
                                        if (dt.Columns[j].ColumnName == schema.Rows[k]["COLUMN_NAME"].ToString()) {
                                            int length = 0;
                                            string strParaName = _prefixParam + dt.Columns[j].ColumnName;
                                            switch (_param.DataBaseType) {
                                            case DataBaseType.SQLServer:
                                                length = Convert.ToInt32(schema.Rows[k]["CHARACTER_MAXIMUM_LENGTH"]);
                                                SqlCommand sqlCmd = dbCmd.DbCommand as SqlCommand;
                                                SqlDbType sqldbType = SqlTypeToSqlDbType(schema.Rows[k]["DATA_TYPE"].ToString());
                                                sqlCmd.Parameters.Add(strParaName, sqldbType, length);
                                                sqlCmd.Parameters[strParaName].Value = dt.Rows[i][j];
                                                strDisplaySQL = strDisplaySQL.Replace(strParaName, "'" + sqlCmd.Parameters[strParaName].Value.ToString() + "'");
                                                break;
                                            case DataBaseType.Oracle:
                                                length = Convert.ToInt32(schema.Rows[k]["LENGTH"]);
                                                OracleCommand oraCmd = dbCmd.DbCommand as OracleCommand;
                                                OracleDbType oraDbType = SqlTypeToOracleDbType(schema.Rows[k]["DATATYPE"].ToString());
                                                oraCmd.Parameters.Add(strParaName, oraDbType, length);
                                                oraCmd.Parameters[strParaName].Value = dt.Rows[i][j];
                                                strDisplaySQL = strDisplaySQL.Replace(strParaName, "'" + oraCmd.Parameters[strParaName].Value.ToString() + "'");
                                                break;
                                            case DataBaseType.MySQL:
                                                length = Convert.ToInt32(schema.Rows[k]["CHARACTER_MAXIMUM_LENGTH"]);
                                                MySqlCommand mySqlCmd = dbCmd.DbCommand as MySqlCommand;
                                                MySqlDbType mySqlDbType = SqlTypeToMySqlDbType(schema.Rows[k]["DATA_TYPE"].ToString());
                                                mySqlCmd.Parameters.Add(strParaName, mySqlDbType, length);
                                                mySqlCmd.Parameters[strParaName].Value = dt.Rows[i][j];
                                                strDisplaySQL = strDisplaySQL.Replace(strParaName, "'" + mySqlCmd.Parameters[strParaName].Value.ToString() + "'");
                                                break;
                                            case DataBaseType.SQLite:
                                                length = Convert.ToInt32(schema.Rows[k]["CHARACTER_MAXIMUM_LENGTH"]);
                                                SQLiteCommand sqliteCmd = dbCmd.DbCommand as SQLiteCommand;
                                                DbType dbType = SqlTypeToDbType(schema.Rows[k]["DATA_TYPE"].ToString());
                                                sqliteCmd.Parameters.Add(strParaName, dbType, length);
                                                sqliteCmd.Parameters[strParaName].Value = dt.Rows[i][j];
                                                strDisplaySQL = strDisplaySQL.Replace(strParaName, "'" + sqliteCmd.Parameters[strParaName].Value.ToString() + "'");
                                                break;
                                            }
                                        }
                                    }
                                }
                                count += dbCmd.DbCommand.ExecuteNonQuery();
                            }
                        }
                        dbTrans.Commit();
                    }
                } catch (Exception ex) {
                    _log.TraceError("Error SQL: " + strDisplaySQL);
                    _log.TraceError(ex.Message);
                    throw;
                } finally {
                    if (dbConn.DbConnection.State != ConnectionState.Closed) {
                        dbConn.DbConnection.Close();
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// 删除数据库表中的记录。
        /// 参数strTable表示欲删除的记录的表名。
        /// 参数whereCol表示欲删除记录的列名。
        /// 参数whereVals表示欲删除记录的字段值，可能有多个。
        /// 返回实际删除的记录数
        /// </summary>
        /// <param name="strTable"></param>
        /// <param name="whereCol"></param>
        /// <param name="whereVals"></param>
        /// <returns></returns>
        public int DeleteRecords(string strTable, string whereCol, List<string> whereVals) {
            string strSQL = "delete from " + strTable + " where " + whereCol + " = " + _prefixParam + "value";

            int count = 0;
            DataTable schema = GetTableColumnsSchema(strTable);
            using (DbConnection_IT dbConn = new DbConnection_IT(_param.DataBaseType, _strConn)) {
                string strDisplaySQL = string.Empty;
                try {
                    dbConn.DbConnection.Open();
                    using (DbTransaction dbTrans = dbConn.DbConnection.BeginTransaction()) {
                        using (DbCommand_IT dbCmd = new DbCommand_IT(_param.DataBaseType, strSQL, dbConn)) {
                            for (int i = 0; i < whereVals.Count; i++) {
                                strDisplaySQL = strSQL;
                                int index = 0;
                                for (int k = 0; k < schema.Rows.Count; k++) {
                                    if (whereCol == schema.Rows[k]["COLUMN_NAME"].ToString()) {
                                        index = k;
                                    }
                                    int length = 0;
                                    switch (_param.DataBaseType) {
                                    case DataBaseType.SQLServer:
                                        length = Convert.ToInt32(schema.Rows[index]["CHARACTER_MAXIMUM_LENGTH"]);
                                        SqlCommand sqlCmd = dbCmd.DbCommand as SqlCommand;
                                        SqlDbType sqlDbType = SqlTypeToSqlDbType(schema.Rows[index]["DATA_TYPE"].ToString());
                                        sqlCmd.Parameters.Add(_prefixParam + "value", sqlDbType, length);
                                        sqlCmd.Parameters[_prefixParam + "value"].Value = whereVals[i];
                                        strDisplaySQL = strDisplaySQL.Replace(_prefixParam + "value", "'" + sqlCmd.Parameters[_prefixParam + "value"].Value.ToString() + "'");
                                        break;
                                    case DataBaseType.Oracle:
                                        length = Convert.ToInt32(schema.Rows[index]["LENGTH"]);
                                        OracleCommand oracleCmd = dbCmd.DbCommand as OracleCommand;
                                        OracleDbType oraDbType = SqlTypeToOracleDbType(schema.Rows[index]["DATATYPE"].ToString());
                                        oracleCmd.Parameters.Add(_prefixParam + "value", oraDbType, length);
                                        oracleCmd.Parameters[_prefixParam + "value"].Value = whereVals[i];
                                        strDisplaySQL = strDisplaySQL.Replace(_prefixParam + "value", "'" + oracleCmd.Parameters[_prefixParam + "value"].Value.ToString() + "'");
                                        break;
                                    case DataBaseType.MySQL:
                                        length = Convert.ToInt32(schema.Rows[index]["CHARACTER_MAXIMUM_LENGTH"]);
                                        MySqlCommand mySqlCmd = dbCmd.DbCommand as MySqlCommand;
                                        MySqlDbType mySqlDbType = SqlTypeToMySqlDbType(schema.Rows[index]["DATA_TYPE"].ToString());
                                        mySqlCmd.Parameters.Add(_prefixParam + "value", mySqlDbType, length);
                                        mySqlCmd.Parameters[_prefixParam + "value"].Value = whereVals[i];
                                        strDisplaySQL = strDisplaySQL.Replace(_prefixParam + "value", "'" + mySqlCmd.Parameters[_prefixParam + "value"].Value.ToString() + "'");
                                        break;
                                    case DataBaseType.SQLite:
                                        length = Convert.ToInt32(schema.Rows[index]["CHARACTER_MAXIMUM_LENGTH"]);
                                        SQLiteCommand sqliteCmd = dbCmd.DbCommand as SQLiteCommand;
                                        DbType dbType = SqlTypeToDbType(schema.Rows[index]["DATA_TYPE"].ToString());
                                        sqliteCmd.Parameters.Add(_prefixParam + "value", dbType, length);
                                        sqliteCmd.Parameters[_prefixParam + "value"].Value = whereVals[i];
                                        strDisplaySQL = strDisplaySQL.Replace(_prefixParam + "value", "'" + sqliteCmd.Parameters[_prefixParam + "value"].Value.ToString() + "'");
                                        break;
                                    }
                                }
                                count += dbCmd.DbCommand.ExecuteNonQuery();
                            }
                        }
                        dbTrans.Commit();
                    }
                } catch (Exception ex) {
                    _log.TraceError("Error SQL: " + strDisplaySQL);
                    _log.TraceError(ex.Message);
                    throw;
                } finally {
                    if (dbConn.DbConnection.State != ConnectionState.Closed) {
                        dbConn.DbConnection.Close();
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// 删除数据库表中的记录，返回受影响的行数。
        /// 参数strTable表示欲删除的记录的表名。
        /// 参数whereDic表示欲删除哪几条记录的where条件，key为列名，value为值
        /// 返回实际删除的记录数
        /// </summary>
        /// <param name="strTable"></param>
        /// <param name="whereDic"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 读取数据库表中的数据
        /// 参数dt的名称表示欲读取的表名，dt的columns为欲读取的字段，dt的rows应为空
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="whereDic"></param>
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

        /// <summary>
        /// sql数据类型（如：varchar）转换为DbType类型
        /// </summary>
        /// <param name="sqlTypeString"></param>
        /// <returns></returns>
        public static DbType SqlTypeToDbType(string sqlTypeString) {
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
        public static Type SqlTypeToCsharpType(SqlDbType sqlType) {
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

        /// <summary>
        /// sql数据类型（如：varchar）转换为OracleDbType类型
        /// </summary>
        /// <param name="sqlTypeString"></param>
        /// <returns></returns>
        public static OracleDbType SqlTypeToOracleDbType(string sqlTypeString) {
            OracleDbType type;
            switch (sqlTypeString.ToLower()) {
            case "bfile":
                type = OracleDbType.BFile;
                break;
            case "blob":
                type = OracleDbType.Blob;
                break;
            case "char":
                type = OracleDbType.Char;
                break;
            case "clob":
                type = OracleDbType.Clob;
                break;
            case "date":
            case "datetime":
                type = OracleDbType.Date;
                break;
            case "decimal":
                type = OracleDbType.Decimal;
                break;
            case "double":
                type = OracleDbType.BinaryDouble;
                break;
            case "float":
                type = OracleDbType.BinaryFloat;
                break;
            case "int":
            case "integer":
                type = OracleDbType.Int32;
                break;
            case "long":
                type = OracleDbType.Long;
                break;
            case "nchar":
                type = OracleDbType.NChar;
                break;
            case "nclob":
                type = OracleDbType.NClob;
                break;
            case "number":
                type = OracleDbType.Decimal;
                break;
            case "nvarchar":
            case "nvarchar2":
                type = OracleDbType.NVarchar2;
                break;
            case "raw":
                type = OracleDbType.Raw;
                break;
            case "real":
                type = OracleDbType.Decimal;
                break;
            case "timestamp":
                type = OracleDbType.TimeStamp;
                break;
            case "varchar":
            case "varchar2":
                type = OracleDbType.Varchar2;
                break;
            case "xml":
                type = OracleDbType.XmlType;
                break;
            default:
                type = OracleDbType.Raw;
                break;
            }
            return type;
        }

        /// <summary>
        /// sql数据类型（如：varchar）转换为MySqlDbType类型
        /// </summary>
        /// <param name="sqlTypeString"></param>
        /// <returns></returns>
        public static MySqlDbType SqlTypeToMySqlDbType(string sqlTypeString) {
            MySqlDbType type;
            switch (sqlTypeString.ToLower()) {
            case "bigint":
                type = MySqlDbType.Int64;
                break;
            case "binary":
                type = MySqlDbType.Binary;
                break;
            case "bit":
                type = MySqlDbType.Bit;
                break;
            case "char":
                type = MySqlDbType.UByte;
                break;
            case "datetime":
                type = MySqlDbType.DateTime;
                break;
            case "decimal":
                type = MySqlDbType.Decimal;
                break;
            case "double":
            case "float":
                type = MySqlDbType.Double;
                break;
            case "image":
                type = MySqlDbType.Blob;
                break;
            case "int":
            case "integer":
                type = MySqlDbType.Int32;
                break;
            case "money":
                type = MySqlDbType.Decimal;
                break;
            case "nchar":
                type = MySqlDbType.VarChar;
                break;
            case "ntext":
                type = MySqlDbType.LongText;
                break;
            case "numeric":
                type = MySqlDbType.Decimal;
                break;
            case "nvarchar":
                type = MySqlDbType.VarChar;
                break;
            case "smalldatetime":
                type = MySqlDbType.DateTime;
                break;
            case "real":
                type = MySqlDbType.Float;
                break;
            case "smallint":
                type = MySqlDbType.Int16;
                break;
            case "smallmoney":
                type = MySqlDbType.Decimal;
                break;
            case "sql_variant":
                type = MySqlDbType.Binary;
                break;
            case "text":
                type = MySqlDbType.LongText;
                break;
            case "timestamp":
                type = MySqlDbType.Timestamp;
                break;
            case "tinyint":
                type = MySqlDbType.Byte;
                break;
            case "uniqueidentifier":
                type = MySqlDbType.Guid;
                break;
            case "varbinary":
                type = MySqlDbType.VarBinary;
                break;
            case "varchar":
                type = MySqlDbType.VarChar;
                break;
            case "xml":
                type = MySqlDbType.LongText;
                break;
            default:
                type = MySqlDbType.Binary;
                break;
            }
            return type;
        }
    }

    public enum DataBaseType {
        SQLServer,
        Oracle,
        MySQL,
        SQLite
    }

    public class ModelParameter {
        public DataBaseType DataBaseType { get; set; }
        public string UserName { get; set; }
        public string PassWord { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        public string DBorService { get; set; }
    }

    internal class DbConnection_IT : IDisposable {
        public DbConnection DbConnection { get; set; }

        public DbConnection_IT(DataBaseType type, string strConn) {
            DbConnection = GetDbConnection(type, strConn);
        }

        private DbConnection GetDbConnection(DataBaseType dataBaseType, string strConn) {
            switch (dataBaseType) {
            case DataBaseType.SQLServer:
                return new SqlConnection(strConn);
            case DataBaseType.Oracle:
                return new OracleConnection(strConn);
            case DataBaseType.MySQL:
                return new MySqlConnection(strConn);
            case DataBaseType.SQLite:
                return new SQLiteConnection(strConn);
            default:
                return null;
            }
        }

        public void Dispose() {
            if (DbConnection != null) {
                DbConnection.Dispose();
            }
        }
    }

    internal class DbCommand_IT : IDisposable {
        public DbCommand DbCommand { get; set; }

        public DbCommand_IT(DataBaseType dataBaseType, string strSQL, DbConnection_IT dbConn) {
            DbCommand = GetDbCommand(dataBaseType, strSQL, dbConn);
        }

        private DbCommand GetDbCommand(DataBaseType dataBaseType, string strSQL, DbConnection_IT dbConn) {
            switch (dataBaseType) {
            case DataBaseType.SQLServer:
                SqlConnection sqlConn = dbConn.DbConnection as SqlConnection;
                return new SqlCommand(strSQL, sqlConn);
            case DataBaseType.Oracle:
                OracleConnection oraConn = dbConn.DbConnection as OracleConnection;
                return new OracleCommand(strSQL, oraConn);
            case DataBaseType.MySQL:
                MySqlConnection mySqlConn = dbConn.DbConnection as MySqlConnection;
                return new MySqlCommand(strSQL, mySqlConn);
            case DataBaseType.SQLite:
                SQLiteConnection sqliteConn = dbConn.DbConnection as SQLiteConnection;
                return new SQLiteCommand(strSQL, sqliteConn);
            default:
                return null;
            }
        }

        public void Dispose() {
            if (DbCommand != null) {
                DbCommand.Dispose();
            }
        }
    }

    internal class DbDataAdapter_IT : IDisposable {
        public DbDataAdapter DbDataAdapter { get; set; }

        public DbDataAdapter_IT(DataBaseType dataBaseType, string strSQL, DbConnection_IT dbConn) {
            DbDataAdapter = GetDbAdapter(dataBaseType, strSQL, dbConn);
        }

        private DbDataAdapter GetDbAdapter(DataBaseType dataBaseType, string strSQL, DbConnection_IT dbConn) {
            switch (dataBaseType) {
            case DataBaseType.SQLServer:
                SqlConnection sqlConn = dbConn.DbConnection as SqlConnection;
                return new SqlDataAdapter(strSQL, sqlConn);
            case DataBaseType.Oracle:
                OracleConnection oraConn = dbConn.DbConnection as OracleConnection;
                return new OracleDataAdapter(strSQL, oraConn);
            case DataBaseType.MySQL:
                MySqlConnection mySqlConn = dbConn.DbConnection as MySqlConnection;
                return new MySqlDataAdapter(strSQL, mySqlConn);
            case DataBaseType.SQLite:
                SQLiteConnection sqliteConn = dbConn.DbConnection as SQLiteConnection;
                return new SQLiteDataAdapter(strSQL, sqliteConn);
            default:
                return null;
            }
        }

        public void Dispose() {
            if (DbDataAdapter != null) {
                DbDataAdapter.Dispose();
            }
        }
    }
}
