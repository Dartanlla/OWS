<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { http, errorText, type NewPlayerCohort, type Outlier, type QuestFunnelRow, type StoppedPlayer } from '../api';
import { num, leaf, utc } from '../format';
import Ago from '../components/Ago.vue';

const newPlayers = ref<NewPlayerCohort[]>([]);
const stopped = ref<StoppedPlayer[]>([]);
const quests = ref<QuestFunnelRow[]>([]);
const outliers = ref<Outlier[]>([]);
const loaded = ref(false);
const error = ref('');

const totals = computed(() =>
	newPlayers.value.reduce(
		(sum, d) => ({
			accounts: sum.accounts + d.accounts,
			madeCharacter: sum.madeCharacter + d.madeCharacter,
			level5: sum.level5 + d.level5,
			level10: sum.level10 + d.level10,
			nextDay: sum.nextDay + d.nextDay,
			afterAWeek: sum.afterAWeek + d.afterAWeek,
		}),
		{ accounts: 0, madeCharacter: 0, level5: 0, level10: 0, nextDay: 0, afterAWeek: 0 },
	),
);

/** The places most stopped players were last seen: map and last quest, most common first. */
function top(values: (string | null)[]): { value: string; count: number }[] {
	const counts = new Map<string, number>();
	for (const v of values) if (v) counts.set(v, (counts.get(v) ?? 0) + 1);
	return [...counts.entries()].map(([value, count]) => ({ value, count })).sort((a, b) => b.count - a.count).slice(0, 5);
}
const topMaps = computed(() => top(stopped.value.map((s) => s.mapName)));
const topQuests = computed(() => top(stopped.value.map((s) => (s.lastQuest ? leaf(s.lastQuest) : null))));
const maxAccepted = computed(() => Math.max(1, ...quests.value.map((q) => q.accepted)));

function share(part: number, whole: number): string {
	return whole ? `${Math.round((part / whole) * 100)}%` : '';
}

function times(value: number, median: number): number {
	return median > 0 ? value / median : 0;
}

onMounted(async () => {
	try {
		const { data } = await http.get('/Insight');
		newPlayers.value = data.newPlayers;
		stopped.value = data.stopped;
		quests.value = data.quests;
		outliers.value = data.outliers;
	} catch (e) {
		error.value = errorText(e);
	} finally {
		loaded.value = true;
	}
});
</script>

<template>
	<div class="page">
		<div class="page-head">
			<h1>Insight</h1>
			<span class="sub">From zone server events. PIE test accounts are left out.</span>
		</div>
		<p v-if="error" class="notice bad">{{ error }}</p>
		<p v-if="!loaded" class="muted">Reading events…</p>

		<template v-if="loaded && !error">
			<h2>New accounts · 30 days</h2>
			<div class="table-wrap">
				<table>
					<thead>
						<tr>
							<th>Signed up</th><th class="num">Accounts</th><th class="num">Made a character</th><th class="num">Level 5</th>
							<th class="num">Level 10</th><th class="num">Played next day</th><th class="num">Played after a week</th>
						</tr>
					</thead>
					<tbody>
						<tr v-for="d in newPlayers" :key="d.day">
							<td class="nowrap">{{ utc(d.day).slice(0, 10) }}</td>
							<td class="num">{{ num(d.accounts) }}</td>
							<td class="num">{{ num(d.madeCharacter) }} <span class="muted">{{ share(d.madeCharacter, d.accounts) }}</span></td>
							<td class="num">{{ num(d.level5) }} <span class="muted">{{ share(d.level5, d.accounts) }}</span></td>
							<td class="num">{{ num(d.level10) }} <span class="muted">{{ share(d.level10, d.accounts) }}</span></td>
							<td class="num">{{ num(d.nextDay) }} <span class="muted">{{ share(d.nextDay, d.accounts) }}</span></td>
							<td class="num">{{ num(d.afterAWeek) }} <span class="muted">{{ share(d.afterAWeek, d.accounts) }}</span></td>
						</tr>
						<tr v-if="newPlayers.length > 1" class="total">
							<td>All</td>
							<td class="num">{{ num(totals.accounts) }}</td>
							<td class="num">{{ num(totals.madeCharacter) }} <span class="muted">{{ share(totals.madeCharacter, totals.accounts) }}</span></td>
							<td class="num">{{ num(totals.level5) }} <span class="muted">{{ share(totals.level5, totals.accounts) }}</span></td>
							<td class="num">{{ num(totals.level10) }} <span class="muted">{{ share(totals.level10, totals.accounts) }}</span></td>
							<td class="num">{{ num(totals.nextDay) }} <span class="muted">{{ share(totals.nextDay, totals.accounts) }}</span></td>
							<td class="num">{{ num(totals.afterAWeek) }} <span class="muted">{{ share(totals.afterAWeek, totals.accounts) }}</span></td>
						</tr>
						<tr v-if="!newPlayers.length"><td colspan="7" class="empty">No accounts were made in the last 30 days.</td></tr>
					</tbody>
				</table>
			</div>

			<h2>Where new players stop</h2>
			<p class="muted">Characters made in the last 30 days that have not played for 3 days, as they were at their last logout.</p>
			<div v-if="stopped.length" class="grid-2">
				<dl class="kv">
					<div v-for="m in topMaps" :key="m.value"><dt>{{ m.value }}</dt><dd>{{ num(m.count) }}</dd></div>
				</dl>
				<dl class="kv">
					<div v-for="q in topQuests" :key="q.value"><dt>{{ q.value }}</dt><dd>{{ num(q.count) }}</dd></div>
				</dl>
			</div>
			<div class="table-wrap" :style="stopped.length ? 'margin-top: 12px' : ''">
				<table>
					<thead><tr><th>Character</th><th class="num">Level</th><th>Map</th><th>Last quest</th><th class="num">Played</th><th>Last seen</th></tr></thead>
					<tbody>
						<tr v-for="s in stopped" :key="s.characterName">
							<td><RouterLink :to="`/players/character/${encodeURIComponent(s.characterName)}`">{{ s.characterName }}</RouterLink></td>
							<td class="num">{{ num(s.level) }}</td>
							<td>{{ s.mapName }}</td>
							<td>
								<template v-if="s.lastQuest">{{ leaf(s.lastQuest) }} <span class="muted">{{ s.lastQuestEvent === 'quest_completed' ? 'done' : 'taken' }}</span></template>
								<span v-else class="muted">None</span>
							</td>
							<td class="num">{{ num(s.minutesPlayed) }} min</td>
							<td><Ago :at="s.lastSeen ?? s.createDate" /></td>
						</tr>
						<tr v-if="!stopped.length"><td colspan="6" class="empty">Every new character played in the last 3 days.</td></tr>
					</tbody>
				</table>
			</div>

			<h2>Quests · 30 days</h2>
			<div class="table-wrap">
				<table>
					<thead><tr><th>Quest</th><th class="num">Taken</th><th class="num">Done</th><th>Done share</th><th class="num">Median time</th></tr></thead>
					<tbody>
						<tr v-for="q in quests" :key="q.quest">
							<td :title="q.quest">{{ leaf(q.quest) }}</td>
							<td class="num">{{ num(q.accepted) }}</td>
							<td class="num">{{ num(q.completed) }}</td>
							<td class="bar-cell">
								<div class="bar" :style="{ width: `${(q.accepted / maxAccepted) * 100}%`, opacity: 0.25 }"></div>
								<div class="bar overlay" :style="{ width: `${(q.completed / maxAccepted) * 100}%` }"></div>
								<span class="muted">{{ share(q.completed, q.accepted) }}</span>
							</td>
							<td class="num">{{ q.medianMinutes === null ? '—' : `${num(q.medianMinutes)} min` }}</td>
						</tr>
						<tr v-if="!quests.length"><td colspan="5" class="empty">No quest was taken in the last 30 days.</td></tr>
					</tbody>
				</table>
			</div>

			<h2>Outliers · 7 days</h2>
			<p class="muted">At least 1 hour played and one rate over 3 times the median. A lead for bots and exploits, not proof. Juri leaves out trades and staff gifts.</p>
			<div class="table-wrap">
				<table>
					<thead>
						<tr><th>Character</th><th class="num">Hours</th><th class="num">Juri / h</th><th class="num">Levels / h</th><th class="num">Loot / h</th><th class="num">Longest session</th></tr>
					</thead>
					<tbody>
						<tr v-for="o in outliers" :key="o.characterName">
							<td><RouterLink :to="`/players/character/${encodeURIComponent(o.characterName)}`">{{ o.characterName }}</RouterLink></td>
							<td class="num">{{ num(o.hours, 1) }}</td>
							<td class="num" :class="{ neg: times(o.juriPerHour, o.medianJuriPerHour) > 3 }">
								{{ num(o.juriPerHour) }} <span class="muted">×{{ num(times(o.juriPerHour, o.medianJuriPerHour), 1) }}</span>
							</td>
							<td class="num" :class="{ neg: times(o.levelsPerHour, o.medianLevelsPerHour) > 3 }">
								{{ num(o.levelsPerHour, 2) }} <span class="muted">×{{ num(times(o.levelsPerHour, o.medianLevelsPerHour), 1) }}</span>
							</td>
							<td class="num" :class="{ neg: times(o.lootPerHour, o.medianLootPerHour) > 3 }">
								{{ num(o.lootPerHour, 1) }} <span class="muted">×{{ num(times(o.lootPerHour, o.medianLootPerHour), 1) }}</span>
							</td>
							<td class="num" :class="{ neg: times(o.longestSessionHours, o.medianLongestSessionHours) > 3 }">{{ num(o.longestSessionHours, 1) }} h</td>
						</tr>
						<tr v-if="!outliers.length"><td colspan="6" class="empty">No character is far above the median.</td></tr>
					</tbody>
				</table>
			</div>
		</template>
	</div>
</template>

<style scoped>
tr.total td {
	font-weight: 600;
	background: var(--paper-sunken);
}
.bar-cell {
	position: relative;
	min-width: 180px;
}
.bar-cell .bar {
	position: absolute;
	left: 12px;
	top: 50%;
	transform: translateY(-50%);
	max-width: calc(100% - 64px);
}
.bar-cell .muted {
	position: relative;
	float: right;
}
</style>
