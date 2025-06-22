/*这是生成的配置信息,任何修改都会被生成覆盖,如果需要修改,请在生成SC_ProjectInfo.vue中修改,searchFormOptions、editFormOptions、columns属性
Author:vol
 QQ:283591387
 Date:2024
*/
export default function(){
		const table = {
			tableName: "SC_ProjectInfo",
			tableCNName: "项目信息表",
			titleField:'AwardNoticeFileUrl',
			key: 'ProjectID',
			sortName: "StartDate"
		}

	    const searchFormFields = {};
	    const searchFormOptions = []
        const editFormFields = {};
        const editFormOptions = [];

		const columns = [{field:'ProjectName',title:'项目名称',type:'string'},
                       {field:'Location',title:'项目地址',type:'string',bind:{ key:'省市字典',data:[]}},
                       {field:'Street',title:'街道',type:'string'},
                       {field:'StartDate',title:'开始日期',type:'datetime'},
                       {field:'TotalInvestment',title:'总投资',type:'decimal'},
                       {field:'TotalLaborCost',title:'总劳务',type:'decimal'},
                       {field:'ProjectStatus',title:'项目状态',type:'string',bind:{ key:'项目状态',data:[]}},
                       {field:'AwardNoticeFileUrl',title:'中标通知书',type:'string',link:true}];

        const detail = {columns:[]};
        const details = [];

    return {
        table,
		searchFormFields,
		searchFormOptions,
        editFormFields,
        editFormOptions,
		columns,
		detail,
		details
    }
}