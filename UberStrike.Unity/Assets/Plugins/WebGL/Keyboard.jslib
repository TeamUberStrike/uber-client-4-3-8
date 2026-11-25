// UberStrike mobile WebGL keyboard: a DOM overlay keyboard used by the game for
// chat, login and any other text input (TouchScreenKeyboard and Unity's hidden
// HTML input are unreliable on mobile browsers).
//
// - Top bar: circular X (cancel = sends an empty message) and OK (confirm/send)
//   around a textarea or password input (masked=1 shows bullets).
// - The caret is drawn by the keyboard itself (custom-caret), because native
//   carets stop rendering on iOS Safari after a few seconds.
// - Layout picker: globe key (tap cycles, hold opens the list) or long-press
//   space; selection persists in localStorage.
// - Key sounds: the real iOS / Android keyboard clicks, loaded per platform from
//   StreamingAssets/Audio/Keyboard/{ios,android}.
//
// The C# caller passes the GameObject and method that receive the typed text.
var UberKeyboardBridge = {
    UberOpenTextKeyboard: function(initialTextPtr, gameObjectNamePtr, methodNamePtr, maskedPtr) {
        try {
            var initial = initialTextPtr ? UTF8ToString(initialTextPtr) : "";
            var gameObjectName = gameObjectNamePtr ? UTF8ToString(gameObjectNamePtr) : "TouchInputManager";
            var methodName = methodNamePtr ? UTF8ToString(methodNamePtr) : "OnWebGLChatSend";
            var masked = !!maskedPtr;

            if (!window.uberChatKeyboard) {
                window.uberChatKeyboard = (function() {
                    var MAX_LENGTH = 140;
                    var caretText = null;
                    var caretPass = null;
                    var mirrorDiv = null;
                    var mirrorMark = null;
                    var mirrorInput = null;
                    var textWrap = null;
                    var passWrap = null;
                    var audioCtx = null;
                    var keyNoiseBuf = null;
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
                    var cancelButtonEl = null;
                    var okButtonEl = null;
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

                    function ensureAudio() {
                        try {
                            var AC = window.AudioContext || window.webkitAudioContext;
                            if (!AC) return null;
                            if (!audioCtx) audioCtx = new AC();
                            if (audioCtx.state === "suspended") {
                                audioCtx.resume().catch(function() {
                                });
                            }
                            return audioCtx;
                        } catch (e) {
                            return null;
                        }
                    }

                    function keyNoise() {
                        if (keyNoiseBuf) return keyNoiseBuf;
                        var ctx = ensureAudio();
                        if (!ctx) return null;
                        try {
                            var dur = 0.008;
                            keyNoiseBuf = ctx.createBuffer(1, Math.max(1, Math.floor(ctx.sampleRate * dur)), ctx.sampleRate);
                            var data = keyNoiseBuf.getChannelData(0);
                            for (var i = 0; i < data.length; i++) {
                                data[i] = (Math.random() * 2 - 1) * Math.pow(1 - i / data.length, 2);
                            }
                            return keyNoiseBuf;
                        } catch (e) {
                            return null;
                        }
                    }

                    function playKeyClick(freq) {
                        var ctx = ensureAudio();
                        if (!ctx) return;
                        try {
                            var t = ctx.currentTime;
                            var osc = ctx.createOscillator();
                            var gain = ctx.createGain();
                            osc.type = "sine";
                            osc.frequency.value = freq || 1800;
                            gain.gain.setValueAtTime(0.12, t);
                            gain.gain.exponentialRampToValueAtTime(0.0001, t + 0.045);
                            osc.connect(gain);
                            gain.connect(ctx.destination);
                            osc.start(t);
                            osc.stop(t + 0.05);

                            var buf = keyNoise();
                            if (buf) {
                                var src = ctx.createBufferSource();
                                src.buffer = buf;
                                var ng = ctx.createGain();
                                ng.gain.value = 0.05;
                                src.connect(ng);
                                ng.connect(ctx.destination);
                                src.start(t);
                            }
                        } catch (e) {
                        }
                    }

                    var soundBuffers = {};
                    var soundsLoading = false;

                    function keyboardPlatform() {
                        try {
                            var ua = navigator.userAgent.toLowerCase();
                            if (/iphone|ipod|ipad/.test(ua)) return "ios";
                            if (navigator.maxTouchPoints > 1 && /macintosh/.test(ua)) return "ios";
                            if (/android/.test(ua)) return "android";
                        } catch (e) {
                        }
                        return "ios";
                    }

                    function loadKeySounds() {
                        if (soundsLoading) return;
                        soundsLoading = true;
                        var files = {
                            standard: "KeypressStandard.wav",
                            spacebar: "KeypressSpacebar.wav",
                            "delete": "KeypressDelete.wav"
                        };
                        for (var key in files) {
                            if (!files.hasOwnProperty(key)) continue;
                            (function(k, file) {
                                try {
                                    var base = "StreamingAssets/Audio/Keyboard/" + keyboardPlatform() + "/";
                                    fetch(base + "44k/" + file + "?v=5").then(function(resp) {
                                        if (!resp || !resp.ok) throw new Error("http");
                                        return resp.arrayBuffer();
                                    }).then(function(ab) {
                                        var ctx = ensureAudio();
                                        if (!ctx) throw new Error("no ctx");
                                        var done = function(decoded) {
                                            soundBuffers[k] = decoded;
                                        };
                                        var pr = ctx.decodeAudioData(ab, done, function() {
                                        });
                                        if (pr && typeof pr.then === "function") pr.then(done, function() {
                                        });
                                    }).catch(function() {
                                        try {
                                            fetch(base + "48k/" + file + "?v=5").then(function(resp) {
                                                if (!resp || !resp.ok) throw new Error("http");
                                                return resp.arrayBuffer();
                                            }).then(function(ab) {
                                                var ctx = ensureAudio();
                                                if (!ctx) throw new Error("no ctx");
                                                var done = function(decoded) {
                                                    soundBuffers[k] = decoded;
                                                };
                                                var pr = ctx.decodeAudioData(ab, done, function() {
                                                });
                                                if (pr && typeof pr.then === "function") pr.then(done, function() {
                                                });
                                            }).catch(function() {
                                            });
                                        } catch (e) {
                                        }
                                    });
                                } catch (e) {
                                }
                            })(key, files[key]);
                        }
                    }

                    function playKeySound(key, fallbackFreq) {
                        loadKeySounds();
                        var ctx = ensureAudio();
                        if (!ctx) return;
                        var buf = soundBuffers[key];
                        if (buf) {
                            try {
                                var src = ctx.createBufferSource();
                                src.buffer = buf;
                                var g = ctx.createGain();
                                g.gain.value = 0.6;
                                src.connect(g);
                                g.connect(ctx.destination);
                                src.start(0);
                                return;
                            } catch (e) {
                            }
                        }
                        playKeyClick(fallbackFreq);
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
                            button.style.transform = pressed ? "scale(0.92)" : "";
                            button.style.boxShadow = pressed ? "inset 0 0 0 2px rgba(255,255,255,0.7)" : "";
                            if (pressed) {
                                if (button.__savedBg === undefined) button.__savedBg = button.style.background;
                                button.style.background = transparentMode ? "rgba(115,115,115,0.6)" : "#5a5a5a";
                            } else {
                                if (button.__savedBg !== undefined) {
                                    button.style.background = button.__savedBg;
                                    button.__savedBg = undefined;
                                }
                            }
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
                        if (ch === " ") {
                            playKeySound("spacebar", 1200);
                        } else {
                            playKeySound("standard", 1600 + Math.floor(Math.random() * 600));
                        }
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

                        playKeySound("delete", 900);
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

                    var pendingHeightSync = false;

                    function applyInputHeight() {
                        if (!inputEl || inputEl === passInput) return;
                        inputEl.style.height = "auto";
                        var h = inputEl.scrollHeight;
                        if (h > 0) inputEl.style.height = h + "px";
                    }

                    function syncInputHeight() {
                        if (pendingHeightSync) return;
                        pendingHeightSync = true;
                        if (window.requestAnimationFrame) {
                            window.requestAnimationFrame(function() {
                                pendingHeightSync = false;
                                applyInputHeight();
                            });
                        } else {
                            pendingHeightSync = false;
                            applyInputHeight();
                        }
                    }

                    function selectInput(start, end) {
                        if (!inputEl) return;
                        inputEl.setSelectionRange(start, end);
                        updateCaret();
                    }

                    function updateCaret() {
                        if (!inputEl || !root || root.style.display !== "flex") return;
                        var caretEl = inputEl === passInput ? caretPass : caretText;
                        if (caretText && caretText !== caretEl && caretText.style.display !== "none") caretText.style.display = "none";
                        if (caretPass && caretPass !== caretEl && caretPass.style.display !== "none") caretPass.style.display = "none";
                        if (!caretEl) return;
                        if (document.activeElement !== inputEl) {
                            if (caretEl.style.display !== "none") caretEl.style.display = "none";
                            return;
                        }

                        var s = inputEl.selectionStart;
                        var e = inputEl.selectionEnd;
                        if (typeof s !== "number" || typeof e !== "number") return;

                        var pos = e;
                        try {
                            if (inputEl.selectionDirection === "backward") pos = s;
                        } catch (err) {
                        }

                        var x, y, h;
                        if (inputEl === passInput) {
                            var masked = "";
                            for (var i = 0; i < pos; i++) masked += "\u2022";
                            mirrorInput.textContent = masked;
                            x = 1 + 10 + mirrorInput.offsetWidth - (inputEl.scrollLeft || 0);
                            y = 1 + Math.max(0, Math.round((inputEl.clientHeight - 22) / 2));
                            h = Math.min(22, inputEl.clientHeight - 2);
                        } else {
                            mirrorDiv.style.width = Math.max(10, inputEl.clientWidth - 22) + "px";
                            while (mirrorDiv.firstChild) mirrorDiv.removeChild(mirrorDiv.firstChild);
                            mirrorDiv.appendChild(document.createTextNode(inputEl.value.slice(0, pos)));
                            mirrorDiv.appendChild(mirrorMark);
                            x = 1 + 10 + mirrorMark.offsetLeft - inputEl.scrollLeft;
                            y = 1 + 6 + mirrorMark.offsetTop - inputEl.scrollTop;
                            h = 22;
                        }

                        if (y < -2 || y > inputEl.clientHeight + 2) {
                            if (caretEl.style.display !== "none") caretEl.style.display = "none";
                            return;
                        }
                        caretEl.style.left = x + "px";
                        caretEl.style.top = y + "px";
                        caretEl.style.height = h + "px";
                        if (caretEl.style.display !== "block") caretEl.style.display = "block";
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
                        var circleBg = transparentMode ? "rgba(51,51,51,0.35)" : "#333";
                        var circleBorder = transparentMode ? "1px solid rgba(255,255,255,0.45)" : "0";
                        if (cancelButtonEl) {
                            cancelButtonEl.style.background = circleBg;
                            cancelButtonEl.style.border = circleBorder;
                        }
                        if (okButtonEl) {
                            okButtonEl.style.background = circleBg;
                            okButtonEl.style.border = circleBorder;
                        }
                    }

                    function ensureFocus() {
                        if (!inputEl || !root || root.style.display !== "flex") return;
                        if (layoutMenuOpen) return;
                        try {
                            if (document.activeElement !== inputEl) inputEl.focus();
                            updateCaret();
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
                        if (caretText) caretText.style.display = "none";
                        if (caretPass) caretPass.style.display = "none";
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
                            "background:transparent;box-sizing:border-box;";
                        panelEl.style.paddingTop = "6px";
                        panelEl.style.paddingRight = "calc(10px + env(safe-area-inset-right,0px))";
                        panelEl.style.paddingBottom = "env(safe-area-inset-bottom,0px)";
                        panelEl.style.paddingLeft = "calc(10px + env(safe-area-inset-left,0px))";

                        var placeholderStyle = document.createElement("style");
                        placeholderStyle.textContent =
                            "#uber-chat-keyboard .uber-input::placeholder { color:#777; }" +
                            "#uber-chat-keyboard .uber-input::-webkit-input-placeholder { color:#777; }" +
                            "#uber-chat-keyboard .uber-caret { position:absolute;width:2px;height:22px;background:#fff;" +
                            "border-radius:1px;pointer-events:none;display:none;z-index:5;" +
                            "animation:uberCaretBlink 1.06s linear infinite; }" +
                            "@keyframes uberCaretBlink { 0%,49.99% { opacity:1; } 50%,100% { opacity:0; } }";
                        root.appendChild(placeholderStyle);

                        textInput = document.createElement("textarea");
                        textInput.className = "uber-input";
                        textInput.inputMode = "none";
                        textInput.placeholder = "Type a message...";
                        textInput.style.cssText =
                            "display:block;width:100%;box-sizing:border-box;resize:none;outline:none;caret-color:transparent;" +
                            "font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;" +
                            "min-height:clamp(34px,8vh,42px);max-height:clamp(96px,26vh,190px);overflow-y:auto;" +
                            "background:#0b0b0b;border:1px solid #3a3a3a;border-radius:10px;padding:6px 10px;" +
                            "color:#fff;font-size:18px;line-height:1.25;" +
                            "word-wrap:break-word;white-space:pre-wrap;user-select:text;-webkit-user-select:text;" +
                            "touch-action:auto;-webkit-touch-callout:default;";

                        passInput = document.createElement("input");
                        passInput.className = "uber-input";
                        passInput.type = "password";
                        passInput.inputMode = "none";
                        passInput.placeholder = "Type a message...";
                        passInput.style.cssText =
                            "display:none;width:100%;box-sizing:border-box;outline:none;caret-color:transparent;" +
                            "font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;" +
                            "height:clamp(34px,8vh,42px);background:#0b0b0b;border:1px solid #3a3a3a;" +
                            "border-radius:10px;padding:6px 10px;color:#fff;font-size:18px;line-height:1.25;" +
                            "user-select:text;-webkit-user-select:text;" +
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

                        var refocusSoon = function() {
                            setTimeout(function() {
                                if (!layoutMenuOpen) ensureFocus();
                            }, 0);
                        };
                        root.addEventListener("pointerdown", refocusSoon, false);
                        root.addEventListener("touchstart", refocusSoon, false);
                        window.addEventListener("focus", refocusSoon, false);

                        var makeCircleButton = function(label, ariaLabel, fontSize, onClick) {
                            var b = document.createElement("button");
                            b.type = "button";
                            b.textContent = label;
                            b.setAttribute("aria-label", ariaLabel);
                            b.style.cssText =
                                "display:flex;align-items:center;justify-content:center;flex:none;" +
                                "width:clamp(52px,12vh,64px);height:clamp(52px,12vh,64px);" +
                                "border-radius:50%;border:" + (transparentMode ? "1px solid rgba(255,255,255,0.45)" : "0") + ";" +
                                "background:" + (transparentMode ? "rgba(51,51,51,0.35)" : "#333") + ";color:#fff;outline:none;cursor:pointer;" +
                                "font:700 " + fontSize + "px/1 -apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;" +
                                "letter-spacing:0.3px;touch-action:manipulation;-webkit-touch-callout:none;" +
                                "-webkit-tap-highlight-color:transparent;transition:transform 50ms ease,filter 50ms ease,box-shadow 50ms ease;";
                            bindPress(b, onClick);
                            return b;
                        };

                        cancelButtonEl = makeCircleButton("\u2715", "Cancel", 24, function() {
                            try {
                                SendMessage(targetObject, sendMethod, "");
                            } catch (e) {
                            }
                            hide();
                        });

                        okButtonEl = makeCircleButton("OK", "OK", 18, function() {
                            setTimeout(function() {
                                send();
                            }, 120);
                        });

                        textWrap = document.createElement("div");
                        textWrap.style.cssText = "position:relative;flex:1 1 0%;min-width:0;";
                        passWrap = document.createElement("div");
                        passWrap.style.cssText = "position:relative;flex:1 1 0%;min-width:0;display:none;";
                        textWrap.appendChild(textInput);
                        passWrap.appendChild(passInput);

                        caretText = document.createElement("div");
                        caretText.className = "uber-caret";
                        textWrap.appendChild(caretText);
                        caretPass = document.createElement("div");
                        caretPass.className = "uber-caret";
                        passWrap.appendChild(caretPass);

                        mirrorDiv = document.createElement("div");
                        mirrorDiv.style.cssText =
                            "position:absolute;top:-9999px;left:-9999px;visibility:hidden;" +
                            "white-space:pre-wrap;word-wrap:break-word;overflow-wrap:break-word;" +
                            "font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;font-size:18px;line-height:1.25;";
                        mirrorMark = document.createElement("span");

                        mirrorInput = document.createElement("span");
                        mirrorInput.style.cssText =
                            "position:absolute;top:-9999px;left:-9999px;visibility:hidden;white-space:pre;" +
                            "font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;font-size:18px;line-height:1.25;";
                        document.body.appendChild(mirrorDiv);
                        document.body.appendChild(mirrorInput);

                        var topBar = document.createElement("div");
                        topBar.style.cssText = "display:flex;align-items:flex-start;gap:8px;width:100%;margin-bottom:6px;";
                        topBar.appendChild(cancelButtonEl);
                        topBar.appendChild(textWrap);
                        topBar.appendChild(passWrap);
                        topBar.appendChild(okButtonEl);

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
                        textInput.addEventListener("focus", updateCaret, false);
                        passInput.addEventListener("focus", updateCaret, false);
                        textInput.addEventListener("blur", updateCaret, false);
                        passInput.addEventListener("blur", updateCaret, false);
                        textInput.addEventListener("scroll", updateCaret, false);
                        document.addEventListener("selectionchange", function() {
                            if (root && root.style.display === "flex") updateCaret();
                        }, false);

                        panelEl.appendChild(topBar);
                        panelEl.appendChild(keysHost);
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

                            var placeholder;
                            if (sendMethod === "OnWebGLTextInput") {
                                placeholder = maskedMode ? "Password" : "Email";
                            } else {
                                placeholder = "Type a message...";
                            }
                            textInput.placeholder = placeholder;
                            passInput.placeholder = placeholder;

                            if (maskedMode) {
                                textInput.value = "";
                                inputEl = passInput;
                                textInput.style.display = "none";
                                passInput.style.display = "block";
                                textWrap.style.display = "none";
                                passWrap.style.display = "block";
                            } else {
                                passInput.value = "";
                                inputEl = textInput;
                                passInput.style.display = "none";
                                textInput.style.display = "block";
                                passWrap.style.display = "none";
                                textWrap.style.display = "block";
                            }

                            inputEl.value = initialText || "";
                            if (inputEl.value.length > MAX_LENGTH) inputEl.value = inputEl.value.slice(0, MAX_LENGTH);

                            symbols = false;
                            shift = false;
                            layoutMenuOpen = false;
                            applyTheme();
                            renderKeys();
                            root.style.display = "flex";
                            ensureAudio();
                            loadKeySounds();
                            syncInputHeight();

                            inputEl.setSelectionRange(inputEl.value.length, inputEl.value.length);
                            inputEl.focus();
                            updateCaret();
                            try {
                                setTimeout(function() {
                                    if (inputEl) inputEl.focus();
                                    updateCaret();
                                }, 60);
                                setTimeout(function() {
                                    if (inputEl) inputEl.focus();
                                    updateCaret();
                                }, 150);
                                setTimeout(function() {
                                    if (inputEl) inputEl.focus();
                                    updateCaret();
                                }, 400);
                            } catch (e) {
                            }

                            clearFocusTimer();
                            try {
                                focusTimer = setInterval(ensureFocus, 400);
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


mergeInto(LibraryManager.library, UberKeyboardBridge);
