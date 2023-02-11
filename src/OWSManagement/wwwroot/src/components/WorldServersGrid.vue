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
    <div class="world-servers-container" >
        <h2>World Servers</h2>
        <div>
            <v-data-table
                :headers = "data.headers"
                :items = "data.rows"
                :items-per-page="5"
                class="elevation-1 users-table">
            </v-data-table>
        </div>
    </div>
</template>

<style scoped>
    .world-servers-container {
        margin-top: 0px;
    }

</style>