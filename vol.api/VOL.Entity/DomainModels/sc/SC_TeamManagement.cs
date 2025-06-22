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
    [Entity(TableCnName = "班组信息",TableName = "SC_TeamManagement")]
    public partial class SC_TeamManagement:BaseEntity
    {
        /// <summary>
       ///班组ID
       /// </summary>
       [Key]
       [Display(Name ="班组ID")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid TeamID { get; set; }

       /// <summary>
       ///班组编号
       /// </summary>
       [Display(Name ="班组编号")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string TeamCode { get; set; }

       /// <summary>
       ///班组名称
       /// </summary>
       [Display(Name ="班组名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string TeamName { get; set; }

       /// <summary>
       ///班组类型
       /// </summary>
       [Display(Name ="班组类型")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string TeamType { get; set; }

       /// <summary>
       ///班组状态
       /// </summary>
       [Display(Name ="班组状态")]
       [MaxLength(20)]
       [Column(TypeName="nvarchar(20)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string Status { get; set; }

       /// <summary>
       ///项目ID
       /// </summary>
       [Display(Name ="项目ID")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid ProjectID { get; set; }

       /// <summary>
       ///公司ID
       /// </summary>
       [Display(Name ="公司ID")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? CompanyID { get; set; }

       /// <summary>
       ///父班组ID
       /// </summary>
       [Display(Name ="父班组ID")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? ParentTeamID { get; set; }

       /// <summary>
       ///班组长ID
       /// </summary>
       [Display(Name ="班组长ID")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? LeaderID { get; set; }

       /// <summary>
       ///班组长姓名
       /// </summary>
       [Display(Name ="班组长姓名")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string LeaderName { get; set; }

       /// <summary>
       ///班组长电话
       /// </summary>
       [Display(Name ="班组长电话")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string LeaderPhone { get; set; }

       /// <summary>
       ///副组长ID
       /// </summary>
       [Display(Name ="副组长ID")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? DeputyLeaderID { get; set; }

       /// <summary>
       ///副组长姓名
       /// </summary>
       [Display(Name ="副组长姓名")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string DeputyLeaderName { get; set; }

       /// <summary>
       ///班组工种
       /// </summary>
       [Display(Name ="班组工种")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string JobType { get; set; }

       /// <summary>
       ///班组人数
       /// </summary>
       [Display(Name ="班组人数")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? TeamSize { get; set; }

       /// <summary>
       ///最大人数
       /// </summary>
       [Display(Name ="最大人数")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? MaxSize { get; set; }

       /// <summary>
       ///最小人数
       /// </summary>
       [Display(Name ="最小人数")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? MinSize { get; set; }

       /// <summary>
       ///成立日期
       /// </summary>
       [Display(Name ="成立日期")]
       [Column(TypeName="date")]
       [Editable(true)]
       public DateTime? EstablishDate { get; set; }

       /// <summary>
       ///解散日期
       /// </summary>
       [Display(Name ="解散日期")]
       [Column(TypeName="date")]
       [Editable(true)]
       public DateTime? DissolveDate { get; set; }

       /// <summary>
       ///工作区域
       /// </summary>
       [Display(Name ="工作区域")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string WorkArea { get; set; }

       /// <summary>
       ///工作内容
       /// </summary>
       [Display(Name ="工作内容")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       [Editable(true)]
       public string WorkContent { get; set; }

       /// <summary>
       ///班次
       /// </summary>
       [Display(Name ="班次")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string WorkShift { get; set; }

       /// <summary>
       ///安全评分
       /// </summary>
       [Display(Name ="安全评分")]
       [DisplayFormat(DataFormatString="5,2")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       public decimal? SafetyScore { get; set; }

       /// <summary>
       ///质量评分
       /// </summary>
       [Display(Name ="质量评分")]
       [DisplayFormat(DataFormatString="5,2")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       public decimal? QualityScore { get; set; }

       /// <summary>
       ///效率评分
       /// </summary>
       [Display(Name ="效率评分")]
       [DisplayFormat(DataFormatString="5,2")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       public decimal? EfficiencyScore { get; set; }

       /// <summary>
       ///最后评估日期
       /// </summary>
       [Display(Name ="最后评估日期")]
       [Column(TypeName="date")]
       [Editable(true)]
       public DateTime? LastEvaluationDate { get; set; }

       /// <summary>
       ///备注
       /// </summary>
       [Display(Name ="备注")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       [Editable(true)]
       public string Remarks { get; set; }

       /// <summary>
       ///创建人ID
       /// </summary>
       [Display(Name ="创建人ID")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? CreatorId { get; set; }

       /// <summary>
       ///创建人
       /// </summary>
       [Display(Name ="创建人")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string CreatorName { get; set; }

       /// <summary>
       ///创建时间
       /// </summary>
       [Display(Name ="创建时间")]
       [Column(TypeName="datetime2")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime CreationTime { get; set; }

       /// <summary>
       ///最后修改ID
       /// </summary>
       [Display(Name ="最后修改ID")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? LastModifierId { get; set; }

       /// <summary>
       ///最后修改人
       /// </summary>
       [Display(Name ="最后修改人")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string LastModifierName { get; set; }

       /// <summary>
       ///最后修改时间
       /// </summary>
       [Display(Name ="最后修改时间")]
       [Column(TypeName="datetime2")]
       [Editable(true)]
       public DateTime? LastModifiedTime { get; set; }

       /// <summary>
       ///是否删除
       /// </summary>
       [Display(Name ="是否删除")]
       [Column(TypeName="bit")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public bool IsDeleted { get; set; }

       /// <summary>
       ///删除时间
       /// </summary>
       [Display(Name ="删除时间")]
       [Column(TypeName="datetime2")]
       [Editable(true)]
       public DateTime? DeleteTime { get; set; }

       /// <summary>
       ///删除ID
       /// </summary>
       [Display(Name ="删除ID")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? DeleterId { get; set; }

       
    }
}