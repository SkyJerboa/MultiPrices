$(document).ready(function() {
    $('div.change-list-item .button-apply').on('click', function() {
        changeAction('apply', this.parentElement);
    });

    $('div.change-list-item .button-cancel').on('click', function() {
        changeAction('cancel', this.parentElement);
    });
});

function changeAction(action, item) {
    let changeId = $(item).find('input').first().val();
    let params = {
        action: action,
        id: changeId
    };
    
    $.ajax({
        type: 'POST',
        url: '/relationchanges',
        data: params,
        success: function() {
            window.location.reload();
        }
    });
}