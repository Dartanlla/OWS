<script setup lang="ts">
    import { ref, reactive } from 'vue';

    interface Data {
        headers: Object,
        rows: Object
    }

    const data: Data = reactive({
        headers: [
        ],
        rows: [
        ]
    });
</script>

<template>
<v-container>
    <div class="zone-instances-container">
        <h2>Zone Instances</h2>
        <div>
            <v-data-table :headers="data.headers"
                          :items="data.rows"
                          :items-per-page="5"
                          class="elevation-1 users-table">
            </v-data-table>
        </div>
    </div>
</v-container>
</template>

<style scoped>
    .zone-instances-container {
        margin-top: 0px;
        text-align: center;
    }
</style>