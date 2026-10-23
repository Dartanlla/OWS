<script setup lang="ts">
import { ref, watch, nextTick } from 'vue';
import { errorText } from '../api';

// A confirm step for every action that changes the game. The button names the action; a reason is written to the
// audit log and, for sanctions, shown to the player.
const props = defineProps<{
	open: boolean;
	title: string;
	confirmLabel: string;
	danger?: boolean;
	needsReason?: boolean;
	run: (reason: string) => Promise<string | void>;
}>();
const emit = defineEmits<{ (e: 'close'): void; (e: 'done', message: string): void }>();

const reason = ref('');
const busy = ref(false);
const error = ref('');
const dialog = ref<HTMLDialogElement>();

watch(
	() => props.open,
	async (open) => {
		reason.value = '';
		error.value = '';
		await nextTick();
		if (open) dialog.value?.showModal();
		else dialog.value?.close();
	},
);

async function confirm() {
	if (props.needsReason && !reason.value.trim()) {
		error.value = 'Write a reason first.';
		return;
	}
	busy.value = true;
	error.value = '';
	try {
		const message = await props.run(reason.value.trim());
		emit('done', message || 'Done.');
		emit('close');
	} catch (e) {
		error.value = errorText(e);
	} finally {
		busy.value = false;
	}
}
</script>

<template>
	<dialog ref="dialog" @close="emit('close')" @cancel.prevent="emit('close')">
		<form method="dialog" @submit.prevent="confirm">
			<h3>{{ title }}</h3>
			<slot />
			<label v-if="needsReason" class="field">
				Reason
				<textarea v-model="reason" maxlength="500" autofocus></textarea>
			</label>
			<p v-if="error" class="notice bad">{{ error }}</p>
			<div class="buttons">
				<button type="button" @click="emit('close')">Cancel</button>
				<button type="submit" :class="danger ? 'danger' : 'primary'" :disabled="busy">{{ confirmLabel }}</button>
			</div>
		</form>
	</dialog>
</template>

<style scoped>
dialog {
	border: 1px solid var(--rule-strong);
	border-radius: 4px;
	background: var(--paper-raised);
	color: var(--ink);
	padding: 20px 22px;
	width: min(460px, calc(100vw - 32px));
}
dialog::backdrop {
	background: rgba(38, 32, 26, 0.45);
}
h3 {
	font: 500 20px/1.2 var(--font-display);
	margin: 0 0 14px;
}
form {
	display: grid;
	gap: 12px;
}
form :deep(p) {
	margin: 0;
}
.buttons {
	display: flex;
	justify-content: flex-end;
	gap: 8px;
	margin-top: 4px;
}
</style>
