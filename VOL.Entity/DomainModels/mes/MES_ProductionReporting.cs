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
    [Entity(TableCnName = "生产报工",TableName = "MES_ProductionReporting",DetailTable =  new Type[] { typeof(MES_ProductionReportingDetail)},DetailTableCnName = "报工明细",DBServer = "VOLContext")]
    public partial class MES_ProductionReporting:BaseEntity
    {
        /// <summary>
       /// 生产报工ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="报工ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid ReportingID { get; set; }

       /// <summary>
       /// 关联的生产订单ID (外键, 关联MES_ProductionOrder.OrderID)
       /// </summary>
       [Display(Name ="订单")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? OrderID { get; set; }

       /// <summary>
       /// 生产报工的唯一单据编号
       /// </summary>
       [Display(Name ="报工单号")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ReportingNumber { get; set; }

       /// <summary>
       /// 执行报工操作的人员姓名或ID
       /// </summary>
       [Display(Name ="报工人")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string ReportedBy { get; set; }

       /// <summary>
       /// 实际报工的时间
       /// </summary>
       [Display(Name ="报工时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? ReportingTime { get; set; }

       /// <summary>
       /// 本次报工的总数量 (合格+不合格)
       /// </summary>
       [Display(Name ="报工数量")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? Total { get; set; }

       /// <summary>
       /// 本次报工中产生的不合格品数量
       /// </summary>
       [Display(Name ="不合格数量")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? RejectedQuantity { get; set; }

       /// <summary>
       /// 本次报工中产生的合格品数量
       /// </summary>
       [Display(Name ="合格数量")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? AcceptedQuantity { get; set; }

       /// <summary>
       /// 本次报工所记录的工时 (单位: 小时)
       /// </summary>
       [Display(Name ="工时(小时)")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       public decimal? ReportHour { get; set; }

       /// <summary>
       /// 报工单的审批状态
       /// 可能的值:
       /// 0: 待提交 (Pending Submission)
       /// 1: 待审核 (Pending Approval)
       /// 2: 已审核 (Approved)
       /// 3: 已驳回 (Rejected)
       /// </summary>
       [Display(Name ="审批状态")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? AuditStatus { get; set; }

       /// <summary>
       /// 执行审批操作的人员姓名或ID
       /// </summary>
       [Display(Name ="审批人")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Auditor { get; set; }

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

       /// <summary>
       /// 关联的生产报工明细列表
       /// </summary>
       [Display(Name ="报工明细")]
       [ForeignKey("ReportingID")]
       public List<MES_ProductionReportingDetail> MES_ProductionReportingDetail { get; set; }


       
    }
}