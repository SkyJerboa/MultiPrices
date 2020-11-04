$(() => {
    $('#block-game-info .apply-to-children').on('click', (event) => {
        let param = $(event.currentTarget).closest('div').find('input');
        let name = param[0].name;
        let data = {
            type: 'appy-to-children',
            parameter: name
        };
        sendApply(data);
    });

    $('#block-tags img.send-side-but').on('click', (event) => {
        let data = {
            type: 'appy-to-children',
            parameter: 'Tags'
        };
        sendApply(data);
    });

    $('#block-languages img.send-side-but').on('click', (event) => {
        let data = {
            type: 'appy-to-children',
            parameter: 'Languages'
        };
        sendApply(data);
    });

    $('#block-system-requirements img.send-side-but').on('click', (event) => {
        let data = {
            type: 'appy-to-children',
            parameter: 'SystemRequirements'
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