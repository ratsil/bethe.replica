$(document).ready(function() {

    // API.Authorize(function() {
    //     window.location = 'newreplicaadmin.html';
    // });

    // ентер на поле пароля
    $('[name=password]').keyup(function(e) {
        if (e.which === 13) {
            loginPass();
        }
        return false;
    });
    $('[name=login]').keyup(function(e) {
        if (e.which === 13) {
            loginPass();
        }
        return false;
    });

    $("#signInBtn").click(function() {
        loginPass();
        $('[name=password]').keyup();
    });

});

function loginPass() {
    let sLogin = $("[name=login]").val();
    let sPass = $("[name=password]").val();
    API.Users.SignIn(sLogin, sPass, function(o) {
        if (true === o) {
            window.location = "newreplicaadmin.html";
            return true;
        } else {
            let myalert = $(".alert");
            myalert.slideDown(1000);
            $("[name=login]").val('');
            $("[name=password]").val('');
            setInterval(function() {
                myalert.slideUp(2000);
            }, 2000);
            return false;
        }
    });
}