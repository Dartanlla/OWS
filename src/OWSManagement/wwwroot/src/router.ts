import { createRouter, createWebHistory } from 'vue-router';

export default createRouter({
	history: createWebHistory(),
	routes: [
		{ path: '/', component: () => import('./views/Overview.vue'), meta: { title: 'Overview' } },
		{ path: '/players', component: () => import('./views/Players.vue'), meta: { title: 'Players' } },
		{ path: '/players/account/:id', component: () => import('./views/Account.vue'), meta: { title: 'Account' } },
		{ path: '/players/character/:name', component: () => import('./views/Character.vue'), meta: { title: 'Character' } },
		{ path: '/players/character/:name/restore', component: () => import('./views/Restore.vue'), meta: { title: 'Restore' } },
		{ path: '/insight', component: () => import('./views/Insight.vue'), meta: { title: 'Insight' } },
		{ path: '/moderation', component: () => import('./views/Moderation.vue'), meta: { title: 'Moderation' } },
		{ path: '/economy', component: () => import('./views/Economy.vue'), meta: { title: 'Economy' } },
		{ path: '/world', component: () => import('./views/World.vue'), meta: { title: 'World' } },
		{ path: '/world/map', component: () => import('./views/LiveMap.vue'), meta: { title: 'Live map' } },
		{ path: '/audit', component: () => import('./views/Audit.vue'), meta: { title: 'Audit' } },
		{ path: '/system', component: () => import('./views/System.vue'), meta: { title: 'System' } },
		{ path: '/:pathMatch(.*)*', redirect: '/' },
	],
});
