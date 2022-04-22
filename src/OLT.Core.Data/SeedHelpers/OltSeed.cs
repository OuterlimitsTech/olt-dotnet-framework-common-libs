using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Reflection;

namespace OLT.Core
{
    public abstract class OltSeed<TContext> : IOltSeedDataHelper<TContext>
        where TContext : DbContext

    {
        protected OltSeed(TContext context)
        {
            Context = context;
        }

        public TContext Context { get; }
        public abstract void RunSeed();
        protected virtual DateTimeOffset DefaultCreateDate { get; } = new DateTimeOffset(DateTime.Today.Year, 1, 1, 0, 0, 0, 0, DateTimeOffset.Now.Offset);
        protected virtual string DefaultUsername { get; } = "SystemLoad";

        protected short GetEnumCodeSortOrder<TEnum>(TEnum item)
        {
            var type = item.GetType();
            var attribute = type.GetField(item.ToString()).GetCustomAttributes(typeof(CodeAttribute), false).Cast<CodeAttribute>().FirstOrDefault();
            return attribute?.DefaultSort ?? 9999;
        }

        protected string GetEnumDescription<TEnum>(TEnum item)
        {
            var type = item.GetType();
            var attribute = type.GetField(item.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>().FirstOrDefault();
            return attribute?.Description;
        }

        protected string GetEnumCode<TEnum>(TEnum item)
        {
            var type = item.GetType();
            var attribute = type.GetField(item.ToString()).GetCustomAttributes(typeof(CodeAttribute), false).Cast<CodeAttribute>().FirstOrDefault();
            return attribute?.Code;
        }

        protected Guid? GetUniqueId<TEnum>(TEnum item)
        {
            var type = item.GetType();
            var attribute = type.GetField(item.ToString()).GetCustomAttributes(typeof(UniqueIdAttribute), false).Cast<UniqueIdAttribute>().FirstOrDefault();
            return attribute?.UniqueId;
        }

        protected T GetAttributeInstance<T, TE>(TE item)
            where TE : struct
            where T : Attribute
        {
            var type = item.GetType();
            var attribute = type.GetField(item.ToString()).GetCustomAttributes(typeof(T), false).Cast<T>().FirstOrDefault();
            return attribute;
        }


        protected void SeedFromEnum<TData, TEnum>(Action<TData, TEnum> setProperties = null, Action<TData> saveAction = null)
                where TData : class, IOltEntityCodeValueEnum, new()
                where TEnum : struct
        {
            var etype = typeof(TEnum);

            if (!etype.IsEnum)
                throw new Exception($"Type '{etype.AssemblyQualifiedName}' must be enum");

            var ntype = Enum.GetUnderlyingType(etype);

            if (ntype == typeof(long) || ntype == typeof(ulong) || ntype == typeof(uint))
                throw new Exception();

            foreach (TEnum evalue in Enum.GetValues(etype))
            {
                var isNew = false;
                var id = (int)Convert.ChangeType(evalue, typeof(int));
                var item = Context.Set<TData>().FirstOrDefault(p => p.Id == id);

                if (item == null)
                {
                    isNew = true;
                    //item = Activator.CreateInstance<TData>();
                    item = new TData
                    {
                        Id = id
                    };

                    if (item is IOltEntityAudit auditEntity)
                    {
                        auditEntity.CreateUser = DefaultUsername;
                        auditEntity.CreateDate = DefaultCreateDate;
                        auditEntity.ModifyUser = DefaultUsername;
                        auditEntity.ModifyDate = DefaultCreateDate;
                    }


                    if (item.Id <= 0)
                    {
                        throw new Exception("Enum underlying value must be positive");
                    }
                }

                item.Code = GetEnumCode(evalue) ?? Enum.GetName(etype, evalue);
                item.Name = GetEnumDescription(evalue) ?? Enum.GetName(etype, evalue);
                item.SortOrder = GetEnumCodeSortOrder(evalue);


                if (item is IOltEntityUniqueId uidTableEntity)
                {
                    var uid = GetUniqueId(evalue);
                    if (uid.HasValue)
                    {
                        uidTableEntity.UniqueId = uid.Value;
                    }
                    else if (uidTableEntity.UniqueId == Guid.Empty)
                    {
                        uidTableEntity.UniqueId = Guid.NewGuid();
                    }
                }

                setProperties?.Invoke(item, evalue);

                if (saveAction != null)
                {
                    saveAction.Invoke(item);
                    continue;
                }

                if (isNew)
                {
                    Context.Set<TData>().Add(item);
                }
                Context.SaveChanges();

            }
        }

        ////private void SaveInsertIdentity<TData>(TData entity)
        ////    where TData: class, IOltEntityId
        ////{
        ////    //To enable debugging, uncomment these lines
        ////    //if (System.Diagnostics.Debugger.IsAttached == false)
        ////    //    System.Diagnostics.Debugger.Launch();


        ////    if (Context.Set<TData>().Any(p => p.Id == entity.Id))
        ////    {
        ////        Context.SaveChanges();
        ////        return;
        ////    }

        ////    var tableName = Context.GetTableName<TData>();

        ////    var insertSql = BuildSqlInsert(entity, (data, columnNames) => { columnNames.Add("Discriminator", $"'{entity.GetDiscriminator()}'"); }, tableName);

        ////    using (var db = new SqlConnection(this.Context.Database.Connection.ConnectionString))
        ////    {
        ////        db.Open();

        ////        using (var transaction = db.BeginTransaction())
        ////        {
        ////            var sql = new StringBuilder();
        ////            sql.Append($"SET IDENTITY_INSERT {tableName} ON;");
        ////            sql.Append($"{insertSql};");
        ////            sql.Append($"SET IDENTITY_INSERT {tableName} OFF;");
        ////            using (var cmd = new SqlCommand(sql.ToString(), db, transaction))
        ////            {
        ////                cmd.ExecuteNonQuery();
        ////            }
        ////            transaction.Commit();
        ////        }

        ////    }
  
        ////}


        protected static string GetColumnName(PropertyInfo item)
        {
            var attribute = item.GetCustomAttributes(typeof(ColumnAttribute), false).Cast<ColumnAttribute>().FirstOrDefault();
            return attribute?.Name;
        }

        protected string BuildSqlInsert<TData>(TData entity, Action<TData, Dictionary<string, string>> addColumns = null, string tableName = null)
            where TData : class, IOltEntity
        {
            var names = typeof(TData).GetProperties()
                    .Select(property => new { property, property.Name, property.CustomAttributes, property.Attributes, property.PropertyType })
                    .ToList();


            var columnNames = new Dictionary<string, string>();

            names.ForEach(prop =>
            {

                if (prop.PropertyType.IsEnum)
                {
                    return;
                }

                var colName = GetColumnName(prop.property) ?? prop.Name;

                if (colName == null) return;

                var value = typeof(TData).GetProperty(prop.Name).GetValue(entity, null);



                if (!(value is string))
                {
                    if (typeof(IEnumerable).IsAssignableFrom(prop.property.PropertyType))
                    {
                        return;
                    }
                    if (typeof(IOltEntityCodeValueEnum).IsAssignableFrom(prop.property.PropertyType))
                    {
                        return;
                    }
                    if (typeof(IOltEntity).IsAssignableFrom(prop.property.PropertyType))
                    {
                        return;
                    }
                }

                var val = string.Empty;
                if (value == null)
                {
                    val = "Null";
                }
                else if (value is bool boolValue)
                {
                    val = boolValue ? "1" : "0";
                }
                else if (value is DateTime)
                {
                    val = $"'{value}'";
                }
                else if (value is DateTimeOffset)
                {
                    val = $"'{value}'";
                }
                else if (value is Guid)
                {
                    val = $"'{value}'";
                }
                else if (value is string)
                {
                    val = $"'{value}'";
                }
                else
                {
                    val = value.ToString();
                }
                columnNames.Add(colName, val);

            });

            addColumns?.Invoke(entity, columnNames);

            tableName = tableName ?? Context.GetTableName<TData>();

            return $"INSERT {tableName}({columnNames.Keys.Join(",")}) Values ({columnNames.Values.Join(",")})";
        }


    }
}
