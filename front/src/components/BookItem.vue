<template>
    <div class="book">
        <img :src="book.imageUrl" alt="" class="book-image">
        <div class="book-content">
            <p class="name">{{ book.title }}</p>
            <p class="author">{{ book.publisher }}</p>
        </div>
    </div>
</template>
<script>
export default {
    props: {
        book: {
            type: Object,
            default: () => { }
        }
    }
}
</script>
<style lang="scss" scoped>
.book {
    width: 208px;
    height: 360px;
    padding: 12px;
    border-radius: 8px;
    display: inline-flex;
    flex-direction: column;

    img {
        width: 100%;
        height: 278px;
    }
}
</style>