<template>
    <div class="book-card" @click="$router.push(`${isPublicPage ? '' : '/user'}/books/${book.bookId}`)">
        <div class="book-cover">
            <img v-if="book.imageUrl" :src="book.imageUrl" :alt="book.title" />
            <div v-else class="book-cover-placeholder">
                <span class="placeholder-icon">📖</span>
            </div>
            <div class="book-badge" v-if="book.availableCopies > 0">
                {{ book.availableCopies }} có sẵn
            </div>
            <div class="book-badge badge-unavailable" v-else-if="book.totalCopies > 0">
                Không thể mượn
            </div>
        </div>
        <div class="book-info">
            <div class="book-type-tag" :class="typeClass">{{ typeLabel }}</div>
            <h3 class="book-title">{{ book.title }}</h3>
            <div class="book-authors" v-if="book.authors?.length">
                {{book.authors.map(a => a.name || a).join(', ')}}
            </div>
            <div class="book-meta">
                <span v-if="book.publishedYear">{{ book.publishedYear }}</span>
                <span v-if="book.publisher" class="separator">·</span>
                <span v-if="book.publisher" class="publisher">{{ truncate(book.publisher, 30) }}</span>
            </div>
            <div class="book-categories" v-if="book.categories?.length">
                <span v-for="cat in book.categories.slice(0, 2)" :key="cat.categoryId" class="cat-tag">
                    {{ cat.name }}
                </span>
            </div>
            <div class="book-reason" v-if="book.reason">
                <span class="reason-icon">✨</span> {{ book.reason }}
            </div>
        </div>
        <div class="book-actions">
            <button v-if="showBorrow && (book.availableCopies > 0 || book.documentTypeId === 4)" class="btn-borrow"
                @click.stop="$emit('borrow', book)">
                                <!-- <Icon v-if="book.documentTypeId !== 4" icon="fluent:border-inside-16-regular" width="16" height="16" /> -->
                {{ book.documentTypeId === 4 ? '📖 Đọc online' : '📚 Đặt mượn' }}
            </button>
            <button v-else-if="showBorrow && book.documentTypeId !== 2" class="btn-borrow btn-runOut">
                Hết bản sao có thể mượn
            </button>
        </div>
    </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const props = defineProps({
    book: { type: Object, required: true },
    showBorrow: { type: Boolean, default: true }
})

defineEmits(['borrow', 'notify'])
const route = useRoute()

const typeLabel = computed(() => {
    const map = { 1: 'Sách', 2: 'Bài trích', 3: 'Luận án', 4: 'Ebook' }
    return map[props.book.documentTypeId] ?? 'Tài liệu'
})

const typeClass = computed(() => {
    const map = { 1: 'type-book', 2: 'type-article', 3: 'type-thesis', 4: 'type-ebook' }
    return map[props.book.documentTypeId] ?? ''
})

const isPublicPage = computed(() => route?.name?.includes("public"))

const truncate = (str, len) => !str ? '' : str.length > len ? str.slice(0, len) + '...' : str
</script>

<style lang="scss" scoped>
.book-card {
    display: flex;
    gap: 16px;
    padding: 16px;
    background: #fff;
    border-radius: 12px;
    border: 1px solid #e8e8e8;
    cursor: pointer;
    transition: all 0.2s;

    &:hover {
        border-color: #3949ab;
        box-shadow: 0 4px 16px rgba(57, 73, 171, 0.12);
        transform: translateY(-1px);
    }
}

.book-cover {
    position: relative;
    flex-shrink: 0;
    width: 72px;
    height: fit-content;
    margin: auto 0;

    img {
        width: 72px;
        height: 96px;
        object-fit: cover;
        border-radius: 6px;
        border: 1px solid #e0e0e0;
        font-size: 12px;
        overflow: hidden;
        max-width: 72px;
        max-height: 96px;
        min-height: 96px;
    }
}

.book-cover-placeholder {
    width: 72px;
    height: 96px;
    background: linear-gradient(135deg, #e8eaf6, #c5cae9);
    border-radius: 6px;
    display: flex;
    align-items: center;
    justify-content: center;

    .placeholder-icon {
        font-size: 28px;
    }
}

.book-badge {
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    text-align: center;
    background: #2e7d32;
    color: #fff;
    font-size: 10px;
    font-weight: 700;
    padding: 2px 4px;
    border-radius: 0 0 6px 6px;

    &.badge-unavailable {
        background: #757575;
    }
}

.book-info {
    flex: 1;
    min-width: 0;
}

.book-type-tag {
    display: inline-block;
    font-size: 11px;
    font-weight: 700;
    padding: 2px 8px;
    border-radius: 99px;
    margin-bottom: 6px;

    &.type-book {
        background: #e8eaf6;
        color: #3949ab;
    }

    &.type-article {
        background: #e0f2f1;
        color: #00695c;
    }

    &.type-thesis {
        background: #fce4ec;
        color: #c2185b;
    }

    &.type-ebook {
        background: #fff3e0;
        color: #e65100;
    }
}

.book-title {
    font-size: 15px;
    font-weight: 700;
    color: #1a1a2e;
    margin: 0 0 4px;
    line-height: 1.4;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
}

.book-authors {
    font-size: 13px;
    color: #3949ab;
    font-weight: 500;
    margin-bottom: 4px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.book-meta {
    font-size: 12px;
    color: #888;
    margin-bottom: 6px;

    .separator {
        margin: 0 4px;
    }
}

.book-categories {
    display: flex;
    gap: 4px;
    flex-wrap: wrap;
}

.cat-tag {
    font-size: 11px;
    padding: 2px 8px;
    background: #f5f5f5;
    color: #666;
    border-radius: 99px;
}

.book-reason {
    font-size: 12px;
    color: #e65100;
    margin-top: 6px;
    font-style: italic;

    .reason-icon {
        font-style: normal;
    }
}

.book-actions {
    display: flex;
    flex-direction: column;
    justify-content: center;
    flex-shrink: 0;
}

.btn-borrow {
    padding: 8px 14px;
    background: #3949ab;
    color: #fff;
    border: none;
    border-radius: 8px;
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    white-space: nowrap;
    transition: background 0.15s;

    &:hover {
        background: #2c3a8c;
    }

    &.btn-runOut {
        background: #fff;
        color: #ababad;
        border: 1.5px solid #ababad;
        cursor: default;
    }
}
</style>