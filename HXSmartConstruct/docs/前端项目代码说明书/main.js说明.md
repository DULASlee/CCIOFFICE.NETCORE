# main.js 项目入口文件说明

## 文件概述
main.js是Vue应用的启动入口文件，负责：
- 创建Vue根实例
- 注册全局组件/插件
- 初始化全局配置
- 挂载应用到DOM

## 核心代码解析

```javascript
// 1. Vue实例创建
const app = createApp(App)

// 2. 全局组件注册
app.component('VolBox', VolBox)
app.component('VolForm', VolForm)

// 3. 插件安装
app.use(router)
app.use(store)
app.use(ElementPlus)

// 4. 全局配置
app.config.globalProperties.$http = http 
app.config.globalProperties.$permission = permission

// 5. 挂载到DOM
app.mount('#app')
```

## 关键配置说明

### 1. 全局组件注册
- VolBox: 通用弹窗组件
- VolForm: 动态表单组件
- VolTable: 数据表格组件

### 2. 插件安装
- router: 路由系统
- store: 状态管理
- ElementPlus: UI组件库

### 3. 全局属性
- $http: 封装的axios实例
- $permission: 权限控制对象

## 注意事项
1. 组件注册顺序可能影响样式优先级
2. 全局属性应谨慎使用，避免污染全局空间
3. 生产环境需配置合适的错误处理
