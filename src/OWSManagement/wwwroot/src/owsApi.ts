import { owsApiClient } from "../src/owsApiClient";

const client: owsApiClient = new owsApiClient("/api");

export default {

    searchUsers(search: string) {
        return client.get('/Users', { params: { search } });
    },
    getRoles() {
        return client.get('/Users/Roles');
    },
    addUser(data: Record<string, unknown>) {
        return client.post('/Users', data);
    },
    updateUser(data: Record<string, unknown>) {
        return client.put('/Users', data);
    },
    setUserNetworkTestFlag(data: Record<string, unknown>) {
        return client.put('/Users/NetworkTestFlag', data);
    },
    searchCharacters(search: string) {
        return client.get('/Characters', { params: { search } });
    },
    getCharactersForUser(userGuid: string) {
        return client.get('/Characters/ForUser/' + userGuid);
    },
    setCharacterFlags(data: Record<string, unknown>) {
        return client.put('/Characters/Flags', data);
    },
    getZones() {
        return client.get('/Zones');
    },
    addZone(data: Record<string, unknown>) {
        return client.post('/Zones', data);
    },
    updateZone(data: Record<string, unknown>) {
        return client.put('/Zones', data);
    },
    deleteZone(mapId: number) {
        return client.delete('/Zones/' + mapId);
    },
    getZoneInstances() {
        return client.get('/Instances');
    },
    getAudit(search: string, limit: number) {
        return client.get('/Audit', { params: { search, limit } });
    },
    getStatus() {
        return client.get('/System/Status');
    },

}
