//# sourceURL=assets-assets.js

$(document).ready(el => {

    // генерируем таблицу клипов для первой загрузки
    loadClipsInTable();

    // При клике на табе подгружаем из базы таблицу соответствующих ассетов
    $('#assetsTab a').on('click', function(e) {
        e.preventDefault();
        let sId = $(this).attr('id');
        switch (sId) {
            case 'navAllTab':
                loadAllAssets();
                break;
            case 'navClipTab':
                loadClipsInTable();
                break;
            case 'navAdTab':
                loadAdvertisementInTable();
                break;
            case 'navProgramsTab':
                loadProgramsInTable();
                break;
            case 'navDesignTab':
                loadDesignInTable();
                break;
        }
        $(this).tab('show');
    })

    // btn Add Clip
    $('#addClip').click(function() {
        let ui = $('#modal-window');
        Templates.Attach('assets/modal-add-clip.html', false, ui, function() {
            $('#modalAddClip').modal('show');
        })
    });

    // btn Add Advertisement
    $('#addAd').click(function() {
        $('#modalAddAd').modal('show');
    });

    // btn Add Program
    $('#addProgram').click(function() {
        $('#modalAddProgram').modal('show');
    });

    // btn Add Design
    $('#addDesign').click(function() {
        $('#modalAddDesign').modal('show');
    });

    // при клике на строку в таблице 
    // запоминаем объект строки в куки или берем ID из первого td
    // по ID делаем запрос данных
    // вызываем модальное окно и заполняем поля данными объекта
    $(document).on('click', '#tableAssetClips tr', function(e) {
        let parent = e.target.parentNode;
        let nId = parseInt(parent.firstElementChild.innerHTML);
        API.MAM.Clips.Get(nId, function(oResponce) {
                let oSomething = oResponce;
                let ui = $('#modal-window').empty();
                Templates.Attach('assets/modal-add-clip.html', false, ui, function() {
                    $('#modalAddClip').modal('show');
                })
            })
            // let str = $('#inputGroupArtist').val();
            // str = (str !== "") ? str + ' & ' + name : name;
            // $('#inputGroupArtist').val(str);
    });

})

function loadAllAssets() {
    $('#tableAssetsAll').empty();
    API.MAM.Assets.List("all", function(oValue) {
        $(oValue).each(function(index, el) {
            $(makeRow(el)).appendTo('#tableAssetsAll');
        });
    });
}

function loadClipsInTable() {
    $('#tableAssetClips').empty();
    API.MAM.Clips.List(function(oValue) {
        $(oValue).each(function(index, el) {
            $(makeRow(el, true)).appendTo('#tableAssetClips');
        });
    })
}

function loadAdvertisementInTable() {
    $('#tableAssetsAdvertisement').empty();
    API.MAM.Advertisements.List(function(oValue) {
        $(oValue).each(function(index, el) {
            $(makeRow(el)).appendTo('#tableAssetsAdvertisement');
        });
    })
}

function loadProgramsInTable() {
    $('#tableAssetsPrograms').empty();
    API.MAM.Programs.List(function(oValue) {
        $(oValue).each(function(index, el) {
            $(makeRow(el)).appendTo('#tableAssetsPrograms');
        });
    })
}

function loadDesignInTable() {
    $('#tableAssetsDesign').empty();
    API.MAM.Designs.List(function(oValue) {
        $(oValue).each(function(index, el) {
            $(makeRow(el)).appendTo('#tableAssetsDesign');
        });
    })
}

function makeRow(el, bRot) {
    let row;
    let nID = el.nID;
    let sName = el.sName;
    let sDuration = getDuration(el.nFramesQty);
    let sFile, sType, sClass, sRotation;
    try { sFile = el.cFile.sFilename; } catch { sFile = ""; };
    try { sType = el.stVideo.cType.sName; } catch { sType = ""; };
    try { sRotation = el.cRotation.sName; } catch { sRotation = ""; };
    try { sClass = el.aClasses[0].sName; } catch { sClass = ""; };
    // to -> tableAssetClips
    row = "<tr class='tr-control'>";
    row += "<td>" + nID + "</td>";
    row += "<td>" + sName + "</td>";
    row += "<td>" + sFile + "</td>";
    row += "<td>" + sDuration + "</td>";
    row += "<td>" + sType + "</td>";
    if (bRot)
        row += "<td>" + sRotation + "</td>";
    row += "<td>" + sClass + "</td>";
    row += "</tr>";
    return row;
}