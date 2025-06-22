/*
 *Author：codesoft
 *Contact：971926469@qq.com
 *代码由框架生成,此处任何更改都可能导致被代码生成器覆盖
 *所有业务编写全部应在Partial文件夹下SC_ProjectInfoService与ISC_ProjectInfoService中编写
 */
using VOL.SC.IRepositories;
using VOL.SC.IServices;
using VOL.Core.BaseProvider;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.SC.Services
{
    public partial class SC_ProjectInfoService : ServiceBase<SC_ProjectInfo, ISC_ProjectInfoRepository>
    , ISC_ProjectInfoService, IDependency
    {
    public static ISC_ProjectInfoService Instance
    {
      get { return AutofacContainerModule.GetService<ISC_ProjectInfoService>(); } }
    }
 }
