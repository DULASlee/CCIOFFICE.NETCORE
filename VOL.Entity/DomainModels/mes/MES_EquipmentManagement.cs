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
    [Entity(TableCnName = "设备管理",DBServer = "VOLContext")]
    public partial class MES_EquipmentManagement:BaseEntity
    {
        /// <summary>
       /// 设备ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="设备ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid EquipmentID { get; set; }

       /// <summary>
       /// 设备的唯一编码
       /// </summary>
       [Display(Name ="设备编码")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string EquipmentCode { get; set; }

       /// <summary>
       /// 设备的名称
       /// </summary>
       [Display(Name ="设备名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string EquipmentName { get; set; }

       /// <summary>
       /// 设备类型 (例如: 车床, 铣床, 检测设备等，具体值需参照业务定义)
       /// </summary>
       [Display(Name ="设备类型")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string EquipmentType { get; set; }

       /// <summary>
       /// 设备的制造商名称
       /// </summary>
       [Display(Name ="制造商")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Manufacturer { get; set; }

       /// <summary>
       /// 设备的购买日期
       /// </summary>
       [Display(Name ="购买日期")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? PurchaseDate { get; set; }

       /// <summary>
       /// 设备的保修期限 (单位: 月)
       /// </summary>
       [Display(Name ="保修期（月）")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? WarrantyPeriod { get; set; }

       /// <summary>
       /// 设备在车间或工厂中的具体安装位置
       /// </summary>
       [Display(Name ="安装位置")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string InstallationLocation { get; set; }

       /// <summary>
       /// 设备的当前状态 (例如: 运行中, 停机, 维修中, 保养中, 待报废等，具体值需参照业务定义)
       /// </summary>
       [Display(Name ="设备状态")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string EquipmentStatus { get; set; }

       /// <summary>
       /// 此设备的负责人姓名或ID
       /// </summary>
       [Display(Name ="责任人")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ResponsiblePerson { get; set; }

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