/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果要增加方法请在当前目录下Partial文件夹SC_WorkerAttendanceController编写
 */
using Microsoft.AspNetCore.Mvc;
using VOL.Core.Controllers.Basic;
using VOL.Entity.AttributeManager;
using VOL.SC.IServices;
namespace VOL.SC.Controllers
{
    [Route("api/SC_WorkerAttendance")]
    [PermissionTable(Name = "SC_WorkerAttendance")]
    public partial class SC_WorkerAttendanceController : ApiBaseController<ISC_WorkerAttendanceService>
    {
        public SC_WorkerAttendanceController(ISC_WorkerAttendanceService service)
        : base(service)
        {
        }
    }
}

