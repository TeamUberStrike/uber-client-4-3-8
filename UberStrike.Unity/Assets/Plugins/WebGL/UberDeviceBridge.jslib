var UberDeviceBridge = {
    // 0 = PC/other, 1 = iOS, 2 = Android (based on browser user agent).
    // Application.isMobilePlatform is unreliable in WebGL (touch-based, not UA),
    // so the device class is detected here instead.
    UberGetDeviceType: function() {
        var ua = navigator.userAgent.toLowerCase();
        if (/iphone|ipod|ipad/.test(ua)) return 1;
        if (navigator.maxTouchPoints > 1 && /macintosh/.test(ua)) return 1; // iPadOS desktop mode
        if (/android/.test(ua)) return 2;
        return 0;
    },

    // Safe-area inset in canvas pixels: 0 = left, 1 = right, 2 = top, 3 = bottom.
    // Screen.safeArea is not populated on WebGL, so the CSS env() values from the
    // viewport (viewport-fit=cover) are forwarded to the game instead. The result
    // uses the same pixel space as Screen.width/Screen.height so the mobile menu
    // insets match a native iOS/Android build.
    UberGetSafeAreaInset: function(side) {
        try {
            if (!document.body) return 0;

            var probe = document.getElementById("uber-safe-area-probe");
            if (!probe) {
                probe = document.createElement("div");
                probe.id = "uber-safe-area-probe";
                probe.style.cssText =
                    "position:fixed;left:0;top:0;width:0;height:0;visibility:hidden;pointer-events:none;" +
                    "padding:env(safe-area-inset-top,0px) env(safe-area-inset-right,0px) " +
                    "env(safe-area-inset-bottom,0px) env(safe-area-inset-left,0px);";
                document.body.appendChild(probe);
            }

            var style = window.getComputedStyle(probe);
            var cssPixels = 0;
            if (side === 0) cssPixels = parseFloat(style.paddingLeft) || 0;
            else if (side === 1) cssPixels = parseFloat(style.paddingRight) || 0;
            else if (side === 2) cssPixels = parseFloat(style.paddingTop) || 0;
            else cssPixels = parseFloat(style.paddingBottom) || 0;

            // Convert CSS pixels to the canvas drawing buffer pixels that Unity reports.
            var canvas = document.getElementById("unity-canvas");
            var scale = (canvas && canvas.clientWidth > 0)
                ? (canvas.width / canvas.clientWidth)
                : (window.devicePixelRatio || 1);

            return Math.round(cssPixels * scale);
        } catch (e) {
            return 0;
        }
    },

    // 1 = the physical notch is on the left in landscape, 0 = on the right,
    // -1 = unknown/portrait. iOS reports equal left/right safe-area insets for
    // landscape (notch plus rounded corners), so the menu needs the orientation
    // to pick the notch side, exactly like the native builds do.
    UberGetLandscapeNotchOnLeft: function() {
        try {
            var angle = (typeof window.orientation === "number")
                ? window.orientation
                : (window.screen && window.screen.orientation ? window.screen.orientation.angle : 0);

            if (angle === 90) return 1;
            if (angle === -90 || angle === 270) return 0;
            return -1;
        } catch (e) {
            return -1;
        }
    },


    // iOS: touchstart is no gesture, so unlock again on touchend/click (howler.js style).
    UberWebAudioSetup__deps: ['$WEBAudio'],
    UberWebAudioSetup: function() {
        try {
            if (navigator.audioSession) navigator.audioSession.type = "playback";
        } catch (e) {}

        var unlock = function(ev) {
            try {
                if (!WEBAudio || !WEBAudio.audioContext) return;
                var ctx = WEBAudio.audioContext;
                if (ctx.state !== "running" && ctx.state !== "closed") {
                    var p = ctx.resume();
                    var src = ctx.createBufferSource();
                    src.buffer = ctx.createBuffer(1, 1, 22050);
                    src.connect(ctx.destination);
                    src.start(0);
                }
                var cache = WEBAudio.audioCache;
                if (cache) {
                    while (cache.length < 20) cache.push(new Audio());
                    for (var i = 0; i < cache.length; i++) {
                        if (!cache[i].uberUnlocked) {
                            cache[i].autoplay = false;
                            cache[i].load();
                            cache[i].uberUnlocked = true;
                        }
                    }
                }
            } catch (e) {
            }
        };

        var events = ["touchend", "pointerup", "click", "keydown"];
        for (var i = 0; i < events.length; i++) {
            window.addEventListener(events[i], unlock, true);
        }
        document.addEventListener("visibilitychange", function(ev) {
            if (!document.hidden) unlock(ev);
        });
    },

    // Open a URL in a new browser tab. Application.OpenURL is unreliable on
    // WebGL (often swallowed by popup blockers). window.open within the click
    // gesture normally works. If it is blocked (iOS Safari blocks it because
    // Unity IMGUI callbacks run outside the browser's user-activation window),
    // show a real link overlay so the player's tap is a genuine user gesture.
    UberOpenUrl: function(url) {
        var target = UTF8ToString(url);
        try {
            var win = window.open(target, "_blank");
            if (win) return;
        } catch (e) {
        }

        var existing = document.getElementById("uber-open-url-overlay");
        if (existing) existing.parentNode.removeChild(existing);

        var overlay = document.createElement("div");
        overlay.id = "uber-open-url-overlay";
        overlay.style.cssText = "position:fixed;inset:0;background:rgba(0,0,0,0.85);z-index:999999;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:16px;font-family:sans-serif;";

        var msg = document.createElement("div");
        msg.textContent = "Tap below to open the payment page";
        msg.style.cssText = "color:#fff;font-size:16px;text-align:center;padding:0 24px;";

        var link = document.createElement("a");
        link.href = target;
        link.target = "_blank";
        link.rel = "noopener noreferrer";
        link.textContent = "BUY NOW";
        link.style.cssText = "display:inline-block;background:#1a73e8;color:#fff;font-size:18px;font-weight:bold;padding:14px 28px;border-radius:8px;text-decoration:none;";
        link.addEventListener("click", function() {
            if (overlay.parentNode) overlay.parentNode.removeChild(overlay);
        });

        var cancel = document.createElement("button");
        cancel.textContent = "Cancel";
        cancel.style.cssText = "background:transparent;border:1px solid #888;color:#ccc;font-size:14px;padding:8px 18px;border-radius:8px;";
        cancel.onclick = function() { if (overlay.parentNode) overlay.parentNode.removeChild(overlay); };

        overlay.appendChild(msg);
        overlay.appendChild(link);
        overlay.appendChild(cancel);
        document.body.appendChild(overlay);

    },

    // Mobile WebGL keyboard. TouchScreenKeyboard and Unity's hidden HTML input
    // are unreliable on mobile browsers, so the game opens this DOM overlay instead.
    // It is used for the in-game chat, the login screen and any other text field.
    // There is deliberately no cancel button: the only visible action is Send (sending
    // an empty message just closes the overlay). The C# caller passes the GameObject
    // and method that receive the text; password fields pass masked=1 so the text is
    // shown as bullets.
    UberOpenChatKeyboard: function(initialTextPtr, gameObjectNamePtr) {
        UberDeviceBridge.UberOpenTextKeyboard(initialTextPtr, gameObjectNamePtr, 0, 0);
    },

    UberOpenTextKeyboard: function(initialTextPtr, gameObjectNamePtr, methodNamePtr, maskedPtr) {
        try {
            var initial = initialTextPtr ? UTF8ToString(initialTextPtr) : "";
            var gameObjectName = gameObjectNamePtr ? UTF8ToString(gameObjectNamePtr) : "TouchInputManager";
            var methodName = methodNamePtr ? UTF8ToString(methodNamePtr) : "OnWebGLChatSend";
            var masked = !!maskedPtr;

            if (!window.uberChatKeyboard) {
                window.uberChatKeyboard = (function() {
                    var MAX_LENGTH = 140;
                    var text = "";
                    var shift = false;
                    var symbols = false;
                    var keyboardLayout = "qwerty";
                    var layoutDetected = false;
                    var manualLayout = false;
                    var root = null;
                    var display = null;
                    var keysHost = null;
                    var targetObject = "TouchInputManager";
                    var sendMethod = "OnWebGLChatSend";
                    var maskedMode = false;

                    var LAYOUTS = {
                        qwerty: [
                            ["q", "w", "e", "r", "t", "y", "u", "i", "o", "p"],
                            ["a", "s", "d", "f", "g", "h", "j", "k", "l"],
                            ["\u21E7", "z", "x", "c", "v", "b", "n", "m", "\u232B"]
                        ],
                        azerty: [
                            ["a", "z", "e", "r", "t", "y", "u", "i", "o", "p"],
                            ["q", "s", "d", "f", "g", "h", "j", "k", "l", "m"],
                            ["\u21E7", "w", "x", "c", "v", "b", "n", "\u232B"]
                        ],
                        qwertz: [
                            ["q", "w", "e", "r", "t", "z", "u", "i", "o", "p"],
                            ["a", "s", "d", "f", "g", "h", "j", "k", "l"],
                            ["\u21E7", "y", "x", "c", "v", "b", "n", "m", "\u232B"]
                        ]
                    };

                    function fire(ev) {
                        if (ev) {
                            ev.preventDefault();
                            ev.stopPropagation();
                        }
                    }

                    function bindPress(button, onPress) {
                        var maxTimer = null;
                        var releaseTimer = null;
                        var pressedAt = 0;

                        function setPressed(pressed) {
                            button.style.filter = pressed ? "brightness(1.8)" : "";
                            button.style.transform = pressed ? "scale(0.92)" : "";
                            button.style.boxShadow = pressed ? "inset 0 0 0 2px rgba(255,255,255,0.7)" : "";
                        }

                        function clearMaxTimer() {
                            if (maxTimer) {
                                clearTimeout(maxTimer);
                                maxTimer = null;
                            }
                        }

                        function release() {
                            clearMaxTimer();
                            var elapsed = Date.now() - pressedAt;
                            var delay = Math.max(0, 80 - elapsed);
                            if (releaseTimer) clearTimeout(releaseTimer);
                            releaseTimer = setTimeout(function() {
                                setPressed(false);
                            }, delay);
                        }

                        var handler = function(ev) {
                            fire(ev);
                            pressedAt = Date.now();
                            setPressed(true);
                            clearMaxTimer();
                            maxTimer = setTimeout(release, 1200);
                            onPress();
                        };

                        if (window.PointerEvent) {
                            button.addEventListener("pointerdown", handler, false);
                            button.addEventListener("pointerup", release, false);
                            button.addEventListener("pointercancel", release, false);
                        } else {
                            button.addEventListener("touchstart", handler, false);
                            button.addEventListener("touchend", release, false);
                            button.addEventListener("touchcancel", release, false);
                            button.addEventListener("mousedown", handler, false);
                            button.addEventListener("mouseup", release, false);
                        }
                    }

                    function makeButton(label, flex, onPress, height) {
                        var button = document.createElement("button");
                        button.type = "button";
                        button.textContent = label;
                        button.style.cssText =
                            "flex:" + flex + ";min-width:0;height:" + (height || "clamp(34px,10vh,42px)") +
                            ";margin:0;padding:0;outline:none;border:0;border-radius:7px;background:#333;color:#fff;" +
                            "font:600 17px/1 -apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;" +
                            "cursor:pointer;touch-action:manipulation;-webkit-touch-callout:none;" +
                            "-webkit-tap-highlight-color:transparent;transition:transform 50ms ease,filter 50ms ease;";
                        bindPress(button, onPress);
                        return button;
                    }

                    function makeRow(keys) {
                        var row = document.createElement("div");
                        row.style.cssText = "display:flex;gap:4px;margin-top:clamp(3px,0.8vh,6px);";
                        for (var i = 0; i < keys.length; i++) {
                            row.appendChild(keys[i]);
                        }
                        return row;
                    }

                    function detectKeyboardLayout() {
                        if (layoutDetected) return;
                        layoutDetected = true;

                        try {
                            if (navigator.keyboard && navigator.keyboard.getLayoutMap) {
                                navigator.keyboard.getLayoutMap().then(function(map) {
                                    var layout = null;
                                    try {
                                        var q = (map.get("KeyQ") || "").toLowerCase();
                                        var y = (map.get("KeyY") || "").toLowerCase();
                                        if (q === "a") layout = "azerty";
                                        else if (y === "z") layout = "qwertz";
                                        else if (q === "q") layout = "qwerty";
                                    } catch (e) {
                                    }
                                    if (layout && !manualLayout && LAYOUTS[layout]) {
                                        keyboardLayout = layout;
                                        renderKeys();
                                    }
                                });
                                return;
                            }
                        } catch (e) {
                        }

                        var lang = "en";
                        try {
                            lang = (navigator.language || navigator.userLanguage || "en").toLowerCase();
                        } catch (e) {
                        }

                        var layout = "qwerty";
                        if (lang.indexOf("fr") === 0 || lang.indexOf("be") === 0) layout = "azerty";
                        else if (lang.indexOf("de") === 0 || lang.indexOf("hu") === 0 ||
                            lang.indexOf("cs") === 0 || lang.indexOf("sk") === 0 ||
                            lang.indexOf("ro") === 0 || lang.indexOf("ch") === 0) layout = "qwertz";
                        if (LAYOUTS[layout]) keyboardLayout = layout;
                    }

                    function setLayout(id) {
                        manualLayout = true;
                        keyboardLayout = id;
                        shift = false;
                        symbols = false;
                        renderKeys();
                    }

                    function makeLayoutButton(label, id) {
                        var button = makeButton(label, 1, function() {
                            setLayout(id);
                        }, "clamp(24px,4.4vh,28px)");
                        button.style.fontSize = "11px";
                        button.style.fontWeight = "700";
                        button.style.letterSpacing = "0.4px";
                        if (keyboardLayout === id) button.style.background = "#1a73e8";
                        return button;
                    }

                    function addChar(ch) {
                        if (text.length >= MAX_LENGTH) return;
                        text += ch;
                        updateDisplay();
                    }

                    function makeCharKey(ch) {
                        return makeButton(ch, 1, function() {
                            addChar(ch);
                        });
                    }

                    function makeLetterKey(ch) {
                        return makeButton(shift ? ch.toUpperCase() : ch, 1, function() {
                            addChar(shift ? ch.toUpperCase() : ch);
                            if (shift) {
                                shift = false;
                                renderKeys();
                            }
                        });
                    }

                    function updateDisplay() {
                        if (!display) return;
                        var shown = text;
                        if (maskedMode && text.length) {
                            shown = new Array(text.length + 1).join("\u2022");
                        }
                        display.textContent = text.length ? shown : "Type a message...";
                        display.style.color = text.length ? "#fff" : "#777";
                        display.scrollTop = display.scrollHeight;
                    }

                    function renderKeys() {
                        if (!keysHost) return;
                        while (keysHost.firstChild) keysHost.removeChild(keysHost.firstChild);

                        var layoutRow = [];
                        var layoutOptions = [["QWERTY", "qwerty"], ["AZERTY", "azerty"], ["QWERTZ", "qwertz"]];
                        for (var li = 0; li < layoutOptions.length; li++) {
                            layoutRow.push(makeLayoutButton(layoutOptions[li][0], layoutOptions[li][1]));
                        }
                        keysHost.appendChild(makeRow(layoutRow));

                        var rows;
                        if (symbols) {
                            rows = [
                                ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0"],
                                ["-", "/", ":", ";", "(", ")", "$", "&", "@", "\""],
                                [".", ",", "?", "!", "'", "\"", "\u232B"]
                            ];
                        } else {
                            rows = LAYOUTS[keyboardLayout] || LAYOUTS.qwerty;
                        }

                        for (var r = 0; r < rows.length; r++) {
                            var rowKeys = [];
                            for (var c = 0; c < rows[r].length; c++) {
                                var label = rows[r][c];
                                var key;
                                if (label === "\u21E7") {
                                    key = makeButton(label, 1.5, function() {
                                        shift = !shift;
                                        renderKeys();
                                    });
                                    if (shift) key.style.background = "#1a73e8";
                                } else if (label === "\u232B") {
                                    key = makeButton(label, 1.5, function() {
                                        if (text.length) {
                                            text = text.slice(0, -1);
                                            updateDisplay();
                                        }
                                    });
                                } else if (/^[a-z]$/.test(label)) {
                                    key = makeLetterKey(label);
                                } else {
                                    key = makeCharKey(label);
                                }
                                rowKeys.push(key);
                            }
                            keysHost.appendChild(makeRow(rowKeys));
                        }

                        var bottom = [];
                        if (symbols) {
                            bottom.push(makeButton("ABC", 1.5, function() {
                                symbols = false;
                                shift = false;
                                renderKeys();
                            }));
                            bottom.push(makeButton("SPACE", 5, function() {
                                addChar(" ");
                            }));
                        } else {
                            bottom.push(makeButton("123", 1.5, function() {
                                symbols = true;
                                shift = false;
                                renderKeys();
                            }));
                            bottom.push(makeCharKey(","));
                            bottom.push(makeButton("SPACE", 4, function() {
                                addChar(" ");
                            }));
                            bottom.push(makeCharKey("."));
                            bottom.push(makeCharKey("?"));
                        }
                        keysHost.appendChild(makeRow(bottom));
                    }

                    function hide() {
                        if (root) root.style.display = "none";
                        text = "";
                    }

                    function send() {
                        var message = text;
                        try {
                            SendMessage(targetObject, sendMethod, message);
                        } catch (e) {
                        }
                    }

                    function build() {
                        root = document.createElement("div");
                        root.id = "uber-chat-keyboard";
                        root.style.cssText =
                            "position:fixed;top:0;left:0;right:0;bottom:0;z-index:999;display:none;" +
                            "flex-direction:column;align-items:center;justify-content:flex-end;" +
                            "background:rgba(0,0,0,0.55);" +
                            "font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;" +
                            "touch-action:manipulation;user-select:none;-webkit-user-select:none;" +
                            "-webkit-touch-callout:none;-webkit-tap-highlight-color:transparent;";

                        var panel = document.createElement("div");
                        panel.style.cssText =
                            "width:100%;max-width:900px;background:#161616;border-radius:16px 16px 0 0;" +
                            "padding:6px calc(10px + env(safe-area-inset-right,0px)) " +
                            "calc(6px + env(safe-area-inset-bottom,0px)) " +
                            "calc(10px + env(safe-area-inset-left,0px));box-sizing:border-box;" +
                            "box-shadow:0 -8px 30px rgba(0,0,0,0.5);";

                        display = document.createElement("div");
                        display.style.cssText =
                            "flex:1 1 auto;min-height:0;width:100%;max-width:900px;box-sizing:border-box;" +
                            "overflow-y:auto;background:rgba(11,11,11,0.92);border-radius:12px 12px 0 0;" +
                            "padding:10px 12px;padding-top:calc(10px + env(safe-area-inset-top,0px));" +
                            "margin:0;color:#fff;font-size:18px;line-height:1.3;" +
                            "word-wrap:break-word;white-space:pre-wrap;text-align:left;";

                        keysHost = document.createElement("div");

                        var sendButton = document.createElement("button");
                        sendButton.type = "button";
                        sendButton.textContent = "SEND";
                        sendButton.style.cssText =
                            "display:block;width:100%;height:clamp(46px,13vh,56px);margin-top:6px;border:0;border-radius:12px;" +
                            "background:#1a73e8;color:#fff;outline:none;cursor:pointer;" +
                            "font:800 22px/1 -apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;" +
                            "letter-spacing:1px;touch-action:manipulation;-webkit-touch-callout:none;" +
                            "-webkit-tap-highlight-color:transparent;transition:transform 50ms ease,filter 50ms ease;";

                        bindPress(sendButton, send);

                        panel.appendChild(keysHost);
                        panel.appendChild(sendButton);
                        root.appendChild(display);
                        root.appendChild(panel);
                        document.body.appendChild(root);
                    }

                    return {
                        show: function(initialText, targetName, methodName, masked) {
                            if (!root) build();
                            detectKeyboardLayout();
                            targetObject = targetName || "TouchInputManager";
                            sendMethod = methodName || "OnWebGLChatSend";
                            maskedMode = !!masked;
                            text = initialText || "";
                            if (text.length > MAX_LENGTH) text = text.slice(0, MAX_LENGTH);
                            shift = false;
                            symbols = false;
                            updateDisplay();
                            renderKeys();
                            root.style.display = "flex";
                        },
                        hide: hide
                    };
                })();
            }

            window.uberChatKeyboard.show(initial, gameObjectName, methodName, masked);
        } catch (e) {
        }
    },

    UberCloseChatKeyboard: function() {
        try {
            if (window.uberChatKeyboard) window.uberChatKeyboard.hide();
        } catch (e) {
        }
    },
};

mergeInto(LibraryManager.library, UberDeviceBridge);
