
const API_URL = "https://misatracker-api.onrender.com/api/push";
const TIMEOUT_MS = 10000;

function getToken() {
    const keys = [
        "misaToken",
        "token",
        "jwt",
        "authToken",
        "accessToken"
    ];

    for (const key of keys) {
        const value = localStorage.getItem(key);

        if (value && value !== "null" && value !== "undefined") {
            return value;
        }
    }

    return null;
}

function withTimeout(promise, message, timeout = TIMEOUT_MS) {
    let timer;

    return Promise.race([
        promise,
        new Promise((_, reject) => {
            timer = setTimeout(
                () => reject(new Error(message)),
                timeout
            );
        })
    ]).finally(() => clearTimeout(timer));
}

async function fetchWithTimeout(url, options = {}) {
    return withTimeout(
        fetch(url, options),
        `Сервер не ответил за ${TIMEOUT_MS / 1000} секунд: ${url}`
    );
}

export async function enablePushNotifications() {
    if (!("serviceWorker" in navigator)) {
        throw new Error("Браузер не поддерживает Service Worker.");
    }

    if (!("PushManager" in window)) {
        throw new Error("Браузер не поддерживает push-уведомления.");
    }

    if (!("Notification" in window)) {
        throw new Error("Браузер не поддерживает уведомления.");
    }

    const token = getToken();

    if (!token) {
        throw new Error(
            "Токен не найден. Выйди из аккаунта и войди снова."
        );
    }

    if (!window.isSecureContext) {
        throw new Error(
            "Push работает через HTTPS или localhost."
        );
    }

    let permission = Notification.permission;

    if (permission === "default") {
        permission = await Notification.requestPermission();
    }

    if (permission !== "granted") {
        throw new Error(
            "Разреши уведомления в настройках браузера."
        );
    }

    let registration;

    try {
        registration = await withTimeout(
            navigator.serviceWorker.ready,
            "Service Worker не запустился. Проверь регистрацию PWA."
        );
    } catch (error) {
        throw new Error(`Ошибка Service Worker: ${error.message}`,
            { cause: error }
        );
        
    }

    let keyResponse;

    try {
        keyResponse = await fetchWithTimeout(
            `${API_URL}/public-key`
        );
    } catch (error) {
        throw new Error(
            `Не удалось получить VAPID-ключ. Проверь, запущен ли backend. ${error.message}`,
            { cause: error }

        );
    }

    if (!keyResponse.ok) {
        throw new Error(
            `Backend не отдал VAPID-ключ. HTTP ${keyResponse.status}`
        );
    }

    const keyData = await keyResponse.json();
    const publicKey = keyData.publicKey;

    if (!publicKey) {
        throw new Error("Backend вернул пустой VAPID-ключ.");
    }

    let subscription = await registration.pushManager.getSubscription();

    if (!subscription) {
        try {
            subscription = await withTimeout(
                registration.pushManager.subscribe({
                    userVisibleOnly: true,
                    applicationServerKey: urlBase64ToUint8Array(publicKey)
                }),
                "Браузер не завершил создание push-подписки."
            );
        } catch (error) {
            throw new Error(
                `Не удалось создать push-подписку: ${error.message}`, { cause: error }

            );
        }
    }

    const subscriptionJson = subscription.toJSON();

    if (
        !subscriptionJson.endpoint ||
        !subscriptionJson.keys?.p256dh ||
        !subscriptionJson.keys?.auth
    ) {
        throw new Error("Браузер вернул неполные данные подписки.");
    }

    let response;

    try {
        response = await fetchWithTimeout(`${API_URL}/subscribe`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                Authorization: `Bearer ${token}`
            },
            body: JSON.stringify({
                endpoint: subscriptionJson.endpoint,
                p256dh: subscriptionJson.keys.p256dh,
                auth: subscriptionJson.keys.auth
            })
        });
    } catch (error) {
        throw new Error(
            `Не удалось сохранить подписку на сервере. ${error.message}`, { cause: error }
        );
    }

    if (!response.ok) {
        const details = await response.text().catch(() => "");

        throw new Error(
            `Сервер отклонил подписку (HTTP ${response.status}). ${details}`
        );
    }

    return "Push-уведомления подключены! 🍓";
}

export async function disablePushNotifications() {
    const token = getToken();

    if (!token) {
        throw new Error("Токен не найден. Войди в аккаунт.");
    }

    const registration = await withTimeout(
        navigator.serviceWorker.ready,
        "Service Worker не запустился."
    );

    const subscription =
        await registration.pushManager.getSubscription();

    if (!subscription) {
        return "Активной push-подписки нет.";
    }

    const response = await fetchWithTimeout(
        `${API_URL}/unsubscribe`,
        {
            method: "DELETE",
            headers: {
                "Content-Type": "application/json",
                Authorization: `Bearer ${token}`
            },
            body: JSON.stringify({
                endpoint: subscription.endpoint
            })
        }
    );

    if (!response.ok) {
        throw new Error(
            `Не удалось удалить подписку. HTTP ${response.status}`
        );
    }

    await subscription.unsubscribe();

    return "Push-уведомления отключены.";
}

function urlBase64ToUint8Array(base64String) {
    const padding = "=".repeat(
        (4 - (base64String.length % 4)) % 4
    );

    const base64 = (base64String + padding)
        .replace(/-/g, "+")
        .replace(/_/g, "/");

    const rawData = window.atob(base64);
    const output = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; i++) {
        output[i] = rawData.charCodeAt(i);
    }

    return output;
}
