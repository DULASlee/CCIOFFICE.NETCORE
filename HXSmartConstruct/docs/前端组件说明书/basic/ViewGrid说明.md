# ViewGrid 组件说明

## 功能概述
ViewGrid是核心表格组件，提供：
- 数据展示与分页
- 复杂表格功能集成
- 自定义列配置
- 数据筛选与操作

## 文件结构
```
ViewGrid/
├── Action.js          // 表格操作逻辑
├── AuditHis.vue       // 审核历史组件
├── ViewGrid.vue       // 主组件
├── ViewGrid.less      // 样式文件
├── props.js           // 属性定义
└── ...                // 其他功能模块
```

## 核心功能

### 1. 基础表格
```vue
<ViewGrid
  :columns="columns"
  :data="tableData"
  @row-click="handleRowClick"
/>
```

### 2. 属性配置
| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| columns | Array | [] | 列配置 |
| data | Array | [] | 表格数据 |
| height | Number | - | 表格高度 |
| pagination | Boolean | true | 是否分页 |

### 3. 事件列表
| 事件名 | 参数 | 说明 |
|--------|------|------|
| row-click | row, index | 行点击事件 |
| selection-change | rows | 选中行变化 |

## 高级功能

### 1. 自定义列
```javascript
// columns配置示例
{
  title: '姓名',
  field: 'name',
  render: (h, params) => {
    return h('span', { style: { color: 'red' }}, params.row.name)
  }
}
```

### 2. 数据筛选
```javascript
// 使用filter组件
{
  title: '状态',
  field: 'status',
  filter: {
    type: 'select',
    options: [
      { label: '启用', value: 1 },
      { label: '禁用', value: 0 }
    ]
  }
}
```

## 使用示例

### 1. 基础使用
```javascript
export default {
  data() {
    return {
      columns: [...],
      tableData: [...]
    }
  },
  methods: {
    handleRowClick(row) {
      console.log('当前行:', row)
    }
  }
}
```

### 2. 集成其他组件
```vue
<ViewGrid>
  <template #action="{ row }">
    <el-button @click="edit(row)">编辑</el-button>
  </template>
</ViewGrid>
```

## 注意事项
1. 大数据量建议启用虚拟滚动
2. 复杂操作建议使用slot插槽
3. 样式覆盖请使用深层选择器
