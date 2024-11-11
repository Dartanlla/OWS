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
    removeUser(data: Record<string, unknown>) {
        return client.post('/Users/RemoveUser', data);
    },
    getCharacters() {
        return client.get('/Characters/Get');
    },
    getWorldServers() {
        return client.get('/WorldServers/Get');
    },
    getZones() {
        return client.get('/Zones/Get');
    },
    getZoneInstances() {
        return client.get('/Zones/GetInstances');
    },
}
