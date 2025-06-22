/*****************************************************************************************
**  Author:codesoft
**  QQ:283591387
**  框架文档： http://v3.volcore.xyz/
*****************************************************************************************/
//此js文件是用来自定义扩展业务代码，在当前[表.vue]文件中也可以实现业务处理

let extension = {
  components: {
    //查询界面扩展组件
    gridHeader: '',
    gridBody: '',
    gridFooter: '',
    //新建、编辑弹出框扩展组件
    modelHeader: '',
    modelBody: '',
    modelRight: '',
    modelFooter: ''
  },
  tableAction: '', //指定某张表的权限(这里填写表名,默认不用填写)
  buttons: { view: [], box: [], detail: [] }, //扩展的按钮
  methods: {
     //下面这些方法可以保留也可以删除
    onInit() {  //框架初始化配置前，
        // 添加操作列（如已存在操作列可合并按钮）
        const actionCol = this.columns.find(col => col.field === 'action');
        if (!actionCol) {
          this.columns.push({
            field: 'action',
            title: '操作',
            type: 'action',
            width: 180,
            align: 'center',
            fixed: 'right',
            formatter: (row) => {
              return `
                <el-button type='default' size='small' class='project-config-btn custom-action-btn'>项目配置</el-button>
                <el-button type='default' size='small' class='workhour-config-btn custom-action-btn'>工时配置</el-button>
              `;
            },
            click: (row, column, event) => {
              if (event.target && event.target.classList.contains('project-config-btn')) {
                this.$emit && this.$emit('showConfig', row, '项目配置');
              } else if (event.target && event.target.classList.contains('workhour-config-btn')) {
                this.$emit && this.$emit('showConfig', row, '工时配置');
              }
            }
          });
        }
        // 添加点击项目名称的导航处理
        this.columns.forEach(column => {
          if (column.field === 'ProjectName') {
            column.click = (row) => {
              this.$router.push({
                path: '/sc/sc/SC_ProjectInfoDetail',
                query: { projectId: row.ProjectID, projectName: row.ProjectName }
              });
            }
          }
        });
    },
    onInited() {
      //框架初始化配置后
      //如果要配置明细表,在此方法操作
      //this.detailOptions.columns.forEach(column=>{ });
    },
    searchBefore(param) {
      //界面查询前,可以给param.wheres添加查询参数
      console.log('查询参数:', param);
      
      // 如果 wheres 是字符串，先转换为数组
      if (typeof param.wheres === 'string') {
        param.wheres = JSON.parse(param.wheres);
      }

      // 确保 param.wheres 是数组
      if (!Array.isArray(param.wheres)) {
        param.wheres = [];
      }
      
      // 处理所有查询条件
      param.wheres = param.wheres.map(where => {
        switch(where.name) {
          case 'ProjectName':
            // 项目名称使用模糊查询
            where.displayType = 'like';
            break;
          case 'ProjectStatus':
            // 确保项目状态的值是字符串
            where.value = where.value?.toString() ?? '';
            break;
          case 'StartDate':
            // 确保日期格式正确
            if (where.value) {
              where.value = new Date(where.value).toISOString();
            }
            break;
          case 'Industry':
            // 确保行业的值是字符串
            where.value = where.value?.toString() ?? '';
            break;
        }
        return where;
      });

      // 将 wheres 转回字符串
      param.wheres = JSON.stringify(param.wheres);
      
      // 调试输出处理后的查询条件
      console.log('处理后的查询参数:', param);
      
      return true;
    },
    searchAfter(result) {
      //查询后，result返回的查询数据,可以在显示到表格前处理表格的值
      console.log('查询结果:', result);
      return true;
    },
    addBefore(formData) {
      //新建保存前formData为对象，包括明细表，可以给给表单设置值，自己输出看formData的值
      return true;
    },
    updateBefore(formData) {
      //编辑保存前formData为对象，包括明细表、删除行的Id
      return true;
    },
    rowClick({ row, column, event }) {
      //查询界面点击行事件
      // this.$refs.table.$refs.table.toggleRowSelection(row); //单击行时选中当前行;
    },
    modelOpenAfter(row) {
      //点击编辑、新建按钮弹出框后，可以在此处写逻辑，如，从后台获取数据
      //(1)判断是编辑还是新建操作： this.currentAction=='Add';
      //(2)给弹出框设置默认值
      //(3)this.editFormFields.字段='xxx';
      //如果需要给下拉框设置默认值，请遍历this.editFormOptions找到字段配置对应data属性的key值
      //看不懂就把输出看：console.log(this.editFormOptions)
    }
  }
};
export default extension;
