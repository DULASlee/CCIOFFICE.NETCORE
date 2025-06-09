# 表单设计器模块说明 (formDraggable.vue)

## 模块概述
本文件实现了动态表单设计器功能，主要特点包括：
- 可视化表单设计
- 组件拖拽布局
- 表单配置保存
- 自定义组件扩展

## 核心功能

### 1. 设计器组件集成
```javascript
import VolFormDraggable from "@/components/basic/VolFormDraggable/index.js"

components: {
  VolFormDraggable // 表单设计器核心组件
}
```

### 2. 表单配置保存
```javascript
methods: {
  save(options) {
    // options包含完整的表单配置
    this.$Message.success("配置保存成功")
    console.log(JSON.stringify(options))
  }
}
```

### 3. 自定义组件扩展
```javascript
data() {
  return {
    userComponents: [] // 可扩展的自定义组件
  }
}
```

## 配置数据结构
表单配置包含以下关键属性：
- `fields`: 表单字段配置
- `layout`: 表单布局设置
- `rules`: 表单验证规则
- `options`: 表单全局选项

## 使用示例

### 1. 获取表单配置
```javascript
// 通过save方法获取
save(options) {
  // 将配置提交到后端
  this.http.post('/api/form/save', options)
}
```

### 2. 扩展自定义组件
```javascript
created() {
  this.userComponents = [
    {
      name: '自定义组件',
      component: CustomComponent,
      icon: 'el-icon-star-on'
    }
  ]
}
```

## 注意事项
1. 表单配置需考虑版本兼容性
2. 生产环境需校验表单配置安全性
3. 复杂表单需考虑性能优化
4. 自定义组件需实现标准接口
