<script setup lang="ts">
import { utc } from '../format';

// A red ink seal, stamped on an account or character only while it is banned or muted.
defineProps<{ kind: 'banned' | 'muted'; until?: string | null }>();
</script>

<template>
	<span class="seal" :class="kind" :title="until ? `Until ${utc(until)}` : 'Permanent'">
		{{ kind }}
		<small>{{ until ? utc(until).slice(0, 10) : 'permanent' }}</small>
	</span>
</template>

<style scoped>
.seal {
	display: inline-flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	padding: 4px 10px 3px;
	border: 2px solid var(--seal);
	border-radius: 4px;
	color: var(--seal);
	font: 700 13px/1 var(--font);
	letter-spacing: 0.16em;
	text-transform: uppercase;
	transform: rotate(-4deg);
	box-shadow: inset 0 0 0 1px rgba(184, 51, 31, 0.25);
	background: rgba(184, 51, 31, 0.05);
	user-select: none;
}
.seal.muted {
	border-style: double;
	border-width: 3px;
}
small {
	font: 600 9px/1.4 var(--font);
	letter-spacing: 0.08em;
	margin-top: 2px;
}
</style>
