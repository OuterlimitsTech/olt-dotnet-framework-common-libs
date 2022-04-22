using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OLT.Core
{
    public static class ContextExtensions
    {
        /// <summary>
        /// Executes <see cref="string.Join(string,string[])"/> on the current <see cref="IEnumerable{T}"/> of strings.
        /// </summary>
        /// <param name="list">The current <see cref="IEnumerable{T}"/> of strings.</param>
        /// <param name="separator">A <see cref="string"/> containing the value that will be placed between each <see cref="string"/> in the collection.</param>
        /// <returns>A <see cref="string"/> containing the joined strings.</returns>
        private static string Join(this IEnumerable<string> list, string separator)
        {
            return String.Join(separator, new List<string>(list).ToArray());
        }


        public static string[] GetKeyNames<TEntity>(this DbContext context)
            where TEntity : class
        {
            return context.GetKeyNames(typeof(TEntity));
        }

        public static string[] GetKeyNames(this DbContext context, Type entityType)
        {
            var metadata = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;

            // Get the mapping between CLR types and metadata OSpace
            var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));

            // Get metadata for given CLR type
            var entityMetadata = metadata
                .GetItems<EntityType>(DataSpace.OSpace)
                .Single(e => objectItemCollection.GetClrType((StructuralType)e) == entityType);




            var keys = new List<string>();
            entityMetadata.KeyProperties.ToList().ForEach(property =>
            {
                var colName = GetColumnName(entityType, property.Name, context);
                keys.Add(colName);
            });

            return keys.ToArray();
        }

        public static string GetTableName<T>(this DbContext context) where T : class
        {
            ObjectContext objectContext = ((IObjectContextAdapter)context).ObjectContext;

            return objectContext.GetTableName<T>();
        }

        public static string GetTableName<T>(this ObjectContext context) where T : class
        {
            string sql = context.CreateObjectSet<T>().ToTraceString();
            Regex regex = new Regex("FROM (?<table>.*) AS");
            Match match = regex.Match(sql);

            return match.Groups["table"].Value;
        }

        public static string GetColumnName(Type type, string propertyName, DbContext context)
        {
            var metadata = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;

            // Get the part of the model that contains info about the actual CLR types
            var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));

            // Get the entity type from the model that maps to the CLR type
            var entityType = metadata.GetItems<EntityType>(DataSpace.OSpace)
                .Single(e => objectItemCollection.GetClrType(e) == type);

            // Get the entity set that uses this entity type
            var entitySet = metadata.GetItems<EntityContainer>(DataSpace.CSpace)
                .Single()
                .EntitySets
                .Single(s => s.ElementType.Name == entityType.Name);

            // Find the mapping between conceptual and storage model for this entity set
            var mapping = metadata.GetItems<EntityContainerMapping>(DataSpace.CSSpace)
                .Single()
                .EntitySetMappings
                .Single(s => s.EntitySet == entitySet);

            // Find the storage entity set (table) that the entity is mapped
            ////var tableEntitySet = mapping
            ////    .EntityTypeMappings.Single()
            ////    .Fragments.Single()
            ////    .StoreEntitySet;

            // Return the table name from the storage entity set
            //var tableName = tableEntitySet.MetadataProperties["Table"].Value ?? tableEntitySet.Name;

            // Find the storage property (column) that the property is mapped
            var columnName = mapping.EntityTypeMappings.Single()
                .Fragments.Single()
                .PropertyMappings
                .OfType<ScalarPropertyMapping>()
                .Single(m => m.Property.Name == propertyName)
                .Column
                .Name;

            //return tableName + "." + columnName;
            return columnName;
        }

        public static DataTable ToDataTable<T>(this DbContext context, IEnumerable<T> entityList) where T : class
        {
            var properties = typeof(T).GetProperties();
            var table = new DataTable();
            var entityClass = new Dictionary<string, Type>();
            var colPropInfo = new List<PropertyInfo>();
            var propColName = new Dictionary<string, string>();

            var tableName = GetTableName<T>(context);
            var tableSchema = tableName.Split('.')[0].Replace("[", string.Empty).Replace("]", string.Empty);
            tableName = tableName.Split('.')[1].Replace("[", string.Empty).Replace("]", string.Empty);



            DataTable dtColumns = new DataTable();
            using (SqlConnection conn = new SqlConnection(context.Database.Connection.ConnectionString))
            {
                string sql = $"SELECT COLUMN_NAME, ORDINAL_POSITION FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '{tableSchema}' AND TABLE_NAME = '{tableName}' ORDER BY ORDINAL_POSITION";
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                SqlDataAdapter da = new SqlDataAdapter(sql, conn);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                da.Fill(dtColumns);
            }



            foreach (var property in properties)
            {
                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                if (type.IsEnum)
                {
                    Debug.WriteLine($"type.IsEnum = {type.Name}");
                }
                else if (type.IsPrimitive || type.IsValueType || type == typeof(string))
                {
                    var colName = GetColumnName(typeof(T), property.Name, context);
                    //table.Columns.Add(property.Name, type);
                    //table.Columns.Add(colName, type);
                    entityClass.Add(colName, type);
                    propColName.Add(colName, property.Name);
                    colPropInfo.Add(property);
                }
                else
                {
                    Debug.WriteLine(type.Name);
                }
            }

            for (int i = 0; i < dtColumns.Rows.Count; i++)
            {
                var colName = dtColumns.Rows[i]["COLUMN_NAME"].ToString();
                var type = entityClass[colName];
                table.Columns.Add(colName, type);
            }

            //if (System.Diagnostics.Debugger.IsAttached == false)
            //    System.Diagnostics.Debugger.Launch();
            foreach (var entity in entityList)
            {
                var dr = table.NewRow();

                ////var values = new List<object>();
                ////var propValues = colProps.Select(p => new { p.Name, Value = p.GetValue(entity, null) }).ToDictionary(t => t.Name, v => v.Value);
                colPropInfo.ForEach(prop =>
                {
                    var colName = GetColumnName(typeof(T), prop.Name, context);
                    var value = prop.GetValue(entity, null);
                    try
                    {


                        if (value == null)
                        {
                            dr[colName] = DBNull.Value;
                        }
                        else
                        {
                            dr[colName] = value;
                        }

                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                        throw new Exception($"col:{colName} has val: {value}");
                    }

                    //var value = propValues[prop.Name];
                    //values.Add(value);
                });

                table.Rows.Add(dr);
                ////table.Rows.Add(values);

                //table.Rows.Add(colProps.Select(p => p.GetValue(entity, null)).ToArray());
            }
            return table;
        }

        public static void SqlBulkCopy<T>(this DbContext context, IEnumerable<T> entityList) where T : class
        {

            var dataTable = context.ToDataTable(entityList);

            // Create a table and add results
            //var dataTable = MakeTable(results);

            var connectionString = context.Database.Connection.ConnectionString;

            // Open a connection to database.
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();


                // Create the SqlBulkCopy object. 
                // Note that the column positions in the source DataTable 
                // match the column positions in the destination table so 
                // there is no need to map columns. 
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = context.GetTableName<T>();

                    try
                    {
                        // Write from the source to the destination.
                        bulkCopy.WriteToServer(dataTable);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
        }

        private static void DropTempTable(SqlConnection connection, string tempTablename)
        {
            var cmdTempTable = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            cmdTempTable.CommandText = "DROP TABLE " + tempTablename;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
            cmdTempTable.ExecuteNonQuery();
        }

        private static void CreateTempTable(SqlConnection connection, string destinationTableName, string tempTablename)
        {

            var cmdTempTable = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            cmdTempTable.CommandText = "SELECT TOP 0 * \r\n" +
                                       "INTO " + tempTablename + "\r\n" +
                                       "FROM " + destinationTableName;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
            cmdTempTable.ExecuteNonQuery();

        }

        public static void SqlBulkUpdate<T>(
            this DbContext context,
            IEnumerable<T> entityList,
            string[] matchOn = null,
            bool drop = true,
            string[] doNotUpdate = null) where T : class
        {

            var dataTable = context.ToDataTable(entityList);

            // Create a table and add results
            //var dataTable = MakeTable(results);

            var connectionString = context.Database.Connection.ConnectionString;

            var destinationTableName = context.GetTableName<T>();
            var tempTableName = $"tmp_{destinationTableName}_{Guid.NewGuid():N}".Replace("[", string.Empty).Replace("]", string.Empty).Replace(".", "_");



            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                CreateTempTable(connection, destinationTableName, tempTableName);
            }

            var mergeSql = string.Empty;
            try
            {
                using (var bulkCopy = new SqlBulkCopy(connectionString, SqlBulkCopyOptions.Default))
                {
                    bulkCopy.DestinationTableName = tempTableName;

                    try
                    {
                        // Write from the source to the destination.
                        bulkCopy.WriteToServer(dataTable);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }

                var primaryKeys = context.GetKeyNames<T>().ToList();
                var updateColumns = dataTable.Columns.Cast<DataColumn>()
                    .Where(p => !primaryKeys.Contains(p.ColumnName))
                    .Where(p => !Enumerable.Contains((doNotUpdate ?? new string[] { }), p.ColumnName))
                    .Select(s => s.ColumnName)
                    .ToList();
                var insertColumns = dataTable.Columns.Cast<DataColumn>()
                    .Where(p => !primaryKeys.Contains(p.ColumnName))
                    .Select(s => s.ColumnName)
                    .ToList();
                mergeSql = BuildSqlMerge(true, destinationTableName, tempTableName, matchOn?.ToList() ?? primaryKeys, updateColumns, insertColumns);


                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var cmdTempTable = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                    cmdTempTable.CommandText = mergeSql;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                    cmdTempTable.ExecuteNonQuery();
                }

            }
            catch (Exception exception)
            {
                var errorMsg = $"{mergeSql}{Environment.NewLine}{exception}";
                throw new Exception(errorMsg);
            }
            finally
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    if (drop)
                    {
                        DropTempTable(connection, tempTableName);
                    }
                }

            }

        }


        private static string BuildSqlMerge(
            bool includeInsert,
            string destinationTableName,
            string tempTablename,
            List<string> matchingColumn,
            List<string> columnNamesToUpdate,
            List<string> columnNamesToInsert)
        {

            //Func<string, string> buildTargetSource = col =>
            //{

            //}

            var onColSql = matchingColumn.Select(col => $"Target.[{col}]=Source.[{col}]").Join(" AND ");
            var updateColSql = columnNamesToUpdate.Select(col => $"Target.[{col}]=Source.[{col}]").Join(",\r\n");

            //var updateSql = "";
            //for (var i = 0; i < columnNamesToUpdate.Length; i++)
            //{
            //    updateSql += String.Format("Target.[{0}]=Source.[{0}]", columnNamesToUpdate[i]);
            //    if (i < columnNamesToUpdate.Length - 1)
            //        updateSql += ",";
            //}

            //var sqlMatching = new StringBuilder();

            var mergeSql = $"MERGE INTO {destinationTableName} AS Target\r\n" +
                           $"USING {tempTablename} AS Source\r\n" +
                           "ON\r\n" +
                           //"Target." + matchingColumn + " = Source." + matchingColumn + "\r\n" +
                           $"{onColSql}\r\n" +
                           "WHEN MATCHED THEN\r\n" +
                           $"UPDATE SET {updateColSql}";

            if (includeInsert)
            {
                var insertColSql = columnNamesToInsert.Select(col => $"[{col}]").Join(",\r\n");
                var selectFromColSql = columnNamesToInsert.Select(col => $"Source.[{col}]").Join(",\r\n");

                mergeSql = $"{mergeSql}\r\n" +
                           "WHEN NOT MATCHED THEN\r\n" +
                           "INSERT (\r\n" +
                           $"{insertColSql})\r\n" +
                           "VALUES (\r\n" +
                           $"{selectFromColSql})";

            }

            //var cmdTempTable = connection.CreateCommand();
            //cmdTempTable.CommandText = mergeSql;
            //cmdTempTable.ExecuteNonQuery();
            return $"{mergeSql};";
        }
    }
}
