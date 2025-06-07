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
    [Entity(TableCnName = "BOM明细",TableName = "MES_Bom_Detail",DBServer = "VOLContext")]
    public partial class MES_Bom_Detail:BaseEntity
    {
        /// <summary>
       /// BOM明细ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid DomDetailId { get; set; }

       /// <summary>
       /// 所属BOM主表ID (外键, 关联MES_Bom_Main.BomId)
       /// </summary>
       [Display(Name ="BomId")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? BomId { get; set; }

       /// <summary>
       /// 在BOM中的序号
       /// </summary>
       [Display(Name ="序号")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int Sequence { get; set; }

       /// <summary>
       /// 子件(组件或原材料)的物料编码
       /// </summary>
       [Display(Name ="子件物料编码")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string MaterialCode { get; set; }

       /// <summary>
       /// 子件(组件或原材料)的物料名称
       /// </summary>
       [Display(Name ="子件物料名称")]
       [MaxLength(300)]
       [Column(TypeName="nvarchar(300)")]
       [Editable(true)]
       public string MaterialName { get; set; }

       /// <summary>
       /// 子件的规格型号
       /// </summary>
       [Display(Name ="规格型号")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string Spec { get; set; }

       /// <summary>
       /// 生产一个父件所需的子件数量 (单台用量)
       /// </summary>
       [Display(Name ="单台用量")]
       [DisplayFormat(DataFormatString="24,3")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public decimal UsageQty { get; set; }

       /// <summary>
       /// 子件的消耗方式 (例如: 领料, 线边库等，具体值需参照业务定义)
       /// </summary>
       [Display(Name ="消耗方式")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string ConsumeModel { get; set; }

       /// <summary>
       /// 子件投料的仓库编码或名称
       /// </summary>
       [Display(Name ="投料仓库")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Warehouse { get; set; }

       /// <summary>
       /// 子件的供应商编码或名称
       /// </summary>
       [Display(Name ="供应商")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string SupplierCode { get; set; }

       /// <summary>
       /// 齐套比例 (用于物料配套检查)
       /// </summary>
       [Display(Name ="齐套比例")]
       [DisplayFormat(DataFormatString="24,3")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       public decimal? KitScale { get; set; }

       /// <summary>
       /// 启用状态
       /// 可能的值:
       /// 1: 启用 (Enabled)
       /// 0: 禁用 (Disabled)
       /// </summary>
       [Display(Name ="启用状态")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? Enable { get; set; }

       /// <summary>
       /// 创建者ID
       /// 创建人名称
       /// </summary>
       [Display(Name ="创建人")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? CreateID { get; set; }

       /// <summary>
       ///创建人
       /// </summary>
       [Display(Name ="创建人")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
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
       [Display(Name ="更新人")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? ModifyID { get; set; }

       /// <summary>
       /// 修改人名称
       /// </summary>
       [Display(Name ="更新人名称")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string Modifier { get; set; }

       /// <summary>
       /// 记录更新时间
       /// </summary>
       [Display(Name ="更新时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? ModifyDate { get; set; }

       
    }
}