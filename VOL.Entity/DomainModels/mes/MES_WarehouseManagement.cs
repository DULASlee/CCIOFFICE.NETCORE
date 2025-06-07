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
    [Entity(TableCnName = "仓库管理",TableName = "MES_WarehouseManagement",DBServer = "VOLContext")]
    public partial class MES_WarehouseManagement:BaseEntity
    {
        /// <summary>
       /// 仓库ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="仓库ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid WarehouseID { get; set; }

       /// <summary>
       /// 仓库的唯一编码
       /// </summary>
       [Display(Name ="仓库编码")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string WarehouseCode { get; set; }

       /// <summary>
       /// 仓库的名称
       /// </summary>
       [Display(Name ="仓库名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string WarehouseName { get; set; }

       /// <summary>
       /// 仓库的类型 (例如: 原材料仓, 成品仓, 半成品仓, 不良品仓等，具体值参照业务定义)
       /// </summary>
       [Display(Name ="仓库类型")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string WarehouseType { get; set; }

       /// <summary>
       /// 仓库的实际面积 (例如: 平方米)
       /// </summary>
       [Display(Name ="仓库面积")]
       [DisplayFormat(DataFormatString="10,2")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public decimal WarehouseArea { get; set; }

       /// <summary>
       /// 仓库所在的物理地址
       /// </summary>
       [Display(Name ="仓库地址")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string WarehouseAddress { get; set; }

       /// <summary>
       /// 仓库的联系电话
       /// </summary>
       [Display(Name ="仓库电话")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string WarehousePhone { get; set; }

       /// <summary>
       /// 负责此仓库的管理员姓名或ID
       /// </summary>
       [Display(Name ="仓库管理员")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string WarehouseManager { get; set; }

       /// <summary>
       /// 仓库的当前状态 (例如: 正常, 盘点中, 禁用等，具体值参照业务定义)
       /// </summary>
       [Display(Name ="仓库状态")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string WarehouseStatus { get; set; }

       /// <summary>
       /// 仓库的设计或额定容量 (例如: 存储单元数量, 最大重量等)
       /// </summary>
       [Display(Name ="仓库容量")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int WarehouseCapacity { get; set; }

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