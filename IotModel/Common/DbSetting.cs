using NewLife.Configuration;
using System;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>DB配置</summary>
    [Description("DB配置")]
    [Config("Config/DbSetting.config")]
    public class DbSetting : Config<DbSetting>
    {
        /// <summary>是否反向工程</summary>
        [Description("是否反向工程")]
        public Boolean Migration { get; set; } = true;

        /// <summary>数据库类型(Tidb/MySql/PostgreSQL,对应SqlSugar.DbType枚举名)</summary>
        [Description("数据库类型(Tidb/MySql/PostgreSQL,对应SqlSugar.DbType枚举名)")]
        public String DbTypeName { get; set; } = "Tidb";

        /// <summary>Mysql数据库连接字符串</summary>
        [Description("Mysql数据库连接字符串")]
        public String MysqlConString { get; set; } = "Server=192.168.0.100;Port=3306;Database=zhjngkdb;Uid=root;Pwd=cenBo@123;Charset=utf8mb4;AllowLoadLocalInfile=true;SslMode=None;Pooling=true;Min Pool Size=1;Max Pool Size=5;";

        /// <summary>Mysql数据库连接字符串(分表)</summary>
        [Description("Mysql数据库连接字符串(分表)")]
        public String MysqlSplitConString { get; set; } = "Server=192.168.0.100;Port=3306;Database=zhjngkdb_split;Uid=root;Pwd=cenBo@123;Charset=utf8mb4;AllowLoadLocalInfile=true;SslMode=None;Pooling=true;Min Pool Size=1;Max Pool Size=5;";

        /// <summary>Sqlite数据库连接字符串</summary>
        [Description("Sqlite数据库连接字符串")]
        public string SqliteConString { get; set; } = "DataSource=Administrative/division2023.db;Pooling=true;Mode=ReadWrite;";

        /// <summary>Timescale遥测库连接字符串(空=遥测写入服务不启用;DDL见database/timescaledb)</summary>
        [Description("Timescale遥测库连接字符串(空=遥测写入服务不启用;DDL见database/timescaledb)")]
        public String TimescaleConString { get; set; } = "";

        /// <summary>Tendis是否启用集群</summary>
        [Description("Tendis是否启用集群")]
        public Boolean IsTendisCluster { get; set; } = false;

        /// <summary>Tendis连接字符串(集群)</summary>
        [Description("Tendis连接字符串(集群)")]
        public String TendisConStringCluster { get; set; } = "Server=192.168.0.100:30000,192.168.0.100:30001,192.168.0.100:30002;Pwd=Cenbo88211111;Db=1";

        /// <summary>Tendis连接字符串(单例)</summary>
        [Description("Tendis连接字符串(单例)")]
        public String TendisConString { get; set; } = "Server=192.168.0.100:30000;Pwd=Cenbo88211111;Db=1";
    }
}
