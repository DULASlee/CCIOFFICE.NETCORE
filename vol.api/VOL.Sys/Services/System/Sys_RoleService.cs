﻿/*
 *Author：codesoft
 *Contact：971926469@qq.com
 *Date：2023-07-01
 * 此代码由框架生成，请勿随意更改
 */
using VOL.Sys.IRepositories;
using VOL.Sys.IServices;
using VOL.Core.BaseProvider;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;

namespace VOL.Sys.Services
{
    public partial class Sys_RoleService : ServiceBase<Sys_Role, ISys_RoleRepository>, ISys_RoleService, IDependency
    {
        public Sys_RoleService(ISys_RoleRepository repository)
             : base(repository) 
        { 
           Init(repository);
        }
        public static ISys_RoleService Instance
        {
           get { return AutofacContainerModule.GetService<ISys_RoleService>(); }
        }
    }
}

