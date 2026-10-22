<script setup lang="ts">
    import { reactive, onMounted, onUnmounted } from 'vue';
    import owsApi from '../owsApi';

    interface Data {
        headers: Array<object>,
        rows: Array<Record<string, any>>,
        loading: boolean,
        loadedAt: number,
        message: string
    }

    const statusNames: Record<number, string> = { 1: 'Starting', 2: 'Ready', 3: 'Shutting down' };

    const data: Data = reactive({
        headers: [
            { title: 'Instance', key: 'mapInstanceID' },
            { title: 'Zone', key: 'zoneName' },
            { title: 'Region', key: 'serverRegion' },
            { title: 'Port', key: 'port' },
            { title: 'Status', key: 'status' },
            { title: 'Players', key: 'numberOfReportedPlayers' },
            { title: 'Characters', key: 'onlineCharacters', sortable: false },
            { title: 'Frame ms (avg / max)', key: 'serverFrameMsAvg' },
            { title: 'CPU (% of a core)', key: 'serverCpuPct' },
            { title: 'Memory MB', key: 'memoryUsedMB' },
            { title: 'Uptime', key: 'uptimeSeconds' },
            { title: 'Build', key: 'buildCL' },
            { title: 'Last heartbeat', key: 'lastUpdateFromServer' }
        ],
        rows: [],
        loading: false,
        loadedAt: Date.now(),
        message: ''
    });

    function loadZoneInstancesGrid() {
        data.loading = true;

        owsApi.getZoneInstances().then((response: any) => {
            data.rows = Array.isArray(response.data) ? response.data : [];
            data.loadedAt = Date.now();
            data.message = '';
        }).catch((error: any) => {
            data.message = 'Could not load zone instances: ' + (error?.message ?? 'unknown error');
        }).finally(function () {
            data.loading = false;
        });
    }

    function formatUptime(seconds: number | null): string {
        if (seconds == null) {
            return '';
        }
        const hours = Math.floor(seconds / 3600);
        const minutes = Math.floor((seconds % 3600) / 60);
        return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
    }

    // MapInstances.LastUpdateFromServer is a UTC timestamp without a zone, so it arrives without a 'Z'.
    function formatHeartbeatAge(value: string | null): string {
        if (!value) {
            return 'never';
        }
        const utc = /Z|[+-]\d\d:\d\d$/.test(value) ? value : value + 'Z';
        return Math.max(0, Math.round((data.loadedAt - Date.parse(utc)) / 1000)) + ' s ago';
    }

    let refreshTimer: number | undefined;

    onMounted(() => {
        loadZoneInstancesGrid();
        refreshTimer = window.setInterval(loadZoneInstancesGrid, 10000);
    });

    onUnmounted(() => {
        window.clearInterval(refreshTimer);
    });
</script>

<template>
<v-container>
    <div class="zone-instances-container">
        <v-alert v-if="data.message" type="error" class="mb-4">{{ data.message }}</v-alert>
        <div>
            <v-data-table :headers="data.headers"
                          :items="data.rows"
                          :loading="data.loading"
                          :items-per-page="25"
                          class="elevation-1 users-table">
                <template v-slot:top>
                    <v-toolbar flat>
                        <v-toolbar-title>Zone Instances</v-toolbar-title>
                        <v-spacer></v-spacer>
                        <v-btn variant="text"
                               :loading="data.loading"
                               @click="loadZoneInstancesGrid">
                            <v-icon icon="mdi-refresh"></v-icon> Refresh
                        </v-btn>
                    </v-toolbar>
                </template>
                <template v-slot:item.status="{ item }">
                    {{ statusNames[item.raw.status] ?? item.raw.status }}
                </template>
                <template v-slot:item.serverFrameMsAvg="{ item }">
                    <span v-if="item.raw.serverFrameMsAvg != null">{{ item.raw.serverFrameMsAvg.toFixed(1) }} / {{ item.raw.serverFrameMsMax?.toFixed(1) }}</span>
                </template>
                <template v-slot:item.serverCpuPct="{ item }">
                    <span v-if="item.raw.serverCpuPct != null">{{ Math.round(item.raw.serverCpuPct) }}</span>
                </template>
                <template v-slot:item.uptimeSeconds="{ item }">
                    {{ formatUptime(item.raw.uptimeSeconds) }}
                </template>
                <template v-slot:item.buildCL="{ item }">
                    {{ item.raw.buildCL === 0 ? 'dev' : item.raw.buildCL }}
                </template>
                <template v-slot:item.lastUpdateFromServer="{ item }">
                    {{ formatHeartbeatAge(item.raw.lastUpdateFromServer) }}
                </template>
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
