<!--
这是生成的文件，事件处理、自定义配置，见移动端文档：表单、表格配置
Author:vol
QQ:283591387
Date:2024
-->
<template>
	<vol-view ref="viewRef" :table="table" :columns="columns" :table-data="tableData"
			  :searchFormFields="searchFormFields" :searchFormOptions="searchFormOptions" :editFormFields="editFormFields"
			  :editFormOptions="editFormOptions" @searchClick="loadData" @addClick="modelOpenBefore" :saveBefore="saveBefore"
			  :saveAfter="saveAfter" :delBefore="delBefore">
		<!--表格配置 -->
		<vol-table ref="tableRef" :ck="false" :index="false" :default-load="true" :url="tableUrl" @rowClick="modelOpenBefore" :loadBefore="searchBefore"
				   :loadAfter="searchAfter" :direction="direction" :titleField="table.titleField" :columns="columns"
				   :table-data="tableData">
		</vol-table>
	</vol-view>
</template>
<script setup>
import options from "./SC_ProjectInfoOptions.js";
import { onLoad } from '@dcloudio/uni-app'
import { ref, reactive, getCurrentInstance, defineEmits, defineExpose, defineProps, watch, nextTick } from "vue";
import VolView from '@/components/vol-view/vol-view.vue';
import VolTable from '@/components/vol-table/vol-table.vue';
const { proxy } = getCurrentInstance();
	//发起请求proxy.http.get/post
	//消息提示proxy.$toast()

	//表格显示方式:list=列表显示，horizontal=表格显示
	const direction = ref('list')

	//vol-view组件
	const viewRef = ref(null);
	//table组件
	const tableRef = ref(null);

	//表格数据，可以直接获取使用
	const tableData = ref([]);

	//编辑、查询、表格配置
	//要对table注册事件、格式化、按钮等，看移动端文档上的table示例配置
	//表单配置看移动端文档上的表单示例配置，searchFormOptions查询配置，editFormOptions编辑配置
	const { table, searchFormFields, searchFormOptions, editFormFields, editFormOptions, columns } = reactive(options());
	const tableUrl = ref('api/' + table.tableName + '/getPageData');

	//查询前方法，可以设置查询条件(与生成页面文档上的searchBefore配置一致)
	const searchBefore = (params) => {
		return true;
	}

	//查询后方法，res返回的查询结果
	const searchAfter = (res) => {
		nextTick(() => {
			viewRef.value.searchAfter(res);
		})
		return true;
	}

	//打开新建、编辑弹出框
	const modelOpenBefore = (row, index, obj, callback) => {
        //跳转到新页面编辑
        uni.navigateTo({
			url: "/pages/sc/SC_ProjectInfo/SC_ProjectInfoEdit?id=" + ((row || {})[table.key] || ''),
            fail(e) {
                console.log(e)
            }
		})

		//与上面二选一，当前页面弹出框编辑或跳转新页面编辑
		////新建操作
		//if (!row) {
		//	//这里可以设置默认值：editFormFields.字段=
		//	callback(true); //返回false，不会弹出框
		//	return;
		//}
		////编辑
		//viewRef.value.showEdit(row, index);
		////这里可以给弹出框字段设置或修改值：editFormFields.字段=
	}

	//新建、编辑保存前
	const saveBefore = (formData, isAdd, callback) => {
		callback(true); //返回false，不会保存
	}

	//新建、编辑保存后
	const saveAfter = (res, isAdd) => {}

	//主表删除前方法
	const delBefore = (ids, rows, result) => {
		return true;//返回false不会执行删除
	}

	//调用表格查询
	const loadData = (params) => {
		//生成查询条件
		params = params || viewRef.value.getSearchParameters();
		//params可以设置查询条件
		tableRef.value.load(params);
	}

	//如果是其他页面跳转过来的，获取页面跳转参数
	onLoad((ops) => {})

	//监听表单输入，做实时计算
	// watch(
	// 	() => editFormFields.字段,
	// 	(newValue, oldValue) => {
	// 	})
	defineExpose({
		//对外暴露数据
	})
</script>
<style lang="less" scoped>
</style>
