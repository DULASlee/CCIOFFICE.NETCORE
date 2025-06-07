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
    [Entity(TableCnName = "物料包装",DBServer = "VOLContext")]
    public partial class MES_Packaging:BaseEntity
    {
        /// <summary>
       /// 包装规格ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="包装ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid PackagingID { get; set; }

       /// <summary>
       /// 包装规格的名称
       /// </summary>
       [Display(Name ="包装名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string PackagingName { get; set; }

       /// <summary>
       /// 包装类型 (例如: 纸箱, 木箱, 托盘, 袋装等，具体值参照业务定义)
       /// </summary>
       [Display(Name ="包装类型")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string PackagingType { get; set; }

       /// <summary>
       /// 关联的物料ID (指此包装规格适用于哪种物料, 外键, 关联MES_Material.MaterialID)
       /// </summary>
       [Display(Name ="关联物料")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? MaterialID { get; set; }

       /// <summary>
       /// 包装的外部尺寸 (例如: 长*宽*高)
       /// </summary>
       [Display(Name ="包装尺寸")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Size { get; set; }

       /// <summary>
       /// 包装自身的重量
       /// </summary>
       [Display(Name ="包装重量")]
       [DisplayFormat(DataFormatString="10,2")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       public decimal? Weight { get; set; }

       /// <summary>
       /// 此包装规格的成本
       /// </summary>
       [Display(Name ="包装成本")]
       [DisplayFormat(DataFormatString="10,2")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       public decimal? Cost { get; set; }

       /// <summary>
       /// 提供此包装材料的供应商ID (外键, 可能关联供应商表)
       /// </summary>
       [Display(Name ="供应商ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? SupplierID { get; set; }

       /// <summary>
       /// 此包装材料的当前库存数量
       /// </summary>
       [Display(Name ="库存数量")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? StockQuantity { get; set; }

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