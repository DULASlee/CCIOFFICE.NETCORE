// *Author：codesoft
// *Contact：971926469@qq.com
// *代码由框架生成,任何更改都可能导致被代码生成器覆盖
export default function(){
    const table = {
        key: 'ID',
        footer: "Foots",
        cnName: '项目工人',
        name: 'SC_WorkerBaseInfo',
        url: "/SC_WorkerBaseInfo/",
        sortName: "WorkerName"
    };
    const tableName = table.name;
    const tableCNName = table.cnName;
    const newTabEdit = false;
    const key = table.key;
    const editFormFields = {};
    const editFormOptions = [];
    const searchFormFields = {};
    const searchFormOptions = [];
    const columns = [{field:'ID',title:'工人ID',type:'guid',width:110,hidden:true,readonly:true,require:true,align:'left'},
                       {field:'WorkerCode',title:'编号',type:'string',width:110,require:true,align:'left'},
                       {field:'WorkerName',title:'姓名',type:'string',width:120,require:true,align:'left'},
                       {field:'IdCardNumber',title:'身份证号',type:'string',width:110,require:true,align:'left'},
                       {field:'IdCardType',title:'证件类型',type:'string',width:110,require:true,align:'left'},
                       {field:'Gender',title:'性别',type:'string',width:110,align:'left'},
                       {field:'BirthDate',title:'出生',type:'datetime',width:150,align:'left'},
                       {field:'Age',title:'年龄',type:'int',width:110,align:'left'},
                       {field:'Phone',title:'手机',type:'string',width:110,align:'left'},
                       {field:'EmergencyContact',title:'紧急联系人',type:'string',width:120,align:'left'},
                       {field:'EmergencyPhone',title:'紧急联系人电话',type:'string',width:110,align:'left'},
                       {field:'CurrentAddress',title:'住址',type:'string',width:180,align:'left'},
                       {field:'ProjectID',title:'项目ID',type:'guid',width:110,require:true,align:'left'},
                       {field:'CompanyID',title:'公司ID',type:'guid',width:110,align:'left'},
                       {field:'TeamID',title:'班组ID',type:'guid',width:110,align:'left'},
                       {field:'JobType',title:'工种',type:'string',width:110,align:'left'},
                       {field:'SkillLevel',title:'技能等级',type:'string',width:110,align:'left'},
                       {field:'EntryDate',title:'进场',type:'datetime',width:150,align:'left'},
                       {field:'ExitDate',title:'出场',type:'datetime',width:150,align:'left'},
                       {field:'WorkStatus',title:'状态',type:'string',width:110,require:true,align:'left'},
                       {field:'Education',title:'教育',type:'string',width:110,align:'left'},
                       {field:'PoliticalStatus',title:'政治面貌',type:'string',width:110,align:'left'},
                       {field:'NativePlace',title:'籍贯',type:'string',width:120,align:'left'},
                       {field:'Nation',title:'名族',type:'string',width:110,align:'left'},
                       {field:'MaritalStatus',title:'婚姻',type:'string',width:110,align:'left'},
                       {field:'BloodType',title:'血型',type:'string',width:110,align:'left'},
                       {field:'HasInsurance',title:'是否有保险',type:'bool',width:110,align:'left'},
                       {field:'HealthStatus',title:'健康状况',type:'string',width:110,align:'left'},
                       {field:'InsuranceNumber',title:'保险编号',type:'string',width:110,align:'left'},
                       {field:'FaceFeature',title:'人脸特征值',type:'string',width:110,align:'left'},
                       {field:'PhotoUrl',title:'照片',type:'string',width:220,align:'left'},
                       {field:'IsRealName',title:'是否实名',type:'bool',width:110,align:'left'},
                       {field:'RealNameTime',title:'实名时间',type:'datetime ',width:110,align:'left'},
                       {field:'Remarks',title:'备注',type:'string',width:220,align:'left'},
                       {field:'CreatorId',title:'CreatorId',type:'guid',width:110,align:'left'},
                       {field:'CreatorName',title:'CreatorName',type:'string',width:120,align:'left'},
                       {field:'CreationTime',title:'CreationTime',type:'datetime',width:110,require:true,align:'left'},
                       {field:'LastModifierId',title:'LastModifierId',type:'guid',width:110,align:'left'},
                       {field:'LastModifierName',title:'LastModifierName',type:'string',width:120,align:'left'},
                       {field:'LastModifiedTime',title:'LastModifiedTime',type:'datetime',width:110,align:'left'},
                       {field:'IsDeleted',title:'IsDeleted',type:'bool',width:110,require:true,align:'left'},
                       {field:'DeleteTime',title:'DeleteTime',type:'datetime',width:110,align:'left'},
                       {field:'DeleterId',title:'DeleterId',type:'guid',width:110,align:'left'}];
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