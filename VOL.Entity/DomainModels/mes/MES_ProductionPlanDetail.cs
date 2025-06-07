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
    [Entity(TableCnName = "订单明细",TableName = "MES_ProductionPlanDetail",DBServer = "VOLContext")]
    public partial class MES_ProductionPlanDetail:BaseEntity
    {
        /// <summary>
       /// 生产计划明细ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="计划明细ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid PlanDetailID { get; set; }

       /// <summary>
       /// 所属生产订单ID (外键, 关联MES_ProductionOrder.OrderID)
       /// </summary>
       [Display(Name ="订单ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? OrderID { get; set; }

       /// <summary>
       /// 计划生产的物料编码 (外键, 关联MES_Material.MaterialCode)
       /// </summary>
       [Display(Name ="物料编码")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string MaterialCode { get; set; }

       /// <summary>
       /// 计划生产的物料名称
       /// </summary>
       [Display(Name ="物料名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string MaterialName { get; set; }

       /// <summary>
       /// 计划生产的物料规格型号
       /// </summary>
       [Display(Name ="物料规格")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string MaterialSpecification { get; set; }

       /// <summary>
       /// 计划数量的单位 (例如: 个, 件, Kg)
       /// </summary>
       [Display(Name ="单位")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Unit { get; set; }

       /// <summary>
       /// 此明细项的计划生产数量
       /// </summary>
       [Display(Name ="订单数量")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? PlanQuantity { get; set; }

       /// <summary>
       /// 计划开始生产的时间
       /// </summary>
       [Display(Name ="计划开始时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? PlannedStartTime { get; set; }

       /// <summary>
       /// 计划结束生产的时间
       /// </summary>
       [Display(Name ="计划结束时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? PlannedEndTime { get; set; }

       /// <summary>
       /// 生产计划明细的状态 (例如: 未开始, 进行中, 已完成, 已暂停, 已取消等，具体值参照业务定义)
       /// </summary>
       [Display(Name ="计划状态")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string PlanStatus { get; set; }

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