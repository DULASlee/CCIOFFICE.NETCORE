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
    [Entity(TableCnName = "库存管理",DBServer = "VOLContext")]
    public partial class MES_InventoryManagement:BaseEntity
    {
        /// <summary>
       /// 库存记录ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="库存ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid InventoryID { get; set; }

       /// <summary>
       /// 相关的业务单据号 (例如: 入库单号, 出库单号)
       /// </summary>
       [Display(Name ="单据号")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string DocumentNo { get; set; }

       /// <summary>
       /// 库存物料的名称
       /// </summary>
       [Display(Name ="物料名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string MaterialName { get; set; }

       /// <summary>
       /// 库存物料的编号
       /// </summary>
       [Display(Name ="物料编号")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string MaterialCode { get; set; }

       /// <summary>
       /// 库存物料的规格型号
       /// </summary>
       [Display(Name ="规格型号")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string SpecificationModel { get; set; }

       /// <summary>
       /// 物料所在的仓库ID (外键, 可能关联仓库表)
       /// </summary>
       [Display(Name ="仓库ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? WarehouseID { get; set; }

       /// <summary>
       /// 物料所在的货位ID (外键, 可能关联货位表 MES_LocationManagement.LocationID)
       /// </summary>
       [Display(Name ="货位ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? LocationID { get; set; }

       /// <summary>
       /// 当前的库存数量
       /// </summary>
       [Display(Name ="库存数量")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int InventoryQuantity { get; set; }

       /// <summary>
       /// 库存数量的单位 (例如: 个, 件, Kg, 米等)
       /// </summary>
       [Display(Name ="库存单位")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string InventoryUnit { get; set; }

       /// <summary>
       /// 库存的总成本或单位成本 (根据业务逻辑确定)
       /// </summary>
       [Display(Name ="库存成本")]
       [DisplayFormat(DataFormatString="10,2")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public decimal InventoryCost { get; set; }

       /// <summary>
       /// 库存的状态 (例如: 合格, 待检, 冻结, 锁定等，具体值需参照业务定义)
       /// </summary>
       [Display(Name ="库存状态")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string InventoryStatus { get; set; }

       /// <summary>
       /// 物料的最近入库日期
       /// </summary>
       [Display(Name ="入库日期")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? InboundDate { get; set; }

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