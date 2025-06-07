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
    [Entity(TableCnName = "货位管理",DBServer = "VOLContext")]
    public partial class MES_LocationManagement:BaseEntity
    {
        /// <summary>
       /// 货位ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="货位ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid LocationID { get; set; }

       /// <summary>
       /// 货位所属的仓库ID (外键, 可能关联仓库表)
       /// </summary>
       [Display(Name ="所属仓库ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? WarehouseID { get; set; }

       /// <summary>
       /// 货位的唯一编码
       /// </summary>
       [Display(Name ="货位编码")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string LocationCode { get; set; }

       /// <summary>
       /// 货位的描述性名称
       /// </summary>
       [Display(Name ="货位名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string LocationName { get; set; }

       /// <summary>
       /// 货位的类型 (例如: 存储区, 拣选区, 暂存区等，具体值需参照业务定义)
       /// </summary>
       [Display(Name ="货位类型")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string LocationType { get; set; }

       /// <summary>
       /// 货位的容量 (例如: 可以存放的物料数量或体积)
       /// </summary>
       [Display(Name ="货位容量")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int LocationCapacity { get; set; }

       /// <summary>
       /// 货位的当前状态 (例如: 空闲, 已占用, 禁用等，具体值需参照业务定义)
       /// </summary>
       [Display(Name ="货位状态")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string LocationStatus { get; set; }

       /// <summary>
       /// 货位在仓库布局中的行号标识
       /// </summary>
       [Display(Name ="货位行号")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int LocationRow { get; set; }

       /// <summary>
       /// 货位在仓库布局中的列号标识
       /// </summary>
       [Display(Name ="货位列号")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int LocationColumn { get; set; }

       /// <summary>
       /// 货位在仓库布局中的层数或高度标识
       /// </summary>
       [Display(Name ="货位层数")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int LocationFloor { get; set; }

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