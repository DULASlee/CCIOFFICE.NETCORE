# VolUpload.vue 代码说明

## 文件位置
`src/components/basic/VolUpload.vue`

## 组件功能
这是一个功能强大的文件上传组件，主要功能包括：
1. 支持单文件/多文件上传
2. 支持图片预览和文件下载
3. 自动文件类型校验
4. 支持文件大小限制
5. 集成图片查看器

## 核心代码结构

### 1. 模板结构
```html
<template>
  <div class="upload-container">
    <!-- 上传按钮 -->
    <div class="input-btns">
      <input type="file" ref="input" @change="handleChange" :multiple="multiple"/>
      
      <!-- 图片上传模式 -->
      <div v-if="img" class="upload-img">
        <!-- 图片预览区域 -->
        <div v-for="(file, index) in files" class="img-item">
          <img :src="getImgSrc(file)"/>
          <!-- 操作按钮 -->
          <div class="operation">
            <i class="el-icon-view" @click="previewImg(index)"></i>
            <i class="el-icon-delete" @click="removeFile(index)"></i>
          </div>
        </div>
      </div>
      
      <!-- 文件列表 -->
      <ul v-if="!img" class="upload-list">
        <li v-for="(file, index) in files" class="list-file">
          <span @click="fileOnClick(index, file)">
            <i :class="format(file)"></i>
            {{ file.name }}
          </span>
          <span @click="removeFile(index)" class="file-remove">
            <i class="el-icon-close"></i>
          </span>
        </li>
      </ul>
    </div>
    
    <!-- 图片查看器 -->
    <vol-image-viewer ref="viewer"></vol-image-viewer>
  </div>
</template>
```

### 2. 主要属性配置

| 属性 | 说明 | 类型 | 默认值 |
|------|------|------|--------|
| fileInfo | 已上传文件列表 | Array | [] |
| multiple | 是否多选 | Boolean | false |
| maxFile | 最大文件数 | Number | 5 |
| maxSize | 文件大小限制(MB) | Number | 50 |
| img | 是否为图片上传 | Boolean | false |
| excel | 是否为Excel文件 | Boolean | false |
| autoUpload | 是否自动上传 | Boolean | true |
| url | 上传接口地址 | String | "" |

## 关键实现细节

### 1. 文件上传处理
```javascript
// 文件选择处理
handleChange(e) {
  const files = e.target.files;
  if(!this.checkFile(files)) return;
  
  // 添加到待上传列表
  this.files.push(...files);
  
  // 自动上传
  if(this.autoUpload) {
    this.upload();
  }
}

// 执行上传
upload() {
  const formData = new FormData();
  this.files.forEach(file => {
    formData.append('fileInput', file, file.name);
  });
  
  this.http.post(this.url, formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  }).then(res => {
    // 处理上传结果
  });
}
```

### 2. 图片预览功能
```javascript
// 获取图片URL
getImgSrc(file) {
  if(file.path) {
    return this.http.ipAddress + file.path;
  }
  return window.URL.createObjectURL(file);
}

// 预览图片
previewImg(index) {
  const imgs = this.files.map(file => this.getImgSrc(file));
  this.$refs.viewer.show(imgs, index);
}
```

### 3. 文件类型校验
```javascript
// 文件类型检查
checkFile(files) {
  const imgTypes = ['gif','jpg','jpeg','png','bmp','webp'];
  
  files.forEach(file => {
    const ext = file.name.split('.').pop().toLowerCase();
    
    // 图片类型检查
    if(this.img && !imgTypes.includes(ext)) {
      this.$message.error('请选择图片文件');
      return false;
    }
    
    // 文件大小检查
    if(file.size > this.maxSize * 1024 * 1024) {
      this.$message.error(`文件大小不能超过${this.maxSize}MB`);
      return false;
    }
  });
  
  return true;
}
```

## 使用示例

### 基本使用
```javascript
<vol-upload 
  :file-info="fileList"
  :url="uploadUrl"
  @change="onFileChange"
/>
```

### 图片上传
```javascript
<vol-upload 
  :img="true"
  :multiple="true"
  :max-file="3"
  :url="imageUploadUrl"
/>
```

### 自定义文件类型
```javascript
<vol-upload 
  :file-types="['pdf','doc','docx']"
  :url="docUploadUrl"
/>
```

## 注意事项
1. 需要配置正确的上传接口地址(url)
2. 大文件上传需要调整maxSize限制
3. 图片预览功能依赖VolImageViewer组件
4. 多文件上传时需设置multiple属性
5. 文件类型校验优先级: img > excel > fileTypes
