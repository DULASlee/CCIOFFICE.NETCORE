<template>
  <div>
    <slot></slot>
  </div>
</template>

<script>
export default {
  name: 'VolView',
  props: {
    table: Object,
    columns: Array,
    tableData: Array,
    searchFormFields: Object,
    searchFormOptions: Array,
    editFormFields: Object,
    editFormOptions: Array
  },
  methods: {
    searchAfter(res) {
      // 占位方法，实际项目可扩展
    }
  }
}
</script>

<style scoped>
</style>
