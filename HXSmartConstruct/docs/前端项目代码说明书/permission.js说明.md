# 权限控制说明 (permission.js)

## 文件概述
本文件实现了前端权限控制核心逻辑，主要功能包括：
- 动态菜单权限获取
- 按钮级权限控制
- 权限验证与过滤
- 异常权限处理

## 核心API说明

### 1. 获取菜单权限
```javascript
/**
 * 获取树形菜单权限
 * @return {Promise} 包含菜单数据的Promise
 */
getMenu()
```

### 2. 获取按钮权限
```javascript
/**
 * 获取过滤后的按钮权限
 * @param {String} path 路由路径 
 * @param {Array} extra 额外按钮配置
 * @param {String} table 表名
 * @param {String} tableName 表名(备用)
 * @return {Array} 过滤后的按钮数组
 */
getButtons(path, extra, table, tableName)
```

## 权限控制实现

### 1. 权限数据结构
从Vuex获取的权限数据格式：
```javascript
{
  path: '/sys/user',
  permission: ['add', 'edit', 'delete'] // 允许的操作
}
```

### 2. 按钮权限过滤
```javascript
let gridButtons = buttons.filter(item => {
  return !item.value || permissions.includes(item.value)
})
```

### 3. 权限验证流程
1. 从Vuex获取当前路由/表的权限配置
2. 如果没有配置则使用默认权限
3. 过滤按钮配置只保留有权限的按钮

## 使用示例

### 1. 获取菜单权限
```javascript
import permission from '@/api/permission'

permission.getMenu().then(menuData => {
  // 处理菜单数据
})
```

### 2. 控制按钮显示
```javascript
const buttons = permission.getButtons(
  this.$route.path, 
  [], 
  this.table.name
)
```

## 注意事项
1. 权限数据应在登录成功后初始化
2. 按钮的value值需与后台权限配置一致
3. 未配置权限的路由默认只显示查询按钮
4. 生产环境需确保权限验证可靠
