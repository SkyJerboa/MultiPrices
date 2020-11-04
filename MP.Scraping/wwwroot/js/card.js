let imgs = {
    deleted: [],
    added: []
}

$(document).ready(function () {
    childrenEvents();
    languagesEvents();
    tagsEvents();
    pricesEvents();
    translationsEventes();
    systemRequirementsEvents();
    imageBlockEvents();

    $('#block-service-images div.card-image button.but-del').on('click', (event) => {
        let img = $(event.currentTarget).closest('div.card-image');
        let id = img.find('input[name=id]').val();
        imgs.deleted.push(id);

        img.remove();
    });

    $('#set-complete').on('click', (event) => {
        $.ajax({
            type: 'POST',
            url: '?handler=setconfirm',
            success: function (data, status) {
                window.location.reload();
            }
        });
    });

    let sortable = $('#screenshots');
    if (sortable.length > 0) {
        sortable.sortable();
        sortable.disableSelection();
    }
});

function childrenEvents() {
    const newChildDiv = () => {
        return `
            <div class="chlds-item">
                <span><strong>New Child</strong></span>
                <input name="ChildID" type="text" pattern="[0-9]{4,}" placeholder="id" size="6" title="Значение должно быть цифровым" required>
                <button type="button">Remove</button>
            </div>`
    }

    $('.chlds-item button').on("click", function () {
        this.parentElement.remove();
    });

    $('button.add-chld-item').on("click", function () {
        let newChild = $(newChildDiv()).insertBefore(this);
        newChild.children('button').on("click", function () {
            this.parentElement.remove();
        });
    });
}

function languagesEvents() {
    const langHtml = () => {
        return `
        <div class="card-lang" data-type="dictionary">
            <input type="text" size="3" pattern="[a-z]{2}_[A-Z]{2}" title="Строка должна иметь формат ru_RU" placeholder="ru_RU" data-type="key" required>
            <select name="lang" data-type="value">
                <option value="0">Unknovn</option>
                <option value="1">Voice</option>
                <option value="2">Text</option>
                <option value="3">All</option>
            </select>
            <button type="button" class="card-lang-delete-button">X</button>
        </div>`
    }

    $('.card-lang-delete-button').on('click', (event) => {
        $(event.currentTarget).parent().remove();
    });

    $('#lang-add-but').on('click', (event) => {
        $(event.currentTarget).closest('.card-item').append(langHtml)
            .find('.card-lang-delete-button').on('click', (event) => {
                $(event.currentTarget).parent().remove();
            });
    });
}

function tagsEvents() {
    const tagHtml = (id, name) => {
        return `
        <div class="card-tag">
            <div>${name}</div>
            <input type="hidden" name="Tag" value="${id}"/>
            <button type="button" class="card-tag-delete-but">X</button>
        </div>`;
    }

    let inputField = $('#dialog-window input')[0];
    if (!inputField)
        return;

    autocomplete(inputField, Object.keys(tags));

    $('.card-tag-delete-but').on('click', (event) => {
        $(event.currentTarget).closest('.card-tag').remove();
    });

    $('#tag-add-but').on('click', (event) => {
        $('#dialog-window-back').addClass('show');
    });

    $('#dialog-window-back').on('click', (event) => {
        if (event.target.id != 'dialog-window-back' && event.target.tagName != 'BUTTON')
            return;

        $(event.currentTarget).removeClass('show');
    });

    $('#tag_window_submit').on('click', (event) => {
        let val = inputField.value;
        if (val && tags[val]) {
            $('.card-tags').append(tagHtml(tags[val], val))
                .find('.card-tag-delete-but').on('click', (event) => {
                    $(event.currentTarget).closest('.card-tag').remove();
                });
        }
    });
}

function pricesEvents() {
    const pi = $('#price-infos');

    const priceDiv = `<div class="price-flex one-line-flex" data-type="object">
            <div>Child</div>
            <input type="hidden" name="ID">
            <div>
                Price:<input name="CurrentPrice" type="text" size="8" placeholder="Price" required>
            </div>
            <div>
                Discount:<input name="Discount" type="text" size="4" placeholder="Discount" required>
            </div>
            <div>
                Grab Date:<input name="ChangingDate" type="datetime-local" size="20" placeholder="Changing date" required>
            </div>
            <div>
                <button class="but-del-price" type="button">X</button>
            </div>
        </div>`;

    delPrice = (event) => $(event.currentTarget).closest('div.price-flex').remove();
    addPrice = (event) => {
        let newItem = $(priceDiv).insertBefore($(event.currentTarget));
        newItem.find('button.but-del-price').on('click', delPrice);
    }

    pi.find('button.but-del-price').on('click', delPrice);
    pi.find('button.but-add-price').on('click', addPrice);


    const priceInfosId = 'price-infos';
    const butIds = 'price-info';

    const firstInput = $('#price-info-actions input');
    const secondInput = $('#price-info-actions select');

    const itemName = 'price-country';
    const subItemName = 'price-currency';

    const onAddAction = (newItem) => {
        newItem.find('button.but-del-price').on('click', delPrice);
        newItem.find('button.but-add-price').on('click', addPrice);
    };

    let subItemPrint = (itemValue, subItemValue) => {
        return `<div class="price-info" id="${itemValue}-${subItemValue}">
            <div class="price-flex one-line-flex" data-type="object">
                <input type="hidden" name="Type" value="PriceInfo" />
                <input type="hidden" name="ID" value="0">
                <input type="hidden" name="CountryCode" value="${itemValue}">
                <input type="hidden" name="CurrencyCode" value="${subItemValue}">
                <div>
                    Full Price:<input name="FullPrice" type="text" size="8" placeholder="Full Price" required>
                </div>
                <div>
                    Current Price:<input name="CurrentPrice" type="text" size="8" placeholder="Current Price" required>
                </div>
                <div>
                    Discount:<input name="Discount" type="text" size="3" placeholder="Discount" required>
                </div>
                <div>
                    Is Free:<input name="IsFree" type="checkbox">
                </div>
                <div>
                    Preorder:<input name="IsPreorder" type="checkbox">
                </div>
                <div>
                    IsAvailable:<input name="IsAvailable" type="checkbox">
                </div>
                <div>
                    URL:<input name="GameLink" type="text" size="20" placeholder="Game URL" required>go</a>
                </div>
            </div>
                ${priceDiv}
            <button type="button" class="but-add-price" style="margin-left: 2em;" onclick="${addPrice}">Add</button>
        </div>`
    }

    selectedLi(priceInfosId, butIds, firstInput, secondInput, itemName, subItemName, subItemPrint, onAddAction);
}

function translationsEventes() {
    const translations = $('#translations');
    const transSelect = $('#translations-actions select');

    const transLiHtml = (langCode) => {
        return `
            <li>
                <input id="li-lang-${langCode}" name="langCode" type="radio" value="${langCode}">
                <label for="li-lang-${langCode}">${langCode.toUpperCase()}</label>
            </li>`
    }
    const transDivHtml = (langCode) => {
        return `
            <div id="${langCode}">
            <div class="card-params">
                <div data-type="object">
                    <div>Name:</div>
                    <input name="LanguageCode" type="hidden" value="${langCode}" />
                    <input name="Key" type="hidden" value="game_name" />
                    <input name="Value" type="text">
                </div>
            </div>
            <div class="card-params">
                <div data-type="object">
                    <div>Desctiption:</div>
                    <input name="LanguageCode" type="hidden" value="${langCode}" />
                    <input name="Key" type="hidden" value="game_description" />
                    <textarea name="Value" cols="99" rows="25"></textarea>
                </div>
            </div>
        </div>`
    }
    const transOption = (langCode) => {
        return `
            <option value="${langCode}">${langCode.toUpperCase()}</option>`
    }

    translations.find('input[type=radio]').change(showLiItem);

    $('#translations-del-button').on('click', (event) => {
        const trans = translations.children('ul').find('input:checked');
        const transCode = trans.val();

        if (!transCode)
            return;

        trans.parent().remove();
        $(`#${transCode}`).remove();

        transSelect.append(transOption(transCode));

        const langClick = translations.children('ul').find('input').first();
        if (langClick)
            langClick.click();
    });

    $('#trans-add-button').on('click', (event) => {
        const langCode = transSelect.val();

        if (!langCode)
            return;

        translations.find('ul').append(transLiHtml(langCode));
        translations.append(transDivHtml(langCode));

        transSelect.find(`option[value=${langCode}]`).remove();

        const newTrans = translations.find(`ul input[value=${langCode}]`);
        newTrans.on('click', showLiItem);
        newTrans.click();
    });
}

function systemRequirementsEvents() {
    const sysReqId = 'system-requirements';
    const butIds = 'sys-req';

    const firstInput = $('#sys-req-actions select[name=system]');
    const secondInput = $('#sys-req-actions select[name=system-type]');

    const itemName = 'sys-os';
    const subItemName = 'sys-type';

    const sysReqTypeDivHtml = (os, type) => {
        return `
            <div class="card-params" id="${os}-${type}" data-type="object">
                <input type="hidden" name="SystemType" value="${os}"/>
                <input type="hidden" name="Type" value="${type}"/>
                <div><div>Operating System:</div> <input name="OS" type="text"></div>
                <div><div>Processor:</div> <input name="CPU" type="text"></div>
                <div><div>Graphic:</div> <input name="GPU" type="text"></div>
                <div><div>RAM:</div> <input name="RAM" type="text" maxlength="10"></div>
                <div><div>Hard Drive:</div> <input name="Storage" type="text" maxlength="10"></div>
                <div><div>DirectX:</div> <input name="DirectX" type="text" maxlength="20"></div>
                <div><div>Sound:</div> <input name="Sound" type="text"></div>
                <div><div>Network:</div> <input name="Network" type="text"></div>
                <div><div>Other:</div> <input name="Other" type="text"></div>
            </div>`
    }

    selectedLi(sysReqId, butIds, firstInput, secondInput, itemName, subItemName, sysReqTypeDivHtml);
}

function imageBlockEvents() {
    const noImgUrl = '/images/no-image.png';
    const createCardImg = (src) => {
        return `
            <div class="card-image">
                <input type="hidden" name="id" value="0">
                <div>Screenshot</div>
                <img class="card-img-added" class="zoomD" src="${src}">
                <div>
                    <button type="button" class="but-img-delete">Delete</button>
                </div>
            </div>`
    }

    $('.but-img-replace').on('click', (event) => {
        $(event.currentTarget).parent().find('input').click();
    });

    $('.card-image input[type=file]').change((event) => {
        let input = event.currentTarget;
        if (input.files && input.files[0]) {
            var reader = new FileReader();

            reader.onload = function (e) {
                $(input).closest('.card-image').find('img').attr('src', e.target.result);
            }

            reader.readAsDataURL(input.files[0]);
        }
    });

    $('.but-img-clear').on('click', (event) => {
        let cardImg = $(event.currentTarget).closest('.card-image');
        let img = cardImg.find('img');
        let id = cardImg.find('input[name=id]').val();

        img.attr('src', noImgUrl);
        imgs.deleted.push(id);
    });

    $('.but-img-delete').on('click', (event) => {
        let id = $(event.currentTarget).closest('div.card-image').find('input[name=id]').val();
        if (id != 0)
            imgs.deleted.push(id);

        $(event.currentTarget).closest('li').remove();
    });

    $('#add-screenshoot-but').on('click', (event) => {
        let input = $(event.currentTarget).parent().find('input[type=file]');
        input.click();
    });

    $('#add-img-div input[type=file]').change((event) => {
        let input = event.currentTarget;
        let butDiv = $('#add-img-div');
        if (input.files) {
            for (let file of input.files) {
                var reader = new FileReader();

                reader.onload = function (e) {
                    let newDiv = $(createCardImg(e.target.result)).insertBefore(butDiv);
                    newDiv.find('button').on('click', (event) => {
                        $(event.currentTarget).closest('.card-image').remove();
                        const index = imgs.added.indexOf(file);
                        if (index > -1) {
                            imgs.added.splice(index, 1);
                        }
                    });
                }

                reader.readAsDataURL(file);
                imgs.added.push(file);
            }
        }
    });
}

function saveData(blockName) {
    let obj = {}

    switch (blockName) {
        case 'children':
            obj = getFormData($('#block-children input'));
            break;
        case 'game-info':
            obj.Game = getFormData($('#block-game-info input, #block-game-info select'));
            break;
        case 'languages':
            obj.Languages = getFormDictionay($('#block-languages *[data-type=dictionary]'));
            break;
        case 'tags':
            obj = getFormData($('#block-tags div.card-tags input'));
            break;
        case 'price-infos':
            let priceInfos = getFormObjectsArray($('#block-price-infos div[data-type=object]'));
            let pi;
            obj.PriceInfos = [];
            priceInfos.map((item) => {
                if (item.Type == 'PriceInfo') {
                    pi = item;
                    pi.Prices = [];
                    obj.PriceInfos.push(pi);
                    return;
                }

                pi.Prices.push(item);
            });
            break;
        case 'translations':
            obj.Translations = getFormObjectsArray($('#block-translations div[data-type=object]'));
            break;
        case 'system-requirements':
            obj.SystemRequirements = getFormObjectsArray($('#block-system-requirements div[data-type=object]'));
            break;
        case 'images':
            saveImages();
            return;
        case 'service-images':
            obj.added = imgs.added;
            obj.deleted = imgs.deleted;
            break;
    }

    obj.type = blockName;
    console.log(obj);
    sendAjax(obj);
}

function saveImages() {
    let fd = new FormData();
    let imgsBlock = $('#block-images');

    const addImg = (img, tag) => {
        if (img)
            fd.append(tag, img);
    }

    imgs.added.map((item) => {
        fd.append('screenshot', item);
    })
    fd.append('remove', imgs.deleted);

    addImg(imgsBlock.find('div[data-tag=logo-vertical] input')[0].files[0], 'logo-vertical');
    addImg(imgsBlock.find('div[data-tag=logo-horizontal] input')[0].files[0], 'logo-horizontal');
    addImg(imgsBlock.find('div[data-tag=logo] input')[0].files[0], 'logo');
    addImg(imgsBlock.find('div[data-tag=header-long] input')[0].files[0], 'header-long');

    let newOrder = [];
    imgsBlock.find('#screenshots li').map((index, item) => {
        newOrder.push($(item).find('input[name=id]').val());
    });
    fd.append('order', newOrder);

    $.ajax({
        type: 'POST',
        url: '',
        data: fd,
        processData: false,
        contentType: false,
        success: function (data, status) {
            window.location.reload();
        },
        error: function (jqXhr, textStatus, errorMessage) {
            console.log(textStatus);
            console.log(errorMessage);
        }
    });
}

function selectedLi(blockId, butId, firstinput, secondInput, itemName, subItemName, subItemPrint, onAddAction) {
    const block = $(`#${blockId}`);

    const groupedHtml = (id) => {
        return `
            <div id="${id}">
                <ul class="horizontal-radio">
                </ul>
            </div>`
    }
    const itemLiHtml = (itemType) => {
        return `
        <li>
            <input type="radio" checked="checked" value="${itemType}" name="${itemName}" id="item-${itemType}">
            <label for="item-${itemType}">${itemType}</label>
        </li>`
    }
    const subItemLiHtml = (itemType, subItemValue) => {
        return `
            <li>
                <input type="radio" checked="checked" value="${itemType}-${subItemValue}" name="${subItemName}-${itemType}" id="subItem-${itemType}-${subItemValue}">
                <label for="subItem-${itemType}-${subItemValue}">${subItemValue.toUpperCase()}</label>
            </li>`;
    }

    block.find('input[type=radio]').change(showLiItem);

    $(`#${butId}-del-button`).on('click', (event) => {
        const ulItem = block.children('ul').find('input:checked');
        const itemValue = ulItem.val();
        if (!itemValue)
            return;

        const itemDiv = block.find(`#${itemValue}`);
        const subitemUl = itemDiv.find('ul input:checked');
        const typeValue = subitemUl.val();
        if (!typeValue)
            return;

        itemDiv.find(`#${typeValue}`).remove();
        subitemUl.closest('li').remove();
        if (itemDiv.children().length == 1) {
            itemDiv.remove();
            ulItem.closest('li').remove();
        }

    });

    $(`#${butId}-add-button`).on('click', () => {
        const itemValue = firstinput.val();
        const subItemValue = secondInput.val();

        if (itemValue == 'none' || subItemValue == 'none')
            return;

        let item = $(`#${itemValue}`);

        if (item.length == 0) {
            block.children('ul').append(itemLiHtml(itemValue));
            block.append(groupedHtml(itemValue));
            let itemLi = block.find(`ul>li>input[value=${itemValue}]`);
            itemLi.on('click', showLiItem);
            itemLi.click();
            item = $(`#${itemValue}`);
        } else {
            block.find(`ul li>input[value=${itemValue}]`).click();
        }

        let type = item.find(`#${itemValue}-${subItemValue}`);
        if (type.length == 0) {
            item.find('ul').append(subItemLiHtml(itemValue, subItemValue));
            let newItem = item.append(subItemPrint(itemValue, subItemValue));

            if (onAddAction)
                onAddAction(newItem);

            let typeLi = item.find(`ul>li>input[value=${itemValue}-${subItemValue}]`);
            typeLi.on('click', showLiItem);
            typeLi.click();
        } else {
            item.find(`ul>li>input[value=${itemValue}-${subItemValue}]`).click();
        }
    });
}

function showLiItem(event) {
    let value = event.currentTarget.value;
    $(event.target).closest("div").children().each((index, item) => {
        item = $(item);
        if (item.is('div')) {
            if (item.attr('id') == value)
                item.attr('hidden', false);
            else
                item.attr('hidden', true);
        }
    })
}

function getFormData($form) {
    let unindexed_array = $form.serializeArray();
    getUnchackedInput($form).map((index, item) => { unindexed_array.push(item) });
    let indexed_array = {};

    $.map(unindexed_array, function (n, i) {
        let val = n['value'];
        let name = n['name'];
        if (val != undefined && val !== '')
            indexed_array[name] = (indexed_array[name]) ? indexed_array[name] + ',' + val : val;
    });

    return indexed_array;
}

function getFormArray($form) {
    let unindexed_array = $form.serializeArray();
    getUnchackedInput($form).map((index, item) => { unindexed_array.push(item) });
    let indexed_array = [];

    unindexed_array.forEach(function (child) {
        let obj = {};
        obj[child.name] = child.value;
        indexed_array.push(obj);
    });

    return indexed_array;
}

function getUnchackedInput($form) {
    const unchecked = $form
        .filter((index, item) => item.type == 'checkbox' && !item.checked && item.value == 'true');
    return unchecked.map((index, item) => { return { name: item.name, value: false } });
}

function getFormDictionay($form) {
    let dictionary = {};
    $form.map((index, item) => {
        let key = $(item).find('*[data-type=key]').val();
        let val = $(item).find('*[data-type=value]').val();
        dictionary[key] = val;
    });

    return dictionary;
}

function getFormObjectsArray($form) {
    let array = [];
    $form.map((index, item) => {
        let obj = getFormData($(item).find('input, select, textarea'));
        array.push(obj);
    });

    return array;
}

function sendAjax(data) {
    $.ajax({
        type: 'POST',
        url: '',
        data: JSON.stringify(data),
        dataType: 'json',
        contentType: 'application/json',
        success: function (data, status) {
            if (data.success) {
                window.location.reload();
            } else {
                $('#exceptionModalWindow .modal-body').html(data.error);
                $('#exceptionModalWindow').modal('show');
            }
        },
        error: function (jqXhr, textStatus, errorMessage) {
            console.log(textStatus);
            console.log(errorMessage);

            $('#exceptionModalWindow .modal-body').html(errorMessage);
            $('#exceptionModalWindow').modal('show');
        }
    });
}