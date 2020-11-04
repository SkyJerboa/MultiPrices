$(() => {
    $('#compile-scripts-but').on('click', (event) => {
        sendAction('compileScripts');
    });
    $('#reload-assembly-but').on('click', (event) => {
        sendAction('reloadAssembly');
    });
});

function sendAction(action) {
    $.ajax({
        type: 'POST',
        url: `${window.location.pathname}?type=${action}`,
        dataType: 'json',
        contentType: 'application/json'
    });
}