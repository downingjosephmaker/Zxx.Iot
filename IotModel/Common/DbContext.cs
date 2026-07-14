using CenBoCommon.Zxx;
using FluentValidation;
using IotLog;
using NewLife;
using SqlSugar;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace IotModel
{
    /// <summary>
    /// 基础
    /// </summary>
    public class SqlSugar_Db
    {
        private readonly Lazy<SqlSugarScope> _db;
        public SqlSugarScope Db => _db.Value;

        private string ConnectionString;
        private DbType DbType = DbType.MySql;
        private Action<SqlSugarException> OnConnectionError; // 连接异常回调

        public SqlSugar_Db(string _ConnectionString, DbType _DbType, Action<SqlSugarException> onConnectionError = null)
        {
            ConnectionString = _ConnectionString;
            DbType = _DbType;
            OnConnectionError = onConnectionError;
            // 在构造函数中初始化Lazy
            _db = new Lazy<SqlSugarScope>(() => CreateDatabase());
        }

        private SqlSugarScope CreateDatabase()
        {
            try
            {
                if (ConnectionString.IsZxxNullOrEmpty()) return null;

                //AllowLoadLocalInfile=true;(启用bulkCopy=大数据快速入库，报错：MYSQL库中执行：SET GLOBAL local_infile=1)
                return new SqlSugarScope(new ConnectionConfig()
                {
                    ConnectionString = ConnectionString,
                    DbType = DbType,
                    InitKeyType = InitKeyType.Attribute,
                    IsAutoCloseConnection = true,
                    MoreSettings = new ConnMoreSettings()
                    {
                        IsNoReadXmlDescription = true,  //禁止读取XML中备注,true是禁用
                        SqlServerCodeFirstNvarchar = true,
                        SqliteCodeFirstEnableDefaultValue = true, //Sqlite启用默认值
                        SqliteCodeFirstEnableDescription = true, //Sqlite启用备注
                        SqliteCodeFirstEnableDropColumn = true,  //Sqlite启用删除列
                        EnableCodeFirstUpdatePrecision = true,  //启用decimal和double类型精度修改
                        DisableNvarchar = true, //禁用nvarchar
                    },
                    ConfigureExternalServices = new ConfigureExternalServices()
                    {
                        EntityService = (x, p) => //处理列名
                        {
                            p.DbColumnName = UtilMethods.ToUnderLine(p.DbColumnName);//驼峰转下划线方法

                            // 自动将 Nullable<T> 属性标记为可空列，避免DataTable映射空值报错
                            //支持string?和string  
                            if (p.IsPrimarykey == false && new NullabilityInfoContext()
                             .Create(x).WriteState is NullabilityState.Nullable)
                            {
                                p.IsNullable = true;
                            }

                            // PostgreSQL 方言适配：实体统一按 MySQL/TiDB 方言声明(bigint/int 带 Length、bit、tinyint)，
                            // 这些在 PG 下会拼出 bigint(20) 等非法 DDL(42601)。此处按库统一改写列类型，一处解决全仓映射，
                            // 不改任何实体定义。仅在 CodeFirst 显式指定 DataType 时生效，未显式指定的走 SqlSugar 默认 C# 类型映射。
                            if (DbType == SqlSugar.DbType.PostgreSQL && !string.IsNullOrEmpty(p.DataType))
                            {
                                var dt = p.DataType.Trim().ToLower();
                                switch (dt)
                                {
                                    case "bigint":
                                    case "int":
                                    case "integer":
                                    case "smallint":
                                        // PG 整数类型不接受长度，清空 Length 避免拼出 int(11)/bigint(20)
                                        p.Length = 0;
                                        p.DataType = dt == "int" ? "int4" : (dt == "bigint" ? "int8" : (dt == "smallint" ? "int2" : "int4"));
                                        break;
                                    case "tinyint":
                                        // PG 无 tinyint，用 smallint(int2) 承载
                                        p.Length = 0;
                                        p.DataType = "int2";
                                        break;
                                    case "bit":
                                        // 实体里 bit 用于承载 bool/0-1，PG 用 bool
                                        p.Length = 0;
                                        p.DataType = "bool";
                                        break;
                                    case "datetime":
                                        p.Length = 0;
                                        p.DataType = "timestamp";
                                        break;
                                    case "double":
                                        p.Length = 0;
                                        p.DataType = "float8";
                                        break;
                                    case "float":
                                        p.Length = 0;
                                        p.DataType = "float4";
                                        break;
                                }
                            }
                        },
                        EntityNameService = (x, p) => //处理表名
                        {
                            p.DbTableName = UtilMethods.ToUnderLine(p.DbTableName);//驼峰转下划线方法
                        }
                    }
                }, db =>
                {
                    // 租户隔离：查询过滤 + 插入回填（覆盖直接使用 Db 做联表等自定义查询的路径）
                    TenantIsolation.Attach(db);

                    db.Aop.OnLogExecuted = (sql, pars) =>
                    {
                        // 直接写入 SqlSugarHelper 的 AsyncLocal，DbContext<T>.sqlSugar 读同一个值
                        SqlSugarHelper.SqlSugar = SugarSqlFormat.FormatParam(sql, pars);
                        SqlSugarHelper.SqlError = "";
                    };

                    db.Aop.OnError = (exp) =>
                    {
                        // 直接写入 SqlSugarHelper 的 AsyncLocal，DbContext<T>.sqlError 读同一个值
                        SqlSugarHelper.SqlSugar = "";
                        SqlSugarHelper.SqlError = SugarSqlFormat.FormatParam(exp.Sql, exp.Parametres);

                        // 检测连接异常并调用回调
                        if (OnConnectionError != null && IsConnectionError(exp))
                        {
                            OnConnectionError(exp);
                        }
                    };
                });
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 判断是否为连接相关的异常。
        /// 不依赖任何厂商驱动类型名（如 MySqlException/NpgsqlException），
        /// 而是递归检测 InnerException 中是否为 ADO.NET 通用基类 DbException，
        /// 再叠加跨厂商通用的连接错误关键词，确保 MySQL/TiDB/PostgreSQL/SQLite 全部适用。
        /// </summary>
        private static bool IsConnectionError(SqlSugarException exp)
        {
            if (exp == null) return false;

            // 1. 递归扫描异常链，命中 ADO.NET 通用数据库异常基类即视为连接相关
            //    （MySqlException/NpgsqlException/SqliteException 等均派生自 System.Data.Common.DbException）
            for (Exception inner = exp.InnerException; inner != null; inner = inner.InnerException)
            {
                if (inner is System.Data.Common.DbException) return true;
            }

            // 2. 叠加跨厂商通用连接错误关键词（含 PostgreSQL/MySQL/SQLite 常见措辞）
            var message = (exp.Message + " " + (exp.InnerException?.Message ?? "")).ToLower();
            return message.Contains("connection") ||
                   message.Contains("network") ||
                   message.Contains("timeout") ||
                   message.Contains("unable to connect") ||
                   message.Contains("lost connection") ||
                   message.Contains("broken pipe") ||
                   message.Contains("connection refused") ||   // PostgreSQL/通用
                   message.Contains("server closed") ||         // PostgreSQL 服务端关闭连接
                   message.Contains("server has gone away") ||  // MySQL
                   message.Contains("connection reset") ||
                   message.Contains("connection is closed") ||
                   message.Contains("no connection") ||
                   message.Contains("database is locked") ||    // SQLite
                   message.Contains("disk i/o error") ||        // SQLite
                   message.Contains("连接") ||
                   message.Contains("超时") ||
                   message.Contains("网络");
        }
    }

    /// <summary>
    /// 数据库运行时重连辅助类
    /// </summary>
    internal static class DbReconnectHelper
    {
        /// <summary>
        /// 数据库连接状态包装器
        /// </summary>
        internal class DbConnectionState
        {
            public SqlSugarScope DbInstance;
            public bool IsReconnecting;
            public readonly object LockObj;
            public readonly Func<SqlSugarScope> RecreateAction;
            public readonly string DbName;

            public DbConnectionState(string dbName, object lockObj, Func<SqlSugarScope> recreateAction)
            {
                DbName = dbName;
                LockObj = lockObj;
                RecreateAction = recreateAction;
            }
        }

        /// <summary>
        /// 创建带有运行时重连功能的数据库连接
        /// </summary>
        public static SqlSugarScope CreateWithReconnect(
            string connectionString,
            DbType dbType,
            DbConnectionState state)
        {
            SqlSugar_Db sqldb = new SqlSugar_Db(
                connectionString,
                dbType,
                onConnectionError: (exp) =>
                {
                    if (!state.IsReconnecting)
                    {
                        LogHelper.ErrorLogWrite("DbReconnectHelper", "OnConnectionError", $"[{state.DbName}] 检测到数据库连接异常: {exp.Message}，正在尝试重建连接...", "数据库连接");
                        TryReconnect(state);
                    }
                });

            return sqldb.Db;
        }

        /// <summary>
        /// 尝试重新连接
        /// </summary>
        private static void TryReconnect(DbConnectionState state)
        {
            lock (state.LockObj)
            {
                if (state.IsReconnecting) return; // 防止并发重连
                state.IsReconnecting = true;

                try
                {
                    LogHelper.SysLogWrite("DbReconnectHelper", "TryReconnect", $"[{state.DbName}] 开始重建数据库连接...", "数据库连接");
                    state.DbInstance?.Dispose(); // 释放旧连接
                    state.DbInstance = null;
                    state.DbInstance = state.RecreateAction(); // 重新创建

                    if (state.DbInstance != null)
                    {
                        // 测试新连接是否可用
                        state.DbInstance.Ado.ExecuteCommand("SELECT 1");
                        LogHelper.SysLogWrite("DbReconnectHelper", "TryReconnect", $"[{state.DbName}] 数据库连接重建成功", "数据库连接");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.ErrorLogWrite("DbReconnectHelper", "TryReconnect", $"[{state.DbName}] 数据库连接重建失败: {ex.Message}", "数据库连接");
                }
                finally
                {
                    state.IsReconnecting = false;
                }
            }
        }
    }

    /// <summary>
    /// 分表的库
    /// </summary>
    public class SqlSugar_Split
    {
        public static DbType _DbType = Enum.TryParse<DbType>(DbSetting.Current.DbTypeName, true, out var dbtype) ? dbtype : DbType.Tidb;
        private static readonly object _lock = new object();
        private static DbReconnectHelper.DbConnectionState _state;

        private static SqlSugarScope _Db = null;
        public static SqlSugarScope Db
        {
            get
            {
                if (_Db == null)
                {
                    lock (_lock)
                    {
                        if (_Db == null)
                        {
                            _state = new DbReconnectHelper.DbConnectionState("SqlSugar_Split", _lock, CreateDatabase);
                            _Db = CreateDatabase();
                            _state.DbInstance = _Db;
                        }
                    }
                }
                return _Db;
            }
        }

        private static SqlSugarScope CreateDatabase()
        {
            var db = DbReconnectHelper.CreateWithReconnect(
                DbSetting.Current.MysqlSplitConString,
                _DbType,
                _state);

            _Db = db;
            if (_state != null)
            {
                _state.DbInstance = db;
            }
            return db;
        }

        /// <summary>
        /// 库是否存在
        /// </summary>
        public static bool IsDatabase = false;
    }

    /// <summary>
    /// 不分表的库
    /// </summary>
    public class SqlSugar_Custom
    {
        public static DbType _DbType = Enum.TryParse<DbType>(DbSetting.Current.DbTypeName, true, out var dbtype) ? dbtype : DbType.Tidb;
        private static readonly object _lock = new object();
        private static DbReconnectHelper.DbConnectionState _state;

        private static SqlSugarScope _Db = null;
        public static SqlSugarScope Db
        {
            get
            {
                if (_Db == null)
                {
                    lock (_lock)
                    {
                        if (_Db == null)
                        {
                            _state = new DbReconnectHelper.DbConnectionState("SqlSugar_Custom", _lock, CreateDatabase);
                            _Db = CreateDatabase();
                            _state.DbInstance = _Db;
                        }
                    }
                }
                return _Db;
            }
        }

        private static SqlSugarScope CreateDatabase()
        {
            var db = DbReconnectHelper.CreateWithReconnect(
                DbSetting.Current.MysqlConString,
                _DbType,
                _state);

            _Db = db;
            if (_state != null)
            {
                _state.DbInstance = db;
            }
            return db;
        }

        /// <summary>
        /// 库是否存在
        /// </summary>
        public static bool IsDatabase = false;
    }

    /// <summary>
    /// Sqlite库
    /// </summary>
    public class SqlSugar_Sqlite
    {
        public static DbType _DbType = DbType.Sqlite;
        private static readonly object _lock = new object();
        private static DbReconnectHelper.DbConnectionState _state;
        private static SqlSugarScope _Db = null;

        public static SqlSugarScope Db
        {
            get
            {
                lock (_lock)
                {
                    if (_Db == null && !DbSetting.Current.SqliteConString.IsZxxNullOrEmpty())
                    {
                        _state = new DbReconnectHelper.DbConnectionState("SqlSugar_Sqlite", _lock, CreateDatabase);
                        _Db = CreateDatabase();
                        _state.DbInstance = _Db;
                    }
                }
                return _Db;
            }
        }

        private static SqlSugarScope CreateDatabase()
        {
            var partArry = DbSetting.Current.SqliteConString.Split(';');
            string xdpath = partArry[0].Replace("DataSource=", "");
            string sqlitePath = Path.Combine(AppContext.BaseDirectory, xdpath);

            if (!File.Exists(sqlitePath))
            {
                return null;
            }

            var db = DbReconnectHelper.CreateWithReconnect(
                $"DataSource={sqlitePath}",
                _DbType,
                _state);

            _Db = db;
            if (_state != null)
            {
                _state.DbInstance = db;
            }
            return db;
        }

        /// <summary>
        /// 库是否存在
        /// </summary>
        public static bool IsDatabase = false;
    }


    /// <summary>
    /// 数据库连接健康检查，启动时等待数据库就绪
    /// </summary>
    public static class DatabaseHealthCheck
    {
        /// <summary>
        /// 阻塞等待数据库连接成功，失败时每隔 retryIntervalSeconds 秒重试，不会抛出异常
        /// </summary>
        /// <param name="retryIntervalSeconds">重试间隔（秒），默认30秒</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async Task WaitForConnectionAsync(int retryIntervalSeconds = 30, CancellationToken cancellationToken = default)
        {
            var connections = new List<(string Name, string ConnStr, DbType DbType)>
            {
                ("主库(Custom)", DbSetting.Current.MysqlConString, SqlSugar_Custom._DbType),
                //("分表库(Split)", DbSetting.Current.MysqlSplitConString, SqlSugar_Split._DbType),
            };

            foreach (var (name, connStr, dbType) in connections)
            {
                if (string.IsNullOrEmpty(connStr)) continue;
                await WaitForSingleConnectionAsync(name, connStr, dbType, retryIntervalSeconds, cancellationToken);
            }
        }

        private static async Task WaitForSingleConnectionAsync(string name, string connStr, DbType dbType, int retryIntervalSeconds, CancellationToken cancellationToken)
        {
            int attempt = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                attempt++;
                try
                {
                    // 使用 SqlSugar 创建临时连接，通过 DbType 自动适配不同数据库驱动，实现通用检测
                    using var db = new SqlSugarScope(new ConnectionConfig()
                    {
                        ConnectionString = connStr,
                        DbType = dbType,
                        IsAutoCloseConnection = true,
                    });
                    await db.Ado.GetScalarAsync("SELECT 1");
                    LogHelper.SysLogWrite("DatabaseHealthCheck", "WaitForConnectionAsync", $"[DatabaseHealthCheck] {name} 连接成功 (第{attempt}次尝试)", "数据库连接");
                    return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    LogHelper.ErrorLogWrite("DatabaseHealthCheck", "WaitForConnectionAsync", $"[DatabaseHealthCheck] {name} 连接失败 (第{attempt}次尝试): {ex.Message}，{retryIntervalSeconds}秒后重试...", "数据库连接");
                    try
                    {
                        await Task.Delay(retryIntervalSeconds * 1000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Redis缓存库
    /// </summary>
    public class RedisHelper
    {
        private static IDatabase _redisDb = null;
        private static readonly object _lock = new object();
        private static DateTime _lastFailTime = DateTime.MinValue;
        private static readonly TimeSpan _failureCooldown = TimeSpan.FromSeconds(30);
        private static int _consecutiveFailures = 0;
        private static readonly int _maxConsecutiveFailures = 3;
        public static readonly int _CacheSeconds = 3600 * 1;

        public static IDatabase RedisService
        {
            get
            {
                // 1. 熔断检查
                if (_consecutiveFailures >= _maxConsecutiveFailures)
                {
                    var timeSinceLastFail = DateTime.Now - _lastFailTime;
                    if (timeSinceLastFail < _failureCooldown)
                    {
                        return null; // 熔断中，直接返回
                    }
                    else
                    {
                        _consecutiveFailures = 0; // 冷却期过后，重置计数
                    }
                }

                if (_redisDb == null)
                {
                    lock (_lock)
                    {
                        if (_redisDb == null)
                        {
                            try
                            {
                                _redisDb = RedisConnectionManager.GetDatabase();
                                if (_redisDb != null)
                                {
                                    _consecutiveFailures = 0; // 成功，重置失败计数
                                }
                                else
                                {
                                    _consecutiveFailures++;
                                    _lastFailTime = DateTime.Now;
                                }
                            }
                            catch (Exception ex)
                            {
                                _consecutiveFailures++;
                                _lastFailTime = DateTime.Now;
                                LogHelper.ErrorLogWrite("DbContext", "Execute", ex.ToString(), "数据访问");
                            }
                        }
                    }
                }
                return _redisDb;
            }
        }
    }

    /// <summary>
    /// 数据库操作帮助类
    /// </summary>
    public class SqlSugarHelper
    {
        // 用 AsyncLocal 存储最近执行的 SQL 和错误，保证并发请求互不干扰
        // OnLogExecuted/OnError 回调写入，DbContext<T>.sqlSugar 属性读取
        private static readonly AsyncLocal<string> _lastSql = new AsyncLocal<string>();
        private static readonly AsyncLocal<string> _lastError = new AsyncLocal<string>();
        public static string SqlSugar
        {
            get => _lastSql.Value ?? "";
            set => _lastSql.Value = value;
        }
        public static string SqlError
        {
            get => _lastError.Value ?? "";
            set => _lastError.Value = value;
        }

        private static Dictionary<string, ConditionalType> _diccontype = new Dictionary<string, ConditionalType>();
        /// <summary>
        /// 查询条件操作符
        /// </summary>
        public static Dictionary<string, ConditionalType> diccontype
        {
            get
            {
                lock (_diccontype)
                {
                    if (_diccontype.Count == 0)
                    {
                        _diccontype.Add("=", ConditionalType.Equal);
                        _diccontype.Add("!=", ConditionalType.NoEqual);
                        _diccontype.Add(">", ConditionalType.GreaterThan);
                        _diccontype.Add(">=", ConditionalType.GreaterThanOrEqual);
                        _diccontype.Add("<", ConditionalType.LessThan);
                        _diccontype.Add("<=", ConditionalType.LessThanOrEqual);
                        _diccontype.Add("in", ConditionalType.In);
                        _diccontype.Add("notin", ConditionalType.NotIn);
                        _diccontype.Add("like", ConditionalType.Like);
                        _diccontype.Add("nolike", ConditionalType.NoLike);
                        _diccontype.Add("isnull", ConditionalType.IsNullOrEmpty);
                    }
                }
                return _diccontype;
            }
        }

        private static Dictionary<string, WhereType> _wheretype = new Dictionary<string, WhereType>();
        /// <summary>
        /// 查询条件操作符
        /// </summary>
        public static Dictionary<string, WhereType> wheretype
        {
            get
            {
                lock (_wheretype)
                {
                    if (_wheretype.Count == 0)
                    {
                        _wheretype.Add("and", WhereType.And);
                        _wheretype.Add("or", WhereType.Or);
                        _wheretype.Add("null", WhereType.Null);
                    }
                }
                return _wheretype;
            }
        }

        public static List<string> TableAll = new List<string>();

        private static Dictionary<int, string> _ObjLevel = new Dictionary<int, string>();
        public static Dictionary<int, string> ObjLevel
        {
            get
            {
                lock (_ObjLevel)
                {
                    if (_ObjLevel.Count == 0)
                    {
                        _ObjLevel.Add(1, "A");
                        _ObjLevel.Add(2, "B");
                        _ObjLevel.Add(3, "C");
                        _ObjLevel.Add(4, "D");
                        _ObjLevel.Add(5, "E");
                        _ObjLevel.Add(6, "F");
                        _ObjLevel.Add(7, "G");
                        _ObjLevel.Add(8, "H");
                        _ObjLevel.Add(9, "I");
                        _ObjLevel.Add(10, "J");
                    }
                }
                return _ObjLevel;
            }
        }
    }

    /// <summary>
    /// 数据库操作基础类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DbContext<T> where T : class, new()
    {
        public string sqlSugar
        {
            get
            {
                return SqlSugarHelper.SqlSugar;
            }
        }
        public string sqlError
        {
            get
            {
                return SqlSugarHelper.SqlError;
            }
        }
        public SqlSugarScope Db
        {
            get
            {
                if (IsSplitTable) return SqlSugar_Split.Db;
                if (IsSqliteTable) return SqlSugar_Sqlite.Db;
                return SqlSugar_Custom.Db;
            }
        }
        public bool IsDatabase
        {
            get
            {
                if (IsSplitTable) return SqlSugar_Split.IsDatabase;
                return SqlSugar_Custom.IsDatabase;
            }
        }
        private DbType DbType
        {
            get
            {
                if (IsSplitTable) return SqlSugar_Split._DbType;
                return SqlSugar_Custom._DbType;
            }
        }
        private Dictionary<string, ConditionalType> diccontype = SqlSugarHelper.diccontype;

        public Dictionary<int, string> ObjLevel
        {
            get
            {
                return SqlSugarHelper.ObjLevel;
            }
        }

        /// <summary>
        /// 是否反向工程
        /// </summary>
        private bool IsMigration = false;
        /// <summary>
        ///  是否分表 
        /// </summary>
        private bool IsSplitTable = false;
        /// <summary>
        ///  是否Sqlite 
        /// </summary>
        private bool IsSqliteTable = false;

        // 缓存相关
        private bool IsUseRedisCache = false;
        private readonly string typename = typeof(T).Name;

        // T 是否租户隔离实体(ITenantEntity)：缓存出口内存过滤的判定依据
        private static readonly bool IsTenantEntity = typeof(ITenantEntity).IsAssignableFrom(typeof(T));

        //数据验证
        private readonly EntityValidator<T> _validator = new EntityValidator<T>();

        // 事务期间的专用连接（CopyNew 得到的实例），非事务时为 null。
        // 使用 AsyncLocal 确保每个 async 执行上下文独立持有自己的事务连接，
        // 避免单例 DAO 上并发调用者共享同一 _transactionDb 而引发 MySqlConnection reuse 错误。
        private readonly AsyncLocal<ISqlSugarClient> _transactionDb = new AsyncLocal<ISqlSugarClient>();

        /// <summary>
        /// 常规数据操作入口。
        /// 事务期间返回当前 async 上下文的 CopyNew 实例（确保事务一致性）；
        /// 其他时候每次返回新建的 CopyNew 实例（避免并发 MySqlConnection 重入）。
        /// 参考 https://www.donet5.com/Home/Doc?typeId=1224 排查3：定时任务/BackgroundService/Task.WhenAll 场景必须使用 CopyNew。
        /// CopyNew 不会继承 SqlSugarScope 上的 AOP 钩子，需要手动挂载 OnLogExecuted/OnError，
        /// 否则 sqlSugar/sqlError 属性无法捕获到实际执行的 SQL。
        /// </summary>
        private ISqlSugarClient GetOperDb()
        {
            if (_transactionDb.Value != null) return _transactionDb.Value;

            var db = Db.CopyNew();
            db.Aop.OnLogExecuted = (sql, pars) =>
            {
                SqlSugarHelper.SqlError = "";
                SqlSugarHelper.SqlSugar = SugarSqlFormat.FormatParam(sql, pars);
            };
            db.Aop.OnError = (exp) =>
            {
                SqlSugarHelper.SqlSugar = "";
                SqlSugarHelper.SqlError = SugarSqlFormat.FormatParam(exp.Sql, exp.Parametres);
            };
            // CopyNew 同样不继承过滤器：租户隔离逐实例挂载
            TenantIsolation.Attach(db);
            return db;
        }

        private IDatabase RedisService
        {
            get
            {
                if (IsUseRedisCache) return RedisHelper.RedisService;
                return null;
            }
        }

        // 记录每个类型的上次验证时间，避免频繁验证
        private static readonly Dictionary<string, DateTime> _lastValidationTimes = new Dictionary<string, DateTime>();
        // 验证间隔时间(10分钟)
        private static readonly TimeSpan _validationInterval = TimeSpan.FromMinutes(10);

        private readonly object _fullEntityContext;

        #region 构造函数

        public DbContext(object fullEntityContext = null)
        {
            _fullEntityContext = fullEntityContext;
            if (!IsMigration)
            {
                var attrs = typeof(T).GetCustomAttributes();
                if (attrs.IsZxxAny())
                {
                    string tablename = "";
                    var attr = attrs.FirstOrDefault(t => t.TypeId.ToString().ToLower().Contains("sugartable"));
                    if (attr != null)
                    {
                        tablename = (attr as SugarTable).TableName;
                    }
                    if (attrs.Any(t => t.TypeId.ToString().ToLower().Contains("splittable"))) IsSplitTable = true;
                    if (attrs.Any(t => t.TypeId.ToString().ToLower().Contains("entitycache"))) IsUseRedisCache = true;
                    if (attrs.Any(t => t.TypeId.ToString().ToLower().Contains("dbsqlite"))) IsSqliteTable = true;
                    if (DbSetting.Current.Migration)
                    {
                        DeleteFromRedis().Wait();
                        if (!IsDatabase && Db != null)
                        {
                            StaticConfig.CodeFirst_MySqlCollate = "utf8mb4_general_ci";
                            Db.DbMaintenance.CreateDatabase();
                            if (IsSplitTable) SqlSugar_Split.IsDatabase = true;
                            else SqlSugar_Custom.IsDatabase = true;

                            List<string> names = new List<string>();
                            var tables = Db.DbMaintenance.GetTableInfoList(false);//true 走缓存 false不走缓存
                            foreach (var table in tables)
                            {
                                lock (SqlSugarHelper.TableAll)
                                {
                                    if (!SqlSugarHelper.TableAll.Contains(table.Name))
                                        SqlSugarHelper.TableAll.Add(table.Name);
                                }
                            }
                        }
                        lock (SqlSugarHelper.TableAll)
                        {
                            if (!SqlSugarHelper.TableAll.Contains(tablename))
                            {
                                if (!IsSplitTable)
                                {
                                    Db.CodeFirst.InitTables<T>();
                                    Init();
                                }
                                else
                                {
                                    Db.CodeFirst.SplitTables().InitTables<T>();
                                }
                                IsMigration = true;
                                SqlSugarHelper.TableAll.Add(tablename);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Init()
        {
            // 调用FullEntityContext的Init方法
            if (_fullEntityContext != null)
            {
                var initMethod = _fullEntityContext.GetType().GetMethod("Init", new[] { typeof(object[]) });
                if (initMethod != null)
                {
                    initMethod.Invoke(_fullEntityContext, new object[] { new object[] { this } });
                }
            }
        }

        /// <summary>
        /// 种子期显式主键直插：OffIdentity 写入固定 ID 后同步 PG 自增序列。
        /// PG 的 serial 序列不因显式插值推进，不同步则运行期首次自增插入撞种子主键；
        /// MySQL/TiDB 显式插值自动推进 AUTO_INCREMENT，无需处理。
        /// </summary>
        protected void SeedOffIdentity(List<T> list)
        {
            Db.Insertable(list).OffIdentity().ExecuteCommand();
            if (DbType == DbType.PostgreSQL)
            {
                var entityinfo = Db.EntityMaintenance.GetEntityInfo<T>();
                var idcol = entityinfo.Columns.FirstOrDefault(t => t.IsIdentity);
                if (idcol != null)
                {
                    Db.Ado.ExecuteCommand(
                        $"SELECT setval(pg_get_serial_sequence('{entityinfo.DbTableName}', '{idcol.DbColumnName}'), (SELECT MAX(\"{idcol.DbColumnName}\") FROM \"{entityinfo.DbTableName}\"))");
                }
            }
        }

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
                            var names = cusa.NamedArguments.ToList();
                            if (names.Any(t => t.MemberName == "ColumnName"))
                                column.FieldName = names.Find(t => t.MemberName == "ColumnName").TypedValue.Value.ToString();
                            if (string.IsNullOrWhiteSpace(column.FieldName))
                                column.FieldName = column.ParamName;
                            if (fi.PropertyType.Name.ToLower().Contains("date")) column.IsTime = true;
                            if (names.Any(t => t.MemberName == "IsPrimaryKey")) column.IsPrimaryKey = true;
                            list.Add(column);
                        }
                    }
                }
            }

            return list;
        }

        public Tuple<List<IConditionalModel>, string> GetSqlModel(ActionPara model)
        {
            string orderby = string.Empty;
            List<IConditionalModel> conModel = new();
            var fieldlist = GetFieldNames();
            // 处理排序条件
            orderby = GetOrderByCondition(model.sconlist, fieldlist);

            // 处理查询条件
            if (model.sconlist.Count > 0)
            {
                // 按 ParamGroupName 分组处理条件
                var groupedConditions = model.sconlist
                    .FindAll(s => s.ParamSort == 0)
                    .GroupBy(c => c.ParamGroupName ?? string.Empty);

                foreach (var group in groupedConditions)
                {
                    var groupConditions = new List<IConditionalModel>();

                    foreach (var condition in group)
                    {
                        var fieldName = GetFieldName(condition.ParamName, fieldlist);
                        if (string.IsNullOrEmpty(fieldName)) continue;

                        var conditionalModel = CreateConditionalModel(condition, fieldName);
                        if (conditionalModel != null)
                        {
                            groupConditions.Add(conditionalModel);
                        }
                    }

                    if (groupConditions.Any())
                    {
                        // 无论是否有分组名，都需要考虑 GroupCondition 字段
                        if (groupConditions.Count == 1)
                        {
                            // 只有一个条件时，直接添加（不需要考虑内部连接逻辑）
                            conModel.AddRange(groupConditions);
                        }
                        else
                        {
                            // 多个条件时，需要根据 GroupCondition 字段处理内部连接逻辑
                            var conditionalCollection = new ConditionalCollections
                            {
                                ConditionalList = CreateGroupConditionList(group.ToList(), groupConditions)
                            };
                            conModel.Add(conditionalCollection);
                        }
                    }
                }
            }
            // 如果主键是 SnowId 且提供了时间范围，添加时间范围查询
            var idProperty = fieldlist.Find(f => f.IsPrimaryKey && f.ParamName.ToLower() == "snowid");
            if (idProperty != null)
            {
                // 处理开始时间f
                if (!string.IsNullOrEmpty(model.starttime))
                {
                    var startTime = model.starttime.ToZxxDateTime();
                    conModel.Add(new ConditionalModel
                    {
                        FieldName = idProperty.FieldName,
                        ConditionalType = ConditionalType.GreaterThanOrEqual,
                        FieldValue = SnowModel.Instance.GetId(startTime).ToString()
                    });
                }

                // 处理结束时间
                if (!string.IsNullOrEmpty(model.endtime))
                {
                    var endTime = model.endtime.ToZxxDateTime();
                    conModel.Add(new ConditionalModel
                    {
                        FieldName = idProperty.FieldName,
                        ConditionalType = ConditionalType.LessThan,
                        FieldValue = SnowModel.Instance.GetId(endTime).ToString()
                    });
                }
            }
            else
            {
                var createTime = fieldlist.Find(f => f.ParamName.ToLower() == "createtime");
                if (createTime != null)
                {                     // 处理开始时间
                    if (!string.IsNullOrEmpty(model.starttime))
                    {
                        conModel.Add(new ConditionalModel
                        {
                            FieldName = createTime.FieldName,
                            ConditionalType = ConditionalType.GreaterThanOrEqual,
                            FieldValue = model.starttime
                        });
                    }

                    // 处理结束时间
                    if (!string.IsNullOrEmpty(model.endtime))
                    {
                        conModel.Add(new ConditionalModel
                        {
                            FieldName = createTime.FieldName,
                            ConditionalType = ConditionalType.LessThan,
                            FieldValue = model.endtime
                        });
                    }
                }
            }

            return new Tuple<List<IConditionalModel>, string>(conModel, orderby);
        }

        /// <summary>
        /// 获取排序条件
        /// </summary>
        private string GetOrderByCondition(List<SelectCondition> sconlist, List<TableColumn> fieldlist)
        {
            var sortList = sconlist.FindAll(t => t.ParamSort > 0);
            if (sortList.Any())
            {
                return GetSqlOrderByCondition(sortList, fieldlist);
            }

            // 默认根据主键倒序排序
            var primaryKey = fieldlist.FindAll(k => k.IsPrimaryKey && !string.IsNullOrWhiteSpace(k.FieldName));
            if (primaryKey.Any())
            {
                return string.Join(",", primaryKey.Select(f => $"{f.FieldName} desc"));
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取Order by语句
        /// </summary>
        /// <param name="sconlist"></param>
        /// <returns></returns>
        private string GetSqlOrderByCondition(List<SelectCondition> sconlist, List<TableColumn> fieldlist)
        {
            List<string> sqllist = new();
            try
            {
                sconlist.FindAll(t => t.ParamSort > 0).ForEach(scn =>
                {
                    var fieldName = GetFieldName(scn.ParamName, fieldlist);
                    if (!string.IsNullOrEmpty(fieldName))
                    {
                        if (scn.ParamSort == 1)
                        {
                            sqllist.Add($"{fieldName} asc");
                        }
                        else if (scn.ParamSort == 2)
                        {
                            sqllist.Add($"{fieldName} desc");
                        }
                    }
                });
            }
            catch (Exception)
            {
                throw;
            }
            return string.Join(" , ", sqllist);
        }

        /// <summary>
        /// 获取字段名
        /// </summary>
        private string GetFieldName(string paramName, List<TableColumn> fieldlist)
        {
            var field = fieldlist.Find(f => f.ParamName.ToLower() == paramName.ToLower());
            return field?.FieldName ?? paramName;
        }

        /// <summary>
        /// 创建分组条件列表，处理分组内部的 AND/OR 逻辑
        /// </summary>
        private List<KeyValuePair<WhereType, ConditionalModel>> CreateGroupConditionList(
            List<SelectCondition> originalConditions,
            List<IConditionalModel> groupConditions)
        {
            var result = new List<KeyValuePair<WhereType, ConditionalModel>>();

            for (int i = 0; i < groupConditions.Count; i++)
            {
                var conditionalModel = (ConditionalModel)groupConditions[i];
                WhereType whereType = WhereType.And; // 默认使用 AND

                if (i == 0)
                {
                    // 第一个条件通常不需要连接符，但这里我们还是需要指定
                    whereType = WhereType.And;
                }
                else if (i < originalConditions.Count)
                {
                    // 获取原始条件的 wheretype 字段
                    var originalCondition = originalConditions[i];
                    if (!string.IsNullOrEmpty(originalCondition.GroupCondition))
                    {
                        var whereTypeKey = originalCondition.GroupCondition.ToLower();
                        if (SqlSugarHelper.wheretype.TryGetValue(whereTypeKey, out WhereType parsedWhereType))
                        {
                            whereType = parsedWhereType;
                        }
                    }
                }

                result.Add(new KeyValuePair<WhereType, ConditionalModel>(whereType, conditionalModel));
            }

            return result;
        }

        /// <summary>
        /// 创建条件模型
        /// </summary>
        private IConditionalModel CreateConditionalModel(SelectCondition condition, string fieldName)
        {
            if (string.IsNullOrEmpty(condition.ParamValue)) return null;

            var type = condition.ParamType?.ToLower();
            if (string.IsNullOrEmpty(type)) return null;

            switch (type)
            {
                case "=":
                    return new ConditionalModel
                    {
                        FieldName = fieldName,
                        ConditionalType = ConditionalType.Equal,
                        FieldValue = condition.ParamValue
                    };
                case "!=":
                    return new ConditionalModel
                    {
                        FieldName = fieldName,
                        ConditionalType = ConditionalType.NoEqual,
                        FieldValue = condition.ParamValue
                    };
                case "like":
                    return new ConditionalModel
                    {
                        FieldName = fieldName,
                        ConditionalType = ConditionalType.Like,
                        FieldValue = condition.ParamValue
                    };
                case "nolike":
                    return new ConditionalModel
                    {
                        FieldName = fieldName,
                        ConditionalType = ConditionalType.NoLike,
                        FieldValue = condition.ParamValue
                    };
                case "in":
                    return new ConditionalModel
                    {
                        FieldName = fieldName,
                        ConditionalType = ConditionalType.In,
                        FieldValue = condition.ParamValue.Trim('\'')
                    };
                case "notin":
                    return new ConditionalModel
                    {
                        FieldName = fieldName,
                        ConditionalType = ConditionalType.NotIn,
                        FieldValue = condition.ParamValue.Trim('\'')
                    };
                case ">":
                    return new ConditionalModel
                    {
                        FieldName = fieldName,
                        ConditionalType = ConditionalType.GreaterThan,
                        FieldValue = condition.ParamValue
                    };
                case ">=":
                    return new ConditionalModel
                    {
                        FieldName = fieldName,
                        ConditionalType = ConditionalType.GreaterThanOrEqual,
                        FieldValue = condition.ParamValue
                    };
                case "<":
                    return new ConditionalModel
                    {
                        FieldName = fieldName,
                        ConditionalType = ConditionalType.LessThan,
                        FieldValue = condition.ParamValue
                    };
                case "<=":
                    return new ConditionalModel
                    {
                        FieldName = fieldName,
                        ConditionalType = ConditionalType.LessThanOrEqual,
                        FieldValue = condition.ParamValue
                    };
                case "isnull":
                    return new ConditionalModel
                    {
                        FieldName = fieldName,
                        ConditionalType = ConditionalType.IsNullOrEmpty,
                        FieldValue = null
                    };
                default:
                    // 如果在 diccontype 中找到对应的类型，使用它
                    if (diccontype.TryGetValue(type, out ConditionalType conditionalType))
                    {
                        return new ConditionalModel
                        {
                            FieldName = fieldName,
                            ConditionalType = conditionalType,
                            FieldValue = condition.ParamValue
                        };
                    }
                    return null;
            }
        }

        /// <summary>
        /// 记录刚执行的 SQL（Info 级）。
        /// 在 DbContext&lt;T&gt; 的每个方法执行 SQL 后调用，
        /// 自动带上 DAO 类名、方法名，TraceId 由 LogContext 自动注入。
        /// 受 LogBootstrap.EnableSqlLog 开关控制（appsettings LogConfig:EnableSqlLog），
        /// 关闭时不写 "SQL执行" 追踪日志以减少日志量；SQL 错误日志不受影响。
        /// </summary>
        private void LogSql(string sql, [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            if (LogBootstrap.EnableSqlLog && !string.IsNullOrEmpty(sql))
            {
                LogHelper.SysLogWrite(typename, method, sql, "SQL执行");
            }
        }

        /// <summary>
        /// 记录 SQL 执行异常（Error 级），在 catch 块中调用。
        /// </summary>
        private void LogSqlError(Exception ex, [System.Runtime.CompilerServices.CallerMemberName] string method = "")
        {
            LogHelper.ErrorLogWrite(typename, method, $"{sqlError}，异常：{ex}", "SQL错误");
        }

        #endregion

        #region 查询操作

        /// <summary>
        /// 获取单条数据
        /// </summary>
        /// <param name="wheres"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual T GetOneBy(Expression<Func<T, bool>> wheres)
        {
            try
            {
                // 启用缓存时：从 Redis 全表数据内存过滤，不查数据库
                if (IsUseRedisCache)
                {
                    var allList = FilterTenantScope(EnsureCacheLoaded());
                    var compiled = wheres.Compile();
                    return allList.FirstOrDefault(x =>
                    {
                        try { return compiled(x); }
                        catch { return false; }
                    });
                }

                // 从数据库获取
                T result = default;
                if (IsSplitTable)
                {
                    result = GetOperDb().Queryable<T>().SplitTable().Where(wheres).First();
                }
                else
                {
                    result = GetOperDb().Queryable<T>().Where(wheres).First();
                }
                LogSql(sqlSugar);

                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        /// <summary>
        /// 根据sql语句获取data
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual object GetScalar(string sql, object parameters = null)
        {
            try
            {
                var result = GetOperDb().Ado.GetScalar(sql, parameters);
                LogSql(sqlSugar);
                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        /// <summary>
        /// 缓存出口的租户过滤：Redis 持有全表快照，返回调用方前按当前租户上下文过滤，
        /// 与 SQL 侧 QueryFilter 同谓词（tenant_id=0 平台共享 或 落在可见集；超管/无上下文豁免）。
        /// </summary>
        private List<T> FilterTenantScope(List<T> list)
        {
            if (!IsTenantEntity || TenantIsolation.FilterOff || !list.IsZxxAny()) return list;
            var visible = TenantScope.CurrentVisibleTenantIds;
            return list.Where(x =>
            {
                var id = ((ITenantEntity)x).TenantId;
                return id == 0 || visible.Contains(id);
            }).ToList();
        }

        /// <summary>
        /// 确保 Redis 已加载全表数据。缓存空时从数据库查全表写入 Redis。
        /// 所有查询方法（GetOneBy/GetListBy 等）在缓存分支开头调用此方法，
        /// 且必须经 FilterTenantScope 过滤后再交给调用方（缓存内容是全租户快照）。
        /// </summary>
        /// <returns>Redis 中的全表数据（空列表表示无数据或缓存不可用）</returns>
        private List<T> EnsureCacheLoaded()
        {
            var allList = GetListFromRedis().Result;
            if (allList.IsZxxAny()) return allList;

            // 缓存空：从数据库查全表（不带任何 Where 条件）；ClearFilter 绕过租户过滤器，
            // 缓存必须持有全租户快照，否则先到请求的租户会把自己的子集写成"全表"
            List<T> freshData;
            if (IsSplitTable)
            {
                freshData = GetOperDb().Queryable<T>().ClearFilter<ITenantEntity>().SplitTable().ToList();
            }
            else
            {
                freshData = GetOperDb().Queryable<T>().ClearFilter<ITenantEntity>().ToList();
            }
            LogSql(sqlSugar);

            // 全表写入 Redis（直接覆盖，不合并）
            if (freshData.IsZxxAny() && RedisService != null)
            {
                try
                {
                    RedisService.StringSetAsync(typename, freshData.ToJson(),
                        TimeSpan.FromSeconds(RedisHelper._CacheSeconds)).Wait();
                }
                catch (Exception ex)
                {
                    LogSqlError(ex);
                }
            }
            return freshData;
        }

        /// <summary>
        /// 获取所有
        /// </summary>
        /// <returns></returns>
        public virtual List<T> GetList()
        {
            try
            {
                // 启用缓存时：EnsureCacheLoaded 已确保全表加载，出口按租户过滤后返回
                if (IsUseRedisCache)
                {
                    return FilterTenantScope(EnsureCacheLoaded());
                }

                // 从数据库获取
                List<T> result = default;
                if (IsSplitTable)
                {
                    result = GetOperDb().Queryable<T>().SplitTable().ToList();
                }
                else
                {
                    result = GetOperDb().Queryable<T>().ToList();
                }
                LogSql(sqlSugar);

                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        /// <summary>
        /// 根据条件获取列表
        /// </summary>
        /// <param name="wheres"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual List<T> GetListBy(Expression<Func<T, bool>> wheres)
        {
            try
            {
                // 启用缓存时：从 Redis 全表数据内存过滤，不查数据库
                if (IsUseRedisCache)
                {
                    var allList = FilterTenantScope(EnsureCacheLoaded());
                    var predicate = wheres.Compile();
                    return allList.Where(x =>
                    {
                        try { return predicate(x); }
                        catch { return false; }
                    }).ToList();
                }

                // 从数据库获取
                List<T> result = default;
                if (IsSplitTable)
                {
                    result = GetOperDb().Queryable<T>().SplitTable().Where(wheres).ToList();
                }
                else
                {
                    result = GetOperDb().Queryable<T>().Where(wheres).ToList();
                }
                LogSql(sqlSugar);

                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        /// <summary>
        /// 根据参数获取列表(不分页)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual List<T> GetListBy(ActionPara model)
        {
            try
            {
                model.page = 0;
                model.pagesize = 0;
                // 启用缓存时：从 Redis 全表数据内存过滤，不查数据库
                if (IsUseRedisCache)
                {
                    var allList = FilterTenantScope(EnsureCacheLoaded());
                    try
                    {
                        return model.GetListBy(allList);
                    }
                    catch (Exception cacheEx)
                    {
                        LogHelper.ErrorLogWrite("DbContext", "GetListBy", cacheEx.ToString(), "缓存操作");
                    }
                }

                var sqlmodel = GetSqlModel(model);
                List<T> list;
                if (IsSplitTable)
                {
                    list = GetOperDb().Queryable<T>()
                            .SplitTable()
                            .Where(sqlmodel.Item1)
                            .OrderByIF(!string.IsNullOrWhiteSpace(sqlmodel.Item2), sqlmodel.Item2)
                            .ToList();
                }
                else
                {
                    list = GetOperDb().Queryable<T>()
                            .Where(sqlmodel.Item1)
                            .OrderByIF(!string.IsNullOrWhiteSpace(sqlmodel.Item2), sqlmodel.Item2)
                            .ToList();
                }
                LogSql(sqlSugar);

                return list;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        /// <summary>
        /// 根据封装的条件查询分页数据
        /// </summary>
        /// <param name="model"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual List<T> GetListByPage(ActionPara model, ref int total)
        {
            try
            {
                List<T> list = new List<T>();
                // 启用缓存时：从 Redis 全表数据内存分页，不查数据库
                if (IsUseRedisCache)
                {
                    var allList = FilterTenantScope(EnsureCacheLoaded());
                    try
                    {
                        var dataitem = model.GetListByPage(allList);
                        total = dataitem.Item2;
                        return dataitem.Item1;
                    }
                    catch (Exception cacheEx)
                    {
                        LogHelper.ErrorLogWrite("DbContext", "GetListByPage", cacheEx.ToString(), "缓存操作");
                    }
                }

                var sqlmodel = GetSqlModel(model);
                int totalNumber = 0;
                if (IsSplitTable)
                {
                    // SplitTable 必须在 OrderBy 之前调用，否则 SqlSugar 重写 UNION 查询时会丢失排序
                    var _list = GetOperDb().Queryable<T>()
                             .SplitTable()
                             .Where(sqlmodel.Item1)
                             .OrderByIF(!string.IsNullOrWhiteSpace(sqlmodel.Item2), sqlmodel.Item2)
                             .ToPageList(model.page, model.pagesize, ref totalNumber);
                    if (_list.IsZxxAny()) list.AddRange(_list);
                }
                else
                {
                    var _list = GetOperDb().Queryable<T>()
                       .Where(sqlmodel.Item1)
                       .OrderByIF(!string.IsNullOrWhiteSpace(sqlmodel.Item2), sqlmodel.Item2)
                       .ToPageList(model.page, model.pagesize, ref totalNumber);
                    if (_list.IsZxxAny()) list.AddRange(_list);
                }
                total = totalNumber;
                LogSql(sqlSugar);
                return list;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        /// <summary>
        /// 根据表达式查询总数
        /// </summary>
        /// <param name="wheres">表达式</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual int GetListCount(Expression<Func<T, bool>> wheres)
        {
            try
            {
                int totalNumber = 0;

                // 启用缓存时：从 Redis 全表数据内存计数，不查数据库
                if (IsUseRedisCache)
                {
                    var allList = FilterTenantScope(EnsureCacheLoaded());
                    var predicate = wheres.Compile();
                    return allList.Count(x =>
                    {
                        try { return predicate(x); }
                        catch { return false; }
                    });
                }

                if (IsSplitTable)
                {
                    var result = GetOperDb().Queryable<T>().SplitTable().Where(wheres).ToPageList(1, 5, ref totalNumber);
                }
                else
                {
                    var result = GetOperDb().Queryable<T>().Where(wheres).ToPageList(1, 5, ref totalNumber);
                }
                LogSql(sqlSugar);

                return totalNumber;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        /// <summary>
        /// 根据条件获取列表(未实现Redis方法)
        /// </summary>
        /// <param name="conModel"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual List<T> GetListBy(List<IConditionalModel> conModel)
        {
            try
            {
                List<T> result = default;
                if (IsSplitTable)
                {
                    result = GetOperDb().Queryable<T>().SplitTable().Where(conModel).ToList();
                }
                else
                {
                    result = GetOperDb().Queryable<T>().Where(conModel).ToList();
                }
                // 如果启用Redis缓存且数据库有记录，更新Redis缓存
                if (IsUseRedisCache && !result.IsZxxAny())
                {
                    DeleteFromRedis().Wait();
                }

                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        /// <summary>
        /// 根据表达式查询分页(未实现Redis方法)
        /// </summary>
        /// <returns></returns>
        public virtual List<T> GetPageList(Expression<Func<T, bool>> wheres, PageModel page)
        {
            try
            {
                int totalNumber = 0;
                List<T> list = new List<T>();
                if (IsSplitTable)
                {
                    var result = GetOperDb().Queryable<T>().SplitTable().Where(wheres).ToPageList(page.PageIndex, page.PageSize, ref totalNumber);
                    page.TotalCount = totalNumber;
                    list.AddRange(result);
                }
                else
                {
                    var result = GetOperDb().Queryable<T>().Where(wheres).ToPageList(page.PageIndex, page.PageSize, ref totalNumber);
                    page.TotalCount = totalNumber;
                    list.AddRange(result);
                }
                // 如果启用Redis缓存且数据库有记录，更新Redis缓存
                if (IsUseRedisCache && !list.IsZxxAny())
                {
                    DeleteFromRedis().Wait();
                }
                return list;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        /// <summary>
        /// 根据条件获取分页(未实现Redis方法)
        /// </summary>
        /// <param name="conModel"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual List<T> GetPageList(List<IConditionalModel> conModel, PageModel page)
        {
            try
            {
                int totalNumber = 0;
                List<T> list = new List<T>();
                if (IsSplitTable)
                {
                    var result = GetOperDb().Queryable<T>().SplitTable().Where(conModel).ToPageList(page.PageIndex, page.PageSize, ref totalNumber);
                    page.TotalCount = totalNumber;
                    list.AddRange(result);
                }
                else
                {
                    var result = GetOperDb().Queryable<T>().Where(conModel).ToPageList(page.PageIndex, page.PageSize, ref totalNumber);
                    page.TotalCount = totalNumber;
                    list.AddRange(result);
                }
                // 如果启用Redis缓存且数据库有记录，更新Redis缓存
                if (IsUseRedisCache && !list.IsZxxAny())
                {
                    DeleteFromRedis().Wait();
                }
                return list;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        /// <summary>
        /// 根据表达式查询分页并排序(未实现Redis方法)
        /// </summary>
        /// <param name="whereExpression">it</param>
        /// <param name="pageModel"></param>
        /// <param name="orderByExpression">it=>it.id或者it=>new{it.id,it.name}</param>
        /// <param name="orderByType">OrderByType.Desc</param>
        /// <returns></returns>
        public virtual List<T> GetPageList(Expression<Func<T, bool>> wheres, PageModel page, Expression<Func<T, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
        {
            try
            {
                int totalNumber = 0;
                List<T> list = new List<T>();
                if (IsSplitTable)
                {
                    var result = GetOperDb().Queryable<T>().SplitTable()
                    .Where(wheres).OrderByIF(orderByExpression != null, orderByExpression, orderByType)
                    .ToPageList(page.PageIndex, page.PageSize, ref totalNumber);
                    page.TotalCount = totalNumber;
                    list.AddRange(result);
                }
                else
                {
                    var result = GetOperDb().Queryable<T>().OrderByIF(orderByExpression != null, orderByExpression, orderByType)
                    .Where(wheres)
                    .ToPageList(page.PageIndex, page.PageSize, ref totalNumber);
                    page.TotalCount = totalNumber;
                    list.AddRange(result);
                }
                // 如果启用Redis缓存且数据库有记录，更新Redis缓存
                if (IsUseRedisCache && !list.IsZxxAny())
                {
                    DeleteFromRedis().Wait();
                }
                return list;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        #endregion

        #region 更新操作

        /// <summary>
        /// 根据实体更新，实体需要有主键
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual bool Update(T obj)
        {
            return UpdateRange(new List<T> { obj });
        }

        public virtual bool UpdateRange(List<T> updateObjs)
        {
            try
            {
                // 执行数据验证
                foreach (var entity in updateObjs)
                {
                    var validationResult = _validator.Validate(entity);
                    if (!validationResult.IsValid)
                    {
                        throw new ValidationException(string.Join("；", validationResult.Errors.Select(e => e.ErrorMessage)));
                    }
                }
                // 先更新数据库
                bool result;
                if (IsSplitTable)
                {
                    result = GetOperDb().Updateable(updateObjs).SplitTable().ExecuteCommand() > 0;
                }
                else
                {
                    result = GetOperDb().Updateable(updateObjs).ExecuteCommand() > 0;
                }
                LogSql(sqlSugar);
                if (IsUseRedisCache && result)
                {
                    DeleteFromRedis().Wait();
                }

                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        public virtual bool UpdateDicColumns(Dictionary<string, object> dicparam, Expression<Func<T, bool>> where)
        {
            try
            {
                List<string> columnNames = new List<string>();
                foreach (var col in dicparam.Keys)
                {
                    columnNames.Add(col);
                }
                var entity = new T();
                var properties = typeof(T).GetProperties();
                foreach (var property in properties)
                {
                    if (dicparam.ContainsKey(property.Name))
                        property.SetValue(entity, dicparam[property.Name]);
                }
                var validationResult = _validator.Validate(entity, options =>
                {
                    options.IncludeProperties(columnNames.ToArray());
                });
                if (!validationResult.IsValid)
                {
                    throw new ValidationException(string.Join("；", validationResult.Errors.Select(e => e.ErrorMessage)));
                }

                // 先更新数据库
                bool result;
                if (IsSplitTable)
                {
                    result = GetOperDb().Updateable<T>(dicparam).Where(where).SplitTable().ExecuteCommand() > 0;
                }
                else
                {
                    result = GetOperDb().Updateable<T>(dicparam).Where(where).ExecuteCommand() > 0;
                }
                var aa = sqlSugar;
                if (IsUseRedisCache && result)
                {
                    DeleteFromRedis().Wait();
                }

                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        public virtual bool UpdateColumns(T obj, Expression<Func<T, object>> column)
        {
            return UpdateColumns(new List<T> { obj }, column);
        }

        public virtual bool UpdateColumns(List<T> updateObjs, Expression<Func<T, object>> column)
        {
            try
            {
                // 提取要更新的列名
                var columnNames = ExtractColumnNames(column);
                if (columnNames.Count == 0) return false;
                // 只验证要更新的列
                foreach (var entity in updateObjs)
                {
                    var validationResult = _validator.Validate(entity, options =>
                    {
                        options.IncludeProperties(columnNames.ToArray());
                    });

                    if (!validationResult.IsValid)
                    {
                        throw new ValidationException(string.Join("；", validationResult.Errors.Select(e => e.ErrorMessage)));
                    }
                }

                bool result;
                if (IsSplitTable)
                {
                    result = GetOperDb().Updateable(updateObjs).UpdateColumns(column).SplitTable().ExecuteCommand() > 0;
                }
                else
                {
                    result = GetOperDb().Updateable(updateObjs).UpdateColumns(column).ExecuteCommand() > 0;
                }
                var aa = sqlSugar;
                if (IsUseRedisCache && result)
                {
                    // 转换为新的表达式类型
                    var memberInit = Expression.MemberInit(
                        Expression.New(typeof(T)),
                        columnNames.Select(name => Expression.Bind(
                            typeof(T).GetProperty(name),
                            Expression.Property(Expression.Parameter(typeof(T), "x"), name)
                        ))
                    );
                    var lambda = Expression.Lambda<Func<T, T>>(memberInit, Expression.Parameter(typeof(T), "x"));
                    UpdateRedisCache(updateObjs, lambda).Wait();
                }
                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        public virtual bool UpdateColumns(Expression<Func<T, T>> columns, Expression<Func<T, bool>> where)
        {
            try
            {
                // 获取要更新的属性
                var expression = columns.Body as MemberInitExpression;
                if (expression == null) return false;  // 无法解析表达式时，清空缓存

                // 获取要更新的属性名
                var columnNames = expression.Bindings
                    .Select(b => b.Member.Name)
                    .ToArray();

                // 创建更新后的对象
                var parameter = Expression.Parameter(typeof(T), "x");
                var newObj = Expression.Lambda<Func<T, T>>(expression, parameter).Compile();

                // 更新匹配的记录
                var old = new T();
                var entity = new T();
                var updatedItem = newObj(old);
                foreach (var propName in columnNames)
                {
                    var prop = typeof(T).GetProperty(propName);
                    if (prop != null)
                    {
                        var value = prop.GetValue(updatedItem);
                        prop.SetValue(entity, value);
                    }
                }

                var validationResult = _validator.Validate(entity, options =>
                {
                    options.IncludeProperties(columnNames.ToArray());
                });
                if (!validationResult.IsValid)
                {
                    throw new ValidationException(string.Join("；", validationResult.Errors.Select(e => e.ErrorMessage)));
                }

                bool result;
                if (IsSplitTable)
                {
                    result = GetOperDb().Updateable<T>().SetColumns(columns).Where(where).SplitTable().ExecuteCommand() > 0;
                }
                else
                {
                    result = GetOperDb().Updateable<T>().SetColumns(columns).Where(where).ExecuteCommand() > 0;
                }
                var aa = sqlSugar;

                // 如果启用Redis缓存且数据库更新成功
                if (IsUseRedisCache && result)
                {
                    // 使用表达式筛选需要更新的缓存记录
                    UpdateRedisCache(null, columns, where).Wait();
                }
                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        public virtual bool UpdateIgnoreColumns(T obj, Expression<Func<T, object>> column)
        {
            return UpdateIgnoreColumns(new List<T> { obj }, column);
        }

        public virtual bool UpdateIgnoreColumns(List<T> updateObjs, Expression<Func<T, object>> column)
        {
            try
            {
                // 提取要忽略的列名
                var ignoreColumnNames = ExtractColumnNames(column);
                if (ignoreColumnNames.Count == 0) return false;

                // 获取所有属性
                var allProperties = typeof(T).GetProperties()
                    .Where(p => p.CanRead && p.CanWrite)
                    .ToList();

                // 验证除了忽略列之外的所有列
                foreach (var entity in updateObjs)
                {
                    var validationResult = _validator.Validate(entity, options =>
                    {
                        options.IncludeProperties(allProperties
                            .Select(p => p.Name)
                            .Where(name => !ignoreColumnNames.Contains(name))
                            .ToArray());
                    });

                    if (!validationResult.IsValid)
                    {
                        throw new ValidationException(string.Join("；", validationResult.Errors.Select(e => e.ErrorMessage)));
                    }
                }

                bool result;
                if (IsSplitTable)
                {
                    result = GetOperDb().Updateable(updateObjs).IgnoreColumns(column).SplitTable().ExecuteCommand() > 0;
                }
                else
                {
                    result = GetOperDb().Updateable(updateObjs).IgnoreColumns(column).ExecuteCommand() > 0;
                }
                var aa = sqlSugar;
                if (IsUseRedisCache && result)
                {
                    // 转换为新的表达式类型
                    var memberInit = Expression.MemberInit(
                        Expression.New(typeof(T)),
                        allProperties
                            .Where(p => !ignoreColumnNames.Contains(p.Name))
                            .Select(p => Expression.Bind(
                                p,
                                Expression.Property(Expression.Parameter(typeof(T), "x"), p)
                            ))
                    );
                    var lambda = Expression.Lambda<Func<T, T>>(memberInit, Expression.Parameter(typeof(T), "x"));
                    UpdateRedisCache(updateObjs, lambda, null, true).Wait();
                }
                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        /// <summary>
        /// 大数据批量更新
        /// </summary>
        /// <param name="updateObjs"></param>
        /// <returns></returns>
        public virtual bool UpdateBulkCopy(List<T> updateObjs)
        {
            try
            {
                // 执行数据验证
                foreach (var entity in updateObjs)
                {
                    var validationResult = _validator.Validate(entity);
                    if (!validationResult.IsValid)
                    {
                        throw new ValidationException(string.Join("；", validationResult.Errors.Select(e => e.ErrorMessage)));
                    }
                }

                bool result;
                if (IsSplitTable)
                {
                    result = Db.Fastest<T>().SplitTable().BulkUpdate(updateObjs) > 0;
                }
                else
                {
                    result = Db.Fastest<T>().BulkUpdate(updateObjs) > 0;
                }
                if (IsUseRedisCache && result)
                {
                    DeleteFromRedis().Wait();
                }
                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        #endregion

        #region 新增或更新操作(upsert，对齐 OpenSearch 的 SaveBatch/Save 语义)

        /// <summary>
        /// 新增或更新单条（主键存在则更新，不存在则新增）
        /// </summary>
        public virtual bool Save(T obj)
        {
            return SaveBatch(new List<T> { obj });
        }

        /// <summary>
        /// 批量新增或更新（主键存在则更新，不存在则新增）。
        /// 分表场景下按主键 SnowId 落到对应分表；非分表场景按主键 upsert。
        /// </summary>
        public virtual bool SaveBatch(List<T> models)
        {
            try
            {
                // 执行数据验证
                foreach (var entity in models)
                {
                    var validationResult = _validator.Validate(entity);
                    if (!validationResult.IsValid)
                    {
                        throw new ValidationException(string.Join("；", validationResult.Errors.Select(e => e.ErrorMessage)));
                    }
                }

                bool result;
                if (IsSplitTable)
                {
                    // 分表：SnowId 已标注 [SplitField]，SqlSugar 会按主键推导目标分表；PageSize(1000) 防止单批过大
                    var x = GetOperDb().Storageable(models).SplitTable().PageSize(1000);
                    result = x.ExecuteCommand() > 0;
                }
                else
                {
                    var x = GetOperDb().Storageable(models).PageSize(1000);
                    result = x.ExecuteCommand() > 0;
                }
                LogSql(sqlSugar);

                if (IsUseRedisCache && result)
                {
                    DeleteFromRedis().Wait();
                }

                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        #endregion

        #region 新增操作

        public virtual bool Insert(T obj)
        {
            return InsertRange(new List<T> { obj });
        }

        public virtual bool InsertRange(List<T> insertObjs)
        {
            try
            {
                // 执行数据验证
                foreach (var entity in insertObjs)
                {
                    var validationResult = _validator.Validate(entity);
                    if (!validationResult.IsValid)
                    {
                        throw new ValidationException(string.Join("；", validationResult.Errors.Select(e => e.ErrorMessage)));
                    }
                }

                // 先保存到数据库
                bool result;
                if (IsSplitTable)
                {
                    result = GetOperDb().Insertable(insertObjs).SplitTable().ExecuteCommand() > 0;
                }
                else
                {
                    result = GetOperDb().Insertable(insertObjs).ExecuteCommand() > 0;
                }
                LogSql(sqlSugar);
                if (IsUseRedisCache && result)
                {
                    DeleteFromRedis().Wait();
                }

                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        /// <summary>
        /// 大数量插入，比逐行插入快
        /// </summary>
        public virtual bool InsertBulkCopy(List<T> insertObjs)
        {
            try
            {
                // 执行数据验证
                foreach (var entity in insertObjs)
                {
                    var validationResult = _validator.Validate(entity);
                    if (!validationResult.IsValid)
                    {
                        throw new ValidationException(string.Join("；", validationResult.Errors.Select(e => e.ErrorMessage)));
                    }
                }
                bool result;
                if (IsSplitTable)
                {
                    result = Db.Fastest<T>().SplitTable().BulkCopy(insertObjs) > 0;
                }
                else
                {
                    result = Db.Fastest<T>().BulkCopy(insertObjs) > 0;
                }

                // 如果启用Redis缓存且数据库保存成功
                if (IsUseRedisCache && result)
                {
                    DeleteFromRedis().Wait();
                }

                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        public virtual bool InsertColumns(T obj, Expression<Func<T, object>> column)
        {
            return InsertColumns(new List<T> { obj }, column);
        }

        public virtual bool InsertColumns(List<T> insertObjs, Expression<Func<T, object>> column)
        {
            try
            {
                // 提取要新增的列名
                var columnNames = ExtractColumnNames(column);
                if (columnNames.Count == 0) return false;
                // 只验证要新增的列
                foreach (var entity in insertObjs)
                {
                    var validationResult = _validator.Validate(entity, options =>
                    {
                        options.IncludeProperties(columnNames.ToArray());
                    });

                    if (!validationResult.IsValid)
                    {
                        throw new ValidationException(string.Join("；", validationResult.Errors.Select(e => e.ErrorMessage)));
                    }
                }

                bool result;
                if (IsSplitTable)
                {
                    result = GetOperDb().Insertable(insertObjs).InsertColumns(column).SplitTable().ExecuteCommand() > 0;
                }
                else
                {
                    result = GetOperDb().Insertable(insertObjs).InsertColumns(column).ExecuteCommand() > 0;
                }
                var aa = sqlSugar;
                if (IsUseRedisCache && result)
                {
                    DeleteFromRedis().Wait();
                }

                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        public virtual bool InsertIgnoreColumns(T obj, Expression<Func<T, object>> column)
        {
            return InsertIgnoreColumns(new List<T> { obj }, column);
        }

        public virtual bool InsertIgnoreColumns(List<T> insertObjs, Expression<Func<T, object>> column)
        {
            try
            {
                // 提取要忽略的列名
                var ignoreColumnNames = ExtractColumnNames(column);
                if (ignoreColumnNames.Count == 0) return false;

                // 获取所有属性
                var allProperties = typeof(T).GetProperties()
                    .Where(p => p.CanRead && p.CanWrite)
                    .ToList();

                // 验证除了忽略列之外的所有列
                foreach (var entity in insertObjs)
                {
                    var validationResult = _validator.Validate(entity, options =>
                    {
                        options.IncludeProperties(allProperties
                            .Select(p => p.Name)
                            .Where(name => !ignoreColumnNames.Contains(name))
                            .ToArray());
                    });

                    if (!validationResult.IsValid)
                    {
                        throw new ValidationException(string.Join("；", validationResult.Errors.Select(e => e.ErrorMessage)));
                    }
                }

                bool result;
                if (IsSplitTable)
                {
                    result = GetOperDb().Insertable(insertObjs).IgnoreColumns(column).SplitTable().ExecuteCommand() > 0;
                }
                else
                {
                    result = GetOperDb().Insertable(insertObjs).IgnoreColumns(column).ExecuteCommand() > 0;
                }
                var aa = sqlSugar;
                if (IsUseRedisCache && result)
                {
                    DeleteFromRedis().Wait();
                }

                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        public virtual T InsertReturnEntity(T insertObj)
        {
            try
            {
                var validationResult = _validator.Validate(insertObj);
                if (!validationResult.IsValid)
                {
                    throw new ValidationException(string.Join("；", validationResult.Errors.Select(e => e.ErrorMessage)));
                }
                var entity = GetOperDb().Insertable(insertObj).ExecuteReturnEntity();
                var aa = sqlSugar;
                if (IsUseRedisCache && entity != null)
                {
                    DeleteFromRedis().Wait();
                }
                return entity;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        #endregion

        #region 删除操作

        public virtual bool DeleteBy(Expression<Func<T, bool>> wheres)
        {
            try
            {
                // 先删除数据库数据
                bool result;
                if (IsSplitTable)
                {
                    // 带选择器的重载 SplitTable(tabs => tabs) 支持 Where(expression) 直接删除
                    result = GetOperDb().Deleteable<T>().Where(wheres).SplitTable(tabs => tabs).ExecuteCommand() > 0;
                }
                else
                {
                    result = GetOperDb().Deleteable<T>().Where(wheres).ExecuteCommand() > 0;
                }
                LogSql(sqlSugar);
                // 如果启用Redis缓存且数据库删除成功
                if (IsUseRedisCache && result)
                {
                    // 删除成功后强制刷新Redis缓存：直接清除整个类型的缓存key，
                    DeleteFromRedis().Wait();
                }

                return result;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
        }

        #endregion

        #region 事务函数

        /// <summary>
        /// 事务函数
        /// </summary>
        /// <param name="tranAction"></param>
        /// <returns></returns>
        public virtual bool TranAction(Action tranAction)
        {
            bool isresult = false;
            // 为本次事务创建独立连接实例，同时通过 _transactionDb 让事务内的所有
            // GetOperDb() 调用都使用此同一实例，确保事务一致性。
            var newDb = Db.CopyNew();
            // 事务专用连接同样挂载租户隔离（CopyNew 不继承过滤器）
            TenantIsolation.Attach(newDb);
            _transactionDb.Value = newDb;
            try
            {
                newDb.BeginTran();

                tranAction();

                newDb.CommitTran();
                isresult = true;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                newDb.RollbackTran();
                // 校验异常原样抛出（保留类型，供 ActionFilter 识别并返回字段级错误）
                if (ex is ValidationException) throw;
                throw new Exception($"SQL语句：{sqlError}，错误日志：{ex.ToString()}");
            }
            finally
            {
                _transactionDb.Value = null;
            }

            return isresult;
        }

        #endregion

        #region 其他方法

        /// <summary>
        /// 获取实体的Json字段名
        /// </summary>
        /// <returns></returns>
        private string GetJsonFieldName()
        {
            var prop = typeof(T).GetProperties()
                .FirstOrDefault(p => p.GetCustomAttributes(typeof(JsonFieldAttribute), true).Any());
            return prop?.Name;
        }

        /// <summary>
        /// 数据库备份
        /// </summary>
        /// <param name="dirpath">备份路径</param>
        /// <returns></returns>
        public virtual bool BackupDataBase(string dirpath)
        {
            bool isok = false;
            try
            {
                string time = DateTime.Now.ToDateString();
                string filename = "";
                if (DbType == DbType.SqlServer)
                {
                    filename = Path.Combine(dirpath, $"{time}.bak");
                    isok = Db.DbMaintenance.BackupDataBase(Db.Ado.Connection.Database, filename);
                }
                else if (DbType == DbType.Sqlite)
                {
                    filename = Path.Combine(dirpath, $"{time}.db");
                    isok = Db.DbMaintenance.BackupDataBase(null, filename);
                }
                else if (DbType == DbType.MySql || DbType == DbType.Tidb)
                {
                    filename = Path.Combine(dirpath, $"{time}.sql");
                    isok = Db.DbMaintenance.BackupDataBase(Db.Ado.Connection.Database, filename);
                }
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                LogHelper.ErrorLogWrite("DbContext", "Execute", ex.ToString(), "数据访问");
            }
            return isok;
        }

        /// <summary>
        /// 反向工程
        /// </summary>
        /// <returns></returns>
        public virtual bool InitTables()
        {
            bool isok = false;
            try
            {
                if (DbSetting.Current.Migration)
                {
                    List<Type> lstSplitTypes = new List<Type>();
                    List<Type> lstTypes = new List<Type>();
                    List<Type> lstDaoTypes = new List<Type>();
                    var assembly = Assembly.GetExecutingAssembly();
                    if (assembly != null)
                    {
                        List<string> daotypes = new List<string>();
                        foreach (var item in assembly.ExportedTypes)
                        {
                            var attrs = item.GetCustomAttributes();
                            bool isSplitTable = false;
                            if (attrs.Any(t => t.TypeId.ToString().ToLower().Contains("fullentity"))) continue;
                            if (attrs.Any(t => t.TypeId.ToString().ToLower().Contains("expand"))) continue;
                            if (attrs.Any(t => t.TypeId.ToString().ToLower().Contains("dbsqlite"))) continue;
                            if (attrs.Any(t => t.TypeId.ToString().ToLower().Contains("splittable"))) isSplitTable = true;
                            var cus = attrs.FirstOrDefault(t => t.TypeId.ToString().ToLower().Contains("sugartable"));
                            if (cus != null)
                            {
                                var tablename = (cus as SugarTable).TableName;
                                lock (SqlSugarHelper.TableAll)
                                {
                                    if (Db != null && !SqlSugarHelper.TableAll.Contains(tablename))
                                    {
                                        if (isSplitTable) lstSplitTypes.Add(item);
                                        else lstTypes.Add(item);
                                    }
                                    else
                                    {
                                        daotypes.Add(item.Name.ToLower());
                                    }
                                }
                            }
                        }
                        foreach (var item in assembly.ExportedTypes)
                        {
                            if (item.Name.ToLower().Contains("dao")
                                && !item.Name.ToLower().Contains("syscommon")
                                && !item.Name.ToLower().Contains("sysfullentity"))
                            {
                                if (!daotypes.Any(t => item.Name.ToLower().Contains(t)))
                                    lstDaoTypes.Add(item);
                            }
                        }
                    }
                    if (lstTypes.Count > 0)
                    {
                        StaticConfig.CodeFirst_MySqlCollate = "utf8mb4_general_ci";
                        Db.DbMaintenance.CreateDatabase();
                        //SqlSugar_Custom.Db.CodeFirst.InitTables(lstTypes.ToArray());
                        if (lstDaoTypes.Count > 0)
                        {
                            foreach (var item in lstDaoTypes)
                            {
                                object instance = Activator.CreateInstance(item);
                                //var methodInfo = item.GetMethod("Init");
                                //if (methodInfo != null)
                                //{
                                //    try
                                //    {
                                //        methodInfo.Invoke(instance, null);
                                //    }
                                //    catch { }
                                //}
                            }
                        }
                        isok = true;
                    }
                    if (lstSplitTypes.Count > 0)
                    {
                        StaticConfig.CodeFirst_MySqlCollate = "utf8mb4_general_ci";
                        Db.DbMaintenance.CreateDatabase();
                        SqlSugar_Split.Db.CodeFirst.SplitTables().InitTables(lstSplitTypes.ToArray());
                        isok = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                LogHelper.ErrorLogWrite("DbContext", "Execute", ex.ToString(), "数据访问");
            }
            return isok;
        }

        #endregion

        #region Redis缓存相关

        /// <summary>
        /// 清除缓存
        /// </summary>
        public async Task ClearCache()
        {
            if (IsUseRedisCache)
            {
                await DeleteFromRedis();
            }
        }

        /// <summary>
        /// Redis缓存是否包含指定key
        /// </summary>
        private bool RedisContainsKey()
        {
            try
            {
                return RedisService?.KeyExists(typename) ?? false;
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                LogHelper.ErrorLogWrite("DbContext", "Execute", ex.ToString(), "数据访问");
            }
            return false;
        }

        /// <summary>
        /// 从Redis获取列表
        /// </summary>
        private async Task<List<T>> GetListFromRedis()
        {
            List<T> list = new List<T>();
            try
            {
                if (RedisService != null)
                {
                    // 验证缓存与数据库的数据一致性
                    await ValidateCacheConsistency();

                    var valuestr = await RedisService.StringGetAsync(typename);
                    var _list = valuestr.HasValue ? valuestr.ToString().ToObject<List<T>>() : null;
                    if (_list.IsZxxAny()) list.AddRange(_list);
                }
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                LogHelper.ErrorLogWrite("DbContext", "Execute", ex.ToString(), "数据访问");
            }
            return list;
        }

        /// <summary>
        /// 验证Redis缓存与数据库的数据一致性
        /// 每10分钟检测一次，如果数量不一致则刷新缓存
        /// </summary>
        private async Task ValidateCacheConsistency()
        {
            try
            {
                // 检查是否需要进行验证
                bool shouldValidate = false;
                lock (_lastValidationTimes)
                {
                    if (!_lastValidationTimes.TryGetValue(typename, out DateTime lastValidation) ||
                        (DateTime.Now - lastValidation) >= _validationInterval)
                    {
                        _lastValidationTimes[typename] = DateTime.Now;
                        shouldValidate = true;
                    }
                }

                if (!shouldValidate) return;

                // 从Redis获取数据计数
                var valuestr = await RedisService.StringGetAsync(typename);
                var cacheData = valuestr.HasValue ? valuestr.ToString().ToObject<List<T>>() : null;
                int cacheCount = cacheData?.Count ?? 0;

                // 从数据库获取记录数（ClearFilter 绕过租户过滤器：缓存是全租户快照，须与全表计数对账）
                int dbCount = 0;
                if (IsSplitTable)
                {
                    dbCount = GetOperDb().Queryable<T>().ClearFilter<ITenantEntity>().SplitTable().Count();
                }
                else
                {
                    dbCount = GetOperDb().Queryable<T>().ClearFilter<ITenantEntity>().Count();
                }

                // 如果数量不一致，清除缓存
                if (cacheCount != dbCount)
                {
                    LogHelper.SysLogWrite(nameof(DbContext<T>), "VerifyCacheCount", $"缓存验证: {typename} 缓存数量({cacheCount})与数据库数量({dbCount})不一致，清除缓存", "缓存校验");
                    await DeleteFromRedis();

                    // 获取最新数据并缓存（ClearFilter 同上：缓存全租户快照）
                    List<T> freshData;
                    if (IsSplitTable)
                    {
                        freshData = GetOperDb().Queryable<T>().ClearFilter<ITenantEntity>().SplitTable().ToList();
                    }
                    else
                    {
                        freshData = GetOperDb().Queryable<T>().ClearFilter<ITenantEntity>().ToList();
                    }

                    if (freshData.IsZxxAny())
                    {
                        await SaveBatchToRedis(freshData);
                    }
                }
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                LogHelper.ErrorLogWrite("DbContext", "Execute", ex.ToString(), "数据访问");
            }
        }

        /// <summary>
        /// 保存到Redis
        /// </summary>
        private async Task<bool> SaveToRedis(T value)
        {
            bool res = false;
            try
            {
                if (RedisService != null)
                {
                    res = await SaveBatchToRedis(new List<T> { value });
                }
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                LogHelper.ErrorLogWrite("DbContext", "Execute", ex.ToString(), "数据访问");
            }
            return res;
        }

        /// <summary>
        /// 批量保存到Redis
        /// </summary>
        private async Task<bool> SaveBatchToRedis(List<T> values)
        {
            bool res = false;
            try
            {
                if (RedisService != null)
                {
                    var _list = await GetListFromRedis();
                    if (_list.IsZxxAny())
                    {
                        var fieldNames = GetFieldNames();
                        var IsPrimaryKey = fieldNames.Find(t => t.IsPrimaryKey);
                        foreach (var item in values)
                        {
                            var pkFieldValue = item.GetType().GetProperty(IsPrimaryKey.ParamName).GetValue(item);
                            if (pkFieldValue != null)
                            {
                                _list.RemoveAll(t => pkFieldValue.Equals(t.GetType().GetProperty(IsPrimaryKey.ParamName).GetValue(t)));
                                _list.Add(item);
                            }
                        }
                        res = await RedisService.StringSetAsync(typename, _list.ToJson(), TimeSpan.FromSeconds(RedisHelper._CacheSeconds));
                    }
                    else
                    {
                        res = await RedisService.StringSetAsync(typename, values.ToJson(), TimeSpan.FromSeconds(RedisHelper._CacheSeconds));
                    }
                }
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                LogHelper.ErrorLogWrite("DbContext", "Execute", ex.ToString(), "数据访问");
            }
            return res;
        }

        /// <summary>
        /// 从Redis删除
        /// </summary>
        private async Task DeleteFromRedis()
        {
            try
            {
                if (RedisService != null)
                {
                    await RedisService.KeyDeleteAsync(typename);
                }
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                LogHelper.ErrorLogWrite("DbContext", "Execute", ex.ToString(), "数据访问");
            }
        }

        /// <summary>
        /// 根据条件从Redis删除
        /// </summary>
        private async Task<bool> DeleteFromRedisByPredicate(Predicate<T> wheres)
        {
            bool res = false;
            try
            {
                if (RedisService != null && RedisContainsKey())
                {
                    var list = await GetListFromRedis();
                    if (list.IsZxxAny())
                    {
                        list.RemoveAll(wheres);
                        if (list.Count == 0)
                        {
                            await DeleteFromRedis();
                            res = true;
                        }
                        else res = await RedisService.StringSetAsync(typename, list.ToJson(), TimeSpan.FromSeconds(RedisHelper._CacheSeconds));
                    }
                }
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                LogHelper.ErrorLogWrite("DbContext", "Execute", ex.ToString(), "数据访问");
            }
            return res;
        }

        /// <summary>
        /// 更新Redis缓存中的对象字段
        /// </summary>
        /// <param name="updateObjs">要更新的实体列表(如果为null则使用where条件)</param>
        /// <param name="columns">列表达式</param>
        /// <param name="where">条件表达式(可空)</param>
        /// <param name="isIgnore">是否为忽略模式(true=忽略这些列，false=只更新这些列)</param>
        /// <returns>更新是否成功</returns>
        private async Task<bool> UpdateRedisCache(List<T> updateObjs, Expression<Func<T, T>> columns, Expression<Func<T, bool>> where = null, bool isIgnore = false)
        {
            bool res = false;
            try
            {
                if (RedisService == null)
                {
                    return false;
                }

                // 读取Redis中已有的数据
                var list = await GetListFromRedis();
                if (!list.IsZxxAny())
                {
                    // 如果Redis中没有数据，直接清空缓存即可
                    await DeleteFromRedis();
                    return true;
                }

                // 获取要更新的属性
                var memberInitExpression = columns.Body as MemberInitExpression;
                if (memberInitExpression == null)
                {
                    // 无法解析表达式时，清空缓存
                    await DeleteFromRedis();
                    return true;
                }

                // 获取要更新的属性名
                var updatePropertyNames = memberInitExpression.Bindings
                    .Select(b => b.Member.Name)
                    .ToList();

                // 获取主键字段
                var fieldNames = GetFieldNames();
                var primaryKey = fieldNames.Find(t => t.IsPrimaryKey);
                if (primaryKey == null)
                {
                    // 没有主键时，无法精确更新，直接清空缓存
                    await DeleteFromRedis();
                    return true;
                }

                var primaryKeyProp = typeof(T).GetProperty(primaryKey.ParamName);
                if (primaryKeyProp == null)
                {
                    await DeleteFromRedis();
                    return true;
                }

                // 判断更新方式：使用提供的对象还是根据where条件筛选
                bool hasUpdated = false;
                if (updateObjs.IsZxxAny())
                {
                    // 使用提供的对象更新Redis
                    hasUpdated = UpdateByObjects(list, updateObjs, primaryKeyProp, updatePropertyNames);
                }
                else if (where != null)
                {
                    // 使用where条件筛选并更新Redis
                    hasUpdated = UpdateByCondition(list, where, updatePropertyNames, memberInitExpression);
                }
                else
                {
                    // 既没有对象也没有条件，直接清空缓存
                    await DeleteFromRedis();
                    return true;
                }

                // 如果有数据更新，保存回Redis
                if (hasUpdated)
                {
                    res = await RedisService.StringSetAsync(typename, list.ToJson(), TimeSpan.FromSeconds(RedisHelper._CacheSeconds));
                }
            }
            catch (Exception ex)
            {
                LogSqlError(ex);
                LogHelper.ErrorLogWrite("DbContext", "Execute", ex.ToString(), "数据访问");
                // 发生异常时，清空缓存
                await DeleteFromRedis();
                res = false;
            }
            return res;
        }

        /// <summary>
        /// 根据对象列表更新Redis缓存
        /// </summary>
        private bool UpdateByObjects(List<T> cacheList, List<T> updateObjs, PropertyInfo primaryKeyProp, List<string> updatePropertyNames)
        {
            bool hasUpdated = false;
            foreach (var obj in updateObjs)
            {
                var keyValue = primaryKeyProp.GetValue(obj);
                if (keyValue != null)
                {
                    // 在Redis列表中查找对应的数据
                    for (int i = 0; i < cacheList.Count; i++)
                    {
                        var existingKeyValue = primaryKeyProp.GetValue(cacheList[i]);
                        if (existingKeyValue != null && existingKeyValue.Equals(keyValue))
                        {
                            // 找到匹配的数据，更新指定列
                            foreach (var propName in updatePropertyNames)
                            {
                                var prop = typeof(T).GetProperty(propName);
                                if (prop != null)
                                {
                                    var value = prop.GetValue(obj);
                                    prop.SetValue(cacheList[i], value);
                                    hasUpdated = true;
                                }
                            }
                            break;
                        }
                    }
                }
            }
            return hasUpdated;
        }

        /// <summary>
        /// 根据条件更新Redis缓存
        /// </summary>
        private bool UpdateByCondition(List<T> cacheList, Expression<Func<T, bool>> where, List<string> updatePropertyNames, MemberInitExpression memberInitExpression)
        {
            try
            {
                // 编译where表达式为委托函数
                var predicate = where.Compile();

                // 筛选出符合条件的缓存记录
                var matchedItems = cacheList.Where(predicate).ToList();
                if (!matchedItems.Any())
                {
                    return false;
                }

                // 创建更新后的对象
                var parameter = Expression.Parameter(typeof(T), "x");
                var newObj = Expression.Lambda<Func<T, T>>(memberInitExpression, parameter).Compile();

                // 更新匹配的记录
                foreach (var item in matchedItems)
                {
                    var updatedItem = newObj(item);
                    foreach (var propName in updatePropertyNames)
                    {
                        var prop = typeof(T).GetProperty(propName);
                        if (prop != null)
                        {
                            var value = prop.GetValue(updatedItem);
                            prop.SetValue(item, value);
                        }
                    }
                }
                return true;
            }
            catch
            {
                // 编译或执行表达式失败，返回需要清理缓存
                return true;
            }
        }

        /// <summary>
        /// 提取列表达式中的列名
        /// </summary>
        private List<string> ExtractColumnNames(Expression<Func<T, object>> column)
        {
            List<string> columnNames = new List<string>();
            if (column == null)
                return columnNames;

            if (column.Body is NewExpression newExp)
            {
                foreach (var member in newExp.Members)
                {
                    columnNames.Add(member.Name);
                }
            }
            else if (column.Body is MemberExpression memberExp)
            {
                columnNames.Add(memberExp.Member.Name);
            }
            else if (column.Body is UnaryExpression unaryExp)
            {
                if (unaryExp.Operand is MemberExpression innerMemberExp)
                {
                    columnNames.Add(innerMemberExp.Member.Name);
                }
            }

            return columnNames;
        }

        #endregion

    }
}
