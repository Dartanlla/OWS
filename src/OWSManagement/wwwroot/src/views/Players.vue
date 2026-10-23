<script setup lang="ts">
import { computed, ref, watch, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { http, errorText, type AccountRow, type CharacterRow } from '../api';
import { num, leaf } from '../format';
import Ago from '../components/Ago.vue';

const route = useRoute();
const router = useRouter();
const query = ref(String(route.query.q ?? ''));
const accounts = ref<AccountRow[]>([]);
const characters = ref<CharacterRow[]>([]);
const distribution = ref<{ levels: { key: string; count: number }[]; classes: { key: string; count: number }[] } | null>(null);
const error = ref('');
const loading = ref(false);

async function search() {
	loading.value = true;
	try {
		const { data } = await http.get('/Players/Search', { params: { q: query.value.trim() } });
		accounts.value = data.accounts;
		characters.value = data.characters;
		error.value = '';
	} catch (e) {
		error.value = errorText(e);
	} finally {
		loading.value = false;
	}
}

function submit() {
	router.replace({ query: query.value.trim() ? { q: query.value.trim() } : {} });
}

const maxLevelCount = computed(() => Math.max(1, ...(distribution.value?.levels ?? []).map((l) => l.count)));
const maxClassCount = computed(() => Math.max(1, ...(distribution.value?.classes ?? []).map((c) => c.count)));

watch(
	() => route.query.q,
	(q) => {
		query.value = String(q ?? '');
		search();
	},
);

onMounted(async () => {
	search();
	try {
		distribution.value = (await http.get('/Players/Distribution')).data;
	} catch {
		distribution.value = null;
	}
});
</script>

<template>
	<div class="page">
		<div class="page-head"><h1>Players</h1></div>
		<form class="toolbar" @submit.prevent="submit">
			<input v-model="query" type="search" placeholder="Character, account name, email, Steam ID or user GUID" aria-label="Search players" />
			<button type="submit" class="primary">Search</button>
			<span v-if="!query" class="muted">Showing the most recently active.</span>
		</form>
		<p v-if="error" class="notice bad">{{ error }}</p>

		<h2>Characters</h2>
		<div class="table-wrap">
			<table>
				<thead>
					<tr>
						<th>Character</th><th>Account</th><th>Class</th><th class="num">Level</th><th class="num">Job</th>
						<th>Where</th><th class="num">Juri</th><th>Last active</th>
					</tr>
				</thead>
				<tbody>
					<tr v-for="c in characters" :key="c.characterID">
						<td><RouterLink :to="`/players/character/${encodeURIComponent(c.charName)}`">{{ c.charName }}</RouterLink></td>
						<td>
							<RouterLink v-if="c.userGUID" :to="`/players/account/${c.userGUID}`">{{ c.accountName }}</RouterLink>
						</td>
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
					<tr v-if="!characters.length && !loading"><td colspan="8" class="empty">No character matches. Search by part of the name.</td></tr>
				</tbody>
			</table>
		</div>

		<h2>Accounts</h2>
		<div class="table-wrap">
			<table>
				<thead>
					<tr><th>Account</th><th>Email</th><th>Steam ID</th><th>Role</th><th class="num">Characters</th><th>State</th><th>Last login</th></tr>
				</thead>
				<tbody>
					<tr v-for="a in accounts" :key="a.userGUID">
						<td><RouterLink :to="`/players/account/${a.userGUID}`">{{ a.firstName }}</RouterLink></td>
						<td class="muted">{{ a.email }}</td>
						<td class="mono">{{ a.steamId }}</td>
						<td><span v-if="a.role !== 'Player'" class="tag live">{{ a.role }}</span><span v-else class="muted">Player</span></td>
						<td class="num">{{ a.characterCount }}</td>
						<td>
							<span v-if="a.banned" class="tag bad">Banned</span>
							<span v-if="a.muted" class="tag warn">Muted</span>
						</td>
						<td><Ago :at="a.lastAccess" /></td>
					</tr>
					<tr v-if="!accounts.length && !loading"><td colspan="7" class="empty">No account matches.</td></tr>
				</tbody>
			</table>
		</div>

		<div v-if="distribution && !query" class="grid-2">
			<section>
				<h2>Base level</h2>
				<div class="table-wrap">
					<table>
						<tbody>
							<tr v-for="l in distribution.levels" :key="l.key">
								<td class="num" style="width: 64px">{{ l.key }}</td>
								<td><div class="bar" :style="{ width: `${(l.count / maxLevelCount) * 100}%` }"></div></td>
								<td class="num" style="width: 64px">{{ l.count }}</td>
							</tr>
						</tbody>
					</table>
				</div>
			</section>
			<section>
				<h2>Class</h2>
				<div class="table-wrap">
					<table>
						<tbody>
							<tr v-for="c in distribution.classes" :key="c.key">
								<td style="width: 120px">{{ leaf(c.key) }}</td>
								<td><div class="bar" :style="{ width: `${(c.count / maxClassCount) * 100}%` }"></div></td>
								<td class="num" style="width: 64px">{{ c.count }}</td>
							</tr>
						</tbody>
					</table>
				</div>
			</section>
		</div>
	</div>
</template>
