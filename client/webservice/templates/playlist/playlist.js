//# sourceURL=playlist.js

$(document).ready(function() {

    aStatuses = [{ nID: 1, sName: "planned" },
        { nID: 2, sName: "queued" },
        { nID: 3, sName: "prepared" },
        { nID: 4, sName: "onair" },
        { nID: 5, sName: "played" },
        { nID: 6, sName: "skipped" },
        { nID: 7, sName: "failed" }
    ]

    let timerID = setInterval(getInfoForPinkString, 1000)

    // Закладка Archive ------------------------------------------------

    // устанавливаем минимум в календаре
    $('#btnArchiveByDate').attr('max', getMaxMinDate);

    // кнопка Сегодня 
    $('#btnArchiveToday').click(function() {
        loadArchiveTable();
    });

    // кнопка Вчера 
    $('#btnArchiveYesterday').click(function() {
        let dtStart = new Date();
        dtStart.setDate(dtStart.getDate() - 2);
        let dtEnd = new Date();
        dtEnd.setDate(dtEnd.getDate() - 1);
        loadArchiveTable(dtStart, dtEnd);
    });

    // кнопка ... дней назад 
    $('#btnArchiveShowDaysAgo').click(function() {
        let nDaysAgo = $('#daysAgo').val();
        let dtStart = new Date();
        dtStart.setDate(dtStart.getDate() - nDaysAgo);
        let dtEnd = new Date();
        dtEnd.setDate(dtEnd.getDate() - (nDaysAgo - 1));
        loadArchiveTable(dtStart, dtEnd);
    });

    // кнопка календарь дней назад 
    $('#btnArchiveByDate').on("change", function() {
        let nCalDay = $('#btnArchiveByDate').val();
        let dtStart = new Date(nCalDay);
        let dtEnd = new Date(nCalDay);
        dtEnd.setDate(dtEnd.getDate() + 1);
        loadArchiveTable(dtStart, dtEnd);
    });

    // кнопка Изменить время выхода
    $('#btnOkNewTime').click(function() {
        $('#moveClipToAnotherTime').modal('hide');
        alert('Изменили время выхода');
    });

    // Закладка OnAir --------------------------------------------------
    // кнопка Import
    $('[name=playlistImport]').click(function() {
        $('#playListImport').modal('show');
    });

    // кнопка Refresh - закладка в эфире
    $('#btnPLOnAirRefresh').click(function() {
        loadOnAirInTable();
    });

    // Закладка Planned ------------------------------------------------

    // устанавливаем минимум в календаре

    $('#datePlanned').attr('min', getMaxMinDate);

    // кнопка Today 
    $('#btnPlannedImport').click(function() {
        $('#insertAsset').modal('show');
    });

    $('#btnPlannedToday').click(function() {
        let dtStart = new Date();
        let dtEnd = new Date();
        dtEnd.setDate(dtEnd.getDate() + 1);
        loadPlannedInTable(dtStart, dtEnd);
    });
    // кнопка Tomorrow
    $('#btnPlannedTomorrow').click(function() {
        let dtStart = new Date();
        dtStart.setDate(dtStart.getDate() + 1);
        let dtEnd = new Date();
        dtEnd.setDate(dtEnd.getDate() + 2);
        loadPlannedInTable(dtStart, dtEnd);
    });
    // кнопка AfterTomorrow
    $('#btnPlannedAfterTomorrow').click(function() {
        let dtStart = new Date();
        dtStart.setDate(dtStart.getDate() + 2);
        let dtEnd = new Date();
        dtEnd.setDate(dtEnd.getDate() + 3);
        loadPlannedInTable(dtStart, dtEnd);
    });

    // кнопка календарь дней назад 
    $('#datePlanned').on("change", function() {
        let nCalDay = $('#datePlanned').val();
        let dtStart = new Date(nCalDay);
        let dtEnd = new Date(nCalDay);
        dtEnd.setDate(dtEnd.getDate() + 1);
        loadPlannedInTable(dtStart, dtEnd);
    });


    //если у нас есть кука плейлиста, то открываем вкладку которая прописана в куке
    //иначе и по дефолту открываем вкладку onAir

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
        }
    } else {
        loadOnAirInTable();
    }

    // клики по закладкам --------------------------------

    $('[data-target="#collapseOne"]').on('click', function() {
        // alert('Клик на ссылке архив');
        CoockiesReStore('Archive');
        loadArchiveTable();
    })

    $('[data-target="#collapseTwo"]').on('click', function() {
        // alert('Клик на ссылке В эфире');
        CoockiesReStore('OnAir');
    })

    $('[data-target="#collapseThree"]').on('click', function() {
        // alert('Клик на ссылке Планы');
        CoockiesReStore('Planned');
        let dtStart = new Date();
        let dtEnd = new Date();
        dtEnd.setDate(dtEnd.getDate() + 1);
        loadPlannedInTable(dtStart, dtEnd);
    })

    // при клике на строку в таблице Planned
    // запоминаем объект строки в куки или берем ID из первого td
    // вызываем окно с меню
    $(document).on('click', '#onPlannedTable tr', function(e) {
        let parent = e.target.parentNode;
        let nId = parseInt(parent.firstElementChild.innerHTML);
        positionMenu(e);
        toggleMenuOn();
    });

    //  ----------------------------------------------------------------- Клики на контекстном меню

    // Передвинуть на другое время
    $('[data-action=PlanMoveTime]').on('click', function() {
            toggleMenuOff();
            $('#moveClipToAnotherTime').modal('show');
            // return false;
        })
        // Вставить ассеты после или добавить блок...
    $('[data-action=PlanEdit]').on('click', function() {
            toggleMenuOff();
            alert('Вставить ассеты после или добавить блок...');
            // $('#moveClipToAnotherTime').modal('show');
            // return false;
        })
        // Удалить
    $('[data-action=PlanDelete]').on('click', function() {
            toggleMenuOff();
            alert('Удалить');

            // $('#moveClipToAnotherTime').modal('show');
            // return false;
        })
        // Удалить ВСЁ что ниже
    $('[data-action=PlanDeleteAllDown]').on('click', function() {
            toggleMenuOff();
            alert('Удалить ВСЁ что ниже');
            // $('#moveClipToAnotherTime').modal('show');
            // return false;
        })
        // Свойства
    $('[data-action=PlanProperty]').on('click', function() {
            toggleMenuOff();
            alert('Удалить ВСЁ что ниже');
            // $('#moveClipToAnotherTime').modal('show');
            // return false;
        })
        // Обновить плейлис
    $('[data-action=PlanRefresh]').on('click', function() {
            toggleMenuOff();
            alert('Обновить плейлис');
            // $('#moveClipToAnotherTime').modal('show');
            // return false;
        })
        // Пересчитать на 5 часов вперед
    $('[data-action=PlanRecalcToFive]').on('click', function() {
            toggleMenuOff();
            alert('Пересчитать на 5 часов вперед');
            // $('#moveClipToAnotherTime').modal('show');
            // return false;
        })
        // Групповое перемещение
    $('[data-action=PlanGroupeMove]').on('click', function() {
            toggleMenuOff();
            alert('Групповое перемещение');
            // $('#moveClipToAnotherTime').modal('show');
            // return false;
        })
        // Копировать на время
    $('[data-action=PlanCopyToTime]').on('click', function() {
            toggleMenuOff();
            alert('Копировать на время');
            // $('#moveClipToAnotherTime').modal('show');
            // return false;
        })
        // Копировать в конец
    $('[data-action=PlanCopyToEnd]').on('click', function() {
            toggleMenuOff();
            alert('Копировать в конец');
            // $('#moveClipToAnotherTime').modal('show');
            // return false;
        })
        // Верстка первоочередного плейлиста
    $('[data-action=PlanMakePL]').on('click', function() {
        toggleMenuOff();
        alert('Верстка первоочередного плейлиста');
        // $('#moveClipToAnotherTime').modal('show');
        // return false;
    })


    /**
     * ---------------------------------------------------------------- Контекстное меню -------------------------
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

//-------------------------------------------- on document end -----------------------------------------------------------------------------

// получает и выводит информацию по клипам в эфире в розовой строке
function getInfoForPinkString() {
    API.PL.Items.ComingUpGet(function(oValue) {
        if (0 < oValue.length) {
            let nameOnAir = oValue[0].sName;
            let dtOnAir = getReplicaDataTime(oValue[0].dtStart);
            let typeOnAir = oValue[0].cFile.cStorage.sName;
            let nameNext = oValue[1].sName;
            let dtNext = getReplicaDataTime(oValue[1].dtStart);
            let typeNext = oValue[1].cFile.cStorage.sName;
            let firstRow = '<tr><td class="font-size-1" style="width: 40%;"> <b>Сейчас в эфире:</b> ' +
                nameOnAir + '</td><td  class="text-center font-size-1" style="width: 25%;"> <b>Время выхода: </b>' +
                dtOnAir + '</td><td class="font-size-1" style="width: 15%;"> <b>Тип:</b> ' +
                typeOnAir + '</td><td class="font-size-1  style="width: 15%;""> <b>Осталось:</b></td></tr>';
            firstRow += '<tr><td class="font-size-1 "> <b>Далее:</b> ' +
                nameNext + '</td><td class="text-center font-size-1"><b> Время выхода:</b> ' +
                dtNext + '</td><td class="font-size-1 "><b> Тип:</b> ' +
                typeNext + '</td><td> </td></tr>';
            $('#thePinkData').html(firstRow);
        }
    });
    return false;
}

// перезапоминание куков
function CoockiesReStore(sCookieValue) {
    let aNames = ['Archive', 'OnAir', 'Planned'];
    Cookies.Delete('plFoder');
    if (0 < aNames.indexOf(sCookieValue)) {
        Cookies.Set('plFolder', sCookieValue);
    } else {
        console.log('ошибка при запоминании кука закладки плейлиста - где-то неверный аргумент при вызове.')
    }
}

// ------------------------------------------------------------------------------- таблица закладки Архив
// получение данных и формирование таблицы
function loadArchiveTable(dtStart, dtEnd) {
    Loader.Show();
    $('#onArchiveTable').empty();
    // если аргументов нет - выводим архив за сегодня
    if (undefined === dtStart && undefined === dtEnd) {
        dtStart = new Date();
        dtStart.setDate(dtStart.getDate() - 1);
        dtEnd = new Date();
    }
    API.PL.Items.ArchiveList(dtStart, dtEnd, oValue => {

        // API.PL.Items.List(ret, oValue => {
        $(oValue).each(function(index, el) {
            $(makeRow(el, 'arh')).appendTo('#onArchiveTable');
        });

        let target = $('#itemsInArchive');
        target.empty();
        target.html('<strong>' + oValue.length + ' items total</strong> ');
        Loader.Hide();
    })
    return false;
}

// ------------------------------------------------------------------------------------- таблица закладки OnAir
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

// ------------------------------------------------------------------------------------ таблица закладки Planned
function loadPlannedInTable(dtStart, dtEnd) {
    Loader.Show();
    $('#onPlannedTable').empty();

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

// -------------------------------------------------------- Делает HTML строчки таблиц -----------------------------------------------------------------------------------
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
        try { dtLastDate = getReplicaDataTime(el.dtStart); } catch { dtLastDate = ""; };
        // try { dtLastDate = getReplicaDataTime(el.dtStartReal); } catch { dtLastDate = ""; };
    } else if (sNameTab == 'plan') {
        try { dtLastDate = getReplicaDataTime(el.dtStartPlanned); } catch { dtLastDate = ""; };
    } else {
        try { dtLastDate = getReplicaDataTime(el.dtStart); } catch { dtLastDate = ""; };
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

// преобразует строку даты из бд в немного другой формат для таблицы
function getReplicaDataTime(sdtDT) {

    let sDT, dtTemp;
    dtTemp = String(sdtDT);
    let nDatePos = dtTemp.indexOf('T');
    let nTimePos = nDatePos + 9;

    sDT = sdtDT.slice(0, nDatePos) + ' ' + sdtDT.slice(nDatePos + 1, nTimePos);

    return sDT;
}

//преобразование текущей даты в строку для ограничения календарей
function getMaxMinDate() {
    let sDate = "";
    let dtToday = new Date();
    let sYY = String(dtToday.getFullYear());
    let sMM = (10 < dtToday.getMonth()) ? String(dtToday.getMonth() + 1) : "0" + (dtToday.getMonth() + 1);
    let sDD = String(dtToday.getDate());
    return sDate = sYY + '-' + sMM + '-' + sDD;
}


//