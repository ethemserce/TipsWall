$.templates("continentTemplate",
    {
        markup: "#continentTemplate",
        //helpers: myHelpers,
        templates: {
            leaguesTemplate: "#leaguesTemplate",
        }
    });

$(document).ready(function () {
    renderContinents();

})

function renderContinents() {

    $.ajax({
        type: "GET",
        url: "/League/ContinentJson",
        cache: false,
        beforeSend: function () {
            $("#divLoading").show();
        },
        success: function (data) {
            $('#divContinents').empty();
            $("#divContinents").append(
                $.render.continentTemplate(data)
            );
            $("#divLoading").hide();
        },
        error: function () {
            $("#divLoading").hide();
            PreOdds.Utils.ShowErrorNotification("İşleminiz sırasında bir hata oluştu, lütfen daha sonra tekrar deneyin.");
        }
    });
}

function renderLeagues(countryId) {

    var checked = "#checkbox_" + countryId;
    if ($(checked).prop("checked") == false) {
        $("#divLeagues_" + countryId).empty();
        document.getElementById("checkbox_" + countryId).checked = false;
        return false;
    }
    else {
        $.ajax({
            type: "GET",
            url: "/League/LeaguesJson",
            data: { 'countryId': countryId },
            cache: false,
            beforeSend: function () {
                //$("#divLoading").show();
            },
            success: function (data) {
                $("#divLeagues_" + countryId).empty();
                $("#divLeagues_" + countryId).append(
                    $.render.leaguesTemplate(data)
                );
                document.getElementById("checkbox_" + countryId).checked = true;
                //$("#divLoading").hide();
            },
            error: function () {
                $("#divLoading").hide();
                PreOdds.Utils.ShowErrorNotification("İşleminiz sırasında bir hata oluştu, lütfen daha sonra tekrar deneyin.");
            }
        });
    }
}