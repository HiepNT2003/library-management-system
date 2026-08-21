<template>
    <div class="toast-panel" v-if="toastMessage">
        <div class="toast-item" :class="toastMessage.status">
            <div class="toast_msg" :class="toastMessage.status">
                <Icon icon="streamline:delete-1-solid" width="14" height="14" class="close" @click="closeToast" />
                <h3>{{ toastMessage.title }}</h3>
                <p v-if="toastMessage.message">{{ toastMessage.message }}</p>
            </div>
        </div>
    </div>
</template>
<script>
import { Icon } from '@iconify/vue'
import { useToastMessageStore } from '../stores/toastMessage';

export default {
    components: { Icon },
    props: {
        toastMessage: {
            type: Object,
            default: () => { }
        }
    },
    created() {
        if (this.toastMessage.showTime) {
            const toastMessageStore = useToastMessageStore();
            setTimeout(() => { toastMessageStore.removeToastMessage(this.toastMessage.id) }, this.toastMessage.showTime
            );
        }
    },
    methods: {
        closeToast() {
            const toastMessageStore = useToastMessageStore();
            toastMessageStore.removeToastMessage(this.toastMessage.id)
        }
    }
}
</script>
<style lang="scss" scoped>
.toast-panel {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    transition: all 0.5s ease 0s;
    position: absolute;
    padding: 0 1rem;
    position: fixed;
    top: 20px;
    right: 0;
    z-index: 101;
}

.toast-item {
    /*overflow: hidden;*/
    max-height: 25rem;
    min-height: 36px;
    transition: all 0.5s ease 0s;
    position: relative;
    opacity: 0;
    transform: translateX(100%);
    animation: toast-slide-in 0.4s ease forwards;
}

@keyframes toast-slide-in {
    from {
        opacity: 0;
        transform: translateX(100%);
    }

    to {
        opacity: 1;
        transform: translateX(0);
    }
}

.toast_msg {
    background: #fff;
    color: #f5f5f5;
    padding: 0.8rem 2rem 0.8rem 2rem;
    text-align: center;
    border-radius: 1rem;
    position: relative;
    font-weight: 300;
    margin: 1rem 0;
    text-align: left;
    max-width: 27rem;
    min-width: 20rem;
    transition: all 0.5s ease 0s;
    opacity: 1;
    border: 0.15rem solid #fff2;
    box-shadow: 0 0 1.5rem 0 #1a1f4360;
}

.toast_msg:before {
    content: "";
    position: absolute;
    width: 0.5rem;
    height: calc(100% - 1.5rem);
    top: 0.75rem;
    left: 0.5rem;
    z-index: 0;
    border-radius: 1rem;
    background: var(--clr);
}

.toast_msg h3 {
    font-size: 1rem;
    margin: 0;
    line-height: 1.35rem;
    font-weight: 600;
    position: relative;
    color: var(--clr);
}

.toast_msg p {
    position: relative;
    font-size: 0.95rem;
    z-index: 1;
    margin: 0.25rem 0 0;
    color: #595959;
    line-height: 1.3rem;
}

.close {
    position: absolute;
    color: #666666;
    text-align: center;
    right: 1rem;
    cursor: pointer;
    border-radius: 100%;
}

.close:after {
    position: absolute;
    font-family: 'Varela Round', san-serif;
    width: 100%;
    height: 100%;
    left: 0;
    font-size: 1.8rem;
    content: "+";
    transform: rotate(-45deg);
    border-radius: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    color: #595959;
    text-indent: 1px;
}

.close:hover:after {
    background: var(--clr);
    color: #fff;
}

.toast-item.Warning {
    animation-delay: 1s;
}

.toast-item.Error {
    animation-delay: 0s;
}

.toast_msg.Help {
    --bg: #05478a;
    --clr: #0070e0;
    --brd: #0070e0;
}

.icon-help:after {
    content: "?";
}

.toast_msg.Success {
    --bg: #005e38;
    --clr: #03a65a;
    --brd: var(--cs3);
}

.icon-success:after {
    content: "L";
    font-size: 1.5rem;
    font-weight: bold;
    padding-bottom: 0.35rem;
    transform: rotateY(180deg) rotate(-38deg);
    text-indent: 0.1rem;
}

.toast_msg.Warning {
    --bg: #c24914;
    --clr: #fc8621;
    --brd: #fc8621;
}

.icon-warning:after {
    content: "!";
    font-weight: bold;
}

.toast_msg.Error {
    --bg: #851d41;
    --clr: #db3056;
    --brd: #db3056;
}

.icon-error:after {
    content: "+";
    font-size: 2.85rem;
    line-height: 1.2rem;
    transform: rotate(45deg);
}

.toast_msg a {
    color: var(--clr);
}

.toast_msg a:hover {
    color: var(--bg);
}
</style>