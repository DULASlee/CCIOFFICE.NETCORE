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
    [Entity(TableCnName = "定时任务",TableName = "Sys_QuartzOptions")]
    public partial class Sys_QuartzOptions:BaseEntity
    {
        /// <summary>
       /// 任务ID
       /// </summary>
       [Key]
       [Display(Name ="Id")]
       [Column(TypeName="uniqueidentifier")]
       [Required(AllowEmptyStrings=false)]
       public Guid Id { get; set; }

       /// <summary>
       /// 任务的名称
       /// </summary>
       [Display(Name ="任务名称")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string TaskName { get; set; }

       /// <summary>
       /// 任务所属的分组
       /// </summary>
       [Display(Name ="任务分组")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string GroupName { get; set; }

       /// <summary>
       /// API请求方式
       /// 例如: POST, GET, PUT, DELETE
       /// </summary>
       [Display(Name ="请求方式")]
       [MaxLength(50)]
       [Column(TypeName="varchar(50)")]
       [Editable(true)]
       public string Method { get; set; }

       /// <summary>
       /// API请求超时时间 (单位: 秒)
       /// </summary>
       [Display(Name ="超时时间(秒)")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? TimeOut { get; set; }

       /// <summary>
       /// CRON表达式，用于定义任务的执行计划
       /// </summary>
       [Display(Name ="Corn表达式")]
       [MaxLength(100)]
       [Column(TypeName="varchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string CronExpression { get; set; }

       /// <summary>
       /// 任务执行调用的API的URL地址
       /// </summary>
       [Display(Name ="Url地址")]
       [MaxLength(2000)]
       [Column(TypeName="nvarchar(2000)")]
       [Editable(true)]
       public string ApiUrl { get; set; }

       /// <summary>
       /// POST请求时的参数 (通常为JSON字符串)
       /// </summary>
       [Display(Name ="post参数")]
       [Column(TypeName="nvarchar(max)")]
       [Editable(true)]
       public string PostData { get; set; }

       /// <summary>
       /// API请求认证的Key (用于请求头)
       /// </summary>
       [Display(Name ="AuthKey")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string AuthKey { get; set; }

       /// <summary>
       /// API请求认证的Value (用于请求头)
       /// </summary>
       [Display(Name ="AuthValue")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string AuthValue { get; set; }

       /// <summary>
       /// 任务的描述信息
       /// </summary>
       [Display(Name ="描述")]
       [MaxLength(2000)]
       [Column(TypeName="nvarchar(2000)")]
       [Editable(true)]
       public string Describe { get; set; }

       /// <summary>
       /// 任务最后一次成功执行的时间
       /// </summary>
       [Display(Name ="最后执行执行")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? LastRunTime { get; set; }

       /// <summary>
       /// 任务的当前运行状态
       /// 可能的值 (具体值需参照实际枚举或文档):
       /// 0: 停止 (Stopped)
       /// 1: 运行中 (Running)
       /// 2: 暂停 (Paused)
       /// 3: 异常 (Error)
       /// </summary>
       [Display(Name ="运行状态")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? Status { get; set; }

       /// <summary>
       /// 创建者ID
       /// </summary>
       [Display(Name ="CreateID")]
       [Column(TypeName="int")]
       public int? CreateID { get; set; }

       /// <summary>
       /// 创建人名称
       /// </summary>
       [Display(Name ="创建人")]
       [MaxLength(30)]
       [Column(TypeName="nvarchar(30)")]
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
       [Display(Name ="ModifyID")]
       [Column(TypeName="int")]
       public int? ModifyID { get; set; }

       /// <summary>
       /// 修改者名称
       /// </summary>
       [Display(Name ="Modifier")]
       [MaxLength(30)]
       [Column(TypeName="nvarchar(30)")]
       public string Modifier { get; set; }

       /// <summary>
       /// 记录修改时间
       /// </summary>
       [Display(Name ="修改时间")]
       [Column(TypeName="datetime")]
       public DateTime? ModifyDate { get; set; }

       
    }
}