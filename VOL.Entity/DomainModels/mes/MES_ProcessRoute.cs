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
    [Entity(TableCnName = "工线路线",DBServer = "VOLContext")]
    public partial class MES_ProcessRoute:BaseEntity
    {
        /// <summary>
       /// 工艺路线ID (主键)
       /// </summary>
       [Key]
       [Display(Name ="路线ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid RouteID { get; set; }

       /// <summary>
       /// 当前工序ID (外键, 关联MES_Process.ProcessID, 表示此工艺路线中的一个步骤)
       /// </summary>
       [Display(Name ="工序ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? ProcessID { get; set; }

       /// <summary>
       /// 适用此工艺路线的产品ID或编码 (外键, 可能关联产品表)
       /// </summary>
       [Display(Name ="产品ID")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string ProductID { get; set; }

       /// <summary>
       /// 适用此工艺路线的产品名称
       /// </summary>
       [Display(Name ="产品名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string ProductName { get; set; }

       /// <summary>
       /// 当前工序在此产品工艺路线中的顺序号
       /// </summary>
       [Display(Name ="路线顺序")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int RouteSequence { get; set; }

       /// <summary>
       /// 对此工艺路线或步骤的描述
       /// </summary>
       [Display(Name ="路线描述")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string RouteDescription { get; set; }

       /// <summary>
       /// 上一个工序的ID (外键, 关联MES_Process.ProcessID, 用于定义工序流转顺序)
       /// </summary>
       [Display(Name ="前工序ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? PreProcessID { get; set; }

       /// <summary>
       /// 下一个工序的ID (外键, 关联MES_Process.ProcessID, 用于定义工序流转顺序)
       /// </summary>
       [Display(Name ="后工序ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       public Guid? NextProcessID { get; set; }

       /// <summary>
       /// 工艺路线的状态 (例如: 设计中, 已发布, 试运行, 已归档, 已禁用等，具体值参照业务定义)
       /// </summary>
       [Display(Name ="路线状态")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string RouteStatus { get; set; }

       /// <summary>
       /// 此工艺路线的负责人姓名或ID
       /// </summary>
       [Display(Name ="路线责任人")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string RouteResponsible { get; set; }

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