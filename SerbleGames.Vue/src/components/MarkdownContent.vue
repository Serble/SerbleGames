<template>
  <div class="markdown-content" v-html="renderedMarkdown"></div>
</template>

<script setup>
import { computed } from 'vue';
import { renderMarkdown } from '../utils/markdown.js';

const props = defineProps({
  content: {
    type: String,
    required: true
  }
});

const renderedMarkdown = computed(() => renderMarkdown(props.content));
</script>

<style scoped>
.markdown-content {
  @apply leading-relaxed break-words;
  overflow-wrap: break-word;
  word-wrap: break-word;
}

.markdown-content :deep(h1),
.markdown-content :deep(h2),
.markdown-content :deep(h3),
.markdown-content :deep(h4),
.markdown-content :deep(h5),
.markdown-content :deep(h6) {
  @apply font-bold mb-3 mt-4;
}

.markdown-content :deep(h1) {
  @apply text-2xl;
}

.markdown-content :deep(h2) {
  @apply text-xl;
}

.markdown-content :deep(h3) {
  @apply text-lg;
}

.markdown-content :deep(h4),
.markdown-content :deep(h5),
.markdown-content :deep(h6) {
  @apply text-base;
}

.markdown-content :deep(p) {
  @apply mb-3;
}

.markdown-content :deep(ul),
.markdown-content :deep(ol) {
  @apply ml-4 mb-3;
}

.markdown-content :deep(li) {
  @apply mb-1;
}

.markdown-content :deep(ul li) {
  @apply list-disc list-inside;
}

.markdown-content :deep(ol li) {
  @apply list-decimal list-inside;
}

.markdown-content :deep(blockquote) {
  @apply border-l-4 border-serble-primary pl-4 italic text-serble-text-muted mb-3;
}

.markdown-content :deep(code) {
  @apply bg-serble-dark px-2 py-1 rounded font-mono text-sm;
}

.markdown-content :deep(pre) {
  @apply bg-serble-dark p-4 rounded overflow-x-auto mb-3;
}

.markdown-content :deep(pre code) {
  @apply bg-transparent px-0 py-0;
}

.markdown-content :deep(a) {
  @apply text-serble-primary hover:underline;
}

.markdown-content :deep(hr) {
  @apply border-serble-border my-4;
}

.markdown-content :deep(strong) {
  @apply font-bold;
}

.markdown-content :deep(em) {
  @apply italic;
}

.markdown-content :deep(table) {
  @apply w-full border-collapse mb-3;
}

.markdown-content :deep(th),
.markdown-content :deep(td) {
  @apply border border-serble-border px-3 py-2 text-left;
}

.markdown-content :deep(th) {
  @apply bg-serble-dark font-bold;
}
</style>
