// *Author：codesoft
// *Contact：971926469@qq.com
// *代码由框架生成,任何更改都可能导致被代码生成器覆盖
export default function(){
    const table = {
        key: 'AttendanceID',
        footer: "Foots",
        cnName: '工人考勤',
        name: 'SC_WorkerAttendance',
        url: "/SC_WorkerAttendance/",
        sortName: "AttendanceDate"
    };
    const tableName = table.name;
    const tableCNName = table.cnName;
    const newTabEdit = false;
    const key = table.key;
    const editFormFields = {};
    const editFormOptions = [];
    const searchFormFields = {};
    const searchFormOptions = [];
    const columns = [{field:'AttendanceID',title:'记录ID',type:'guid',width:110,hidden:true,readonly:true,require:true,align:'left'},
                       {field:'WorkerID',title:'工人ID',type:'guid',width:110,require:true,align:'left'},
                       {field:'ProjectID',title:'项目ID',type:'guid',width:110,require:true,align:'left'},
                       {field:'AttendanceDate',title:'考勤时间',type:'datetime',width:150,require:true,align:'left'},
                       {field:'CheckInTime',title:'上班打卡',type:'datetime',width:110,align:'left'},
                       {field:'CheckInType',title:'打卡方式',type:'string',width:110,align:'left'},
                       {field:'CheckInLocation',title:'打卡位置',type:'string',width:180,align:'left'},
                       {field:'CheckOutTime',title:'下班打卡时间',type:'datetime',width:110,align:'left'},
                       {field:'CheckOutType',title:'打卡方式',type:'string',width:110,align:'left'},
                       {field:'CheckOutLocation',title:'打卡位置',type:'string',width:180,align:'left'},
                       {field:'WorkHours',title:'工作时长',type:'decimal',width:110,align:'left'},
                       {field:'AttendanceStatus',title:'考勤状态',type:'string',width:110,align:'left'},
                       {field:'OvertimeHours',title:'加班时长',type:'decimal',width:110,align:'left'},
                       {field:'Remarks',title:'备注',type:'string',width:220,align:'left'},
                       {field:'CreationTime',title:'创建',type:'string ',width:110,require:true,align:'left'},
                       {field:'IsDeleted',title:'是否删除',type:'bool',width:110,require:true,align:'left'}];
    const detail ={columns:[]};
    const details = [];

    return {
        table,
        key,
        tableName,
        tableCNName,
        newTabEdit,
        editFormFields,
        editFormOptions,
        searchFormFields,
        searchFormOptions,
        columns,
        detail,
        details
    };
}