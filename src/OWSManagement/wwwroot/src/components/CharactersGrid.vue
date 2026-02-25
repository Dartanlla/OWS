<script setup lang="ts">
    import { ref, reactive, onMounted } from 'vue';
    import owsApi from '../owsApi';

    interface Data 
    {
        headers: Array<object>,
        rows: Array<object>,
        deleteCharacter: Record<string, unknown>,
        deleteCharacterIndex: number,
        addingANewCharacter: boolean
    }

    const data: Data = reactive({
        headers: [
            { title: 'Actions', sortable: false, align: 'start', key: 'actions' },
            { title: 'Character Name', align: 'start', key: 'charName' },
            { title: 'Level', key: 'characterLevel' },
            { title: 'Map Location', key: 'mapName' },
            { title: 'X Location', key: 'x' },
            { title: 'Y Location', key: 'y' },
            { title: 'Z Location', key: 'z' }
        ],
        rows: [],
        deleteCharacter: {},
        deleteCharacterIndex: -1,
        addingANewCharacter: false
    });

     function loadCharactersGrid() {
         owsApi.getCharacters().then((response: any) => {
             if (response.data != null) {
                 data.rows = response.data;
             }

         }).catch((error: any) => {
             console.log(error);
         }).finally(function () {

         });
    }

    function deleteCharacter(characterToDelete: Record<string, unknown>) {
        if (confirm("Are you sure you want to remove the character: " + characterToDelete)) {
            alert("Delete the Character.  Not implemented yet!");
            /*data.deleteUserIndex = data.rows.indexOf(userToDelete);
            data.deleteUser = Object.assign({}, userToDelete);
            owsApi.removeUser(data.deleteUser).then((response: any) => {
                if (response.data != null) {
                    if (response.data) {
                        alert("User deleted!");
                        location.reload(); // Reloads the current page
                    }
                    else {
                        alert("Unable to delete the user!");
                    }
                }
            }).catch((error: any) => {
                console.log(error);
            }).finally(function () {

            });*/
        }
    }

    onMounted(() => {
        loadCharactersGrid();
    });
</script>

<template>
    <v-container>
        <div class="characters-container">
            <div>
                <v-data-table :headers="data.headers"
                              :items="data.rows"
                              :items-per-page="10"
                              class="elevation-1 characters-table">
                    <template v-slot:top>
                        <v-toolbar flat>
                            <v-toolbar-title>Characters</v-toolbar-title>
                            <v-divider class="mx-4"
                                       inset
                                       vertical></v-divider>
                            <v-spacer></v-spacer>
                            <v-btn rounded="pill"
                                   color="primary"
                                   @click="">
                                <v-icon icon="mdi-plus"></v-icon> Add New Character
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
                                @click="deleteCharacter(item.raw)">
                            mdi-delete
                        </v-icon>
                    </template>
                </v-data-table>
            </div>
        </div>
    </v-container>
</template>

<style scoped>
    .characters-container {
        margin-top: 0px;
    }
    .characters-table table thead th {
        background-color: blue;
    }

</style>