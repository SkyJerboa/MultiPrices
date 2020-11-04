$(function() {
    $('#pin_button').on('click', () => doAction('pin'));
    $('#unpin_button').on('click', () => doAction('unpin'));
    $('#add_button').on('click', () => doAction('add'));
    $('#delete_button').on('click', () => doAction('delete'));

    $('#dialog-window-back').on('click', (event) => {
        if (event.target.id != 'dialog-window-back' && event.target.tagName != 'BUTTON')
            return;

        $(event.currentTarget).removeClass('show');
    });
});

function doAction(action) {
    let params, selectedTags, inputField;
    let selectedTagsArr = [];
    let dWindow = ('#dialog-window-back');

    switch (action) {
        case "pin":
            $(dWindow).addClass('show');

            selectedTags = $('#tags>div.main-tag>input:checked');
            let notSelectedTags = $('#tags>div.main-tag>input:not(:checked');
            let notSelectedTagsArr = [];

            notSelectedTags.each((i, item) => {
                notSelectedTagsArr.push(item.value);
            });
            selectedTags.each((i, item) => {
                selectedTagsArr.push(item.value);
            });

            inputField = $('#tag_in_window')[0];
            autocomplete(inputField, notSelectedTagsArr);

            $('#tag_window_submit').on('click', () => {
                params = {
                    mainTag: inputField.value,
                    pinTags: selectedTagsArr
                };

                sendAjax(action, params);
            });
            break;
        case 'unpin':
            selectedTags = $('#tags>div.main-tag>div.sub-tags input:checked');

            selectedTags.each((i, item) => {
                selectedTagsArr.push(item.value);
            });

            params = { unpinTags: selectedTagsArr };
            sendAjax(action, params);

            break;
        case 'add':
            $(dWindow).show();

            inputField = $('#tag_in_window')[0];

            $('#tag_window_submit').on('click', () => {
                params = { tag: inputField.value };
                sendAjax(action, params);
            });
            break;
        case 'delete':
            let selectedMainTags = $('#tags>div.main-tag>input:checked');
            let selectedSubTags = $('#tags>div.main-tag>div.sub-tags input:checked');
            let selectedMainTagsArr = [];
            let selectedSubTagsArr = [];

            selectedMainTags.each((i, item) => {
                selectedMainTagsArr.push(item.value);
            });
            selectedSubTags.each((i, item) => {
                selectedSubTagsArr.push(item.value);
            });

            params = {
                mainTags: selectedMainTagsArr,
                subTags: selectedSubTagsArr
            };
            sendAjax(action, params);

            break;
    }
};

function sendAjax(action, params) {

    params.action = action;

    $.ajax({
        type: 'POST',
        data: params,
        success: (response) => {
            if (!response.success)
                alert(response.error);
            else
                window.location.reload();
        }
    });
};