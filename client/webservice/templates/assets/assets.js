//# sourceURL=asset.js

$(document).ready(function() {


    // load template assets-assets
    Templates.Attach('assets/assets-assets.html', $('#collapseAssetsAssets'), function() {});

    $("#collapseAssetsMedia").on('shown.bs.collapse', function() {
        // load template assets-media
        $('#collapseAssetsMedia').empty();
        Templates.Attach('assets/assets-media.html', $('#collapseAssetsMedia'), function() {});
    })

    $("#collapseAssetsPersons").on('shown.bs.collapse', function() {
        // load template assets-persons
        $('#collapseAssetsPersons').empty();
        Templates.Attach('assets/assets-persons.html', $('#collapseAssetsPersons'), function() {});
    })

});