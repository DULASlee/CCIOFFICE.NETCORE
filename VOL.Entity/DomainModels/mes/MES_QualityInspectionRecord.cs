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
    [Entity(TableCnName = "质检记录",DBServer = "VOLContext")]
    public partial class MES_QualityInspectionRecord:BaseEntity
    {
        /// <summary>
       /// 质量检验记录ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="检验记录ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid InspectionRecordID { get; set; }

       /// <summary>
       /// 关联的质量检验计划明细ID (外键, 关联MES_QualityInspectionPlanDetail.InspectionPlanDetailID)
       /// </summary>
       [Display(Name ="检验计划明细ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? InspectionPlanDetailID { get; set; }

       /// <summary>
       /// 本次检验的唯一单据编号 (可能与检验计划单号关联)
       /// </summary>
       [Display(Name ="检验单号")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string InspectionNumber { get; set; }

       /// <summary>
       /// 执行检验操作的人员姓名或ID
       /// </summary>
       [Display(Name ="检验员")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string Inspector { get; set; }

       /// <summary>
       /// 实际进行检验操作的时间
       /// </summary>
       [Display(Name ="检验时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime InspectionTime { get; set; }

       /// <summary>
       /// 本次实际执行检验的物料数量
       /// </summary>
       [Display(Name ="实际检验数量")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int InspectedQuantity { get; set; }

       /// <summary>
       /// 经检验合格的物料数量
       /// </summary>
       [Display(Name ="合格数量")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int PassedQuantity { get; set; }

       /// <summary>
       /// 经检验不合格的物料数量
       /// </summary>
       [Display(Name ="不合格数量")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int FailedQuantity { get; set; }

       /// <summary>
       /// 本次检验的总体结果
       /// 可能的值:
       /// "合格" (Passed)
       /// "不合格" (Failed)
       /// (也可能包含其他如 "让步接收" 等状态，具体值参照业务定义)
       /// </summary>
       [Display(Name ="检验结果（合格、不合格）")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string InspectionResult { get; set; }

       /// <summary>
       /// 如果检验结果为不合格，此处记录缺陷的详细描述
       /// </summary>
       [Display(Name ="缺陷描述")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string DefectDescription { get; set; }

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