<script setup lang="ts">
    import { ref } from 'vue'

    const count = ref(0)
</script>

<template>
    <v-list color="transparent">
        <v-list-item href="/" prepend-icon="mdi-view-dashboard" title="Dashboard"></v-list-item>
        <v-list-item href="/Users" prepend-icon="mdi-account-box" title="Users"></v-list-item>
        <v-list-item href="/Settings" prepend-icon="mdi-cog" title="Settings"></v-list-item>
    </v-list>
</template>

<style scoped>
</style>