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
    [Entity(TableCnName = "项目工人",TableName = "SC_WorkerBaseInfo")]
    public partial class SC_WorkerBaseInfo:BaseEntity
    {
        /// <summary>
       ///工人ID
       /// </summary>
       [Key]
       [Display(Name ="工人ID")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid ID { get; set; }

       /// <summary>
       ///编号
       /// </summary>
       [Display(Name ="编号")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string WorkerCode { get; set; }

       /// <summary>
       ///姓名
       /// </summary>
       [Display(Name ="姓名")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string WorkerName { get; set; }

       /// <summary>
       ///身份证号
       /// </summary>
       [Display(Name ="身份证号")]
       [MaxLength(18)]
       [Column(TypeName="nvarchar(18)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string IdCardNumber { get; set; }

       /// <summary>
       ///证件类型
       /// </summary>
       [Display(Name ="证件类型")]
       [MaxLength(20)]
       [Column(TypeName="nvarchar(20)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string IdCardType { get; set; }

       /// <summary>
       ///性别
       /// </summary>
       [Display(Name ="性别")]
       [MaxLength(10)]
       [Column(TypeName="nvarchar(10)")]
       [Editable(true)]
       public string Gender { get; set; }

       /// <summary>
       ///出生
       /// </summary>
       [Display(Name ="出生")]
       [Column(TypeName="date")]
       [Editable(true)]
       public DateTime? BirthDate { get; set; }

       /// <summary>
       ///年龄
       /// </summary>
       [Display(Name ="年龄")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? Age { get; set; }

       /// <summary>
       ///手机
       /// </summary>
       [Display(Name ="手机")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string Phone { get; set; }

       /// <summary>
       ///紧急联系人
       /// </summary>
       [Display(Name ="紧急联系人")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string EmergencyContact { get; set; }

       /// <summary>
       ///紧急联系人电话
       /// </summary>
       [Display(Name ="紧急联系人电话")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string EmergencyPhone { get; set; }

       /// <summary>
       ///住址
       /// </summary>
       [Display(Name ="住址")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string CurrentAddress { get; set; }

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
       ///班组ID
       /// </summary>
       [Display(Name ="班组ID")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? TeamID { get; set; }

       /// <summary>
       ///工种
       /// </summary>
       [Display(Name ="工种")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string JobType { get; set; }

       /// <summary>
       ///技能等级
       /// </summary>
       [Display(Name ="技能等级")]
       [MaxLength(20)]
       [Column(TypeName="nvarchar(20)")]
       [Editable(true)]
       public string SkillLevel { get; set; }

       /// <summary>
       ///进场
       /// </summary>
       [Display(Name ="进场")]
       [Column(TypeName="date")]
       [Editable(true)]
       public DateTime? EntryDate { get; set; }

       /// <summary>
       ///出场
       /// </summary>
       [Display(Name ="出场")]
       [Column(TypeName="date")]
       [Editable(true)]
       public DateTime? ExitDate { get; set; }

       /// <summary>
       ///状态
       /// </summary>
       [Display(Name ="状态")]
       [MaxLength(20)]
       [Column(TypeName="nvarchar(20)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string WorkStatus { get; set; }

       /// <summary>
       ///教育
       /// </summary>
       [Display(Name ="教育")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string Education { get; set; }

       /// <summary>
       ///政治面貌
       /// </summary>
       [Display(Name ="政治面貌")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string PoliticalStatus { get; set; }

       /// <summary>
       ///籍贯
       /// </summary>
       [Display(Name ="籍贯")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string NativePlace { get; set; }

       /// <summary>
       ///名族
       /// </summary>
       [Display(Name ="名族")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string Nation { get; set; }

       /// <summary>
       ///婚姻
       /// </summary>
       [Display(Name ="婚姻")]
       [MaxLength(20)]
       [Column(TypeName="nvarchar(20)")]
       [Editable(true)]
       public string MaritalStatus { get; set; }

       /// <summary>
       ///血型
       /// </summary>
       [Display(Name ="血型")]
       [MaxLength(10)]
       [Column(TypeName="nvarchar(10)")]
       [Editable(true)]
       public string BloodType { get; set; }

       /// <summary>
       ///是否有保险
       /// </summary>
       [Display(Name ="是否有保险")]
       [Column(TypeName="bit")]
       [Editable(true)]
       public bool? HasInsurance { get; set; }

       /// <summary>
       ///健康状况
       /// </summary>
       [Display(Name ="健康状况")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string HealthStatus { get; set; }

       /// <summary>
       ///保险编号
       /// </summary>
       [Display(Name ="保险编号")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string InsuranceNumber { get; set; }

       /// <summary>
       ///人脸特征值
       /// </summary>
       [Display(Name ="人脸特征值")]
       [Column(TypeName="varbinary(max)")]
       [Editable(true)]
       public string FaceFeature { get; set; }

       /// <summary>
       ///照片
       /// </summary>
       [Display(Name ="照片")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       [Editable(true)]
       public string PhotoUrl { get; set; }

       /// <summary>
       ///是否实名
       /// </summary>
       [Display(Name ="是否实名")]
       [Column(TypeName="bit")]
       [Editable(true)]
       public bool? IsRealName { get; set; }

       /// <summary>
       ///实名时间
       /// </summary>
       [Display(Name ="实名时间")]
       [Column(TypeName="datetime2")]
       [Editable(true)]
       public DateTime? RealNameTime { get; set; }

       /// <summary>
       ///备注
       /// </summary>
       [Display(Name ="备注")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       [Editable(true)]
       public string Remarks { get; set; }

       /// <summary>
       ///
       /// </summary>
       [Display(Name ="CreatorId")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? CreatorId { get; set; }

       /// <summary>
       ///
       /// </summary>
       [Display(Name ="CreatorName")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string CreatorName { get; set; }

       /// <summary>
       ///
       /// </summary>
       [Display(Name ="CreationTime")]
       [Column(TypeName="datetime2")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime CreationTime { get; set; }

       /// <summary>
       ///
       /// </summary>
       [Display(Name ="LastModifierId")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? LastModifierId { get; set; }

       /// <summary>
       ///
       /// </summary>
       [Display(Name ="LastModifierName")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string LastModifierName { get; set; }

       /// <summary>
       ///
       /// </summary>
       [Display(Name ="LastModifiedTime")]
       [Column(TypeName="datetime2")]
       [Editable(true)]
       public DateTime? LastModifiedTime { get; set; }

       /// <summary>
       ///
       /// </summary>
       [Display(Name ="IsDeleted")]
       [Column(TypeName="bit")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public bool IsDeleted { get; set; }

       /// <summary>
       ///
       /// </summary>
       [Display(Name ="DeleteTime")]
       [Column(TypeName="datetime2")]
       [Editable(true)]
       public DateTime? DeleteTime { get; set; }

       /// <summary>
       ///
       /// </summary>
       [Display(Name ="DeleterId")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? DeleterId { get; set; }

       
    }
}