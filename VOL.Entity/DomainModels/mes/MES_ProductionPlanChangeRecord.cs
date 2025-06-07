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
    [Entity(TableCnName = "变更记录",TableName = "MES_ProductionPlanChangeRecord",DBServer = "VOLContext")]
    public partial class MES_ProductionPlanChangeRecord:BaseEntity
    {
        /// <summary>
       /// 生产计划变更记录ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="变更记录ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid ChangeRecordID { get; set; }

       /// <summary>
       /// 关联的生产计划明细ID (外键, 关联MES_ProductionPlanDetail.PlanDetailID)
       /// </summary>
       [Display(Name ="计划明细ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? PlanDetailID { get; set; }

       /// <summary>
       /// 相关生产订单的编号
       /// </summary>
       [Display(Name ="订单编号")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string OrderNumber { get; set; }

       /// <summary>
       /// 相关生产订单的客户名称
       /// </summary>
       [Display(Name ="客户名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string CustomerName { get; set; }

       /// <summary>
       /// 相关生产订单的订单日期
       /// </summary>
       [Display(Name ="订单日期")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? OrderDate { get; set; }

       /// <summary>
       /// 生产计划实际发生变更的日期
       /// </summary>
       [Display(Name ="变更日期")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime ChangeDate { get; set; }

       /// <summary>
       /// 变更前的原计划生产数量
       /// </summary>
       [Display(Name ="原计划数量")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int OriginalPlanQuantity { get; set; }

       /// <summary>
       /// 变更后的新计划生产数量
       /// </summary>
       [Display(Name ="新计划数量")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int NewPlanQuantity { get; set; }

       /// <summary>
       /// 变更前的原计划开始生产时间
       /// </summary>
       [Display(Name ="原计划开始时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? OriginalPlannedStartTime { get; set; }

       /// <summary>
       /// 变更后的新计划开始生产时间
       /// </summary>
       [Display(Name ="新计划开始时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? NewPlannedStartTime { get; set; }

       /// <summary>
       /// 变更前的原计划结束生产时间
       /// </summary>
       [Display(Name ="原计划结束时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? OriginalPlannedEndTime { get; set; }

       /// <summary>
       /// 变更后的新计划结束生产时间
       /// </summary>
       [Display(Name ="新计划结束时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? NewPlannedEndTime { get; set; }

       /// <summary>
       /// 生产计划变更的原因 (例如: 客户需求变更, 物料短缺, 设备故障等，具体值参照业务定义)
       /// </summary>
       [Display(Name ="变更原因")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string ChangeReason { get; set; }

       /// <summary>
       /// 执行此次变更操作的人员姓名或ID
       /// </summary>
       [Display(Name ="变更人")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string ChangedBy { get; set; }

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