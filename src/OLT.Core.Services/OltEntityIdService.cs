﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace OLT.Core
{
    public abstract class OltEntityIdService<TContext, TEntity> : OltEntityService<TContext, TEntity>, IOltEntityIdService<TEntity>
        where TEntity : class, IOltEntityId, IOltEntity
        where TContext : class, IOltDbContext
    {
        protected OltEntityIdService(
            IOltServiceManager serviceManager,
            TContext context) : base(serviceManager, context)
        {
        }


        protected virtual TEntity FindBy(int id) => GetQueryable(id).FirstOrDefault();
        protected virtual IQueryable<TEntity> GetQueryable(int id) => GetQueryable().Where(p => p.Id == id);
        public virtual TModel Get<TModel>(int id) where TModel : class, new() => base.Get<TModel>(GetQueryable(id));
        public TModel Get<TModel>(Guid uid) where TModel : class, new() => Get<TModel>(GetQueryable(uid));
        public IEnumerable<TModel> GetAll<TModel>(Guid uid) where TModel : class, new() => GetAll<TModel>(GetQueryable(uid));

        protected IQueryable<TEntity> GetQueryable(Guid uid)
        {
            if (typeof(IOltEntityUniqueId).IsAssignableFrom(typeof(TEntity)))
            {
                Expression<Func<TEntity, bool>> getByUid = x => ((IOltEntityUniqueId)x).UniqueId == uid;
                getByUid = (Expression<Func<TEntity, bool>>)OltRemoveCastsVisitor.Visit(getByUid);
                return this.Repository.Where(getByUid);
            }
            throw new Exception($"Unable to cast to {nameof(IOltEntityUniqueId)}");
        }

        public override TModel Add<TModel>(TModel model)
        {
            var adapter = ServiceManager.AdapterResolver.GetAdapter<TEntity, TModel>();
            var entity = Repository.Create();
            adapter.Map(model, entity);
            Repository.Add(entity);
            SaveChanges();
            return Get<TModel>(entity.Id);
        }

        public override TResponseModel Add<TResponseModel, TSaveModel>(TSaveModel model)
        {
            var adapter = ServiceManager.AdapterResolver.GetAdapter<TEntity, TSaveModel>();
            var entity = Repository.Create();
            adapter.Map(model, entity);
            Repository.Add(entity);
            SaveChanges();
            return Get<TResponseModel>(entity.Id);
        }

        public override IEnumerable<TResponseModel> Add<TResponseModel, TSaveModel>(IEnumerable<TSaveModel> list)
        {
            var adapter = ServiceManager.AdapterResolver.GetAdapter<TEntity, TSaveModel>();
            var entities = new List<TEntity>();
            list.ToList().ForEach(model =>
            {
                var entity = Repository.Create();
                adapter.Map(model, entity);
                entities.Add(Repository.Add(entity));
            });

            SaveChanges();

            var returnList = new List<TResponseModel>();
            entities.ForEach(entity =>
            {
                returnList.Add(Get<TResponseModel>(entity.Id));
            });
            return returnList;
        }


        public virtual TModel Update<TModel>(int id, TModel model)
            where TModel : class, new()
        {
            var adapter = ServiceManager.AdapterResolver.GetAdapter<TEntity, TModel>();
            var entity = base.Include(GetQueryable(id), adapter).FirstOrDefault();
            adapter.Map(model, entity);
            SaveChanges();
            return Get<TModel>(id);
        }


        public virtual TResponseModel Update<TResponseModel, TModel>(int id, TModel model)
            where TModel : class, new()
            where TResponseModel : class, new()
        {
            var adapter = ServiceManager.AdapterResolver.GetAdapter<TEntity, TModel>();
            var entity = base.Include(GetQueryable(id), adapter).FirstOrDefault();
            adapter.Map(model, entity);
            SaveChanges();
            return Get<TResponseModel>(id);
        }

        public virtual TModel Update<TModel>(Guid uid, TModel model)
            where TModel : class, new()
        {
            var adapter = ServiceManager.AdapterResolver.GetAdapter<TEntity, TModel>();
            var entity = base.Include(GetQueryable(uid), adapter).FirstOrDefault();
            adapter.Map(model, entity);
            SaveChanges();
            return Get<TModel>(uid);
        }

        public virtual TResponseModel Update<TResponseModel, TModel>(Guid uid, TModel model)
            where TModel : class, new()
            where TResponseModel : class, new()
        {
            var adapter = ServiceManager.AdapterResolver.GetAdapter<TEntity, TModel>();
            var entity = base.Include(GetQueryable(uid), adapter).FirstOrDefault();
            adapter.Map(model, entity);
            SaveChanges();
            return Get<TResponseModel>(uid);
        }

        public virtual bool SoftDelete(Guid uid)
        {
            var entity = GetQueryable(uid).FirstOrDefault();
            return entity != null && MarkDeleted(entity);
        }

        public virtual bool SoftDelete(int id)
        {
            var entity = GetQueryable(id).FirstOrDefault();
            return entity != null && MarkDeleted(entity);
        }

        public int Count(IOltSearcher<TEntity> searcher)
        {
            return GetQueryable(searcher).Count();
        }
    }
}