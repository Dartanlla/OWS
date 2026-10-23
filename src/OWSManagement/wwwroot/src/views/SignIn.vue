<script setup lang="ts">
const reasons: Record<string, string> = {
	not_staff: 'This Steam account has no staff role. An Admin can give it one on its account page.',
	steam_rejected: 'Steam did not confirm the sign-in. Try again.',
	bad_state: 'The sign-in took too long or came from another tab. Try again.',
	stale_nonce: 'The sign-in took too long. Try again.',
};
const code = new URLSearchParams(window.location.search).get('login_error');
const error = code ? reasons[code] ?? 'Steam sign-in failed. Try again.' : '';
</script>

<template>
	<main class="signin">
		<div class="card">
			<div class="word">Samsara</div>
			<div class="sub">Management</div>
			<p>For staff accounts only. Sign in with the Steam account you play on.</p>
			<p v-if="error" class="notice bad">{{ error }}</p>
			<a class="button primary" href="/auth/steam">Sign in through Steam</a>
		</div>
	</main>
</template>

<style scoped>
.signin {
	min-height: 100vh;
	display: grid;
	place-items: center;
	background: var(--rail);
	padding: 16px;
}
.card {
	background: var(--paper);
	border-radius: 4px;
	padding: 36px 36px 32px;
	width: min(400px, 100%);
}
.word {
	font: 500 40px/1 var(--font-display);
}
.sub {
	font: 600 11px/1 var(--font);
	letter-spacing: 0.22em;
	text-transform: uppercase;
	color: var(--ink-muted);
	margin: 8px 0 24px;
}
p {
	color: var(--ink-muted);
	margin: 0 0 20px;
}
.button {
	display: inline-flex;
	align-items: center;
	height: 36px;
	padding: 0 16px;
	text-decoration: none;
	background: var(--accent);
	border-color: var(--accent);
	color: #fff;
}
.button:hover {
	background: var(--accent-ink);
	text-decoration: none;
}
</style>
