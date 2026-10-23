<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';
import { http, errorText, type Overview, type Signal } from '../api';
import { num, duration, toDate, instanceStatus } from '../format';
import Ago from '../components/Ago.vue';

const data = ref<Overview | null>(null);
const error = ref('');
let timer = 0;

const signalLabel: Record<Signal['kind'], string> = {
	save_failed: 'Failed saves',
	rejected: 'Rejected requests',
	juri: 'Large juri change',
	zone_exit: 'Zone server crashed',
	duplicate: 'Duplicate items',
};

async function load() {
	try {
		data.value = (await http.get<Overview>('/Overview')).data;
		error.value = '';
	} catch (e) {
		error.value = errorText(e);
	}
}

// A heartbeat comes every 10 s; a minute without one means the zone server is stuck or gone.
function heartbeatState(at: string | null): string {
	const date = toDate(at);
	if (!date) return 'bad';
	const age = (Date.now() - date.getTime()) / 1000;
	return age > 60 ? 'bad' : age > 25 ? 'warn' : 'ok';
}

const commonBuild = computed(() => {
	const counts = new Map<number, number>();
	for (const i of data.value?.instances ?? []) if (i.buildCL) counts.set(i.buildCL, (counts.get(i.buildCL) ?? 0) + 1);
	return [...counts.entries()].sort((a, b) => b[1] - a[1])[0]?.[0] ?? null;
});

onMounted(() => {
	load();
	timer = window.setInterval(load, 15000);
});
onUnmounted(() => window.clearInterval(timer));
</script>

<template>
	<div class="page">
		<div class="page-head">
			<h1>Overview</h1>
			<span class="sub">Updates every 15 s</span>
		</div>
		<p v-if="error" class="notice bad">{{ error }}</p>

		<template v-if="data">
			<h2>Today</h2>
			<dl class="kv">
				<div><dt>Online now</dt><dd>{{ num(data.overview.onlineNow) }}</dd></div>
				<div><dt>Zones up</dt><dd>{{ num(data.overview.zonesUp) }}</dd></div>
				<div><dt>Accounts logged in</dt><dd>{{ num(data.overview.accountsToday) }}</dd></div>
				<div><dt>New characters</dt><dd>{{ num(data.overview.newCharactersToday) }}</dd></div>
				<div>
					<dt>Median session, 7 days</dt>
					<dd>{{ data.overview.medianSessionMinutes7d === null ? '—' : duration(data.overview.medianSessionMinutes7d * 60) }}</dd>
				</div>
				<div>
					<dt>Logins</dt>
					<dd><span class="tag" :class="data.loginsOpen ? 'ok' : 'warn'">{{ data.loginsOpen ? 'Open' : 'Closed for maintenance' }}</span></dd>
				</div>
			</dl>

			<h2>Needs attention · last 24 h</h2>
			<div class="table-wrap">
				<table>
					<thead>
						<tr><th>Signal</th><th>Who or where</th><th>Detail</th><th class="num">Count</th><th>Last</th></tr>
					</thead>
					<tbody>
						<tr v-for="(s, i) in data.overview.signals" :key="i">
							<td><span class="tag" :class="s.kind === 'juri' ? 'warn' : 'bad'">{{ signalLabel[s.kind] ?? s.kind }}</span></td>
							<td>
								<RouterLink v-if="s.kind !== 'zone_exit' && s.kind !== 'duplicate' && s.subject" :to="`/players/character/${encodeURIComponent(s.subject)}`">{{ s.subject }}</RouterLink>
								<RouterLink v-else-if="s.kind === 'duplicate'" to="/economy">See Economy</RouterLink>
								<span v-else>{{ s.subject }}</span>
							</td>
							<td :class="{ juri: s.kind === 'juri' }">{{ s.detail }}</td>
							<td class="num">{{ num(s.count) }}</td>
							<td><Ago :at="s.lastSeen" /></td>
						</tr>
						<tr v-if="!data.overview.signals.length"><td colspan="5" class="empty">Nothing needs attention.</td></tr>
					</tbody>
				</table>
			</div>

			<h2>Zone servers</h2>
			<div class="table-wrap">
				<table>
					<thead>
						<tr>
							<th>Zone</th><th>Region</th><th>Instance</th><th>State</th><th class="num">Players</th>
							<th class="num">Tick ms avg / max</th><th class="num">CPU %</th><th class="num">Memory MB</th>
							<th class="num">Up</th><th>Build</th><th>Heartbeat</th>
						</tr>
					</thead>
					<tbody>
						<tr v-for="z in data.instances" :key="z.mapInstanceID">
							<td>{{ z.zoneName }}</td>
							<td>{{ z.serverRegion }}</td>
							<td class="mono">{{ z.mapInstanceID }}:{{ z.port }}</td>
							<td><span class="tag" :class="z.status === 2 ? 'ok' : 'warn'">{{ instanceStatus[z.status] ?? z.status }}</span></td>
							<td class="num" :title="z.onlineCharacters ?? ''">{{ num(z.numberOfReportedPlayers) }}</td>
							<td class="num" :class="{ neg: (z.serverFrameMsAvg ?? 0) > 33 }">{{ num(z.serverFrameMsAvg, 1) }} / {{ num(z.serverFrameMsMax, 0) }}</td>
							<td class="num" :class="{ neg: (z.serverCpuPct ?? 0) > 80 }">{{ num(z.serverCpuPct, 0) }}</td>
							<td class="num">{{ num(z.memoryUsedMB) }}</td>
							<td class="num">{{ duration(z.uptimeSeconds) }}</td>
							<td>
								<span v-if="!z.buildCL" class="muted">dev</span>
								<span v-else :class="{ neg: commonBuild !== null && z.buildCL !== commonBuild }" :title="z.buildCL !== commonBuild ? 'Not the build most zones run' : ''">CL {{ z.buildCL }}</span>
							</td>
							<td><span class="dot" :class="heartbeatState(z.lastUpdateFromServer)"></span><Ago :at="z.lastUpdateFromServer" /></td>
						</tr>
						<tr v-if="!data.instances.length"><td colspan="11" class="empty">No zone server is running. Zones start when a player travels to them.</td></tr>
					</tbody>
				</table>
			</div>
		</template>
	</div>
</template>
