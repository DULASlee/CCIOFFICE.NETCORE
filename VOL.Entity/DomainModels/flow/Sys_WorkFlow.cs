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
    [Entity(TableCnName = "审批流程配置",TableName = "Sys_WorkFlow",DetailTable =  new Type[] { typeof(Sys_WorkFlowStep)},DetailTableCnName = "审批步骤")]
    public partial class Sys_WorkFlow:BaseEntity
    {
        /// <summary>
       /// 工作流程ID
       /// </summary>
       [Key]
       [Display(Name ="WorkFlow_Id")]
       [Column(TypeName="uniqueidentifier")]
       [Required(AllowEmptyStrings=false)]
       public Guid WorkFlow_Id { get; set; }

       /// <summary>
       /// 工作流程的名称
       /// </summary>
       [Display(Name ="流程名称")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string WorkName { get; set; }

       /// <summary>
       /// 此工作流程关联的业务表名
       /// </summary>
       [Display(Name ="表名")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string WorkTable { get; set; }

       /// <summary>
       /// 此工作流程关联的业务表对应的功能菜单名称
       /// </summary>
       [Display(Name ="功能菜单")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string WorkTableName { get; set; }

       /// <summary>
       /// 权重，用于多个流程满足相同条件时，权重大的优先匹配
       /// </summary>
       [Display(Name ="权重(相同条件权重大的优先匹配)")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? Weight { get; set; }

       /// <summary>
       /// 是否启用此工作流程
       /// 可能的值:
       /// 1: 启用 (Enabled)
       /// 0: 禁用 (Disabled)
       /// </summary>
       [Display(Name ="是否启用")]
       [Column(TypeName="tinyint")]
       [Editable(true)]
       public byte? Enable { get; set; }

       /// <summary>
       /// 流程节点配置信息 (通常为JSON格式)
       /// </summary>
       [Display(Name ="节点信息")]
       [Column(TypeName="nvarchar(max)")]
       [Editable(true)]
       public string NodeConfig { get; set; }

       /// <summary>
       /// 流程节点连接线配置信息 (通常为JSON格式)
       /// </summary>
       [Display(Name ="连接配置")]
       [Column(TypeName="nvarchar(max)")]
       [Editable(true)]
       public string LineConfig { get; set; }

       /// <summary>
       /// 备注信息
       /// </summary>
       [Display(Name ="备注")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       [Editable(true)]
       public string Remark { get; set; }

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
       public string Creator { get; set; }

       /// <summary>
       /// 记录创建时间
       /// </summary>
       [Display(Name ="创建时间")]
       [Column(TypeName="datetime")]
       public DateTime? CreateDate { get; set; }

       /// <summary>
       /// 修改者ID
       /// </summary>
       [Display(Name ="ModifyID")]
       [Column(TypeName="int")]
       public int? ModifyID { get; set; }

       /// <summary>
       /// 修改人名称
       /// </summary>
       [Display(Name ="修改人")]
       [MaxLength(30)]
       [Column(TypeName="nvarchar(30)")]
       public string Modifier { get; set; }

       /// <summary>
       /// 记录修改时间
       /// </summary>
       [Display(Name ="修改时间")]
       [Column(TypeName="datetime")]
       public DateTime? ModifyDate { get; set; }

       /// <summary>
       /// 审核中的数据是否允许编辑
       /// 可能的值:
       /// 1: 允许 (Allow)
       /// 0: 不允许 (Disallow)
       /// </summary>
       [Display(Name ="审核中数据是否可以编辑")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? AuditingEdit { get; set; }

       /// <summary>
       /// 关联的审批步骤列表
       /// </summary>
       [Display(Name ="审批步骤")]
       [ForeignKey("WorkFlow_Id")]
       public List<Sys_WorkFlowStep> Sys_WorkFlowStep { get; set; }

    }
}