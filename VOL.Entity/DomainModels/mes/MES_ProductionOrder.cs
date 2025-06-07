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
    [Entity(TableCnName = "生产订单",TableName = "MES_ProductionOrder",DetailTable =  new Type[] { typeof(MES_ProductionPlanDetail)},DetailTableCnName = "订单明细",DBServer = "VOLContext")]
    public partial class MES_ProductionOrder:BaseEntity
    {
        /// <summary>
       /// 生产订单ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="订单ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid OrderID { get; set; }

       /// <summary>
       /// 生产订单的唯一编号
       /// </summary>
       [Display(Name ="订单编号")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string OrderNumber { get; set; }

       /// <summary>
       /// 客户的公司或个人名称 (外键, 可能关联MES_Customer.CustomerName)
       /// </summary>
       [Display(Name ="客户名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string CustomerName { get; set; }

       /// <summary>
       /// 生产订单的创建日期
       /// </summary>
       [Display(Name ="订单日期")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime OrderDate { get; set; }

       /// <summary>
       /// 客户要求的或计划的交货日期
       /// </summary>
       [Display(Name ="交货日期")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime DeliveryDate { get; set; }

       /// <summary>
       /// 订单的总数量 (通常是主产品的数量)
       /// </summary>
       [Display(Name ="订单数量")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? OrderQty { get; set; }

       /// <summary>
       /// 订单的优先级 (例如: 高, 中, 低，具体值参照业务定义)
       /// </summary>
       [Display(Name ="优先级")]
       [MaxLength(255)]
       [Column(TypeName="nvarchar(255)")]
       [Editable(true)]
       public string LV { get; set; }

       /// <summary>
       /// 订单的排产状态 (例如: 未排产, 已排产, 生产中, 已完成, 已取消等，具体值参照业务定义)
       /// </summary>
       [Display(Name ="排产状态")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string OrderStatus { get; set; }

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
       public string Modifier { get; set; }

       /// <summary>
       /// 记录修改时间
       /// </summary>
       [Display(Name ="修改时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? ModifyDate { get; set; }

       /// <summary>
       /// 关联的生产计划明细或订单明细列表
       /// </summary>
       [Display(Name ="订单明细")]
       [ForeignKey("OrderID")]
       public List<MES_ProductionPlanDetail> MES_ProductionPlanDetail { get; set; }


       
    }
}