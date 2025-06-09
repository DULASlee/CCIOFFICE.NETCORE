# 表单设计组件说明

## 组件概述
表单设计组件提供可视化表单设计功能，主要特点包括：
- 拖拽式表单设计
- 动态表单生成
- 多类型表单控件支持
- 表单验证配置

## 核心组件

### 1. 表单设计器 (VolFormDraggable)
```html
<vol-form-draggable
  :template="formTemplate"
  @form-change="onFormChange"
></vol-form-draggable>
```

### 2. 表单模板配置
```javascript
formTemplate: {
  fields: [
    {
      type: "input",
      label: "用户名",
      field: "username",
      required: true
    },
    {
      type: "select",
      label: "角色",
      field: "role",
      options: ["admin", "user"]
    }
  ]
}
```

## 表单控件类型

### 1. 基础控件
| 类型 | 说明 | 配置项 |
|------|------|--------|
| input | 文本输入 | placeholder, maxlength |
| select | 下拉选择 | options, multiple |
| radio | 单选按钮 | options |
| checkbox | 多选框 | options |
| date | 日期选择 | format, pickerType |

### 2. 高级控件
| 类型 | 说明 | 配置项 |
|------|------|--------|
| upload | 文件上传 | limit, accept |
| editor | 富文本编辑器 | height, toolbar |
| table | 表格编辑 | columns, data |

## 表单验证

### 1. 验证规则配置
```javascript
rules: {
  username: [
    { required: true, message: '请输入用户名' },
    { min: 3, max: 10, message: '长度3-10个字符' }
  ],
  email: [
    { type: 'email', message: '请输入有效邮箱' }
  ]
}
```

### 2. 验证触发方式
```javascript
validateTrigger: {
  input: ['change', 'blur'],
  select: ['change']
}
```

## 动态表单生成

### 1. 生成流程
1. 解析模板配置
2. 生成表单DOM结构
3. 绑定验证规则
4. 初始化表单数据

### 2. 示例代码
```javascript
generateForm(template) {
  return this.$refs.form.initForm({
    fields: template.fields,
    rules: template.rules
  });
}
```

## 使用示例

### 1. 初始化表单设计器
```javascript
this.formTemplate = {
  fields: [...],
  rules: {...}
}
```

### 2. 获取表单数据
```javascript
this.$refs.form.validate(valid => {
  if (valid) {
    const formData = this.$refs.form.getFormData();
  }
})
```

## 注意事项
1. 表单字段名需唯一
2. 复杂表单建议分步骤设计
3. 动态表单需注意性能优化
4. 移动端需特殊适配控件样式
