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
    [Entity(TableCnName = "供应商",DBServer = "VOLContext")]
    public partial class MES_Supplier:BaseEntity
    {
        /// <summary>
       /// 供应商ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="供应商ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid SupplierID { get; set; }

       /// <summary>
       /// 供应商的公司或个人名称
       /// </summary>
       [Display(Name ="供应商名")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string SupplierName { get; set; }

       /// <summary>
       /// 供应商的主要联系人姓名
       /// </summary>
       [Display(Name ="联系人")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ContactPerson { get; set; }

       /// <summary>
       /// 联系人的电话号码
       /// </summary>
       [Display(Name ="联系电话")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ContactPhone { get; set; }

       /// <summary>
       /// 联系人的电子邮箱地址
       /// </summary>
       [Display(Name ="邮箱地址")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Email { get; set; }

       /// <summary>
       /// 供应商的通讯或邮寄地址
       /// </summary>
       [Display(Name ="联系地址")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Address { get; set; }

       /// <summary>
       /// 供应商类型 (例如: 原材料供应商, 设备供应商, 服务供应商等，具体值参照业务定义)
       /// </summary>
       [Display(Name ="供应商类型")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string SupplierType { get; set; }

       /// <summary>
       /// 供应商主要供应的产品或服务范围 (具体值参照业务定义)
       /// </summary>
       [Display(Name ="供应范围")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ProductRange { get; set; }

       /// <summary>
       /// 供应商的质量评级 (例如: A级, B级, C级, 合格供应商等，具体值参照业务定义)
       /// </summary>
       [Display(Name ="质量评级")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string QualityRating { get; set; }

       /// <summary>
       /// 其他备注信息
       /// </summary>
       [Display(Name ="备注信息")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Remarks { get; set; }

       /// <summary>
       /// 创建者ID
       /// </summary>
       [Display(Name ="创建人ID")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? CreateID { get; set; }

       /// <summary>
       /// 创建人名称
       /// </summary>
       [Display(Name ="创建人")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Creator { get; set; }

       /// <summary>
       /// 记录创建时间
       /// </summary>
       [Display(Name ="创建时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? CreateDate { get; set; }

       /// <summary>
       /// 修改者ID
       /// </summary>
       [Display(Name ="修改人ID")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? ModifyID { get; set; }

       /// <summary>
       /// 修改人名称
       /// </summary>
       [Display(Name ="修改人")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Modifier { get; set; }

       /// <summary>
       /// 记录修改时间
       /// </summary>
       [Display(Name ="修改时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? ModifyDate { get; set; }

       
    }
}