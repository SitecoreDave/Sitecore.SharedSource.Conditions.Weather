using Sitecore.Rules;
using Sitecore.Rules.Conditions;
using System;

namespace Sitecore.SharedSource.Conditions.Weather
{
    public class TemperatureCondition<T> : OperatorCondition<T> where T : RuleContext
    {
        public string Value { get; set; }

        protected override bool Execute(T ruleContext)
        {
            if (String.IsNullOrEmpty(Value)) return false;

            double value;
            if(!Double.TryParse(Value, out value)) return false;

            var tempString = Common.GetTemperature();

            double temperature;
            if(!Double.TryParse(tempString, out temperature)) return false;

            var compareResult = temperature.CompareTo(value);

            switch (GetOperator())
            {
                case ConditionOperator.Equal:
                    return compareResult == 0;
                case ConditionOperator.GreaterThanOrEqual:
                    return compareResult == 0 || compareResult > 0;
                case ConditionOperator.GreaterThan:
                    return compareResult > 0;
                case ConditionOperator.LessThanOrEqual:
                    return compareResult == 0 || compareResult < 0;
                case ConditionOperator.LessThan:
                    return compareResult < 0;
                case ConditionOperator.NotEqual:
                    return compareResult != 0;
                default:
                    return false;
            }
        }
    }
}