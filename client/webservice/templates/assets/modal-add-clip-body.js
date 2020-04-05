//# sourceURL=modal-add-clip-body.js


$(document).ready(el => {

    // для переноса данных из окна выбора артистов
    if (Cookies.Get("artistList")) {
        let aString = Cookies.Get("artistList");
        let aArtistsList = [];
        aArtistsList = aString.split(",");
        let nSize = aArtistsList.length;
        for (let i = 0; i < nSize; i++) {
            $('#selectSingers').append('<li class="list-group-item">' + aArtistsList[i] + '</li>');
        }
        Cookies.Delete("artistList");
    }

    //добавляем в строку артистов артистов из списка
    $(document).on('click', '#selectSingers', function(e) {
        let name = $(e.target).text();

        let str = $('#inputGroupArtist').val();
        str = (str !== "") ? str + ' & ' + name : name;
        $('#inputGroupArtist').val(str);
    });

    //btn change content in modal window
    $('#btnFileBrowse').click(el => {
        // open something
        $('.modal-body').empty();
        $('.modal-footer').empty();
        $('#addClipTitle').text('Choose file');

        // load template assets-media
        Templates.Attach('assets/assets-media.html', $('.modal-body'), function() {});

        //add cancel button to footer modal window if tables loaded from modal
        $('<input class="btn btn-outline-secondary" type="button" value="Cancel" id="cancelMediaButton">').appendTo('.modal-footer');
        $('#cancelMediaButton').click(function() {
            $('#modalAddClip').modal('hide');
        });


        //add button to footer modal window if tables loaded from modal
        $('<input class="btn btn-primary" type="button" value="Select" id="selectMediaButton">').appendTo('.modal-footer');
        $('#selectMediaButton').click(function() {
            // тут надо запомнить выбор в куки

            $('.modal-body').empty();
            $('.modal-footer').empty();
            $('#addClipTitle').text('Add Clip');

            Templates.Attach('assets/modal-add-clip-body.html', $('.modal-body'), function() {});
            Templates.Attach('assets/modal-add-clip-footer.html', $('.modal-footer'), function() {});
        });
    })

    //выбрать артистов - меняем модальное окно
    $('#btnAddArtist').click(el => {
        $('.modal-body').empty();
        $('.modal-footer').empty();
        $('#addClipTitle').text('Исполнители');
        // load template assets-media
        Templates.Attach('assets/modal-add-artist.html', $('.modal-body'), function() {});

        //add cancel button to footer modal window if tables loaded from modal
        $('<input class="btn btn-outline-secondary" type="button" value="Cancel" id="cancelMediaButton">').appendTo('.modal-footer');
        $('#cancelMediaButton').click(function() {
            $('#modalAddClip').modal('hide');
        });
        //add button to footer modal window if tables loaded from modal
        $('<input class="btn btn-primary" type="button" value="Select" id="selectArtistButton">').appendTo('.modal-footer');
        $('#selectArtistButton').click(function() {
            let aArtists = [];
            $('#selectedSingers li').each(function(index, el) {
                aArtists[index] = el.innerHTML;
            });
            // тут надо запомнить данные в куки
            Cookies.Set("artistList", aArtists, 1);
            $('.modal-body').empty();
            $('.modal-footer').empty();
            $('#addClipTitle').text('Add Clip');

            Templates.Attach('assets/modal-add-clip-body.html', $('.modal-body'), function() {});
            Templates.Attach('assets/modal-add-clip-footer.html', $('.modal-footer'), function() {});

        });
    })

})