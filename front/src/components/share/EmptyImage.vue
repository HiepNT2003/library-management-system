<template>
    <div class="empty_img">
        <Icon icon="ion:images" width="20" height="20" />
    </div>
</template>
<script setup>
import { Icon } from "@iconify/vue";

</script>
<style lang="scss" scoped>
.empty_img {
    border: 1px solid #dcdcdc;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    border-radius: 8px;
    width: 150px;
    height: 220px;
    cursor: pointer;
    margin-bottom: 4px;
}
</style>