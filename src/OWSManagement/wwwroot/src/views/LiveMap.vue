<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { http, errorText, type LivePosition } from '../api';
import { num } from '../format';
import Ago from '../components/Ago.vue';

/**
 * One map's minimap bake (DT_MinimapData): a square image centred on (centerX, centerY) that spans captureWidth world
 * units. World +X is up on the image and +Y is right.
 */
interface MapImage {
	label: string;
	image: string;
	captureWidth: number;
	centerX: number;
	centerY: number;
}

const router = useRouter();
const positions = ref<LivePosition[]>([]);
const images = ref<Record<string, MapImage>>({});
const selected = ref('');
const error = ref('');
let timer = 0;

const maps = computed(() => {
	const counts = new Map<string, number>();
	for (const p of positions.value) if (p.mapName) counts.set(p.mapName, (counts.get(p.mapName) ?? 0) + 1);
	for (const name of Object.keys(images.value)) if (!counts.has(name)) counts.set(name, 0);
	return [...counts.entries()].sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0]));
});
const onMap = computed(() => positions.value.filter((p) => p.mapName === selected.value));
const image = computed(() => images.value[selected.value] ?? null);

/** Without a bake, the view fits the players present, with a margin. */
const frame = computed(() => {
	if (image.value) return { cx: image.value.centerX, cy: image.value.centerY, width: image.value.captureWidth };
	const xs = onMap.value.map((p) => p.x);
	const ys = onMap.value.map((p) => p.y);
	if (!xs.length) return { cx: 0, cy: 0, width: 20000 };
	const span = Math.max(Math.max(...xs) - Math.min(...xs), Math.max(...ys) - Math.min(...ys), 5000) * 1.3;
	return { cx: (Math.max(...xs) + Math.min(...xs)) / 2, cy: (Math.max(...ys) + Math.min(...ys)) / 2, width: span };
});

function place(p: LivePosition): { left: string; top: string } {
	const f = frame.value;
	const u = 0.5 + (p.y - f.cy) / f.width;
	const v = 0.5 - (p.x - f.cx) / f.width;
	return { left: `${Math.min(Math.max(u, 0), 1) * 100}%`, top: `${Math.min(Math.max(v, 0), 1) * 100}%` };
}

async function load() {
	try {
		positions.value = (await http.get<LivePosition[]>('/World/Positions')).data;
		if (!selected.value && maps.value.length) selected.value = maps.value[0][0];
		error.value = '';
	} catch (e) {
		error.value = errorText(e);
	}
}

onMounted(async () => {
	try {
		images.value = await (await fetch('/maps/maps.json')).json();
	} catch {
		images.value = {};
	}
	await load();
	timer = window.setInterval(load, 5000);
});
onUnmounted(() => window.clearInterval(timer));
</script>

<template>
	<div class="page">
		<div class="page-head">
			<h1>Live map</h1>
			<span class="sub">Positions every 5 s from the zone servers</span>
		</div>
		<p v-if="error" class="notice bad">{{ error }}</p>

		<div class="tabs" role="tablist">
			<button v-for="[map, count] in maps" :key="map" role="tab" :aria-selected="selected === map" :class="{ on: selected === map }" @click="selected = map">
				{{ images[map]?.label ?? map }} <span class="muted">{{ count }}</span>
			</button>
		</div>

		<div v-if="selected" class="layout">
			<div class="map" :class="{ grid: !image }" :style="image ? { backgroundImage: `url(${image.image})` } : {}">
				<button
					v-for="p in onMap"
					:key="p.characterName"
					class="pin"
					:style="place(p)"
					:title="`${p.characterName} · level ${p.level}`"
					:aria-label="`${p.characterName}, level ${p.level}`"
					@click="router.push(`/players/character/${encodeURIComponent(p.characterName)}`)"
				>
					<span class="name">{{ p.characterName }}</span>
				</button>
				<p v-if="!image" class="hint muted">No minimap image for this map. Players are placed relative to each other; up is world +X.</p>
			</div>
			<div class="table-wrap side">
				<table>
					<thead><tr><th>Character</th><th class="num">Level</th><th class="num">X, Y</th><th>Seen</th></tr></thead>
					<tbody>
						<tr v-for="p in onMap" :key="p.characterName">
							<td><RouterLink :to="`/players/character/${encodeURIComponent(p.characterName)}`">{{ p.characterName }}</RouterLink></td>
							<td class="num">{{ p.level }}</td>
							<td class="num mono">{{ num(p.x) }}, {{ num(p.y) }}</td>
							<td><Ago :at="p.updatedAt" /></td>
						</tr>
						<tr v-if="!onMap.length"><td colspan="4" class="empty">Nobody is on this map.</td></tr>
					</tbody>
				</table>
			</div>
		</div>
		<p v-else-if="!error" class="muted">Nobody is in the world.</p>
	</div>
</template>

<style scoped>
.layout {
	display: grid;
	grid-template-columns: minmax(0, 720px) minmax(260px, 1fr);
	gap: 20px;
	align-items: start;
}
.map {
	position: relative;
	aspect-ratio: 1;
	border: 1px solid var(--rule-strong);
	border-radius: var(--radius);
	background: var(--paper-sunken) center / cover no-repeat;
	overflow: hidden;
}
.map.grid {
	background-image: linear-gradient(var(--rule) 1px, transparent 1px), linear-gradient(90deg, var(--rule) 1px, transparent 1px);
	background-size: 10% 10%;
	background-position: 0 0;
	background-repeat: repeat;
}
.pin {
	position: absolute;
	width: 12px;
	height: 12px;
	padding: 0;
	margin: -6px 0 0 -6px;
	border-radius: 50%;
	border: 2px solid #fff;
	background: var(--accent);
	box-shadow: 0 0 0 1px rgba(0, 0, 0, 0.35);
}
.pin:hover:not(:disabled) {
	background: var(--accent-ink);
}
.pin .name {
	position: absolute;
	left: 12px;
	top: -5px;
	font: 600 11px/1 var(--font);
	color: var(--ink);
	background: var(--paper-raised);
	padding: 2px 4px;
	border-radius: 2px;
	white-space: nowrap;
	pointer-events: none;
	opacity: 0;
}
.pin:hover .name,
.pin:focus-visible .name {
	opacity: 1;
}
.hint {
	position: absolute;
	left: 10px;
	bottom: 8px;
	margin: 0;
	font-size: 12px;
}
.side {
	max-height: 720px;
	overflow-y: auto;
}
@media (max-width: 900px) {
	.layout {
		grid-template-columns: 1fr;
	}
}
</style>
