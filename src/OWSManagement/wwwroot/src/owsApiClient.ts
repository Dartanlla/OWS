import axios from 'axios';
import { AxiosInstance } from 'axios';

/**
 * Create a new Axios client instance
 * @see https://github.com/mzabriskie/axios#creating-an-instance
 */
const getClient = (baseUrl = "") => {

    const options: object | undefined = {
        baseURL: baseUrl,
        headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json',
            'X-CustomerGUID': ''
        },
        timeout: 60000
    };

    const client = axios.create(options);

    client.interceptors.response.use(
        function (response) {
            return response;
        },
        function (error) {
            const res = error.response;
            if (res === null) {
                window.location.href = "/";
                console.error("Timeout!");
            }
            else {
                if (res.status === 401) {
                    window.location.href = "/";
                    console.error("Unauthorized API Call!");
                }
                if (res.status === 500) {
                    alert("An error has occured!");
                }
            }

            return Promise.reject(error);
        }
    );


    return client;
};

class owsApiClient {
    client: AxiosInstance;

    constructor(baseUrl: string) {
        this.client = getClient(baseUrl);
    }

    get(url: string, conf = {}) {
        return this.client.get(url, conf)
            .then(response => Promise.resolve(response))
            .catch(error => Promise.reject(error));
    }

    delete(url: string, conf = {}) {
        return this.client.delete(url, conf)
            .then(response => Promise.resolve(response))
            .catch(error => Promise.reject(error));
    }

    head(url: string, conf = {}) {
        return this.client.head(url, conf)
            .then(response => Promise.resolve(response))
            .catch(error => Promise.reject(error));
    }

    options(url: string, conf = {}) {
        return this.client.options(url, conf)
            .then(response => Promise.resolve(response))
            .catch(error => Promise.reject(error));
    }

    post(url: string, data = {}, conf = {}) {
        return this.client.post(url, data, conf)
            .then(response => Promise.resolve(response))
            .catch(error => Promise.reject(error));
    }

    put(url: string, data = {}, conf = {}) {
        return this.client.put(url, data, conf)
            .then(response => Promise.resolve(response))
            .catch(error => Promise.reject(error));
    }

    patch(url: string, data = {}, conf = {}) {
        return this.client.patch(url, data, conf)
            .then(response => Promise.resolve(response))
            .catch(error => Promise.reject(error));
    }
}

export { owsApiClient };

/**
 * Base HTTP Client
 */
export default {
    // Provide request methods with the default base_url
    get(url: string, conf = {}) {
        return getClient().get(url, conf)
            .then(response => Promise.resolve(response))
            .catch(error => Promise.reject(error));
    },

    delete(url: string, conf = {}) {
        return getClient().delete(url, conf)
            .then(response => Promise.resolve(response))
            .catch(error => Promise.reject(error));
    },

    head(url: string, conf = {}) {
        return getClient().head(url, conf)
            .then(response => Promise.resolve(response))
            .catch(error => Promise.reject(error));
    },

    options(url: string, conf = {}) {
        return getClient().options(url, conf)
            .then(response => Promise.resolve(response))
            .catch(error => Promise.reject(error));
    },

    post(url: string, data = {}, conf = {}) {
        return getClient().post(url, data, conf)
            .then(response => Promise.resolve(response))
            .catch(error => Promise.reject(error));
    },

    put(url: string, data = {}, conf = {}) {
        return getClient().put(url, data, conf)
            .then(response => Promise.resolve(response))
            .catch(error => Promise.reject(error));
    },

    patch(url: string, data = {}, conf = {}) {
        return getClient().patch(url, data, conf)
            .then(response => Promise.resolve(response))
            .catch(error => Promise.reject(error));
    },
};