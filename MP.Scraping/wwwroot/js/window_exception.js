var excWindow;
var excTextNode;
$(document).ready(function() {
    excWindow = $('#exception-window');
    excTextNode = $(excWindow).find('.exception-window-text');

    $(excWindow).find('#exception-window-close-button').on('click', function() {
        excWindow.hide();
    });
});

function show(id) {
    let params = {
        action: 'getExc',
        id: id
    };

    $.ajax({
        method: 'GET',
        data: params,
        success: function(response) {
            excTextNode.text(response);
            excWindow.show();
        }
    });
}