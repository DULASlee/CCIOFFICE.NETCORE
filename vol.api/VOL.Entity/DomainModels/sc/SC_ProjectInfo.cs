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
    [Entity(TableCnName = "项目信息表",TableName = "SC_ProjectInfo")]
    public partial class SC_ProjectInfo:BaseEntity
    {
        /// <summary>
       ///项目编号
       /// </summary>
       [Key]
       [Display(Name ="项目编号")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid ProjectID { get; set; }

       /// <summary>
       ///项目名称
       /// </summary>
       [Display(Name ="项目名称")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string ProjectName { get; set; }

       /// <summary>
       ///项目地址
       /// </summary>
       [Display(Name ="项目地址")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string Location { get; set; }

       /// <summary>
       ///街道
       /// </summary>
       [Display(Name ="街道")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string Street { get; set; }

       /// <summary>
       ///社区
       /// </summary>
       [Display(Name ="社区")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string Community { get; set; }

       /// <summary>
       ///开始日期
       /// </summary>
       [Display(Name ="开始日期")]
       [Column(TypeName="date")]
       [Editable(true)]
       public DateTime? StartDate { get; set; }

       /// <summary>
       ///结束日期
       /// </summary>
       [Display(Name ="结束日期")]
       [Column(TypeName="date")]
       [Editable(true)]
       public DateTime? EndDate { get; set; }

       /// <summary>
       ///总投资
       /// </summary>
       [Display(Name ="总投资")]
       [DisplayFormat(DataFormatString="18,2")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       public decimal? TotalInvestment { get; set; }

       /// <summary>
       ///投资性质
       /// </summary>
       [Display(Name ="投资性质")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string InvestmentType { get; set; }

       /// <summary>
       ///总劳务
       /// </summary>
       [Display(Name ="总劳务")]
       [DisplayFormat(DataFormatString="18,2")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       public decimal? TotalLaborCost { get; set; }

       /// <summary>
       ///项目状态
       /// </summary>
       [Display(Name ="项目状态")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string ProjectStatus { get; set; }

       /// <summary>
       ///项目分类
       /// </summary>
       [Display(Name ="项目分类")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string ProjectCategory { get; set; }

       /// <summary>
       ///所属行业
       /// </summary>
       [Display(Name ="所属行业")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string Industry { get; set; }

       /// <summary>
       ///项目地点
       /// </summary>
       [Display(Name ="项目地点")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       [Editable(true)]
       public string Address { get; set; }

       /// <summary>
       ///项目经度
       /// </summary>
       [Display(Name ="项目经度")]
       [DisplayFormat(DataFormatString="9,6")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       public decimal? Longitude { get; set; }

       /// <summary>
       ///项目纬度
       /// </summary>
       [Display(Name ="项目纬度")]
       [DisplayFormat(DataFormatString="8,6")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       public decimal? Latitude { get; set; }

       /// <summary>
       ///施工许可证
       /// </summary>
       [Display(Name ="施工许可证")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string LicenseNumber { get; set; }

       /// <summary>
       ///项目监管区域
       /// </summary>
       [Display(Name ="项目监管区域")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string SupervisoryRegion { get; set; }

       /// <summary>
       ///本地项目编码
       /// </summary>
       [Display(Name ="本地项目编码")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string LocalProjectCode { get; set; }

       /// <summary>
       ///行业主管部门
       /// </summary>
       [Display(Name ="行业主管部门")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string IndustryDept { get; set; }

       /// <summary>
       ///行业主管编码
       /// </summary>
       [Display(Name ="行业主管编码")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string IndustryDeptCode { get; set; }

       /// <summary>
       ///合同开始日期
       /// </summary>
       [Display(Name ="合同开始日期")]
       [Column(TypeName="date")]
       [Editable(true)]
       public DateTime? ContractStartDate { get; set; }

       /// <summary>
       ///工程进度
       /// </summary>
       [Display(Name ="工程进度")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string ProjectProgress { get; set; }

       /// <summary>
       ///工程款支付担保
       /// </summary>
       [Display(Name ="工程款支付担保")]
       [Column(TypeName="tinyint")]
       [Editable(true)]
       public byte? HasPaymentGuarantee { get; set; }

       /// <summary>
       ///是否有施工许可证
       /// </summary>
       [Display(Name ="是否有施工许可证")]
       [Column(TypeName="tinyint")]
       [Editable(true)]
       public byte? HasConstructionPermit { get; set; }

       /// <summary>
       ///总包编号
       /// </summary>
       [Display(Name ="总包编号")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? GeneralConTractorID { get; set; }

       /// <summary>
       ///总承包单位
       /// </summary>
       [Display(Name ="总承包单位")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string GeneralConTractorName { get; set; }

       /// <summary>
       ///总承包单位信用编码
       /// </summary>
       [Display(Name ="总承包单位信用编码")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string GeneralConTractorCreditCode { get; set; }

       /// <summary>
       ///项目经理
       /// </summary>
       [Display(Name ="项目经理")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ProjectManager { get; set; }

       /// <summary>
       ///项目经理电话
       /// </summary>
       [Display(Name ="项目经理电话")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string ProjectManagerPhone { get; set; }

       /// <summary>
       ///项目负责人
       /// </summary>
       [Display(Name ="项目负责人")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ProjectResponsible { get; set; }

       /// <summary>
       ///项目负责人电话
       /// </summary>
       [Display(Name ="项目负责人电话")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string ProjectResponsiblePhone { get; set; }

       /// <summary>
       ///中标通知书
       /// </summary>
       [Display(Name ="中标通知书")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       [Editable(true)]
       public string AwardNoticeFileUrl { get; set; }

       /// <summary>
       ///建设单位编号
       /// </summary>
       [Display(Name ="建设单位编号")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? BuilderID { get; set; }

       /// <summary>
       ///建设单位名称
       /// </summary>
       [Display(Name ="建设单位名称")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string BuilderName { get; set; }

       /// <summary>
       ///建设单位信用编码
       /// </summary>
       [Display(Name ="建设单位信用编码")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string BuilderCreditCode { get; set; }

       /// <summary>
       ///分包单位ID
       /// </summary>
       [Display(Name ="分包单位ID")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? LaborSubConTractorID { get; set; }

       /// <summary>
       ///分包单位名称
       /// </summary>
       [Display(Name ="分包单位名称")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string LaborSubConTractorName { get; set; }

       /// <summary>
       ///分包单位信用代码
       /// </summary>
       [Display(Name ="分包单位信用代码")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string LaborSubConTractorCreditCode { get; set; }

       /// <summary>
       ///建设性质
       /// </summary>
       [Display(Name ="建设性质")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string BuildNature { get; set; }

       /// <summary>
       ///建设规模
       /// </summary>
       [Display(Name ="建设规模")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string BuildScale { get; set; }

       /// <summary>
       ///总面积
       /// </summary>
       [Display(Name ="总面积")]
       [DisplayFormat(DataFormatString="18,2")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       public decimal? TotalArea { get; set; }

       /// <summary>
       ///总长度
       /// </summary>
       [Display(Name ="总长度")]
       [DisplayFormat(DataFormatString="18,2")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       public decimal? TotalLength { get; set; }

       /// <summary>
       ///工程用途
       /// </summary>
       [Display(Name ="工程用途")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string ProjectPurpose { get; set; }

       /// <summary>
       ///项目进度类型
       /// </summary>
       [Display(Name ="项目进度类型")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string ProjectProgressType { get; set; }

       /// <summary>
       ///实名制管理员
       /// </summary>
       [Display(Name ="实名制管理员")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string RealNameSupervisor { get; set; }

       /// <summary>
       ///实名制管理员手机
       /// </summary>
       [Display(Name ="实名制管理员手机")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string RealNameSupervisorPhone { get; set; }

       /// <summary>
       ///劳资专管
       /// </summary>
       [Display(Name ="劳资专管")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string LaborSupervisor { get; set; }

       /// <summary>
       ///劳资专管手机
       /// </summary>
       [Display(Name ="劳资专管手机")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string LaborSupervisorPhone { get; set; }

       /// <summary>
       ///主管部门投诉电话
       /// </summary>
       [Display(Name ="主管部门投诉电话")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string DeptComplaintPhone { get; set; }

       /// <summary>
       ///人社劳动监察电话
       /// </summary>
       [Display(Name ="人社劳动监察电话")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string LaborComplaintPhone { get; set; }

       /// <summary>
       ///企业投诉电话
       /// </summary>
       [Display(Name ="企业投诉电话")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string EnterpriseComplaintPhone { get; set; }

       /// <summary>
       ///项目部投诉电话
       /// </summary>
       [Display(Name ="项目部投诉电话")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string ProjectDeptComplaintPhone { get; set; }

       /// <summary>
       ///扩展信息
       /// </summary>
       [Display(Name ="扩展信息")]
       [Column(TypeName="nvarchar(max)")]
       [Editable(true)]
       public string ExtraInfo { get; set; }

       /// <summary>
       ///创建人ID
       /// </summary>
       [Display(Name ="创建人ID")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? CreatorId { get; set; }

       /// <summary>
       ///创建时间
       /// </summary>
       [Display(Name ="创建时间")]
       [Column(TypeName="datetime2")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime CreationTime { get; set; }

       /// <summary>
       ///最后修改时间
       /// </summary>
       [Display(Name ="最后修改时间")]
       [Column(TypeName="datetime2")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime LastModifiedTime { get; set; }

       /// <summary>
       ///是否删除
       /// </summary>
       [Display(Name ="是否删除")]
       [Column(TypeName="bit")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public bool IsDeleted { get; set; }

       
    }
}