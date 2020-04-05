//# sourceURL=modal-add-artist.js

let aArtists = [
    "Bernes",
    "Buzova",
    "Will Smith",
    "Wamma Watson",
    "Kamberbatch",
    "Dawni jr.",
    "Kirkorov",
    "Pugacheva",
    "Bregneva"
]

$(document).ready(el => {

    fillingList();

    //перемещаем элементы из одного списка в другой
    $(document).on('click', '#selectSingers', function(e) {
        let name = $(e.target).text();
        $('#selectedSingers').append('<li class="list-group-item">' + name + '</li>');
        $(e.target).remove();
    });

    $(document).on('click', '#selectedSingers', function(e) {
        let name = $(e.target).text();
        $('#selectSingers').append('<li class="list-group-item">' + name + '</li>');
        $(e.target).remove();
    });

})

function fillingList() {
    let nSize = aArtists.length;
    for (let i = 0; i < nSize; i++) {
        $('#selectSingers').append('<li class="list-group-item">' + aArtists[i] + '</li>');
    }
}