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
    // The input box is a readonly textarea/password field so the browser provides
    // the caret, text selection and copy/paste. It is used for the in-game chat,
    // the login screen and any other text field.
    // There is deliberately no cancel button: the only visible action is Send (sending
    // an empty message just closes the overlay). The C# caller passes the GameObject
    // and method that receive the text; password fields pass masked=1 so the text is
    // shown as bullets.
    UberOpenTextKeyboard: function(initialTextPtr, gameObjectNamePtr, methodNamePtr, maskedPtr) {
        try {
            var initial = initialTextPtr ? UTF8ToString(initialTextPtr) : "";
            var gameObjectName = gameObjectNamePtr ? UTF8ToString(gameObjectNamePtr) : "TouchInputManager";
            var methodName = methodNamePtr ? UTF8ToString(methodNamePtr) : "OnWebGLChatSend";
            var masked = !!maskedPtr;

            if (!window.uberChatKeyboard) {
                window.uberChatKeyboard = (function() {
                    var MAX_LENGTH = 140;
                    var LONG_PRESS_MS = 450;
                    var LAYOUT_STORAGE_KEY = "uber.keyboardLayout";
                    var shift = false;
                    var symbols = false;
                    var layoutMenuOpen = false;
                    var keyboardLayout = "qwerty";
                    var layoutDetected = false;
                    var manualLayout = false;
                    var root = null;
                    var textInput = null;
                    var passInput = null;
                    var inputEl = null;
                    var keysHost = null;
                    var menuBackdrop = null;
                    var panelEl = null;
                    var sendButtonEl = null;
                    var targetObject = "TouchInputManager";
                    var sendMethod = "OnWebGLChatSend";
                    var maskedMode = false;
                    var transparentMode = false;

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
                        ],
                        dvorak: [
                            ["'", ",", ".", "p", "y", "f", "g", "c", "r", "l"],
                            ["a", "o", "e", "u", "i", "d", "h", "t", "n", "s"],
                            ["\u21E7", ";", "q", "j", "k", "x", "b", "m", "w", "v", "z", "\u232B"]
                        ],
                        colemak: [
                            ["q", "w", "f", "p", "g", "j", "l", "u", "y", ";"],
                            ["a", "r", "s", "t", "d", "h", "n", "e", "i", "o"],
                            ["\u21E7", "z", "x", "c", "v", "b", "k", "m", "\u232B"]
                        ]
                    };

                    var LAYOUT_LABELS = {
                        qwerty: "QWERTY",
                        azerty: "AZERTY",
                        qwertz: "QWERTZ",
                        dvorak: "DVORAK",
                        colemak: "COLEMAK"
                    };

                    try {
                        var savedLayout = window.localStorage.getItem(LAYOUT_STORAGE_KEY);
                        if (savedLayout && LAYOUTS[savedLayout]) {
                            keyboardLayout = savedLayout;
                            manualLayout = true;
                        }
                    } catch (e) {
                    }

                    function fire(ev) {
                        if (ev) {
                            ev.preventDefault();
                            ev.stopPropagation();
                        }
                    }

                    function bindPress(button, onPress, repeat, onLongPress) {
                        var releaseTimer = null;
                        var repeatDelayTimer = null;
                        var repeatInterval = null;
                        var repeatCount = 0;
                        var pressedAt = 0;
                        var activePointerId = null;
                        var activeTouchId = null;
                        var mouseActive = false;
                        var ignoreMouseUntil = 0;
                        var listening = false;
                        var holdTimer = null;
                        var longPressFired = false;

                        function setPressed(pressed) {
                            button.style.filter = pressed ? "brightness(1.8)" : "";
                            button.style.transform = pressed ? "scale(0.92)" : "";
                            button.style.boxShadow = pressed ? "inset 0 0 0 2px rgba(255,255,255,0.7)" : "";
                        }

                        function stopRepeat() {
                            if (repeatDelayTimer) {
                                clearTimeout(repeatDelayTimer);
                                repeatDelayTimer = null;
                            }
                            if (repeatInterval) {
                                clearInterval(repeatInterval);
                                repeatInterval = null;
                            }
                            repeatCount = 0;
                        }

                        function clearHold() {
                            if (holdTimer) {
                                clearTimeout(holdTimer);
                                holdTimer = null;
                            }
                        }

                        function isInside(x, y) {
                            if (x === undefined || x === null || y === undefined || y === null) return false;
                            var rect = button.getBoundingClientRect();
                            return x >= rect.left && x <= rect.right && y >= rect.top && y <= rect.bottom;
                        }

                        function findTouch(list, id) {
                            if (!list) return null;
                            for (var i = 0; i < list.length; i++) {
                                if (list[i].identifier === id) return list[i];
                            }
                            return null;
                        }

                        function unpress() {
                            var elapsed = Date.now() - pressedAt;
                            var delay = Math.max(0, 80 - elapsed);
                            if (releaseTimer) clearTimeout(releaseTimer);
                            releaseTimer = setTimeout(function() {
                                releaseTimer = null;
                                setPressed(false);
                            }, delay);
                        }

                        function detachWindowListeners() {
                            if (!listening) return;
                            listening = false;
                            window.removeEventListener("pointerup", onWindowPointerUp, true);
                            window.removeEventListener("pointercancel", onWindowPointerCancel, true);
                            window.removeEventListener("touchend", onWindowTouchEnd, true);
                            window.removeEventListener("touchcancel", onWindowTouchCancel, true);
                            window.removeEventListener("mouseup", onWindowMouseUp, true);
                            window.removeEventListener("blur", onWindowBlur, true);
                            document.removeEventListener("visibilitychange", onVisibilityChange, true);
                        }

                        function finish(commit) {
                            detachWindowListeners();
                            stopRepeat();
                            clearHold();
                            activePointerId = null;
                            activeTouchId = null;
                            mouseActive = false;
                            if (!repeat && commit && !longPressFired) onPress();
                            longPressFired = false;
                            unpress();
                        }

                        function onWindowPointerUp(ev) {
                            if (activePointerId === null || ev.pointerId !== activePointerId) return;
                            finish(isInside(ev.clientX, ev.clientY));
                        }

                        function onWindowPointerCancel(ev) {
                            if (activePointerId === null || ev.pointerId !== activePointerId) return;
                            finish(false);
                        }

                        function onWindowTouchEnd(ev) {
                            if (activeTouchId === null) return;
                            var touch = findTouch(ev.changedTouches, activeTouchId);
                            if (!touch) return;
                            finish(isInside(touch.clientX, touch.clientY));
                        }

                        function onWindowTouchCancel(ev) {
                            if (activeTouchId === null) return;
                            if (!findTouch(ev.changedTouches, activeTouchId)) return;
                            finish(false);
                        }

                        function onWindowMouseUp(ev) {
                            if (!mouseActive || ev.button !== 0) return;
                            finish(isInside(ev.clientX, ev.clientY));
                        }

                        function onWindowBlur() {
                            finish(false);
                        }

                        function onVisibilityChange() {
                            if (document.hidden) finish(false);
                        }

                        function begin() {
                            pressedAt = Date.now();
                            if (releaseTimer) {
                                clearTimeout(releaseTimer);
                                releaseTimer = null;
                            }
                            setPressed(true);

                            longPressFired = false;
                            clearHold();
                            if (onLongPress) {
                                holdTimer = setTimeout(function() {
                                    holdTimer = null;
                                    longPressFired = true;
                                    onLongPress();
                                }, LONG_PRESS_MS);
                            }

                            if (!listening) {
                                listening = true;
                                if (window.PointerEvent) {
                                    window.addEventListener("pointerup", onWindowPointerUp, true);
                                    window.addEventListener("pointercancel", onWindowPointerCancel, true);
                                } else {
                                    window.addEventListener("touchend", onWindowTouchEnd, true);
                                    window.addEventListener("touchcancel", onWindowTouchCancel, true);
                                    window.addEventListener("mouseup", onWindowMouseUp, true);
                                }
                                window.addEventListener("blur", onWindowBlur, true);
                                document.addEventListener("visibilitychange", onVisibilityChange, true);
                            }

                            if (repeat) {
                                stopRepeat();
                                onPress();
                                repeatDelayTimer = setTimeout(function() {
                                    repeatDelayTimer = null;
                                    repeatInterval = setInterval(function() {
                                        onPress();
                                        repeatCount++;
                                        if (repeatCount > 20) onPress();
                                    }, 50);
                                }, 400);
                            }
                        }

                        if (window.PointerEvent) {
                            button.addEventListener("pointerdown", function(ev) {
                                fire(ev);
                                if (activePointerId !== null) return;
                                activePointerId = ev.pointerId;
                                begin();
                            }, false);
                        } else {
                            button.addEventListener("touchstart", function(ev) {
                                fire(ev);
                                if (activeTouchId !== null) return;
                                var touch = ev.changedTouches && ev.changedTouches[0];
                                if (!touch) return;
                                activeTouchId = touch.identifier;
                                ignoreMouseUntil = Date.now() + 1000;
                                begin();
                            }, false);
                            button.addEventListener("mousedown", function(ev) {
                                if (ev.button !== 0 || mouseActive) return;
                                if (activeTouchId !== null || Date.now() < ignoreMouseUntil) return;
                                mouseActive = true;
                                fire(ev);
                                begin();
                            }, false);
                        }
                    }

                    function makeButton(label, flex, onPress, height, repeat, onLongPress) {
                        var button = document.createElement("button");
                        button.type = "button";
                        button.textContent = label;
                        button.style.cssText =
                            "flex:" + flex + ";min-width:0;height:" + (height || "clamp(34px,10vh,42px)") +
                            ";margin:0;padding:0;outline:none;border:" + (transparentMode ? "1px solid rgba(255,255,255,0.45)" : "0") + ";border-radius:7px;" +
                            "background:" + (transparentMode ? "rgba(51,51,51,0.35)" : "#333") + ";color:#fff;" +
                            "font:600 17px/1 -apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;" +
                            "cursor:pointer;touch-action:manipulation;-webkit-touch-callout:none;" +
                            "-webkit-tap-highlight-color:transparent;transition:transform 50ms ease,filter 50ms ease,box-shadow 50ms ease;";
                        bindPress(button, onPress, repeat, onLongPress);
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
                                        var w = (map.get("KeyW") || "").toLowerCase();
                                        var e = (map.get("KeyE") || "").toLowerCase();
                                        var r = (map.get("KeyR") || "").toLowerCase();
                                        var t = (map.get("KeyT") || "").toLowerCase();
                                        var y = (map.get("KeyY") || "").toLowerCase();
                                        if (q === "a") layout = "azerty";
                                        else if (y === "z") layout = "qwertz";
                                        else if (q === "'" && w === ",") layout = "dvorak";
                                        else if (e === "f" && r === "p" && t === "g") layout = "colemak";
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
                            var langs = navigator.languages && navigator.languages.length
                                ? navigator.languages
                                : [navigator.language || navigator.userLanguage || "en"];
                            lang = (langs[0] || "en").toLowerCase();
                        } catch (e) {
                        }

                        var layout = "qwerty";
                        if (lang.indexOf("fr-ca") === 0) layout = "qwerty";
                        else if (lang.indexOf("fr") === 0 || lang.indexOf("be") === 0 || lang.indexOf("lu") === 0) layout = "azerty";
                        else if (lang.indexOf("de") === 0 || lang.indexOf("at") === 0 ||
                            lang.indexOf("hu") === 0 || lang.indexOf("cs") === 0 ||
                            lang.indexOf("sk") === 0 || lang.indexOf("ro") === 0 ||
                            lang.indexOf("ch") === 0 || lang.indexOf("sl") === 0 ||
                            lang.indexOf("hr") === 0) layout = "qwertz";
                        if (!manualLayout && LAYOUTS[layout]) keyboardLayout = layout;
                    }

                    function persistLayout(id) {
                        try {
                            window.localStorage.setItem(LAYOUT_STORAGE_KEY, id);
                        } catch (e) {
                        }
                    }

                    function setLayout(id) {
                        manualLayout = true;
                        keyboardLayout = id;
                        shift = false;
                        symbols = false;
                        layoutMenuOpen = false;
                        persistLayout(id);
                        renderKeys();
                    }

                    function cycleLayout() {
                        var ids = [];
                        var id;
                        for (id in LAYOUTS) {
                            if (LAYOUTS.hasOwnProperty(id)) ids.push(id);
                        }
                        if (ids.length < 2) return;
                        var index = ids.indexOf(keyboardLayout);
                        setLayout(ids[(index + 1) % ids.length]);
                    }

                    function openLayoutPicker() {
                        layoutMenuOpen = true;
                        renderKeys();
                    }

                    function closeLayoutPicker() {
                        if (!layoutMenuOpen) return;
                        layoutMenuOpen = false;
                        renderKeys();
                    }

                    function makeMenuEntry(id) {
                        var label = LAYOUT_LABELS[id] || id.toUpperCase();
                        var active = keyboardLayout === id;
                        var button = makeButton((active ? "\u2713  " : "\u2003  ") + label, 0, function() {
                            setLayout(id);
                        }, "clamp(38px,7vh,48px)");
                        button.style.flex = "none";
                        button.style.width = "100%";
                        button.style.fontSize = "14px";
                        button.style.fontWeight = active ? "700" : "600";
                        button.style.letterSpacing = "0.2px";
                        button.style.textAlign = "left";
                        button.style.padding = "0 12px";
                        button.style.background = active ? (transparentMode ? "rgba(26,115,232,0.55)" : "#1a73e8") : (transparentMode ? "rgba(51,51,51,0.35)" : "#333");
                        button.setAttribute("role", "menuitemradio");
                        button.setAttribute("aria-checked", active ? "true" : "false");
                        return button;
                    }

                    function addChar(ch) {
                        if (!inputEl) return;

                        var start = inputEl.selectionStart;
                        var end = inputEl.selectionEnd;
                        if (start === null || start === undefined) {
                            start = end = inputEl.value.length;
                        }

                        if (inputEl.value.length - (end - start) + ch.length > MAX_LENGTH) return;

                        inputEl.value = inputEl.value.slice(0, start) + ch + inputEl.value.slice(end);
                        selectInput(start + ch.length, start + ch.length);
                        syncInputHeight();
                    }

                    function doBackspace() {
                        if (!inputEl) return;

                        var start = inputEl.selectionStart;
                        var end = inputEl.selectionEnd;
                        if (start === null || start === undefined) {
                            start = end = inputEl.value.length;
                        }

                        if (start !== end) {
                            inputEl.value = inputEl.value.slice(0, start) + inputEl.value.slice(end);
                            selectInput(start, start);
                        } else if (start > 0) {
                            inputEl.value = inputEl.value.slice(0, start - 1) + inputEl.value.slice(start);
                            selectInput(start - 1, start - 1);
                        }

                        syncInputHeight();
                    }

                    function makeCharKey(ch) {
                        return makeButton(ch, 1, function() {
                            addChar(ch);
                        });
                    }

                    function makeLetterKey(ch) {
                        var out = shift ? ch.toUpperCase() : ch;
                        return makeButton(out, 1, function() {
                            addChar(out);
                            if (shift) {
                                shift = false;
                                renderKeys();
                            }
                        });
                    }

                    function makeLayoutKey() {
                        var label = LAYOUT_LABELS[keyboardLayout] || keyboardLayout.toUpperCase();
                        var globe = makeButton("", 1.5, cycleLayout, null, null, openLayoutPicker);
                        globe.textContent = "";
                        globe.style.display = "flex";
                        globe.style.flexDirection = "column";
                        globe.style.alignItems = "center";
                        globe.style.justifyContent = "center";
                        globe.style.gap = "1px";
                        globe.style.lineHeight = "1";
                        globe.setAttribute("aria-label", "Keyboard layout: " + label + ". Tap to switch, hold for list.");
                        globe.setAttribute("aria-haspopup", "menu");
                        globe.setAttribute("aria-expanded", layoutMenuOpen ? "true" : "false");
                        globe.title = "Keyboard layout: " + label + " (tap to switch, hold for list)";

                        var glyph = document.createElement("span");
                        glyph.textContent = "\uD83C\uDF10";
                        glyph.style.fontSize = "15px";
                        glyph.style.lineHeight = "1";

                        var name = document.createElement("span");
                        name.textContent = label;
                        name.style.fontSize = "8px";
                        name.style.fontWeight = "700";
                        name.style.letterSpacing = "0.2px";
                        name.style.lineHeight = "1";
                        name.style.opacity = "0.85";

                        globe.appendChild(glyph);
                        globe.appendChild(name);
                        return globe;
                    }

                    function getText() {
                        return inputEl ? inputEl.value : "";
                    }

                    function syncInputHeight() {
                        if (!inputEl || inputEl === passInput) return;
                        inputEl.style.height = "auto";
                        inputEl.style.height = inputEl.scrollHeight + "px";
                    }

                    function selectInput(start, end) {
                        if (!inputEl) return;
                        inputEl.setSelectionRange(start, end);
                        try {
                            setTimeout(function() {
                                if (inputEl) inputEl.focus();
                            }, 0);
                        } catch (e) {
                        }
                    }

                    var focusTimer = null;

                    function applyTheme() {
                        var solidPanel = transparentMode ? "transparent" : "#161616";
                        var solidRoot = transparentMode ? "transparent" : "rgba(0,0,0,0.55)";
                        var solidInput = transparentMode ? "rgba(11,11,11,0.75)" : "#0b0b0b";

                        if (root) root.style.background = solidRoot;
                        if (panelEl) panelEl.style.background = solidPanel;
                        if (textInput) textInput.style.background = solidInput;
                        if (passInput) passInput.style.background = solidInput;
                        if (sendButtonEl) {
                            sendButtonEl.style.background = transparentMode ? "rgba(51,51,51,0.35)" : "#333";
                            sendButtonEl.style.border = transparentMode ? "1px solid rgba(255,255,255,0.45)" : "0";
                        }
                    }

                    function ensureFocus() {
                        if (!inputEl) return;
                        if (layoutMenuOpen) return;
                        try {
                            if (document.activeElement !== inputEl) {
                                inputEl.focus();
                            }
                        } catch (e) {
                        }
                    }

                    function clearFocusTimer() {
                        if (focusTimer) {
                            clearInterval(focusTimer);
                            focusTimer = null;
                        }
                    }

                    function renderKeys() {
                        if (!keysHost) return;
                        while (keysHost.firstChild) keysHost.removeChild(keysHost.firstChild);

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
                                if (label === "\u21E7" || label === "{shift}") {
                                    key = makeButton("\u21E7", 1.5, function() {
                                        shift = !shift;
                                        renderKeys();
                                    });
                                    if (shift) key.style.background = transparentMode ? "rgba(26,115,232,0.55)" : "#1a73e8";
                                } else if (label === "\u232B" || label === "{bksp}") {
                                    key = makeButton(label, 1.5, doBackspace, null, doBackspace);
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
                            bottom.push(makeLayoutKey());
                            bottom.push(makeButton("SPACE", 5, function() {
                                addChar(" ");
                            }, null, null, openLayoutPicker));
                        } else {
                            bottom.push(makeButton("123", 1.5, function() {
                                symbols = true;
                                shift = false;
                                renderKeys();
                            }));
                            bottom.push(makeCharKey(","));
                            bottom.push(makeLayoutKey());
                            bottom.push(makeButton("SPACE", 4, function() {
                                addChar(" ");
                            }, null, null, openLayoutPicker));
                            bottom.push(makeCharKey("."));
                            bottom.push(makeCharKey("?"));
                        }
                        keysHost.appendChild(makeRow(bottom));

                        if (menuBackdrop) {
                            menuBackdrop.style.display = layoutMenuOpen ? "block" : "none";
                        }

                        if (layoutMenuOpen) {
                            var scrim = document.createElement("div");
                            scrim.style.cssText =
                                "position:absolute;left:0;right:0;top:0;bottom:0;z-index:9;" +
                                "background:rgba(0,0,0,0.35);";
                            scrim.addEventListener("pointerdown", function(ev) {
                                fire(ev);
                                closeLayoutPicker();
                            }, false);
                            scrim.addEventListener("touchstart", function(ev) {
                                fire(ev);
                                closeLayoutPicker();
                            }, false);
                            keysHost.appendChild(scrim);

                            var menu = document.createElement("div");
                            menu.setAttribute("role", "menu");
                            menu.style.cssText =
                                "position:absolute;left:0;right:0;" +
                                "bottom:calc(clamp(34px,10vh,42px) + clamp(3px,0.8vh,6px) + 2px);" +
                                "z-index:10;max-height:clamp(160px,38vh,280px);overflow-y:auto;" +
                                "background:" + (transparentMode ? "rgba(26,26,26,0.92)" : "#1a1a1a") + ";border:1px solid #3a3a3a;border-radius:10px;padding:6px;" +
                                "box-shadow:0 8px 24px rgba(0,0,0,0.6);" +
                                "display:flex;flex-direction:column;gap:4px;";

                            var header = document.createElement("div");
                            header.textContent = "Keyboard layout";
                            header.style.cssText =
                                "font-size:11px;font-weight:700;letter-spacing:0.6px;text-transform:uppercase;" +
                                "color:#9aa0a6;padding:2px 8px 4px;";
                            menu.appendChild(header);

                            for (var layoutId in LAYOUTS) {
                                if (!LAYOUTS.hasOwnProperty(layoutId)) continue;
                                menu.appendChild(makeMenuEntry(layoutId));
                            }
                            keysHost.appendChild(menu);
                        }
                    }

                    function hide() {
                        if (root) root.style.display = "none";
                        if (inputEl) inputEl.value = "";
                        layoutMenuOpen = false;
                        if (menuBackdrop) menuBackdrop.style.display = "none";
                        clearFocusTimer();
                    }

                    function send() {
                        if (layoutMenuOpen) {
                            closeLayoutPicker();
                            return;
                        }

                        var message = getText();
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
                            "align-items:flex-end;justify-content:center;" +
                            "background:transparent;" +
                            "font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;" +
                            "touch-action:manipulation;user-select:none;-webkit-user-select:none;" +
                            "-webkit-touch-callout:none;-webkit-tap-highlight-color:transparent;";

                        panelEl = document.createElement("div");
                        panelEl.style.cssText =
                            "position:relative;z-index:9;width:100%;max-width:900px;" +
                            "background:transparent;" +
                            "padding:6px calc(10px + env(safe-area-inset-right,0px)) " +
                            "calc(6px + env(safe-area-inset-bottom,0px)) " +
                            "calc(10px + env(safe-area-inset-left,0px));box-sizing:border-box;";

                        var placeholderStyle = document.createElement("style");
                        placeholderStyle.textContent =
                            "#uber-chat-keyboard .uber-input::placeholder { color:#777; }" +
                            "#uber-chat-keyboard .uber-input::-webkit-input-placeholder { color:#777; }";
                        root.appendChild(placeholderStyle);

                        textInput = document.createElement("textarea");
                        textInput.className = "uber-input";
                        textInput.inputMode = "none";
                        textInput.placeholder = "Type a message...";
                        textInput.style.cssText =
                            "display:block;width:100%;box-sizing:border-box;resize:none;outline:none;" +
                            "min-height:clamp(34px,8vh,42px);max-height:clamp(96px,26vh,190px);overflow-y:auto;" +
                            "background:#0b0b0b;border:1px solid #3a3a3a;border-radius:10px;padding:6px 10px;" +
                            "margin-bottom:6px;color:#fff;caret-color:#fff;font-size:18px;line-height:1.25;" +
                            "word-wrap:break-word;white-space:pre-wrap;user-select:text;-webkit-user-select:text;" +
                            "touch-action:auto;-webkit-touch-callout:default;";

                        passInput = document.createElement("input");
                        passInput.className = "uber-input";
                        passInput.type = "password";
                        passInput.inputMode = "none";
                        passInput.placeholder = "Type a message...";
                        passInput.style.cssText =
                            "display:none;width:100%;box-sizing:border-box;outline:none;" +
                            "height:clamp(34px,8vh,42px);background:#0b0b0b;border:1px solid #3a3a3a;" +
                            "border-radius:10px;padding:6px 10px;margin-bottom:6px;color:#fff;caret-color:#fff;" +
                            "font-size:18px;line-height:1.25;user-select:text;-webkit-user-select:text;" +
                            "touch-action:auto;-webkit-touch-callout:default;";

                        keysHost = document.createElement("div");
                        keysHost.style.cssText = "position:relative;width:100%;";

                        menuBackdrop = document.createElement("div");
                        menuBackdrop.style.cssText = "position:fixed;top:0;left:0;right:0;bottom:0;z-index:8;display:none;";
                        var closeMenu = function() {
                            closeLayoutPicker();
                        };
                        menuBackdrop.addEventListener("pointerdown", closeMenu, false);
                        menuBackdrop.addEventListener("touchstart", closeMenu, false);
                        root.appendChild(menuBackdrop);

                        sendButtonEl = document.createElement("button");
                        sendButtonEl.type = "button";
                        sendButtonEl.textContent = "SEND";
                        sendButtonEl.style.cssText =
                            "display:block;width:100%;height:clamp(46px,13vh,56px);margin-top:6px;border:" + (transparentMode ? "1px solid rgba(255,255,255,0.45)" : "0") + ";border-radius:7px;" +
                            "background:" + (transparentMode ? "rgba(51,51,51,0.35)" : "#333") + ";color:#fff;outline:none;cursor:pointer;" +
                            "font:700 18px/1 -apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;" +
                            "letter-spacing:1px;touch-action:manipulation;-webkit-touch-callout:none;" +
                            "-webkit-tap-highlight-color:transparent;transition:transform 50ms ease,filter 50ms ease,box-shadow 50ms ease;";

                        bindPress(sendButtonEl, function() {
                            setTimeout(function() {
                                send();
                            }, 120);
                        });

                        var preventNativeKeyboard = function(ev) {
                            var el = ev && ev.target;
                            try {
                                if (el && el.setAttribute && el.removeAttribute) {
                                    el.setAttribute("readonly", "readonly");
                                    setTimeout(function() {
                                        el.removeAttribute("readonly");
                                    }, 0);
                                }
                            } catch (e) {
                            }
                        };
                        textInput.addEventListener("focus", preventNativeKeyboard, false);
                        passInput.addEventListener("focus", preventNativeKeyboard, false);

                        panelEl.appendChild(textInput);
                        panelEl.appendChild(passInput);
                        panelEl.appendChild(keysHost);
                        panelEl.appendChild(sendButtonEl);
                        root.appendChild(panelEl);
                        document.body.appendChild(root);
                    }

                    return {
                        show: function(initialText, targetName, methodName, masked) {
                            transparentMode = methodName === "OnWebGLChatSend";
                            if (!root) build();
                            detectKeyboardLayout();
                            var pending = window.__uberPendingLayouts;
                            if (pending) {
                                window.__uberPendingLayouts = null;
                                for (var pi = 0; pi < pending.length; pi++) {
                                    LAYOUTS[pending[pi].id] = pending[pi].rows;
                                    LAYOUT_LABELS[pending[pi].id] = pending[pi].label || pending[pi].id.toUpperCase();
                                    keyboardLayout = pending[pi].id;
                                }
                                manualLayout = true;
                                persistLayout(keyboardLayout);
                            }
                            targetObject = targetName || "TouchInputManager";
                            sendMethod = methodName || "OnWebGLChatSend";
                            maskedMode = !!masked;

                            if (maskedMode) {
                                textInput.value = "";
                                inputEl = passInput;
                                textInput.style.display = "none";
                                passInput.style.display = "block";
                            } else {
                                passInput.value = "";
                                inputEl = textInput;
                                passInput.style.display = "none";
                                textInput.style.display = "block";
                            }

                            inputEl.value = initialText || "";
                            if (inputEl.value.length > MAX_LENGTH) inputEl.value = inputEl.value.slice(0, MAX_LENGTH);

                            symbols = false;
                            shift = false;
                            layoutMenuOpen = false;
                            applyTheme();
                            renderKeys();
                            root.style.display = "flex";
                            syncInputHeight();

                            inputEl.setSelectionRange(inputEl.value.length, inputEl.value.length);
                            inputEl.focus();
                            try {
                                setTimeout(function() {
                                    if (inputEl) inputEl.focus();
                                }, 60);
                            } catch (e) {
                            }

                            clearFocusTimer();
                            try {
                                focusTimer = setInterval(ensureFocus, 700);
                            } catch (e) {
                            }
                        },
                        hide: hide,
                        addLayout: function(id, label, rows, force) {
                            LAYOUTS[id] = rows;
                            LAYOUT_LABELS[id] = label || id.toUpperCase();
                            if (force || !manualLayout) {
                                keyboardLayout = id;
                                manualLayout = true;
                                shift = false;
                                symbols = false;
                                layoutMenuOpen = false;
                                persistLayout(id);
                                if (root && root.style.display === "flex") renderKeys();
                            }
                        },
                        loadLayout: function(url, id, label) {
                            try {
                                fetch(url).then(function(resp) {
                                    if (!resp || !resp.ok) return;
                                    return resp.json();
                                }).then(function(rows) {
                                    if (rows && rows.length) {
                                        LAYOUTS[id] = rows;
                                        LAYOUT_LABELS[id] = label || id.toUpperCase();
                                        keyboardLayout = id;
                                        manualLayout = true;
                                        shift = false;
                                        symbols = false;
                                        layoutMenuOpen = false;
                                        persistLayout(id);
                                        if (root && root.style.display === "flex") renderKeys();
                                    }
                                }).catch(function() {
                                });
                            } catch (e) {
                            }
                        }
                    };
                })();
            }

            window.uberChatKeyboard.show(initial, gameObjectName, methodName, masked);
        } catch (e) {
            try {
                if (window.console && window.console.error) window.console.error("UberOpenTextKeyboard failed:", e);
            } catch (e2) {
            }
        }
    },

    UberAddKeyboardLayout: function(idPtr, labelPtr, rowsJsonPtr) {
        try {
            var id = idPtr ? UTF8ToString(idPtr) : "";
            var label = labelPtr ? UTF8ToString(labelPtr) : "";
            var rowsJson = rowsJsonPtr ? UTF8ToString(rowsJsonPtr) : "";
            if (!id || !rowsJson) return;
            var rows = JSON.parse(rowsJson);
            if (!rows || !rows.length) return;

            if (window.uberChatKeyboard) {
                window.uberChatKeyboard.addLayout(id, label, rows, true);
            } else {
                if (!window.__uberPendingLayouts) window.__uberPendingLayouts = [];
                window.__uberPendingLayouts.push({ id: id, label: label, rows: rows });
            }
        } catch (e) {
        }
    },

    UberLoadKeyboardLayout: function(urlPtr, idPtr, labelPtr) {
        try {
            var url = urlPtr ? UTF8ToString(urlPtr) : "";
            var id = idPtr ? UTF8ToString(idPtr) : "";
            var label = labelPtr ? UTF8ToString(labelPtr) : "";
            if (!url || !id) return;

            try {
                fetch(url).then(function(resp) {
                    if (!resp || !resp.ok) return;
                    return resp.json();
                }).then(function(rows) {
                    if (!rows || !rows.length) return;

                    if (window.uberChatKeyboard) {
                        window.uberChatKeyboard.addLayout(id, label, rows, false);
                    } else {
                        if (!window.__uberPendingLayouts) window.__uberPendingLayouts = [];
                        window.__uberPendingLayouts.push({ id: id, label: label, rows: rows });
                    }
                }).catch(function() {
                });
            } catch (e) {
            }
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
