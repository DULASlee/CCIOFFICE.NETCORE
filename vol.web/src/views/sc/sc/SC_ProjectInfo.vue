<template>
    <view-grid ref="grid"
               :columns="columns"
             :detail="detail"
             :modal="false"  
             :details="details"
             :editFormFields="editFormFields"
             :editFormOptions="editFormOptions"
             :searchFormFields="searchFormFields"
             :searchFormOptions="searchFormOptions"
             :table="table"
             :onInit="onInit"
             :onInited="onInited"
             :searchBefore="searchBefore"
             :searchAfter="searchAfter"
             :addBefore="addBefore"
             :updateBefore="updateBefore"
             :rowClick="rowClick"
             :modelOpenBefore="modelOpenBefore"
             :modelOpenAfter="modelOpenAfter">
      <!-- 自定义组件数据槽扩展，更多数据槽slot见文档 -->
      <template #gridHeader>
      </template>
        
    </view-grid>
  
    <!-- 配置弹窗 -->
    <el-dialog v-model="configDialog.visible" :title="configDialog.title" width="500px" top="10vh">
      <div style="min-height:400px;display:flex;flex-direction:column;align-items:center;justify-content:center;">
        <div style="font-size:18px;margin-bottom:24px;">项目ID：{{ configDialog.projectId }}</div>
        <div style="font-size:18px;">项目名称：{{ configDialog.projectName }}</div>
        <div style="margin-top:32px;color:#bbb;">（此处为后续业务实现预留）</div>
      </div>
    </el-dialog>
  </template>
  <script setup lang="jsx">
    import viewOptions from './SC_ProjectInfo/options.js'
    import { ref, reactive, getCurrentInstance } from "vue";
  
    const grid = ref(null);
    const { proxy } = getCurrentInstance()
    const { table, editFormFields, editFormOptions, searchFormFields, searchFormOptions, columns, detail, details } = reactive(viewOptions())
    
    //为项目名称列添加点击逻辑
    columns.forEach(col => {
        if (col.field === 'ProjectName') {
            col.formatter = (row) => {
                return `<a class='project-link' style='color:#1a73e8;text-decoration:underline;cursor:pointer;'>${row.ProjectName}</a>`;
            };
            col.click = (row) => {
                sessionStorage.setItem('SC_ProjectInfoDetailRow', JSON.stringify(row));
                proxy.$router.push({
                    path: '/sc/sc/SC_ProjectInfoDetail',
                    query: { projectId: row.ProjectID, projectName: row.ProjectName }
                });
            };
        }
    });
  
    // 添加操作列
    columns.push({
        field: 'operations',
        title: '操作',
        width: 180,
        align: 'center',
        fixed: 'right', // 固定在右侧
        formatter: (row) => {
            return `
                <div class="operation-buttons">
                    <button class="btn-project-config" data-project-id="${row.ProjectID}" data-project-name="${row.ProjectName}">
                        项目配置
                    </button>
                    <button class="btn-worktime-config" data-project-id="${row.ProjectID}" data-project-name="${row.ProjectName}">
                        工时配置
                    </button>
                </div>
            `;
        },
        click: (row, column, event) => {
            const target = event.target;
            
            // 项目配置按钮点击
            if (target.classList.contains('btn-project-config')) {
                handleProjectConfig(row);
            }
            // 工时配置按钮点击
            else if (target.classList.contains('btn-worktime-config')) {
                handleWorktimeConfig(row);
            }
        }
    });
  
    // 项目配置处理函数
    const handleProjectConfig = (row) => {
        console.log('项目配置:', row);
        // 调用弹窗显示函数
        onShowConfig(row, '项目配置');
    };
  
    // 工时配置处理函数
    const handleWorktimeConfig = (row) => {
        console.log('工时配置:', row);
        // 调用弹窗显示函数
        onShowConfig(row, '工时配置');
    };
  
    let gridRef;
    const onInit = async ($vm) => {
        gridRef = $vm;
    }
    const onInited = async () => {
    }
    const searchBefore = async (param) => {
        return true;
    }
    const searchAfter = async (rows, result) => {
      return true;
    }
    const addBefore = async (formData) => {
        return true;
    }
    const updateBefore = async (formData) => {
        return true;
    }
    const rowClick = ({ row, column, event }) => {
    }
    const modelOpenBefore = async (row) => {
        return true;
    }
    const modelOpenAfter = (row) => {
    }
  
    // 配置弹窗相关
    const configDialog = ref({ 
        visible: false, 
        title: '', 
        projectId: '', 
        projectName: '' 
    });
  
    function onShowConfig(row, type) {
        configDialog.value.visible = true;
        configDialog.value.title = type;
        configDialog.value.projectId = row.ProjectID;
        configDialog.value.projectName = row.ProjectName;
    }
  
    defineExpose({})
  </script>
  <style lang="less">
  .detail-panel {
      border: 1px solid #ccc;
      padding: 16px;
      margin-top: 16px;
  }
  .project-link {
    color: #1a73e8;
    text-decoration: underline;
    cursor: pointer;
  }
  .project-link:hover {
    color: #0b53b7;
  }
  
  /* 操作按钮样式 */
  .operation-buttons {
    display: flex;
    gap: 8px;
    justify-content: center;
    align-items: center;
  }
  
  .operation-buttons button {
    padding: 4px 12px;
    border: 1px solid #d9d9d9;
    border-radius: 4px;
    background-color: #fff;
    color: #333;
    font-size: 12px;
    cursor: pointer;
    transition: all 0.3s;
  }
  
  .operation-buttons button:hover {
    border-color: #1a73e8;
    color: #1a73e8;
    background-color: #f0f7ff;
  }
  
  .btn-project-config {
    background-color: #e6f7ff;
    border-color: #91d5ff;
    color: #1890ff;
  }
  
  .btn-project-config:hover {
    background-color: #bae7ff;
    border-color: #40a9ff;
  }
  
  .btn-worktime-config {
    background-color: #f6ffed;
    border-color: #b7eb8f;
    color: #52c41a;
  }
  
  .btn-worktime-config:hover {
    background-color: #d9f7be;
    border-color: #73d13d;
  }
  </style>