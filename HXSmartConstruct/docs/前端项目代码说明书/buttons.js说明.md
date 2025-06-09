# 按钮配置说明 (buttons.js)

## 文件概述
本文件定义了项目中所有标准按钮的配置，包括：
- 基础操作按钮（查询、新增、编辑、删除等）
- 按钮样式配置
- 点击事件处理
- 权限标识配置

## 按钮配置结构

### 1. 基础属性
```javascript
{
  name: '查询',      // 按钮显示文本
  value: 'Search',   // 权限标识(需与后端一致)
  icon: 'el-icon-search', // 图标类名
  type: 'primary',   // 按钮类型
  color: '#5b64ff',  // 自定义颜色
  plain: true        // 是否朴素按钮
}
```

### 2. 事件绑定
```javascript
onClick: function() {
  this.search(); // 指向当前组件实例的方法
}
```

## 标准按钮列表

| 按钮名称 | 权限标识 | 图标 | 类型 | 默认颜色 |
|---------|---------|------|------|---------|
| 查询 | Search | el-icon-search | primary | #5b64ff |
| 新建 | Add | el-icon-plus | - | #3F51B5 | 
| 编辑 | Update | el-icon-edit | primary | #79bbff |
| 删除 | Delete | el-icon-delete | danger | #F56C6C |
| 审核 | Audit | el-icon-check | primary | - |
| 反审 | AntiAudit | el-icon-finished | - | #67C23A |
| 导入 | Import | el-icon-top | success | - |
| 导出 | Export | el-icon-bottom | primary | - |

## 使用示例

### 1. 基础使用
```javascript
import buttons from '@/api/buttons'

// 获取有权限的按钮
const visibleButtons = buttons.filter(btn => {
  return hasPermission(btn.value)
})
```

### 2. 自定义按钮
```javascript
const customBtn = {
  name: '自定义',
  value: 'Custom',
  icon: 'el-icon-star-on',
  onClick() {
    this.customMethod() 
  }
}
```

## 注意事项
1. 按钮的value值必须与后端权限配置一致
2. 颜色优先级：color > type
3. 新增按钮需同步更新权限配置
4. 按钮事件需在当前组件实现对应方法
