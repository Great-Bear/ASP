// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function isValidForm(e) {
    if (e.parentElement.querySelector("#comment").value.length == 0) {
        alert("Поле не может быть пустым");
        return false
    };
}