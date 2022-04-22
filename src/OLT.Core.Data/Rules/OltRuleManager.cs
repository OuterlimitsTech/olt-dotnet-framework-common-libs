using System;
using System.Collections.Generic;

namespace OLT.Core
{
    public interface IOltActionRule<in TRequest, in TContext> : IOltRule, IOltInjectableSingleton
        where TRequest : class, IOltRuleRequest
        where TContext : class, IOltDbContext
    {
        IOltRuleResult CanExecute(TRequest request, TContext context);
        IOltRuleResult Execute(TRequest request, TContext context);
    }

    public abstract class OltActionRule<TRequest, TContext> : OltDisposable, IOltActionRule<TRequest, TContext>
        where TRequest : class, IOltRuleRequest
        where TContext : class, IOltDbContext
    {
        //public abstract bool CanExecute(TContext context, TRequest request);
        //public abstract bool Execute(TContext context, TRequest request);
        //public abstract string RuleName { get; }
        public abstract IOltRuleResult CanExecute(TRequest request, TContext context);
        public abstract IOltRuleResult Execute(TRequest request, TContext context);
        public virtual string RuleName => this.GetType().FullName;
        protected virtual IOltRuleResult Success() => new OltRuleResult();
        protected virtual IOltRuleResult BadRequest(OltValidationSeverityTypes severity, string message) => new OltRuleResult(severity, message);

    }

    public class OltRuleManager : OltDisposable, IOltRuleManager
    {
        //private readonly List<IOltRule> _rules;

        //public OltRuleManager(IServiceProvider serviceProvider)
        //{
        //    _rules = serviceProvider.GetServices<IOltRule>().ToList();
        //}

        //public virtual TRule GetRule<TRule>()
        //    where TRule : class, IOltRule
        //{
        //    var ruleName = typeof(TRule).FullName;
        //    var rule = _rules.FirstOrDefault(p => p.RuleName == ruleName);
        //    if (rule == null)
        //    {
        //        throw new Exception($"Rule Not Found {typeof(TRule)}");
        //    }
        //    return rule as TRule;
        //}


        //public virtual List<TRule> GetRules<TRule>()
        //    where TRule : IOltRule
        //{
        //    return _rules.OfType<TRule>().ToList();
        //}
        public TRule GetRule<TRule>() where TRule : class, IOltRule
        {
            throw new NotImplementedException();
        }

        public List<TRule> GetRules<TRule>() where TRule : IOltRule
        {
            throw new NotImplementedException();
        }
    }
}