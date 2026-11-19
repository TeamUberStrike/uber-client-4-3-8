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

    // Open a URL in a new browser tab. Application.OpenURL is unreliable on
    // WebGL (often swallowed by popup blockers). window.open within the click
    // gesture normally works; the temporary anchor click is the fallback when
    // it returns null.
    UberOpenUrl: function(url) {
        var target = UTF8ToString(url);
        try {
            var win = window.open(target, "_blank");
            if (!win) {
                var a = document.createElement("a");
                a.href = target;
                a.target = "_blank";
                a.rel = "noopener noreferrer";
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
            }
        } catch (e) {
            try {
                window.open(target, "_blank");
            } catch (e2) {
            }
        }
    },
};

mergeInto(LibraryManager.library, UberDeviceBridge);
