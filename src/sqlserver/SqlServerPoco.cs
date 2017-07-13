using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace dbfaker.sqlserver
{
    //https://msdn.microsoft.com/en-us/library/microsoft.web.management.databasemanager.column.isidentity%28v=vs.90%29.aspx?f=255&MSPPError=-2147217396

    public class SqlServerPoco:IPoco
    {
        private string ConnectionString;

        public SqlServerPoco(string connectionstring)
        {
            ConnectionString = connectionstring;
        }


        public IList<DbTable> GetDbTables()
        {
            IList<DbTable> dbtables = new List<DbTable>();

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var primarykeys = GetPrimaryKeys(connection);
                var indexkeys = GetIndexs(connection);
                var columns = GetColumns(connection);

                var table = connection.GetSchema("Tables");

                foreach (DataRow row in table.Rows)
                {
                    var dbtable = new DbTable {Name = row[2].ToString(), SechemaName = row[1].ToString()};

                    dbtable.Columns = columns.Where(item => item.TableName == dbtable.Name);
                    dbtable.PrimaryKeys = primarykeys.FirstOrDefault(item => item.TableName == dbtable.Name);
                    dbtable.IndexKeys = indexkeys.Where(item => item.TableName == dbtable.Name);
                    dbtables.Add(dbtable);
                }
            }

            return dbtables;
        }

        public static string SqlType2CsharpType(string sqlType)
        {
            switch (sqlType)
            {
                case "bigint":
                    return "long";
                case "binary":
                    return "byte[]";
                case "bit":
                    return "bool";
                case "char":
                    return "string";
                case "date":
                    return "DateTime";
                case "datetime":
                    return "DateTime";
                case "datetime2":
                    return "DateTime";
                case "datetimeoffset":
                    return "DateTimeOffset";
                case "decimal":
                    return "decimal";
                case "float":
                    return "double";
                case "image":
                    return "byte[]";
                case "int":
                    return "int";
                case "money":
                    return "decimal";
                case "nchar":
                    return "string";
                case "ntext":
                    return "string";
                case "numeric":
                    return "decimal";
                case "nvarchar":
                    return "string";
                case "real":
                    return "Single";
                case "rowversion":
                    return "byte[]";
                case "sql_variant":
                    return "object";
                case "text":
                    return "string";
                case "time":
                    return "TimeSpan";
                case "timestamp":
                    return "byte[]";
                case "tinyint":
                    return "byte";
                case "uniqueidentifier":
                    return "Guid";
                case "varbinary":
                    return "byte[]";
                case "varchar":
                    return "string";
                case "xml":
                    return "Xml";

                default:
                    return string.Empty;
            }
        }

        private IList<DbColumn> GetColumns(SqlConnection connection)
        {
            using (var command = new SqlCommand())
            {
                command.Connection  = connection;
                command.CommandText =
  @" SELECT 
    	t.name AS table_name, 
    	s.name as schema_name,
    	c.name AS column_name,
	    c.is_identity,
	    c.is_nullable,
	    c.max_length,
	    tp.name as data_type,
    	SC.COLUMN_DEFAULT AS default_value, 
    	dc.name AS default_constraint_name
    	
	FROM  
    	sys.all_columns c
    JOIN 
    	sys.tables t ON c.object_id = t.object_id
    JOIN 
    	sys.schemas s ON t.schema_id = s.schema_id
    LEFT JOIN
    	sys.types AS tp ON tp.system_type_id= c.system_type_id AND tp.name <> 'sysname'
    LEFT JOIN 
    	sys.default_constraints dc ON c.default_object_id = dc.object_id
    LEFT JOIN 
    	INFORMATION_SCHEMA.COLUMNS SC ON (SC.TABLE_NAME = t.name AND SC.COLUMN_NAME = c.name)
	ORDER BY schema_name,table_name";
                command.CommandType = CommandType.Text;

                IList<DbColumn> results = new List<DbColumn>();

                using (SqlDataReader reader = command.ExecuteReader())
                {

                    while (reader.Read())
                    {
                        results.Add(new DbColumn()
                        {
                            
                            SchemaName = Convert.ToString(reader["schema_name"]),
                            TableName =  Convert.ToString(reader["table_name"]),
                            ColumnName = Convert.ToString(reader["column_name"]),
                            DataType = SqlType2CsharpType(Convert.ToString(reader["data_type"])),
                            IsIdentity = Convert.ToBoolean(reader["is_identity"]),
                            IsNullable = Convert.ToBoolean(reader["is_nullable"]),
                            MaxLength = Convert.ToInt32(reader["max_length"]),
                            DefaultValue = Convert.ToString(reader["default_value"]),
                            
                        });
                    }
                }

                return results;
            }
        } 
        
        private IList<DbIndex> GetPrimaryKeys(SqlConnection connection)
        {
            using (var command = new SqlCommand())
            {
                command.Connection = connection;
                command.CommandText = @"select
                                IndexName = kc.name,
                                SchemaName = ss.name,
                                TableName = object_name(kc.parent_object_id),
                                IsDescending = ic.is_descending_key,
                                IsIdentity = c.is_identity,
                                ColumnName = c.name
                                
                            from sys.key_constraints kc
                            inner join sys.index_columns ic on kc.parent_object_id = ic.object_id and kc.unique_index_id = ic.index_id and kc.type = 'PK'
                            inner join sys.columns c on ic.object_id = c.object_id and ic.column_id = c.column_id
                            inner join sys.schemas ss on kc.schema_id = ss.schema_id
                            order by SchemaName, TableName";

                command.CommandType = CommandType.Text;

                IList<DbIndex> results = new List<DbIndex>();
                using (SqlDataReader reader = command.ExecuteReader())
                {

                    while (reader.Read())
                    {
                        results.Add(new DbIndex()
                        {
                            ColumnName = Convert.ToString(reader["ColumnName"]),
                            TableName = Convert.ToString(reader["TableName"]),
                            //IsIdentity = Convert.ToBoolean(reader["IsIdentity"]),
                            IndexName = Convert.ToString(reader["IndexName"]),
                            IsDescending = Convert.ToBoolean(reader["IsDescending"]),
                            IsUnique = true
                        });
                    }
                }

                return results;
            }
        }

        private IList<DbIndex> GetIndexs(SqlConnection connection)
        {
            using (var command = new SqlCommand())
            {
                command.Connection = connection;
                command.CommandText = @"SELECT 
     TableName = t.name,
     IndexName = ind.name,
     IndexId = ind.index_id,
     ColumnId = ic.index_column_id,
     ColumnName = c.name,
     IsDescending = c.is_identity,
     IsUnique = ind.is_unique,
     IsDescending = ic.is_descending_key,
     IsIncludedColumn = ic.is_included_column
FROM 
     sys.indexes ind 
INNER JOIN 
     sys.index_columns ic ON  ind.object_id = ic.object_id and ind.index_id = ic.index_id 
INNER JOIN 
     sys.columns c ON ic.object_id = c.object_id and ic.column_id = c.column_id 
INNER JOIN 
     sys.tables t ON ind.object_id = t.object_id 
WHERE 
     ind.is_primary_key = 0
     AND ind.is_unique_constraint = 0 
     AND t.is_ms_shipped = 0 ";

                command.CommandType = CommandType.Text;

                IList<DbIndex> results = new List<DbIndex>();
                using (SqlDataReader reader = command.ExecuteReader())
                {

                    while (reader.Read())
                    {
                        results.Add(new DbIndex()
                        {
                            ColumnName = Convert.ToString(reader["ColumnName"]),
                            TableName = Convert.ToString(reader["TableName"]),
                            //IsIdentity = false,
                            IndexName = Convert.ToString(reader["IndexName"]),
                            IsDescending = Convert.ToBoolean(reader["IsDescending"]),
                            IsUnique = Convert.ToBoolean(reader["IsUnique"]),
                            IsIncludedColumn = Convert.ToBoolean(reader["IsIncludedColumn"])
                        });
                    }
                }

                return results;
            }
        }


    }
}
