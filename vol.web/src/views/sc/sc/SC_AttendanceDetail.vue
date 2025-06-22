<template>
  <div>工地考勤详细内容（项目ID：{{ projectId }}）</div>
</template>
<script setup>
const props = defineProps({ projectId: String, projectData: Object });
</script> 