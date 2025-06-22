<template>
  <view class="edit-container">
    <text>SC_Project 编辑页面（占位）</text>
    <!-- 可根据实际业务添加表单组件等内容 -->
  </view>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { onLoad } from '@dcloudio/uni-app';

const id = ref('');

onLoad((options) => {
  id.value = options.id || '';
  // 可在此处根据 id 加载详情数据
});
</script>

<style scoped>
.edit-container {
  padding: 32rpx;
}
</style>
