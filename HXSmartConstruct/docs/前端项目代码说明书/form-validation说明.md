# 表单验证说明

## 验证机制概述
表单验证系统提供以下功能：
- 实时验证与提交验证
- 内置常用验证规则
- 自定义验证方法
- 异步远程验证
- 多语言错误提示

## 验证规则配置

### 1. 基本规则配置
```javascript
rules: {
  username: [
    { required: true, message: '请输入用户名' },
    { min: 3, max: 10, message: '长度3-10个字符' }
  ],
  password: [
    { required: true, message: '请输入密码' },
    { pattern: /^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$/, 
      message: '至少8位包含字母和数字' }
  ]
}
```

### 2. 规则参数说明
| 参数 | 类型 | 说明 |
|------|------|------|
| required | Boolean | 是否必填 |
| message | String | 错误提示 |
| min/max | Number | 最小/最大长度 |
| pattern | RegExp | 正则验证 |
| validator | Function | 自定义验证函数 |
| trigger | String | 触发方式(change/blur) |

## 内置验证方法

### 1. 常用验证类型
```javascript
// 类型验证
{ type: 'email' }
{ type: 'url' }
{ type: 'number' }

// 范围验证
{ min: 6, max: 20 } // 长度
{ min: 0, max: 100 } // 数值范围

// 格式验证
{ pattern: /^1[3-9]\d{9}$/ } // 手机号
```

### 2. 复合验证
```javascript
password: [
  { required: true },
  { min: 8, message: '至少8位' },
  { 
    validator: (rule, value) => value === this.formData.confirmPassword,
    message: '两次输入不一致' 
  }
]
```

## 自定义验证

### 1. 自定义验证函数
```javascript
{
  validator: (rule, value, callback) => {
    if (!/^[a-z]+$/.test(value)) {
      callback(new Error('只能包含小写字母'));
    } else {
      callback();
    }
  }
}
```

### 2. 异步验证
```javascript
{
  validator: (rule, value) => {
    return new Promise((resolve, reject) => {
      checkUserExists(value).then(exists => {
        exists ? reject('用户名已存在') : resolve();
      });
    });
  }
}
```

## 验证触发机制

### 1. 触发方式配置
```javascript
// 单个字段配置
username: [
  { required: true, trigger: 'blur' }
]

// 全局配置
<vol-form :validate-trigger="['change', 'blur']">
```

### 2. 手动触发验证
```javascript
// 验证整个表单
this.$refs.form.validate()

// 验证单个字段
this.$refs.form.validateField('username')
```

## 错误处理

### 1. 错误提示展示
```html
<vol-form-item prop="username">
  <vol-input v-model="form.username" />
  <template #error="{ error }">
    <span class="error-text">{{ error }}</span>
  </template>
</vol-form-item>
```

### 2. 错误清除方法
```javascript
// 清除所有错误
this.$refs.form.clearValidate()

// 清除单个字段错误
this.$refs.form.clearValidate('username')
```

## 高级功能

### 1. 动态规则
```javascript
// 根据条件切换规则
watch: {
  'form.type'(val) {
    this.rules.amount = val === 1 ? 
      [{ required: true }] : 
      [{ min: 100, max: 1000 }]
  }
}
```

### 2. 表单分组验证
```javascript
// 验证指定分组
this.$refs.form.validateGroup(['basicInfo'])

// 分组规则配置
groups: {
  basicInfo: ['name', 'age'],
  contactInfo: ['phone', 'email']
}
```

## 最佳实践
1. 复杂验证逻辑建议拆分为独立验证函数
2. 频繁触发的验证建议添加防抖处理
3. 异步验证需要处理加载状态
4. 移动端注意错误提示的友好展示
5. 表单重置时清除验证状态
