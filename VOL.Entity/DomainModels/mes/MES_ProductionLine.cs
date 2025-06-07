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
    [Entity(TableCnName = "产线管理",TableName = "MES_ProductionLine",DetailTable =  new Type[] { typeof(MES_ProductionLineDevice)},DetailTableCnName = "产线设备",DBServer = "VOLContext")]
    public partial class MES_ProductionLine:BaseEntity
    {
        /// <summary>
       /// 产线ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="产线ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid ProductionLineID { get; set; }

       /// <summary>
       /// 生产线的名称
       /// </summary>
       [Display(Name ="产线名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string LineName { get; set; }

       /// <summary>
       /// 生产线的类型 (例如: 装配线, 加工线, 包装线等，具体值参照业务定义)
       /// </summary>
       [Display(Name ="产线类型")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string LineType { get; set; }

       /// <summary>
       /// 生产线的设计产能或额定产能信息 (例如: 件/小时)
       /// </summary>
       [Display(Name ="产能信息")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       public string Capacity { get; set; }

       /// <summary>
       /// 生产线的当前状态 (例如: 运行中, 停线, 维护中, 闲置等，具体值参照业务定义)
       /// </summary>
       [Display(Name ="产线状态")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       public string Status { get; set; }

       /// <summary>
       /// 此生产线的负责人姓名或ID
       /// </summary>
       [Display(Name ="负责人")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ResponsiblePerson { get; set; }

       /// <summary>
       /// 生产线在车间或工厂中的具体位置
       /// </summary>
       [Display(Name ="产线位置")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Location { get; set; }

       /// <summary>
       /// 生产线正式启用的日期
       /// </summary>
       [Display(Name ="启用日期")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? StartDate { get; set; }

       /// <summary>
       /// 生产线停用的日期 (如果已停用)
       /// </summary>
       [Display(Name ="停用日期")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? EndDate { get; set; }

       /// <summary>
       /// 其他备注信息
       /// </summary>
       [Display(Name ="备注信息")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Remarks { get; set; }

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
       /// 关联的产线设备列表 (定义此产线包含哪些设备)
       /// </summary>
       [Display(Name ="产线设备")]
       [ForeignKey("ProductionLineID")]
       public List<MES_ProductionLineDevice> MES_ProductionLineDevice { get; set; }


       
    }
}