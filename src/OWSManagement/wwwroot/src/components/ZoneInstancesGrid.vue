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
            { title: 'Instance ID', align: 'start', key: 'mapInstanceId' },
            { title: 'World Server ID', key: 'worldServerId' },
            { title: 'Map ID', key: 'mapId' },
            { title: 'Port', key: 'port' },
            { title: 'Status', key: 'status' },
            { title: 'Number of Players', key: 'numberOfReportedPlayers' }
        ],
        rows: []
    });

    function loadZoneInstancesGrid() {
        owsApi.getZoneInstances().then((response: any) => {
            if (response.data != null) {
                data.rows = response.data;
            }

        }).catch((error: any) => {
            console.log(error);
        }).finally(function () {

        });
    }

    onMounted(() => {
        loadZoneInstancesGrid();
    });
</script>

<template>
<v-container>
    <div class="zone-instances-container">
        <div>
            <v-data-table :headers="data.headers"
                          :items="data.rows"
                          :items-per-page="10"
                          class="elevation-1 users-table">
                <template v-slot:top>
                    <v-toolbar flat>
                        <v-toolbar-title>Zone Instances</v-toolbar-title>
                        <v-divider class="mx-4"
                                   inset
                                   vertical></v-divider>
                        <v-spacer></v-spacer>
                        <v-btn rounded="pill"
                               color="primary"
                               @click="">
                            <v-icon icon="mdi-plus"></v-icon> Add New Zone Instance
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
    .zone-instances-container {
        margin-top: 0px;
    }

    .users-table table thead th {
        background-color: blue;
    }
</style>