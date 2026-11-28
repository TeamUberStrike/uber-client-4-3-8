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

    // iOS Safari tap-to-reveal zone: the top strip of the page where taps are consumed by the
    // browser UI instead of reaching the game. The strip is a fixed CSS-pixel height on every
    // iPhone (browser chrome does not scale with the screen), but Unity sees canvas pixels, so
    // it is converted with the canvas' CSS-to-buffer scale (device pixel ratio and any render
    // resolution scaling). The C# side only uses this on iOS (UberGetDeviceType == 1).
    UberGetTopDeadZonePixels: function() {
        try {
            var cssPixels = 28;
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
                    if (p && typeof p.then === "function") {
                        p.catch(function() {
                        });
                    }
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

        var events = ["touchend", "pointerup", "click", "keydown", "focus"];
        for (var i = 0; i < events.length; i++) {
            window.addEventListener(events[i], unlock, true);
        }
        document.addEventListener("visibilitychange", function(ev) {
            if (!document.hidden) unlock(ev);
        });
        window.addEventListener("pageshow", function(ev) {
            if (ev.persisted) {
                setTimeout(unlock, 0);
                setTimeout(unlock, 250);
            }
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

    // Mobile WebGL keyboard lives in Keyboard.jslib (UberOpenTextKeyboard,
    // UberAddKeyboardLayout, UberLoadKeyboardLayout, UberCloseKeyboard).

    // Gyro for WebGL: devicemotion rates (deg/s) + deviceorientation tilt. iOS needs a tap to grant.
    UberGyroStart: function() {
        if (window.__uberGyro) return;
        var g = window.__uberGyro = { rate: [0, 0, 0], beta: 0, gamma: 0, motionAt: 0, tiltAt: 0, perm: 3 };
        var listen = function() {
            window.addEventListener("devicemotion", function(e) {
                var r = e.rotationRate;
                if (!r || r.alpha === null) return;
                g.rate = [r.beta || 0, r.gamma || 0, r.alpha || 0];
                g.motionAt = performance.now();
            });
            window.addEventListener("deviceorientation", function(e) {
                if (e.beta === null) return;
                g.beta = e.beta; g.gamma = e.gamma || 0;
                g.tiltAt = performance.now();
            });
        };
        if (typeof DeviceMotionEvent !== "undefined" && typeof DeviceMotionEvent.requestPermission === "function") {
            g.perm = 0;
            var ask = function() {
                window.removeEventListener("touchend", ask, true);
                window.removeEventListener("click", ask, true);
                var p1 = DeviceMotionEvent.requestPermission();
                var p2 = (typeof DeviceOrientationEvent !== "undefined" && typeof DeviceOrientationEvent.requestPermission === "function")
                    ? DeviceOrientationEvent.requestPermission() : Promise.resolve("granted");
                Promise.all([p1, p2]).then(function(s) {
                    g.perm = (s[0] === "granted" && s[1] === "granted") ? 1 : 2;
                    if (g.perm === 1) listen();
                }).catch(function() { g.perm = 2; });
            };
            window.addEventListener("touchend", ask, true);
            window.addEventListener("click", ask, true);
        } else {
            listen();
        }
    },

    // 0 waiting for tap, 1 granted, 2 denied, 3 no prompt needed.
    UberGyroPermission: function() {
        return window.__uberGyro ? window.__uberGyro.perm : 0;
    },

    // out[0..5] = valid, rate x/y/z (deg/s, device axes), beta, gamma (deg).
    UberGyroRead: function(ptr) {
        var g = window.__uberGyro, i = ptr >> 2;
        var now = performance.now();
        var ok = g && now - g.motionAt < 500 && now - g.tiltAt < 500;
        HEAPF32[i] = ok ? 1 : 0;
        if (!ok) return;
        HEAPF32[i + 1] = g.rate[0]; HEAPF32[i + 2] = g.rate[1]; HEAPF32[i + 3] = g.rate[2];
        HEAPF32[i + 4] = g.beta; HEAPF32[i + 5] = g.gamma;
    },
};


mergeInto(LibraryManager.library, UberDeviceBridge);
