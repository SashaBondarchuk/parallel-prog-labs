let socket = null;

// Get the WebSocket URL - use same host as webpage with /chat path (proxied by nginx)
const WS_PROTOCOL = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
const WS_HOST = window.location.hostname;
const WS_PORT = window.location.port ? `:${window.location.port}` : '';
const WS_URL = `${WS_PROTOCOL}//${WS_HOST}${WS_PORT}/chat`;

export function connect(user, callbacks) {
    if (!user) {
        callbacks.onError(new Error('Username is required to connect'));
        return;
    }

    try {
        socket = new WebSocket(`${WS_URL}?user=${user}`);
    } catch (error) {
        callbacks.onError(error);
        return;
    }

    socket.onopen = function() {
        callbacks.onOpen();
    };

    socket.onmessage = function(event) {
        try {
            const data = JSON.parse(event.data);
            callbacks.onMessage(data);
        } catch (error) {
            callbacks.onError(error);
        }
    };

    socket.onclose = function(event) {
        callbacks.onClose(event);
    };

    socket.onerror = function(error) {
        callbacks.onError(error);
    };
}

export function sendSystemMessage(message) {
    if (socket && socket.readyState === WebSocket.OPEN) {
        try {
            socket.send(JSON.stringify(message));
        } catch (error) {
            throw error;
        }
    } else {
        throw new Error('WebSocket is not connected');
    }
}

export function isSocketOpen() {
    return socket && socket.readyState === WebSocket.OPEN;
}
