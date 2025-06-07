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
    [Entity(TableCnName = "制造BOM",TableName = "MES_Bom_Main",DetailTable =  new Type[] { typeof(MES_Bom_Detail)},DetailTableCnName = "BOM明细",DBServer = "VOLContext")]
    public partial class MES_Bom_Main:BaseEntity
    {
        /// <summary>
       /// BOM主表ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid BomId { get; set; }

       /// <summary>
       /// BOM的唯一编号
       /// </summary>
       [Display(Name ="BOM编号")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string Code { get; set; }

       /// <summary>
       /// 母件(成品或半成品)的物料编码
       /// </summary>
       [Display(Name ="母件物料编码")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string MaterialCode { get; set; }

       /// <summary>
       /// 母件(成品或半成品)的物料名称
       /// </summary>
       [Display(Name ="母件物料名称")]
       [MaxLength(300)]
       [Column(TypeName="nvarchar(300)")]
       [Editable(true)]
       public string MaterialName { get; set; }

       /// <summary>
       /// 母件的规格型号
       /// </summary>
       [Display(Name ="规格型号")]
       [MaxLength(400)]
       [Column(TypeName="nvarchar(400)")]
       [Editable(true)]
       public string Spec { get; set; }

       /// <summary>
       /// BOM的用途 (例如: 生产, 研发, 测试等，具体值需参照业务定义)
       /// </summary>
       [Display(Name ="用途")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string Purpose { get; set; }

       /// <summary>
       /// BOM的版本号
       /// </summary>
       [Display(Name ="版本")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string Edition { get; set; }

       /// <summary>
       /// BOM开始生效的日期
       /// </summary>
       [Display(Name ="有效日期")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime EffectiveDate { get; set; }

       /// <summary>
       /// BOM失效的日期
       /// </summary>
       [Display(Name ="失效日期")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime InvalidDate { get; set; }

       /// <summary>
       /// 启用状态
       /// 可能的值:
       /// 1: 启用 (Enabled)
       /// 0: 禁用 (Disabled)
       /// </summary>
       [Display(Name ="启用状态")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int Enable { get; set; }

       /// <summary>
       /// 创建者ID
       /// 创建人名称
       /// </summary>
       [Display(Name ="创建人")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int CreateID { get; set; }

       /// <summary>
       ///创建人
       /// </summary>
       [Display(Name ="创建人")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string Creator { get; set; }

       /// <summary>
       /// 记录创建时间
       /// </summary>
       [Display(Name ="创建时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime CreateDate { get; set; }

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

       /// <summary>
       /// 关联的BOM明细列表
       /// </summary>
       [Display(Name ="BOM明细")]
       [ForeignKey("BomId")]
       public List<MES_Bom_Detail> MES_Bom_Detail { get; set; }


       
    }
}