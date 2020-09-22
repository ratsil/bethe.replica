class ReplicaTable {
<<<<<<< HEAD

    oTypeTables = {
        PlayListArchive: {
            sName: "PlayListArchive",
            aColNames: ["Дата и время выхода", "Название ассета", "Хронометраж", "Статус", "Имя файла", "Класс", "Ротация", "Тип"]
        },
        PlayListAir: {
            sName: "PlayListAir",
=======
    oTables = {
        PlayListArchive: {
            sName: "PlayListArchive",
            nCol: 8,
            aColNames: ["Дата и время выхода", "Название ассета", "Хронометраж", "Статус", "Имя файла", "Класс", "Ротация", "Тип"]
        },        
        PlayListAir: {
            sName: "PlayListAir",
            nCol: 8,
>>>>>>> b8b619966ef26a4bc9b5fa861f4558eb84c9896e
            aColNames: ["Дата и время выхода", "Название ассета", "Хронометраж", "Статус", "Имя файла", "Класс", "Ротация", "Тип"]
        },
        PlayListPlan: {
            sName: "PlayListPlan",
<<<<<<< HEAD
=======
            nCol: 8,
>>>>>>> b8b619966ef26a4bc9b5fa861f4558eb84c9896e
            aColNames: ["Дата и время выхода", "Название ассета", "Хронометраж", "Статус", "Имя файла", "Класс", "Ротация", "Тип"]
        },
        AssetsAssetsAll: {
            sName: "AssetsAssetsAll",
<<<<<<< HEAD
=======
            nCol: 7,
>>>>>>> b8b619966ef26a4bc9b5fa861f4558eb84c9896e
            aColNames: ["ID", "Исполнитель : Клип", "Файл", "Длительность", "Тип", "Ротация", "Класс"]
        },
        AssetsAssetsClips: {
            sName: "AssetsAssetsClips",
<<<<<<< HEAD
=======
            nCol: 7,
>>>>>>> b8b619966ef26a4bc9b5fa861f4558eb84c9896e
            aColNames: ["ID", "Исполнитель : Клип", "Файл", "Длительность", "Тип", "Ротация", "Класс"]
        },
        AssetsAssetsAd: {
            sName: "AssetsAssetsAd",
<<<<<<< HEAD
=======
            nCol: 7,
>>>>>>> b8b619966ef26a4bc9b5fa861f4558eb84c9896e
            aColNames: ["ID", "Исполнитель : Клип", "Файл", "Длительность", "Тип", "Ротация", "Класс"]
        },
        AssetsAssetsProgram: {
            sName: "AssetsAssetsProgram",
<<<<<<< HEAD
=======
            nCol: 7,
>>>>>>> b8b619966ef26a4bc9b5fa861f4558eb84c9896e
            aColNames: ["ID", "Исполнитель : Клип", "Файл", "Длительность", "Тип", "Ротация", "Класс"]
        },
        AssetsAssetsDesign: {
            sName: "AssetsAssetsDesign",
<<<<<<< HEAD
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
=======
            nCol: 7,
            aColNames: ["ID", "Исполнитель : Клип", "Файл", "Длительность", "Тип", "Ротация", "Класс"]
        },
        AssetsPersons:{
            sName: "AssetsPersons",
            nCol: 3,
            aColNames: ["Название", "Тип", "id"]
        }
    }

//Конструктор. просто конструктор
    constructor(sTypeTable){
        this.title = oTitle;
    }

// Получить даные для таблицы
    getData(oTypeTable){
        switch (oTypeTable.sName) {
            case "PlayListArchive":
                break
            case "PlayListAir":
                break
            case "PlayListPlan":
                break
            case "AssetsAssetsAll":
                break
            case "AssetsAssetsClips":
                break
            case "AssetsAssetsAd":
                break
            case "AssetsAssetsProgram":
                break
            case "AssetsAssetsDesign":
                break    
            case "AssetsPersons":
                break
        }

    }

// Сгенерировать строку таблицы в HTML
    makeRow(){

    }

//Вывести таблицу в нужное место
    static attachTableTo(oAttachPoint){

    }

//конец класса
>>>>>>> b8b619966ef26a4bc9b5fa861f4558eb84c9896e
}