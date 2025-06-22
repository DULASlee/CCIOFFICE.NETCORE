<!--
 *Author：codesoft
 *Contact：971926469@qq.com
 *业务请在@/extension/sc/sc/SC_ConTractings.jsx或SC_ConTractings.vue文件编写
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
        <!-- 自定义组件数据槽扩展，更多数据槽slot见文档 -->
        <template #gridHeader>
        </template>
    </view-grid>
</template>
<script setup lang="jsx">
    import extend from "@/extension/sc/sc/SC_ConTractings.jsx";
    import viewOptions from './SC_ConTractings/options.js'
    import { ref, reactive, getCurrentInstance, watch, onMounted, defineProps } from "vue";
    
    const grid = ref(null);
    const { proxy } = getCurrentInstance()
    
    const props = defineProps({
        projectId: [String, Number],
        projectData: Object
    });
    
    //http请求，proxy.http.post/get
    const { table, editFormFields, editFormOptions, searchFormFields, searchFormOptions, columns, detail, details } = reactive(viewOptions())

    let gridRef;//对应[表.jsx]文件中this.使用方式一样
    
    //生成对象属性初始化
    const onInit = async ($vm) => {
        gridRef = $vm;
        //与jsx中的this.xx使用一样，只需将this.xx改为gridRef.xx
        //更多属性见：https://v3.volcore.xyz/docs/view-grid
    }
    
    //生成对象属性初始化后,操作明细表配置用到
    const onInited = async () => {
        // 如果是在项目详情页Tab中，立即触发一次查询
        if (props.projectData) {
            // 添加调试日志
            console.log('参建单位组件初始化，项目数据:', props.projectData);
            console.log('GeneralConTractorID:', props.projectData.GeneralConTractorID);
            console.log('BuilderID:', props.projectData.BuilderID);
            console.log('LaborSubConTractorID:', props.projectData.LaborSubConTractorID);
            
            // 触发查询
            if (gridRef && gridRef.refresh) {
                gridRef.refresh();
            }
        }
    }
    
    const searchBefore = async (param) => {
        // 详情页Tab模式：只查当前项目的3家参建单位
        if (props.projectData) {
            const ids = [
                props.projectData.GeneralConTractorID,
                props.projectData.BuilderID,
                props.projectData.LaborSubConTractorID
            ].filter(id => id && id !== '' && id !== null && id !== undefined);
            
            console.log('筛选参建单位，提取的ID列表:', ids);
            
            if (ids.length > 0) {
                // 清空原有条件
                param.wheres = [];
                
                // // 方案1：尝试使用多个OR条件（如果in查询不支持）
                // ids.forEach((id, index) => {
                //     const condition = {
                //         name: 'ConTractingID',
                //         value: id,
                //         displayType: '='
                //     };
                    
                //     // 第二个及以后的条件添加 or 逻辑
                //     if (index > 0) {
                //         condition.type = 'or';  // 尝试不同的属性名
                //         condition.logicalOperator = 'or';  // 或者这个
                //         condition.logic = 'or';  // 或者这个
                //     }
                    
                //     param.wheres.push(condition);
                // });
                
                //方案2：如果上面不行，试试这种格式
                param.wheres = [{
                    name: 'ConTractingID',
                    value: ids.join(','),  // 用逗号拼接
                    displayType: 'in'
                }];
                
                // 方案3：或者试试包装成对象
                // param.wheres = [{
                //     name: 'ConTractingID',
                //     value: { in: ids },
                //     displayType: 'in'
                // }];
                
                console.log('查询参数:', param);
                console.log('查询条件详情:', JSON.stringify(param.wheres, null, 2));
            } else {
                // 没有关联单位时，查不到数据
                console.log('没有找到任何参建单位ID，将返回空结果');
                param.wheres = [{ 
                    name: 'ConTractingID', 
                    value: '00000000-0000-0000-0000-000000000000',
                    displayType: '='
                }];
            }
        }
        return true;
    }
    
    const searchAfter = async (rows, result) => {
        if (props.projectData) {
            console.log('查询结果，共找到参建单位:', rows?.length || 0, '条');
            
            // 如果方案1（OR查询）没有正确筛选，尝试在前端手动筛选
            if (rows && rows.length > 3) {
                const ids = [
                    props.projectData.GeneralConTractorID,
                    props.projectData.BuilderID,
                    props.projectData.LaborSubConTractorID
                ].filter(Boolean);
                
                // 前端筛选
                const filteredRows = rows.filter(row => 
                    ids.includes(row.ConTractingID)
                );
                
                console.log('前端筛选后的结果:', filteredRows.length, '条');
                
                // 如果前端筛选有效，替换结果
                if (filteredRows.length <= 3 && filteredRows.length > 0) {
                    rows.splice(0, rows.length, ...filteredRows);
                    result.total = filteredRows.length;
                }
            }
        }
        return true;
    }
    
    const addBefore = async (formData) => {
        //新建保存前formData为对象，包括明细表，可以给给表单设置值，自己输出看formData的值
        return true;
    }
    
    const updateBefore = async (formData) => {
        //编辑保存前formData为对象，包括明细表、删除行的Id
        return true;
    }
    
    const rowClick = ({ row, column, event }) => {
        //查询界面点击行事件
        // grid.value.toggleRowSelection(row); //单击行时选中当前行;
    }
    
    const modelOpenBefore = async (row) => {//弹出框打开后方法
        return true;//返回false，不会打开弹出框
    }
    
    const modelOpenAfter = (row) => {
        //弹出框打开后方法,设置表单默认值,按钮操作等
    }
    
    // 监听projectData变化，当数据变化时重新查询
    watch(() => props.projectData, (newData) => {
        if (newData && gridRef && gridRef.refresh) {
            console.log('项目数据更新，重新查询参建单位');
            gridRef.refresh();
        }
    }, { deep: true });
    
    // 添加一个方法来检查后端支持的查询格式
    const checkBackendFormat = () => {
        if (gridRef && gridRef.searchFormFields) {
            console.log('搜索表单字段:', gridRef.searchFormFields);
        }
        if (gridRef && gridRef.table) {
            console.log('表格配置:', gridRef.table);
        }
    };
    
    // 在mounted时调用检查
    onMounted(() => {
        setTimeout(() => {
            checkBackendFormat();
        }, 1000);
    });
    
    //监听表单输入，做实时计算
    //watch(() => editFormFields.字段,(newValue, oldValue) => {	})
    
    //对外暴露数据
    defineExpose({})
</script>
<style lang="less" scope>
</style>