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
    [Entity(TableCnName = "设备故障",DBServer = "VOLContext")]
    public partial class MES_EquipmentFaultRecord:BaseEntity
    {
        /// <summary>
       /// 故障记录ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="故障记录ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid FaultRecordID { get; set; }

       /// <summary>
       /// 发生故障的设备ID (外键, 关联MES_EquipmentManagement.EquipmentID)
       /// </summary>
       [Display(Name ="设备ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? EquipmentID { get; set; }

       /// <summary>
       /// 故障发生的日期
       /// </summary>
       [Display(Name ="故障日期")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? FaultDate { get; set; }

       /// <summary>
       /// 故障的类型 (例如: 机械故障, 电气故障, 软件故障等，具体值需参照业务定义)
       /// </summary>
       [Display(Name ="故障类型")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string FaultType { get; set; }

       /// <summary>
       /// 对故障情况的具体描述
       /// </summary>
       [Display(Name ="故障描述")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string FaultDescription { get; set; }

       /// 故障造成的影响程度或范围 (例如: 停机, 性能下降, 安全隐患等，具体值需参照业务定义)
       /// </summary>
       [Display(Name ="故障影响")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string FaultImpact { get; set; }

       /// <summary>
       /// 报告此故障的人员姓名或ID
       /// </summary>
       [Display(Name ="故障报告人")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string FaultReportedBy { get; set; }

       /// <summary>
       /// 故障当前的处理状态 (例如: 待处理, 处理中, 已解决, 已关闭等，具体值需参照业务定义)
       /// </summary>
       [Display(Name ="故障状态")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string FaultStatus { get; set; }

       /// <summary>
       /// 开始进行故障排查的时间
       /// </summary>
       [Display(Name ="故障排查开始时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? TroubleshootingStartTime { get; set; }

       /// <summary>
       /// 故障排查结束的时间
       /// </summary>
       [Display(Name ="故障排查结束时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? TroubleshootingEndTime { get; set; }

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