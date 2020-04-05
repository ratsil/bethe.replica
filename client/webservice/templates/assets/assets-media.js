//# sourceURL=assets-media.js

$(document).ready(el => {

    loadMediaControl();

    //клик по таблице файлов - управление медиа файлами
    $('[data-target=tableMediaControl]').click(function(ev) {
        $('.tr-media-control-active').removeClass('tr-media-control-active');
        $(ev.target.parentNode).addClass('tr-media-control-active');
        let idStorages = $(ev.target).attr('data-id');
        // а тут еще подгружаем таблицу файлов
        loadFilesToTable(idStorages);
    })

    //клик по таблице файлов
    $('.table-media-source tr').click(function() {
        $('.tr-media-source-active').removeClass('tr-media-source-active');
        $(this).addClass('tr-media-source-active');
    })
})

function loadMediaControl() {
    API.Media.Storages.List(function(oValue) {
        $('[data-target=tableMediaControl]').empty();
        let row = "";
        $(oValue).each(function(index, el) {
            let id = el.nID;
            let sName = el.sName;
            let sFTypeName = el.cType.sName;
            row = "<tr>";
            row += "<td data-id=" + id + ">" + sName + "</td>";
            row += "<td data-id=" + id + ">" + sFTypeName + "</td>";
            row += "</tr>";
            $(row).appendTo('[data-target=tableMediaControl]');
            row = "";
        })
    })
}

function loadFilesToTable(sStorageId) {
    let nIdStorages = parseInt(sStorageId);
    API.Media.Files.List(nIdStorages, function(oValue) {
        $('[data-target=tableMediaFile]').empty();
        let row = "";
        Object.keys(oValue).forEach(function(index, el) {
            let nKey = parseInt(index);
            let oVal = oValue[nKey];
            let sFileName = (oVal && oVal.hasOwnProperty('sFile')) ? oVal.sFilename : "";
            let dtModifDate = oVal.dtModification;
            row = "<tr>";
            row += "<td>" + sFileName + "</td>";
            row += "<td>" + dtModifDate + "</td>";
            row += "</tr>";
            $(row).appendTo('[data-target=tableMediaFile]');
            row = "";
        })
    })
}