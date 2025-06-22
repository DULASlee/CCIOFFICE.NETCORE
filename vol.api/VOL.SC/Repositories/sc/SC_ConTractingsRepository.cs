/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *Repository提供数据库操作，如果要增加数据库操作请在当前目录下Partial文件夹SC_ConTractingsRepository编写代码
 */
using VOL.SC.IRepositories;
using VOL.Core.BaseProvider;
using VOL.Core.EFDbContext;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.SC.Repositories
{
    public partial class SC_ConTractingsRepository : RepositoryBase<SC_ConTractings> , ISC_ConTractingsRepository
    {
    public SC_ConTractingsRepository(VOLContext dbContext)
    : base(dbContext)
    {

    }
    public static ISC_ConTractingsRepository Instance
    {
      get {  return AutofacContainerModule.GetService<ISC_ConTractingsRepository>(); } }
    }
}
