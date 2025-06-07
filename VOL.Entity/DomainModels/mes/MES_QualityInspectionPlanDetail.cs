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
    [Entity(TableCnName = "质检明细",TableName = "MES_QualityInspectionPlanDetail",DBServer = "VOLContext")]
    public partial class MES_QualityInspectionPlanDetail:BaseEntity
    {
        /// <summary>
       /// 质量检验计划明细ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="明细ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid InspectionPlanDetailID { get; set; }

       /// <summary>
       /// 所属质量检验计划ID (外键, 关联MES_QualityInspectionPlan.InspectionPlanID)
       /// </summary>
       [Display(Name ="检验id")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? InspectionPlanID { get; set; }

       /// <summary>
       /// 需要检验的物料编码 (外键, 关联MES_Material.MaterialCode)
       /// </summary>
       [Display(Name ="检验物料")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string MaterialCode { get; set; }

       /// <summary>
       /// 需要检验的物料名称
       /// </summary>
       [Display(Name ="物料名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string MaterialName { get; set; }

       /// <summary>
       /// 需要检验的物料规格型号
       /// </summary>
       [Display(Name ="物料规格")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string MaterialSpecification { get; set; }

       /// <summary>
       /// 计划检验的物料数量
       /// </summary>
       [Display(Name ="检验数量")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int QuantityToInspect { get; set; }

       /// <summary>
       /// 采用的检验方法或规程 (具体值参照业务或标准文档定义)
       /// </summary>
       [Display(Name ="检验方法")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string InspectionMethod { get; set; }

       /// <summary>
       /// 检验时依据的技术标准或质量标准 (具体值参照业务或标准文档定义)
       /// </summary>
       [Display(Name ="检验标准")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string InspectionStandard { get; set; }

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