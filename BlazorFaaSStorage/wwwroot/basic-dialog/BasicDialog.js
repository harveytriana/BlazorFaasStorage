/*
Based in Boostrap (no jquery)
Author: Harvey Triana
 */
const _modal = document.getElementById('basic-dialog');
const _backdrop = document.getElementById("backdrop");

export function OpenModal() {
    _backdrop.style.display = "block"
    _modal.style.display = "block"
    _modal.classList.add("show")
}

export function CloseModal() {
    _backdrop.style.display = "none"
    _modal.style.display = "none"
    _modal.classList.remove("show")
}

// When the user clicks anywhere outside of the modal, close it
window.onclick = function (event) {
    if (event.target == _modal) {
        CloseModal();
    }
}