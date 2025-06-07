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
    [Entity(TableCnName = "工序统计",DBServer = "VOLContext")]
    public partial class MES_ProcessReport:BaseEntity
    {
        /// <summary>
       /// 工序汇报ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="汇报ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid ReportID { get; set; }

       /// <summary>
       /// 关联的工序ID (外键, 关联MES_Process.ProcessID)
       /// </summary>
       [Display(Name ="工序ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? ProcessID { get; set; }

       /// <summary>
       /// 汇报的日期
       /// </summary>
       [Display(Name ="汇报日期")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime ReportDate { get; set; }

       /// <summary>
       /// 在此工序上完成的合格品数量
       /// </summary>
       [Display(Name ="完成数量")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int CompletedQuantity { get; set; }

       /// <summary>
       /// 在此工序上产生的不良品数量
       /// </summary>
       [Display(Name ="不良数量")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int DefectiveQuantity { get; set; }

       /// <summary>
       /// 提交此汇报的人员姓名或ID
       /// </summary>
       [Display(Name ="汇报人")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ReportedBy { get; set; }

       /// <summary>
       /// 汇报的状态 (例如: 已提交, 已审核, 已确认等，具体值参照业务定义)
       /// </summary>
       [Display(Name ="汇报状态")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ReportStatus { get; set; }

       /// <summary>
       /// 关于本次汇报的备注信息
       /// </summary>
       [Display(Name ="汇报备注")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ReportRemarks { get; set; }

       /// <summary>
       /// 对应生产批次的工序开始操作时间
       /// </summary>
       [Display(Name ="工序开始时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? StartTime { get; set; }

       /// <summary>
       /// 对应生产批次的工序结束操作时间
       /// </summary>
       [Display(Name ="工序结束时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? EndTime { get; set; }

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