<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import {
	http, can, errorText, parseJson,
	type AuditEntry, type CharacterDetail, type ChatExcerptLine, type ChatLine, type GameEvent, type PlayerReport, type Sanction, type TradePartner, type ZoneCommand,
} from '../api';
import { num, leaf, itemName, utc, duration, source, toDate } from '../format';
import Ago from '../components/Ago.vue';
import Seal from '../components/Seal.vue';
import ActionDialog from '../components/ActionDialog.vue';

const route = useRoute();
const name = String(route.params.name);
const character = ref<CharacterDetail | null>(null);
const commands = ref<ZoneCommand[]>([]);
const now = ref(Date.now());
const audit = ref<AuditEntry[]>([]);
const sanctions = ref<Sanction[]>([]);
const events = ref<GameEvent[]>([]);
const eventFilter = ref('');
const chat = ref<ChatLine[]>([]);
const reports = ref<PlayerReport[]>([]);
const trades = ref<TradePartner[]>([]);
const sentrySearch = ref<string | null>(null);
const error = ref('');
const message = ref('');
type Tab = 'items' | 'ledger' | 'chat' | 'quests' | 'reports' | 'audit' | 'commands' | 'stats';
const tab = ref<Tab>('items');
const tabs: { id: Tab; label: string }[] = [
	{ id: 'items', label: 'Items' },
	{ id: 'ledger', label: 'Ledger' },
	{ id: 'chat', label: 'Chat' },
	{ id: 'quests', label: 'Quests' },
	{ id: 'reports', label: 'Reports' },
	{ id: 'audit', label: 'Trades and audit' },
	{ id: 'commands', label: 'Commands' },
	{ id: 'stats', label: 'Stats' },
];

type Action = 'kick' | 'unstuck' | 'give_item' | 'give_juri' | 'flags';
const action = ref<Action | null>(null);
const itemTag = ref('Item.');
const count = ref(1);
const amount = ref(1000);
const flags = ref({ isAdmin: false, isModerator: false, isInternalNetworkTestUser: false });

const eventKinds = ['currency_changed', 'item_gained', 'item_lost', 'trade', 'refine', 'level_up', 'player_died', 'session_started', 'session_ended', 'quest_accepted', 'quest_completed', 'rpc_rejected', 'persistence_failed', 'restored'];
const reportsAgainst = computed(() => reports.value.filter((r) => r.targetName.toLowerCase() === name.toLowerCase()));
const reportsBy = computed(() => reports.value.filter((r) => r.reporterName.toLowerCase() === name.toLowerCase()));

// A refresh that started before a load must not overwrite what the load brought.
let loadSeq = 0;
let refreshing = false;

const equipped = computed(() => character.value?.items.filter((i) => i.equipped) ?? []);
const bag = computed(() => character.value?.items.filter((i) => !i.equipped) ?? []);
const activeBan = computed(() => sanctions.value.find((s) => s.kind === 'ban' && s.active) ?? null);
const activeMute = computed(() => sanctions.value.find((s) => s.kind === 'mute' && s.active) ?? null);

const actionTitle = computed(() => ({
	kick: `Kick ${name}`,
	unstuck: `Move ${name} to a safe spot`,
	give_item: `Give ${name} an item`,
	give_juri: `Give ${name} juri`,
	flags: `Flags for ${name}`,
})[action.value ?? 'kick']);

async function load() {
	const seq = ++loadSeq;
	try {
		const { data } = await http.get(`/Players/Character/${encodeURIComponent(name)}`);
		if (seq !== loadSeq) return;
		character.value = data.character;
		commands.value = data.commands;
		audit.value = data.audit;
		reports.value = data.reports;
		trades.value = data.trades;
		sentrySearch.value = data.sentrySearch;
		flags.value = {
			isAdmin: data.character.isAdmin,
			isModerator: data.character.isModerator,
			isInternalNetworkTestUser: data.character.isInternalNetworkTestUser,
		};
		if (data.character.userGUID) {
			sanctions.value = (await http.get(`/Players/Account/${data.character.userGUID}`)).data.sanctions;
		}
		error.value = '';
	} catch (e) {
		error.value = errorText(e);
	}
}

async function loadEvents(older = false) {
	const before = older && events.value.length ? events.value[events.value.length - 1].gameEventID : 0;
	const { data } = await http.get<GameEvent[]>(`/Players/Character/${encodeURIComponent(name)}/Events`, {
		params: { event: eventFilter.value, before, limit: 100 },
	});
	events.value = older ? [...events.value, ...data] : data;
}

async function loadChat() {
	chat.value = (await http.get<ChatLine[]>(`/Players/Character/${encodeURIComponent(name)}/Chat`)).data;
}

function show(next: Tab) {
	tab.value = next;
	if (next === 'ledger' && !events.value.length) loadEvents();
	if (next === 'chat' && !chat.value.length) loadChat();
}

async function run(reason: string): Promise<string> {
	if (action.value === 'flags') {
		await http.put(`/Players/Character/${encodeURIComponent(name)}/Flags`, flags.value);
		await load();
		return 'Flags saved. They apply at the next login.';
	}
	const body: Record<string, unknown> = { command: action.value, reason };
	if (action.value === 'give_item') Object.assign(body, { itemTag: itemTag.value.trim(), count: count.value });
	if (action.value === 'give_juri') Object.assign(body, { amount: amount.value });
	const { data } = await http.post(`/Players/Character/${encodeURIComponent(name)}/Commands`, body);
	if (!data.success) throw { response: { status: 200, data } };
	await load();
	return character.value?.onlineZone
		? 'Sent. The zone server runs it within 10 s; see Commands for the result.'
		: 'Queued. It runs when this character next logs in.';
}

function fields(e: GameEvent): Record<string, any> {
	try {
		return JSON.parse(e.fields);
	} catch {
		return {};
	}
}

function signed(value: number): string {
	return `${value > 0 ? '+' : ''}${num(value)}`;
}

function describe(e: GameEvent): string {
	const f = fields(e);
	switch (e.event) {
		case 'currency_changed':
			return `${source(f.source)} · balance ${num(f.balance)}`;
		case 'item_gained':
		case 'item_lost':
			return `${itemName(f.item ?? '')}${f.refine ? ` +${f.refine}` : ''} · ${source(f.source)}`;
		case 'refine': {
			const mark = f.kind === 'starup' ? '★' : '+';
			return `${itemName(f.item ?? '')} ${mark}${f.from} → ${mark}${f.to} · ${source(f.outcome)}${f.chance !== undefined ? ` · ${Math.round(f.chance * 100)}%` : ''}`;
		}
		case 'level_up':
			return `${f.kind === 'job' ? 'Job' : 'Base'} level ${f.level}`;
		case 'player_died':
			return `Killed by ${String(f.killer ?? '').replace(/^Mob\./, '')} (${f.killerType})`;
		case 'session_started':
			return 'Entered the zone';
		case 'session_ended':
			return `${f.reason} after ${duration(f.seconds)}`;
		case 'quest_accepted':
		case 'quest_completed':
			return leaf(f.quest);
		case 'rpc_rejected':
			return `${f.rpc}: ${f.reason}`;
		case 'persistence_failed':
			return `${f.endpoint} save failed (${f.status}) ${f.reason ?? ''}`;
		case 'trade':
			return `with ${f.partner}: gave ${num(f.juriGiven ?? 0)} juri and ${(f.itemsGiven ?? []).length} item(s), got ${num(f.juriReceived ?? 0)} juri and ${(f.itemsReceived ?? []).length} item(s)`;
		case 'restored':
			return `Restored from ${f.backup} by ${f.by}`;
		default:
			return e.fields;
	}
}

/** The signed amount a ledger row leads with: juri for currency, quantity for items. */
function ledgerAmount(e: GameEvent): { text: string; tone: string } | null {
	const f = fields(e);
	if (e.event === 'currency_changed') return { text: `${signed(f.delta)} juri`, tone: f.delta < 0 ? 'neg' : 'pos' };
	if (e.event === 'item_gained') return { text: `+${f.qty ?? 1}`, tone: 'pos' };
	if (e.event === 'item_lost') return { text: `−${f.qty ?? 1}`, tone: 'neg' };
	return null;
}

function tone(e: GameEvent): string {
	return e.event === 'rpc_rejected' || e.event === 'persistence_failed' ? 'neg' : '';
}

function commandState(c: ZoneCommand): { label: string; tone: string } {
	if (c.completedAt) return { label: c.result ?? 'done', tone: c.result === 'ok' ? 'ok' : 'bad' };
	// A gift is sent once; no answer after two minutes means the zone stopped. Check the ledger before giving again.
	const sentMs = toDate(c.deliveredAt)?.getTime();
	if (sentMs && c.command.startsWith('give_') && now.value - sentMs > 120000) return { label: 'unconfirmed', tone: 'bad' };
	if (isExpired(c)) return { label: 'expired', tone: '' };
	return { label: c.deliveredAt ? 'sent' : 'waiting', tone: 'live' };
}

function isExpired(c: ZoneCommand): boolean {
	return new Date(c.expiresAt.endsWith('Z') ? c.expiresAt : `${c.expiresAt}Z`).getTime() < now.value;
}

// Open commands change on the zone's 5 s poll, and a gift turns unconfirmed with time alone.
async function refreshCommands() {
	now.value = Date.now();
	if (refreshing || !commands.value.some((c) => !c.completedAt && !isExpired(c))) return;
	refreshing = true;
	const seq = loadSeq;
	try {
		const fresh = (await http.get(`/Players/Character/${encodeURIComponent(name)}`)).data.commands;
		if (seq === loadSeq) commands.value = fresh;
	} catch {
		// The next tick tries again.
	} finally {
		refreshing = false;
	}
}

let timer = 0;
onMounted(() => {
	load();
	timer = window.setInterval(refreshCommands, 10000);
});
onUnmounted(() => window.clearInterval(timer));
</script>

<template>
	<div class="page">
		<p v-if="error" class="notice bad">{{ error }}</p>
		<template v-if="character">
			<div class="page-head">
				<h1>{{ character.charName }}</h1>
				<span class="sub">
					{{ leaf(character.classTag) }} · Level {{ num(character.baseLevel) }} · Job {{ num(character.jobLevel) }} ·
					<RouterLink v-if="character.userGUID" :to="`/players/account/${character.userGUID}`">{{ character.accountName }}</RouterLink>
				</span>
				<span v-if="character.onlineZone" class="tag live">Online · {{ character.onlineZone }}</span>
				<Seal v-if="activeBan" kind="banned" :until="activeBan.expiresAt" />
				<Seal v-if="activeMute" kind="muted" :until="activeMute.expiresAt" />
				<div class="actions">
					<button v-if="character.onlineZone" class="danger" @click="action = 'kick'">Kick</button>
					<template v-if="can('GameMaster')">
						<button @click="action = 'unstuck'">Unstuck</button>
						<button @click="action = 'give_item'">Give item</button>
						<button @click="action = 'give_juri'">Give juri</button>
					</template>
					<button v-if="can('Admin')" @click="action = 'flags'">Flags</button>
					<RouterLink v-if="can('Admin')" class="button" :to="`/players/character/${encodeURIComponent(character.charName)}/restore`">Restore</RouterLink>
				</div>
			</div>
			<p v-if="message" class="notice">{{ message }}</p>

			<dl class="kv">
				<div><dt>Juri</dt><dd class="juri">{{ num(character.gold) }}</dd></div>
				<div><dt>Map</dt><dd>{{ character.mapName }}</dd></div>
				<div><dt>Position</dt><dd class="mono">{{ num(character.x) }}, {{ num(character.y) }}, {{ num(character.z) }}</dd></div>
				<div><dt>Last active</dt><dd><Ago :at="character.lastActivity" /></dd></div>
				<div><dt>Created</dt><dd>{{ utc(character.createDate).slice(0, 10) }}</dd></div>
				<div><dt>Economy revision</dt><dd>{{ character.economyRevision }}</dd></div>
				<div>
					<dt>Flags</dt>
					<dd>
						<span v-if="character.isAdmin" class="tag live">Admin</span>
						<span v-if="character.isModerator" class="tag live">Moderator</span>
						<span v-if="character.isInternalNetworkTestUser" class="tag">LAN test</span>
						<span v-if="!character.isAdmin && !character.isModerator && !character.isInternalNetworkTestUser" class="muted">None</span>
					</dd>
				</div>
			</dl>

			<div class="tabs" role="tablist">
				<button v-for="t in tabs" :key="t.id" role="tab" :aria-selected="tab === t.id" :class="{ on: tab === t.id }" @click="show(t.id)">
					{{ t.label }}
				</button>
			</div>

			<template v-if="tab === 'items'">
				<h2>Equipped</h2>
				<div class="table-wrap">
					<table>
						<thead><tr><th class="num">Slot</th><th>Item</th><th class="num">Refine</th><th class="num">Stars</th><th>Cards</th><th>State</th><th>Instance</th></tr></thead>
						<tbody>
							<tr v-for="i in equipped" :key="i.inSlotNumber">
								<td class="num">{{ i.slot }}</td>
								<td>{{ itemName(i.itemIDTag) }}</td>
								<td class="num">{{ i.refine ? `+${i.refine}` : '' }}</td>
								<td class="num">{{ i.stars || '' }}</td>
								<td>{{ i.cards.map(itemName).join(', ') }}</td>
								<td><span v-if="i.locked" class="tag">Locked</span> <span v-if="i.damaged" class="tag bad">Damaged</span></td>
								<td class="mono muted" :title="i.instanceId ?? 'Not saved yet'">{{ i.instanceId?.slice(0, 8) ?? '—' }}</td>
							</tr>
							<tr v-if="!equipped.length"><td colspan="7" class="empty">Nothing equipped.</td></tr>
						</tbody>
					</table>
				</div>
				<h2>Bag</h2>
				<div class="table-wrap">
					<table>
						<thead><tr><th class="num">Slot</th><th>Item</th><th class="num">Quantity</th><th class="num">Refine</th><th>Cards</th><th>State</th><th>Instance</th></tr></thead>
						<tbody>
							<tr v-for="i in bag" :key="i.inSlotNumber">
								<td class="num">{{ i.slot }}</td>
								<td>{{ itemName(i.itemIDTag) }}</td>
								<td class="num">{{ num(i.quantity) }}</td>
								<td class="num">{{ i.refine ? `+${i.refine}` : '' }}</td>
								<td>{{ i.cards.map(itemName).join(', ') }}</td>
								<td><span v-if="i.locked" class="tag">Locked</span> <span v-if="i.damaged" class="tag bad">Damaged</span></td>
								<td class="mono muted" :title="i.instanceId ?? 'Not saved yet'">{{ i.instanceId?.slice(0, 8) ?? '—' }}</td>
							</tr>
							<tr v-if="!bag.length"><td colspan="7" class="empty">The bag is empty.</td></tr>
						</tbody>
					</table>
				</div>
				<template v-if="character.shops.length">
					<h2>Player shops</h2>
					<div class="table-wrap">
						<table>
							<thead><tr><th>Shop</th><th>Zone</th><th>Sale</th><th>Settlement</th><th class="num">Listings</th><th class="num">Proceeds waiting</th><th>Opened</th></tr></thead>
							<tbody>
								<tr v-for="s in character.shops" :key="s.playerShopID">
									<td>{{ s.shopName }}</td><td>{{ s.zoneName }}</td><td>{{ s.saleStatus }}</td><td>{{ s.settlementStatus }}</td>
									<td class="num">{{ s.listings }}</td><td class="num juri">{{ num(s.proceedsGoldPending) }}</td><td><Ago :at="s.createdAt" /></td>
								</tr>
							</tbody>
						</table>
					</div>
				</template>
			</template>

			<template v-if="tab === 'ledger'">
				<div class="toolbar">
					<select v-model="eventFilter" aria-label="Event type" @change="loadEvents()">
						<option value="">Every event</option>
						<option v-for="k in eventKinds" :key="k" :value="k">{{ k.replace(/_/g, ' ') }}</option>
					</select>
				</div>
				<div class="table-wrap">
					<table>
						<thead><tr><th>When</th><th>Event</th><th>Detail</th><th>Map</th></tr></thead>
						<tbody>
							<tr v-for="e in events" :key="e.gameEventID">
								<td><Ago :at="e.eventTime" /></td>
								<td class="nowrap">{{ e.event.replace(/_/g, ' ') }}</td>
								<td :class="tone(e)">
									<span v-if="ledgerAmount(e)" :class="ledgerAmount(e)?.tone" class="amount">{{ ledgerAmount(e)?.text }}</span>
									{{ describe(e) }}
								</td>
								<td class="muted">{{ e.mapName }}</td>
							</tr>
							<tr v-if="!events.length"><td colspan="4" class="empty">No events recorded for this character yet.</td></tr>
						</tbody>
					</table>
				</div>
				<div class="toolbar"><button v-if="events.length >= 100" @click="loadEvents(true)">Load older</button></div>
			</template>

			<template v-if="tab === 'chat'">
				<div class="table-wrap">
					<table>
						<thead><tr><th>When</th><th>Channel</th><th>From</th><th>To</th><th>Message</th></tr></thead>
						<tbody>
							<tr v-for="c in chat" :key="c.chatLogID">
								<td><Ago :at="c.createDate" /></td>
								<td>{{ c.channel }}</td>
								<td>{{ c.senderName }}</td>
								<td>{{ c.targetName }}</td>
								<td :class="{ muted: c.dropped }">{{ c.message }} <span v-if="c.dropped" class="tag warn">Muted</span></td>
							</tr>
							<tr v-if="!chat.length"><td colspan="5" class="empty">No chat in the last 30 days.</td></tr>
						</tbody>
					</table>
				</div>
			</template>

			<template v-if="tab === 'quests'">
				<div class="table-wrap">
					<table>
						<thead><tr><th>Quest</th><th>Journal</th></tr></thead>
						<tbody>
							<tr v-for="q in character.quests" :key="q.questIDTag">
								<td>{{ leaf(q.questIDTag) }}</td>
								<td class="muted">{{ q.questJournalTagContainer.split(',').map((t) => leaf(t.replace(/[()"\s]|TagName=/g, ''))).filter(Boolean).join(', ') }}</td>
							</tr>
							<tr v-if="!character.quests.length"><td colspan="2" class="empty">No quests.</td></tr>
						</tbody>
					</table>
				</div>
			</template>

			<template v-if="tab === 'reports'">
				<p class="toolbar">
					<a v-if="sentrySearch" :href="sentrySearch" target="_blank" rel="noopener">Bug reports and crashes in Sentry</a>
					<span v-else class="muted">Set OWS_SENTRY_URL to link this character's bug reports and crashes.</span>
				</p>
				<h2>Reports against {{ character.charName }}</h2>
				<div class="table-wrap">
					<table>
						<thead><tr><th>Filed</th><th>By</th><th>Reason</th><th>Zone</th><th>State</th></tr></thead>
						<tbody>
							<tr v-for="r in reportsAgainst" :key="r.reportID">
								<td><Ago :at="r.createDate" /></td>
								<td><RouterLink :to="`/players/character/${encodeURIComponent(r.reporterName)}`">{{ r.reporterName }}</RouterLink></td>
								<td :title="parseJson<ChatExcerptLine[]>(r.chatExcerpt, []).map((l) => `${l.sender}: ${l.text}`).join('\n')">{{ r.reason }}</td>
								<td class="muted">{{ r.mapName }}</td>
								<td><span v-if="!r.resolvedAt" class="tag bad">Open</span><span v-else class="muted">Resolved by {{ r.resolvedBy }}</span></td>
							</tr>
							<tr v-if="!reportsAgainst.length"><td colspan="5" class="empty">Nobody has reported this character.</td></tr>
						</tbody>
					</table>
				</div>
				<h2>Reports by {{ character.charName }}</h2>
				<div class="table-wrap">
					<table>
						<thead><tr><th>Filed</th><th>Against</th><th>Reason</th><th>State</th></tr></thead>
						<tbody>
							<tr v-for="r in reportsBy" :key="r.reportID">
								<td><Ago :at="r.createDate" /></td>
								<td><RouterLink :to="`/players/character/${encodeURIComponent(r.targetName)}`">{{ r.targetName }}</RouterLink></td>
								<td>{{ r.reason }}</td>
								<td><span v-if="!r.resolvedAt" class="tag bad">Open</span><span v-else class="muted">Resolved</span></td>
							</tr>
							<tr v-if="!reportsBy.length"><td colspan="4" class="empty">This character has not reported anyone.</td></tr>
						</tbody>
					</table>
				</div>
				<p class="muted">Review and resolve reports on the <RouterLink to="/moderation">Moderation</RouterLink> page.</p>
			</template>

			<template v-if="tab === 'audit'">
				<h2>Trade partners</h2>
				<div class="table-wrap">
					<table>
						<thead><tr><th>Partner</th><th class="num">Trades</th><th class="num">Juri given / received</th><th class="num">Items given / received</th><th>Last</th></tr></thead>
						<tbody>
							<tr v-for="p in trades" :key="p.partner">
								<td><RouterLink :to="`/players/character/${encodeURIComponent(p.partner)}`">{{ p.partner }}</RouterLink></td>
								<td class="num">{{ num(p.trades) }}</td>
								<td class="num juri">{{ num(p.juriGiven) }} / {{ num(p.juriReceived) }}</td>
								<td class="num">{{ num(p.itemsGiven) }} / {{ num(p.itemsReceived) }}</td>
								<td><Ago :at="p.lastTrade" /></td>
							</tr>
							<tr v-if="!trades.length"><td colspan="5" class="empty">No trades recorded.</td></tr>
						</tbody>
					</table>
				</div>
				<h2>Audit</h2>
				<div class="table-wrap">
					<table>
						<thead><tr><th>When</th><th>Action</th><th>Actor</th><th>Target</th><th>Detail</th></tr></thead>
						<tbody>
							<tr v-for="a in audit" :key="a.auditLogID">
								<td><Ago :at="a.createDate" /></td>
								<td>{{ a.action.replace(/_/g, ' ') }}</td>
								<td>{{ a.actor }}</td>
								<td>{{ a.target }}</td>
								<td class="mono muted detail">{{ a.detail }}</td>
							</tr>
							<tr v-if="!audit.length"><td colspan="5" class="empty">No audit rows name this character.</td></tr>
						</tbody>
					</table>
				</div>
			</template>

			<template v-if="tab === 'commands'">
				<div class="table-wrap">
					<table>
						<thead><tr><th>Sent</th><th>Command</th><th>By</th><th>State</th><th>Detail</th></tr></thead>
						<tbody>
							<tr v-for="c in commands" :key="c.commandID">
								<td><Ago :at="c.createDate" /></td>
								<td>{{ c.command.replace(/_/g, ' ') }} <span class="mono muted">{{ c.args !== '{}' ? c.args : '' }}</span></td>
								<td class="muted">{{ c.createdBy }}</td>
								<td><span class="tag" :class="commandState(c).tone">{{ commandState(c).label }}</span></td>
								<td class="muted">{{ c.resultDetail }}</td>
							</tr>
							<tr v-if="!commands.length"><td colspan="5" class="empty">No commands sent to this character.</td></tr>
						</tbody>
					</table>
				</div>
			</template>

			<template v-if="tab === 'stats'">
				<dl class="kv">
					<div v-for="(value, key) in character.stats" :key="key"><dt>{{ key }}</dt><dd>{{ num(value) }}</dd></div>
				</dl>
			</template>
		</template>

		<ActionDialog
			:open="!!action"
			:title="actionTitle"
			:confirm-label="{ kick: 'Kick', unstuck: 'Move to safe spot', give_item: 'Give item', give_juri: 'Give juri', flags: 'Save flags' }[action ?? 'kick']"
			:danger="action === 'kick'"
			:needs-reason="action !== 'flags'"
			:run="run"
			@close="action = null"
			@done="(m) => (message = m)"
		>
			<p v-if="action === 'kick'" class="muted">The player is disconnected and sees the reason. They can log in again.</p>
			<p v-if="action === 'unstuck'" class="muted">The character moves to the zone's start point, now or at the next login.</p>
			<template v-if="action === 'give_item'">
				<label class="field">Item tag <input v-model="itemTag" placeholder="Item.Weapon.Sword" /></label>
				<label class="field">Count <input v-model.number="count" type="number" min="1" max="999" /></label>
			</template>
			<label v-if="action === 'give_juri'" class="field">Amount <input v-model.number="amount" type="number" min="1" /></label>
			<template v-if="action === 'flags'">
				<label><input v-model="flags.isAdmin" type="checkbox" /> Admin flag</label>
				<label><input v-model="flags.isModerator" type="checkbox" /> Moderator flag</label>
				<label><input v-model="flags.isInternalNetworkTestUser" type="checkbox" /> LAN test (joins zone servers at 127.0.0.1)</label>
			</template>
		</ActionDialog>
	</div>
</template>

<style scoped>
.amount {
	font-weight: 600;
	margin-right: 6px;
}
.detail {
	max-width: 420px;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}
</style>
