//# sourceURL=modal-add-clip.js

$(document).ready(function() {

    $('.modal-body').empty();
    $('.modal-footer').empty();
    $('#addClipTitle').text('Add Clip');

    Templates.Attach('assets/modal-add-clip-body.html', $('.modal-body'), function() {});
    Templates.Attach('assets/modal-add-clip-footer.html', $('.modal-footer'), function() {});


})