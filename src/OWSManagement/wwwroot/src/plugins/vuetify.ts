import { createVuetify, ThemeDefinition } from 'vuetify';
import * as vuetifyComponents from 'vuetify/lib/components/index';
import * as directives from 'vuetify/lib/directives/index';
import { aliases, mdi } from 'vuetify/iconsets/mdi';
import * as labs from 'vuetify/labs/components';
import 'vuetify/styles'

const myCustomLightTheme: ThemeDefinition = {
    dark: false,
    colors: {
      background: '#FFFFFF',
      surface: '#FFFFFF',
      primary: '#6200EE',
      'primary-darken-1': '#3700B3',
      secondary: '#03DAC6',
      'secondary-darken-1': '#018786',
      error: '#B00020',
      info: '#2196F3',
      success: '#4CAF50',
      warning: '#FB8C00',
    }
  }

export const vuetify = createVuetify({
    directives,
    theme:{
        defaultTheme: 'light',
        themes:{
            myCustomLightTheme,
        }
    },
    components: {
        ...vuetifyComponents,
        ...labs,
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
