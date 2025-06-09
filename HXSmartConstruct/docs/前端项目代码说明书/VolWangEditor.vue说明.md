# VolWangEditor.vue 代码说明

## 文件位置
`src/components/editor/VolWangEditor.vue`

## 组件功能
这是一个基于wangEditor的富文本编辑器组件，主要功能包括：
1. 集成wangEditor富文本编辑器
2. 支持图片上传
3. 支持自定义上传方法
4. 双向数据绑定

## 核心代码结构

### 1. 模板结构
```html
<template>
  <div class="hello" ref="volWangEditor"></div>
</template>
```

### 2. 主要属性配置

| 属性 | 说明 | 类型 | 默认值 |
|------|------|------|--------|
| url | 图片上传接口地址 | String | "" |
| upload | 自定义上传方法 | Function | null |
| modelValue | 编辑器内容 | String | "" |
| height | 编辑器高度 | Number | 250 |
| minWidth | 最小宽度 | Number | 650 |
| minHeight | 最小高度 | Number | 100 |

## 关键实现细节

### 1. 编辑器初始化
```javascript
mounted() {
  this.editor = new E(this.$refs.volWangEditor);
  
  // 配置编辑器
  this.editor.config.height = this.height;
  this.editor.config.onchange = (html) => {
    this.$emit("update:modelValue", html);
  };
  
  // 配置图片上传
  this.editor.config.uploadImgServer = this.http.ipAddress + this.url;
  
  // 创建编辑器
  this.editor.create();
  this.editor.txt.html(this.modelValue);
}
```

### 2. 图片上传处理
```javascript
// 自定义上传方法
editor.config.customUploadImg = function (resultFiles, insertImgFn) {
  if (自定义上传方法) {
    // 使用自定义上传
    resultFiles.forEach(file => {
      $this.upload(file, insertImgFn);
    });
  } else {
    // 使用默认上传
    const formData = new FormData();
    resultFiles.forEach(file => {
      formData.append("fileInput", file, file.name);
    });
    
    $this.http.post($this.url, formData, true, {
      headers: {'Content-Type':'multipart/form-data'}
    }).then(x => {
      if (x.status) {
        insertImgFn($this.http.ipAddress + x.data + file.name);
      }
    });
  }
};
```

### 3. 数据双向绑定
```javascript
watch: {
  modelValue(newVal) {
    if (newVal !== this.lastHtml) {
      this.editor.txt.html(newVal);
    }
    this.lastHtml = newVal;
  }
}
```

## 使用示例

### 基本使用
```javascript
<vol-wang-editor 
  v-model="content"
  :url="uploadUrl"
/>
```

### 自定义上传
```javascript
<vol-wang-editor
  v-model="content"
  :upload="customUpload"
/>

methods: {
  customUpload(file, insertFn) {
    // 自定义上传逻辑
    uploadFile(file).then(url => {
      insertFn(url);
    });
  }
}
```

## 注意事项
1. 需要安装wangeditor依赖
2. 图片上传需要配置正确的url或upload方法
3. 编辑器高度可通过height属性调整
4. 内容变更通过v-model双向绑定
5. 自定义上传方法需调用insertImgFn插入图片
