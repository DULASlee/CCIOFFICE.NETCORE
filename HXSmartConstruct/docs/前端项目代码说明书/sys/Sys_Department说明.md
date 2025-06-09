# 部门管理模块说明 (Sys_Department.jsx)

## 模块概述
本文件定义了部门管理模块的前端扩展配置，主要功能包括：
- 部门树形结构展示与管理
- 父子部门关系处理
- 自定义操作按钮
- 部门数据增删改查

## 核心配置

### 1. 树形表格配置
```javascript
onInit() {
  this.rowKey = 'DepartmentId' // 部门ID作为唯一标识
  this.rowParentField = "ParentId" // 父部门ID字段
  this.paginationHide = true // 隐藏分页
  this.lazy = false // 非懒加载模式
}
```

### 2. 子部门加载
```javascript
loadTreeChildren(tree, treeNode, resolve) {
  this.http.post(`api/Sys_Department/getTreeTableChildrenData?departmentId=${tree.DepartmentId}`)
    .then(result => resolve(result.rows))
}
```

### 3. 标准API接口
- `POST /api/Sys_Department/getPageData` 获取部门分页数据
- `POST /api/Sys_Department/getTreeTableChildrenData` 获取子部门
- `POST /api/Sys_Department/add` 新增部门
- `POST /api/Sys_Department/update` 修改部门
- `POST /api/Sys_Department/delete` 删除部门

## 关键功能实现

### 1. 自定义操作列
```javascript
onInited() {
  this.columns.push({
    title: '操作',
    field: '操作',
    width: 100,
    fixed: 'right',
    render: (h, { row }) => (
      <div>
        <el-button onClick={() => this.addBtnClick(row)} type="primary" link icon="Plus"/>
        <el-button onClick={() => this.edit(row)} type="success" link icon="Edit"/>
        <el-button onClick={() => this.del(row)} type="danger" link icon="Delete"/>
      </div>
    )
  })
}
```

### 2. 新增部门默认值设置
```javascript
modelOpenAfter(row) {
  if (this.addCurrnetRow) {
    let parentIds = this.base.getTreeAllParent(this.addCurrnetRow.DepartmentId, data)
      .map(x => x.id)
    this.editFormFields.ParentId = parentIds
  }
}
```

## 使用示例

### 1. 获取部门树数据
```javascript
import http from '@/api/http'

http.post('/api/Sys_Department/getPageData', {
  page: 1,
  rows: 1000 // 获取全部部门
}).then(data => {
  // 处理部门数据
})
```

## 注意事项
1. 部门删除需处理子部门关系
2. 树形数据加载需考虑性能优化
3. 部门变更可能影响用户权限
4. 生产环境需严格校验数据权限
