# 表格组件说明

## 组件概述
表格组件提供数据展示和交互功能，主要特点包括：
- 支持水平和列表两种展示模式
- 自动分页和数据加载
- 自定义列配置
- 行选择和操作

## 核心组件

### 1. 基础表格 (vol-table)
```html
<vol-table
  :url="apiUrl"
  :columns="columns"
  @rowClick="onRowClick"
></vol-table>
```

### 2. 高级表格 (ViewGrid)
```html
<view-grid
  ref="grid"
  :options="gridOptions"
  @search="onSearch"
>
  <template #gridHeader>
    <!-- 自定义表头 -->
  </template>
</view-grid>
```

## 表格配置

### 1. 列配置示例
```javascript
columns: [
  {
    field: 'name',
    title: '姓名',
    width: 100,
    align: 'center',
    formatter: (value, row) => {
      return `${value}(${row.age})`
    }
  },
  {
    field: 'actions',
    title: '操作',
    type: 'buttons',
    buttons: [
      { text: '编辑', click: 'edit' },
      { text: '删除', click: 'delete' }
    ]
  }
]
```

### 2. 表格属性
| 属性 | 类型 | 说明 |
|------|------|------|
| url | String | 数据接口地址 |
| columns | Array | 列配置 |
| height | Number | 表格高度 |
| direction | String | 显示方向(horizontal/list) |
| ck | Boolean | 是否显示复选框 |
| index | Boolean | 是否显示行号 |

## 数据加载

### 1. 分页加载
```javascript
// 加载初始数据
this.$refs.table.load()

// 加载更多数据
this.$refs.table.loadMore()
```

### 2. 数据转换
```javascript
// 获取选中行数据
const selectedRows = this.$refs.table.getSelectRows()

// 获取当前页数据
const currentData = this.$refs.table.getTableData()
```

## 事件处理

### 1. 常用事件
| 事件 | 说明 | 参数 |
|------|------|------|
| rowClick | 行点击事件 | row, index |
| rowButtons | 行按钮点击 | button, row, index |
| loadBefore | 加载前回调 | params |
| loadAfter | 加载后回调 | data |

### 2. 自定义行操作
```javascript
methods: {
  onRowClick(row, index) {
    console.log('点击行:', row)
  },
  onButtonClick(button, row) {
    if (button.click === 'edit') {
      this.editRow(row)
    }
  }
}
```

## 使用示例

### 1. 基本使用
```javascript
export default {
  data() {
    return {
      apiUrl: 'api/user/list',
      columns: [...]
    }
  }
}
```

### 2. 自定义加载
```javascript
this.$refs.table.load({
  page: 1,
  size: 20,
  name: '张三'
})
```

## 注意事项
1. 列配置中的field需与接口返回字段对应
2. 大数据量建议使用分页加载
3. 复杂表格可考虑使用ViewGrid组件
4. 移动端需注意表格高度适配
