# 路由配置说明 (router/index.js)

## 文件概述
本文件定义了项目的路由配置，包含：
- 前端路由表结构
- 全局路由守卫
- 路由错误处理
- 权限控制逻辑

## 核心路由结构

### 1. 主路由框架
```javascript
{
  path: '/',
  component: () => import('@/views/Index.vue'),
  redirect: '/home',
  children: [
    ...viewgird,  // 动态表格路由
    ...redirect,  // 重定向路由
    // 其他业务路由
  ]
}
```

### 2. 业务路由示例
```javascript
{
  path: '/formDraggable',
  name: 'formDraggable',
  component: () => import('@/views/formDraggable/formDraggable.vue')
}
```

## 关键功能说明

### 1. 路由守卫
```javascript
router.beforeEach((to, from, next) => {
  // 权限校验逻辑
  if (!to.meta.anonymous && !store.getters.isLogin()) {
    next({ path: '/login' })
  } else {
    next()
  }
})
```

### 2. 动态导入
使用`() => import()`实现路由级代码分割：
```javascript
component: () => import('@/views/Home.vue')
```

### 3. 路由元信息
```javascript
meta: {
  keepAlive: false, // 是否缓存组件
  anonymous: true   // 是否免登录
}
```

## 特殊路由处理

### 1. 视图表格路由
- 通过`...viewgird`导入动态生成的路由配置
- 支持表格页面的快速生成

### 2. 错误处理
```javascript
router.onError((error) => {
  console.error('路由错误:', error)
  // 开发环境显示错误提示
})
```

## 注意事项
1. 新增路由需考虑权限控制需求
2. 保持路由name的唯一性
3. 生产环境需处理路由加载失败的情况
4. 路由层级不宜过深（建议不超过3层）
