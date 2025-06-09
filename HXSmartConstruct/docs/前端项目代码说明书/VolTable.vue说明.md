# VolTable.vue 代码说明

## 文件位置
`src/components/basic/VolTable.vue`

## 组件功能
这是一个高度可配置的动态表格组件，主要功能包括：
1. 支持多种表格列类型(文本、图片、文件、日期等)
2. 支持表格行内编辑
3. 内置分页功能
4. 支持树形表格
5. 支持单元格合并
6. 支持自定义渲染

## 核心代码结构

### 1. 模板结构
```html
<template>
  <!-- 表格容器 -->
  <div class="vol-table">
    <!-- 加载遮罩 -->
    <div class="mask" v-if="loading">
      <vol-loading></vol-loading>
    </div>
    
    <!-- 主表格 -->
    <el-table>
      <!-- 选择列 -->
      <el-table-column v-if="ck" type="selection" />
      
      <!-- 序号列 -->
      <el-table-column v-if="columnIndex" type="index" />
      
      <!-- 动态生成表格列 -->
      <el-table-column v-for="column in tableColumns">
        <!-- 各种列类型的渲染 -->
      </el-table-column>
    </el-table>
    
    <!-- 分页组件 -->
    <div class="block pagination" v-if="!paginationHide">
      <el-pagination />
    </div>
  </div>
</template>
```

### 2. 支持的列类型
组件支持超过15种列类型，包括：
- 基础类型：text、number、textarea
- 选择类型：select、radio、checkbox、cascader
- 日期时间：date、datetime、time
- 特殊类型：switch、rate、color、img、file
- 自定义类型：render

### 3. 核心逻辑方法

#### 表格初始化
```javascript
initConfig()      // 初始化表格配置
initDicKeys()     // 初始化字典数据
initSummary()     // 初始化合计行
```

#### 数据操作方法
```javascript
load()            // 加载表格数据
reset()           // 重置表格
getSelectionRows() // 获取选中行
```

#### 编辑相关方法
```javascript
rowClick()        // 行点击事件
rowDbClick()      // 行双击事件
inputKeypress()   // 输入框事件
selectChange()    // 选择框变更
```

## 关键实现细节

### 1. 动态表格列生成
```javascript
// 通过columns配置生成表格列
props.columns.forEach(column => {
  // 根据column.type渲染不同列组件
  switch(column.type) {
    case 'select':
      // 渲染选择器
      break;
    case 'img':
      // 渲染图片
      break;
    // 其他类型...
  }
})
```

### 2. 行内编辑实现
```javascript
// 双击行进入编辑状态
const rowClick = (row, column) => {
  edit.rowIndex = row.elementIndex;
  edit.columnIndex = columnIndex;
  
  // 自动聚焦到可编辑单元格
  nextTick(() => {
    const ref = `${column.field}${row.elementIndex}`;
    this.$refs[ref]?.focus();
  });
}
```

### 3. 分页控制
```javascript
// 分页变更处理
const handleSizeChange = (val) => {
  paginations.size = val;
  load();
}

const handleCurrentChange = (val) => {
  paginations.page = val; 
  load();
}
```

### 4. 图片/文件处理
```html
<!-- 图片列 -->
<img v-for="file in getFilePath(row[column.field])"
     :src="file.path + access_token"
     @click="viewImg(row, column, file.path)"/>

<!-- 文件列 -->  
<a v-for="file in getFilePath(row[column.field])"
   @click="dowloadFile(file)">
  {{ file.name }}
</a>
```

## 使用示例

### 基本使用
```javascript
<vol-table
  :columns="columns"
  :tableData="tableData"
  @selection-change="onSelectionChange"
/>
```

### 列配置示例
```javascript
const columns = [
  {
    field: 'name',
    title: '姓名',
    type: 'text',
    width: 120
  },
  {
    field: 'avatar', 
    title: '头像',
    type: 'img',
    width: 80
  }
]
```

## 高级功能说明

### 1. 树形表格实现
```javascript
// 配置lazy和load属性实现懒加载
<el-table
  :lazy="lazy"
  :load="loadTreeChildren"
  :row-key="rowKey"
  :expand-row-keys="expandRowKeys"
/>

// 加载子节点方法
const loadTreeChildren = (tree, resolve) => {
  // 调用API获取子节点数据
  http.get('/api/getChildren', {id: tree.id})
    .then(data => resolve(data))
}
```

### 2. 远程数据加载
```javascript
// 配置url属性实现远程加载
const load = async (query) => {
  const res = await http.get(props.url, {
    page: paginations.page,
    rows: paginations.size,
    ...query
  });
  
  // 处理返回数据
  rowData.value = res.rows;
  paginations.total = res.total;
}
```

### 3. 行内编辑API
```javascript
// 通过ref调用表格方法
const tableRef = ref();

// 添加行
tableRef.value.addRow(newRow);

// 删除行 
tableRef.value.delRow();

// 跳转到指定单元格
tableRef.value.toNextCell(row, nextField);
```

### 4. 自定义渲染
```javascript
// 使用render属性自定义单元格内容
{
  field: 'status',
  title: '状态',
  render: (row, column) => {
    return <el-tag type={row.status === 1 ? 'success' : 'danger'}>
      {row.status === 1 ? '启用' : '禁用'}
    </el-tag>
  }
}
```

## 完整API说明

### 表格方法
| 方法名 | 说明 | 参数 |
|--------|------|------|
| load | 加载表格数据 | query: 查询条件 |
| reset | 重置表格 | - |
| getSelectionRows | 获取选中行 | - |
| addRow | 添加行 | row: 行数据 |
| delRow | 删除行 | - |
| setEdit | 设置编辑状态 | index: 行索引(-1结束编辑) |

### 表格属性
| 属性 | 说明 | 类型 | 默认值 |
|------|------|------|--------|
| columns | 列配置 | Array | [] |
| tableData | 表格数据 | Array | [] |
| url | 远程API地址 | String | '' |
| height | 表格高度 | Number/String | - |
| rowKey | 行唯一标识 | String | 'id' |
| lazy | 是否懒加载 | Boolean | false |

## 注意事项
1. 需要配合Element Plus组件库使用
2. 分页功能需要配置url或tableData
3. 图片/文件列需要配置正确的访问路径
4. 行内编辑需要配置edit属性
5. 树形表格需要配置rowKey和load方法
6. 远程加载需要处理分页参数
7. 复杂渲染建议使用render函数
