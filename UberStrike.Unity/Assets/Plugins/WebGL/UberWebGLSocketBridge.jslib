/**
 * Vendored + patched Nakama WebGL socket bridge.
 * Names are prefixed (UberNK*) so they don't collide with the package's
 * UnityWebGLSocketBridge.jslib symbols. Adds a beforeunload close-all so a page
 * reload cleanly closes every open socket instead of leaving a stale socket on
 * the Nakama node (which its single-socket policy then rejects for minutes).
 */

var UberWebGLSocketBridge = {
    $UBER_BRIDGE_NAME: {},
    $UBER_OPEN_METHOD_NAME: {},
    $UBER_CLOSE_METHOD_NAME: {},
    $UBER_MESSAGE_METHOD_NAME: {},
    $UBER_ERROR_METHOD_NAME: {},
    $UBER_SOCKET_REF_MAP: {},

    $UBER_INITIALIZE: function() {
        UBER_SOCKET_REF_MAP = new Map();
        UBER_BRIDGE_NAME = "[Nakama]";
        UBER_OPEN_METHOD_NAME = "NKBridgeOnOpen";
        UBER_CLOSE_METHOD_NAME = "NKBridgeOnClose";
        UBER_MESSAGE_METHOD_NAME = "NKBridgeOnMessage";
        UBER_ERROR_METHOD_NAME = "NKBridgeOnError";

        window.addEventListener('beforeunload', function() {
            try {
                UBER_SOCKET_REF_MAP.forEach(function(ws) {
                    try { ws.onclose = null; ws.onerror = null; ws.close(); } catch (e) {}
                });
                UBER_SOCKET_REF_MAP.clear();
            } catch (e) {}
        });
    },

    UberNKCreateSocket: function(socketRef, addressPtr) {
        if (!(UBER_SOCKET_REF_MAP instanceof Map)) {
            UBER_INITIALIZE();
        }

        if (UBER_SOCKET_REF_MAP.has(socketRef)) {
            SendMessage(UBER_BRIDGE_NAME, UBER_ERROR_METHOD_NAME, socketRef + "_" + "A WebSocket already exists for this reference.");
            return;
        }

        var address = UTF8ToString(addressPtr);
        var ws = new WebSocket(address);
        ws.onmessage = function(e) {
            if (typeof e.data == 'string') {
                SendMessage(UBER_BRIDGE_NAME, UBER_MESSAGE_METHOD_NAME, socketRef + "_" + e.data);
            } else {
                SendMessage(UBER_BRIDGE_NAME, UBER_ERROR_METHOD_NAME, socketRef + "_" + "Received invalid data from remote.");
            }
        };
        ws.onopen = function(e) {
            SendMessage(UBER_BRIDGE_NAME, UBER_OPEN_METHOD_NAME, socketRef + "_");
        };
        ws.onclose = function(e) {
            UBER_SOCKET_REF_MAP.delete(socketRef);
            SendMessage(UBER_BRIDGE_NAME, UBER_CLOSE_METHOD_NAME, socketRef + "_" + e.code + "_" + e.reason);
        };
        ws.onerror = function(e) {
            UBER_SOCKET_REF_MAP.delete(socketRef);
            SendMessage(UBER_BRIDGE_NAME, UBER_ERROR_METHOD_NAME, socketRef + "_" + "WebSocket error");
        };
        UBER_SOCKET_REF_MAP.set(socketRef, ws);
    },

    UberNKSocketState: function (socketRef) {
        if (!(UBER_SOCKET_REF_MAP instanceof Map)) {
            UBER_INITIALIZE();
        }

        if (!UBER_SOCKET_REF_MAP.has(socketRef)) {
            SendMessage(UBER_BRIDGE_NAME, UBER_ERROR_METHOD_NAME, socketRef + "_" + "Did not find websocket with given reference.");
            return -1;
        }
        return UBER_SOCKET_REF_MAP.get(socketRef).readyState;
    },

    UberNKCloseSocket: function (socketRef) {
        if (!(UBER_SOCKET_REF_MAP instanceof Map)) {
            UBER_INITIALIZE();
        }

        if (!UBER_SOCKET_REF_MAP.has(socketRef)) {
            SendMessage(UBER_BRIDGE_NAME, UBER_ERROR_METHOD_NAME, socketRef + "_" + "Did not find websocket with given reference.");
        } else {
            UBER_SOCKET_REF_MAP.get(socketRef).close();
        }
    },

    UberNKSendData: function (socketRef, msg) {
        if (!(UBER_SOCKET_REF_MAP instanceof Map)) {
            UBER_INITIALIZE();
        }

        if (!UBER_SOCKET_REF_MAP.has(socketRef)) {
            SendMessage(UBER_BRIDGE_NAME, UBER_ERROR_METHOD_NAME, socketRef + "_" + "Did not find websocket with given reference.");
        } else {
            var data = UTF8ToString(msg);
            UBER_SOCKET_REF_MAP.get(socketRef).send(data);
        }
    },
};

// Auto add to depends
autoAddDeps(UberWebGLSocketBridge, '$UBER_INITIALIZE');
autoAddDeps(UberWebGLSocketBridge, '$UBER_SOCKET_REF_MAP');
autoAddDeps(UberWebGLSocketBridge, '$UBER_BRIDGE_NAME');
autoAddDeps(UberWebGLSocketBridge, '$UBER_OPEN_METHOD_NAME');
autoAddDeps(UberWebGLSocketBridge, '$UBER_CLOSE_METHOD_NAME');
autoAddDeps(UberWebGLSocketBridge, '$UBER_MESSAGE_METHOD_NAME');
autoAddDeps(UberWebGLSocketBridge, '$UBER_ERROR_METHOD_NAME');
mergeInto(LibraryManager.library, UberWebGLSocketBridge);
