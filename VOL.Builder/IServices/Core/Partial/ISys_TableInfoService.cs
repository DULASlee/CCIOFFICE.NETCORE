using VOL.Core.BaseProvider;
using VOL.Entity.DomainModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using VOL.Core.Utilities;

namespace VOL.Builder.IServices
{
    public partial interface ISys_TableInfoService
    {
        /// <summary>
        /// 获取表结构树数据
        /// </summary>
        /// <returns>返回一个包含表结构树信息元组 (Item1:string, Item2:string) : (System.String, System.String)</returns>
        Task<(string, string)> GetTableTree();

        /// <summary>
        /// 创建实体模型
        /// </summary>
        /// <param name="tableInfo">表信息对象 : VOL.Entity.DomainModels.Sys_TableInfo</param>
        /// <returns>创建的实体模型代码 : System.String</returns>
        string CreateEntityModel(Sys_TableInfo tableInfo);

        /// <summary>
        /// 保存编辑的表信息
        /// </summary>
        /// <param name="sysTableInfo">系统表信息对象 : VOL.Entity.DomainModels.Sys_TableInfo</param>
        /// <returns>Web响应内容 : VOL.Core.Utilities.WebResponseContent</returns>
        WebResponseContent SaveEidt(Sys_TableInfo sysTableInfo);

        /// <summary>
        /// 创建服务层代码
        /// </summary>
        /// <param name="tableName">表名 : System.String</param>
        /// <param name="nameSpace">命名空间 : System.String</param>
        /// <param name="foldername">文件夹名称 : System.String</param>
        /// <param name="webController">是否创建Web控制器 : System.Boolean</param>
        /// <param name="apiController">是否创建Api控制器 : System.Boolean</param>
        /// <returns>创建的服务层代码 : System.String</returns>
        string CreateServices(string tableName, string nameSpace, string foldername, bool webController, bool apiController);

        /// <summary>
        /// 创建Vue页面
        /// </summary>
        /// <param name="sysTableInfo">系统表信息对象 : VOL.Entity.DomainModels.Sys_TableInfo</param>
        /// <param name="vuePath">Vue页面路径 : System.String</param>
        /// <returns>创建的Vue页面代码 : System.String</returns>
        string CreateVuePage(Sys_TableInfo sysTableInfo, string vuePath);

        /// <summary>
        /// 加载表数据
        /// </summary>
        /// <param name="parentId">父节点ID : System.Int32</param>
        /// <param name="tableName">表名 : System.String</param>
        /// <param name="columnCNName">列中文名 : System.String</param>
        /// <param name="nameSpace">命名空间 : System.String</param>
        /// <param name="foldername">文件夹名称 : System.String</param>
        /// <param name="table_Id">表ID : System.Int32</param>
        /// <param name="isTreeLoad">是否树形加载 : System.Boolean</param>
        /// <returns>加载的表数据 : System.Object</returns>
        object LoadTable(int parentId, string tableName, string columnCNName, string nameSpace, string foldername, int table_Id, bool isTreeLoad);

        /// <summary>
        /// 同步表结构
        /// </summary>
        /// <param name="tableName">表名 : System.String</param>
        /// <returns>Web响应内容 : System.Threading.Tasks.Task<VOL.Core.Utilities.WebResponseContent></returns>
        Task<WebResponseContent> SyncTable(string tableName);

        /// <summary>
        /// 删除树节点（表信息）
        /// </summary>
        /// <param name="table_Id">表ID : System.Int32</param>
        /// <returns>Web响应内容 : System.Threading.Tasks.Task<VOL.Core.Utilities.WebResponseContent></returns>
        Task<WebResponseContent> DelTree(int table_Id);
    }
}
