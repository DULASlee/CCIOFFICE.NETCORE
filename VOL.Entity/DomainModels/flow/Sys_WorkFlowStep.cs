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
    [Entity(TableCnName = "审批节点配置",TableName = "Sys_WorkFlowStep")]
    public partial class Sys_WorkFlowStep:BaseEntity
    {
        /// <summary>
       /// 工作流步骤ID
       /// </summary>
       [Key]
       [Display(Name ="WorkStepFlow_Id")]
       [Column(TypeName="uniqueidentifier")]
       [Required(AllowEmptyStrings=false)]
       public Guid WorkStepFlow_Id { get; set; }

       /// <summary>
       /// 所属工作流程ID (外键，关联Sys_WorkFlow.WorkFlow_Id)
       /// </summary>
       [Display(Name ="流程主表id")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? WorkFlow_Id { get; set; }

       /// <summary>
       /// 流程设计器中的步骤ID
       /// </summary>
       [Display(Name ="流程节点Id")]
       [MaxLength(100)]
       [Column(TypeName="varchar(100)")]
       [Editable(true)]
       public string StepId { get; set; }

       /// <summary>
       /// 审批节点的名称
       /// </summary>
       [Display(Name ="节点名称")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string StepName { get; set; }

       /// <summary>
       /// 节点类型
       /// 1: 按用户审批
       /// 2: 按角色审批
       /// </summary>
       [Display(Name ="节点类型(1=按用户审批,2=按角色审批)")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? StepType { get; set; }

       /// <summary>
       /// 审批用户ID或角色ID (根据StepType决定)
       /// </summary>
       [Display(Name ="审批用户id或角色id")]
       [MaxLength(500)]
       [Column(TypeName="varchar(500)")]
       [Editable(true)]
       public string StepValue { get; set; }

       /// <summary>
       /// 备注信息
       /// </summary>
       [Display(Name ="备注")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       [Editable(true)]
       public string Remark { get; set; }

       /// <summary>
       /// 审批节点的顺序号
       /// </summary>
       [Display(Name ="审批顺序")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? OrderId { get; set; }

       /// <summary>
       /// 记录创建时间
       /// </summary>
       [Display(Name ="创建时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? CreateDate { get; set; }

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
       /// 是否启用此步骤
       /// 可能的值:
       /// 1: 启用 (Enabled)
       /// 0: 禁用 (Disabled)
       /// </summary>
       [Display(Name ="Enable")]
       [Column(TypeName="tinyint")]
       public byte? Enable { get; set; }

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
       /// 修改者ID
       /// </summary>
       [Display(Name ="ModifyID")]
       [Column(TypeName="int")]
       public int? ModifyID { get; set; }

       /// <summary>
       /// 下一个审批节点的ID (或ID列表，逗号分隔)
       /// </summary>
       [Display(Name ="下一个审批节点")]
       [MaxLength(500)]
       [Column(TypeName="varchar(500)")]
       [Editable(true)]
       public string NextStepIds { get; set; }

       /// <summary>
       /// 父级节点的ID (或ID列表，用于并行或分支流程)
       /// </summary>
       [Display(Name ="父级节点")]
       [MaxLength(2000)]
       [Column(TypeName="varchar(2000)")]
       [Editable(true)]
       public string ParentId { get; set; }

       /// <summary>
       /// 审核未通过时的处理方式
       /// 可能的值 (具体需参照业务定义):
       /// 1: 返回上一节点 (Return to previous node)
       /// 2: 流程重新开始 (Restart workflow)
       /// 3: 流程结束 (End workflow)
       /// </summary>
       [Display(Name ="审核未通过(返回上一节点,流程重新开始,流程结束)")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? AuditRefuse { get; set; }

       /// <summary>
       /// 驳回时的处理方式
       /// 可能的值 (具体需参照业务定义):
       /// 1: 返回上一节点 (Return to previous node)
       /// 2: 流程重新开始 (Restart workflow)
       /// 3: 流程结束 (End workflow)
       /// </summary>
       [Display(Name ="驳回(返回上一节点,流程重新开始,流程结束)")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? AuditBack { get; set; }

       /// <summary>
       /// 审批方式
       /// 可能的值:
       /// 0: 单人审批 (Single approval)
       /// 1: 会签 (启用会签) (Countersign enabled)
       /// </summary>
       [Display(Name ="审批方式(启用会签)")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? AuditMethod { get; set; }

       /// <summary>
       /// 审核后是否发送邮件通知
       /// 可能的值:
       /// 1: 是 (Yes)
       /// 0: 否 (No)
       /// </summary>
       [Display(Name ="审核后发送邮件通知")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? SendMail { get; set; }

       /// <summary>
       /// 进入此节点的审核条件 (通常为表达式或JSON配置)
       /// </summary>
       [Display(Name ="审核条件")]
       [MaxLength(4000)]
       [Column(TypeName="nvarchar(4000)")]
       [Editable(true)]
       public string Filters { get; set; }

       /// <summary>
       /// 节点属性类型
       /// 可能的值:
       /// "start": 开始节点
       /// "node": 普通审批节点
       /// "end": 结束节点
       /// </summary>
       [Display(Name ="节点属性(start、node、end))")]
       [MaxLength(50)]
       [Column(TypeName="varchar(50)")]
       [Editable(true)]
       public string StepAttrType { get; set; }

       /// <summary>
       /// 权重，用于相同条件的多个后续步骤选择，权重大的优先
       /// </summary>
       [Display(Name ="权重(相同条件权重大的优先匹配)")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? Weight { get; set; }

       
    }
}