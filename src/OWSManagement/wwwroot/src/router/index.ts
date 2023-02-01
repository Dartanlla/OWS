import { createRouter, createWebHistory, RouteRecordRaw } from "vue-router";
import routesNames from "@/router/routesNames";

import IndexView from "@/views/IndexView.vue";

const routes: Array<RouteRecordRaw> = [
    {
        path: "/",
        name: routesNames.index,
        component: IndexView,
    }
];

const router = createRouter({
    history: createWebHistory(),
    routes,
});

export default router;
