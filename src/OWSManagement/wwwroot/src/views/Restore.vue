<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { http, errorText, type Result, type RestoreSnapshot, type StagingStatus } from '../api';
import { num, itemName, leaf, utc } from '../format';
import Ago from '../components/Ago.vue';

interface Backup {
	name: string;
	bytes: number;
	writtenAt: string;
}

const route = useRoute();
const name = String(route.params.name);
const enabled = ref(true);
const backups = ref<Backup[]>([]);
const status = ref<StagingStatus | null>(null);
const chosen = ref('');
const live = ref<RestoreSnapshot | null>(null);
const backup = ref<RestoreSnapshot | null>(null);
const confirm = ref('');
const busy = ref(false);
const error = ref('');
const message = ref('');
let timer = 0;

const ready = computed(() => status.value?.state === 'ready');

type Row = { key: string; now: string; then: string };

function itemText(i: RestoreSnapshot['items'][number] | undefined): string {
	return i ? `${itemName(i.item)}${i.quantity > 1 ? ` × ${i.quantity}` : ''}` : '';
}

const itemChanges = computed<Row[]>(() => {
	if (!live.value || !backup.value) return [];
	const key = (i: RestoreSnapshot['items'][number]) => `${i.inventory} ${i.slot}`;
	const now = new Map(live.value.items.map((i) => [key(i), i]));
	const then = new Map(backup.value.items.map((i) => [key(i), i]));
	return [...new Set([...now.keys(), ...then.keys()])]
		.filter((k) => {
			const a = now.get(k);
			const b = then.get(k);
			return !a || !b || a.item !== b.item || a.quantity !== b.quantity || a.customData !== b.customData;
		})
		.sort((a, b) => a.localeCompare(b, undefined, { numeric: true }))
		.map((k) => ({ key: k, now: itemText(now.get(k)), then: itemText(then.get(k)) }));
});

function tagChanges(nowRows: { tag: string; value: string }[], thenRows: { tag: string; value: string }[]): Row[] {
	const now = new Map(nowRows.map((r) => [r.tag, r.value]));
	const then = new Map(thenRows.map((r) => [r.tag, r.value]));
	return [...new Set([...now.keys(), ...then.keys()])]
		.filter((k) => now.get(k) !== then.get(k))
		.sort()
		.map((k) => ({ key: leaf(k), now: now.has(k) ? now.get(k) || 'yes' : '', then: then.has(k) ? then.get(k) || 'yes' : '' }));
}
const skillChanges = computed(() => (live.value && backup.value ? tagChanges(live.value.abilities, backup.value.abilities) : []));
const questChanges = computed(() => (live.value && backup.value ? tagChanges(live.value.quests, backup.value.quests) : []));

async function loadState() {
	try {
		const { data } = await http.get('/Restore');
		enabled.value = data.enabled;
		backups.value = data.backups;
		status.value = data.status;
		if (!chosen.value && backups.value.length) chosen.value = backups.value[1]?.name ?? backups.value[0].name;
		if (ready.value && !live.value) await compare();
		error.value = '';
	} catch (e) {
		error.value = errorText(e);
	}
}

async function stage() {
	busy.value = true;
	live.value = null;
	backup.value = null;
	try {
		await http.post('/Restore/Stage', { dump: chosen.value });
		message.value = 'Loading the backup. This takes a minute or two.';
		status.value = { state: 'loading', dump: chosen.value, at: '', stopsAt: '', error: '' };
	} catch (e) {
		message.value = errorText(e);
	} finally {
		busy.value = false;
	}
}

async function compare() {
	const { data } = await http.get(`/Restore/Character/${encodeURIComponent(name)}`);
	live.value = data.live;
	backup.value = data.backup;
}

async function restore() {
	busy.value = true;
	try {
		const { data } = await http.post<Result>(`/Restore/Character/${encodeURIComponent(name)}`, { confirm: confirm.value });
		message.value = data.success ? `${name} is restored from ${status.value?.dump}. The change shows in their ledger.` : data.errorMessage ?? 'The restore failed.';
		if (data.success) {
			confirm.value = '';
			await compare();
		}
	} catch (e) {
		message.value = errorText(e);
	} finally {
		busy.value = false;
	}
}

onMounted(() => {
	loadState();
	timer = window.setInterval(() => {
		if (status.value?.state === 'loading') loadState();
	}, 3000);
});
onUnmounted(() => window.clearInterval(timer));
</script>

<template>
	<div class="page">
		<div class="page-head">
			<h1>Restore {{ name }}</h1>
			<span class="sub"><RouterLink :to="`/players/character/${encodeURIComponent(name)}`">Back to the character</RouterLink></span>
		</div>
		<p v-if="error" class="notice bad">{{ error }}</p>
		<p v-if="message" class="notice">{{ message }}</p>
		<p v-if="!enabled" class="notice bad">Restore is not set up on this server. Run scripts/hetzner/install-staging.sh on the box.</p>

		<template v-else>
			<h2>1 · Load a backup</h2>
			<p class="muted">Backups run every hour and are kept 14 days. The loaded copy is removed 2 hours after loading.</p>
			<form class="toolbar" @submit.prevent="stage">
				<select v-model="chosen" aria-label="Backup">
					<option v-for="b in backups" :key="b.name" :value="b.name">{{ utc(b.writtenAt) }} · {{ num(b.bytes / 1048576, 1) }} MB</option>
				</select>
				<button type="submit" :disabled="!chosen || busy || status?.state === 'loading'">Load backup</button>
				<span v-if="status" class="muted">
					<span class="tag" :class="{ ok: status.state === 'ready', live: status.state === 'loading', bad: status.state === 'failed' }">{{ status.state }}</span>
					{{ status.dump }}
					<template v-if="status.state === 'ready' && status.stopsAt"> · removed <Ago :at="status.stopsAt" /></template>
					<template v-if="status.error"> · {{ status.error }}</template>
				</span>
			</form>

			<template v-if="ready && live">
				<h2>2 · What changes</h2>
				<p v-if="!backup" class="notice bad">The backup has no character named {{ name }}.</p>
				<template v-else>
					<div class="table-wrap">
						<table>
							<thead><tr><th></th><th>Now</th><th>In the backup</th></tr></thead>
							<tbody>
								<tr><td>Juri</td><td class="juri">{{ num(live.gold) }}</td><td class="juri" :class="{ changed: live.gold !== backup.gold }">{{ num(backup.gold) }}</td></tr>
								<tr><td>Level</td><td>{{ live.level }} · job {{ live.jobLevel }}</td><td :class="{ changed: live.level !== backup.level || live.jobLevel !== backup.jobLevel }">{{ backup.level }} · job {{ backup.jobLevel }}</td></tr>
								<tr><td>Map</td><td>{{ live.mapName }} <span class="mono muted">{{ num(live.x) }}, {{ num(live.y) }}</span></td><td>{{ backup.mapName }} <span class="mono muted">{{ num(backup.x) }}, {{ num(backup.y) }}</span></td></tr>
							</tbody>
						</table>
					</div>

					<h2>Items · {{ itemChanges.length }} slot(s) differ</h2>
					<div class="table-wrap">
						<table>
							<thead><tr><th>Inventory, slot</th><th>Now</th><th>In the backup</th></tr></thead>
							<tbody>
								<tr v-for="r in itemChanges" :key="r.key"><td class="mono">{{ r.key }}</td><td>{{ r.now || '—' }}</td><td class="changed">{{ r.then || '—' }}</td></tr>
								<tr v-if="!itemChanges.length"><td colspan="3" class="empty">The items are the same.</td></tr>
							</tbody>
						</table>
					</div>

					<div class="grid-2">
						<section>
							<h2>Skills · {{ skillChanges.length }} differ</h2>
							<div class="table-wrap">
								<table>
									<thead><tr><th>Skill</th><th class="num">Now</th><th class="num">Backup</th></tr></thead>
									<tbody>
										<tr v-for="r in skillChanges" :key="r.key"><td>{{ r.key }}</td><td class="num">{{ r.now || '—' }}</td><td class="num changed">{{ r.then || '—' }}</td></tr>
										<tr v-if="!skillChanges.length"><td colspan="3" class="empty">Same skills.</td></tr>
									</tbody>
								</table>
							</div>
						</section>
						<section>
							<h2>Quests · {{ questChanges.length }} differ</h2>
							<div class="table-wrap">
								<table>
									<thead><tr><th>Quest</th><th>Now</th><th>Backup</th></tr></thead>
									<tbody>
										<tr v-for="r in questChanges" :key="r.key"><td>{{ r.key }}</td><td>{{ r.now || '—' }}</td><td class="changed">{{ r.then || '—' }}</td></tr>
										<tr v-if="!questChanges.length"><td colspan="3" class="empty">Same quests.</td></tr>
									</tbody>
								</table>
							</div>
						</section>
					</div>

					<h2>3 · Restore</h2>
					<p class="muted">
						The character must be offline. Everything above, its stats and its saved data go back to the backup. The account, other
						characters, shops and their escrow do not change.
					</p>
					<form class="toolbar" @submit.prevent="restore">
						<input v-model="confirm" type="text" :placeholder="`Type ${name} to confirm`" aria-label="Character name to confirm" autocomplete="off" />
						<button type="submit" class="danger" :disabled="busy || confirm.trim().toLowerCase() !== name.toLowerCase()">Restore {{ name }}</button>
					</form>
				</template>
			</template>
		</template>
	</div>
</template>

<style scoped>
td.changed {
	font-weight: 600;
	color: var(--accent-ink);
}
</style>
