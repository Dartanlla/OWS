import { owsApiClient } from "../src/owsApiClient";

const client: owsApiClient = new owsApiClient("/api");

export default {

    getUsers() {
        return client.get('/Users');
    },
    addUser(data: Record<string, unknown>) {
        return client.post('/Users', data);
    },
    updateUser(data: Record<string, unknown>) {
        return client.put('/Users', data);
    },

}