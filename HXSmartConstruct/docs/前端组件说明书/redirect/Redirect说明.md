# Redirect 路由组件说明

## 功能概述
路由重定向组件集合，提供：
- 权限路由控制
- 异常状态处理
- 动态路由重定向
- 导航守卫集成

## 文件结构
```
redirect/
├── 401.vue            // 无权限页面
├── 404.vue            // 404页面
├── coding.vue         // 开发中页面
├── Message.vue        // 消息提示组件
└── RedirectError.vue  // 错误重定向
```

## 核心功能

### 1. 基础重定向
```javascript
// 路由配置
{
  path: '/old',
  redirect: '/new'
}
```

### 2. 动态重定向
```javascript
// 根据状态重定向
{
  path: '/dashboard',
  redirect: to => {
    return hasPermission() ? '/admin' : '/guest'
  }
}
```

## 高级功能

### 1. 导航守卫
```javascript
router.beforeEach((to, from, next) => {
  if (to.meta.requiresAuth && !isAuthenticated()) {
    next({ path: '/401' })
  } else {
    next()
  }
})
```

### 2. 错误处理
```vue
<!-- RedirectError.vue -->
<template>
  <div v-if="error">
    <h1>{{ error.statusCode }}</h1>
    <button @click="handleRetry">重试</button>
  </div>
</template>
```

## 最佳实践

### 1. 路由懒加载
```javascript
{
  path: '/admin',
  component: () => import('@/views/Admin.vue')
}
```

### 2. 权限控制
```javascript
// 动态路由过滤
function filterRoutes(routes) {
  return routes.filter(route => {
    if (route.meta?.permission) {
      return checkPermission(route.meta.permission)
    }
    return true
  })
}
```

## 注意事项
1. 避免重定向循环
2. 生产环境自定义错误页面
3. 敏感路由添加二次验证
4. 记录关键路由跳转日志
