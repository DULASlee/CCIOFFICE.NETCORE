# VolForm 表单组件说明

## 功能概述
VolForm是动态表单组件，提供：
- 可视化表单设计
- 动态表单渲染
- 复杂表单布局
- 集成验证系统

## 文件结构
```
VolForm/
├── index.js           // 组件入口
├── VolForm.vue        // 主组件
├── VolForm.less       // 样式文件
├── VolFormDate.js      // 日期组件
├── VolFormItemRule.js  // 验证规则
└── ...                // 其他功能模块
```

## 核心功能

### 1. 基础表单
```vue
<VolForm
  :form-options="formOptions"
  :form-data="formData"
  @submit="handleSubmit"
/>
```

### 2. 属性配置
| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| formOptions | Object | {} | 表单配置 |
| formData | Object | {} | 表单数据 |
| layout | String | 'horizontal' | 表单布局 |

### 3. 表单配置示例
```javascript
formOptions: {
  formItems: [
    {
      type: 'input',
      label: '用户名',
      field: 'username',
      required: true
    },
    {
      type: 'select',
      label: '角色',
      field: 'role',
      options: [
        { label: '管理员', value: 1 },
        { label: '用户', value: 2 }
      ]
    }
  ]
}
```

## 高级功能

### 1. 动态表单
```javascript
// 根据条件显示/隐藏字段
{
  type: 'input',
  field: 'phone',
  show: formData => formData.region === 'china'
}
```

### 2. 自定义组件
```javascript
// 注册自定义表单组件
VolForm.registerComponent('custom-input', {
  props: ['value'],
  template: `<input v-model="value" class="custom-input">`
})
```

## 使用示例

### 1. 表单提交
```javascript
methods: {
  handleSubmit(isValid, formData) {
    if (isValid) {
      // 提交逻辑
    }
  }
}
```

### 2. 表单验证
```javascript
// 内置验证规则
rules: {
  username: [
    { required: true, message: '必填项' },
    { min: 3, max: 10, message: '长度3-10' }
  ]
}
```

## 注意事项
1. 复杂表单建议分步骤拆解
2. 动态字段注意数据初始化
3. 自定义样式使用scoped
