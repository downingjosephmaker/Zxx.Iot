using System;

namespace IotModel
{
    /// <summary>
    /// 数值范围验证特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IntRangeAttribute : Attribute
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public string ErrorMessage { get; set; }

        public IntRangeAttribute(double min, double max, string errorMessage = null)
        {
            Min = min;
            Max = max;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// 枚举范围验证特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class EnumRangeAttribute : Attribute
    {
        public Type EnumType { get; }
        public string ErrorMessage { get; }

        public EnumRangeAttribute(Type enumType, string errorMessage = null)
        {
            if (!enumType.IsEnum)
                throw new ArgumentException("类型必须是枚举", nameof(enumType));

            EnumType = enumType;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Json字段特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class JsonFieldAttribute : Attribute
    {
        /// <summary>
        /// 字段反序列化类
        /// </summary>
        public Type Entity { get; }
        public JsonFieldAttribute(Type _Entity)
        {
            Entity = _Entity;
        }
    }


    /// <summary>
    /// 表全字段类特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class FullEntityAttribute : Attribute
    {
        public FullEntityAttribute()
        {
        }
    }

    /// <summary>
    /// 表拓展类特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ExpandAttribute : Attribute
    {
        public ExpandAttribute()
        {
        }
    }

    /// <summary>
    /// Tendis缓存特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class EntityCacheAttribute : Attribute
    {
        public EntityCacheAttribute()
        {
        }
    }

    /// <summary>
    /// Sqlite缓存特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class DbSqliteAttribute : Attribute
    {
        public DbSqliteAttribute()
        {
        }
    }
}