<!--
 *Author：codesoft
 *Contact：971926469@qq.com
 *业务请在@/extension/sc/sc/SC_WorkerBaseInfo.jsx或SC_WorkerBaseInfo.vue文件编写
 *新版本支持vue或【表.jsx]文件编写业务,文档见:https://v3.volcore.xyz/docs/view-grid、https://v3.volcore.xyz/docs/web
 -->
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
               :extend="extend">
        <!-- 自定义组件数据槽扩展，更多数据槽slot见文档 -->
        <template #gridHeader>
        </template>
    </view-grid>
</template>
<script setup lang="jsx">
    import extend from "@/extension/sc/sc/SC_WorkerBaseInfo.jsx";
    import viewOptions from './SC_WorkerBaseInfo/options.js'
    import { ref, reactive, getCurrentInstance } from "vue";
    const grid = ref(null);
    const { proxy } = getCurrentInstance()
    //http请求，proxy.http.post/get
    const { table, editFormFields, editFormOptions, searchFormFields, searchFormOptions, columns, detail, details } = reactive(viewOptions())
    //对外暴露数据
    defineExpose({})
</script>
<style lang="less" scope>
</style>
