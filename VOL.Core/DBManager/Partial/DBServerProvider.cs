using System;
using System.Collections.Generic;
using System.Text;
using VOL.Core.Configuration;
using VOL.Core.Dapper;
using VOL.Core.Enums;

namespace VOL.Core.DBManager
{
    /// <summary>
    /// 2022.11.21增加其他数据库(sqlserver、mysql、pgsql、oracle)连接配置说明
    /// 需要把两个DBServerProvider.cs文件都更新下
    /// </summary>
    public partial class DBServerProvider
    {
        ///// <summary>
        ///// 单独配置mysql数据库
        ///// </summary>
        //public static ISqlDapper SqlDapperMySql
        //{
        //    get
        //    {
        //        //读取appsettings.json中的配置
        //        string 数据库连接字符串 = AppSetting.GetSettingString("key");
        //        return new SqlDapper(数据库连接字符串, DbCurrentType.MySql);

        //        //访问数据库方式
        //        //DBServerProvider.SqlDapperMySql.xx
        //    }
        //}


        /// <summary>
        /// 获取第二个MySql数据库的Dapper实例 (如果配置了多个MySql数据库)
        /// </summary>
        /// <remarks>
        /// 连接字符串通常在appsettings.json中配置，键名为"key2"。
        /// 使用方法: DBServerProvider.SqlDapperMySql2.xxx
        /// </remarks>
        /// <returns>MySql数据库的Dapper实例 : VOL.Core.Dapper.ISqlDapper</returns>
        public static ISqlDapper SqlDapperMySql2
        {
            get
            {
                //读取appsettings.json中的配置
                string 数据库连接字符串 = AppSetting.GetSettingString("key2");
                return new SqlDapper(数据库连接字符串, DbCurrentType.MySql);

                //访问数据库方式
                //DBServerProvider.SqlDapperMySql2.xx
            }
        }

        ///// <summary>
        ///// 单独配置SqlServer数据库
        ///// </summary>
        //public static ISqlDapper SqlDapperSqlServer
        //{
        //    get
        //    {
        //        //读取appsettings.json中的配置
        //        string 数据库连接字符串 = AppSetting.GetSettingString("key");
        //        return new SqlDapper(数据库连接字符串, DbCurrentType.MsSql);

        //        //访问数据库方式
        //        //DBServerProvider.SqlDapperSqlServer.xx
        //    }
        //}
    }
}
