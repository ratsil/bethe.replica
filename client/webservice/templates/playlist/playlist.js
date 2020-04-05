//# sourceURL=playlist.js

$(document).ready(function () {

    // кнопка Import
    $('[name=playlistImport]').click(function () {
        $('#playListImport').modal('show');
    });

    // кнопка Insert
    $('[name=insertAssets]').click(function () {
        $('#insertAsset').modal('show');
    });

});
