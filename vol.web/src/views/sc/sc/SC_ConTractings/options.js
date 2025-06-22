// *Author：codesoft
// *Contact：971926469@qq.com
// *代码由框架生成,任何更改都可能导致被代码生成器覆盖
export default function(){
    const table = {
        key: 'ConTractingID',
        footer: "Foots",
        cnName: '参建公司',
        name: 'SC_ConTractings',
        url: "/SC_ConTractings/",
        sortName: "UnitName"
    };
    const tableName = table.name;
    const tableCNName = table.cnName;
    const newTabEdit = false;
    const key = table.key;
    const editFormFields = {};
    const editFormOptions = [];
    const searchFormFields = {"ConTractingID":"","UnitName":"","UnitType":"","ContactPerson":""};
    const searchFormOptions = [[{"title":"公司ID","field":"ConTractingID","type":"like"},{"title":"公司名称","field":"UnitName","type":"like"}],[{"title":"公司联系人","field":"ContactPerson","type":"like"},{"dataKey":"建设单位类型","data":[],"title":"公司类型","field":"UnitType","type":"select"}]];
    const columns = [{field:'ConTractingID',title:'公司ID',type:'guid',width:110,hidden:true,readonly:true,require:true,align:'left'},
                       {field:'UnitName',title:'公司名称',type:'string',width:180,require:true,align:'left'},
                       {field:'CreditCode',title:'信用代码',type:'string',width:110,align:'left'},
                       {field:'UnitType',title:'公司类型',type:'string',bind:{ key:'建设单位类型',data:[]},width:110,align:'left'},
                       {field:'Address',title:'公司地址',type:'string',width:220,align:'left'},
                       {field:'ContactPerson',title:'公司联系人',type:'string',width:110,align:'left'},
                       {field:'ContactPhone',title:'联系电话',type:'string',width:110,align:'left'},
                       {field:'CreatorId',title:'创建人ID',type:'guid',width:110,align:'left'},
                       {field:'CreationTime',title:'创建时间',type:'datetime ',width:110,require:true,align:'left'},
                       {field:'LastModifiedTime',title:'最后修改时间',type:'datetime',width:110,require:true,align:'left'},
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