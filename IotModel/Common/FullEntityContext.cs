using CenBoCommon.Zxx;
using IotLog;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IotModel
{
    /// <summary>
    /// 完整实体上下文基类
    /// </summary>
    /// <typeparam name="T">完整实体类型</typeparam>
    public class FullEntityContext<T> where T : class, new()
    {
        /// <summary>
        /// DbContext<>
        /// </summary>
        public object _dbContext;
        /// <summary>
        /// 基础实体类
        /// </summary>
        private readonly Type _baseType;
        /// <summary>
        /// JsonField集合
        /// </summary>
        private readonly Dictionary<string, Type> _jsonFields;

        /// <summary>
        /// 通过反射调用 _dbContext 的方法，并拆包 TargetInvocationException 还原真实异常类型。
        /// 反射调用会把被调用方法抛出的异常包成 TargetInvocationException（真实异常在 InnerException），
        /// 导致上层 is ValidationException 等类型判断失效。此处统一拆包还原。
        /// </summary>
        protected object InvokeDbMethod(MethodInfo method, params object[] args)
        {
            try
            {
                return method.Invoke(_dbContext, args);
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                // 还原原始异常类型和堆栈
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                throw; // 不会执行到
            }
        }

        /// <summary>
        /// 获取SQL语句信息
        /// </summary>
        protected string sqlSugar => _dbContext.GetType().GetProperty("sqlSugar")?.GetValue(_dbContext) as string;

        /// <summary>
        /// 获取SQL错误信息
        /// </summary>
        protected string sqlError => _dbContext.GetType().GetProperty("sqlError")?.GetValue(_dbContext) as string;

        /// <summary>
        /// 获取数据库访问对象
        /// </summary>
        protected readonly SqlSugarScope Db;

        /// <summary>
        /// 树形结构层级标识
        /// </summary>
        public readonly Dictionary<int, string> ObjLevel;

        #region 构造函数
        public FullEntityContext()
        {
            // 获取基础实体类型
            _baseType = typeof(T).BaseType;
            if (_baseType == null)
            {
                throw new Exception($"类型 {typeof(T).Name} 必须继承自基础实体类");
            }

            // 获取所有带有JsonFieldAttribute的属性
            _jsonFields = new Dictionary<string, Type>();
            var jsonFieldProps = _baseType.GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(JsonFieldAttribute), true).Any());

            if (!jsonFieldProps.Any())
            {
                throw new Exception($"基础类型 {_baseType.Name} 必须包含带有 JsonFieldAttribute 的属性");
            }

            foreach (var prop in jsonFieldProps)
            {
                var attr = prop.GetCustomAttribute<JsonFieldAttribute>();
                if (attr?.Entity == null)
                {
                    throw new Exception($"基础类型 {_baseType.Name} 的属性 {prop.Name} 的 JsonFieldAttribute 必须指定扩展类型");
                }
                _jsonFields[prop.Name] = attr.Entity;
            }

            // 创建基础实体上下文，使用T的基类
            var dbContextType = typeof(DbContext<>).MakeGenericType(_baseType);
            _dbContext = Activator.CreateInstance(dbContextType, this);
            Db = _dbContext.GetType().GetProperty("Db")?.GetValue(_dbContext) as SqlSugarScope;
            ObjLevel = SqlSugarHelper.ObjLevel;
        }

        #endregion

        #region 公用方法方法

        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Init(object[] objs)
        {
        }

        /// <summary>
        /// 扩展属性访问器
        /// </summary>
        private class ExpandPropertyVisitor : ExpressionVisitor
        {
            private readonly Dictionary<string, Type> _jsonFields;

            public ExpandPropertyVisitor(Dictionary<string, Type> jsonFields)
            {
                _jsonFields = jsonFields;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                // 检查是否是扩展属性
                var expandProperty = node.Member as PropertyInfo;
                if (expandProperty != null)
                {
                    var jsonField = _jsonFields.FirstOrDefault(x => x.Value == expandProperty.PropertyType);
                    if (!string.IsNullOrEmpty(jsonField.Key))
                    {
                        // 将扩展属性访问转换为JsonField属性访问
                        var baseType = node.Expression.Type.BaseType;
                        var jsonFieldProp = baseType.GetProperty(jsonField.Key);
                        if (jsonFieldProp != null)
                        {
                            // 创建JsonField属性访问表达式
                            var jsonFieldAccess = Expression.Property(
                                Expression.Convert(node.Expression, baseType),
                                jsonFieldProp
                            );

                            // 如果是字符串比较，直接返回
                            if (node.Type == typeof(string))
                            {
                                return jsonFieldAccess;
                            }

                            // 如果是对象比较，需要转换为JSON字符串
                            return Expression.Call(
                                typeof(Operator).GetMethod("ToJson", new[] { typeof(object) }),
                                jsonFieldAccess
                            );
                        }
                    }
                }

                return base.VisitMember(node);
            }
        }

        private class ReplaceParameterVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly ParameterExpression _newParameter;
            private readonly Dictionary<string, Type> _jsonFields;

            public ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter, Dictionary<string, Type> jsonFields = null)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
                _jsonFields = jsonFields;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _oldParameter ? _newParameter : base.VisitParameter(node);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                var newExpr = Visit(node.Expression);
                if (newExpr == node.Expression)
                    return node;

                if (node.Member is PropertyInfo prop)
                {
                    // 属性存在于新类型（基类）上时直接重建
                    var propOnBase = newExpr.Type.GetProperty(prop.Name);
                    if (propOnBase != null)
                        return Expression.Property(newExpr, propOnBase);

                    // 属性只存在于 T（完整实体）上 —— 映射到基类对应的 JSON 字段
                    if (_jsonFields != null)
                    {
                        string jsonKey = null;
                        // 精确匹配（单个扩展对象）
                        var exact = _jsonFields.FirstOrDefault(x => x.Value == prop.PropertyType);
                        if (!string.IsNullOrEmpty(exact.Key))
                        {
                            jsonKey = exact.Key;
                        }
                        // List<扩展对象> 匹配
                        else if (prop.PropertyType.IsGenericType &&
                                 prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            var elemType = prop.PropertyType.GetGenericArguments()[0];
                            var listMatch = _jsonFields.FirstOrDefault(x => x.Value == elemType);
                            if (!string.IsNullOrEmpty(listMatch.Key))
                                jsonKey = listMatch.Key;
                        }

                        if (jsonKey != null)
                        {
                            var jsonProp = newExpr.Type.GetProperty(jsonKey);
                            if (jsonProp != null)
                                return Expression.Property(newExpr, jsonProp);
                        }
                    }
                }

                return Expression.MakeMemberAccess(newExpr, node.Member);
            }
        }

        /// <summary>
        /// 将基础实体转换为完整实体
        /// </summary>
        protected T ConvertToFullEntity(object baseEntity)
        {
            if (baseEntity == null) return null;

            var fullEntity = new T();
            try
            {
                // 复制基础属性
                foreach (var prop in _baseType.GetProperties())
                {
                    var value = prop.GetValue(baseEntity);
                    prop.SetValue(fullEntity, value);
                }

                // 处理所有扩展属性
                foreach (var jsonField in _jsonFields)
                {
                    var jsonFieldProp = _baseType.GetProperty(jsonField.Key);
                    if (jsonFieldProp != null)
                    {
                        var jsonValue = jsonFieldProp.GetValue(baseEntity)?.ToString();
                        if (!string.IsNullOrEmpty(jsonValue))
                        {
                            // 获取扩展属性
                            var expandProperty = typeof(T).GetProperties()
                                .FirstOrDefault(p => p.PropertyType == jsonField.Value);
                            if (expandProperty != null)
                            {
                                // 单个扩展类
                                var toObjectMethod = typeof(Operator).GetMethod("ToObject").MakeGenericMethod(jsonField.Value);
                                var expandObject = toObjectMethod.Invoke(null, new object[] { jsonValue });
                                //var expandObject = jsonValue.ToObject(jsonField.Value);
                                if (expandObject != null)
                                {
                                    expandProperty.SetValue(fullEntity, expandObject);
                                }
                            }
                            else
                            {
                                // 检查是否是扩展类集合
                                var expandListProperty = typeof(T).GetProperties()
                                    .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                                       p.PropertyType.GetGenericTypeDefinition() == typeof(List<>) &&
                                                       p.PropertyType.GetGenericArguments()[0] == jsonField.Value);
                                if (expandListProperty != null)
                                {
                                    // 扩展类集合
                                    var listType = typeof(List<>).MakeGenericType(jsonField.Value);
                                    var toObjectMethod = typeof(Operator).GetMethod("ToObject").MakeGenericMethod(listType);
                                    var expandList = toObjectMethod.Invoke(null, new object[] { jsonValue });
                                    //var expandList = jsonValue.ToObject(listType);
                                    if (expandList != null)
                                    {
                                        expandListProperty.SetValue(fullEntity, expandList);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return fullEntity;
        }

        /// <summary>
        /// 将完整实体转换为基础实体
        /// </summary>
        protected object ConvertToBaseEntity(T fullEntity)
        {
            if (fullEntity == null) return null;

            var baseEntity = Activator.CreateInstance(_baseType);
            try
            {
                // 复制基础属性
                foreach (var prop in _baseType.GetProperties())
                {
                    var value = prop.GetValue(fullEntity);
                    prop.SetValue(baseEntity, value);
                }

                // 处理所有扩展属性
                foreach (var jsonField in _jsonFields)
                {
                    // 检查单个扩展类
                    var expandProperty = typeof(T).GetProperties()
                        .FirstOrDefault(p => p.PropertyType == jsonField.Value);
                    if (expandProperty != null)
                    {
                        var expandObject = expandProperty.GetValue(fullEntity);
                        if (expandObject != null)
                        {
                            var jsonFieldProp = _baseType.GetProperty(jsonField.Key);
                            if (jsonFieldProp != null)
                            {
                                jsonFieldProp.SetValue(baseEntity, expandObject.ToJson());
                            }
                        }
                    }
                    else
                    {
                        // 检查扩展类集合
                        var expandListProperty = typeof(T).GetProperties()
                            .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                               p.PropertyType.GetGenericTypeDefinition() == typeof(List<>) &&
                                               p.PropertyType.GetGenericArguments()[0] == jsonField.Value);
                        if (expandListProperty != null)
                        {
                            var expandList = expandListProperty.GetValue(fullEntity) as IEnumerable<object>;
                            if (expandList.IsZxxAny())
                            {
                                var jsonFieldProp = _baseType.GetProperty(jsonField.Key);
                                if (jsonFieldProp != null)
                                {
                                    jsonFieldProp.SetValue(baseEntity, expandList.ToJson());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return baseEntity;
        }

        /// <summary>
        /// 处理扩展属性的查询条件
        /// </summary>
        private LambdaExpression ConvertExpandCondition(Expression<Func<T, bool>> wheres)
        {
            if (wheres == null) return null;

            // 创建新的参数表达式，类型为基础实体类型
            var parameter = Expression.Parameter(_baseType, wheres.Parameters[0].Name);

            // 创建参数替换访问器
            var visitor = new ReplaceParameterVisitor(wheres.Parameters[0], parameter, _jsonFields);
            var newBody = visitor.Visit(wheres.Body);

            // 创建新的Lambda表达式
            var funcType = typeof(Func<,>).MakeGenericType(_baseType, typeof(bool));
            return Expression.Lambda(funcType, newBody, parameter);
        }

        /// <summary>
        /// 属性名称访问器
        /// </summary>
        private class PropertyNameVisitor : ExpressionVisitor
        {
            private readonly Dictionary<string, string> _propertyMap;

            public PropertyNameVisitor(Dictionary<string, string> propertyMap)
            {
                _propertyMap = propertyMap;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Member is PropertyInfo prop)
                {
                    if (_propertyMap.TryGetValue(prop.Name, out string columnName))
                    {
                        // 创建新的属性访问表达式
                        var newProp = node.Expression.Type.GetProperty(columnName);
                        if (newProp != null)
                        {
                            return Expression.Property(node.Expression, newProp);
                        }
                    }
                }
                return base.VisitMember(node);
            }
        }

        /// <summary>
        /// 转换表达式参数类型
        /// </summary>
        private LambdaExpression ConvertExpression(Expression<Func<T, object>> column)
        {
            var parameter = Expression.Parameter(_baseType, column.Parameters[0].Name);
            var visitor = new ReplaceParameterVisitor(column.Parameters[0], parameter, _jsonFields);
            var newBody = visitor.Visit(column.Body);
            var funcType = typeof(Func<,>).MakeGenericType(_baseType, typeof(object));
            return Expression.Lambda(funcType, newBody, parameter);
        }

        /// <summary>
        /// 转换基础实体列表
        /// </summary>
        private object ConvertToBaseEntityList(List<T> entities)
        {
            var baseEntities = entities.Select(ConvertToBaseEntity).ToList();
            var listType = typeof(List<>).MakeGenericType(_baseType);
            var baseEntityList = Activator.CreateInstance(listType);
            var addMethod = listType.GetMethod("Add");
            foreach (var entity in baseEntities)
            {
                addMethod.Invoke(baseEntityList, new[] { entity });
            }
            return baseEntityList;
        }

        #endregion

        #region 查询操作

        /// <summary>
        /// 获取单条数据
        /// </summary>
        public virtual T GetOneBy(Expression<Func<T, bool>> wheres)
        {
            var method = _dbContext.GetType().GetMethod("GetOneBy", new[] { typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(_baseType, typeof(bool))) });
            var convertedWheres = ConvertExpandCondition(wheres);
            var baseEntity = InvokeDbMethod(method, new object[] { convertedWheres });
            return ConvertToFullEntity(baseEntity);
        }

        /// <summary>
        /// 根据sql语句获取data
        /// </summary>
        public virtual object GetScalar(string sql, object parameters = null)
        {
            var method = _dbContext.GetType().GetMethod("GetScalar", new[] { typeof(string), typeof(object) });
            return InvokeDbMethod(method, new object[] { sql, parameters });
        }

        /// <summary>
        /// 获取所有
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual List<T> GetList()
        {
            var method = _dbContext.GetType().GetMethod("GetList");
            var baseEntities = InvokeDbMethod(method, null) as IEnumerable<object>;
            return baseEntities?.Select(ConvertToFullEntity).ToList();
        }

        /// <summary>
        /// 根据条件获取列表
        /// </summary>
        public virtual List<T> GetListBy(Expression<Func<T, bool>> wheres)
        {
            var method = _dbContext.GetType().GetMethod("GetListBy", new[] { typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(_baseType, typeof(bool))) });
            var convertedWheres = ConvertExpandCondition(wheres);
            var baseEntities = InvokeDbMethod(method, new object[] { convertedWheres }) as IEnumerable<object>;
            return baseEntities?.Select(ConvertToFullEntity).ToList();
        }

        /// <summary>
        /// 根据条件获取列表
        /// </summary>
        public virtual List<T> GetListBy(List<IConditionalModel> conModel)
        {
            var method = _dbContext.GetType().GetMethod("GetListBy", new[] { typeof(List<IConditionalModel>) });
            var baseEntities = InvokeDbMethod(method, new object[] { conModel }) as IEnumerable<object>;
            return baseEntities?.Select(ConvertToFullEntity).ToList();
        }

        /// <summary>
        /// 根据表达式查询分页
        /// </summary>
        public virtual List<T> GetPageList(Expression<Func<T, bool>> wheres, PageModel page)
        {
            var method = _dbContext.GetType().GetMethod("GetPageList", new[] { typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(_baseType, typeof(bool))), typeof(PageModel) });
            var convertedWheres = ConvertExpandCondition(wheres);
            var baseEntities = InvokeDbMethod(method, new object[] { convertedWheres, page }) as IEnumerable<object>;
            return baseEntities?.Select(ConvertToFullEntity).ToList();
        }

        /// <summary>
        /// 根据条件获取分页
        /// </summary>
        public virtual List<T> GetPageList(List<IConditionalModel> conModel, PageModel page)
        {
            var method = _dbContext.GetType().GetMethod("GetPageList", new[] { typeof(List<IConditionalModel>), typeof(PageModel) });
            var baseEntities = InvokeDbMethod(method, new object[] { conModel, page }) as IEnumerable<object>;
            return baseEntities?.Select(ConvertToFullEntity).ToList();
        }

        /// <summary>
        /// 根据表达式查询分页并排序
        /// </summary>
        public virtual List<T> GetPageList(Expression<Func<T, bool>> whereExpression, PageModel page, Expression<Func<T, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
        {
            var method = _dbContext.GetType().GetMethod("GetPageList", new[] {
                typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(_baseType, typeof(bool))),
                typeof(PageModel),
                typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(_baseType, typeof(object))),
                typeof(OrderByType)
            });
            var convertedWheres = ConvertExpandCondition(whereExpression);
            var convertedOrderBy = orderByExpression != null ? ConvertExpression(orderByExpression) : null;
            var baseEntities = InvokeDbMethod(method, new object[] { convertedWheres, page, convertedOrderBy, orderByType }) as IEnumerable<object>;
            return baseEntities?.Select(ConvertToFullEntity).ToList();
        }

        /// <summary>
        /// 转换扩展属性的参数值为目标类型
        /// </summary>
        private static object ConvertExpandPropertyValue(object paramValue, Type propType)
        {
            if (paramValue == null) return null;
            try
            {
                if (propType == typeof(string))
                    return paramValue.ToString();
                if (propType == typeof(int) || propType == typeof(int?))
                    return Convert.ToInt32(paramValue);
                if (propType == typeof(double) || propType == typeof(double?))
                    return Convert.ToDouble(paramValue);
                if (propType == typeof(decimal) || propType == typeof(decimal?))
                    return Convert.ToDecimal(paramValue);
                if (propType == typeof(bool) || propType == typeof(bool?))
                    return Convert.ToBoolean(paramValue);
                if (propType == typeof(DateTime) || propType == typeof(DateTime?))
                    return Convert.ToDateTime(paramValue);
                if (propType.IsEnum)
                    return Enum.Parse(propType, paramValue.ToString());
                return Convert.ChangeType(paramValue, propType);
            }
            catch
            {
                return paramValue;
            }
        }

        /// <summary>
        /// 预处理 ActionPara，将扩展属性条件转换为 JSON like 条件
        /// </summary>
        private ActionPara ProcessActionPara(ActionPara model)
        {
            // 复制 sconlist，避免副作用
            var newSconList = new List<SelectCondition>();
            foreach (var x in model.sconlist)
            {
                newSconList.Add(new SelectCondition
                {
                    ParamName = x.ParamName,
                    ParamType = x.ParamType,
                    ParamValue = x.ParamValue,
                    ParamSort = x.ParamSort,
                    ParamGroupName = x.ParamGroupName,
                    GroupCondition = x.GroupCondition,
                    IsGroupFrist = x.IsGroupFrist,
                });
            }

            // 处理扩展属性条件
            foreach (var jsonField in _jsonFields)
            {
                var expandConds = newSconList.Where(s => s.ParamName.StartsWith($"{jsonField.Key}.")).ToList();
                if (!expandConds.Any()) continue;

                var expandObj = Activator.CreateInstance(jsonField.Value);
                bool hasValidProperty = false;

                foreach (var cond in expandConds)
                {
                    try
                    {
                        string expandPropName = cond.ParamName.Substring(jsonField.Key.Length + 1);
                        var expandProp = jsonField.Value.GetProperty(expandPropName);
                        if (expandProp != null)
                        {
                            var convertedValue = ConvertExpandPropertyValue(cond.ParamValue, expandProp.PropertyType);
                            expandProp.SetValue(expandObj, convertedValue);
                            hasValidProperty = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.ErrorLogWrite("FullEntityContext", "反射调用", ex.ToString(), "数据访问");
                    }
                }

                if (hasValidProperty)
                {
                    var existingCond = newSconList.FirstOrDefault(s => s.ParamName == jsonField.Key);
                    string groupCondition = existingCond?.GroupCondition ?? "and";

                    var jsonFieldSconList = new List<SelectCondition>();
                    foreach (var cond in expandConds)
                    {
                        string expandPropName = cond.ParamName.Substring(jsonField.Key.Length + 1);
                        var expandProp = jsonField.Value.GetProperty(expandPropName);
                        if (expandProp == null) continue;

                        var value = expandProp.GetValue(expandObj);
                        if (value != null)
                        {
                            var singleProperty = new Dictionary<string, object> { [expandPropName] = value };
                            jsonFieldSconList.Add(new SelectCondition
                            {
                                ParamName = jsonField.Key,
                                ParamValue = singleProperty.ToJson().Trim('{', '}'),
                                ParamType = "like",
                                GroupCondition = groupCondition
                            });
                        }
                    }

                    newSconList.RemoveAll(s => s.ParamName.StartsWith($"{jsonField.Key}."));
                    newSconList.AddRange(jsonFieldSconList);
                }
            }

            return new ActionPara
            {
                page = model.page,
                pagesize = model.pagesize,
                starttime = model.starttime,
                endtime = model.endtime,
                sconlist = newSconList,
            };
        }

        /// <summary>
        /// 根据封装的条件查询分页数据
        /// </summary>
        public virtual List<T> GetListBy(ActionPara model)
        {
            var newModel = ProcessActionPara(model);
            var method = _dbContext.GetType().GetMethod("GetListBy", new[] { typeof(ActionPara) });
            var baseEntities = InvokeDbMethod(method, new object[] { newModel }) as IEnumerable<object>;
            return baseEntities?.Select(ConvertToFullEntity).ToList();
        }

        /// <summary>
        /// 根据封装的条件查询分页数据
        /// </summary>
        public virtual List<T> GetListByPage(ActionPara model, ref int total)
        {
            var newModel = ProcessActionPara(model);
            var method = _dbContext.GetType().GetMethod("GetListByPage", new[] { typeof(ActionPara), typeof(int).MakeByRefType() });
            var parameters = new object[] { newModel, total };
            var baseEntities = InvokeDbMethod(method, parameters) as IEnumerable<object>;
            total = (int)parameters[1];
            return baseEntities?.Select(ConvertToFullEntity).ToList();
        }

        /// <summary>
        /// 根据表达式查询总数
        /// </summary>
        /// <param name="wheres">表达式</param>
        /// <returns></returns>
        public virtual int GetListCount(Expression<Func<T, bool>> wheres)
        {
            var method = _dbContext.GetType().GetMethod("GetListCount", new[] { typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(_baseType, typeof(bool))) });
            var convertedWheres = ConvertExpandCondition(wheres);
            var baseEntity = InvokeDbMethod(method, new object[] { convertedWheres });
            return baseEntity.ToZxxInt();
        }

        #endregion

        #region 更新操作

        /// <summary>
        /// 根据实体更新，实体需要有主键
        /// </summary>
        public virtual bool Update(T obj)
        {
            return UpdateRange(new List<T> { obj });
        }

        /// <summary>
        /// 批量更新
        /// </summary>
        public virtual bool UpdateRange(List<T> updateObjs)
        {
            var baseEntityList = ConvertToBaseEntityList(updateObjs);
            var method = _dbContext.GetType().GetMethod("UpdateRange", new[] { typeof(List<>).MakeGenericType(_baseType) });
            return (bool)InvokeDbMethod(method, new object[] { baseEntityList });
        }

        /// <summary>
        /// 更新指定列
        /// </summary>
        public virtual bool UpdateDicColumns(Dictionary<string, object> dicparam, Expression<Func<T, bool>> where)
        {
            var method = _dbContext.GetType().GetMethod("UpdateDicColumns", new[] { typeof(Dictionary<string, object>), typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(_baseType, typeof(bool))) });
            var convertedWhere = ConvertExpandCondition(where);
            return (bool)InvokeDbMethod(method, new object[] { dicparam, convertedWhere });
        }

        /// <summary>
        /// 更新指定列
        /// </summary>
        public virtual bool UpdateColumns(T obj, Expression<Func<T, object>> column)
        {
            return UpdateColumns(new List<T> { obj }, column);
        }

        /// <summary>
        /// 批量更新指定列
        /// </summary>
        public virtual bool UpdateColumns(List<T> updateObjs, Expression<Func<T, object>> column)
        {
            var baseEntityList = ConvertToBaseEntityList(updateObjs);
            var method = _dbContext.GetType().GetMethod("UpdateColumns", new[] { typeof(List<>).MakeGenericType(_baseType), typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(_baseType, typeof(object))) });
            var newExpression = ConvertExpression(column);
            return (bool)InvokeDbMethod(method, new object[] { baseEntityList, newExpression });
        }

        /// <summary>
        /// 更新指定列
        /// </summary>
        public virtual bool UpdateColumns(Expression<Func<T, T>> columns, Expression<Func<T, bool>> where)
        {
            var method = _dbContext.GetType().GetMethod("UpdateColumns", new[] {
                typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(_baseType, _baseType)),
                typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(_baseType, typeof(bool)))
                 });
            var convertedWhere = ConvertExpandCondition(where);
            return (bool)InvokeDbMethod(method, new object[] { columns, convertedWhere });
        }

        /// <summary>
        /// 忽略指定列更新
        /// </summary>
        public virtual bool UpdateIgnoreColumns(T obj, Expression<Func<T, object>> column)
        {
            return UpdateIgnoreColumns(new List<T> { obj }, column);
        }

        /// <summary>
        /// 批量忽略指定列更新
        /// </summary>
        public virtual bool UpdateIgnoreColumns(List<T> updateObjs, Expression<Func<T, object>> column)
        {
            var baseEntityList = ConvertToBaseEntityList(updateObjs);
            var method = _dbContext.GetType().GetMethod("UpdateIgnoreColumns", new[] { typeof(List<>).MakeGenericType(_baseType), typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(_baseType, typeof(object))) });
            var newExpression = ConvertExpression(column);
            return (bool)InvokeDbMethod(method, new object[] { baseEntityList, newExpression });
        }

        /// <summary>
        /// 大数据批量更新
        /// </summary>
        public virtual bool UpdateBulkCopy(List<T> updateObjs)
        {
            var baseEntityList = ConvertToBaseEntityList(updateObjs);
            var method = _dbContext.GetType().GetMethod("UpdateBulkCopy", new[] { typeof(List<>).MakeGenericType(_baseType) });
            return (bool)InvokeDbMethod(method, new object[] { baseEntityList });
        }

        #endregion

        #region 新增操作

        /// <summary>
        /// 插入实体
        /// </summary>
        public virtual bool Insert(T obj)
        {
            return InsertRange(new List<T> { obj });
        }

        /// <summary>
        /// 批量插入实体
        /// </summary>
        public virtual bool InsertRange(List<T> insertObjs)
        {
            var baseEntityList = ConvertToBaseEntityList(insertObjs);
            var method = _dbContext.GetType().GetMethod("InsertRange", new[] { typeof(List<>).MakeGenericType(_baseType) });
            return (bool)InvokeDbMethod(method, new object[] { baseEntityList });
        }

        /// <summary>
        /// 新增或更新单条（主键存在则更新，不存在则新增）
        /// </summary>
        public virtual bool Save(T obj)
        {
            return SaveBatch(new List<T> { obj });
        }

        /// <summary>
        /// 批量新增或更新（主键存在则更新，不存在则新增）
        /// </summary>
        public virtual bool SaveBatch(List<T> models)
        {
            var baseEntityList = ConvertToBaseEntityList(models);
            var method = _dbContext.GetType().GetMethod("SaveBatch", new[] { typeof(List<>).MakeGenericType(_baseType) });
            return (bool)InvokeDbMethod(method, new object[] { baseEntityList });
        }

        /// <summary>
        /// 大数量插入，比逐行插入快
        /// </summary>
        public virtual bool InsertBulkCopy(List<T> insertObjs)
        {
            var baseEntityList = ConvertToBaseEntityList(insertObjs);
            var method = _dbContext.GetType().GetMethod("InsertBulkCopy", new[] { typeof(List<>).MakeGenericType(_baseType) });
            return (bool)InvokeDbMethod(method, new object[] { baseEntityList });
        }

        /// <summary>
        /// 插入指定列
        /// </summary>
        public virtual bool InsertColumns(T obj, Expression<Func<T, object>> column)
        {
            return InsertColumns(new List<T> { obj }, column);
        }

        /// <summary>
        /// 批量插入指定列
        /// </summary>
        public virtual bool InsertColumns(List<T> insertObjs, Expression<Func<T, object>> column)
        {
            var baseEntityList = ConvertToBaseEntityList(insertObjs);
            var method = _dbContext.GetType().GetMethod("InsertColumns", new[] { typeof(List<>).MakeGenericType(_baseType), typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(_baseType, typeof(object))) });
            var newExpression = ConvertExpression(column);
            return (bool)InvokeDbMethod(method, new object[] { baseEntityList, newExpression });
        }

        /// <summary>
        /// 忽略指定列插入
        /// </summary>
        public virtual bool InsertIgnoreColumns(T obj, Expression<Func<T, object>> column)
        {
            return InsertIgnoreColumns(new List<T> { obj }, column);
        }

        /// <summary>
        /// 批量忽略指定列插入
        /// </summary>
        public virtual bool InsertIgnoreColumns(List<T> insertObjs, Expression<Func<T, object>> column)
        {

            var baseEntityList = ConvertToBaseEntityList(insertObjs);
            var method = _dbContext.GetType().GetMethod("InsertIgnoreColumns", new[] { typeof(List<>).MakeGenericType(_baseType), typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(_baseType, typeof(object))) });
            var newExpression = ConvertExpression(column);
            return (bool)InvokeDbMethod(method, new object[] { baseEntityList, newExpression });
        }

        /// <summary>
        /// 插入并返回实体
        /// </summary>
        public virtual T InsertReturnEntity(T insertObj)
        {
            var baseEntity = ConvertToBaseEntity(insertObj);
            var method = _dbContext.GetType().GetMethod("InsertReturnEntity", new[] { _baseType });
            var result = InvokeDbMethod(method, new object[] { baseEntity });
            return ConvertToFullEntity(result);
        }

        #endregion

        #region 删除操作

        /// <summary>
        /// 删除实体
        /// </summary>
        public virtual bool DeleteBy(Expression<Func<T, bool>> wheres)
        {
            var method = _dbContext.GetType().GetMethod("DeleteBy", new[] { typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(_baseType, typeof(bool))) });
            var convertedWheres = ConvertExpandCondition(wheres);
            return (bool)InvokeDbMethod(method, new object[] { convertedWheres });
        }

        #endregion

        #region 事务函数

        /// <summary>
        /// 执行事务
        /// </summary>
        public virtual bool TranAction(Action tranAction)
        {
            var method = _dbContext.GetType().GetMethod("TranAction", new[] { typeof(Action) });
            return (bool)InvokeDbMethod(method, tranAction);
        }

        #endregion

    }
}
