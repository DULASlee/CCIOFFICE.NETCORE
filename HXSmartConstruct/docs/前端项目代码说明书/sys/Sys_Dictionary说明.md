# 数据字典管理模块说明 (Sys_Dictionary.jsx)

## 模块概述
本文件定义了数据字典管理模块的前端扩展配置，主要功能包括：
- 字典分类管理
- 字典项维护
- 字典值配置
- 字典数据缓存刷新

## 核心配置

### 1. 标准API接口
- `POST /api/Sys_Dictionary/getPageData` 获取字典分页数据
- `POST /api/Sys_Dictionary/add` 新增字典
- `POST /api/Sys_Dictionary/update` 修改字典
- `POST /api/Sys_Dictionary/delete` 删除字典
- `POST /api/Sys_Dictionary/getDictionary` 获取字典键值对

### 2. 字典数据结构
```javascript
{
  Dic_ID: 1,           // 字典ID
  DicName: "性别",     // 字典名称 
  DicValue: "Gender",  // 字典编码
  ParentId: 0,         // 父级ID
  Enable: true,        // 是否启用
  Sort: 1,             // 排序
  Remark: "性别字典"   // 备注
}
```

## 关键功能实现

### 1. 字典数据缓存
```javascript
// 获取字典数据并缓存
http.post('/api/Sys_Dictionary/getDictionary', {
  dicNo: 'Gender'
}).then(data => {
  // 处理字典数据
})
```

### 2. 字典分类树
通过`ParentId`字段维护字典分类层级关系

## 使用示例

### 1. 获取字典列表
```javascript
import http from '@/api/http'

http.post('/api/Sys_Dictionary/getPageData', {
  page: 1,
  rows: 10,
  wheres: [{ name: 'DicName', value: '性别' }]
}).then(data => {
  // 处理字典数据
})
```

### 2. 获取字典键值对
```javascript
http.post('/api/Sys_Dictionary/getDictionary', {
  dicNo: 'Gender'
}).then(data => {
  this.genderOptions = data
})
```

## 注意事项
1. 系统关键字典不可删除
2. 字典编码(DicValue)需保持唯一
3. 字典变更后需刷新前端缓存
4. 生产环境需考虑字典数据量性能
