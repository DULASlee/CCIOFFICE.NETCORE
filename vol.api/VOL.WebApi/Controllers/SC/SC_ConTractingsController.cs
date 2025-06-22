/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果要增加方法请在当前目录下Partial文件夹SC_ConTractingsController编写
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Core.Controllers.Basic;
using VOL.Entity.AttributeManager;
using VOL.SC.IServices;
namespace VOL.SC.Controllers
{
    [Route("api/SC_ConTractings")]
    [PermissionTable(Name = "SC_ConTractings")]
    public partial class SC_ConTractingsController : ApiBaseController<ISC_ConTractingsService>
    {
        public SC_ConTractingsController(ISC_ConTractingsService service)
        : base(service)
        {
        }
    }
}

