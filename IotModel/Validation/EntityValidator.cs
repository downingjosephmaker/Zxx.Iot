using FluentValidation;
using SqlSugar;
using System;
using System.Reflection;

namespace IotModel
{
    public class EntityValidator<T> : AbstractValidator<T> where T : class
    {
        public EntityValidator()
        {
            var properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                // 检查是否包含Length属性
                var lengthAttr = property.GetCustomAttribute<SugarColumn>();
                if (lengthAttr?.Length > 0)
                {
                    // 字符串类型验证
                    if (property.PropertyType == typeof(string))
                    {
                        var rule = RuleFor(x => property.GetValue(x) as string);

                        if (!lengthAttr.IsNullable)
                        {
                            rule.NotEmpty().WithMessage($"{property.Name}不能为空");
                        }

                        rule.MaximumLength(lengthAttr.Length)
                            .WithMessage($"{property.Name}长度不能超过{lengthAttr.Length}个字符");
                    }

                    // 数值类型验证
                    if (property.PropertyType == typeof(int) ||
                        property.PropertyType == typeof(long) ||
                        property.PropertyType == typeof(decimal) ||
                        property.PropertyType == typeof(double) ||
                        property.PropertyType == typeof(float))
                    {
                        var rule = RuleFor(x => property.GetValue(x));
                        if (!lengthAttr.IsNullable)
                        {
                            rule.NotEmpty().WithMessage($"{property.Name}不能为空");
                        }
                    }
                }

                // 检查是否包含Range属性
                var rangeAttr = property.GetCustomAttribute<IntRangeAttribute>();
                if (rangeAttr != null)
                {
                    if (property.PropertyType == typeof(int) ||
                        property.PropertyType == typeof(long) ||
                        property.PropertyType == typeof(decimal) ||
                        property.PropertyType == typeof(double) ||
                        property.PropertyType == typeof(float))
                    {
                        var rule = RuleFor(x => Convert.ToDecimal(property.GetValue(x)));

                        rule.GreaterThanOrEqualTo((decimal)rangeAttr.Min)
                            .WithMessage(rangeAttr.ErrorMessage ?? $"{property.Name}不能小于{rangeAttr.Min}");

                        rule.LessThanOrEqualTo((decimal)rangeAttr.Max)
                            .WithMessage(rangeAttr.ErrorMessage ?? $"{property.Name}不能大于{rangeAttr.Max}");
                    }
                }

                // 检查是否包含EnumRange属性
                var enumRangeAttr = property.GetCustomAttribute<EnumRangeAttribute>();
                if (enumRangeAttr != null && property.PropertyType.IsEnum)
                {
                    var rule = RuleFor(x => property.GetValue(x));
                    rule.Must(value => Enum.IsDefined(enumRangeAttr.EnumType, value))
                        .WithMessage(enumRangeAttr.ErrorMessage ?? $"{property.Name}必须是有效的枚举值");
                }

            }
        }
    }
}