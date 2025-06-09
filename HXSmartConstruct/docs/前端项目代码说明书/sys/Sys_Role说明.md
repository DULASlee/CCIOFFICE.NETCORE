# 角色管理模块说明 (Sys_Role.jsx)

## 模块概述
本文件定义了角色管理模块的前端扩展配置，主要功能包括：
- 角色树形结构展示与管理
- 父子角色关系处理
- 角色数据增删改查
- 自定义事件处理

## 核心配置

### 1. 树形表格配置
```javascript
onInit() {
  this.rowKey = 'Role_Id' // 设置树形表格唯一标识
  this.columns.find(x => x.field == 'ParentId').hidden = true
}
```

### 2. 子节点加载
```javascript
loadTreeChildren(tree, treeNode, resolve) {
  this.http.post(`api/role/getTreeTableChildrenData?roleId=${tree.Role_Id}`)
    .then(result => resolve(result.rows))
}
```

### 3. 标准API接口
- `POST /api/role/getPageData` 获取角色分页数据
- `POST /api/role/getTreeTableChildrenData` 获取子角色
- `POST /api/role/add` 新增角色
- `POST /api/role/update` 修改角色
- `POST /api/role/delete` 删除角色

## 关键功能实现

### 1. 树形结构展示
- 使用el-table实现树形表格
- 通过`loadTreeChildren`方法懒加载子节点
- `rowKey`指定唯一标识字段

### 2. 父子角色关系
```javascript
editFormOptions.forEach(x => {
  x.find(item => item.field == 'ParentId').title = '上级角色'
})
```

## 使用示例

### 1. 获取角色树数据
```javascript
import http from '@/api/http'

http.post('/api/role/getPageData', {
  page: 1,
  rows: 10
}).then(data => {
  // 处理角色数据
})
```

## 注意事项
1. 角色删除需处理子角色关系
2. 树形数据加载需考虑性能优化
3. 权限变更需同步更新相关用户
4. 生产环境需严格校验数据权限
