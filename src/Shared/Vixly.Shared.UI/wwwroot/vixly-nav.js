// =========================================================
// FILE: vixly-nav.js
// PURPOSE: The side menu keeps its place (owner, 2026-10-30):
// a section the user closed stays closed, and the menu stays
// scrolled where the user left it after each click, in the
// staff workspace and in Control.
// NOTES:
// - Menus: .ops-nav (workspace) and .vx-sidebar__nav (Control);
//   each <details> is a section, keyed by data-nav-group
//   (else its position in the menu)
// - Closed sections: localStorage, so they stay closed on the
//   next visit; sections the user cannot see keep their state
// - Scroll position: sessionStorage, for this browser tab;
//   both the menu and its sidebar are restored
// - The menu may be drawn again (Control's live pages, the
//   first render after the page arrives): it is set up again
// - Storage refused (private window): the menu works as before
// =========================================================
(function () {
    "use strict";
    var MENUS = ".ops-nav, .vx-sidebar__nav";
    var CLOSED = "vixly-nav:closed";
    var SCROLL = "vixly-nav:scroll";

    function read(store, key) {
        try { return JSON.parse(window[store].getItem(key) || "null"); } catch (e) { return null; }
    }

    function write(store, key, value) {
        try { window[store].setItem(key, JSON.stringify(value)); } catch (e) { /* storage refused */ }
    }

    function sections(menu) {
        return Array.prototype.slice.call(menu.querySelectorAll("details"));
    }

    function keyOf(section, index) {
        return section.getAttribute("data-nav-group") || "section-" + index;
    }

    function saveClosed(menu) {
        var all = sections(menu);
        var mine = all.map(keyOf);
        var closed = (read("localStorage", CLOSED) || []).filter(function (key) { return mine.indexOf(key) < 0; });
        all.forEach(function (section, index) { if (!section.open) { closed.push(keyOf(section, index)); } });
        write("localStorage", CLOSED, closed);
    }

    function saveScroll(menu, side) {
        write("sessionStorage", SCROLL, { menu: menu.scrollTop, side: side ? side.scrollTop : 0 });
    }

    function setUp(menu) {
        if (menu.getAttribute("data-nav-kept") === "true") { return; }
        menu.setAttribute("data-nav-kept", "true");
        var side = menu.closest("aside");
        var closed = read("localStorage", CLOSED) || [];

        sections(menu).forEach(function (section, index) {
            if (closed.indexOf(keyOf(section, index)) >= 0) { section.open = false; }
        });

        var place = read("sessionStorage", SCROLL);
        if (place) {
            menu.scrollTop = place.menu || 0;
            if (side) { side.scrollTop = place.side || 0; }
        }

        // toggle does not bubble: listen while it travels down.
        menu.addEventListener("toggle", function () { saveClosed(menu); }, true);
        menu.addEventListener("scroll", function () { saveScroll(menu, side); }, { passive: true });
        if (side) { side.addEventListener("scroll", function () { saveScroll(menu, side); }, { passive: true }); }
        menu.addEventListener("click", function () { saveScroll(menu, side); }, true);
    }

    function setUpAll() {
        Array.prototype.forEach.call(document.querySelectorAll(MENUS), setUp);
    }

    setUpAll();
    new MutationObserver(setUpAll).observe(document.documentElement, { childList: true, subtree: true });
})();
