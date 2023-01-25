import { createApp } from 'vue';
import App from './App.vue';
import router from '@/router';
import store from '@/store';

// Bootstrap
import "bootstrap/dist/css/bootstrap.min.css";
import "bootstrap";
import "bootstrap-icons/font/bootstrap-icons.css";

const app = createApp(App);

app.use(store).use(router).mount('#app');
