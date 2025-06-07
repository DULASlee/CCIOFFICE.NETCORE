using Newtonsoft.Json;
/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果数据库字段发生变化，请在代码生器重新生成此Model
 */
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VOL.Entity.SystemModels;

namespace VOL.Entity.DomainModels
{
    [Entity(TableCnName = "用户管理",TableName = "Sys_User",ApiInput = typeof(ApiSys_UserInput),ApiOutput = typeof(ApiSys_UserOutput))]
    public partial class Sys_User:BaseEntity
    {
        /// <summary>
       /// 用户登录帐号
       /// </summary>
       [Display(Name ="帐号")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string UserName { get; set; }

       /// <summary>
       /// 用户ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="User_Id")]
       [Column(TypeName="int")]
       [Required(AllowEmptyStrings=false)]
       public int User_Id { get; set; }

       /// <summary>
       /// 性别
       /// 可能的值:
       /// 0: 未知 (Unknown)
       /// 1: 男性 (Male)
       /// 2: 女性 (Female)
       /// </summary>
       [Display(Name ="性别")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? Gender { get; set; }

       /// <summary>
       /// 用户头像的URL链接
       /// </summary>
       [Display(Name ="头像")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       [Editable(true)]
       public string HeadImageUrl { get; set; }

       /// <summary>
       /// 用户所属部门ID (此字段可能已废弃或由其他方式管理，标记为"不用")
       /// </summary>
       [Display(Name ="不用")]
       [Column(TypeName="int")]
       public int? Dept_Id { get; set; }

       /// <summary>
       /// 用户所属部门名称 (此字段可能已废弃或由其他方式管理，标记为"不用")
       /// </summary>
       [Display(Name ="不用")]
       [MaxLength(150)]
       [Column(TypeName="nvarchar(150)")]
       [Editable(true)]
       public string DeptName { get; set; }

       /// <summary>
       /// 用户所属角色ID
       /// </summary>
       [Display(Name ="角色")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int Role_Id { get; set; }

       /// <summary>
       /// 用户所属角色名称 (此字段可能已废弃或由其他方式管理，标记为"不用")
       /// </summary>
       [Display(Name ="不用")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string RoleName { get; set; }

       /// <summary>
       /// 用户当前的身份验证Token
       /// </summary>
       [Display(Name ="Token")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       [Editable(true)]
       public string Token { get; set; }

       /// <summary>
       /// 应用类型或用户类型 (具体含义需参照业务文档或相关枚举)
       /// 例如可能表示:
       /// 1: Web端用户
       /// 2: App端用户
       /// </summary>
       [Display(Name ="类型")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? AppType { get; set; }

       /// <summary>
       /// 用户所属的部门ID列表 (通常用于多部门场景，格式可能为逗号分隔的ID字符串)
       /// </summary>
       [Display(Name ="组织构架")]
       [MaxLength(2000)]
       [Column(TypeName="nvarchar(2000)")]
       [Editable(true)]
       public string DeptIds { get; set; }

       /// <summary>
       /// 用户真实姓名
       /// </summary>
       [Display(Name ="姓名")]
       [MaxLength(20)]
       [Column(TypeName="nvarchar(20)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string UserTrueName { get; set; }

       /// <summary>
       /// 用户登录密码 (通常存储加密后的哈希值)
       /// </summary>
       [Display(Name ="密码")]
       [MaxLength(200)]
       [JsonIgnore]
       [Column(TypeName="nvarchar(200)")]
       public string UserPwd { get; set; }

       /// <summary>
       /// 用户账户创建或注册时间
       /// </summary>
       [Display(Name ="注册时间")]
       [Column(TypeName="datetime")]
       public DateTime? CreateDate { get; set; }

       /// <summary>
       /// 是否为手机注册用户
       /// 可能的值:
       /// 1: 是 (Yes)
       /// 0: 否 (No)
       /// </summary>
       [Display(Name ="手机用户")]
       [Column(TypeName="int")]
       public int? IsRegregisterPhone { get; set; }

       /// <summary>
       /// 用户绑定的手机号码
       /// </summary>
       [Display(Name ="手机号")]
       [MaxLength(11)]
       [Column(TypeName="nvarchar(11)")]
       public string PhoneNo { get; set; }

       /// <summary>
       /// 用户固定电话号码
       /// </summary>
       [Display(Name ="Tel")]
       [MaxLength(20)]
       [Column(TypeName="nvarchar(20)")]
       public string Tel { get; set; }

       /// <summary>
       /// 创建者ID
       /// </summary>
       [Display(Name ="CreateID")]
       [Column(TypeName="int")]
       public int? CreateID { get; set; }

       /// <summary>
       /// 创建人名称
       /// </summary>
       [Display(Name ="创建人")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       public string Creator { get; set; }

       /// <summary>
       /// 账户是否启用
       /// 可能的值:
       /// 1: 启用 (Enabled)
       /// 0: 禁用 (Disabled)
       /// </summary>
       [Display(Name ="是否可用")]
       [Column(TypeName="tinyint")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public byte Enable { get; set; }

       /// <summary>
       /// 修改者ID
       /// </summary>
       [Display(Name ="ModifyID")]
       [Column(TypeName="int")]
       public int? ModifyID { get; set; }

       /// <summary>
       /// 修改人名称
       /// </summary>
       [Display(Name ="修改人")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       public string Modifier { get; set; }

       /// <summary>
       /// 记录修改时间
       /// </summary>
       [Display(Name ="修改时间")]
       [Column(TypeName="datetime")]
       public DateTime? ModifyDate { get; set; }

       /// <summary>
       /// 审核状态
       /// 可能的值:
       /// 0: 待审核 (Pending)
       /// 1: 审核通过 (Approved)
       /// 2: 审核未通过 (Rejected)
       /// </summary>
       [Display(Name ="审核状态")]
       [Column(TypeName="int")]
       public int? AuditStatus { get; set; }

       /// <summary>
       /// 审核人名称
       /// </summary>
       [Display(Name ="审核人")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       public string Auditor { get; set; }

       /// <summary>
       /// 审核操作的时间
       /// </summary>
       [Display(Name ="审核时间")]
       [Column(TypeName="datetime")]
       public DateTime? AuditDate { get; set; }

       /// <summary>
       /// 用户最后一次登录系统的时间
       /// </summary>
       [Display(Name ="最后登陆时间")]
       [Column(TypeName="datetime")]
       public DateTime? LastLoginDate { get; set; }

       /// <summary>
       /// 用户最后一次修改密码的时间
       /// </summary>
       [Display(Name ="最后密码修改时间")]
       [Column(TypeName="datetime")]
       public DateTime? LastModifyPwdDate { get; set; }

       /// <summary>
       /// 用户联系地址
       /// </summary>
       [Display(Name ="地址")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string Address { get; set; }

       /// <summary>
       /// 用户备用联系电话或移动电话
       /// </summary>
       [Display(Name ="电话")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Mobile { get; set; }

       /// <summary>
       /// 用户电子邮箱地址
       /// </summary>
       [Display(Name ="Email")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Email { get; set; }

       /// <summary>
       /// 其他备注信息
       /// </summary>
       [Display(Name ="备注")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string Remark { get; set; }

       /// <summary>
       /// 用于排序的序号
       /// </summary>
       [Display(Name ="排序号")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? OrderNo { get; set; }

       
    }
}
