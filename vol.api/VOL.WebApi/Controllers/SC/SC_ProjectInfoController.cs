/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果要增加方法请在当前目录下Partial文件夹SC_ProjectInfoController编写
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Core.Controllers.Basic;
using VOL.Entity.AttributeManager;
using VOL.SC.IServices;
namespace VOL.SC.Controllers
{
    [Route("api/SC_ProjectInfo")]
    [PermissionTable(Name = "SC_ProjectInfo")]
    public partial class SC_ProjectInfoController : ApiBaseController<ISC_ProjectInfoService>
    {
        public SC_ProjectInfoController(ISC_ProjectInfoService service)
        : base(service)
        {
        }
    }
}

