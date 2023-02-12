import { createVuetify } from 'vuetify';
import * as vuetifyComponents from 'vuetify/lib/components/index';
import * as directives from 'vuetify/lib/directives/index';
import { aliases, mdi } from 'vuetify/iconsets/mdi';
import * as labs from 'vuetify/labs/components';

export const vuetify = createVuetify({
    components: {
        ...vuetifyComponents,
        ...labs,
    },
    directives,
    theme: {
        themes: {
            light: {
                colors: {
                    primary: '#1976D2',
                    secondary: '#424242',
                    accent: '#82B1FF',
                    error: '#FF5252',
                    info: '#2196F3',
                    success: '#4CAF50',
                    warning: '#FFC107',
                    background: '#ffffff',
                },
                dark: false,
                variables: {},
            },
        },
    },
    icons: {
        defaultSet: 'mdi',
        aliases,
        sets: {
            mdi,
        }
    },
    defaults: {
        VDataTable: {
            fixedHeader: true,
            noDataText: 'Results not found',
        },
    },
});