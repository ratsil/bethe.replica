
$(document).ready(function(){


    aStatuses = [{nID: 1, sName: "Запланировано"},
                {nID: 2, sName: "Поставлено в очередь"},
                {nID: 3, sName: "Подготовлено"},
                {nID: 4, sName: "В эфире"},
                {nID: 5, sName: "Проиграно"},
                {nID: 6, sName: "Пропущено"},
                {nID: 7, sName: "С ошибкой"}];

    dtBegin = new Date('01.01.2020');
    dtEnd = new Date();

    API.MAM.Statuses.List (aStat => {
        console.log('---------------------------------------------------------------------')
        if (Array.isArray(aStat)) {
            console.log('API.MAM.Statuses.List - работает! Возвращает:');
            console.log(aStat); 
        }
        else {
            console.log('API.MAM.Statuses.List - не работает! :(');
        }
    })

    API.PL.Items.List(aStatuses, o => {
        console.log('---------------------------------------------------------------------')
        if (o.Data != null) {
            console.log('API.PL.Items.List(aStatuses, fCallback) - работает! Возвращает:');
            console.log('Аргумент:')
            console.log(aStatuses);
            console.log(o.Data);
            console.log(o.Message);
         }
        else {
            console.log('API.PL.Items.List(aStatuses, fCallback) - НЕ работает! Возвращает:');
            console.log('Аргумент:')
            console.log(aStatuses);
            console.log(o.Data);
            console.log(o.Message);
        }
    })      
    
    // ArchiveList: function(dtBegin, dtEnd, fCallback)
    API.PL.Items.ArchiveList(dtBegin, dtEnd, o => {
        console.log('---------------------------------------------------------------------')
        if (Array.isArray(o)) {
            console.log('API.PL.Items.ArchiveList(dtBegin, dtEnd, fCallback) - работает! Возвращает:');
            console.log('Аргументы: dtBegin: '+ dtBegin + ' dtEnd: ' + dtEnd)
            console.log(o);
         }
        else {
            console.log('API.PL.Items.ArchiveList(dtBegin, dtEnd, fCallback) - НЕ работает! Возвращает:');
            console.log('Аргументы: dtBegin: '+ dtBegin + ' dtEnd: ' + dtEnd)
            console.log(o.Data);
            console.log(o.Message);
        }
    })
    
    // PlannedList: function(dtBegin, dtEnd, fCallback) {
    API.PL.Items.PlannedList(dtBegin, dtEnd, o => {
        console.log('---------------------------------------------------------------------')
        if (o.Data != null) {
            console.log('API.PL.Items.PlannedList(dtBegin, dtEnd, fCallback) - работает! Возвращает:');
            console.log('Аргументы: dtBegin: '+ dtBegin + ' dtEnd: ' + dtEnd)
            console.log(o.Data);
            console.log(o.Message);
         }
        else {
            console.log('API.PL.Items.PlannedList(dtBegin, dtEnd, fCallback) - НЕ работает! Возвращает:');
            console.log('Аргументы: dtBegin: '+ dtBegin + ' dtEnd: ' + dtEnd)
            console.log(o.Data);
            console.log(o.Message);
        }
    })   
    
    // AdvertisementsList: function(dtBegin, dtEnd, fCallback)
    API.PL.Items.AdvertisementsList(dtBegin, dtEnd, o => {
        console.log('---------------------------------------------------------------------')
        if (o.Data != null) {
            console.log('API.PL.Items.AdvertisementsList(dtBegin, dtEnd, fCallback) - работает! Возвращает:');
            console.log('Аргументы: dtBegin: '+ dtBegin + ' dtEnd: ' + dtEnd)
            console.log(o.Data);
            console.log(o.Message);
         }
        else {
            console.log('API.PL.Items.AdvertisementsList(dtBegin, dtEnd, fCallback) - НЕ работает! Возвращает:');
            console.log('Аргументы: dtBegin: '+ dtBegin + ' dtEnd: ' + dtEnd)
            console.log(o.Data);
            console.log(o.Message);
        }
    })  

    // ComingUpGet: function(fCallback)
    API.PL.Items.ComingUpGet( o => {
        console.log('---------------------------------------------------------------------')
        if (o != null) {
            console.log('API.PL.Items.ComingUpGet(fCallback) - работает! Возвращает:');
            // console.log(o.Data);
            // console.log(o.Message);
         }
        else {
            console.log('API.PL.Items.ComingUpGet(fCallback) - НЕ работает! Возвращает:');
            // console.log(o.Data);
            // console.log(o.Message);
        }
    })  

    // MinimumForImmediatePLGet: function(fCallback)
    API.PL.Items.MinimumForImmediatePLGet( o => {
        console.log('---------------------------------------------------------------------')
        if (o.sName != null) {
            console.log('API.PL.Items.MinimumForImmediatePLGet(fCallback) - работает! Возвращает:');
            console.log(o.sName);
            console.log(o.nFramesQty);
         }
        else {
            console.log('API.PL.Items.MinimumForImmediatePLGet(fCallback) - НЕ работает! Возвращает:');
            console.log(o.sName);
            console.log(o.nFramesQty);
        }
    })  

    //  AddResultGet: function(fCallback)
    API.PL.Items.AddResultGet( o => {
        console.log('---------------------------------------------------------------------')
        if (o.Data != null) {
            console.log('API.PL.Items.AddResultGet(fCallback) - работает! Возвращает:');
            console.log(o.Data);
            console.log(o.Message);
         }
        else {
            console.log('API.PL.Items.AddResultGet(fCallback) - НЕ работает! Возвращает:');
            console.log(o.Data);
            console.log(o.Message);
        }
    })  

})

