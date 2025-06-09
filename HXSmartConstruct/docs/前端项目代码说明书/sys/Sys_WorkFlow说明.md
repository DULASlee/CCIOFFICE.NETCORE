# 工作流管理模块说明 (Sys_WorkFlow.jsx)

## 模块概述
本文件定义了工作流管理模块的前端扩展配置，主要功能包括：
- 工作流定义管理
- 自定义工作流操作界面
- 流程状态跟踪
- 与流程引擎的交互

## 核心配置

### 1. 自定义组件
```javascript
components: {
  gridHeader: gridHader, // 工作流专用头部组件
  gridBody: '',
  gridFooter: ''
}
```

### 2. 标准API接口
- `POST /api/Sys_WorkFlow/getPageData` 获取工作流定义
- `POST /api/Sys_WorkFlow/add` 新增工作流
- `POST /api/Sys_WorkFlow/update` 修改工作流
- `POST /api/Sys_WorkFlow/delete` 删除工作流
- `POST /api/Sys_WorkFlow/publish` 发布工作流

## 关键功能实现

### 1. 工作流头部组件
```javascript
import gridHader from './Sys_WorkFlow/WorkFlowGridHeader.vue'
```
该组件包含：
- 工作流状态筛选
- 流程分类管理
- 批量操作按钮

### 2. 流程操作事件
```javascript
modelOpenBeforeAsync(row) {
  this.$refs.gridHeader.open(row) // 打开工作流设计器
  return false // 阻止默认弹窗
}
```

## 使用示例

### 1. 获取工作流列表
```javascript
import http from '@/api/http'

http.post('/api/Sys_WorkFlow/getPageData', {
  page: 1,
  rows: 10,
  wheres: [{ name: 'WorkFlowName', value: '审批' }]
}).then(data => {
  // 处理工作流数据
})
```

## 注意事项
1. 工作流发布后不可修改基础配置
2. 删除工作流需确保无运行中的实例
3. 生产环境需严格校验流程权限
4. 流程变更可能影响历史数据
