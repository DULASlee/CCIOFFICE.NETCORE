<template>
  <div>项目信息内容（项目ID：{{ projectId }}）</div>
</template>
<script setup>
const props = defineProps({ projectId: String, projectData: Object });
</script> 