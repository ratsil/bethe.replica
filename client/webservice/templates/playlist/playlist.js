//# sourceURL=playlist.js

$(document).ready(function() {


    // кнопка Import
    $('[name=playlistImport]').click(function() {
        $('#playListImport').modal('show');
    });

    // кнопка Refresh - закладка в эфире
    $('#btnPLOnAirRefresh').click(function() {
        loadOnAirInTable();
    });

    // кнопка Refresh - закладка в эфире
    $('#btnPlannedRefresh').click(function() {
        loadPlannedInTable();
    });

    // кнопка Insert
    $('[name=insertAssets]').click(function() {
        $('#insertAsset').modal('show');
    });

    // кнопка Сегодня - закладка архив
    $('#btnArchiveToday').click(function() {
        loadArchiveToday();
    });

    $('#archiveTable').DataTable({
        paging: false,
        scrollY: 400
    });

    //если у нас есть кука плейлиста, то открываем вкладку которая прописана в куке
    //иначе и по дефолту открываем вкладку onAir
    aStatuses = [{ nID: 1, sName: "planned" },
        { nID: 2, sName: "queued" },
        { nID: 3, sName: "prepared" },
        { nID: 4, sName: "onair" },
        { nID: 5, sName: "played" },
        { nID: 6, sName: "skipped" },
        { nID: 7, sName: "failed" }
    ]

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
                PL.Items.List([4], o => {
                    alert('воу! щас будем смотреть то, что в эфире!');
                })
        }
    } else {
        loadOnAirInTable();
    }

    // при клике на строку в таблице 
    // запоминаем объект строки в куки или берем ID из первого td
    // вызываем окно с меню
    $(document).on('click', '#onPlannedTable tr', function(e) {
        let parent = e.target.parentNode;
        let nId = parseInt(parent.firstElementChild.innerHTML);
        positionMenu(e);
        toggleMenuOn();
    });

    /**
     * Listens for keyup events.
     */

    /**
     * Va riables.
     */
    let contextMenuClassName = "context-menu";
    let contextMenuItemClassName = "context-menu__item";
    let contextMenuLinkClassName = "context-menu__link";
    let contextMenuActive = "context-menu--active";
    let menu = document.querySelector("#context-menu");
    let menuItems = menu.querySelectorAll(".context-menu__item");
    let menuState = 0;
    let menuWidth;
    let menuHeight;
    let menuPosition;
    let menuPositionX;
    let menuPositionY;

    let windowWidth;
    let windowHeight;

    function keyupListener() {
        window.onkeyup = function(e) {
            if (e.keyCode === 27) {
                toggleMenuOff();
            }
        }
    }

    /**
     * Window resize event listener
     */
    function resizeListener() {
        window.onresize = function(e) {
            toggleMenuOff();
        };
    }

    /**
     * Turns the custom context menu on.
     */
    function toggleMenuOn() {
        if (menuState !== 1) {
            menuState = 1;
            menu.classList.add(contextMenuActive);
        }
    }

    /**
     * Turns the custom context menu off.
     */
    function toggleMenuOff() {
        if (menuState !== 0) {
            menuState = 0;
            menu.classList.remove(contextMenuActive);
        }
    }

    /**
     * Get's exact position of event.
     * 
     * @param {Object} e The event passed in
     * @return {Object} Returns the x and y position
     */
    function getPosition(e) {
        let posx = 0;
        let posy = 0;

        if (e.pageX || e.pageY) {
            posx = e.pageX;
            posy = e.pageY;
        } else if (e.clientX || e.clientY) {
            posx = e.clientX + document.body.scrollLeft + document.documentElement.scrollLeft;
            posy = e.clientY + document.body.scrollTop + document.documentElement.scrollTop;
        }

        return {
            x: posx,
            y: posy
        }
    }

    /**
     * Positions the menu properly.
     * 
     * @param {Object} e The event
     */
    function positionMenu(e) {
        clickCoords = getPosition(e);
        clickCoordsX = clickCoords.x;
        clickCoordsY = clickCoords.y;

        toggleMenuOn();

        menuWidth = menu.offsetWidth + 4;
        menuHeight = menu.offsetHeight + 4;

        windowWidth = window.innerWidth;
        windowHeight = window.innerHeight;

        if ((windowWidth - clickCoordsX) < menuWidth) {
            menu.style.left = windowWidth - menuWidth + "px";
        } else {
            menu.style.left = clickCoordsX + "px";
        }

        if ((windowHeight - clickCoordsY) < menuHeight) {
            menu.style.top = windowHeight - menuHeight + "px";
        } else {
            menu.style.top = clickCoordsY + "px";
        }
    }

    keyupListener();

});



// таблица закладки OnAir
function loadOnAirInTable() {
    Loader.Show();
    $('#onAirTable').empty();
    let ret = [{ nID: 4, sName: "onair" }, { nID: 3, sName: "prepared" }, { nID: 2, sName: "queued" }];
    API.PL.Items.List(ret, oValue => {
        $(oValue).each(function(index, el) {
            $(makeRow(el)).appendTo('#onAirTable');
        });
        let target = $('#itemsTotal');
        target.empty();
        target.html('<strong>' + oValue.length + ' items total</strong> ');
        Loader.Hide();
    })
}

// таблица закладки Planned
function loadPlannedInTable() {
    Loader.Show();
    $('#onPlannedTable').empty();
    let dtStart = new Date();
    let dtEnd = new Date();
    dtEnd.setDate(dtEnd.getDate() + 1);
    API.PL.Items.PlannedList(dtStart, dtEnd, oValue => {
        $(oValue).each(function(index, el) {
            $(makeRow(el, 'plan')).appendTo('#onPlannedTable');
        });

        let target = $('#itemsPlannedTotal');
        target.empty();
        target.html('<strong>' + oValue.length + ' items total</strong> ');
        Loader.Hide();
    });
}

// таблица закладки Архив
function loadArchiveToday() {
    Loader.Show();
    $('#onArchiveTable').empty();
    let ret = [{ nID: 5, sName: "played" }];
    let dtYesterday = new Date();
    dtYesterday.setDate(dtYesterday.getDate() - 1);
    let dtToday = new Date();
    // dtToday.setDate(dtToday.getDate() - 1);

    API.PL.Items.ArchiveList(dtYesterday, dtToday, oValue => {

        // API.PL.Items.List(ret, oValue => {
        $(oValue).each(function(index, el) {
            $(makeRow(el, 'arh')).appendTo('#onArchiveTable');
        });

        let target = $('#itemsInArchive');
        target.empty();
        target.html('<strong>' + oValue.length + ' items total</strong> ');
        Loader.Hide();
    })
}

// sNameTab - arh или plan
function makeRow(el, sNameTab) {
    let row;
    let nID = el.nID;
    let sName = el.sName;
    let sDuration = getDuration(el.nFramesQty);
    let sFile, sType, sClass, sRotation, dtLastDate, sStatus;
    try { sFile = el.cFile.sFilename; } catch { sFile = ""; };
    try { sType = el.cFile.cStorage.sName; } catch { sType = ""; };
    try { sRotation = el.cAsset.cRotation.sName; } catch { sRotation = ""; };
    try { sClass = el.aClasses[0].sName; } catch { sClass = ""; };
    if (sNameTab == 'arh') {
        try { dtLastDate = getDataTime(el.dtStartReal); } catch { dtLastDate = ""; };
    } else if (sNameTab == 'plan') {
        try { dtLastDate = getDataTime(el.dtStartPlanned); } catch { dtLastDate = ""; };
    } else {
        try { dtLastDate = getDataTime(el.dtStart); } catch { dtLastDate = ""; };
    }
    try { sStatus = el.cStatus.sName; } catch { sStatus = ""; };

    if (sNameTab == 'plan') {
        switch (sType) {
            case 'клипы':
                row = "<tr class='tr-control tr-clips-planned'>";
                break;
            case 'оформление':
                row = "<tr class='tr-control tr-design-planned'>";
                break;
            case 'реклама':
                row = "<tr class='tr-control tr-ad-planned'>";
                break;
            case 'анонсы':
                row = "<tr class='tr-control tr-ad-planned'>";
                break;
            case 'программы':
                row = "<tr class='tr-control tr-prog-planned'>";
                break;
            case 'заставки':
                row = "<tr class='tr-control tr-zastavki-planned'>";
                break;
            default:
                row = "<tr class='tr-control>";
        }
    } else {
        switch (sStatus) {
            case 'onair':
                row = "<tr class='tr-control tr-onair'>";
                break;
            case 'prepared':
                row = "<tr class='tr-control tr-prepared'>";
                break;
            case 'queued':
                row = "<tr class='tr-control tr-queued'>";
                break;
            default:
                row = "<tr class='tr-control'>";
        }
    }
    row += "<td>" + dtLastDate + "</td>";
    // row += "<td>" + nID + "</td>";
    row += "<td>" + sName + "</td>";
    row += "<td>" + sDuration + "</td>";
    row += "<td>" + sStatus + "</td>";
    row += "<td>" + sFile + "</td>";
    row += "<td>" + sClass + "</td>";
    row += "<td>" + sRotation + "</td>";
    switch (sType) {
        case 'клипы':
            row += "<td class='td-clips'>" + sType + "</td>";
            break;
        case 'оформление':
            row += "<td class='td-design'>" + sType + "</td>";
            break;
        case 'реклама':
            row += "<td class='td-ad'>" + sType + "</td>";
            break;
        case 'анонсы':
            row += "<td class='td-ad'>" + sType + "</td>";
            break;
        case 'программы':
            row += "<td class='td-prog'>" + sType + "</td>";
            break;
        case 'заставки':
            row += "<td class='td-zastavki'>" + sType + "</td>";
            break;
    }
    row += "</tr>";
    return row;
}

// преобразует строку из бд в немного другой формат
function getDataTime(sdtDT) {

    let sDT, dtTemp;
    dtTemp = String(sdtDT);
    let nDatePos = dtTemp.indexOf('T');
    let nTimePos = nDatePos + 9;

    sDT = sdtDT.slice(0, nDatePos) + ' ' + sdtDT.slice(nDatePos + 1, nTimePos);

    return sDT;
}



//