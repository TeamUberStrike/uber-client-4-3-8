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

        var dbg = window.__uberAudioDbg;
        var unlock = function(ev) {
            try {
                if (!WEBAudio || !WEBAudio.audioContext) return;
                var ctx = WEBAudio.audioContext;
                if (ctx.state !== "running" && ctx.state !== "closed") {
                    var p = ctx.resume();
                    if (p) p.then(function() {
                        if (dbg) dbg.res.push(ev.type + ": ok " + ctx.state);
                    }, function(e) {
                        if (dbg) dbg.res.push(ev.type + ": " + e);
                    });
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
                if (dbg) dbg.res.push(ev.type + ": " + e);
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

    UberAudioDebugEnabled: function() {
        return window.__uberAudioDbg ? 1 : 0;
    },

    UberAudioDebugSetUnity: function(text) {
        if (window.__uberAudioDbg) window.__uberAudioDbg.unity = UTF8ToString(text);
    },

    UberAudioDebugInit__deps: ['$WEBAudio'],
    UberAudioDebugInit: function(playGameSound) {
        var dbg = window.__uberAudioDbg;
        if (!dbg || dbg.panel) return;

        var t0 = performance.now();
        var stamp = function() { return ((performance.now() - t0) / 1000).toFixed(1) + "s"; };
        var ctx = WEBAudio.audioContext;
        dbg.states = [];
        if (ctx) {
            dbg.states.push(stamp() + " " + ctx.state);
            ctx.addEventListener("statechange", function() { dbg.states.push(stamp() + " " + ctx.state); });
        }
        var tests = [];
        var note = function(s) { tests.push(stamp() + " " + s); };

        var beepCtx = function(c, freq, label) {
            try {
                if (c.state !== "running") c.resume().then(function() { note(label + " resumed " + c.state); }, function(e) { note(label + " resume " + e); });
                var o = c.createOscillator();
                var g = c.createGain();
                o.frequency.value = freq;
                g.gain.value = 0.3;
                o.connect(g);
                g.connect(c.destination);
                o.start(c.currentTime);
                o.stop(c.currentTime + 0.35);
                note(label + " " + c.state + " " + c.sampleRate + "Hz");
            } catch (e) { note(label + " " + e); }
        };
        var beepNewCtx = function() {
            try {
                var C = window.AudioContext || window.webkitAudioContext;
                if (!dbg.freshCtx) dbg.freshCtx = new C();
                beepCtx(dbg.freshCtx, 440, "2 new ctx");
            } catch (e) { note("2 new ctx " + e); }
        };
        var wavUrl = null;
        var beepElement = function() {
            try {
                if (!wavUrl) {
                    var rate = 22050, n = Math.floor(rate * 0.35), buf = new ArrayBuffer(44 + n * 2), v = new DataView(buf);
                    var w = function(o, s) { for (var k = 0; k < s.length; k++) v.setUint8(o + k, s.charCodeAt(k)); };
                    w(0, "RIFF"); v.setUint32(4, 36 + n * 2, true); w(8, "WAVEfmt "); v.setUint32(16, 16, true);
                    v.setUint16(20, 1, true); v.setUint16(22, 1, true); v.setUint32(24, rate, true); v.setUint32(28, rate * 2, true);
                    v.setUint16(32, 2, true); v.setUint16(34, 16, true); w(36, "data"); v.setUint32(40, n * 2, true);
                    for (var k = 0; k < n; k++) v.setInt16(44 + k * 2, Math.sin(k * 2 * Math.PI * 660 / rate) * 9000, true);
                    wavUrl = URL.createObjectURL(new Blob([buf], { type: "audio/wav" }));
                }
                var a = new Audio(wavUrl);
                var p = a.play();
                if (p) p.then(function() { note("3 html audio playing"); }, function(e) { note("3 html audio " + e); });
            } catch (e) { note("3 html audio " + e); }
        };
        var playGame = function() {
            try { dynCall("v", playGameSound, []); note("4 game sound sent"); } catch (e) { note("4 game " + e); }
        };
        var beepUnity = function() {
            if (WEBAudio.audioContext) beepCtx(WEBAudio.audioContext, 880, "1 unity ctx");
            else note("1 unity ctx none");
        };

        var panel = document.createElement("div");
        panel.id = "uber-audio-debug";
        panel.style.cssText = "position:fixed;left:0;top:0;z-index:2000;width:66vw;max-height:100vh;overflow:auto;box-sizing:border-box;" +
            "background:rgba(0,0,0,0.82);color:#fff;font:12px/1.25 monospace;padding:6px;white-space:pre-wrap;word-break:break-all;";
        var big = document.createElement("button");
        big.textContent = "PLAY TEST SOUND";
        big.style.cssText = "display:block;width:100%;margin:0 0 4px;padding:10px 8px;font:bold 20px sans-serif;border:0;border-radius:8px;background:#2a2;color:#fff;";
        big.addEventListener("click", function(e) {
            e.stopPropagation();
            note("--- all");
            beepUnity();
            beepNewCtx();
            beepElement();
            playGame();
        });
        var hint = document.createElement("div");
        hint.textContent = "1 high = Unity WebAudio, 2 low = new WebAudio, 3 mid = HTML audio, 4 = game sound";
        hint.style.cssText = "font-size:12px;color:#bbb;";
        var row = document.createElement("div");
        row.style.cssText = "display:flex;gap:6px;margin:4px 0;";
        var body = document.createElement("div");
        var small = function(label, fn) {
            var b = document.createElement("button");
            b.textContent = label;
            b.style.cssText = "flex:1;padding:8px 2px;font:bold 14px sans-serif;border:0;border-radius:6px;background:#555;color:#fff;";
            b.addEventListener("click", function(e) { e.stopPropagation(); fn(); });
            row.appendChild(b);
        };
        small("1 high", function() { note("--- 1"); beepUnity(); });
        small("2 low", function() { note("--- 2"); beepNewCtx(); });
        small("3 mid", function() { note("--- 3"); beepElement(); });
        small("4 game", function() { note("--- 4"); playGame(); });
        small("hide", function() { body.style.display = body.style.display === "none" ? "" : "none"; });
        panel.appendChild(big);
        panel.appendChild(hint);
        panel.appendChild(row);
        panel.appendChild(body);
        document.body.appendChild(panel);
        dbg.panel = panel;

        var last = function(a, n) { return a.slice(Math.max(0, a.length - n)); };
        var refresh = function() {
            var c = WEBAudio.audioContext;
            var ev = [];
            for (var k in dbg.ev) ev.push(k + ":" + dbg.ev[k]);
            var ua = navigator.userAgent.replace("Mozilla/5.0 ", "").replace("(KHTML, like Gecko) ", "");
            var s = "";
            s += "ctx: " + (c ? c.state + " " + c.sampleRate + "Hz t=" + c.currentTime.toFixed(1) : "none") + " runFlag=" + WEBAudio.contextIsRunning + "\n";
            s += "webEnabled=" + WEBAudio.audioWebEnabled + " cache=" + (WEBAudio.audioCache ? WEBAudio.audioCache.length : -1) +
                " inst=" + Object.keys(WEBAudio.audioInstances).length + " pending=" + WEBAudio.soundsPendingContextResume.length + "\n";
            s += "states: " + last(dbg.states, 5).join(" | ") + "\n";
            s += "events: " + ev.join(" ") + "\n";
            s += "resume: " + last(dbg.res, 4).join(" | ") + "\n";
            s += "session: " + (navigator.audioSession ? navigator.audioSession.type + "/" + navigator.audioSession.state : "n/a") +
                " activated: " + (navigator.userActivation ? navigator.userActivation.hasBeenActive : "n/a") + "\n";
            s += "UA: " + ua.substring(0, 130) + "\n";
            s += "unity: " + (dbg.unity || "-") + "\n";
            s += "tests:\n  " + last(tests, 7).join("\n  ") + "\n";
            s += "log:\n  " + last(dbg.log, 6).join("\n  ");
            body.textContent = s;
        };
        refresh();
        setInterval(refresh, 500);

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
};

mergeInto(LibraryManager.library, UberDeviceBridge);
