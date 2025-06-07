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
    [Entity(TableCnName = "工序管理",TableName = "MES_Process",DetailTable =  new Type[] { typeof(MES_ProcessRoute)},DetailTableCnName = "工艺路线",DBServer = "VOLContext")]
    public partial class MES_Process:BaseEntity
    {
        /// <summary>
       /// 工序ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="工序ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid ProcessID { get; set; }

       /// <summary>
       /// 工序的唯一编码
       /// </summary>
       [Display(Name ="工序编码")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ProcessCode { get; set; }

       /// <summary>
       /// 工序的名称
       /// </summary>
       [Display(Name ="工序名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ProcessName { get; set; }

       /// <summary>
       /// 工序的类型 (例如: 机加工, 装配, 检验等，具体值参照业务定义)
       /// </summary>
       [Display(Name ="工序类型")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ProcessType { get; set; }

       /// <summary>
       /// 工序在工艺路线中的顺序号
       /// </summary>
       [Display(Name ="工序顺序")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int ProcessSequence { get; set; }

       /// <summary>
       /// 对工序的详细描述
       /// </summary>
       [Display(Name ="工序描述")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ProcessDescription { get; set; }

       /// <summary>
       /// 执行此工序的工作中心或设备组
       /// </summary>
       [Display(Name ="工作中心")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string WorkCenter { get; set; }

       /// <summary>
       /// 完成此工序所需的标准工时 (例如: 小时)
       /// </summary>
       [Display(Name ="标准工时")]
       [DisplayFormat(DataFormatString="10,2")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public decimal StandardWorkingHours { get; set; }

       /// <summary>
       /// 工序的当前状态 (例如: 启用, 禁用, 审核中等，具体值参照业务定义)
       /// </summary>
       [Display(Name ="工序状态")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ProcessStatus { get; set; }

       /// <summary>
       /// 此工序的负责人姓名或ID
       /// </summary>
       [Display(Name ="责任人")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ResponsibleWorker { get; set; }

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
       /// 关联的工艺路线列表 (定义此工序可用于哪些工艺路线)
       /// </summary>
       [Display(Name ="工艺路线")]
       [ForeignKey("ProcessID")]
       public List<MES_ProcessRoute> MES_ProcessRoute { get; set; }


       
    }
}