# ViewGridDicData组件说明文档

## 组件概述
ViewGridDicData是ViewGrid的字典数据处理组件，主要功能包括：
- 自动加载字典数据
- 转换字典值显示
- 处理级联字典
- 缓存字典数据

## 核心配置项

### 字典列配置
```js
{
  field: "Status",
  title: "状态",
  type: "select",
  bind: {
    data: [], // 静态数据
    key: "dicData", // 动态数据key
    url: "/api/Sys_Dictionary/GetDictionary" // 字典数据接口
  }
}
```

### 方法
```js
{
  initDicData() // 初始化字典数据
  getDicText() // 获取字典项文本
  resetDicData() // 重置字典数据
}
```

## 使用方法示例

### 静态字典数据
```js
columns: [
  {
    field: "Gender",
    title: "性别",
    type: "select",
    bind: {
      data: [
        { key: "1", value: "男" },
        { key: "2", value: "女" }
      ]
    }
  }
]
```

### 动态字典数据
```js
columns: [
  {
    field: "Department",
    title: "部门",
    type: "select",
    bind: {
      key: "departmentDic",
      url: "/api/Sys_Department/GetAll"
    }
  }
]
```

## 注意事项
1. 字典数据会自动缓存，避免重复请求
2. 级联字典需要在loadTableAfter回调中处理
3. 动态字典需要先在global中注册key
