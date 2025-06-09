# VolFormDraggable.vue 代码说明

## 文件位置
`src/components/basic/VolFormDraggable/VolFormDraggable.vue`

## 组件功能
这是一个可视化表单设计器组件，主要功能包括：
1. 支持通过拖拽方式构建表单
2. 提供多种表单元素组件
3. 支持表单预览和配置导出
4. 内置表格设计功能
5. 支持数据字典绑定

## 核心代码结构

### 1. 模板结构
```html
<template>
  <div class="drag-container">
    <!-- 左侧组件列表 -->
    <div class="drag-left">
      <div class="left-title">组件列表</div>
      <draggable v-model="components">
        <!-- 可拖拽的表单组件 -->
      </draggable>
    </div>
    
    <!-- 中间设计区域 -->
    <div class="drag-center">
      <div class="center-top">
        <!-- 操作按钮 -->
      </div>
      <el-scrollbar>
        <el-form>
          <draggable v-model="currentComponents">
            <!-- 已添加的表单组件 -->
          </draggable>
        </el-form>
      </el-scrollbar>
    </div>
    
    <!-- 右侧属性配置 -->
    <div class="drag-right">
      <div class="left-title">组件属性</div>
      <div class="attr">
        <!-- 组件属性配置表单 -->
      </div>
    </div>
  </div>
</template>
```

### 2. 支持的组件类型
组件支持多种表单元素类型，包括：
- 基础输入：text、textarea、number
- 选择器：select、radio、checkbox、cascader
- 日期时间：date
- 特殊类型：switch、img、file、editor
- 布局元素：line(分隔线)
- 表格：table

### 3. 核心逻辑方法

#### 表单设计方法
```javascript
end1()      // 左侧组件拖拽到设计区
itemClick() // 点击选中组件
removeItem() // 删除组件
copyItem()  // 复制组件
clearItems() // 清空所有组件
```

#### 预览与导出
```javascript
preview()   // 预览表单设计
save()      // 保存表单配置
download()  // 下载表单配置
```

## 关键实现细节

### 1. 拖拽功能实现
```javascript
// 使用vue-draggable-next实现拖拽
import { VueDraggableNext } from "vue-draggable-next";

components: {
  draggable: VueDraggableNext
}

// 左侧组件拖拽到设计区
<draggable 
  v-model="components"
  @end="end1"
  group="componentsGroup"
>
```

### 2. 组件属性配置
```javascript
// 点击组件显示属性配置
itemClick(item, index) {
  this.currentIndex = index;
  this.currentItem = this.currentComponents[this.currentIndex];
}

// 右侧属性配置区
<div class="attr" v-show="currentIndex != -1">
  <div class="attr-item">
    <div class="text">字段名称</div>
    <el-input v-model="currentItem.name" />
  </div>
  <!-- 其他属性配置... -->
</div>
```

### 3. 表单预览实现
```javascript
preview() {
  // 生成表单配置
  let formOptions = [];
  this.currentComponents.forEach(item => {
    let option = {
      field: item.field,
      title: item.name,
      type: item.type
      // 其他属性...
    };
    formOptions.push(option);
  });
  
  // 打开预览弹窗
  this.viewFormData.formOptions = formOptions;
  this.previewModel = true;
}
```

## 使用示例

### 基本使用
```javascript
<vol-form-draggable 
  :user-components="formComponents"
  @save="onSave"
/>
```

### 保存表单配置
```javascript
const onSave = (data) => {
  // data.daraggeOptions: 拖拽组件配置
  // data.formOptions: 生成的表单配置
  console.log(data);
}
```

## 高级功能说明

### 1. 表格设计功能
```javascript
// 配置表格列
{
  type: 'table',
  name: '数据表格',
  url: 'api/table/getPageData',
  columns: [
    { field: 'id', title: 'ID', width: 80 },
    { field: 'name', title: '名称', edit: true }
  ]
}

// 表格配置弹窗
openTableModel() {
  this.tableModel = true;
  this.currnetTableData = JSON.parse(JSON.stringify(this.currentItem.columns));
}
```

### 2. 数据字典绑定
```javascript
// 从后台获取字典数据
created() {
  this.http.post("api/Sys_Dictionary/GetBuilderDictionary").then(x => {
    this.dicList = x.map(c => ({ key: c, value: c }));
  });
}

// 字典变更处理
dicChange(key) {
  this.http.post("api/Sys_Dictionary/GetVueDictionary", [key]).then(result => {
    this.currentItem.data = result[0].data;
  });
}
```

### 3. 表单预览与导出
```javascript
// 预览表单
preview() {
  this.viewFormData.formOptions = this.generateFormOptions();
  this.previewModel = true;
}

// 下载表单配置
download() {
  this.preview(false);
  this.generateDownloadFile();
}
```

## 完整API说明

### 组件属性
| 属性 | 说明 | 类型 | 默认值 |
|------|------|------|--------|
| user-components | 初始表单组件配置 | Array | [] |

### 组件事件
| 事件名 | 说明 | 回调参数 |
|--------|------|----------|
| save | 保存表单配置时触发 | { daraggeOptions, formOptions } |

### 内置方法
| 方法名 | 说明 |
|--------|------|
| preview() | 预览当前表单设计 |
| save() | 保存表单配置 |
| download() | 下载表单配置JSON |

## 样式定制
组件支持通过以下CSS类名进行样式定制：
- `.drag-container` - 整体容器
- `.drag-left` - 左侧组件面板
- `.drag-center` - 中间设计区域 
- `.drag-right` - 右侧属性面板
- `.item` - 组件列表项
- `.item2` - 设计区组件项

## 注意事项
1. 需要安装vue-draggable-next依赖
2. 表格设计需要单独配置columns等属性
3. 数据字典需要提前在后台配置
4. 文件上传需要配置正确的接口地址
5. 复杂表单建议先预览再保存
6. 使用示例模板可快速构建常见表单
