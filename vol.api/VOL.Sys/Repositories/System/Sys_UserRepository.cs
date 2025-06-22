/*
 *Author：codesoft
 *Contact：971926469@qq.com
 *Date：2023-07-01
 * 此代码由框架生成，请勿随意更改
 */
using VOL.Sys.IRepositories;
using VOL.Core.BaseProvider;
using VOL.Core.EFDbContext;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.Sys.Repositories
{
    public partial class Sys_UserRepository : RepositoryBase<Sys_User>, ISys_UserRepository
    {
        public Sys_UserRepository(VOLContext dbContext)
        : base(dbContext)
        {

        }
        public static ISys_UserRepository Instance
        {
            get { return AutofacContainerModule.GetService<ISys_UserRepository>(); }
        }
    }
}

