<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { http, errorText, parseJson, type AuditEntry, type ChatExcerptLine, type ChatLine, type PlayerReport, type Sanction } from '../api';
import { utc } from '../format';
import Ago from '../components/Ago.vue';

const openOnly = ref(true);
const reports = ref<PlayerReport[]>([]);
const expanded = ref<number | null>(null);
const note = ref('');
const resolving = ref(false);

const activeOnly = ref(true);
const sanctions = ref<Sanction[]>([]);
const staffSearch = ref('');
const staffActions = ref<AuditEntry[]>([]);
const chatSender = ref('');
const chatText = ref('');
const chat = ref<ChatLine[]>([]);
const error = ref('');

async function loadReports() {
	reports.value = (await http.get<PlayerReport[]>('/Moderation/Reports', { params: { open: openOnly.value } })).data;
}

function toggle(report: PlayerReport) {
	expanded.value = expanded.value === report.reportID ? null : report.reportID;
	note.value = '';
}

async function resolve(report: PlayerReport) {
	resolving.value = true;
	try {
		await http.post(`/Moderation/Reports/${report.reportID}/Resolve`, { note: note.value });
		expanded.value = null;
		await loadReports();
	} catch (e) {
		error.value = errorText(e);
	} finally {
		resolving.value = false;
	}
}

async function loadSanctions() {
	sanctions.value = (await http.get<Sanction[]>('/Moderation/Sanctions', { params: { active: activeOnly.value } })).data;
}

async function loadStaff() {
	staffActions.value = (await http.get<AuditEntry[]>('/Moderation/StaffActions', { params: { search: staffSearch.value } })).data;
}

async function loadChat(older = false) {
	const before = older && chat.value.length ? chat.value[chat.value.length - 1].chatLogID : 0;
	const { data } = await http.get<ChatLine[]>('/Moderation/Chat', { params: { sender: chatSender.value, text: chatText.value, before } });
	chat.value = older ? [...chat.value, ...data] : data;
}

function staffDetail(entry: AuditEntry): { raw: string; result: string; role: string } {
	try {
		const d = JSON.parse(entry.detail ?? '{}');
		return { raw: d.raw ?? '', result: d.result ?? '', role: d.role ?? '' };
	} catch {
		return { raw: '', result: '', role: '' };
	}
}

onMounted(async () => {
	try {
		await Promise.all([loadReports(), loadSanctions(), loadStaff(), loadChat()]);
	} catch (e) {
		error.value = errorText(e);
	}
});
</script>

<template>
	<div class="page">
		<div class="page-head"><h1>Moderation</h1></div>
		<p v-if="error" class="notice bad">{{ error }}</p>

		<h2>Player reports</h2>
		<div class="toolbar">
			<label><input v-model="openOnly" type="checkbox" @change="loadReports" /> Open reports only</label>
		</div>
		<div class="table-wrap">
			<table>
				<thead><tr><th>Filed</th><th>Reported</th><th>By</th><th>Reason</th><th>Zone</th><th>State</th><th></th></tr></thead>
				<tbody>
					<template v-for="r in reports" :key="r.reportID">
						<tr>
							<td><Ago :at="r.createDate" /></td>
							<td>
								<RouterLink :to="`/players/character/${encodeURIComponent(r.targetName)}`">{{ r.targetName }}</RouterLink>
								<span v-if="r.targetReports30d > 1" class="tag warn" :title="'Reports against this character in the last 30 days'">{{ r.targetReports30d }} in 30 d</span>
							</td>
							<td>
								<RouterLink :to="`/players/character/${encodeURIComponent(r.reporterName)}`">{{ r.reporterName }}</RouterLink>
								<span v-if="r.reporterReports30d > 3" class="tag" :title="'Reports this character filed in the last 30 days'">{{ r.reporterReports30d }} filed</span>
							</td>
							<td>{{ r.reason }}</td>
							<td class="muted">{{ r.mapName }}</td>
							<td><span v-if="!r.resolvedAt" class="tag bad">Open</span><span v-else class="muted">Resolved</span></td>
							<td class="num"><button class="link" @click="toggle(r)">{{ expanded === r.reportID ? 'Close' : 'Review' }}</button></td>
						</tr>
						<tr v-if="expanded === r.reportID" class="detail">
							<td colspan="7">
								<p v-if="r.resolvedAt" class="muted">Resolved by {{ r.resolvedBy }} {{ utc(r.resolvedAt) }}. {{ r.resolutionNote }}</p>
								<table class="excerpt">
									<tbody>
										<tr v-for="(line, i) in parseJson<ChatExcerptLine[]>(r.chatExcerpt, [])" :key="i">
											<td class="nowrap muted">{{ utc(line.at).slice(11, 16) }}</td>
											<td class="nowrap muted">{{ line.channel }}</td>
											<td class="nowrap" :class="{ hit: line.sender === r.targetName }">{{ line.sender }}<template v-if="line.to"> → {{ line.to }}</template></td>
											<td :class="{ muted: line.dropped }">{{ line.text }}</td>
										</tr>
										<tr v-if="!parseJson<ChatExcerptLine[]>(r.chatExcerpt, []).length">
											<td class="empty">Neither player chatted in the 10 minutes before the report.</td>
										</tr>
									</tbody>
								</table>
								<form v-if="!r.resolvedAt" class="toolbar" @submit.prevent="resolve(r)">
									<input v-model="note" type="text" maxlength="1000" placeholder="What you did (optional)" aria-label="Resolution note" class="grow" />
									<button type="submit" class="primary" :disabled="resolving">Mark resolved</button>
								</form>
							</td>
						</tr>
					</template>
					<tr v-if="!reports.length"><td colspan="7" class="empty">{{ openOnly ? 'No open reports.' : 'No reports yet.' }}</td></tr>
				</tbody>
			</table>
		</div>

		<h2>Sanctions</h2>
		<div class="toolbar">
			<label><input v-model="activeOnly" type="checkbox" @change="loadSanctions" /> Active bans and mutes only</label>
		</div>
		<div class="table-wrap">
			<table>
				<thead><tr><th>Account</th><th>Kind</th><th>Reason</th><th>By</th><th>Issued</th><th>Ends</th><th>State</th></tr></thead>
				<tbody>
					<tr v-for="s in sanctions" :key="s.sanctionID">
						<td><RouterLink :to="`/players/account/${s.userGUID}`">{{ s.accountName ?? s.userGUID }}</RouterLink></td>
						<td><span class="tag" :class="{ bad: s.kind === 'ban', warn: s.kind === 'mute' || s.kind === 'warn' }">{{ s.kind }}</span></td>
						<td>{{ s.reason }}</td>
						<td class="muted">{{ s.issuedBy }}</td>
						<td><Ago :at="s.createDate" /></td>
						<td>
							<template v-if="s.kind === 'ban' || s.kind === 'mute'">{{ s.expiresAt ? utc(s.expiresAt).slice(0, 16) : 'Never' }}</template>
						</td>
						<td>
							<span v-if="s.active" class="tag bad">Active</span>
							<span v-else-if="s.revokedAt" class="muted">Revoked</span>
						</td>
					</tr>
					<tr v-if="!sanctions.length"><td colspan="7" class="empty">{{ activeOnly ? 'No account is banned or muted.' : 'No sanctions yet.' }}</td></tr>
				</tbody>
			</table>
		</div>

		<h2>Staff commands in game</h2>
		<form class="toolbar" @submit.prevent="loadStaff">
			<input v-model="staffSearch" type="search" placeholder="Staff member, command or player" aria-label="Search staff commands" />
			<button type="submit">Search</button>
		</form>
		<div class="table-wrap">
			<table>
				<thead><tr><th>When</th><th>Staff</th><th>Role</th><th>Command</th><th>Result</th></tr></thead>
				<tbody>
					<tr v-for="a in staffActions" :key="a.auditLogID">
						<td><Ago :at="a.createDate" /></td>
						<td><RouterLink :to="`/players/character/${encodeURIComponent(a.actor)}`">{{ a.actor }}</RouterLink></td>
						<td class="muted">{{ staffDetail(a).role }}</td>
						<td class="mono">{{ staffDetail(a).raw || a.target }}</td>
						<td><span class="tag" :class="staffDetail(a).result === 'Success' ? 'ok' : 'bad'">{{ staffDetail(a).result }}</span></td>
					</tr>
					<tr v-if="!staffActions.length"><td colspan="5" class="empty">No staff commands recorded.</td></tr>
				</tbody>
			</table>
		</div>

		<h2>Chat · last 30 days</h2>
		<form class="toolbar" @submit.prevent="loadChat()">
			<input v-model="chatSender" type="search" placeholder="Character (sent or received)" aria-label="Character" />
			<input v-model="chatText" type="search" placeholder="Words in the message" aria-label="Message text" />
			<button type="submit">Search</button>
		</form>
		<div class="table-wrap">
			<table>
				<thead><tr><th>When</th><th>Channel</th><th>From</th><th>To</th><th>Message</th></tr></thead>
				<tbody>
					<tr v-for="c in chat" :key="c.chatLogID">
						<td><Ago :at="c.createDate" /></td>
						<td>{{ c.channel }}</td>
						<td><RouterLink :to="`/players/character/${encodeURIComponent(c.senderName)}`">{{ c.senderName }}</RouterLink></td>
						<td>{{ c.targetName }}</td>
						<td :class="{ muted: c.dropped }">{{ c.message }} <span v-if="c.dropped" class="tag warn">Muted</span></td>
					</tr>
					<tr v-if="!chat.length"><td colspan="5" class="empty">No chat matches.</td></tr>
				</tbody>
			</table>
		</div>
		<div class="toolbar"><button v-if="chat.length >= 200" @click="loadChat(true)">Load older</button></div>
	</div>
</template>

<style scoped>
tr.detail > td { background: var(--paper); padding: 10px 12px 12px; }
tr.detail:hover { background: none; }
.excerpt td { height: auto; padding: 3px 8px; border-bottom: none; }
.excerpt tr:hover { background: none; }
.excerpt .hit { font-weight: 600; }
.grow { flex: 1; min-width: 220px; }
</style>
