// VOL.Entity/DomainModels/Sys/UserDisplayDto.cs
namespace VOL.Entity.DomainModels.Sys
{
    /// <summary>
    /// 用于显示用户列表的简化数据传输对象。
    /// (A simplified Data Transfer Object for displaying user lists.)
    /// </summary>
    public class UserDisplayDto
    {
        public int User_Id { get; set; }
        public string UserName { get; set; } = null!; // Assuming UserName is required
        public string? UserTrueName { get; set; }
        public string? RoleName { get; set; }
        // TenantId can be added here if needed for display
        // public System.Guid? TenantId { get; set; }
    }
}
