# 工作流组件说明 (panel.vue)

## 组件概述
工作流组件提供可视化流程设计功能，基于jsPlumb实现，主要特点包括：
- 拖拽式流程设计
- 多类型节点支持
- 自定义连线规则
- 实时表单配置

## 核心功能

### 1. 组件结构
```html
<div class="flow-panel">
  <!-- 左侧菜单 -->
  <node-menu @addNode="addNode"/>
  
  <!-- 中间画布 --> 
  <div id="efContainer">
    <flow-node v-for="node in data.nodeList"/>
  </div>
  
  <!-- 右侧表单 -->
  <flow-node-form ref="nodeForm"/>
</div>
```

### 2. 核心数据模型
```javascript
data: {
  nodeList: [], // 节点列表
  lineList: []  // 连线列表
},
activeElement: {
  type: "node|line", // 当前选中元素类型
  nodeId: "",       // 节点ID
  sourceId: "",     // 连线起点
  targetId: ""      // 连线终点
}
```

### 3. 节点类型
| 类型 | 说明 | 限制 |
|------|------|------|
| start | 开始节点 | 只能有一个 |
| end | 结束节点 | 只能有一个 |
| task | 任务节点 | 无限制 |
| condition | 条件节点 | 无限制 |

## 交互逻辑

### 1. 节点操作
```javascript
// 添加节点
addNode(evt, nodeMenu, mousePosition) {
  // 校验节点类型限制
  // 计算节点位置
  // 添加到nodeList
}

// 删除节点  
deleteNode(nodeId) {
  // 校验业务规则
  // 从nodeList移除
  // 删除相关连线
}
```

### 2. 连线规则
```javascript
beforeDrop(evt) {
  // 禁止自连接
  if(from === to) return false
  
  // 禁止重复连线
  if(hasLine(from, to)) return false
  
  // 禁止回环连线
  if(hashOppositeLine(from, to)) return false
  
  return true
}
```

## 配置说明

### 1. 流程基础配置
```javascript
formFields: {
  WorkName: "",       // 流程名称
  WorkTable: "",       // 关联数据表
  Weight: 1,          // 流程权重
  AuditingEdit: 0      // 是否同步更新业务数据
}
```

### 2. jsPlumb配置
```javascript
jsplumbSetting: {
  Connector: ["Flowchart"],  // 连线样式
  Anchors: ["BottomCenter"]  // 锚点位置
}
```

## 使用示例

### 1. 初始化工作流
```javascript
dataReload(data) {
  this.data = {
    nodeList: [...],
    lineList: [...] 
  }
  this.jsPlumbInit()
}
```

### 2. 保存工作流
```javascript
saveFlow() {
  return {
    nodes: this.data.nodeList,
    lines: this.data.lineList,
    form: this.formFields
  }
}
```

## 注意事项
1. 节点ID必须唯一
2. 开始/结束节点有数量限制
3. 连线需遵循业务规则
4. 缩放范围限制在0.3-1.0之间
5. 大流程需考虑性能优化
