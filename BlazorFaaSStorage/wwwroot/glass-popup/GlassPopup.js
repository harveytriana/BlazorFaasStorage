/*
Author: Harvey triana
USE
1. Resources
   <link href="glass-popup/GlassPopup.css" rel="stylesheet" />
   <script src="glass-popup/GlassPopup.js"></script>

2. Add data-menu="<popupId>" to elements, i.g.
   <img src="picasso.jpg" data-menu="container" />

3. Add the popup HTML, i.g.
<div class="popup">
    <div id="<popupId>" class="popup-content">
        <a href="#" onclick="processSelection(1)">Home</a>
        <a href="#" onclick="processSelection(2)">About</a>
        ...
    </div>
</div>

4. Scripts, 
<script>
    // after page render,
    contextMenuSetup('glassPopup');
    // actions
    function processSelection(x) {
        console.log(x);
    }
</script>
*/
function contextMenuSetup(menuId) {
    let ls = document.querySelectorAll('[data-menu=' + menuId + ']');
    for (let i = 0; i < ls.length; i++) {
        let e = ls[i];
        if (e.dataset.hasMenu) {// prevent douplicate listener
            continue;
        }
        e.dataset.hasMenu = true;
        e.addEventListener("click", function () {
            showMenu(e, menuId);
        }, false);

        document.getElementById(menuId).addEventListener("mouseleave", function () {
            mouseLeave(menuId);
        }, false);
    }
}

function showMenu(elem, menuId) {
    let l = elem.getBoundingClientRect();
    let p = document.getElementById(menuId);

    let y = l.y + 22;
    // right: let x = l.right + 4;

    // p.offsetWidth is 0, on hide

    let x = l.left - 160 + 16;

    p.style.left = x + "px";
    p.style.top = y + "px";
    p.classList.toggle("popup-show");
    // focus first
    setTimeout(function () {// ?
        p.querySelector('a').focus();
    }, 200);
}

function mouseLeave(menuId) {
    let p = document.getElementById(menuId);
    if (p.classList.contains('popup-show')) {
        p.classList.remove('popup-show');
    }
}
