//# sourceURL=kpp.js

$(document).ready(function() {

    let aTypes = [
        "clip", "advertisment", "program", "design"
    ]

    let aSafe = [
        "анонсы", "заставки", "клипы", "новости", "оформление", "программы", "реклама"
    ]

    // load template kpp-files
    Templates.Attach('kpp/kpp-files.html', $('[name=filesSection]'), function() {});

    $("#collapseTwo").on('shown.bs.collapse', function() {
        // load template kpp-addition
        $('[name=additionSection]').empty();
        Templates.Attach('kpp/kpp-addition.html', $('[name=additionSection]'), function() {});
    })

    $("#collapseThree").on('shown.bs.collapse', function() {
        // load template kpp-tasks
        $('[name=tasksSection]').empty();
        Templates.Attach('kpp/kpp-tasks.html', $('[name=tasksSection]'), function() {});
    })


    if (Cookies.Get('kppFoder')) {
        let kppFolder = Cookies.Get('kppFoder');
        switch (kppFolder) {
            case 'Files':
                dtToday = new Date();

                break;
            case 'Addition':
                dtToday = new Date();

                break;
            case 'Tasks':
                dtToday = new Date();

                break;
            default:
                dtToday = new Date();

        }
    } else {
        dtToday = new Date();


    }

})