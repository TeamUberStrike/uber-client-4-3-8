// Device diagnostics for the server device check. No permission prompts, no labels, no keystroke content.
var UberDeviceDiag = {
    $UberDiag__postset: "setTimeout(function() { try { UberDiag.boot(); } catch (e) {} }, 0);",
    $UberDiag: {
        t0: 0,
        booted: false,
        boot0: null,
        async: {},
        c: {
            pdM: 0, pdT: 0, pdP: 0, pmM: 0, pmT: 0, raw: 0, mm: 0, hov: 0, md: 0, wh: 0, kd: 0, ku: 0, wasd: 0,
            ts: 0, tm: 0, te: 0, tw1: 0, twN: 0, dm: 0, prs: {}, lastPd: 0, ivlSum: 0, ivlN: 0,
            frames: 0, dtSum: 0, dt2Sum: 0, lastRaf: 0,
            ps: 0, ph: 0, rs: 0, oc: 0, vvr: 0, pcc: 0, sdc: 0, vis: 0
        },

        h: function(s) {
            var x = 0x811c9dc5;
            s = String(s);
            for (var i = 0; i < s.length; i++) {
                x ^= s.charCodeAt(i);
                x = (x + ((x << 1) + (x << 4) + (x << 7) + (x << 8) + (x << 24))) >>> 0;
            }
            return ("0000000" + x.toString(16)).slice(-8);
        },

        safe: function(f, d) {
            try { var v = f(); return v === undefined ? d : v; } catch (e) { return d; }
        },

        mq: function(q) {
            try { return window.matchMedia ? window.matchMedia(q).matches : null; } catch (e) { return null; }
        },

        boot: function() {
            if (UberDiag.booted || typeof window === "undefined") return;
            UberDiag.booted = true;
            UberDiag.t0 = (window.performance && performance.now) ? performance.now() : 0;
            var c = UberDiag.c;
            var on = function(t, n, f) { try { t.addEventListener(n, f, { passive: true, capture: true }); } catch (e) {} };

            on(window, "pointerdown", function(e) {
                if (e.pointerType === "mouse") c.pdM++; else if (e.pointerType === "touch") c.pdT++; else if (e.pointerType === "pen") c.pdP++;
                if (e.pointerType === "touch") {
                    c.twN++;
                    if ((e.width || 0) <= 1 && (e.height || 0) <= 1) c.tw1++;
                    c.prs[Math.round((e.pressure || 0) * 10)] = 1;
                }
                var n = performance.now();
                if (c.lastPd) { c.ivlSum += n - c.lastPd; c.ivlN++; }
                c.lastPd = n;
            });
            on(window, "pointermove", function(e) { if (e.pointerType === "mouse") c.pmM++; else if (e.pointerType === "touch") c.pmT++; });
            on(window, "pointerrawupdate", function() { c.raw++; });
            on(window, "mousemove", function(e) { c.mm++; if (!e.buttons) c.hov++; });
            on(window, "mousedown", function() { c.md++; });
            on(window, "wheel", function() { c.wh++; });
            on(window, "keydown", function(e) {
                c.kd++;
                var k = e.code || "";
                if (k === "KeyW" || k === "KeyA" || k === "KeyS" || k === "KeyD" || k.indexOf("Arrow") === 0) c.wasd++;
            });
            on(window, "keyup", function() { c.ku++; });
            on(window, "touchstart", function() { c.ts++; });
            on(window, "touchmove", function() { c.tm++; });
            on(window, "touchend", function() { c.te++; });
            on(window, "devicemotion", function() { c.dm++; });
            on(window, "pageshow", function() { c.ps++; });
            on(window, "pagehide", function() { c.ph++; });
            on(window, "orientationchange", function() { c.oc++; });
            on(document, "visibilitychange", function() { c.vis++; });
            var lastScr = screen.width + "x" + screen.height;
            on(window, "resize", function() {
                c.rs++;
                var s = screen.width + "x" + screen.height;
                if (s !== lastScr) { c.sdc++; lastScr = s; }
            });
            if (window.visualViewport) on(window.visualViewport, "resize", function() { c.vvr++; });
            var watch = ["(any-pointer: coarse)", "(pointer: fine)", "(hover: hover)"];
            for (var i = 0; i < watch.length; i++) {
                try {
                    var m = window.matchMedia(watch[i]);
                    var f = function() { c.pcc++; };
                    if (m.addEventListener) m.addEventListener("change", f); else if (m.addListener) m.addListener(f);
                } catch (e) {}
            }
            var raf = function(t) {
                if (c.lastRaf) {
                    var dt = t - c.lastRaf;
                    if (dt > 0 && dt < 1000) { c.frames++; c.dtSum += dt; c.dt2Sum += dt * dt; }
                }
                c.lastRaf = t;
                window.requestAnimationFrame(raf);
            };
            try { window.requestAnimationFrame(raf); } catch (e) {}

            UberDiag.asyncProbes();
            UberDiag.boot0 = UberDiag.screen();
            UberDiag.boot0.apc = UberDiag.mq("(any-pointer: coarse)");
            UberDiag.boot0.pf = UberDiag.mq("(pointer: fine)");
        },

        asyncProbes: function() {
            var a = UberDiag.async;
            var nav = navigator;
            try {
                if (nav.userAgentData && nav.userAgentData.getHighEntropyValues) {
                    nav.userAgentData.getHighEntropyValues(["architecture", "bitness", "model", "platformVersion", "uaFullVersion", "fullVersionList", "wow64"])
                        .then(function(v) {
                            a.he = {
                                architecture: v.architecture, bitness: v.bitness, model: v.model, platformVersion: v.platformVersion,
                                uaFullVersion: v.uaFullVersion, wow64: v.wow64,
                                fullVersionList: (v.fullVersionList || []).map(function(b) { return b.brand + "/" + b.version; })
                            };
                        }, function() {});
                }
            } catch (e) {}
            try {
                if (nav.getBattery) nav.getBattery().then(function(b) {
                    a.bat = { c: b.charging, l: b.level, ct: isFinite(b.chargingTime) ? b.chargingTime : -1, dt: isFinite(b.dischargingTime) ? b.dischargingTime : -1 };
                }, function() {});
            } catch (e) {}
            try {
                if (nav.storage && nav.storage.estimate) nav.storage.estimate().then(function(e) { a.quota = Math.round((e.quota || 0) / 1048576); }, function() {});
                if (nav.storage && nav.storage.persisted) nav.storage.persisted().then(function(p) { a.persisted = p; }, function() {});
            } catch (e) {}
            try {
                if (nav.mediaDevices && nav.mediaDevices.enumerateDevices) nav.mediaDevices.enumerateDevices().then(function(list) {
                    var d = { audioinput: 0, videoinput: 0, audiooutput: 0 };
                    for (var i = 0; i < list.length; i++) if (d[list[i].kind] !== undefined) d[list[i].kind]++;
                    a.devs = d;
                }, function() {});
            } catch (e) {}
            try {
                if (nav.gpu && nav.gpu.requestAdapter) nav.gpu.requestAdapter().then(function(ad) {
                    if (!ad) { a.gpu = { none: true }; return; }
                    var inf = ad.info || {};
                    a.gpu = { v: inf.vendor || "", a: inf.architecture || "", d: inf.description || "" };
                }, function() {});
            } catch (e) {}
            try {
                var AC = window.OfflineAudioContext || window.webkitOfflineAudioContext;
                if (AC) {
                    var ctx = new AC(1, 4410, 44100);
                    var osc = ctx.createOscillator();
                    osc.type = "triangle";
                    osc.frequency.value = 10000;
                    var cmp = ctx.createDynamicsCompressor();
                    osc.connect(cmp);
                    cmp.connect(ctx.destination);
                    osc.start(0);
                    var done = function(buf) {
                        var d = buf.getChannelData(0), s = 0;
                        for (var i = 4000; i < 4410; i++) s += Math.abs(d[i]);
                        a.afp = UberDiag.h(s.toFixed(6));
                    };
                    var p = ctx.startRendering();
                    if (p && p.then) p.then(done, function() {}); else ctx.oncomplete = function(ev) { done(ev.renderedBuffer); };
                }
            } catch (e) {}
        },

        screen: function() {
            var s = window.screen || {};
            var vv = window.visualViewport;
            var o = s.orientation;
            return {
                w: s.width, h: s.height, aw: s.availWidth, ah: s.availHeight, cd: s.colorDepth, pd: s.pixelDepth,
                dpr: window.devicePixelRatio, iw: window.innerWidth, ih: window.innerHeight, ow: window.outerWidth, oh: window.outerHeight,
                ot: o ? o.type : null, oa: o ? o.angle : (typeof window.orientation === "number" ? window.orientation : null),
                vvw: vv ? Math.round(vv.width) : null, vvh: vv ? Math.round(vv.height) : null, vvs: vv ? vv.scale : null,
                fs: !!(document.fullscreenElement || document.webkitFullscreenElement)
            };
        },

        gl: function() {
            var r = null;
            try {
                var cv = document.createElement("canvas");
                var g2 = cv.getContext("webgl2");
                var g = g2 || cv.getContext("webgl") || cv.getContext("experimental-webgl");
                if (!g) return null;
                var dbg = g.getExtension("WEBGL_debug_renderer_info");
                var ext = g.getSupportedExtensions() || [];
                var has = function(p) { for (var i = 0; i < ext.length; i++) if (ext[i].indexOf(p) >= 0) return true; return false; };
                var vd = g.getParameter(g.MAX_VIEWPORT_DIMS);
                var pr = g.getShaderPrecisionFormat(g.FRAGMENT_SHADER, g.HIGH_FLOAT);
                r = {
                    v1: !!window.WebGLRenderingContext, v2: !!g2,
                    ven: g.getParameter(g.VENDOR), ren: g.getParameter(g.RENDERER),
                    uven: dbg ? g.getParameter(dbg.UNMASKED_VENDOR_WEBGL) : null,
                    uren: dbg ? g.getParameter(dbg.UNMASKED_RENDERER_WEBGL) : null,
                    mts: g.getParameter(g.MAX_TEXTURE_SIZE), mrs: g.getParameter(g.MAX_RENDERBUFFER_SIZE),
                    mvd: vd ? [vd[0], vd[1]] : null, mva: g.getParameter(g.MAX_VERTEX_ATTRIBS),
                    ext: ext.length, exth: UberDiag.h(ext.join(",")),
                    astc: has("astc"), etc: has("compressed_texture_etc"), s3tc: has("s3tc"), pvrtc: has("pvrtc"),
                    prec: pr ? pr.rangeMin + "/" + pr.rangeMax + "/" + pr.precision : null,
                    gpu: !!navigator.gpu
                };
                r.fp = UberDiag.h([r.ven, r.ren, r.uven, r.uren, r.mts, r.mrs, r.mva, r.prec, r.exth].join("|"));
                var lose = g.getExtension("WEBGL_lose_context");
                if (lose) lose.loseContext();
            } catch (e) {}
            var gp = UberDiag.async.gpu;
            if (r && gp) { r.gpuv = gp.v; r.gpua = gp.a; r.gpud = gp.d; }
            return r;
        },

        fp: function() {
            var o = {};
            try {
                var cv = document.createElement("canvas");
                cv.width = 220; cv.height = 40;
                var x = cv.getContext("2d");
                x.textBaseline = "top";
                x.font = "14px 'Arial'";
                x.fillStyle = "#f60"; x.fillRect(100, 1, 62, 20);
                x.fillStyle = "#069"; x.fillText("UberStrike,😃 fp", 2, 15);
                x.fillStyle = "rgba(102,204,0,0.7)"; x.fillText("UberStrike,😃 fp", 4, 17);
                o.canvas = UberDiag.h(cv.toDataURL());
                var e = document.createElement("canvas");
                e.width = 32; e.height = 32;
                var ex = e.getContext("2d");
                ex.font = "24px sans-serif"; ex.textBaseline = "top";
                ex.fillText("😀", 0, 0);
                o.emoji = UberDiag.h(e.toDataURL());
                var m = x.measureText("mmmmmmmmmlli 0O");
                o.text = UberDiag.h([m.width, m.actualBoundingBoxAscent, m.actualBoundingBoxDescent].join(","));
                var fonts = ["Arial", "Helvetica Neue", "Segoe UI", "Roboto", "San Francisco", "Calibri", "Cambria", "Consolas",
                    "Courier New", "Georgia", "Tahoma", "Verdana", "Menlo", "Noto Sans", "Droid Sans", "Ubuntu", "Apple Color Emoji",
                    "Segoe UI Emoji", "Noto Color Emoji", "SF Pro Text", "Lucida Grande", "Marlett", "MS Gothic", "Gulim"];
                var base = {};
                var gens = ["monospace", "serif", "sans-serif"];
                for (var i = 0; i < gens.length; i++) { x.font = "32px " + gens[i]; base[gens[i]] = x.measureText("mmmmmmmmmmlli").width; }
                var bits = "", n = 0;
                for (var j = 0; j < fonts.length; j++) {
                    var found = false;
                    for (var k = 0; k < gens.length && !found; k++) {
                        x.font = "32px '" + fonts[j] + "'," + gens[k];
                        if (x.measureText("mmmmmmmmmmlli").width !== base[gens[k]]) found = true;
                    }
                    bits += found ? "1" : "0";
                    if (found) n++;
                }
                o.fonts = bits; o.fontN = n;
            } catch (e) {}
            try {
                var cs = ["display:grid", "backdrop-filter:blur(1px)", "-webkit-touch-callout:none", "-webkit-overflow-scrolling:touch",
                    "overscroll-behavior:none", "text-size-adjust:none", "-webkit-text-size-adjust:none", "aspect-ratio:1"];
                var b = "";
                for (var i2 = 0; i2 < cs.length; i2++) b += (window.CSS && CSS.supports(cs[i2])) ? "1" : "0";
                o.css = b;
            } catch (e) {}
            o.audio = UberDiag.async.afp || null;
            return o;
        },

        autoFlags: function() {
            var f = [];
            var names = ["__playwright", "__pwInitScripts", "__playwright__binding__", "__puppeteer_evaluation_script__", "callPhantom", "_phantom",
                "__nightmare", "domAutomation", "domAutomationController", "_selenium", "__webdriver_evaluate", "__selenium_unwrapped",
                "__fxdriver_unwrapped", "__webdriver_script_fn", "_Selenium_IDE_Recorder", "__lastWatirAlert"];
            for (var i = 0; i < names.length; i++) { try { if (names[i] in window) f.push(names[i]); } catch (e) {} }
            try {
                var keys = Object.getOwnPropertyNames(window);
                for (var j = 0; j < keys.length; j++) if (/^\$?cdc_|^\$wdc_|^__pw|^__playwright/.test(keys[j]) && f.indexOf(keys[j]) < 0) f.push(keys[j]);
                var dk = Object.getOwnPropertyNames(document);
                for (var k = 0; k < dk.length; k++) if (/^\$?cdc_|^\$wdc_/.test(dk[k])) f.push("doc." + dk[k]);
            } catch (e) {}
            return f.slice(0, 10);
        },

        snap: function(phase) {
            var nav = navigator, a = UberDiag.async, c = UberDiag.c, S = UberDiag.safe, mq = UberDiag.mq;
            var uad = nav.userAgentData;
            var now = performance.now();
            var conn = nav.connection || nav.mozConnection || nav.webkitConnection;
            var mean = c.frames ? c.dtSum / c.frames : 0;
            var vt = document.createElement("video");
            var mr = window.MediaRecorder;
            var mrs = function(t) { try { return mr && mr.isTypeSupported ? mr.isTypeSupported(t) : null; } catch (e) { return null; } };
            var sr = null;
            try { if (typeof WEBAudio !== "undefined" && WEBAudio.audioContext) sr = WEBAudio.audioContext.sampleRate; } catch (e) {}
            return {
                v: 1, kind: "web", phase: phase, t: Math.round(now - UberDiag.t0),
                nav: {
                    ua: nav.userAgent, platform: nav.platform, vendor: nav.vendor, appVersion: nav.appVersion, product: nav.product,
                    productSub: nav.productSub, language: nav.language, languages: S(function() { return Array.prototype.slice.call(nav.languages || [], 0, 5); }, []),
                    cookie: nav.cookieEnabled, pdf: S(function() { return nav.pdfViewerEnabled; }, null),
                    java: S(function() { return nav.javaEnabled ? nav.javaEnabled() : null; }, null),
                    webdriver: S(function() { return nav.webdriver; }, null),
                    plugins: S(function() { return nav.plugins.length; }, null), mimes: S(function() { return nav.mimeTypes.length; }, null)
                },
                uad: uad ? {
                    mobile: uad.mobile, platform: uad.platform,
                    brands: S(function() { return uad.brands.map(function(b) { return b.brand + "/" + b.version; }); }, []),
                    he: a.he || null
                } : null,
                touch: { mtp: S(function() { return nav.maxTouchPoints; }, null), ontouch: "ontouchstart" in window, te: typeof TouchEvent !== "undefined", pe: typeof PointerEvent !== "undefined" },
                mq: {
                    pc: mq("(pointer: coarse)"), pf: mq("(pointer: fine)"), apc: mq("(any-pointer: coarse)"), apf: mq("(any-pointer: fine)"),
                    hh: mq("(hover: hover)"), ahh: mq("(any-hover: hover)"), p3: mq("(color-gamut: p3)"), hdr: mq("(dynamic-range: high)"),
                    dark: mq("(prefers-color-scheme: dark)"), rm: mq("(prefers-reduced-motion: reduce)"), pcm: mq("(prefers-contrast: more)")
                },
                scr: UberDiag.screen(),
                boot: UberDiag.boot0,
                hw: { cores: S(function() { return nav.hardwareConcurrency; }, null), mem: S(function() { return nav.deviceMemory; }, null), bat: a.bat || null, batApi: !!nav.getBattery },
                gl: UberDiag.gl(),
                auto: {
                    wd: S(function() { return nav.webdriver; }, null), chrome: !!window.chrome, perms: !!nav.permissions,
                    sw: "serviceWorker" in nav, shw: "SharedWorker" in window, w: "Worker" in window, wasm: "WebAssembly" in window,
                    flags: UberDiag.autoFlags()
                },
                fp: UberDiag.fp(),
                net: {
                    type: conn ? conn.type || null : null, et: conn ? conn.effectiveType || null : null, dl: conn ? conn.downlink : null,
                    rtt: conn ? conn.rtt : null, sd: conn ? conn.saveData : null, online: nav.onLine, rtc: typeof RTCPeerConnection !== "undefined"
                },
                sto: {
                    ls: S(function() { return !!window.localStorage; }, false), ss: S(function() { return !!window.sessionStorage; }, false),
                    idb: !!window.indexedDB, caches: "caches" in window, quota: a.quota === undefined ? null : a.quota,
                    persisted: a.persisted === undefined ? null : a.persisted, clip: !!nav.clipboard, share: !!nav.share, vib: !!nav.vibrate,
                    dor: typeof DeviceOrientationEvent !== "undefined", dmo: typeof DeviceMotionEvent !== "undefined",
                    dorp: typeof DeviceOrientationEvent !== "undefined" && typeof DeviceOrientationEvent.requestPermission === "function",
                    geo: !!nav.geolocation, md: !!nav.mediaDevices, gp: !!nav.getGamepads,
                    gpn: S(function() { var g = nav.getGamepads(), n = 0; for (var i = 0; i < g.length; i++) if (g[i]) n++; return n; }, null),
                    devs: a.devs || null
                },
                media: {
                    h264: vt.canPlayType('video/mp4; codecs="avc1.42E01E"'), hevc: vt.canPlayType('video/mp4; codecs="hvc1"'),
                    vp9: vt.canPlayType('video/webm; codecs="vp9"'), av1: vt.canPlayType('video/mp4; codecs="av01.0.05M.08"'),
                    aac: vt.canPlayType('audio/mp4; codecs="mp4a.40.2"'), opus: vt.canPlayType('audio/ogg; codecs="opus"'),
                    mr: !!mr, mrWebm: mrs("video/webm"), mrMp4: mrs("video/mp4"), sr: sr
                },
                "in": {
                    pdM: c.pdM, pdT: c.pdT, pdP: c.pdP, pmM: c.pmM, pmT: c.pmT, raw: c.raw, mm: c.mm, hov: c.hov, md: c.md, wh: c.wh,
                    kd: c.kd, ku: c.ku, wasd: c.wasd, ts: c.ts, tm: c.tm, te: c.te, tw1: c.tw1, twN: c.twN, dm: c.dm,
                    prs: Object.keys(c.prs).length, mixed: (c.pdM > 0 && c.pdT > 0),
                    ivl: c.ivlN ? Math.round(c.ivlSum / c.ivlN) : null
                },
                rt: {
                    sec: Math.round((now - UberDiag.t0) / 1000), fps: mean ? Math.round(1000 / mean) : null,
                    ftj: c.frames ? Math.round(Math.sqrt(Math.max(0, c.dt2Sum / c.frames - mean * mean)) * 10) / 10 : null,
                    vs: document.visibilityState, focus: S(function() { return document.hasFocus(); }, null),
                    ps: c.ps, ph: c.ph, rs: c.rs, oc: c.oc, vvr: c.vvr, pcc: c.pcc, sdc: c.sdc, vis: c.vis
                }
            };
        }
    },

    UberDiagCollect__deps: ["$UberDiag"],
    UberDiagCollect: function(phasePtr) {
        var json = "{}";
        try {
            UberDiag.boot();
            json = JSON.stringify(UberDiag.snap(UTF8ToString(phasePtr)));
        } catch (e) {
            json = JSON.stringify({ v: 1, kind: "web", phase: "error", err: String(e).slice(0, 120), nav: { ua: navigator.userAgent } });
        }
        var n = lengthBytesUTF8(json) + 1;
        var p = _malloc(n);
        stringToUTF8(json, p, n);
        return p;
    }
};

mergeInto(LibraryManager.library, UberDeviceDiag);
