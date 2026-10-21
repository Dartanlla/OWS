<script setup lang="ts">
    import { reactive, onMounted } from 'vue';
    import owsApi from '../owsApi';

    interface Data {
        headers: Array<object>,
        rows: Array<Record<string, any>>,
        loading: boolean,
        showZoneDialog: boolean,
        isSavingZone: boolean,
        isAddingNewZone: boolean,
        zoneForm: Record<string, any>,
        showDeleteDialog: boolean,
        deleteTarget: Record<string, any>,
        isDeletingZone: boolean,
        message: string,
        messageType: string
    }

    // Matches every row already in the Maps table: caps 60/80, mode 1, one minute of grace
    // once the last player leaves. Width and Height are fixed at 1 by the insert itself.
    function defaultZoneForm(): Record<string, any> {
        return {
            mapID: 0,
            zoneName: '',
            mapName: '',
            worldCompContainsFilter: '',
            worldCompListFilter: '',
            softPlayerCap: 60,
            hardPlayerCap: 80,
            mapMode: 1,
            minutesToShutdownAfterEmpty: 1
        };
    }

    const data: Data = reactive({
        headers: [
            { title: 'Actions', key: 'actions', sortable: false, align: 'start' },
            { title: 'Map ID', key: 'mapID' },
            { title: 'Zone', key: 'zoneName' },
            { title: 'Map', key: 'mapName' },
            { title: 'Soft Cap', key: 'softPlayerCap' },
            { title: 'Hard Cap', key: 'hardPlayerCap' },
            { title: 'Mode', key: 'mapMode' },
            { title: 'Shutdown (min)', key: 'minutesToShutdownAfterEmpty' },
            { title: 'Instances', key: 'mapInstanceCount' }
        ],
        rows: [],
        loading: false,
        showZoneDialog: false,
        isSavingZone: false,
        isAddingNewZone: true,
        zoneForm: defaultZoneForm(),
        showDeleteDialog: false,
        deleteTarget: {},
        isDeletingZone: false,
        message: '',
        messageType: 'success'
    });

    function loadZonesGrid() {
        data.loading = true;

        owsApi.getZones().then((response: any) => {
            data.rows = Array.isArray(response.data) ? response.data : [];
        }).catch((error: any) => {
            data.messageType = 'error';
            data.message = 'Could not load zones: ' + (error?.message ?? 'unknown error');
        }).finally(function () {
            data.loading = false;
        });
    }

    function clickAddNewZone() {
        data.zoneForm = defaultZoneForm();
        data.isAddingNewZone = true;
        data.message = '';
        data.showZoneDialog = true;
    }

    function clickEditZone(zone: Record<string, any>) {
        data.zoneForm = Object.assign(defaultZoneForm(), zone);
        data.isAddingNewZone = false;
        data.message = '';
        data.showZoneDialog = true;
    }

    function zoneDialogClose() {
        data.showZoneDialog = false;
    }

    // Single-zone maps name the zone after the level, which is every row in the table today.
    // Mirroring saves retyping it, and only while Map Name is still untouched.
    function zoneNameChanged(value: string) {
        if (data.isAddingNewZone && !data.zoneForm.mapName) {
            data.zoneForm.mapName = value;
        }
    }

    function zoneSave() {
        data.isSavingZone = true;
        data.message = '';

        const call = data.isAddingNewZone
            ? owsApi.addZone(data.zoneForm)
            : owsApi.updateZone(data.zoneForm);

        call.then((response: any) => {
            if (response.data?.success) {
                data.messageType = 'success';
                data.message = data.isAddingNewZone
                    ? `Added ${data.zoneForm.zoneName}. The packaged server build must contain a `
                      + `level named ${data.zoneForm.mapName} for it to start.`
                    : `Saved ${data.zoneForm.zoneName}.`;
                data.showZoneDialog = false;
                // Re-read rather than patching the row: MapID is assigned by a sequence, and
                // MapInstanceCount is computed.
                loadZonesGrid();
            }
            else {
                data.messageType = 'error';
                data.message = response.data?.errorMessage || 'Unable to save the zone.';
            }
        }).catch((error: any) => {
            data.messageType = 'error';
            data.message = 'Could not save: ' + (error?.message ?? 'unknown error');
        }).finally(function () {
            data.isSavingZone = false;
        });
    }

    function clickDeleteZone(zone: Record<string, any>) {
        data.deleteTarget = zone;
        data.message = '';
        data.showDeleteDialog = true;
    }

    function deleteDialogClose() {
        data.showDeleteDialog = false;
        data.deleteTarget = {};
    }

    function deleteZoneConfirm() {
        data.isDeletingZone = true;

        owsApi.deleteZone(data.deleteTarget.mapID).then((response: any) => {
            if (response.data?.success) {
                data.messageType = 'success';
                data.message = `Deleted ${data.deleteTarget.zoneName}.`;
                data.showDeleteDialog = false;
                data.deleteTarget = {};
                loadZonesGrid();
            }
            else {
                data.messageType = 'error';
                data.message = response.data?.errorMessage || 'Unable to delete the zone.';
                data.showDeleteDialog = false;
            }
        }).catch((error: any) => {
            data.messageType = 'error';
            data.message = 'Could not delete: ' + (error?.message ?? 'unknown error');
            data.showDeleteDialog = false;
        }).finally(function () {
            data.isDeletingZone = false;
        });
    }

    onMounted(() => {
        loadZonesGrid();
    });
</script>

<template>
<v-container>
    <div class="zones-container">
        <div>
            <v-data-table :headers="data.headers"
                          :items="data.rows"
                          :loading="data.loading"
                          :items-per-page="25"
                          class="elevation-1 zones-table">
                <template v-slot:top>
                    <v-toolbar flat>
                        <v-toolbar-title>Zones</v-toolbar-title>
                        <v-divider class="mx-4"
                                   inset
                                   vertical></v-divider>
                        <v-spacer></v-spacer>
                        <v-btn variant="text"
                               :loading="data.loading"
                               @click="loadZonesGrid">
                            <v-icon icon="mdi-refresh"></v-icon> Refresh
                        </v-btn>
                        <v-btn rounded="pill"
                               color="primary"
                               @click="clickAddNewZone">
                            <v-icon icon="mdi-plus"></v-icon> Add Zone
                        </v-btn>

                        <v-dialog v-model="data.showZoneDialog"
                                  max-width="720px">
                            <v-card>
                                <v-card-title>{{ data.isAddingNewZone ? 'Add Zone' : 'Edit Zone' }}</v-card-title>

                                <v-card-text>
                                    <v-container>
                                        <v-row>
                                            <v-col cols="12"
                                                   sm="6">
                                                <v-text-field v-model="data.zoneForm.zoneName"
                                                              label="Zone Name"
                                                              hint="What the launcher resolves. Must be unique."
                                                              persistent-hint
                                                              @update:modelValue="zoneNameChanged"
                                                              required></v-text-field>
                                            </v-col>
                                            <v-col cols="12"
                                                   sm="6">
                                                <v-text-field v-model="data.zoneForm.mapName"
                                                              label="Map Name"
                                                              hint="The level name in the packaged build, e.g. L_Gullwing01."
                                                              persistent-hint
                                                              required></v-text-field>
                                            </v-col>
                                            <v-col cols="12"
                                                   sm="6">
                                                <v-text-field v-model.number="data.zoneForm.softPlayerCap"
                                                              label="Soft Player Cap"
                                                              min="0"
                                                              type="number"></v-text-field>
                                            </v-col>
                                            <v-col cols="12"
                                                   sm="6">
                                                <v-text-field v-model.number="data.zoneForm.hardPlayerCap"
                                                              label="Hard Player Cap"
                                                              min="0"
                                                              type="number"></v-text-field>
                                            </v-col>
                                            <v-col cols="12"
                                                   sm="6">
                                                <v-text-field v-model.number="data.zoneForm.mapMode"
                                                              label="Map Mode"
                                                              type="number"></v-text-field>
                                            </v-col>
                                            <v-col cols="12"
                                                   sm="6">
                                                <v-text-field v-model.number="data.zoneForm.minutesToShutdownAfterEmpty"
                                                              label="Minutes To Shutdown After Empty"
                                                              min="0"
                                                              type="number"></v-text-field>
                                            </v-col>
                                            <v-col cols="12"
                                                   sm="6">
                                                <v-text-field v-model="data.zoneForm.worldCompContainsFilter"
                                                              label="World Comp Contains Filter"></v-text-field>
                                            </v-col>
                                            <v-col cols="12"
                                                   sm="6">
                                                <v-text-field v-model="data.zoneForm.worldCompListFilter"
                                                              label="World Comp List Filter"></v-text-field>
                                            </v-col>
                                        </v-row>
                                    </v-container>
                                </v-card-text>

                                <v-card-actions>
                                    <v-spacer></v-spacer>
                                    <v-btn color="success"
                                           :loading="data.isSavingZone"
                                           @click="zoneSave">
                                        Save
                                    </v-btn>
                                    <v-btn color="error"
                                           @click="zoneDialogClose">
                                        Cancel
                                    </v-btn>
                                </v-card-actions>
                            </v-card>
                        </v-dialog>

                        <v-dialog v-model="data.showDeleteDialog"
                                  max-width="480px">
                            <v-card>
                                <v-card-title>Delete Zone</v-card-title>
                                <v-card-text>
                                    Delete <strong>{{ data.deleteTarget.zoneName }}</strong> (MapID
                                    {{ data.deleteTarget.mapID }})? This only removes the row from the
                                    Maps table; it does not touch the packaged build.
                                </v-card-text>
                                <v-card-actions>
                                    <v-spacer></v-spacer>
                                    <v-btn color="error"
                                           :loading="data.isDeletingZone"
                                           @click="deleteZoneConfirm">
                                        Delete
                                    </v-btn>
                                    <v-btn variant="text"
                                           @click="deleteDialogClose">
                                        Cancel
                                    </v-btn>
                                </v-card-actions>
                            </v-card>
                        </v-dialog>
                    </v-toolbar>

                    <v-alert v-if="data.message" :type="data.messageType as any" density="compact" class="ma-2">
                        {{ data.message }}
                    </v-alert>
                </template>

                <template v-slot:item.actions="{ item }">
                    <v-icon size="small"
                            class="me-2"
                            icon="mdi-pencil"
                            title="Edit this zone"
                            @click="clickEditZone(item.raw)"></v-icon>
                    <v-icon size="small"
                            icon="mdi-delete"
                            :class="{ 'text-disabled': item.raw.mapInstanceCount > 0 }"
                            :title="item.raw.mapInstanceCount > 0
                                ? 'Shut its instances down first'
                                : 'Delete this zone'"
                            @click="item.raw.mapInstanceCount > 0 ? null : clickDeleteZone(item.raw)"></v-icon>
                </template>

                <template v-slot:no-data>
                    <div class="pa-4">No zones yet.</div>
                </template>
            </v-data-table>

            <v-alert type="info" variant="tonal" density="compact" class="ma-2 text-left">
                A row here only registers the zone with OWS. The packaged server build on the host
                still has to contain a level matching <strong>Map Name</strong>, or the launcher
                will accept the spin-up and then fail to start it.
            </v-alert>
        </div>
    </div>
</v-container>
</template>

<style scoped>
    .zones-container {
        margin-top: 0px;
        text-align: center;
    }
</style>
