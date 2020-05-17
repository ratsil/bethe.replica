class ReplicaTable {
    oTables = {
        PlayListArchive: {
            sName: "PlayListArchive",
            nCol: 8,
            aColNames: ["Дата и время выхода", "Название ассета", "Хронометраж", "Статус", "Имя файла", "Класс", "Ротация", "Тип"]
        },        
        PlayListAir: {
            sName: "PlayListAir",
            nCol: 8,
            aColNames: ["Дата и время выхода", "Название ассета", "Хронометраж", "Статус", "Имя файла", "Класс", "Ротация", "Тип"]
        },
        PlayListPlan: {
            sName: "PlayListPlan",
            nCol: 8,
            aColNames: ["Дата и время выхода", "Название ассета", "Хронометраж", "Статус", "Имя файла", "Класс", "Ротация", "Тип"]
        },
        AssetsAssetsAll: {
            sName: "AssetsAssetsAll",
            nCol: 7,
            aColNames: ["ID", "Исполнитель : Клип", "Файл", "Длительность", "Тип", "Ротация", "Класс"]
        },
        AssetsAssetsClips: {
            sName: "AssetsAssetsClips",
            nCol: 7,
            aColNames: ["ID", "Исполнитель : Клип", "Файл", "Длительность", "Тип", "Ротация", "Класс"]
        },
        AssetsAssetsAd: {
            sName: "AssetsAssetsAd",
            nCol: 7,
            aColNames: ["ID", "Исполнитель : Клип", "Файл", "Длительность", "Тип", "Ротация", "Класс"]
        },
        AssetsAssetsProgram: {
            sName: "AssetsAssetsProgram",
            nCol: 7,
            aColNames: ["ID", "Исполнитель : Клип", "Файл", "Длительность", "Тип", "Ротация", "Класс"]
        },
        AssetsAssetsDesign: {
            sName: "AssetsAssetsDesign",
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
}