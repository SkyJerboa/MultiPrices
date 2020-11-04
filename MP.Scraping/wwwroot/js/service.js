$(document).ready(function() {
    $('#run').on('click', function() {
        $.ajax({
            type: 'POST',
            url: window.location.href + '?action=run',
            data: {
                onlyPrice: $('#onlyPrice').prop("checked"),
                isTesting: $('#isTesting').prop("checked"),
                country: $('#country option:selected').val(),
                currency: $('#currency option:selected').val(),
                lang: $('#lang option:selected').val()
            },
            success: function() {
                window.location.reload();
            }
        });
    });
    $('.show-exception-button').on('click', function() {
        show(this.id);
    });
});