# 状态管理说明 (store/index.js)

## 文件概述
本文件定义了Vuex状态管理核心配置，主要功能包括：
- 用户登录状态管理
- 权限数据管理
- 全局加载状态控制
- 应用语言配置

## 核心状态结构

### 1. 状态定义
```javascript
state: {
  data: {},         // 通用数据存储
  permission: [],  // 权限数据
  isLoading: false, // 全局加载状态
  userInfo: null,  // 用户信息
  appLang: {},     // 语言配置
  serviceList: [] // 服务列表
}
```

### 2. 用户信息管理
```javascript
mutations: {
  setUserInfo(state, data) {
    state.userInfo = data
    localStorage.setItem('user', JSON.stringify(data))
  },
  clearUserInfo(state) {
    state.userInfo = null
    localStorage.removeItem('user')
  }
}
```

## 关键功能说明

### 1. 权限管理
```javascript
getters: {
  getPermission: (state) => (path) => {
    return state.permission.find(x => x.path?.toLowerCase() == path?.toLowerCase())
  }
}
```

### 2. 全局加载状态
```javascript
actions: {
  onLoading(context, flag) {
    context.commit('updateLoadingState', flag)
  }
}
```

### 3. 用户信息持久化
通过localStorage实现用户登录状态持久化：
```javascript
function getUserInfo(state) {
  if (!state.userInfo) {
    state.userInfo = JSON.parse(localStorage.getItem('user')) 
  }
  return state.userInfo
}
```

## 使用示例

### 1. 获取用户信息
```javascript
// 组件中使用
this.$store.getters.getUserInfo()
```

### 2. 设置权限数据
```javascript
// 登录成功后设置权限
this.$store.commit('setPermission', permissionData)
```

### 3. 控制加载状态
```javascript
// 显示加载中
this.$store.dispatch('onLoading', true)
```

## 注意事项
1. 敏感信息(如token)应加密存储
2. 权限数据应在登录成功后立即设置
3. 全局状态变更应通过mutation/action进行
4. 大型项目建议采用模块化设计
