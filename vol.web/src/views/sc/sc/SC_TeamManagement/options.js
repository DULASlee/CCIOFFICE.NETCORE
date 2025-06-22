// *Author：codesoft
// *Contact：971926469@qq.com
// *代码由框架生成,任何更改都可能导致被代码生成器覆盖
export default function(){
    const table = {
        key: 'TeamID',
        footer: "Foots",
        cnName: '班组信息',
        name: 'SC_TeamManagement',
        url: "/SC_TeamManagement/",
        sortName: "TeamName"
    };
    const tableName = table.name;
    const tableCNName = table.cnName;
    const newTabEdit = false;
    const key = table.key;
    const editFormFields = {};
    const editFormOptions = [];
    const searchFormFields = {};
    const searchFormOptions = [];
    const columns = [{field:'TeamID',title:'班组ID',type:'guid',width:110,hidden:true,readonly:true,require:true,align:'left'},
                       {field:'TeamCode',title:'班组编号',type:'string',width:110,require:true,align:'left'},
                       {field:'TeamName',title:'班组名称',type:'string',width:120,require:true,align:'left'},
                       {field:'TeamType',title:'班组类型',type:'string',width:110,align:'left'},
                       {field:'Status',title:'班组状态',type:'string',width:110,require:true,align:'left'},
                       {field:'ProjectID',title:'项目ID',type:'guid',width:110,require:true,align:'left'},
                       {field:'CompanyID',title:'公司ID',type:'guid',width:110,align:'left'},
                       {field:'ParentTeamID',title:'父班组ID',type:'guid',width:110,align:'left'},
                       {field:'LeaderID',title:'班组长ID',type:'guid',width:110,align:'left'},
                       {field:'LeaderName',title:'班组长姓名',type:'string',width:120,align:'left'},
                       {field:'LeaderPhone',title:'班组长电话',type:'string',width:110,align:'left'},
                       {field:'DeputyLeaderID',title:'副组长ID',type:'guid',width:110,align:'left'},
                       {field:'DeputyLeaderName',title:'副组长姓名',type:'string',width:120,align:'left'},
                       {field:'JobType',title:'班组工种',type:'string',width:110,align:'left'},
                       {field:'TeamSize',title:'班组人数',type:'int',width:110,align:'left'},
                       {field:'MaxSize',title:'最大人数',type:'int',width:110,align:'left'},
                       {field:'MinSize',title:'最小人数',type:'int',width:110,align:'left'},
                       {field:'EstablishDate',title:'成立日期',type:'datetime',width:150,align:'left'},
                       {field:'DissolveDate',title:'解散日期',type:'datetime',width:150,align:'left'},
                       {field:'WorkArea',title:'工作区域',type:'string',width:180,align:'left'},
                       {field:'WorkContent',title:'工作内容',type:'string',width:220,align:'left'},
                       {field:'WorkShift',title:'班次',type:'string',width:110,align:'left'},
                       {field:'SafetyScore',title:'安全评分',type:'decimal',width:110,align:'left'},
                       {field:'QualityScore',title:'质量评分',type:'decimal',width:110,align:'left'},
                       {field:'EfficiencyScore',title:'效率评分',type:'decimal',width:110,align:'left'},
                       {field:'LastEvaluationDate',title:'最后评估日期',type:'datetime',width:150,align:'left'},
                       {field:'Remarks',title:'备注',type:'string',width:220,align:'left'},
                       {field:'CreatorId',title:'创建人ID',type:'guid',width:110,align:'left'},
                       {field:'CreatorName',title:'创建人',type:'string',width:120,align:'left'},
                       {field:'CreationTime',title:'创建时间',type:'datetime',width:110,require:true,align:'left'},
                       {field:'LastModifierId',title:'最后修改ID',type:'guid',width:110,align:'left'},
                       {field:'LastModifierName',title:'最后修改人',type:'string',width:120,align:'left'},
                       {field:'LastModifiedTime',title:'最后修改时间',type:'datetime',width:110,align:'left'},
                       {field:'IsDeleted',title:'是否删除',type:'bool',width:110,require:true,align:'left'},
                       {field:'DeleteTime',title:'删除时间',type:'datetime ',width:110,align:'left'},
                       {field:'DeleterId',title:'删除ID',type:'guid',width:110,align:'left'}];
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