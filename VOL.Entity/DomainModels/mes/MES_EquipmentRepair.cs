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
    [Entity(TableCnName = "设备维修",DBServer = "VOLContext")]
    public partial class MES_EquipmentRepair:BaseEntity
    {
        /// <summary>
       /// 维修记录ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="维修ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid RepairID { get; set; }

       /// <summary>
       /// 进行维修的设备ID (外键, 关联MES_EquipmentManagement.EquipmentID)
       /// </summary>
       [Display(Name ="设备ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? EquipmentID { get; set; }

       /// <summary>
       /// 维修实际发生的日期
       /// </summary>
       [Display(Name ="维修日期")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? RepairDate { get; set; }

       /// <summary>
       /// 进行本次维修的主要原因或故障现象
       /// </summary>
       [Display(Name ="维修原因")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string RepairReason { get; set; }

       /// <summary>
       /// 本次维修的具体工作内容描述
       /// </summary>
       [Display(Name ="维修内容")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string RepairContent { get; set; }

       /// <summary>
       /// 执行维修的人员姓名或ID
       /// </summary>
       [Display(Name ="维修人员")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string RepairPerson { get; set; }

       /// <summary>
       /// 本次维修所花费的成本 (材料费、人工费等)
       /// </summary>
       [Display(Name ="维修成本")]
       [DisplayFormat(DataFormatString="10,2")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       public decimal? RepairCost { get; set; }

       /// <summary>
       /// 维修工作的当前状态 (例如: 待维修, 维修中, 已完成, 无法修复等，具体值需参照业务定义)
       /// </summary>
       [Display(Name ="维修状态")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string RepairStatus { get; set; }

       /// <summary>
       /// 维修工作实际开始的时间
       /// </summary>
       [Display(Name ="维修开始时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? RepairStartTime { get; set; }

       /// <summary>
       /// 维修工作实际结束的时间
       /// </summary>
       [Display(Name ="维修结束时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? RepairEndTime { get; set; }

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

       
    }
}