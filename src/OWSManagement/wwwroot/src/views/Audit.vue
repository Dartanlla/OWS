<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { http, errorText, type AuditEntry } from '../api';
import { utc } from '../format';
import Ago from '../components/Ago.vue';

const sources = ['publicapi', 'management', 'zone', 'instancemanagement'];
const actions = ['user_login', 'user_logout', 'zone_join', 'admin_change', 'console_login', 'staff_action', 'trade_completed', 'zone_exit'];

const source = ref('');
const action = ref('');
const search = ref('');
const rows = ref<AuditEntry[]>([]);
const open = ref<number | null>(null);
const error = ref('');

async function load() {
	try {
		rows.value = (await http.get<AuditEntry[]>('/Audit', { params: { source: source.value, action: action.value, search: search.value, limit: 300 } })).data;
		error.value = '';
	} catch (e) {
		error.value = errorText(e);
	}
}

function pretty(detail: string | null): string {
	if (!detail) return '';
	try {
		return JSON.stringify(JSON.parse(detail), null, 2);
	} catch {
		return detail;
	}
}

onMounted(load);
</script>

<template>
	<div class="page">
		<div class="page-head">
			<h1>Audit</h1>
			<span class="sub">Who did what. Rows are never changed or removed.</span>
		</div>
		<form class="toolbar" @submit.prevent="load">
			<select v-model="source" aria-label="Source" @change="load">
				<option value="">Every source</option>
				<option v-for="s in sources" :key="s" :value="s">{{ s }}</option>
			</select>
			<select v-model="action" aria-label="Action" @change="load">
				<option value="">Every action</option>
				<option v-for="a in actions" :key="a" :value="a">{{ a.replace(/_/g, ' ') }}</option>
			</select>
			<input v-model="search" type="search" placeholder="Actor, target or detail" aria-label="Search" />
			<button type="submit">Search</button>
		</form>
		<p v-if="error" class="notice bad">{{ error }}</p>
		<div class="table-wrap">
			<table>
				<thead><tr><th>When</th><th>Source</th><th>Action</th><th>Actor</th><th>Target</th><th>Detail</th></tr></thead>
				<tbody>
					<template v-for="r in rows" :key="r.auditLogID">
						<tr @click="open = open === r.auditLogID ? null : r.auditLogID" style="cursor: pointer">
							<td :title="utc(r.createDate)"><Ago :at="r.createDate" /></td>
							<td class="muted">{{ r.source }}</td>
							<td>{{ r.action.replace(/_/g, ' ') }}</td>
							<td>{{ r.actor }}</td>
							<td>{{ r.target }}</td>
							<td class="mono muted clip">{{ r.detail }}</td>
						</tr>
						<tr v-if="open === r.auditLogID">
							<td colspan="6"><pre class="mono">{{ pretty(r.detail) }}</pre><span v-if="r.clientIP" class="muted">From {{ r.clientIP }}</span></td>
						</tr>
					</template>
					<tr v-if="!rows.length"><td colspan="6" class="empty">No rows match.</td></tr>
				</tbody>
			</table>
		</div>
	</div>
</template>

<style scoped>
.clip {
	max-width: 380px;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}
pre {
	margin: 4px 0;
	white-space: pre-wrap;
}
</style>
