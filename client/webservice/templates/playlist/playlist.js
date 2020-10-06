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

    $('#archiveTable').DataTable({
        paging: false,
        scrollY: 400
    });

//если у нас есть кука плейлиста, то открываем вкладку которая прописана в куке
//иначе и по дефолту открываем вкладку onAir
aStatuses = [{nID: 1, sName: "Запланировано"},
{nID: 2, sName: "Поставлено в очередь"},
{nID: 3, sName: "Подготовлено"},
{nID: 4, sName: "В эфире"},
{nID: 5, sName: "Проиграно"},
{nID: 6, sName: "Пропущено"},
{nID: 7, sName: "С ошибкой"}]

    if (Cookies.Get('plFoder')) {
        let plFolder = Cookies.Get('plFoder');
        dtStart = new Date();
        switch (plFolder) {
            case 'Archive':
                dtToday = new Date();
                PL.Items.ArchiveList(dtStart, dtToday, o => {
                    alert('воу! щас будем смотреть архив!');
                })
                break;
            case 'Plan':
                dtToday = new Date();
                PL.Items.PlannedList(dtStart, dtToday, o => {
                    alert('воу! щас будем смотреть план!');
                })
                break;
            default:
                dtToday = new Date();
                PL.Items.List(aStatuses, o => {
                    alert('воу! щас будем смотреть то, что в эфире!');
                })
        }
    } else {
        dtToday = new Date();
        // API.MAM.Statuses.List (aStatuses => {
            API.PL.Items.List(aStatuses, o => {
                alert('воу! щас будем смотреть то, что в эфире!');
            })  
        // })

    }

});
