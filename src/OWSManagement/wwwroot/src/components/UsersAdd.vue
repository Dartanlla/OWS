<script setup lang="ts">
    import router from '../router';
    import { ref, reactive, onMounted } from 'vue';
    import owsApi from '../owsApi';

    interface Data {
        valid: boolean,
        addUserForm: Record<string, unknown>,
        nameRules: Array<any>,
        emailRules: Array<any>,
        passwordRules: Array<any>
    }

    const data: Data = reactive({
        valid: true,
        addUserForm: {
            firstName: '',
            lastName: '',
            email: '',
            password: ''
        },
        nameRules: [
            (v: string) => !!v || 'Name is required',
            (v: HTMLFormElement) => (v && v.length < 20) || "Name must be less than 20 characters",
        ],
        emailRules: [
            (v: string) => !!v || 'E-mail is required',
            (v: string) => /.+@.+\..+/.test(v) || 'E-mail must be valid',
        ],
        passwordRules: [
            (v: string) => !!v || 'Password is required',
            (v: HTMLFormElement) => (v && v.length >= 6) || 'Password must be 6 or more characters in length',
        ],
    });

    const form: any = ref(null);

    function addUser() {
        form.value.validate();

        if (data.valid)
        {
            owsApi.addUser(data.addUserForm).then((response: any) => {
                if (response.data != null) {
                    if (response.data) {
                        router.go(0);
                    }
                    else
                    {
                        alert("Unable to add the new user!");
                    }
                }

            }).catch((error: any) => {
                console.log(error);
            }).finally(function () {

            });
        }
    }

    function cancelAddUser() {
        router.go(0);
    }
</script>

<template>
    <v-container class="container">
    <v-card class="add-a-new-player-card">
        <v-card-title>Add a new Player User</v-card-title>
        <v-card-text>
            <v-form ref="form"
                    v-model="data.valid"
                    lazy-validation>

                <v-text-field v-model="data.addUserForm.firstName"
                              :counter="20"
                              :rules="data.nameRules"
                              label="First Name"
                              required></v-text-field>

                <v-text-field v-model="data.addUserForm.lastName"
                              :counter="20"
                              :rules="data.nameRules"
                              label="Last Name"
                              required></v-text-field>

                <v-text-field v-model="data.addUserForm.email"
                              :rules="data.emailRules"
                              label="E-mail"
                              required></v-text-field>

                <v-text-field v-model="data.addUserForm.password"
                              :rules="data.passwordRules"
                              label="Password"
                              type="password"
                              style="margin-bottom:30px;"
                              required></v-text-field>


                <v-btn color="success"
                       class="mr-4"
                       @click="addUser"
                       style="margin-right: 20px;">
                    Save
                </v-btn>

                <v-btn color="error"
                       class="mr-4"
                       @click="cancelAddUser">
                    Cancel
                </v-btn>
            </v-form>
        </v-card-text>
    </v-card>
    
    </v-container>
</template>

<style scoped>
    .add-a-new-player-card
    {
        min-width: 300px;
        justify-content: center;
        
            
    }
    .container
    {
        justify-content: center;
        display: flex;
    }
    
</style>