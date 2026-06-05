<script setup lang="ts">
import { gsap } from 'gsap';
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import type { ComponentPublicInstance } from 'vue';

type DurationValue = string | number;

interface MessageOptions {
    id?: string | number;
    text?: string;
    content?: string;
    x?: string;
    y?: string;
    s?: string;
    color?: string;
    d?: DurationValue;
    duration?: DurationValue;
    delay?: DurationValue;
}

interface RuntimeMessage {
    id: string;
    filterId: string;
    content: string;
    x: string;
    y: string;
    s: string;
    color: string;
    duration: number;
    delay: number;
    source: 'default' | 'prop' | 'local';
}

const props = withDefaults(defineProps<{
    text?: string;
    items?: MessageOptions[];
    autoplay?: boolean;
}>(), {
    text: '!!!(づ￣ 3￣)づ╭❤～',
    autoplay: true,
});

const introDuration = 1.5;
const exitDuration = 0.45;
const componentId = `message-${Math.random().toString(36).slice(2)}`;

const messages = ref<HTMLElement | null>(null);
const localMessageList = ref<MessageOptions[]>([]);
const localMessagesStarted = ref(false);
const messageElementMap = new Map<string, HTMLElement>();
const messageTimelines = new Map<string, gsap.core.Timeline>();
const activeMessageKeys = new Map<string, string>();
let localMessageIndex = 0;
let isMounted = false;

const filterState = {
    blur: 10,
    alpha: 20,
    offset: -9,
};

const getMatrixValues = (alpha: number, offset: number): string => `
    1 0 0 0 0
    0 1 0 0 0
    0 0 1 0 0
    0 0 0 ${alpha.toFixed(3)} ${offset.toFixed(3)}
`;

const initialMatrixValues = getMatrixValues(filterState.alpha, filterState.offset);

const splitMessageContent = (content: string) => Array.from(content);

const normalizeDomId = (value: string) => value.replace(/[^a-zA-Z0-9_-]/g, '-');

const parseDurationSeconds = (duration: DurationValue | undefined, fallback = 0) => {
    if (typeof duration === 'number') {
        return Number.isFinite(duration) ? duration : fallback;
    }

    if (!duration) {
        return fallback;
    }

    const value = duration.trim();
    const durationValue = Number.parseFloat(value);

    if (!Number.isFinite(durationValue)) {
        return fallback;
    }

    return value.endsWith('ms') ? durationValue / 1000 : durationValue;
};

const getMessageContent = (message: MessageOptions) => message.content ?? message.text ?? props.text;

const normalizeMessage = (
    message: MessageOptions,
    index: number,
    source: RuntimeMessage['source'],
): RuntimeMessage => {
    const id = String(message.id ?? `${source}-${index}`);

    return {
        id,
        filterId: `${componentId}-${normalizeDomId(id)}`,
        content: getMessageContent(message),
        x: message.x ?? '50%',
        y: message.y ?? `${60 + index * 72}px`,
        s: message.s ?? '50px',
        color: message.color ?? 'red',
        duration: parseDurationSeconds(message.duration ?? message.d, 5),
        delay: parseDurationSeconds(message.delay, 0),
        source,
    };
};

const messageList = computed<RuntimeMessage[]>(() => {
    const propItems = props.items ?? [];

    if (!propItems.length && !localMessageList.value.length && !localMessagesStarted.value) {
        return [normalizeMessage({ id: 'default', content: props.text }, 0, 'default')];
    }

    return [
        ...propItems.map((message, index) => normalizeMessage(message, index, 'prop')),
        ...localMessageList.value.map((message, index) => normalizeMessage(message, propItems.length + index, 'local')),
    ];
});

const setMessageRef = (id: string) => (element: Element | ComponentPublicInstance | null) => {
    let targetElement: unknown = element;

    if (element && !(element instanceof Element)) {
        targetElement = element.$el;
    }

    if (targetElement instanceof HTMLElement) {
        messageElementMap.set(id, targetElement);
        return;
    }

    messageElementMap.delete(id);
};

const getMessageAnimationKey = (message: RuntimeMessage) => JSON.stringify({
    content: message.content,
    x: message.x,
    y: message.y,
    s: message.s,
    color: message.color,
    duration: message.duration,
    delay: message.delay,
});

const clearMessageAnimation = (id: string) => {
    messageTimelines.get(id)?.kill();
    messageTimelines.delete(id);
    activeMessageKeys.delete(id);
};

const removeLocalMessage = (id: string) => {
    localMessageList.value = localMessageList.value.filter((message) => String(message.id) !== id);
};

const applyMessageFilterState = (message: RuntimeMessage, state: typeof filterState) => {
    const blurElement = document.getElementById(`${message.filterId}-blur`);
    const matrixElement = document.getElementById(`${message.filterId}-matrix`);

    blurElement?.setAttribute('stdDeviation', state.blur.toFixed(3));
    matrixElement?.setAttribute('values', getMatrixValues(state.alpha, state.offset));
};

const runMessageAnimation = (message: RuntimeMessage) => {
    const messageElement = messageElementMap.get(message.id);
    const bodyElement = messageElement?.querySelector<HTMLElement>('.message-body');
    const charElements = Array.from(messageElement?.querySelectorAll<HTMLElement>('.message-char') ?? []);

    if (!messageElement || !bodyElement) {
        return;
    }

    clearMessageAnimation(message.id);

    const state = { ...filterState };
    const introStartY = -Math.max(bodyElement.offsetHeight * 1.6, 64);
    const exitY = -Math.max(bodyElement.offsetHeight * 0.8, 24);
    const timeline = gsap.timeline({
        delay: props.autoplay ? message.delay : 0,
        paused: !props.autoplay,
        onComplete: () => {
            if (message.source === 'local') {
                removeLocalMessage(message.id);
            }
        },
    });

    applyMessageFilterState(message, state);

    timeline.set(messageElement, {
        filter: `url(#${message.filterId})`,
    });

    timeline.set(bodyElement, {
        xPercent: -50,
        y: introStartY,
        opacity: 1,
    });

    timeline.set(charElements, {
        y: 0,
        rotation: 0,
        transformOrigin: 'bottom center',
    });

    timeline.to(bodyElement, {
        y: 0,
        duration: introDuration,
        ease: 'power2.out',
    }, 0);

    timeline.to(state, {
        blur: 0,
        alpha: 1,
        offset: 0,
        duration: 0.9,
        ease: 'power2.out',
        onUpdate: () => applyMessageFilterState(message, state),
        onComplete: () => {
            applyMessageFilterState(message, state);
            gsap.set(messageElement, { filter: 'none' });
        },
    }, 0.5);

    if (charElements.length) {
        timeline.to(charElements, {
            keyframes: [
                { y: -8, rotation: 5, duration: 0.35, ease: 'power2.out' },
                { y: 0, rotation: 0, duration: 0.35, ease: 'power2.inOut' },
            ],
            stagger: {
                each: 0.08,
                from: 'start',
            },
        }, introDuration);
    }

    timeline.to(bodyElement, {
        y: exitY,
        opacity: 0,
        duration: exitDuration,
        ease: 'power2.in',
    }, introDuration + message.duration);

    messageTimelines.set(message.id, timeline);
    activeMessageKeys.set(message.id, getMessageAnimationKey(message));
};

const syncMessageAnimations = async () => {
    if (!isMounted) {
        return;
    }

    await nextTick();

    const currentIds = new Set(messageList.value.map((message) => message.id));

    Array.from(activeMessageKeys.keys()).forEach((id) => {
        if (!currentIds.has(id)) {
            clearMessageAnimation(id);
            messageElementMap.delete(id);
        }
    });

    messageList.value.forEach((message) => {
        const animationKey = getMessageAnimationKey(message);

        if (activeMessageKeys.get(message.id) === animationKey) {
            return;
        }

        runMessageAnimation(message);
    });
};

const addMessage = (message: string | MessageOptions) => {
    const nextMessage = typeof message === 'string' ? { content: message } : message;
    const id = String(nextMessage.id ?? `local-${Date.now()}-${localMessageIndex++}`);

    localMessagesStarted.value = true;
    localMessageList.value = [
        ...localMessageList.value,
        {
            ...nextMessage,
            id,
        },
    ];

    return id;
};

const removeMessage = (id: string | number) => {
    const messageId = String(id);

    clearMessageAnimation(messageId);
    removeLocalMessage(messageId);
};

const clearMessages = () => {
    messageTimelines.forEach((timeline) => timeline.kill());
    messageTimelines.clear();
    activeMessageKeys.clear();
    messageElementMap.clear();
    localMessagesStarted.value = true;
    localMessageList.value = [];
};

const play = () => {
    messageTimelines.forEach((timeline) => timeline.play());
};

const pause = () => {
    messageTimelines.forEach((timeline) => timeline.pause());
};

watch(messageList, () => {
    void syncMessageAnimations();
});

onMounted(() => {
    isMounted = true;
    void syncMessageAnimations();
});

onUnmounted(() => {
    clearMessages();
    isMounted = false;
});

defineExpose({
    addMessage,
    removeMessage,
    clearMessages,
    play,
    pause,
});
</script>

<template>
    <Teleport to="body">
        <div class="message-container">
            <div class="messages" ref="messages">
                <div v-for="message in messageList" :key="message.id" :ref="setMessageRef(message.id)" class="message"
                    :data-id="message.id" :style="{
                        '--s': message.s,
                        '--x': message.x,
                        '--y': message.y,
                        '--d': `${message.duration}s`,
                        background: message.color,
                    }">
                    <div class="message-body">
                        <span class="message-content">
                            <span v-for="(char, index) in splitMessageContent(message.content)"
                                :key="`${message.id}-${index}`" class="message-char">
                                {{ char }}
                            </span>
                        </span>
                    </div>
                </div>
            </div>
        </div>
        <svg style="display: none;">
            <defs>
                <filter v-for="message in messageList" :key="message.filterId" :id="message.filterId" x="-50%" y="-50%"
                    width="200%" height="200%">
                    <feGaussianBlur :id="`${message.filterId}-blur`" in="SourceGraphic" stdDeviation="10"
                        result="blur" />
                    <feColorMatrix :id="`${message.filterId}-matrix`" in="blur" mode="matrix"
                        :values="initialMatrixValues" />
                </filter>
            </defs>
        </svg>
    </Teleport>
</template>

<style scoped lang="scss">
.message-container {
    position: fixed;
    top: -2rem;
    width: 100%;
    height: 2rem;
    background: red;
    pointer-events: none;
    z-index: 9999;

    .messages {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: inherit;

        .message {
            position: absolute;
            bottom: 0;
            left: 0;
            width: 100%;
            height: 1rem;
            --x: 100px;
            --y: 100px;
            --s: 50px;
            background: lawngreen;
            filter: none;
            will-change: filter;

            .message-body {
                position: absolute;
                width: fit-content;
                padding: calc(var(--s) * 0.2);
                height: var(--s);
                left: var(--x);
                top: var(--y);
                display: flex;
                justify-content: center;
                align-items: center;
                border-radius: calc(var(--s) * 0.5);
                background: inherit;
                opacity: 0;
                will-change: transform, opacity;

                .message-content {
                    display: inline-flex;
                    align-items: center;
                    font-size: calc(var(--s) * 0.5);
                    color: white;

                    .message-char {
                        display: inline-block;
                        white-space: pre;
                        transform-origin: bottom center;
                    }
                }
            }
        }
    }
}
</style>
