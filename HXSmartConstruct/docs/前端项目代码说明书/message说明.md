# 消息中心组件说明 (Message.vue)

## 组件概述
消息中心组件提供系统消息通知功能，主要特点包括：
- 消息红点提示
- 消息分类展示
- 消息列表和详情
- 响应式交互设计

## 核心功能

### 1. 消息展示结构
```javascript
const msg = reactive([
  {
    title: "消息标题",
    desc: "消息描述",
    date: "2025-03-01",
    type: "消息类型", 
    tag: "消息标签样式"
  }
])
```

### 2. 消息计数和提示
```javascript
const msgCount = ref(3) // 未读消息数量
<el-badge :is-dot="msgCount > 0"> // 红点提示
```

### 3. 消息分类标签
```html
<el-tabs v-model="activeName">
  <el-tab-pane name="msg">消息通知</el-tab-pane>
  <el-tab-pane name="sys">系统消息</el-tab-pane>
  <el-tab-pane name="read">已读消息</el-tab-pane>
</el-tabs>
```

## 数据结构

### 消息对象属性
| 属性 | 类型 | 说明 |
|------|------|------|
| title | String | 消息标题 |
| desc | String | 消息详细描述 |
| date | String | 消息时间 |
| type | String | 消息类型(任务工单/审批流程等) |
| tag | String | 标签样式(primary/success等) |

## 交互逻辑

### 1. 消息点击事件
```javascript
const showMsg = () => {
  model.value = true // 显示消息弹窗
}
```

### 2. 标签切换事件
```javascript
const handleClick = (e) => {
  // 处理标签切换逻辑
}
```

## 样式定制

### 1. 消息项样式
```less
.msg-item {
  border-bottom: 1px solid #eee;
  padding: 10px;
  
  &:hover {
    background: #f9f9f9;
  }
}
```

### 2. 标签页样式
```less
::v-deep(.el-tabs__item) {
  flex: 1; // 等分标签页宽度
}
```

## 使用示例

### 1. 添加新消息
```javascript
msg.push({
  title: "新工单创建",
  desc: "生产部创建了新的工单",
  date: "2025-03-02",
  type: "任务工单",
  tag: "warning"
})
```

### 2. 更新消息计数
```javascript
msgCount.value = msg.filter(m => !m.read).length
```

## 注意事项
1. 消息数据需要后端接口支持
2. 移动端需要调整样式适配
3. 大量消息时需考虑分页加载
4. 消息状态(已读/未读)需要持久化
