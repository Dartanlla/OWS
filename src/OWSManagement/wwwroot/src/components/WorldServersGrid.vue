<script setup lang="ts">
    import { ref, reactive, onMounted } from 'vue';
    import owsApi from '../owsApi';

    interface Data {
        headers: Array<object>,
        rows: Array<object>
    }

    const data: Data = reactive({
        headers: [
            { title: 'Actions', sortable: false, align: 'start', key: 'actions' },
            { title: 'World ID', align: 'start', key: 'worldServerId' },
            { title: 'Internal IP', key: 'internalServerIp' },
            { title: 'Public IP', key: 'serverIp' },
            { title: 'Port', key: 'port' },
            { title: 'Starting Port', key: 'startingMapInstancePort' },
            { title: 'Max Instances', key: 'maxNumberOfInstances' },
            { title: 'Status', key: 'serverStatus' }
        ],
        rows: []
    });

    function loadWorldServersGrid() {
        owsApi.getWorldServers().then((response: any) => {
            if (response.data != null) {
                data.rows = response.data;
            }

        }).catch((error: any) => {
            console.log(error);
        }).finally(function () {

        });
    }

    onMounted(() => {
        loadWorldServersGrid();
    });
</script>

<template>
    <v-container>
    <div class="world-servers-container" >
        <div>
            <v-data-table :headers="data.headers"
                          :items="data.rows"
                          :items-per-page="5"
                          class="elevation-1 users-table">
                <template v-slot:top>
                    <v-toolbar flat>
                        <v-toolbar-title>World Servers</v-toolbar-title>
                        <v-divider class="mx-4"
                                   inset
                                   vertical></v-divider>
                        <v-spacer></v-spacer>
                        <v-btn rounded="pill"
                               color="primary"
                               @click="">
                            <v-icon icon="mdi-plus"></v-icon> Add New Server
                        </v-btn>
                    </v-toolbar>
                </template>
                <template v-slot:item.actions="{ item }">
                    <v-icon size="small"
                            class="me-2"
                            @click=""
                            style="margin-right:10px;">
                        mdi-pencil
                    </v-icon>
                    <v-icon size="small"
                            @click="">
                        mdi-delete
                    </v-icon>
                </template>
            </v-data-table>
        </div>
    </div>
</v-container>
</template>

<style scoped>
    .world-servers-container {
        margin-top: 0px;
    }
    .users-table table thead th {
        background-color: blue;
    }

</style>