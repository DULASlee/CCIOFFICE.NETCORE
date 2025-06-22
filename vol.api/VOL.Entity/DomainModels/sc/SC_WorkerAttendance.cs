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
    [Entity(TableCnName = "工人考勤",TableName = "SC_WorkerAttendance")]
    public partial class SC_WorkerAttendance:BaseEntity
    {
        /// <summary>
       ///记录ID
       /// </summary>
       [Key]
       [Display(Name ="记录ID")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid AttendanceID { get; set; }

       /// <summary>
       ///工人ID
       /// </summary>
       [Display(Name ="工人ID")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid WorkerID { get; set; }

       /// <summary>
       ///项目ID
       /// </summary>
       [Display(Name ="项目ID")]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid ProjectID { get; set; }

       /// <summary>
       ///考勤时间
       /// </summary>
       [Display(Name ="考勤时间")]
       [Column(TypeName="date")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime AttendanceDate { get; set; }

       /// <summary>
       ///上班打卡
       /// </summary>
       [Display(Name ="上班打卡")]
       [Column(TypeName="datetime2")]
       [Editable(true)]
       public DateTime? CheckInTime { get; set; }

       /// <summary>
       ///打卡方式
       /// </summary>
       [Display(Name ="打卡方式")]
       [MaxLength(20)]
       [Column(TypeName="nvarchar(20)")]
       [Editable(true)]
       public string CheckInType { get; set; }

       /// <summary>
       ///打卡位置
       /// </summary>
       [Display(Name ="打卡位置")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string CheckInLocation { get; set; }

       /// <summary>
       ///下班打卡时间
       /// </summary>
       [Display(Name ="下班打卡时间")]
       [Column(TypeName="datetime2")]
       [Editable(true)]
       public DateTime? CheckOutTime { get; set; }

       /// <summary>
       ///打卡方式
       /// </summary>
       [Display(Name ="打卡方式")]
       [MaxLength(20)]
       [Column(TypeName="nvarchar(20)")]
       [Editable(true)]
       public string CheckOutType { get; set; }

       /// <summary>
       ///打卡位置
       /// </summary>
       [Display(Name ="打卡位置")]
       [MaxLength(200)]
       [Column(TypeName="nvarchar(200)")]
       [Editable(true)]
       public string CheckOutLocation { get; set; }

       /// <summary>
       ///工作时长
       /// </summary>
       [Display(Name ="工作时长")]
       [Column(TypeName="numeric")]
       [Editable(true)]
       public decimal? WorkHours { get; set; }

       /// <summary>
       ///考勤状态
       /// </summary>
       [Display(Name ="考勤状态")]
       [MaxLength(20)]
       [Column(TypeName="nvarchar(20)")]
       [Editable(true)]
       public string AttendanceStatus { get; set; }

       /// <summary>
       ///加班时长
       /// </summary>
       [Display(Name ="加班时长")]
       [DisplayFormat(DataFormatString="5,2")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       public decimal? OvertimeHours { get; set; }

       /// <summary>
       ///备注
       /// </summary>
       [Display(Name ="备注")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       [Editable(true)]
       public string Remarks { get; set; }

       /// <summary>
       ///创建
       /// </summary>
       [Display(Name ="创建")]
       [MaxLength(8)]
       [Column(TypeName="datetime2(8)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime CreationTime { get; set; }

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