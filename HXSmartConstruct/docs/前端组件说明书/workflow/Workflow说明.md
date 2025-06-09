# Workflow 工作流组件说明

## 功能概述
Workflow是可视化流程设计组件，提供：
- 可视化流程设计器
- 节点拖拽与连接
- 条件分支配置
- 流程验证与导出

## 文件结构
```
workflow/
├── workflow.vue       // 主组件
├── jsplumb.js         // 连接线库
├── node.vue           // 基础节点
├── node_form.vue      // 表单节点
├── panel.vue          // 控制面板
└── utils.js           // 工具方法
```

## 核心功能

### 1. 基础使用
```vue
<Workflow
  :nodes="initNodes"
  :connections="initConnections"
  @save="handleSave"
/>
```

### 2. 属性配置
| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| nodes | Array | [] | 初始节点 |
| connections | Array | [] | 初始连接 |
| readonly | Boolean | false | 只读模式 |

## 节点类型

### 1. 系统节点类型
| 类型 | 说明 |
|------|------|
| start | 开始节点 |
| end | 结束节点 |
| task | 任务节点 |
| gateway | 网关节点 |

### 2. 自定义节点
```javascript
registerNodeType('custom-node', {
  template: `<div class="custom-node">{{node.name}}</div>`,
  props: ['node']
})
```

## 使用示例

### 1. 初始化流程
```javascript
data() {
  return {
    initNodes: [
      { id: 'node1', type: 'start', x: 100, y: 100 },
      { id: 'node2', type: 'task', x: 300, y: 100 }
    ],
    initConnections: [
      { source: 'node1', target: 'node2' }
    ]
  }
}
```

### 2. 保存流程
```javascript
methods: {
  handleSave(nodes, connections) {
    // 验证流程
    if (!this.validateFlow(nodes)) {
      return this.$message.error('流程不完整');
    }
    // 保存逻辑
  }
}
```

## 高级功能

### 1. 条件分支
```javascript
{
  type: 'gateway',
  conditions: [
    { expression: 'amount > 100', target: 'approve-node' },
    { expression: 'amount <= 100', target: 'reject-node' }
  ]
}
```

### 2. 导入/导出
```javascript
// 导出JSON
const flowData = this.$refs.workflow.export();

// 导入
this.$refs.workflow.import(flowData);
```

## 注意事项
1. 复杂流程建议分阶段设计
2. 节点ID必须唯一
3. 生产环境需要添加权限控制
4. 大流程建议使用懒加载
