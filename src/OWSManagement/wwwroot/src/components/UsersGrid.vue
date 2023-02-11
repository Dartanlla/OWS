<script setup lang="ts">
    import { ref, reactive } from 'vue';

    interface Data {
        headers: Object,
        rows: Object
    }

    const data: Data = reactive({
        headers: [
            {
                title: 'First Name',
                align: 'start',
                sortable: false,
                key: 'firstname',
            },
            { title: 'Last Name', key: 'lastname' },
            { title: 'Email', key: 'email' },
            { title: 'Role', key: 'role' },
        ],
        rows: [
            {
                firstname: 'Test',
                lastname: 'User',
                email: 'test@test.com',
                role: 'Player',
            },
            {
                firstname: 'Test',
                lastname: 'Developer',
                email: 'developer@gmail.com',
                role: 'Developer',
            },
        ]
    });
</script>

<template>
    <div class="users-container">
        <h2>Users</h2>
        <div>
            <v-data-table :headers="data.headers"
                          :items="data.rows"
                          :items-per-page="5"
                          class="elevation-1 users-table"></v-data-table>
        </div>
    </div>
</template>

<style scoped>
    .users-container {
        margin-top: 0px;
    }

    .users-table table thead th {
        background-color: blue;
    }
</style>