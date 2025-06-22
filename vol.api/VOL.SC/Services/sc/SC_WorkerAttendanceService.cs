/*
 *Author：codesoft
 *Contact：971926469@qq.com
 *代码由框架生成,此处任何更改都可能导致被代码生成器覆盖
 *所有业务编写全部应在Partial文件夹下SC_WorkerAttendanceService与ISC_WorkerAttendanceService中编写
 */
using VOL.SC.IRepositories;
using VOL.SC.IServices;
using VOL.Core.BaseProvider;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.SC.Services
{
    public partial class SC_WorkerAttendanceService : ServiceBase<SC_WorkerAttendance, ISC_WorkerAttendanceRepository>
    , ISC_WorkerAttendanceService, IDependency
    {
    public static ISC_WorkerAttendanceService Instance
    {
      get { return AutofacContainerModule.GetService<ISC_WorkerAttendanceService>(); } }
    }
 }
