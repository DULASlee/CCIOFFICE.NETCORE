/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果要增加方法请在当前目录下Partial文件夹SC_TeamManagementController编写
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Core.Controllers.Basic;
using VOL.Entity.AttributeManager;
using VOL.SC.IServices;
namespace VOL.SC.Controllers
{
    [Route("api/SC_TeamManagement")]
    [PermissionTable(Name = "SC_TeamManagement")]
    public partial class SC_TeamManagementController : ApiBaseController<ISC_TeamManagementService>
    {
        public SC_TeamManagementController(ISC_TeamManagementService service)
        : base(service)
        {
        }
    }
}

