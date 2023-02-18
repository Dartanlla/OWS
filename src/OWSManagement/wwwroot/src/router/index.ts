import { createWebHistory, createRouter } from "vue-router";
import Dashboard from "../components/Dashboard.vue";
import UsersGrid from "../components/UsersGrid.vue";
import CharactersGrid from "../components/CharactersGrid.vue";
import WorldServersGrid from "../components/WorldServersGrid.vue";
import ZonesGrid from "../components/ZonesGrid.vue";
import ZoneInstancesGrid from "../components/ZoneInstancesGrid.vue";
import GlobalDataGrid from "../components/GlobalDataGrid.vue";

const routes = [
    {
        path: "/",
        name: "Dashboard",
        component: Dashboard,
        meta: {
            requiresAuth: false
        }
    },
    {
        path: "/users",
        name: "UsersGrid",
        component: UsersGrid,
        meta: {
            requiresAuth: false
        }
    },
    {
        path: "/characters",
        name: "CharactersGrid",
        component: CharactersGrid,
        meta: {
            requiresAuth: false
        }
    },
    {
        path: "/worldservers",
        name: "WorldServersGrid",
        component: WorldServersGrid,
        meta: {
            requiresAuth: false
        }
    },
    {
        path: "/zones",
        name: "ZonesGrid",
        component: ZonesGrid,
        meta: {
            requiresAuth: false
        }
    },
    {
        path: "/zoneinstances",
        name: "ZoneInstancesGrid",
        component: ZoneInstancesGrid,
        meta: {
            requiresAuth: false
        }
    },
    {
        path: "/globaldata",
        name: "GlobalDataGrid",
        component: GlobalDataGrid,
        meta: {
            requiresAuth: false
        }
    },
];

const router = createRouter({
    history: createWebHistory(),
    routes,
});

export default router;
