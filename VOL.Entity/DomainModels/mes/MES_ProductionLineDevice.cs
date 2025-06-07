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
    [Entity(TableCnName = "产线设备",DBServer = "VOLContext")]
    public partial class MES_ProductionLineDevice:BaseEntity
    {
        /// <summary>
       /// 产线设备ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="设备ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid DeviceID { get; set; }

       /// <summary>
       /// 所属产线ID (外键, 关联MES_ProductionLine.ProductionLineID)
       /// </summary>
       [Display(Name ="产线ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? ProductionLineID { get; set; }

       /// <summary>
       /// 设备的名称
       /// </summary>
       [Display(Name ="设备名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string DeviceName { get; set; }

       /// <summary>
       /// 设备的型号
       /// </summary>
       [Display(Name ="设备型号")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string DeviceModel { get; set; }

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
       /// 设备的保修截止日期
       /// </summary>
       [Display(Name ="保修日期")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? WarrantyDate { get; set; }

       /// <summary>
       /// 设备的当前状态 (例如: 运行, 停止, 故障, 维护中等，具体值参照业务定义)
       /// </summary>
       [Display(Name ="设备状态")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string Status { get; set; }

       /// <summary>
       /// 设备在生产线上的具体安装位置或工位
       /// </summary>
       [Display(Name ="线上位置")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string LocationOnLine { get; set; }

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

       
    }
}