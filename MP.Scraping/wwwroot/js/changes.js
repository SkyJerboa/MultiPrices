$(document).ready(function() {
    $('#block-changes .changes-flex').find('button.button-change-apply, button.button-change-remove').on('click', function() {
        let changeId = $(this).closest('div.changes-flex').find('input[name="changeId"]').first().val();
        let fieldName = $(this).closest('div.changes-flex').find('input[name="fieldName"]').first().val();
        let data = {
            id: changeId,
            fieldName: fieldName,
            action: (this.classList.contains('button-change-apply')) ? 'Apply' : 'Remove'
        };
        applyChange(data);
    });

    $('#apply-change-button, #remove-change-button').on('click', function() {
        let block = $('#block-changes');
        let gameId = block.find('input[name=gameId]').val();
        let serviceCode = block.find('input[name=serviceCode]').val();
        let data = {
            gameId: gameId,
            serviceCode: serviceCode,
            action: (this.id == 'apply-change-button') ? 'Apply' : 'Remove'
        };
        applyChange(data);
    });
});

function applyChange(params) {
    $.ajax({
        type: 'POST',
        url: '/changes',
        data: params,
        success: function() {
            window.location.reload();
        }
    });
}