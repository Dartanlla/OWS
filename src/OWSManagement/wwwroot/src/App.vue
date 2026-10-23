<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { http, session, loadSession, signOut, type Overview } from './api';
import SignIn from './views/SignIn.vue';

const router = useRouter();
const search = ref('');
const status = ref<Overview | null>(null);
let timer = 0;

const nav = [
	{ to: '/', label: 'Overview' },
	{ to: '/players', label: 'Players' },
	{ to: '/moderation', label: 'Moderation' },
	{ to: '/economy', label: 'Economy' },
	{ to: '/world', label: 'World' },
	{ to: '/audit', label: 'Audit' },
	{ to: '/system', label: 'System' },
];

async function refreshStatus() {
	if (!session.user) return;
	try {
		status.value = (await http.get<Overview>('/Overview')).data;
	} catch {
		status.value = null;
	}
}

function find() {
	const q = search.value.trim();
	router.push({ path: '/players', query: q ? { q } : {} });
}

onMounted(async () => {
	await loadSession();
	refreshStatus();
	timer = window.setInterval(refreshStatus, 30000);
});
onUnmounted(() => window.clearInterval(timer));
</script>

<template>
	<template v-if="session.checked">
		<SignIn v-if="!session.user" />
		<div v-else class="shell">
			<nav class="rail" aria-label="Sections">
				<RouterLink to="/" class="mark">
					<span class="word">Samsara</span>
					<span class="sub">Management</span>
				</RouterLink>
				<RouterLink v-for="item in nav" :key="item.to" :to="item.to" class="nav-item" :class="{ exact: item.to === '/' }">
					{{ item.label }}
				</RouterLink>
				<div class="who">
					<div>{{ session.user.name }}</div>
					<div class="role">{{ session.user.role }}</div>
					<button class="link signout" @click="signOut">Sign out</button>
				</div>
			</nav>
			<div class="main">
				<header class="top">
					<form role="search" @submit.prevent="find">
						<input v-model="search" type="search" placeholder="Find a character, account, email or Steam ID" aria-label="Find a player" />
					</form>
					<div v-if="status" class="strip">
						<span><span class="dot live"></span>{{ status.overview.onlineNow }} online</span>
						<span><span class="dot" :class="status.overview.zonesUp > 0 ? 'ok' : 'bad'"></span>{{ status.overview.zonesUp }} zones up</span>
						<span v-if="!status.loginsOpen" class="tag warn">Logins closed</span>
					</div>
				</header>
				<RouterView />
			</div>
		</div>
	</template>
</template>

<style scoped>
.shell {
	display: grid;
	grid-template-columns: 196px 1fr;
	min-height: 100vh;
}
.rail {
	background: var(--rail);
	color: var(--rail-text);
	display: flex;
	flex-direction: column;
	padding: 20px 0 16px;
	position: sticky;
	top: 0;
	height: 100vh;
}
.mark {
	display: grid;
	padding: 0 20px 22px;
	color: var(--rail-text-active);
	text-decoration: none;
}
.mark .word {
	font: 500 24px/1 var(--font-display);
}
.mark .sub {
	font: 600 10px/1 var(--font);
	letter-spacing: 0.22em;
	text-transform: uppercase;
	color: var(--rail-text);
	margin-top: 6px;
}
.nav-item {
	color: var(--rail-text);
	padding: 8px 20px;
	border-left: 3px solid transparent;
	font-weight: 600;
	text-decoration: none;
}
.nav-item:hover {
	color: var(--rail-text-active);
	text-decoration: none;
}
.nav-item.router-link-active:not(.exact),
.nav-item.exact.router-link-exact-active {
	color: var(--rail-text-active);
	border-left-color: var(--paper);
	background: rgba(244, 239, 230, 0.06);
}
.who {
	margin-top: auto;
	padding: 14px 20px 0;
	border-top: 1px solid rgba(244, 239, 230, 0.1);
	color: var(--rail-text-active);
	font-weight: 600;
}
.who .role {
	color: var(--rail-text);
	font-weight: 400;
	font-size: 12px;
}
.signout {
	color: var(--rail-text);
	margin-top: 8px;
	font-size: 12px;
	font-weight: 400;
}
.main {
	min-width: 0;
}
.top {
	display: flex;
	align-items: center;
	gap: 20px;
	padding: 12px 32px;
	border-bottom: 1px solid var(--rule);
	background: var(--paper-raised);
	position: sticky;
	top: 0;
	z-index: 1;
}
.top form {
	flex: 1;
	max-width: 520px;
}
.top input {
	width: 100%;
}
.strip {
	display: flex;
	gap: 18px;
	align-items: center;
	color: var(--ink-muted);
	font-weight: 600;
	font-size: 13px;
	margin-left: auto;
}

@media (max-width: 760px) {
	.shell {
		grid-template-columns: 1fr;
	}
	.rail {
		position: static;
		height: auto;
		flex-direction: row;
		overflow-x: auto;
		padding: 10px 8px;
		align-items: center;
	}
	.mark {
		padding: 0 12px 0 8px;
	}
	.mark .sub,
	.who {
		display: none;
	}
	.nav-item {
		border-left: none;
		border-bottom: 2px solid transparent;
		padding: 6px 10px;
		white-space: nowrap;
	}
	.nav-item.router-link-active:not(.exact),
	.nav-item.exact.router-link-exact-active {
		border-bottom-color: var(--paper);
	}
	.top {
		padding: 10px 16px;
		flex-wrap: wrap;
	}
	.strip {
		margin-left: 0;
	}
}
</style>
