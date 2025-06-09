# ViewGridCustomColumn组件说明文档

## 组件概述
ViewGridCustomColumn是ViewGrid的配套组件，用于实现表格列的自定义配置功能，包括：
- 显示/隐藏列
- 调整列顺序
- 固定列位置
- 保存列配置

## 核心配置项

### 属性
```js
props: {
  columns: Array, // 原始列配置
  defaultFields: Array, // 默认显示的列字段
  tableName: String // 表名(用于保存配置)
}
```

### 方法
```js
{
  show() // 显示配置弹窗
}
```

## 使用方法示例

### 基本使用
```vue
<template>
  <view-grid ref="grid">
    <custom-column ref="customColumn"></custom-column>
  </view-grid>
</template>

<script>
export default {
  methods: {
    showColumnConfig() {
      this.$refs.customColumn.show(
        this.columns,
        this.defaultFields,
        "Sys_User"
      )
    }
  }
}
</script>
```

## 注意事项
1. 需要配合ViewGrid组件使用
2. 列配置会本地存储，同表名会复用配置
3. 可通过defaultFields设置默认显示的列
