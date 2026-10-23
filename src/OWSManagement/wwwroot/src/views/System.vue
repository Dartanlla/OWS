<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { http, errorText } from '../api';
import { num, utc } from '../format';
import Ago from '../components/Ago.vue';

interface Info {
	commit: string;
	services: { service: string; healthy: boolean; status: number; ms: number }[];
	backup: { name: string; bytes: number; writtenAt: string } | null;
	migrations: { scriptName: string; applied: string }[];
	links: { grafanaUrl: string | null; sentryUrl: string | null };
}

const info = ref<Info | null>(null);
const error = ref('');

// Backups run hourly; two missed runs is a problem.
function backupAgeHours(at: string): number {
	return (Date.now() - new Date(at.endsWith('Z') ? at : `${at}Z`).getTime()) / 3600000;
}

async function load() {
	try {
		info.value = (await http.get<Info>('/System/Info')).data;
		error.value = '';
	} catch (e) {
		error.value = errorText(e);
	}
}

onMounted(load);
</script>

<template>
	<div class="page">
		<div class="page-head">
			<h1>System</h1>
			<div class="actions"><button @click="load">Check again</button></div>
		</div>
		<p v-if="error" class="notice bad">{{ error }}</p>

		<template v-if="info">
			<h2>Services</h2>
			<div class="table-wrap">
				<table>
					<thead><tr><th>Service</th><th>Health</th><th class="num">Answer, ms</th></tr></thead>
					<tbody>
						<tr v-for="s in info.services" :key="s.service">
							<td class="mono">{{ s.service }}</td>
							<td><span class="dot" :class="s.healthy ? 'ok' : 'bad'"></span>{{ s.healthy ? 'Healthy' : s.status ? `Unhealthy (${s.status})` : 'No answer' }}</td>
							<td class="num">{{ s.ms }}</td>
						</tr>
					</tbody>
				</table>
			</div>

			<h2>Data</h2>
			<dl class="kv">
				<div>
					<dt>Newest backup</dt>
					<dd v-if="info.backup">
						<span class="dot" :class="backupAgeHours(info.backup.writtenAt) > 2 ? 'bad' : 'ok'"></span><Ago :at="info.backup.writtenAt" />
					</dd>
					<dd v-else class="neg">Not found</dd>
				</div>
				<div><dt>Backup size</dt><dd>{{ info.backup ? `${num(info.backup.bytes / 1024, 0)} KB` : '—' }}</dd></div>
				<div><dt>Console version</dt><dd class="mono">{{ info.commit || 'unknown' }}</dd></div>
				<div>
					<dt>Monitoring</dt>
					<dd>
						<a v-if="info.links.grafanaUrl" :href="info.links.grafanaUrl" target="_blank" rel="noopener">Grafana</a>
						<span v-if="info.links.grafanaUrl && info.links.sentryUrl"> · </span>
						<a v-if="info.links.sentryUrl" :href="info.links.sentryUrl" target="_blank" rel="noopener">Sentry</a>
						<span v-if="!info.links.grafanaUrl && !info.links.sentryUrl" class="muted">Not set</span>
					</dd>
				</div>
			</dl>

			<h2>Latest database migrations</h2>
			<div class="table-wrap">
				<table>
					<thead><tr><th>Script</th><th>Applied</th></tr></thead>
					<tbody>
						<tr v-for="m in info.migrations" :key="m.scriptName">
							<td class="mono">{{ m.scriptName }}</td>
							<td :title="utc(m.applied)"><Ago :at="m.applied" /></td>
						</tr>
					</tbody>
				</table>
			</div>
		</template>
	</div>
</template>
