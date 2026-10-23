<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { http, can, errorText, type CharacterRow, type Shop } from '../api';
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

const net7d = computed(() => (data.value?.flows7d ?? []).reduce((sum, f) => sum + f.juriIn - f.juriOut, 0));
const items = computed(() => {
	const q = itemFilter.value.trim().toLowerCase();
	return (data.value?.items ?? []).filter((i) => !q || i.itemIDTag.toLowerCase().includes(q));
});

async function load() {
	try {
		data.value = (await http.get<Economy>('/Economy')).data;
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
