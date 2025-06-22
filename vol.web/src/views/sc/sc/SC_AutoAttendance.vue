<template>
  <div>自动考勤内容（项目ID：{{ projectId }}）</div>
</template>
<script setup>
const props = defineProps({ projectId: String, projectData: Object });
</script> 