<script lang="ts">
    import { defineComponent } from 'vue';
    import Dashboard from './components/Dashboard.vue';
    import SideNav from './components/SideNav.vue';

    export default defineComponent({
        name: 'app',
        data() {
            return {
                drawer: false
            };
        },
        components: {
            Dashboard,
            SideNav
        }
    });
</script>

<template>
    <v-app id="app">
        <v-navigation-drawer 
            theme="dark" 
            v-model="drawer"
        >
            <SideNav />
        </v-navigation-drawer>

        <v-app-bar>
            <v-app-bar-nav-icon @click="drawer = !drawer"></v-app-bar-nav-icon>

            <v-toolbar-title>OWS Management</v-toolbar-title>
        </v-app-bar>

        <v-main>
            <router-view v-slot="{ Component }">
                <component :is="Component" />
            </router-view>
        </v-main>
    </v-app>
</template>

<style scoped>
.logo {
  height: 6em;
  padding: 1.5em;
  will-change: filter;
  transition: filter 300ms;
}
.logo:hover {
  filter: drop-shadow(0 0 2em #646cffaa);
}
.logo.vue:hover {
  filter: drop-shadow(0 0 2em #42b883aa);
}
</style>
