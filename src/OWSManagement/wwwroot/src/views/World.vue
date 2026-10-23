<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { http, can, errorText, type Result, type ZoneCommand, type ZoneInstance } from '../api';
import { num, duration, instanceStatus } from '../format';
import Ago from '../components/Ago.vue';
import ActionDialog from '../components/ActionDialog.vue';

interface Setting {
	settingKey: string;
	settingValue: string;
	updatedAt: string;
	updatedBy: string;
}

interface Zone {
	mapID: number;
	mapName: string;
	zoneName: string;
	worldCompContainsFilter: string;
	worldCompListFilter: string;
	mapMode: number;
	softPlayerCap: number;
	hardPlayerCap: number;
	minutesToShutdownAfterEmpty: number;
	mapInstanceCount: number;
}

const instances = ref<ZoneInstance[]>([]);
const regions = ref<string[]>([]);
const settings = ref<Setting[]>([]);
const commands = ref<ZoneCommand[]>([]);
const zones = ref<Zone[]>([]);
const error = ref('');
const message = ref('');

const broadcast = ref('');
const broadcasting = ref(false);
const motd = ref('');
const stopping = ref<ZoneInstance | null>(null);
const editing = ref<Zone | null>(null);
const removing = ref<Zone | null>(null);

const setting = (key: string) => settings.value.find((s) => s.settingKey === key);
const loginsOpen = computed(() => setting('logins_open')?.settingValue !== 'false');
const allRegions = computed(() => {
	const fromSettings = settings.value.filter((s) => s.settingKey.startsWith('region_status:')).map((s) => s.settingKey.slice('region_status:'.length));
	return [...new Set([...regions.value, ...fromSettings])].sort();
});

async function load() {
	try {
		const { data } = await http.get('/World');
		instances.value = data.instances;
		regions.value = data.regions;
		settings.value = data.settings;
		commands.value = data.commands;
		motd.value = setting('motd')?.settingValue ?? '';
		zones.value = (await http.get<Zone[]>('/Zones')).data;
		error.value = '';
	} catch (e) {
		error.value = errorText(e);
	}
}

async function save(key: string, value: string, done: string) {
	try {
		await http.put('/World/Settings', { key, value });
		await load();
		message.value = done;
	} catch (e) {
		message.value = errorText(e);
	}
}

async function sendBroadcast() {
	broadcasting.value = true;
	try {
		const { data } = await http.post('/World/Broadcast', { text: broadcast.value });
		message.value = data.zones ? `Sent to ${data.zones} zone server(s). Players see it within 10 s.` : 'No zone server is running, so nobody received it.';
		broadcast.value = '';
		await load();
	} catch (e) {
		message.value = errorText(e);
	} finally {
		broadcasting.value = false;
	}
}

async function stop() {
	const { data } = await http.post<Result>(`/World/Instances/${stopping.value!.mapInstanceID}/Stop`);
	if (!data.success) throw { response: { status: 200, data } };
	await load();
	return `${stopping.value!.zoneName} instance ${stopping.value!.mapInstanceID} is stopping.`;
}

function newZone(): Zone {
	return { mapID: 0, mapName: '', zoneName: '', worldCompContainsFilter: '', worldCompListFilter: '', mapMode: 1, softPlayerCap: 60, hardPlayerCap: 80, minutesToShutdownAfterEmpty: 1, mapInstanceCount: 0 };
}

async function saveZone() {
	const zone = editing.value!;
	const { data } = zone.mapID ? await http.put<Result>('/Zones', zone) : await http.post<Result>('/Zones', zone);
	if (!data.success) throw { response: { status: 200, data } };
	await load();
	return `${zone.zoneName} saved.`;
}

async function removeZone() {
	const { data } = await http.delete<Result>(`/Zones/${removing.value!.mapID}`);
	if (!data.success) throw { response: { status: 200, data } };
	await load();
	return `${removing.value!.zoneName} deleted.`;
}

function setRegion(region: string, event: Event) {
	save(`region_status:${region}`, (event.target as HTMLSelectElement).value, `${region} status saved.`);
}

function changed(key: string): string {
	const s = setting(key);
	return s ? `by ${s.updatedBy}` : '';
}

function commandState(c: ZoneCommand): string {
	if (c.completedAt) return c.result ?? 'done';
	return c.deliveredAt ? 'sent' : 'waiting';
}

onMounted(load);
</script>

<template>
	<div class="page">
		<div class="page-head"><h1>World</h1></div>
		<p v-if="error" class="notice bad">{{ error }}</p>
		<p v-if="message" class="notice">{{ message }}</p>

		<div class="grid-2">
			<section>
				<h2>Message every player</h2>
				<form v-if="can('GameMaster')" class="stack" @submit.prevent="sendBroadcast">
					<textarea v-model="broadcast" maxlength="300" placeholder="Servers restart in 10 minutes. Find a safe place to log out." aria-label="Message"></textarea>
					<div class="row">
						<span class="muted">{{ broadcast.length }} / 300 · shows in every player's chat</span>
						<button type="submit" class="primary" :disabled="!broadcast.trim() || broadcasting">Send to every zone</button>
					</div>
				</form>
				<p v-else class="muted">A GameMaster or Admin can message every player.</p>
			</section>

			<section>
				<h2>Message of the day</h2>
				<form class="stack" @submit.prevent="save('motd', motd, motd.trim() ? 'Message of the day saved.' : 'Message of the day removed.')">
					<textarea v-model="motd" maxlength="500" :disabled="!can('Admin')" placeholder="No message. Players see this in chat each time they enter a zone." aria-label="Message of the day"></textarea>
					<div v-if="can('Admin')" class="row">
						<span class="muted">Shown at each zone entry</span>
						<button type="submit" :disabled="motd === (setting('motd')?.settingValue ?? '')">Save</button>
					</div>
				</form>
			</section>
		</div>

		<h2>Access</h2>
		<div class="table-wrap">
			<table>
				<thead><tr><th>Switch</th><th>State</th><th>Changed</th><th></th></tr></thead>
				<tbody>
					<tr>
						<td>Logins</td>
						<td><span class="tag" :class="loginsOpen ? 'ok' : 'warn'">{{ loginsOpen ? 'Open' : 'Closed: staff only' }}</span></td>
						<td class="muted"><template v-if="setting('logins_open')"><Ago :at="setting('logins_open')?.updatedAt" /> {{ changed('logins_open') }}</template></td>
						<td class="num">
							<button v-if="can('Admin')" :class="loginsOpen ? 'danger' : 'primary'"
								@click="save('logins_open', loginsOpen ? 'false' : '', loginsOpen ? 'Logins closed. Players below Moderator cannot log in.' : 'Logins open.')">
								{{ loginsOpen ? 'Close logins' : 'Open logins' }}
							</button>
						</td>
					</tr>
					<tr v-for="region in allRegions" :key="region">
						<td>Region {{ region }} on the server list</td>
						<td>
							<select v-if="can('Admin')" :value="setting(`region_status:${region}`)?.settingValue ?? ''" :aria-label="`Status of ${region}`"
								@change="setRegion(region, $event)">
								<option value="">As configured</option>
								<option value="online">Online</option>
								<option value="maintenance">Maintenance</option>
								<option value="offline">Offline</option>
							</select>
							<span v-else>{{ setting(`region_status:${region}`)?.settingValue ?? 'As configured' }}</span>
						</td>
						<td class="muted"><template v-if="setting(`region_status:${region}`)"><Ago :at="setting(`region_status:${region}`)?.updatedAt" /> {{ changed(`region_status:${region}`) }}</template></td>
						<td></td>
					</tr>
				</tbody>
			</table>
		</div>

		<h2>Running instances</h2>
		<div class="table-wrap">
			<table>
				<thead><tr><th>Zone</th><th>Region</th><th>Instance</th><th>State</th><th class="num">Players</th><th>Characters</th><th class="num">Up</th><th></th></tr></thead>
				<tbody>
					<tr v-for="z in instances" :key="z.mapInstanceID">
						<td>{{ z.zoneName }}</td>
						<td>{{ z.serverRegion }}</td>
						<td class="mono">{{ z.mapInstanceID }}:{{ z.port }}</td>
						<td><span class="tag" :class="z.status === 2 ? 'ok' : 'warn'">{{ instanceStatus[z.status] ?? z.status }}</span></td>
						<td class="num">{{ num(z.numberOfReportedPlayers) }}</td>
						<td>
							<template v-for="(c, i) in (z.onlineCharacters ?? '').split(', ').filter(Boolean)" :key="c">
								<span v-if="i">, </span><RouterLink :to="`/players/character/${encodeURIComponent(c)}`">{{ c }}</RouterLink>
							</template>
						</td>
						<td class="num">{{ duration(z.uptimeSeconds) }}</td>
						<td class="num"><button v-if="can('Admin') && z.status === 2" class="link" @click="stopping = z">Stop</button></td>
					</tr>
					<tr v-if="!instances.length"><td colspan="8" class="empty">No zone server is running.</td></tr>
				</tbody>
			</table>
		</div>

		<h2>Zones</h2>
		<div v-if="can('Admin')" class="toolbar"><button @click="editing = newZone()">Add zone</button></div>
		<div class="table-wrap">
			<table>
				<thead><tr><th>Zone</th><th>Map</th><th class="num">Soft cap</th><th class="num">Hard cap</th><th class="num">Idle shutdown</th><th class="num">Instances</th><th></th></tr></thead>
				<tbody>
					<tr v-for="z in zones" :key="z.mapID">
						<td>{{ z.zoneName }}</td>
						<td class="mono">{{ z.mapName }}</td>
						<td class="num">{{ z.softPlayerCap }}</td>
						<td class="num">{{ z.hardPlayerCap }}</td>
						<td class="num">{{ z.minutesToShutdownAfterEmpty }} min</td>
						<td class="num">{{ z.mapInstanceCount }}</td>
						<td class="num">
							<template v-if="can('Admin')">
								<button class="link" @click="editing = { ...z }">Edit</button>
								<button v-if="!z.mapInstanceCount" class="link" style="margin-left: 12px" @click="removing = z">Delete</button>
							</template>
						</td>
					</tr>
				</tbody>
			</table>
		</div>

		<h2>Recent zone commands</h2>
		<div class="table-wrap">
			<table>
				<thead><tr><th>Sent</th><th>Command</th><th>Target</th><th>By</th><th>State</th><th>Detail</th></tr></thead>
				<tbody>
					<tr v-for="c in commands" :key="c.commandID">
						<td><Ago :at="c.createDate" /></td>
						<td>{{ c.command.replace(/_/g, ' ') }}</td>
						<td>
							<RouterLink v-if="c.characterName" :to="`/players/character/${encodeURIComponent(c.characterName)}`">{{ c.characterName }}</RouterLink>
							<span v-else class="mono">instance {{ c.mapInstanceID }}</span>
						</td>
						<td class="muted">{{ c.createdBy }}</td>
						<td><span class="tag" :class="{ ok: c.result === 'ok', bad: c.completedAt && c.result !== 'ok', live: !c.completedAt }">{{ commandState(c) }}</span></td>
						<td class="muted">{{ c.resultDetail }}</td>
					</tr>
					<tr v-if="!commands.length"><td colspan="6" class="empty">No commands sent yet.</td></tr>
				</tbody>
			</table>
		</div>

		<ActionDialog :open="!!stopping" :title="`Stop ${stopping?.zoneName}`" confirm-label="Stop instance" danger :run="stop" @close="stopping = null" @done="(m) => (message = m)">
			<p class="muted">
				{{ stopping?.numberOfReportedPlayers ? `${stopping.numberOfReportedPlayers} player(s) on it are disconnected.` : 'Nobody is on it.' }}
				The zone starts again when a player travels there.
			</p>
		</ActionDialog>

		<ActionDialog :open="!!editing" :title="editing?.mapID ? `Edit ${editing.zoneName}` : 'Add a zone'" confirm-label="Save zone" :run="saveZone" @close="editing = null" @done="(m) => (message = m)">
			<template v-if="editing">
				<label class="field">Zone name <input v-model="editing.zoneName" /></label>
				<label class="field">Map name <input v-model="editing.mapName" class="mono" /></label>
				<div class="row">
					<label class="field">Soft cap <input v-model.number="editing.softPlayerCap" type="number" min="1" /></label>
					<label class="field">Hard cap <input v-model.number="editing.hardPlayerCap" type="number" min="1" /></label>
					<label class="field">Idle shutdown, min <input v-model.number="editing.minutesToShutdownAfterEmpty" type="number" min="0" /></label>
				</div>
			</template>
		</ActionDialog>

		<ActionDialog :open="!!removing" :title="`Delete ${removing?.zoneName}`" confirm-label="Delete zone" danger :run="removeZone" @close="removing = null" @done="(m) => (message = m)">
			<p class="muted">Players cannot travel to it until it is added again.</p>
		</ActionDialog>
	</div>
</template>

<style scoped>
.stack {
	display: grid;
	gap: 8px;
}
.row {
	display: flex;
	gap: 12px;
	align-items: center;
	justify-content: space-between;
	flex-wrap: wrap;
}
.row .field input {
	width: 110px;
}
</style>
