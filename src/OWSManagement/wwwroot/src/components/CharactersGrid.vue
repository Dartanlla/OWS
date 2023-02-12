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
    <div class="characters-container" >
        <h2>Characters</h2>
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
    .characters-container {
        margin-top: 0px;
    }

</style>