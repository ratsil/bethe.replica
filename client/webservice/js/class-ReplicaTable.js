class ReplicaTable {

    oTypeTables = {
        PlayListArchive: {
            sName: "PlayListArchive",
            aColNames: ["Дата и время выхода", "Название ассета", "Хронометраж", "Статус", "Имя файла", "Класс", "Ротация", "Тип"]
        },
        PlayListAir: {
            sName: "PlayListAir",
            aColNames: ["Дата и время выхода", "Название ассета", "Хронометраж", "Статус", "Имя файла", "Класс", "Ротация", "Тип"]
        },
        PlayListPlan: {
            sName: "PlayListPlan",
            aColNames: ["Дата и время выхода", "Название ассета", "Хронометраж", "Статус", "Имя файла", "Класс", "Ротация", "Тип"]
        },
        AssetsAssetsAll: {
            sName: "AssetsAssetsAll",
            aColNames: ["ID", "Исполнитель : Клип", "Файл", "Длительность", "Тип", "Ротация", "Класс"]
        },
        AssetsAssetsClips: {
            sName: "AssetsAssetsClips",
            aColNames: ["ID", "Исполнитель : Клип", "Файл", "Длительность", "Тип", "Ротация", "Класс"]
        },
        AssetsAssetsAd: {
            sName: "AssetsAssetsAd",
            aColNames: ["ID", "Исполнитель : Клип", "Файл", "Длительность", "Тип", "Ротация", "Класс"]
        },
        AssetsAssetsProgram: {
            sName: "AssetsAssetsProgram",
            aColNames: ["ID", "Исполнитель : Клип", "Файл", "Длительность", "Тип", "Ротация", "Класс"]
        },
        AssetsAssetsDesign: {
            sName: "AssetsAssetsDesign",
            aColNames: ["ID", "Исполнитель : Клип", "Файл", "Длительность", "Тип", "Ротация", "Класс"]
        },
        AssetsPersons: {
            sName: "AssetsPersons",
            aColNames: ["Название", "Тип", "id"]
        },
    }

    //Конструктор. просто конструктор
    constructor(sTypeTable, oDataTable, sIdPlace) {
        let oTableHeaderRow; // объект содержащий заголовки таблицы
        let sNewTable; // таблица готовая к публикации (html)
        // Если не накосячили с вызовом типа таблицы, то на выходе получим объект с заголовками
        if (sTypeTable in this.oTypeTables) {
            oTableHeaderRow = this.oTypeTables[sTypeTable]; // получаем заголовки таблицы
            sNewTable = this.makeTable(oTableHeaderRow, oDataTable); // получаем модель таблицы
            this.attachTableTo(sIdPlace, sNewTable); // генерим HTML таблицы в нужное место
        }
        else {
            console.log('Неверное имя свойства при обращении к oTypeTables в конструкторе!');
            break;
        }
    }

    makeTable(oTableHeaderRow, oDataTable) {
        let sNewTable ='<table>';
        sNewTable += this.makeHeadRow(oTableHeaderRow);
        sNewTable += this.makeRows(oDataTable);
        sNewTable += '</table>'
        return sNewTable;
    }

    makeHeadRow(oTableHeaderRow) {
        let nSize = oTableHeaderRow.aColNames.lenght;
        let sHeaderTable = '<thead><tr>';

        oTableHeaderRow.aColNames.forEach(el => {
            sHeaderTable += '<th>' + el + '</th>'
        });
        sHeaderTable += '</tr></thead>';
        return sHeaderTable;
    }

    // Сгенерировать строку таблицы в HTML
    makeRows(oDataTable) {
        let sBodyTable = '<tbody>';

        $(oDataTable).each(function(){

            let nID = el.nID;
            let sName = el.sName;
            let sDuration = getDuration(el.nFramesQty);
            let sFile, sType, sClass, sRotation;
            try { sFile = el.cFile.sFilename; } catch { sFile = ""; };
            try { sType = el.stVideo.cType.sName; } catch { sType = ""; };
            try { sRotation = el.cRotation.sName; } catch { sRotation = ""; };
            try { sClass = el.aClasses[0].sName; } catch { sClass = ""; };
            // to -> tableAssetClips
            sBodyTable = "<tr class='tr-control'>";
            sBodyTable += "<td>" + nID + "</td>";
            sBodyTable += "<td>" + sName + "</td>";
            sBodyTable += "<td>" + sFile + "</td>";
            sBodyTable += "<td>" + sDuration + "</td>";
            sBodyTable += "<td>" + sType + "</td>";
            if (bRot)
                sBodyTable += "<td>" + sRotation + "</td>";
            sBodyTable += "<td>" + sClass + "</td>";
            sBodyTable += "</tr>";

            sBodyTable += '</tbody>'
            return sBodyTable;
        })
    }

    //Вывести таблицу в нужное место
    attachTableTo(sIdPlace, sNewTable) {

    }

    //конец класса
}