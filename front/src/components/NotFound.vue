<template>
    <div class="not_found_wrap">
        <img src="../assets/Images/NotFound.jpg" alt="" srcset="">
        <div class="content">{{ content }}</div>
    </div>
</template>
<script>
export default {
    props: {
        content: {
            type: String,
            default: "Không tìm thấy dữ liệu"
        }
    }
}
</script>
<style lang="scss" scoped>
.not_found_wrap {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;

    img {
        width: 240px;
    }

    .content {
        font-size: 20px;
    }
}
</style>