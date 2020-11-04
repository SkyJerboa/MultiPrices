$(() => {
    const imgWindow = $('#add-img-window');

    imgWindow.find('div.added-img-info-div button').on('click', (event) => {
        let parentDiv = $(event.currentTarget).parent();
        parentDiv.find('input').val('');
        parentDiv.find('select').prop('selectedIndex', 0);
    });

    $('#but-add-img-win').on('click', (event) => {
        let newImgDiv = imgWindow.find('div.added-img-info-div').first();
        $(newImgDiv.prop('outerHTML'))
            .insertBefore($(event.currentTarget.parentElement))
            .find('button')
            .on('click', (event) => {
                $(event.currentTarget).parent().remove();
            });
    });

    $('#add-service-imgs-div').on('click', (event) => {
        $('#add-img-window').addClass('show');
    });

    $('#but-save-img-win').on('click', (event) => {
        imgs.added = [];
        imgWindow.find('div.added-img-info-div').map((index, item) => {
            let tag = $(item).find('select').val();
            let url = $(item).find('input').val();

            if (tag && url && tag != 'none') {
                imgs.added.push({
                    tag,
                    url
                });
            }
        });

        $('#new-img-count').text(imgs.added.length);

        imgWindow.removeClass('show');
    });

    $('#add-img-window').on('click', (event) => {
        if (event.target.id == 'add-img-window')
            imgWindow.removeClass('show');
    });
});