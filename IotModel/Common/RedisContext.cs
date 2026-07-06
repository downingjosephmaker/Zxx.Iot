using CenBoCommon.Zxx;
using NewLife.Log;
using NewLife.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace IotModel
{

    public class RedisContext<T> where T : class, new()
    {
        private readonly IDatabase _redisDb = RedisHelper.RedisService;
        private readonly string typename = typeof(T).Name;

        private List<TableColumn> GetFieldNames()
        {
            List<TableColumn> list = new();
            PropertyInfo[] tmod = typeof(T).GetProperties();
            foreach (PropertyInfo fi in tmod)
            {
                foreach (var cusa in fi.CustomAttributes)
                {
                    if (cusa.AttributeType.Name == "SugarColumn")
                    {
                        TableColumn column = new()
                        {
                            ParamName = fi.Name
                        };

                        if (cusa.NamedArguments.Count >= 1)
                        {
                            string ColumnName = cusa.NamedArguments[0].MemberName;
                            if (ColumnName == "ColumnName")
                            {
                                column.FieldName = cusa.NamedArguments[0].TypedValue.Value.ToString();
                                if (column.FieldName.ToLower().Contains("time")
                                && !column.FieldName.ToLower().Contains("checktime"))
                                {
                                    column.IsTime = true;
                                }
                            }

                            if (cusa.NamedArguments.Count >= 2)
                            {
                                if (cusa.NamedArguments[1].MemberName == "IsPrimaryKey")
                                {
                                    column.IsPrimaryKey = true;
                                }
                            }
                            list.Add(column);
                        }
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// 新增/更新
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cacheDurationInSeconds">过期时间(秒)</param>
        public virtual async Task<bool> Save(T value, int cacheDurationInSeconds)
        {
            bool res = false;
            try
            {
                res = await SaveBatch(new List<T> { value }, cacheDurationInSeconds);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
            return res;
        }

        /// <summary>
        /// 批量新增/更新
        /// </summary>
        /// <param name="values"></param>
        /// <param name="cacheDurationInSeconds">过期时间(秒)</param>
        public virtual async Task<bool> SaveBatch(List<T> values, int cacheDurationInSeconds = 0)
        {
            bool res = false;
            try
            {
                var _list = await GetList();
                if (_list.IsZxxAny())
                {
                    var fieldNames = GetFieldNames();
                    var IsPrimaryKey = fieldNames.Find(t => t.IsPrimaryKey);
                    for (int i = _list.Count - 1; i >= 0; i--)
                    {
                        var item = _list[i];
                        var pkFieldValue = item.GetType().GetProperty(IsPrimaryKey.ParamName).GetValue(item, null);
                        if (pkFieldValue != null)
                        {
                            if (values.Any(t => t.GetType().GetProperty(IsPrimaryKey.ParamName).GetValue(item, null) == pkFieldValue)) _list.Remove(item);
                        }
                    }
                    _list.AddRange(values);
                    if (cacheDurationInSeconds > 0)
                    {
                        res = await _redisDb.StringSetAsync(typename, _list.ToJson(), TimeSpan.FromSeconds(cacheDurationInSeconds));
                    }
                    else
                    {
                        res = await _redisDb.StringSetAsync(typename, _list.ToJson());
                    }
                }
                else
                {
                    if (cacheDurationInSeconds > 0)
                    {
                        res = await _redisDb.StringSetAsync(typename, values.ToJson(), TimeSpan.FromSeconds(cacheDurationInSeconds));
                    }
                    else
                    {
                        res = await _redisDb.StringSetAsync(typename, values.ToJson());
                    }
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
            return res;
        }

        private bool ContainsKey()
        {
            return _redisDb.KeyExists(typename);
        }

        public async Task<List<T>> GetList()
        {
            List<T> list = new List<T>();
            try
            {
                var valuestr = await _redisDb.StringGetAsync(typename);
                if (!valuestr.IsNullOrEmpty)
                {
                    var _list = valuestr.HasValue ? valuestr.ToString().ToObject<List<T>>() : null;
                    if (_list.IsZxxAny()) list.AddRange(_list);
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
            return list;
        }

        public virtual async Task<T> GetOneBy(Predicate<T> wheres)
        {
            try
            {
                var _list = await GetList();
                if (_list.Count > 0)
                {
                    return _list.Find(wheres);
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
            return default(T);
        }

        public virtual async Task<List<T>> GetListBy(Predicate<T> wheres)
        {
            List<T> list = new List<T>();
            try
            {
                var _list = await GetList();
                if (_list.Count > 0)
                {
                    list.AddRange(_list.FindAll(wheres));
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
            return list;
        }

        public virtual async Task DeleteByKey()
        {
            try
            {
                await _redisDb.KeyDeleteAsync(typename);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        public virtual async Task<bool> DeleteBy(Predicate<T> wheres)
        {
            bool res = false;
            try
            {
                if (ContainsKey())
                {
                    var list = await GetList();
                    if (list.IsZxxAny())
                    {
                        list.RemoveAll(wheres);
                        if (list.Count == 0)
                        {
                            await DeleteByKey();
                            res = true;
                        }
                        else res = await SaveBatch(list);
                    }
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
            return res;
        }

    }
}
