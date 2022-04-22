using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;

namespace OLT.Core
{
    public abstract class OltDbContext : DbContext, IOltDbContext
    {
        public enum DefaultStringTypes
        {
            NVarchar,
            Varchar
        }

        protected OltDbContext(string nameOrConnectionString = "name=DbConnection") : base(nameOrConnectionString)
        {
            Database.Log = Write;
        }

        protected OltDbContext(IOltLogService logService, IOltDbAuditUser dbAuditUser, string nameOrConnectionString = "name=DbConnection") : base(nameOrConnectionString)
        {
            this.LogService = logService;
            this.DbAuditUser = dbAuditUser;
        }

        //Public due to UoW patterns needing to set this!
        public virtual IOltLogService LogService { get; set; }
        public virtual IOltDbAuditUser DbAuditUser { get; set; }

        protected virtual void Write(string value)
        {
            LogService?.SqlTrace(value);
        }

        public abstract string DefaultSchema { get; }
        public abstract bool DisableManyToManyCascadeDeleteConvention { get; }
        public abstract bool DisableOneToManyCascadeDeleteConvention { get; }
        public virtual string DefaultAnonymousUser => "GUEST USER";
        public abstract DefaultStringTypes DefaultStringType { get; }

        public virtual string AuditUser
        {
            get
            {
                if (DbAuditUser != null)
                {
                    var userName = DbAuditUser.GetDbUsername();
                    if (!string.IsNullOrWhiteSpace(userName))
                    {
                        return userName;
                    }
                    
                }

                return DefaultAnonymousUser;
            }
        }

        public virtual void BulkCopy<TEntity>(IList<TEntity> entityList) where TEntity : class, IOltEntity
        {
            this.SqlBulkCopy(entityList);
        }

        protected override DbEntityValidationResult ValidateEntity(DbEntityEntry entityEntry, IDictionary<object, object> items)
        {
            if (entityEntry.Entity is IOltEntityAudit createModel)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    createModel.CreateUser = createModel.CreateUser ?? AuditUser;
                    createModel.CreateDate = createModel.CreateDate == DateTimeOffset.MinValue ? DateTimeOffset.UtcNow : createModel.CreateDate;

                }

                createModel.ModifyUser = AuditUser;
                createModel.ModifyDate = DateTimeOffset.UtcNow;
            }

            if (entityEntry.Entity is IOltEntityUniqueId uniqueModel && uniqueModel.UniqueId == Guid.Empty)
            {
                uniqueModel.UniqueId = Guid.NewGuid();
            }

            if (entityEntry.Entity is IOltEntitySortable sortOrder && !(sortOrder.SortOrder > 0))
            {
                sortOrder.SortOrder = 9999;
            }

            if (entityEntry.State == EntityState.Added)
            {
                (entityEntry.Entity as IOltInsertingRecord)?.InsertingRecord(this, entityEntry);
            }

            if (entityEntry.State == EntityState.Modified)
            {
                (entityEntry.Entity as IOltUpdatingRecord)?.UpdatingRecord(this, entityEntry);
            }

            if (entityEntry.State == EntityState.Deleted)
            {
                (entityEntry.Entity as IOltDeletingRecord)?.DeletingRecord(this, entityEntry);
            }


            return base.ValidateEntity(entityEntry, items);
        }


        public override int SaveChanges()
        {

            var entries = this.ChangeTracker.Entries().ToList();
            var changed = entries.Where(p => p.State == EntityState.Added || p.State == EntityState.Modified).ToList();

            if (LogService != null && LogService.IsTraceEnabled)
            {
                foreach (var entry in entries)
                {
                    var tableName = GetTableName(entry);
                    LogService?.Write(OltLogType.Trace, $"Context SaveChanges - {tableName} table {entry.State}");
                }
            }

            //Set any empty string to null
            foreach (var entry in changed)
            {
                var str = typeof(string).Name;
                var properties = from p in entry.Entity.GetType().GetProperties()
                    where p.PropertyType.Name == str
                    select p;

                foreach (var item in properties)
                {
                    var value = (string)item.GetValue(entry.Entity, null);
                    if (string.IsNullOrWhiteSpace(value) && item.CanWrite)
                    {
                        item.SetValue(entry.Entity, null, null);
                    }
                }
            }

            try
            {
                return base.SaveChanges();
            }
            catch (Exception ex)
            {
                var ve = ex as DbEntityValidationException;
                if (ve == null)
                    LogService?.Write(ex);
                else
                    LogValidationError(ve);
                throw;
            }
        }



        protected virtual string GetTableName(DbEntityEntry ent)
        {
            ObjectContext objectContext = ((IObjectContextAdapter)this).ObjectContext;
            Type entityType = ent.Entity.GetType();

            if (entityType.BaseType != null && entityType.Namespace == "System.Data.Entity.DynamicProxies")
                entityType = entityType.BaseType;

            var entityTypeName = entityType.Name;

            EntityContainer container =
                objectContext.MetadataWorkspace.GetEntityContainer(objectContext.DefaultContainerName, DataSpace.CSpace);
            var entitySetName = (from meta in container.BaseEntitySets
                where meta.ElementType.Name == entityTypeName
                select meta.Name).FirstOrDefault();

            return entitySetName ?? entityTypeName;
        }


        protected virtual void LogValidationError(DbEntityValidationException ve)
        {
            if (ve == null) return;

            var validationErrors = new StringBuilder();
            ve.EntityValidationErrors.ToList().ForEach(e =>
                e.ValidationErrors.ToList().ForEach(error =>
                    validationErrors.AppendFormat("Validation Error :: {2}.{0} - {1}", error.PropertyName, error.ErrorMessage, GetTableName(e.Entry))));

            LogService?.Write(ve, validationErrors.ToString());

            throw new DbEntityValidationException("Entity Validation Failed - errors follow:\n" + validationErrors.ToString(), ve);

        }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            if (string.IsNullOrWhiteSpace(DefaultSchema))
            {
                modelBuilder.HasDefaultSchema(DefaultSchema);  //Sets Schema for all tables, unless overridden
            }

            if (DisableManyToManyCascadeDeleteConvention)
            {
                modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();
            }

            if (DisableOneToManyCascadeDeleteConvention)
            {
                modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>(); //Removes Cascade Delete
            }

            if (DefaultStringType == DefaultStringTypes.Varchar)
            {
                modelBuilder.Properties<string>().Configure(x => x.HasColumnType("VARCHAR"));
            }

            modelBuilder.Properties<int>()
                .Where(p => p.Name.Equals("Id"))
                .Configure(c => c.HasColumnName(c.ClrPropertyInfo.ReflectedType.Name + "Id"));

            base.OnModelCreating(modelBuilder);
        }

    
    }
}