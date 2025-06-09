# Lang 多语言组件说明

## 功能概述
Lang组件提供完整的国际化解决方案，支持：
- 多语言动态切换
- 按需加载语言包
- 文本格式化与复数处理
- 与路由深度集成

## 文件结构
```
lang/
├── index.js           // 核心实现
├── lang.vue           // 语言切换UI
├── en.ts              // 英文资源
├── zh-cn.ts           // 中文资源
└── dateFormats.ts     // 日期格式配置
```

## 核心机制

### 1. 语言包定义
```typescript
// zh-cn.ts
export default {
  login: {
    title: '登录',
    username: '用户名',
    password: '密码'
  }
}
```

### 2. 初始化配置
```javascript
const i18n = createI18n({
  locale: 'zh-cn',
  fallbackLocale: 'en',
  messages: {
    'zh-cn': zhCN,
    'en': en
  }
})
```

## 主要功能

### 1. 基本使用
```vue
<template>
  <h1>{{ $t('login.title') }}</h1>
</template>

<script>
export default {
  methods: {
    showMessage() {
      this.$i18n.t('login.welcome', { name: '用户' })
    }
  }
}
</script>
```

### 2. 动态切换
```javascript
// 切换语言
this.$i18n.locale = 'en'

// 持久化存储
localStorage.setItem('user-lang', 'en')
```

## 高级功能

### 1. 异步加载
```javascript
// 按需加载语言包
async function loadLanguageAsync(lang) {
  const messages = await import(`./locales/${lang}.ts`)
  i18n.setLocaleMessage(lang, messages.default)
}
```

### 2. 复数处理
```javascript
// 语言包配置
{
  "cart": {
    "items": "购物车有 {count} 件商品 | 购物车有 {count} 件商品"
  }
}

// 使用
$tc('cart.items', itemCount)
```

## 最佳实践

### 1. 项目结构
```
locales/
├── en/
│   ├── common.ts
│   └── login.ts
├── zh-CN/
│   ├── common.ts
│   └── login.ts
└── index.ts
```

### 2. 组件分离
```vue
<!-- LanguageSwitch.vue -->
<template>
  <el-select v-model="currentLang" @change="changeLanguage">
    <el-option
      v-for="lang in languages"
      :key="lang.value"
      :label="lang.label"
      :value="lang.value"
    />
  </el-select>
</template>
```

## 常见问题

### 1. 热更新问题
```javascript
// 开发环境热重载
if (module.hot) {
  module.hot.accept(['./zh-cn', './en'], () => {
    i18n.setLocaleMessage('zh-cn', require('./zh-cn').default)
    i18n.setLocaleMessage('en', require('./en').default)
  })
}
```

### 2. 动态键值处理
```javascript
// 安全访问
const getI18nValue = (key) => {
  return key.split('.').reduce((o, i) => {
    return o ? o[i] : null
  }, i18n.messages[i18n.locale])
}
```

## 注意事项
1. 语言包键名使用命名空间组织
2. 避免在语言包中使用HTML标签
3. 生产环境预编译语言包
4. 日期时间需要单独处理时区
