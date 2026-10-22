<script setup lang="ts">
    import { reactive, onMounted } from 'vue';
    import owsApi from '../owsApi';

    interface Data {
        headers: Array<object>,
        rows: Array<Record<string, any>>,
        search: string,
        loading: boolean,
        message: string
    }

    const data: Data = reactive({
        headers: [
            { title: 'Time', key: 'createDate' },
            { title: 'Source', key: 'source' },
            { title: 'Actor', key: 'actor' },
            { title: 'Action', key: 'action' },
            { title: 'Target', key: 'target' },
            { title: 'Detail', key: 'detail', sortable: false },
            { title: 'IP', key: 'clientIP' }
        ],
        rows: [],
        search: '',
        loading: false,
        message: ''
    });

    function loadAuditGrid() {
        data.loading = true;

        owsApi.getAudit(data.search, 500).then((response: any) => {
            data.rows = Array.isArray(response.data) ? response.data : [];
            data.message = '';
        }).catch((error: any) => {
            data.message = 'Could not load the audit log: ' + (error?.message ?? 'unknown error');
        }).finally(function () {
            data.loading = false;
        });
    }

    onMounted(() => {
        loadAuditGrid();
    });
</script>

<template>
<v-container>
    <div class="audit-container">
        <v-alert v-if="data.message" type="error" class="mb-4">{{ data.message }}</v-alert>
        <v-data-table :headers="data.headers"
                      :items="data.rows"
                      :loading="data.loading"
                      :items-per-page="50"
                      class="elevation-1">
            <template v-slot:top>
                <v-toolbar flat>
                    <v-toolbar-title>Audit Log</v-toolbar-title>
                    <v-text-field v-model="data.search"
                                  label="Actor, action or target"
                                  density="compact"
                                  hide-details
                                  single-line
                                  class="mx-4"
                                  @keyup.enter="loadAuditGrid"></v-text-field>
                    <v-btn variant="text"
                           :loading="data.loading"
                           @click="loadAuditGrid">
                        <v-icon icon="mdi-magnify"></v-icon> Search
                    </v-btn>
                </v-toolbar>
            </template>
            <template v-slot:item.createDate="{ item }">
                {{ new Date(item.raw.createDate).toLocaleString() }}
            </template>
            <template v-slot:item.detail="{ item }">
                <code class="audit-detail">{{ item.raw.detail }}</code>
            </template>
        </v-data-table>
    </div>
</v-container>
</template>

<style scoped>
    .audit-container {
        margin-top: 0px;
    }

    .audit-detail {
        white-space: pre-wrap;
        word-break: break-all;
        font-size: 0.8em;
    }
</style>
