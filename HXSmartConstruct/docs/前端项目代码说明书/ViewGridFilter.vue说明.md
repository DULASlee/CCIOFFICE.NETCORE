# ViewGridFilter组件说明文档

## 组件概述
ViewGridFilter是ViewGrid的高级筛选组件，提供以下功能：
- 多条件组合查询
- 自定义筛选表单
- 筛选条件保存与复用
- 快速筛选功能

## 核心配置项

### 属性
```js
{
  searchFormOptions: [], // 筛选表单项配置
  searchFormFields: {}, // 筛选表单初始值
  queryFields: [], // 快速查询字段
  select2Count: 5 // 下拉选项显示数量
}
```

### 方法
```js
{
  search() // 执行筛选
  reset() // 重置筛选条件
  saveFilter() // 保存当前筛选条件
}
```

## 使用方法示例

### 基本配置
```js
{
  searchFormOptions: [
    {
      field: "UserName",
      type: "text",
      title: "用户名",
      placeholder: "请输入用户名"
    },
    {
      field: "Status",
      type: "select",
      title: "状态",
      data: [
        { key: "1", value: "启用" },
        { key: "0", value: "禁用" }
      ]
    }
  ],
  queryFields: ["UserName", "Phone"] // 快速查询字段
}
```

### 调用筛选
```js
this.$refs.grid.$refs.searchForm.search()
```

## 注意事项
1. searchFormOptions配置与VolForm组件一致
2. 复杂查询条件建议使用高级筛选
3. 筛选条件可通过localStorage自动保存
