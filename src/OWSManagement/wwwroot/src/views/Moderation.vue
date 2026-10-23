<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { http, errorText, type AuditEntry, type ChatLine, type Sanction } from '../api';
import { utc } from '../format';
import Ago from '../components/Ago.vue';

const activeOnly = ref(true);
const sanctions = ref<Sanction[]>([]);
const staffSearch = ref('');
const staffActions = ref<AuditEntry[]>([]);
const chatSender = ref('');
const chatText = ref('');
const chat = ref<ChatLine[]>([]);
const error = ref('');

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
		await Promise.all([loadSanctions(), loadStaff(), loadChat()]);
	} catch (e) {
		error.value = errorText(e);
	}
});
</script>

<template>
	<div class="page">
		<div class="page-head"><h1>Moderation</h1></div>
		<p v-if="error" class="notice bad">{{ error }}</p>

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
