// VOL.Entity/IBaseInterface/ITenantEntity.cs
namespace VOL.Entity.IBaseInterface
{
    /// <summary>
    /// 定义实体属于特定租户的接口。
    /// (Interface defining that an entity belongs to a specific tenant.)
    /// </summary>
    public interface ITenantEntity
    {
        /// <summary>
        /// 获取或设置租户ID。
        /// (Gets or sets the Tenant ID.)
        /// </summary>
        System.Guid? TenantId { get; set; } // Or int?, string? to match UserInfo.TenantId
    }
}
