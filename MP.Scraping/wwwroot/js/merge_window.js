$(document).ready(function() {
    $('#merge_button').on('click', function() {
        if ($('main.main input:checked').length < 1)
            return;

        if ($('#merge_window').is(":visible")) {
            findAndClearWindow();
            return;
        }

        var selList = $('#merge_window select');
        $('main.main input:checked').each(function(index, item) {
            let val_name = $(item.parentElement).find('.list-game-name')[0].innerText;
            let id = $(item.parentElement).find('input:hidden')[0].value;
            let services = $(item.parentElement).find('.list-game-services')[0].innerText;
            selList.append(`<option value="${id}">
                            ${id}-${services}-${val_name}
                            </option>`);

            $('#merge_window').css('display', 'flex');
        });
    });

    $('#merge_window button').on('click', function() {
        let mergedGames = [],
            parentGame;
        $(this).parent().find('select option').each(function(index, item) {
            if (item.selected)
                parentGame = item.value;
            else
                mergedGames.push(item.value);
        });

        $.ajax({
            type: 'POST',
            url: '/games',
            data: {
                parent: parentGame,
                merged: mergedGames
            }
        });

        findAndClearWindow();
    });
});

function findAndClearWindow() {
    let window = $('#merge_window');
    window.hide();
    window.find('select option').remove();
}