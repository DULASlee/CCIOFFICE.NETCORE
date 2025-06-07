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
    [Entity(TableCnName = "审批流程",TableName = "Sys_WorkFlowTable",DetailTable =  new Type[] { typeof(Sys_WorkFlowTableStep)},DetailTableCnName = "审批节点")]
    public partial class Sys_WorkFlowTable:BaseEntity
    {
        /// <summary>
       /// 工作流实例ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="WorkFlowTable_Id")]
       [Column(TypeName="uniqueidentifier")]
       [Required(AllowEmptyStrings=false)]
       public Guid WorkFlowTable_Id { get; set; }

       /// <summary>
       /// 关联的工作流程定义ID (外键，关联Sys_WorkFlow.WorkFlow_Id)
       /// </summary>
       [Display(Name ="流程id")]
       [Column(TypeName="uniqueidentifier")]
       public Guid? WorkFlow_Id { get; set; }

       /// <summary>
       /// 工作流程的名称
       /// </summary>
       [Display(Name ="流程名称")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       public string WorkName { get; set; }

       /// <summary>
       /// 关联的业务表记录的主键ID值
       /// </summary>
       [Display(Name ="表主键id")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       public string WorkTableKey { get; set; }

       /// <summary>
       /// 关联的业务表名
       /// </summary>
       [Display(Name ="表名")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       public string WorkTable { get; set; }

       /// <summary>
       /// 关联的业务表对应的功能菜单或业务描述名称
       /// </summary>
       [Display(Name ="业务名称")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       public string WorkTableName { get; set; }

       /// <summary>
       /// 当前审批节点的ID (关联Sys_WorkFlowStep.StepId)
       /// </summary>
       [Display(Name ="审核节点ID")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       public string CurrentStepId { get; set; }

       /// <summary>
       /// 当前审批节点的名称
       /// </summary>
       [Display(Name ="审核节点名称")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       public string StepName { get; set; }

       /// <summary>
       /// 当前审批节点的顺序ID (此字段标记为"不用", 可能已废弃)
       /// </summary>
       [Display(Name ="不用")]
       [Column(TypeName="int")]
       public int? CurrentOrderId { get; set; }

       /// <summary>
       /// 审批状态
       /// 可能的值 (具体需参照业务或枚举定义):
       /// 0: 待审核 (Pending)
       /// 1: 审核通过 (Approved)
       /// 2: 审核未通过 (Rejected)
       /// 3: 审核中 (In Progress)
       /// 4: 已撤回 (Recalled)
       /// </summary>
       [Display(Name ="审批状态")]
       [Column(TypeName="int")]
       public int? AuditStatus { get; set; }

       /// <summary>
       /// 创建人名称 (发起人)
       /// </summary>
       [Display(Name ="创建人")]
       [MaxLength(30)]
       [Column(TypeName="nvarchar(30)")]
       public string Creator { get; set; }

       /// <summary>
       /// 记录创建时间 (流程发起时间)
       /// </summary>
       [Display(Name ="创建时间")]
       [Column(TypeName="datetime")]
       public DateTime? CreateDate { get; set; }

       /// <summary>
       /// 创建者ID (发起人ID)
       /// </summary>
       [Display(Name ="CreateID")]
       [Column(TypeName="int")]
       public int? CreateID { get; set; }

       /// <summary>
       /// 是否启用 (此字段可能表示流程实例的有效性)
       /// 可能的值:
       /// 1: 启用 (Enabled)
       /// 0: 禁用 (Disabled)
       /// </summary>
       [Display(Name ="Enable")]
       [Column(TypeName="tinyint")]
       public byte? Enable { get; set; }

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
       [Display(Name ="ModifyDate")]
       [Column(TypeName="datetime")]
       public DateTime? ModifyDate { get; set; }

       /// <summary>
       /// 修改者ID
       /// </summary>
       [Display(Name ="ModifyID")]
       [Column(TypeName="int")]
       public int? ModifyID { get; set; }

       /// <summary>
       /// 关联的审批流程的具体步骤实例列表
       /// </summary>
       [Display(Name ="审批节点")]
       [ForeignKey("WorkFlowTable_Id")]
       public List<Sys_WorkFlowTableStep> Sys_WorkFlowTableStep { get; set; }

    }
}