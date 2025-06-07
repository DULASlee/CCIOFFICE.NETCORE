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
    [Entity(TableCnName = "审批节点",TableName = "Sys_WorkFlowTableStep")]
    public partial class Sys_WorkFlowTableStep:BaseEntity
    {
        /// <summary>
       /// 工作流实例步骤ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="Sys_WorkFlowTableStep_Id")]
       [Column(TypeName="uniqueidentifier")]
       [Required(AllowEmptyStrings=false)]
       public Guid Sys_WorkFlowTableStep_Id { get; set; }

       /// <summary>
       /// 关联的工作流实例ID (外键, 对应Sys_WorkFlowTable.WorkFlowTable_Id)
       /// </summary>
       [Display(Name ="主表id")]
       [Column(TypeName="uniqueidentifier")]
       [Required(AllowEmptyStrings=false)]
       public Guid WorkFlowTable_Id { get; set; }

       /// <summary>
       /// 关联的工作流程定义ID (外键, 对应Sys_WorkFlow.WorkFlow_Id)
       /// </summary>
       [Display(Name ="流程id")]
       [Column(TypeName="uniqueidentifier")]
       public Guid? WorkFlow_Id { get; set; }

       /// <summary>
       /// 审批节点的ID (对应Sys_WorkFlowStep.StepId)
       /// </summary>
       [Display(Name ="节点id")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       public string StepId { get; set; }

       /// <summary>
       /// 审批节点的名称
       /// </summary>
       [Display(Name ="节名称")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       public string StepName { get; set; }

       /// <summary>
       /// 审批类型 (与Sys_WorkFlowStep.StepType一致)
       /// 1: 按用户审批
       /// 2: 按角色审批
       /// </summary>
       [Display(Name ="审批类型")]
       [Column(TypeName="int")]
       public int? StepType { get; set; }

       /// <summary>
       /// 审批用户ID或角色ID (根据StepType决定, 对应Sys_WorkFlowStep.StepValue)
       /// </summary>
       [Display(Name ="节点类型(1=按用户审批,2=按角色审批)")]
       [MaxLength(500)]
       [Column(TypeName="varchar(500)")]
       public string StepValue { get; set; }

       /// <summary>
       /// 审批节点的顺序号
       /// </summary>
       [Display(Name ="审批顺序")]
       [Column(TypeName="int")]
       public int? OrderId { get; set; }

       /// <summary>
       /// 备注信息
       /// </summary>
       [Display(Name ="Remark")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       public string Remark { get; set; }

       /// <summary>
       /// 记录创建时间
       /// </summary>
       [Display(Name ="CreateDate")]
       [Column(TypeName="datetime")]
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
       [Display(Name ="Creator")]
       [MaxLength(30)]
       [Column(TypeName="nvarchar(30)")]
       public string Creator { get; set; }

       /// <summary>
       /// 是否启用此步骤实例
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
       /// 实际执行此步骤审核的用户ID (对应Sys_User.User_Id)
       /// </summary>
       [Display(Name ="审核人id")]
       [Column(TypeName="int")]
       public int? AuditId { get; set; }

       /// <summary>
       /// 实际执行此步骤审核的用户名称
       /// </summary>
       [Display(Name ="审核人")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       public string Auditor { get; set; }

       /// <summary>
       /// 此步骤实例的审核状态
       /// 可能的值 (具体需参照业务或枚举定义):
       /// 0: 待审核/待处理 (Pending)
       /// 1: 同意/已处理 (Approved/Processed)
       /// 2: 拒绝/驳回 (Rejected)
       /// </summary>
       [Display(Name ="审核状态")]
       [Column(TypeName="int")]
       public int? AuditStatus { get; set; }

       /// <summary>
       /// 审核操作执行的时间
       /// </summary>
       [Display(Name ="审核时间")]
       [Column(TypeName="datetime")]
       public DateTime? AuditDate { get; set; }

       /// <summary>
       /// 节点属性类型 (对应Sys_WorkFlowStep.StepAttrType)
       /// 可能的值:
       /// "start": 开始节点
       /// "node": 普通审批节点
       /// "end": 结束节点
       /// </summary>
       [Display(Name ="节点属性(start、node、end))")]
       [MaxLength(50)]
       [Column(TypeName="varchar(50)")]
       public string StepAttrType { get; set; }

       /// <summary>
       /// 父级步骤实例的ID (用于并行或分支流程的追溯)
       /// </summary>
       [Display(Name ="ParentId")]
       [MaxLength(2000)]
       [Column(TypeName="varchar(2000)")]
       public string ParentId { get; set; }

       /// <summary>
       /// 下一个实际执行的步骤实例ID (如果流程非线性，可能为空)
       /// </summary>
       [Display(Name ="NextStepId")]
       [MaxLength(100)]
       [Column(TypeName="varchar(100)")]
       public string NextStepId { get; set; }

       /// <summary>
       /// 权重 (可能用于条件分支选择时的排序)
       /// </summary>
       [Display(Name ="Weight")]
       [Column(TypeName="int")]
       public int? Weight { get; set; }

       
    }
}