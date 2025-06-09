# ViewGrid组件说明文档

## 组件概述
ViewGrid是项目中最核心的表格组件，提供完整的CRUD功能及丰富的扩展能力。主要功能包括：
- 数据表格展示与分页
- 高级搜索表单
- 自定义按钮组
- 弹出编辑框
- 明细表格管理
- 数据导入导出
- 审批流程集成

## 核心配置项

### 基本配置
```js
{
  table: {
    key: "主键字段名", // 必填
    cnName: "中文表名", // 必填
    name: "表名", // 必填
    sortName: "排序字段",
    newTabEdit: false // 是否在新标签页编辑
  },
  columns: [] // 表格列配置
}
```

### 搜索表单配置
```js
{
  searchFormOptions: [], // 搜索表单项配置
  searchFormFields: {}, // 搜索表单初始值
  fixedSearchForm: false // 是否固定搜索表单
}
```

### 编辑表单配置
```js
{
  editFormOptions: [], // 编辑表单项配置
  editFormFields: {}, // 编辑表单初始值
  boxOptions: { // 编辑弹窗配置
    width: "800px",
    height: "500px",
    labelWidth: "100px"
  }
}
```

### 明细表格配置
```js
{
  detail: {
    cnName: "明细表中文名",
    columns: [], // 明细表列配置
    url: "明细数据接口",
    pagination: true // 是否分页
  }
}
```

## 使用方法示例

### 基本使用
```vue
<template>
  <view-grid ref="grid" :table="table" :columns="columns"></view-grid>
</template>

<script>
export default {
  data() {
    return {
      table: {
        key: "Id",
        cnName: "用户管理",
        name: "Sys_User"
      },
      columns: [
        { field: "UserName", title: "用户名" },
        { field: "RoleName", title: "角色" }
      ]
    }
  }
}
</script>
```

### 自定义按钮
```js
{
  buttons: [
    {
      name: "导出",
      onClick: () => {
        this.$refs.grid.exportExcel()
      }
    }
  ]
}
```

## 注意事项
1. 必须配置table.key作为主键字段
2. 明细表格需要单独配置接口url
3. 使用审批流程需要额外配置审批相关参数
4. 组件提供了大量扩展点(slot)可覆盖默认行为
