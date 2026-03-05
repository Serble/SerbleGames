import { ref, computed } from 'vue';

// props
const username = ref('');
const token = ref(localStorage.getItem('backend_token'));
const isAuthenticated = computed(() => !!token.value);

const fetchUser = async () => {
    if (!isAuthenticated.value) return;
    try {
        const response = await client.get('/account');
        username.value = response.data.username;
        // Push the confirmed-valid token to the main process so the GMS can use it.
        if (isElectron()) window.electronAPI.setAuthContext(token.value, API_BASE);
    } catch (e) {
        if (e.response?.status === 401) {
            logout();
        }
    }
};

export function useAuth() {
    return { username, token, isAuthenticated, fetchUser };
}
