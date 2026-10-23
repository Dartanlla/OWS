import { createApp } from 'vue';
import './styles.css';
import App from './App.vue';
import router from './router';

router.afterEach((to) => {
	const title = (to.meta.title as string) || '';
	document.title = title ? `${title} · Samsara Management` : 'Samsara Management';
});

createApp(App).use(router).mount('#app');
