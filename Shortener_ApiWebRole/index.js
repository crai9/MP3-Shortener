$(document).ready(function () {

    $.get("/api/Samples").done(
        function (data) {
            console.log(data);
        });

});