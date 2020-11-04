$(() => {
    $('#block-languages .send-side-but').on('click', (event) => {
        let data = {
            type: 'apply-localization'
        };
        sendApply(data);
    });

    $('#block-system-requirements .send-side-but').on('click', (event) => {
        let data = {
            type: 'apply-system-requirements'
        };
        sendApply(data);
    });

    $('#block-service-images img.right-field-but').on('click', (event) => {
        let id = $(event.currentTarget).closest('div.card-image').find('input[name=id]').val();
        let data = {
            type: 'apply-image',
            id: id
        };
        sendApply(data);
    });
});

function sendApply(data) {
    $.ajax({
        type: 'POST',
        url: '',
        data: JSON.stringify(data),
        dataType: 'json',
        contentType: 'application/json'
    });
}