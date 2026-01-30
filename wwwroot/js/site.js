// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Session keep-alive: Ping server every 4 minutes to prevent session timeout
setInterval(function() {
    fetch('/Home/Index', { 
        method: 'HEAD',
        cache: 'no-cache'
    }).catch(function() {
        // Silently ignore errors
    });
}, 240000); // 4 minutes
