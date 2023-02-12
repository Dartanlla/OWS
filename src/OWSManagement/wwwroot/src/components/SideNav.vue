<script setup lang="ts">
    import { ref } from 'vue'

    const count = ref(0)
</script>

<template>
    <v-list color="transparent">
        <v-list-item href="/" prepend-icon="mdi-view-dashboard" title="Dashboard"></v-list-item>
        <v-list-item href="/users" prepend-icon="mdi-account-box" title="Users"></v-list-item>
        <v-list-item href="/characters" prepend-icon="mdi-account-group" title="Characters"></v-list-item>
        <v-list-item href="/worldservers" prepend-icon="mdi-earth" title="World Servers"></v-list-item>
        <v-list-item href="/zones" prepend-icon="mdi-map" title="Zones"></v-list-item>
        <v-list-item href="/zoneinstances" prepend-icon="mdi-animation" title="Zone Instances"></v-list-item>
        <v-list-item href="/globaldata" prepend-icon="mdi-database" title="Global Data"></v-list-item>
        <v-list-item href="/settings" prepend-icon="mdi-cog" title="Settings"></v-list-item>
    </v-list>
</template>

<style scoped>
</style>