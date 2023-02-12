<script setup lang="ts">
import { ref } from 'vue'

//defineProps<{ msg: string }>()

const count = ref(0)
</script>

<template>
    <div class="dashboard-content">
        <img src="../assets/logo.png" class="img-fluid">
        <h2>Open World Server 2</h2>

        <p><a href="http://www.sabredartstudios.com/">By SabreDartStudios</a></p>

        <div class="d-flex justify-space-around" style="margin-top:30px;">
            <v-btn color="primary" href="https://www.openworldserver.com/" style="margin-right:30px;"><v-icon icon="mdi-checkbox-marked-circle"></v-icon> Documentation</v-btn>
            <v-btn color="secondary" href="https://www.youtube.com/watch?v=yRXzbaNT6_k"><v-icon icon="mdi-cancel"></v-icon> Watch Tutorials</v-btn>
        </div>
    </div>
</template>

<style scoped>
    .dashboard-content {
        text-align: center;
    }
</style>
