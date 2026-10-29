<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { http, can, errorText, session, type AccountRow, type AuditEntry, type CharacterRow, type Sanction } from '../api';
import { num, leaf, utc } from '../format';
import Ago from '../components/Ago.vue';
import Seal from '../components/Seal.vue';
import ActionDialog from '../components/ActionDialog.vue';

const route = useRoute();
const id = String(route.params.id);
const account = ref<AccountRow | null>(null);
const characters = ref<CharacterRow[]>([]);
const logins = ref<AuditEntry[]>([]);
const sanctions = ref<Sanction[]>([]);
const roles = ref<string[]>([]);
const error = ref('');
const message = ref('');

type Kind = 'ban' | 'mute' | 'warn' | 'note';
const dialog = ref<{ kind: Kind } | null>(null);
const minutes = ref(1440);
const characterName = ref('');
const revoking = ref<Sanction | null>(null);
const newRole = ref('');

const durations = [
	{ value: 60, label: '1 hour' },
	{ value: 1440, label: '1 day' },
	{ value: 10080, label: '7 days' },
	{ value: 43200, label: '30 days' },
	{ value: 0, label: 'Permanent' },
];

const activeBan = computed(() => sanctions.value.find((s) => s.kind === 'ban' && s.active) ?? null);
const activeMute = computed(() => sanctions.value.find((s) => s.kind === 'mute' && s.active) ?? null);
const isSelf = computed(() => session.user?.steamId && session.user.steamId === account.value?.steamId);

const dialogTitle = computed(() => {
	const name = account.value?.firstName ?? 'account';
	return { ban: `Ban ${name}`, mute: `Mute ${name}`, warn: `Warn ${name}`, note: `Add a note to ${name}` }[dialog.value?.kind ?? 'note'];
});
const dialogConfirm = computed(() => {
	const label = durations.find((d) => d.value === minutes.value)?.label.toLowerCase();
	return {
		ban: minutes.value ? `Ban for ${label}` : 'Ban permanently',
		mute: minutes.value ? `Mute for ${label}` : 'Mute permanently',
		warn: 'Send warning',
		note: 'Add note',
	}[dialog.value?.kind ?? 'note'];
});

async function load() {
	try {
		const { data } = await http.get(`/Players/Account/${id}`);
		account.value = data.account;
		characters.value = data.characters;
		logins.value = data.logins;
		sanctions.value = data.sanctions;
		newRole.value = data.account.role;
		characterName.value = data.characters[0]?.charName ?? '';
		error.value = '';
	} catch (e) {
		error.value = errorText(e);
	}
}

function open(kind: Kind) {
	minutes.value = kind === 'mute' ? 60 : 1440;
	dialog.value = { kind };
}

async function issue(reason: string) {
	const kind = dialog.value!.kind;
	const { data } = await http.post(`/Players/Account/${id}/Sanctions`, {
		kind,
		reason,
		minutes: kind === 'ban' || kind === 'mute' ? minutes.value : 0,
		characterName: characterName.value || null,
	});
	await load();
	const reach = data.commandsQueued ? ` ${data.commandsQueued} online character(s) will see it within 10 s.` : '';
	return `${kind === 'note' ? 'Note added.' : 'Done.'}${reach}`;
}

async function revoke() {
	await http.post(`/Players/Account/${id}/Sanctions/${revoking.value!.sanctionID}/Revoke`);
	await load();
	return `${revoking.value!.kind === 'ban' ? 'Ban' : 'Mute'} revoked.`;
}

async function saveRole() {
	try {
		const { data } = await http.put(`/Players/Account/${id}/Role`, { role: newRole.value });
		if (!data.success) throw { response: { status: 200, data } };
		await load();
		message.value = `Role set to ${newRole.value}.`;
	} catch (e) {
		message.value = errorText(e);
	}
}

async function toggleLanTest() {
	const on = !account.value?.isInternalNetworkTestUser;
	try {
		const { data } = await http.put(`/Players/Account/${id}/Flags`, { isInternalNetworkTestUser: on });
		if (!data.success) throw { response: { status: 200, data } };
		await load();
		message.value = on ? 'LAN test on. It applies at the next zone join.' : 'LAN test off. It applies at the next zone join.';
	} catch (e) {
		message.value = errorText(e);
	}
}

function loginResult(entry: AuditEntry): string {
	try {
		return JSON.parse(entry.detail ?? '{}').result ?? '';
	} catch {
		return '';
	}
}

function sanctionState(s: Sanction): { label: string; tone: string } {
	if (s.kind === 'warn' || s.kind === 'note') return { label: '', tone: '' };
	if (s.revokedAt) return { label: 'Revoked', tone: '' };
	if (s.active) return { label: 'Active', tone: 'bad' };
	return { label: 'Expired', tone: '' };
}

function canRevoke(s: Sanction): boolean {
	return s.active && (s.kind === 'ban' ? can('Admin') : can('Moderator'));
}

onMounted(async () => {
	load();
	roles.value = (await http.get<string[]>('/Users/Roles')).data;
});
</script>

<template>
	<div class="page">
		<p v-if="error" class="notice bad">{{ error }}</p>
		<template v-if="account">
			<div class="page-head">
				<h1>{{ account.firstName }}</h1>
				<span class="sub">Account</span>
				<Seal v-if="activeBan" kind="banned" :until="activeBan.expiresAt" />
				<Seal v-if="activeMute" kind="muted" :until="activeMute.expiresAt" />
				<div class="actions">
					<button @click="open('note')">Add note</button>
					<button @click="open('warn')">Warn</button>
					<button class="danger" @click="open('mute')">Mute</button>
					<button v-if="can('Admin')" class="danger" @click="open('ban')">Ban</button>
				</div>
			</div>
			<p v-if="message" class="notice">{{ message }}</p>

			<dl class="kv">
				<div><dt>Role</dt><dd>{{ account.role }}</dd></div>
				<div><dt>Steam ID</dt><dd class="mono">{{ account.steamId || '—' }}</dd></div>
				<div><dt>Email</dt><dd>{{ account.email }}</dd></div>
				<div><dt>Region</dt><dd>{{ account.preferredServerRegion || 'Default' }}</dd></div>
				<div>
					<dt>LAN test</dt>
					<dd>
						<span v-if="account.isInternalNetworkTestUser" class="tag">On · joins zone servers at 127.0.0.1</span>
						<span v-else class="muted">Off</span>
						<button v-if="can('Admin')" class="link" @click="toggleLanTest">
							{{ account.isInternalNetworkTestUser ? 'Turn off' : 'Turn on' }}
						</button>
					</dd>
				</div>
				<div><dt>Created</dt><dd :title="utc(account.createDate)">{{ utc(account.createDate).slice(0, 10) }}</dd></div>
				<div><dt>Last login</dt><dd><Ago :at="account.lastAccess" /></dd></div>
				<div><dt>User GUID</dt><dd class="mono">{{ account.userGUID }}</dd></div>
			</dl>

			<div v-if="can('Admin') && !isSelf" class="toolbar">
				<label class="field">
					Role
					<select v-model="newRole">
						<option v-for="r in roles" :key="r" :value="r">{{ r }}</option>
					</select>
				</label>
				<button :disabled="newRole === account.role" style="align-self: end" @click="saveRole">Set role</button>
			</div>

			<h2>Characters</h2>
			<div class="table-wrap">
				<table>
					<thead>
						<tr><th>Character</th><th>Class</th><th class="num">Level</th><th class="num">Job</th><th>Where</th><th class="num">Juri</th><th>Last active</th></tr>
					</thead>
					<tbody>
						<tr v-for="c in characters" :key="c.characterID">
							<td><RouterLink :to="`/players/character/${encodeURIComponent(c.charName)}`">{{ c.charName }}</RouterLink></td>
							<td>{{ leaf(c.classTag) }}</td>
							<td class="num">{{ num(c.baseLevel) }}</td>
							<td class="num">{{ num(c.jobLevel) }}</td>
							<td>
								<span v-if="c.onlineZone" class="tag live">Online · {{ c.onlineZone }}</span>
								<span v-else class="muted">{{ c.mapName }}</span>
							</td>
							<td class="num juri">{{ num(c.gold) }}</td>
							<td><Ago :at="c.lastActivity" /></td>
						</tr>
						<tr v-if="!characters.length"><td colspan="7" class="empty">This account has no characters.</td></tr>
					</tbody>
				</table>
			</div>

			<h2>Sanctions and notes</h2>
			<div class="table-wrap">
				<table>
					<thead>
						<tr><th>Kind</th><th>Reason</th><th>Character</th><th>By</th><th>Issued</th><th>Ends</th><th>State</th><th></th></tr>
					</thead>
					<tbody>
						<tr v-for="s in sanctions" :key="s.sanctionID">
							<td><span class="tag" :class="{ bad: s.kind === 'ban', warn: s.kind === 'mute' || s.kind === 'warn' }">{{ s.kind }}</span></td>
							<td>{{ s.reason }}</td>
							<td>{{ s.characterName }}</td>
							<td class="muted">{{ s.issuedBy }} <span v-if="s.issuedFrom === 'zone'">(in game)</span></td>
							<td><Ago :at="s.createDate" /></td>
							<td>
								<template v-if="s.kind === 'ban' || s.kind === 'mute'">
									<span v-if="s.expiresAt" :title="utc(s.expiresAt)">{{ utc(s.expiresAt).slice(0, 16) }}</span>
									<span v-else>Never</span>
								</template>
							</td>
							<td>
								<span v-if="sanctionState(s).label" class="tag" :class="sanctionState(s).tone">{{ sanctionState(s).label }}</span>
								<span v-if="s.revokedBy" class="muted"> by {{ s.revokedBy }}</span>
							</td>
							<td class="num"><button v-if="canRevoke(s)" class="link" @click="revoking = s">Revoke</button></td>
						</tr>
						<tr v-if="!sanctions.length"><td colspan="8" class="empty">No sanctions or notes.</td></tr>
					</tbody>
				</table>
			</div>

			<h2>Logins</h2>
			<div class="table-wrap">
				<table>
					<thead><tr><th>When</th><th>Method</th><th>Result</th></tr></thead>
					<tbody>
						<tr v-for="l in logins" :key="l.auditLogID">
							<td><Ago :at="l.createDate" /></td>
							<td>{{ l.target === 'password' ? 'Password' : 'Steam' }}</td>
							<td>
								<span class="tag" :class="loginResult(l) === 'ok' ? 'ok' : 'bad'">{{ loginResult(l) || '—' }}</span>
							</td>
						</tr>
						<tr v-if="!logins.length"><td colspan="3" class="empty">No logins recorded.</td></tr>
					</tbody>
				</table>
			</div>
		</template>

		<ActionDialog
			:open="!!dialog"
			:title="dialogTitle"
			:confirm-label="dialogConfirm"
			:danger="dialog?.kind === 'ban' || dialog?.kind === 'mute'"
			needs-reason
			:run="issue"
			@close="dialog = null"
			@done="(m) => (message = m)"
		>
			<p v-if="dialog?.kind === 'ban'" class="muted">The account cannot log in. Its online characters are kicked. The player sees the reason.</p>
			<p v-if="dialog?.kind === 'mute'" class="muted">Chat from every character on this account is dropped. The player sees the reason.</p>
			<p v-if="dialog?.kind === 'warn'" class="muted">The player sees the warning in game, now or at their next login.</p>
			<p v-if="dialog?.kind === 'note'" class="muted">Only staff see notes.</p>
			<label v-if="dialog?.kind === 'ban' || dialog?.kind === 'mute'" class="field">
				For
				<select v-model.number="minutes">
					<option v-for="d in durations" :key="d.value" :value="d.value">{{ d.label }}</option>
				</select>
			</label>
			<label v-if="characters.length" class="field">
				Character
				<select v-model="characterName">
					<option v-for="c in characters" :key="c.characterID" :value="c.charName">{{ c.charName }}</option>
				</select>
			</label>
		</ActionDialog>

		<ActionDialog
			:open="!!revoking"
			:title="`Revoke ${revoking?.kind}`"
			:confirm-label="`Revoke ${revoking?.kind}`"
			:run="revoke"
			@close="revoking = null"
			@done="(m) => (message = m)"
		>
			<p class="muted">{{ revoking?.reason }}</p>
		</ActionDialog>
	</div>
</template>
