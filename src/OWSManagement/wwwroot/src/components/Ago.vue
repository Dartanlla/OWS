<script setup lang="ts">
import { ago, utc } from '../format';

defineProps<{ at: string | null | undefined }>();
</script>

<template>
	<time class="nowrap" :title="utc(at)">{{ ago(at) }}</time>
</template>
