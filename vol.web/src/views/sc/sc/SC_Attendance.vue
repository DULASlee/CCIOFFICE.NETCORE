<template>
  <view-grid ref="grid"
             :columns="columns"
             :detail="detail"
             :details="details"
             :editFormFields="editFormFields"
             :editFormOptions="editFormOptions"
             :searchFormFields="searchFormFields"
             :searchFormOptions="searchFormOptions"
             :table="table"
             :extend="extend"
             :onInit="onInit"
             :onInited="onInited"
             :searchBefore="searchBefore"
             :searchAfter="searchAfter"
             :addBefore="addBefore"
             :updateBefore="updateBefore"
             :rowClick="rowClick"
             :modelOpenBefore="modelOpenBefore"
             :modelOpenAfter="modelOpenAfter">
    <template #gridHeader></template>
  </view-grid>
</template>
<script setup lang="jsx">
import extend from "@/extension/sc/sc/SC_WorkerAttendance.jsx";
import viewOptions from './SC_WorkerAttendance/options.js'
import { ref, reactive, getCurrentInstance, defineProps } from "vue";
const grid = ref(null);
const { proxy } = getCurrentInstance()
const props = defineProps({
  projectId: [String, Number],
  projectData: Object
});
const { table, editFormFields, editFormOptions, searchFormFields, searchFormOptions, columns, detail, details } = reactive(viewOptions())

let gridRef;
const onInit = async ($vm) => {
  gridRef = $vm;
}
const onInited = async () => {};
const searchBefore = async (param) => {
  // 详情页Tab模式：只查当前项目的工地考勤
  if (props.projectId) {
    param.wheres = (param.wheres || []).filter(w => w.name !== 'ProjectID');
    param.wheres.push({ name: 'ProjectID', value: props.projectId });
  }
  return true;
}
const searchAfter = async (rows, result) => { return true; };
const addBefore = async (formData) => { return true; };
const updateBefore = async (formData) => { return true; };
const rowClick = ({ row, column, event }) => {};
const modelOpenBefore = async (row) => { return true; };
const modelOpenAfter = (row) => {};
defineExpose({})
</script>
<style lang="less" scoped>
</style> 