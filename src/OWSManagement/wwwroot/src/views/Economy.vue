<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { http, can, errorText, type CharacterRow, type DropItem, type DropMob, type Shop, type TradePair } from '../api';
import { num, itemName, source } from '../format';
import Ago from '../components/Ago.vue';
import ActionDialog from '../components/ActionDialog.vue';

interface Economy {
	juriTotal: number;
	juriMedian: number;
	characters: number;
	topHolders: CharacterRow[];
	flows7d: { source: string; juriIn: number; juriOut: number; events: number }[];
	items: { itemIDTag: string; quantity: number; holders: number; maxRefine: number }[];
	duplicates: { instanceId: string; itemIDTag: string; copies: number; holders: string }[];
	shops: {
		openShops: number;
		listings: number;
		undeliveredPurchases: number;
		unclaimedProceeds: number;
		abandonedOperations7d: number;
		shops: Shop[];
	};
}

const data = ref<Economy | null>(null);
const error = ref('');
const message = ref('');
const itemFilter = ref('');
const closing = ref<Shop | null>(null);
const pairs = ref<TradePair[]>([]);
const mobs = ref<DropMob[]>([]);
const drops = ref<DropItem[]>([]);
const offOnly = ref(true);

const kills = computed(() => new Map(mobs.value.map((m) => [m.mob, m.kills])));
const shownDrops = computed(() => drops.value.filter((d) => !offOnly.value || (d.deviation !== null && Math.abs(d.deviation) > 3)));

/** One side gave at least 90% of the juri (or, with no juri, of the items) over 3 or more trades: a lead for feeding or selling. */
function oneWay(p: TradePair): string {
	if (p.trades < 3) return '';
	const juri = p.aGaveJuri + p.bGaveJuri;
	const items = p.aGaveItems + p.bGaveItems;
	const [a, total] = juri > 0 ? [p.aGaveJuri, juri] : [p.aGaveItems, items];
	if (total === 0) return '';
	if (a / total >= 0.9) return `${p.a} → ${p.b}`;
	if (a / total <= 0.1) return `${p.b} → ${p.a}`;
	return '';
}

function percent(value: number): string {
	return value >= 0.1 ? `${num(value * 100, 1)}%` : `${num(value * 100, 2)}%`;
}

const net7d = computed(() => (data.value?.flows7d ?? []).reduce((sum, f) => sum + f.juriIn - f.juriOut, 0));
const items = computed(() => {
	const q = itemFilter.value.trim().toLowerCase();
	return (data.value?.items ?? []).filter((i) => !q || i.itemIDTag.toLowerCase().includes(q));
});

async function load() {
	try {
		const [economy, trades, dropData] = await Promise.all([
			http.get<Economy>('/Economy'),
			http.get<{ pairs: TradePair[] }>('/Economy/Trades'),
			http.get<{ mobs: DropMob[]; items: DropItem[] }>('/Economy/Drops'),
		]);
		data.value = economy.data;
		pairs.value = trades.data.pairs;
		mobs.value = dropData.data.mobs;
		drops.value = dropData.data.items;
		error.value = '';
	} catch (e) {
		error.value = errorText(e);
	}
}

async function closeShop() {
	const { data: result } = await http.post(`/Economy/Shops/${closing.value!.playerShopID}/Close`);
	if (!result.success) throw { response: { status: 200, data: { errorMessage: `The shop did not close (${result.reasonCode}).` } } };
	await load();
	return `${closing.value!.shopName} is closed. Its owner claims the items and proceeds at their next login.`;
}

onMounted(load);
</script>

<template>
	<div class="page">
		<div class="page-head"><h1>Economy</h1></div>
		<p v-if="error" class="notice bad">{{ error }}</p>
		<p v-if="message" class="notice">{{ message }}</p>

		<template v-if="data">
			<dl class="kv">
				<div><dt>Juri held</dt><dd class="juri">{{ num(data.juriTotal) }}</dd></div>
				<div><dt>Median per character</dt><dd class="juri">{{ num(data.juriMedian) }}</dd></div>
				<div><dt>Characters</dt><dd>{{ num(data.characters) }}</dd></div>
				<div><dt>Net change, 7 days</dt><dd :class="net7d < 0 ? 'neg' : 'pos'">{{ net7d > 0 ? '+' : '' }}{{ num(net7d) }}</dd></div>
				<div><dt>Possible duplicates</dt><dd :class="{ neg: data.duplicates.length }">{{ data.duplicates.length }}</dd></div>
			</dl>

			<div class="grid-2">
				<section>
					<h2>Juri in and out · 7 days</h2>
					<div class="table-wrap">
						<table>
							<thead><tr><th>Source</th><th class="num">In</th><th class="num">Out</th><th class="num">Events</th></tr></thead>
							<tbody>
								<tr v-for="f in data.flows7d" :key="f.source">
									<td>{{ source(f.source) }}</td>
									<td class="num pos">{{ f.juriIn ? `+${num(f.juriIn)}` : '' }}</td>
									<td class="num neg">{{ f.juriOut ? `−${num(f.juriOut)}` : '' }}</td>
									<td class="num muted">{{ num(f.events) }}</td>
								</tr>
								<tr v-if="!data.flows7d.length"><td colspan="4" class="empty">No juri changes recorded yet. Zone servers send them as players trade, shop and refine.</td></tr>
							</tbody>
						</table>
					</div>
				</section>
				<section>
					<h2>Most juri</h2>
					<div class="table-wrap">
						<table>
							<thead><tr><th>Character</th><th>Account</th><th class="num">Juri</th><th>Last active</th></tr></thead>
							<tbody>
								<tr v-for="c in data.topHolders" :key="c.characterID">
									<td><RouterLink :to="`/players/character/${encodeURIComponent(c.charName)}`">{{ c.charName }}</RouterLink></td>
									<td class="muted">{{ c.accountName }}</td>
									<td class="num juri">{{ num(c.gold) }}</td>
									<td><Ago :at="c.lastActivity" /></td>
								</tr>
							</tbody>
						</table>
					</div>
				</section>
			</div>

			<h2>Possible duplicates</h2>
			<p class="muted">The same item ID in more than one place. Check the holders' ledgers before acting: a lead, not proof.</p>
			<div class="table-wrap">
				<table>
					<thead><tr><th>Item</th><th>Instance ID</th><th class="num">Copies</th><th>Held by</th></tr></thead>
					<tbody>
						<tr v-for="d in data.duplicates" :key="d.instanceId">
							<td>{{ itemName(d.itemIDTag) }}</td>
							<td class="mono">{{ d.instanceId }}</td>
							<td class="num neg">{{ d.copies }}</td>
							<td>
								<template v-for="(holder, i) in d.holders.split(', ')" :key="holder">
									<span v-if="i">, </span><RouterLink :to="`/players/character/${encodeURIComponent(holder)}`">{{ holder }}</RouterLink>
								</template>
							</td>
						</tr>
						<tr v-if="!data.duplicates.length"><td colspan="4" class="empty">No item ID is held twice.</td></tr>
					</tbody>
				</table>
			</div>

			<h2>Trades · 7 days</h2>
			<p class="muted">Pairs by juri traded. One-way: one side gave nearly everything over 3 or more trades, which can mean an alt feeding a main or a sale for real money.</p>
			<div class="table-wrap">
				<table>
					<thead><tr><th>Between</th><th class="num">Trades</th><th class="num">Juri each way</th><th class="num">Items each way</th><th>One-way</th></tr></thead>
					<tbody>
						<tr v-for="p in pairs" :key="`${p.a}|${p.b}`">
							<td>
								<RouterLink :to="`/players/character/${encodeURIComponent(p.a)}`">{{ p.a }}</RouterLink> and
								<RouterLink :to="`/players/character/${encodeURIComponent(p.b)}`">{{ p.b }}</RouterLink>
							</td>
							<td class="num">{{ num(p.trades) }}</td>
							<td class="num juri">{{ num(p.aGaveJuri) }} / {{ num(p.bGaveJuri) }}</td>
							<td class="num">{{ num(p.aGaveItems) }} / {{ num(p.bGaveItems) }}</td>
							<td><span v-if="oneWay(p)" class="tag warn">{{ oneWay(p) }}</span></td>
						</tr>
						<tr v-if="!pairs.length"><td colspan="5" class="empty">No trades in the last 7 days.</td></tr>
					</tbody>
				</table>
			</div>

			<h2>Drop rates · 7 days</h2>
			<p class="muted">What zone servers rolled against each loot table entry, with any rate event counted in. A rate more than 3 standard deviations off its chance after 50 rolls is flagged.</p>
			<div class="toolbar">
				<label><input v-model="offOnly" type="checkbox" /> Flagged rates only</label>
				<span class="muted">{{ num(mobs.length) }} mob types, {{ num(mobs.reduce((s, m) => s + m.kills, 0)) }} kills</span>
			</div>
			<div class="table-wrap">
				<table>
					<thead><tr><th>Mob</th><th class="num">Kills</th><th>Item</th><th class="num">Rolls</th><th class="num">Chance</th><th class="num">Observed</th><th class="num">Off by</th></tr></thead>
					<tbody>
						<tr v-for="d in shownDrops" :key="`${d.mob}|${d.item}`">
							<td>{{ d.mob }}</td>
							<td class="num muted">{{ num(kills.get(d.mob)) }}</td>
							<td>{{ itemName(d.item) }}</td>
							<td class="num">{{ num(d.rolls) }}</td>
							<td class="num">{{ percent(d.chance) }}</td>
							<td class="num">{{ d.rolls ? percent(d.drops / d.rolls) : '—' }}</td>
							<td class="num" :class="{ neg: d.deviation !== null && Math.abs(d.deviation) > 3 }">
								{{ d.deviation === null ? 'few rolls' : `${d.deviation > 0 ? '+' : ''}${num(d.deviation, 1)} σ` }}
							</td>
						</tr>
						<tr v-if="!shownDrops.length">
							<td colspan="7" class="empty">{{ drops.length ? 'Every drop rate is within 3 standard deviations of its chance.' : 'No loot statistics yet. Zone servers send them every 5 minutes.' }}</td>
						</tr>
					</tbody>
				</table>
			</div>

			<h2>Player shops</h2>
			<dl class="kv">
				<div><dt>Open shops</dt><dd>{{ data.shops.openShops }}</dd></div>
				<div><dt>Listings for sale</dt><dd>{{ data.shops.listings }}</dd></div>
				<div><dt>Paid, not delivered</dt><dd :class="{ neg: data.shops.undeliveredPurchases }">{{ data.shops.undeliveredPurchases }}</dd></div>
				<div><dt>Proceeds waiting</dt><dd class="juri">{{ num(data.shops.unclaimedProceeds) }}</dd></div>
				<div><dt>Lost replies, 7 days</dt><dd>{{ data.shops.abandonedOperations7d }}</dd></div>
			</dl>
			<div class="table-wrap" style="margin-top: 12px">
				<table>
					<thead><tr><th>Shop</th><th>Owner</th><th>Zone</th><th>Sale</th><th>Settlement</th><th class="num">Listings</th><th class="num">Proceeds</th><th>Opened</th><th></th></tr></thead>
					<tbody>
						<tr v-for="s in data.shops.shops" :key="s.playerShopID">
							<td>{{ s.shopName }}</td>
							<td><RouterLink :to="`/players/character/${encodeURIComponent(s.ownerCharName)}`">{{ s.ownerCharName }}</RouterLink></td>
							<td>{{ s.zoneName }}</td>
							<td><span class="tag" :class="{ live: s.saleStatus === 'open' }">{{ s.saleStatus }}</span></td>
							<td>{{ s.settlementStatus }}</td>
							<td class="num">{{ s.listings }}</td>
							<td class="num juri">{{ num(s.proceedsGoldPending) }}</td>
							<td><Ago :at="s.createdAt" /></td>
							<td class="num"><button v-if="can('Admin') && s.saleStatus === 'open'" class="link" @click="closing = s">Close</button></td>
						</tr>
						<tr v-if="!data.shops.shops.length"><td colspan="9" class="empty">No open or unsettled shops.</td></tr>
					</tbody>
				</table>
			</div>

			<h2>Items held</h2>
			<div class="toolbar">
				<input v-model="itemFilter" type="search" placeholder="Filter by item tag" aria-label="Filter items" />
			</div>
			<div class="table-wrap">
				<table>
					<thead><tr><th>Item</th><th class="num">Quantity</th><th class="num">Characters</th><th class="num">Highest refine</th></tr></thead>
					<tbody>
						<tr v-for="i in items" :key="i.itemIDTag">
							<td>{{ itemName(i.itemIDTag) }}</td>
							<td class="num">{{ num(i.quantity) }}</td>
							<td class="num">{{ num(i.holders) }}</td>
							<td class="num">{{ i.maxRefine ? `+${i.maxRefine}` : '' }}</td>
						</tr>
					</tbody>
				</table>
			</div>
		</template>

		<ActionDialog
			:open="!!closing"
			:title="`Close ${closing?.shopName}`"
			confirm-label="Close shop"
			danger
			:run="closeShop"
			@close="closing = null"
			@done="(m) => (message = m)"
		>
			<p class="muted">Buyers can no longer buy from it. Unsold items and proceeds go back to {{ closing?.ownerCharName }} at their next login.</p>
		</ActionDialog>
	</div>
</template>
