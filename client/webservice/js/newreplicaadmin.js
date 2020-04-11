
$(document).ready(function() {

    API.Authorize(function() {

        // Loader.Show();

        $('body').removeClass('d-none');

        $("#signOutBtn").click(function() {
            API.Users.SignOut();
            return false;
        });

        if (Cookies.Get('page')) {
            let page = Cookies.Get('page');
            let ui = $('#mainContent').empty();
            setNavLinkActive(page);
            Templates.Attach('/' + page + '/' + page + '.html', ui, function() {
                // Loader.Hide();
            });
        } else {
            setNavLinkActive("net");
            Templates.Attach('/net/net.html', $('#mainContent'), function() {
                // Loader.Hide(1);
            });
        }

        //click by menu links
        $('.rmenu').click(function() {
            // Loader.Show();
            let sNameUi = $(this).attr('name');
            let ui = $('#mainContent').empty();
            setNavLinkActive(sNameUi);

            console.log ('/' + sNameUi + '/' + sNameUi + '.html');
            
            Templates.Attach('/' + sNameUi + '/' + sNameUi + '.html', ui, function() {
                // Loader.Hide();
            });
            Cookies.Set('page', sNameUi, 10);
            return false;
        });

        //switch language
        $('.dropdown-item').click(function(){
            if ($(this).data('switchLang') === 'ru') {
                alert ('choosen Ru');
            } else {
                alert ('choosen En');
            }
        })

    })

});

function setNavLinkActive(sNameUi) {
    $(".rmenu-active").removeClass('rmenu-active');

    let ui = $("[name=" + sNameUi + "]").parent();
    ui.addClass("rmenu-active");
}