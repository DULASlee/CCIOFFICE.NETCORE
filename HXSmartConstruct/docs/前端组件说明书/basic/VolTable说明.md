# VolTable 高级表格组件说明

## 功能概述
VolTable是增强型表格组件，提供：
- 高性能大数据渲染
- 复杂表头与合并单元格
- 行内编辑功能
- 多级表头支持

## 文件结构
```
VolTable/
├── index.js           // 组件入口
├── VolTable.vue       // 主组件
├── VolTable.less      // 样式文件
├── VolTableEdit.js    // 行内编辑逻辑
├── VolTableRender.js  // 渲染逻辑
└── ...                // 其他功能模块
```

## 核心功能

### 1. 基础表格
```vue
<VolTable
  :columns="complexColumns"
  :data="tableData"
  :max-height="600"
/>
```

### 2. 属性配置
| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| columns | Array | [] | 复杂列配置 |
| data | Array | [] | 表格数据 |
| edit-mode | Boolean | false | 行编辑模式 |

### 3. 多级表头配置
```javascript
columns: [
  {
    title: '基本信息',
    children: [
      { title: '姓名', field: 'name' },
      { title: '年龄', field: 'age' }
    ]
  }
]
```

## 高级功能

### 1. 行内编辑
```javascript
// 启用编辑模式
{
  title: '操作',
  field: 'action',
  editComponent: {
    type: 'select',
    options: [
      { label: '通过', value: 1 },
      { label: '拒绝', value: 0 }
    ]
  }
}
```

### 2. 虚拟滚动
```vue
<VolTable
  :virtual-scroll="true"
  :item-size="48"
  :buffer-size="10"
/>
```

## 使用示例

### 1. 大数据渲染
```javascript
// 使用分页加载
loadData(page) {
  this.loading = true;
  fetchData(page).then(res => {
    this.tableData = res.data;
    this.total = res.total;
  });
}
```

### 2. 自定义单元格
```javascript
{
  title: '状态',
  field: 'status',
  render: (h, { row }) => {
    return h('span', {
      class: {
        'active': row.status === 1,
        'inactive': row.status === 0
      }
    }, row.status === 1 ? '启用' : '禁用');
  }
}
```

## 注意事项
1. 大数据量必须启用虚拟滚动
2. 复杂表头不宜超过3级
3. 编辑模式需要单独保存逻辑
4. 样式覆盖使用深度选择器时需谨慎
