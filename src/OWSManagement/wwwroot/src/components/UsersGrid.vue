<script setup lang="ts">
    import { ref, reactive, onMounted } from 'vue';
    import UsersAdd from "./UsersAdd.vue";
    import owsApi from '../owsApi';

    interface Data {
        headers: Array<object>,
        rows: Array<object>,
        showEditingUserDialog: boolean,
        editUser: Record<string, unknown>,
        editUserIndex: number,
        addingANewUser: boolean
    }

    const data: Data = reactive({
        headers: [
            { title: 'Actions', sortable: false, align: 'start', key: 'actions' },
            { title: 'First Name', align: 'start', key: 'firstName', },
            { title: 'Last Name', key: 'lastName' },
            { title: 'Email', key: 'email' },
            { title: 'Role', key: 'role' }
        ],
        rows: [],
        showEditingUserDialog: false,
        editUser: {},
        editUserIndex: -1,
        addingANewUser: false
    });

    function loadUsersGrid() {
        owsApi.getUsers().then((response: any) => {
            if (response.data != null) {
                data.rows = response.data;
            }

        }).catch((error: any) => {
            console.log(error);
        }).finally(function () {

        });
    }

    function clickAddNewUser() {
        data.addingANewUser = true;
    }

    function editUser(userToEdit: Record<string, unknown>) {
        data.editUserIndex = data.rows.indexOf(userToEdit);
        data.editUser = Object.assign({}, userToEdit);
        data.showEditingUserDialog = true;
    }

    function editUserSave() {
        owsApi.updateUser(data.editUser).then((response: any) => {
            if (response.data != null) {
                if (response.data) {
                    data.rows[data.editUserIndex] = data.editUser;
                    data.showEditingUserDialog = false;
                }
                else {
                    alert("Unable to update the user!");
                }
            }

        }).catch((error: any) => {
            console.log(error);
        }).finally(function () {

        });
    }

    function editUserClose() {
        data.showEditingUserDialog = false;
    }

    function deleteUser(userToDelete: Record<string, unknown>) {
        if (confirm("Are you sure you want to remove the player: " + userToDelete.firstName + " " + userToDelete.lastName)) {
            alert("Delete the user.  Not implemented yet!");
        }
    }

    onMounted(() => {
        loadUsersGrid();
    });
</script>

<template>
<v-container>
    <div class="users-container">
        <div v-if="data.addingANewUser">
            <UsersAdd />
        </div>
        <div v-else>
            <div>
                <v-data-table :headers="data.headers"
                              :items="data.rows"
                              :items-per-page="5"
                              class="elevation-1 users-table">
                        
                    <template v-slot:top>
                        <v-toolbar flat>
                            <v-toolbar-title>Users</v-toolbar-title>
                            <v-divider class="mx-4"
                                       inset
                                       vertical></v-divider>
                            <v-spacer></v-spacer>
                            <v-btn rounded="pill"
                                   color="primary"
                                   @click="clickAddNewUser">
                                <v-icon icon="mdi-plus"></v-icon> Add New Player User
                            </v-btn>
                            <v-dialog v-model="data.showEditingUserDialog"
                                      max-width="500px">
                                <v-card>
                                    <v-card-title>Edit User</v-card-title>

                                    <v-card-text>
                                        <v-container>
                                            <v-row>
                                                <v-col cols="10"
                                                       sm="10"
                                                       md="10">
                                                    <v-text-field v-model="data.editUser.firstName"
                                                                  label="First Name"></v-text-field>
                                                </v-col>
                                                <v-col cols="10"
                                                       sm="10"
                                                       md="10">
                                                    <v-text-field v-model="data.editUser.lastName"
                                                                  label="Last Name"></v-text-field>
                                                </v-col>
                                                <v-col cols="10"
                                                       sm="10"
                                                       md="10">
                                                    <v-text-field v-model="data.editUser.email"
                                                                  label="Email"></v-text-field>
                                                </v-col>
                                            </v-row>
                                        </v-container>
                                    </v-card-text>

                                    <v-card-actions>
                                        <v-spacer></v-spacer>
                                        <v-btn color="success"
                                               @click="editUserSave">
                                            Save
                                        </v-btn>
                                        <v-btn color="error"
                                               @click="editUserClose">
                                            Cancel
                                        </v-btn>
                                    </v-card-actions>
                                </v-card>
                            </v-dialog>
                        </v-toolbar>
                    </template>

                    <template v-slot:item.actions="{ item }">
                        <v-icon size="small"
                                class="me-2"
                                @click="editUser(item.raw)"
                                style="margin-right:10px;">
                            mdi-pencil
                        </v-icon>
                        <v-icon size="small"
                                @click="deleteUser(item.raw)">
                            mdi-delete
                        </v-icon>
                    </template>
                </v-data-table>
            </div>
        </div>
    </div>
</v-container>
</template>

<style scoped>
    .users-container {
        margin-top: 0px;
    }

    .users-table table thead th {
        background-color: blue;
    }
</style>