// *Author：codesoft
// *Contact：971926469@qq.com
// *代码由框架生成,任何更改都可能导致被代码生成器覆盖
export default function(){
    const table = {
        key: 'ProjectID',
        footer: "Foots",
        cnName: '项目信息表',
        name: 'SC_ProjectInfo',
        url: "/SC_ProjectInfo/",
        sortName: "StartDate"
    };
    const tableName = table.name;
    const tableCNName = table.cnName;
    const newTabEdit = false;
    const key = table.key;
    const editFormFields = {"ProjectName":"","Location":"","Street":"","Community":"","StartDate":"","EndDate":"","TotalInvestment":"","InvestmentType":"","TotalLaborCost":"","ProjectStatus":"","ProjectCategory":"","Industry":"","Address":"","Longitude":"","Latitude":"","LicenseNumber":"","SupervisoryRegion":""};
    const editFormOptions = [[{"title":"项目名称","required":true,"field":"ProjectName","type":"text"}],
                              [{"dataKey":"省市字典","data":[],"title":"项目地址","field":"Location","type":"treeSelect"},
                               {"title":"街道","field":"Street","type":"text"}],
                              [{"title":"社区","field":"Community","type":"text"}],
                              [{"title":"开始日期","field":"StartDate","type":"date"},
                               {"title":"结束日期","field":"EndDate","type":"date"}],
                              [{"title":"总投资","field":"TotalInvestment","type":"text"},
                               {"dataKey":"投资性质","data":[],"title":"投资性质","field":"InvestmentType","type":"select"}],
                              [{"title":"总劳务","field":"TotalLaborCost","type":"text"}],
                              [{"dataKey":"项目状态","data":[],"title":"项目状态","field":"ProjectStatus","type":"select"},
                               {"dataKey":"项目分类","data":[],"title":"项目分类","field":"ProjectCategory","type":"select"}],
                              [{"dataKey":"所属行业","data":[],"title":"所属行业","field":"Industry","type":"select"}],
                              [{"title":"项目地点","field":"Address","type":"text"}],
                              [{"title":"项目经度","field":"Longitude","type":"text"},
                               {"title":"项目纬度","field":"Latitude","type":"text"}],
                              [{"title":"施工许可证","field":"LicenseNumber","type":"file"}],
                              [{"title":"项目监管区域","field":"SupervisoryRegion","type":"text"}]];
    const searchFormFields = {"ProjectName":"","StartDate":"","ProjectStatus":"","Industry":""};
    const searchFormOptions = [[{"title":"项目名称","field":"ProjectName","type":"like"},{"dataKey":"项目状态","data":[],"title":"项目状态","field":"ProjectStatus","type":"select"}],[{"title":"开始日期","field":"StartDate","type":"date"},{"dataKey":"所属行业","data":[],"title":"所属行业","field":"Industry","type":"select"}]];
    const columns = [{field:'ProjectID',title:'项目编号',type:'guid',width:110,hidden:true,readonly:true,require:true,align:'left'},
                       {field:'ProjectName',title:'项目名称',type:'string',sort:true,width:180,require:true,align:'left'},
                       {field:'Location',title:'项目地址',type:'string',bind:{ key:'省市字典',data:[]},width:180,align:'left'},
                       {field:'Street',title:'街道',type:'string',width:180,align:'left'},
                       {field:'Community',title:'社区',type:'string',width:180,align:'left'},
                       {field:'StartDate',title:'开始日期',type:'datetime',sort:true,width:150,align:'left'},
                       {field:'EndDate',title:'结束日期',type:'datetime',sort:true,width:150,align:'left'},
                       {field:'TotalInvestment',title:'总投资',type:'decimal',width:110,align:'left'},
                       {field:'InvestmentType',title:'投资性质',type:'string',bind:{ key:'投资性质',data:[]},width:110,hidden:true,align:'left'},
                       {field:'TotalLaborCost',title:'总劳务',type:'decimal',width:110,align:'left'},
                       {field:'ProjectStatus',title:'项目状态',type:'string',bind:{ key:'项目状态',data:[]},width:110,align:'left'},
                       {field:'ProjectCategory',title:'项目分类',type:'string',bind:{ key:'项目分类',data:[]},width:110,align:'left'},
                       {field:'Industry',title:'所属行业',type:'string',bind:{ key:'所属行业',data:[]},width:110,align:'left'},
                       {field:'Address',title:'项目地点',type:'string',width:220,align:'left'},
                       {field:'Longitude',title:'项目经度',type:'decimal',width:110,align:'left'},
                       {field:'Latitude',title:'项目纬度',type:'decimal',width:110,align:'left'},
                       {field:'LicenseNumber',title:'施工许可证',type:'file',width:120,align:'left'},
                       {field:'SupervisoryRegion',title:'项目监管区域',type:'string',width:180,align:'left'},
                       {field:'LocalProjectCode',title:'本地项目编码',type:'string',width:120,align:'left'},
                       {field:'IndustryDept',title:'行业主管部门',type:'string',width:120,align:'left'},
                       {field:'IndustryDeptCode',title:'行业主管编码',type:'string',width:120,align:'left'},
                       {field:'ContractStartDate',title:'合同开始日期',type:'datetime',width:150,align:'left'},
                       {field:'ProjectProgress',title:'工程进度',type:'string',width:110,align:'left'},
                       {field:'HasPaymentGuarantee',title:'工程款支付担保',type:'byte',width:110,align:'left'},
                       {field:'HasConstructionPermit',title:'是否有施工许可证',type:'byte',width:110,align:'left'},
                       {field:'GeneralConTractorID',title:'总包编号',type:'guid',width:110,align:'left'},
                       {field:'GeneralConTractorName',title:'总承包单位',type:'string',width:180,align:'left'},
                       {field:'GeneralConTractorCreditCode',title:'总承包单位信用编码',type:'string',width:110,align:'left'},
                       {field:'ProjectManager',title:'项目经理',type:'string',width:120,align:'left'},
                       {field:'ProjectManagerPhone',title:'项目经理电话',type:'string',width:110,align:'left'},
                       {field:'ProjectResponsible',title:'项目负责人',type:'string',width:120,align:'left'},
                       {field:'ProjectResponsiblePhone',title:'项目负责人电话',type:'string',width:110,align:'left'},
                       {field:'AwardNoticeFileUrl',title:'中标通知书',type:'string',link:true,width:220,align:'left'},
                       {field:'BuilderID',title:'建设单位编号',type:'guid',width:110,align:'left'},
                       {field:'BuilderName',title:'建设单位名称',type:'string',width:180,align:'left'},
                       {field:'BuilderCreditCode',title:'建设单位信用编码',type:'string',width:110,align:'left'},
                       {field:'LaborSubConTractorID',title:'分包单位ID',type:'guid',width:110,align:'left'},
                       {field:'LaborSubConTractorName',title:'分包单位名称',type:'string',width:180,align:'left'},
                       {field:'LaborSubConTractorCreditCode',title:'分包单位信用代码',type:'string',width:110,align:'left'},
                       {field:'BuildNature',title:'建设性质',type:'string',width:110,align:'left'},
                       {field:'BuildScale',title:'建设规模',type:'string',width:110,align:'left'},
                       {field:'TotalArea',title:'总面积',type:'decimal',width:110,align:'left'},
                       {field:'TotalLength',title:'总长度',type:'decimal',width:110,align:'left'},
                       {field:'ProjectPurpose',title:'工程用途',type:'string',width:110,align:'left'},
                       {field:'ProjectProgressType',title:'项目进度类型',type:'string',width:110,align:'left'},
                       {field:'RealNameSupervisor',title:'实名制管理员',type:'string',width:120,align:'left'},
                       {field:'RealNameSupervisorPhone',title:'实名制管理员手机',type:'string',width:110,align:'left'},
                       {field:'LaborSupervisor',title:'劳资专管',type:'string',width:120,align:'left'},
                       {field:'LaborSupervisorPhone',title:'劳资专管手机',type:'string',width:110,align:'left'},
                       {field:'DeptComplaintPhone',title:'主管部门投诉电话',type:'string',width:110,align:'left'},
                       {field:'LaborComplaintPhone',title:'人社劳动监察电话',type:'string',width:110,align:'left'},
                       {field:'EnterpriseComplaintPhone',title:'企业投诉电话',type:'string',width:110,align:'left'},
                       {field:'ProjectDeptComplaintPhone',title:'项目部投诉电话',type:'string',width:110,align:'left'},
                       {field:'ExtraInfo',title:'扩展信息',type:'string',width:110,hidden:true,align:'left'},
                       {field:'CreatorId',title:'创建人ID',type:'guid',width:110,hidden:true,align:'left'},
                       {field:'CreationTime',title:'创建时间',type:'datetime',width:110,hidden:true,require:true,align:'left'},
                       {field:'LastModifiedTime',title:'最后修改时间',type:'datetime ',width:110,hidden:true,require:true,align:'left'},
                       {field:'IsDeleted',title:'是否删除',type:'bool',width:110,hidden:true,require:true,align:'left'}];
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