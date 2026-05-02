//var myHelpers = { formatDecimal: formatDecimal };
$.templates("allTemplate",
    {
        markup: "#allTemplate",
        //helpers: myHelpers,
        templates: {
            favoriFixtureTemplate: "#favoriFixtureTemplate",
            selectCountryTemplate: "#selectCountryTemplate"
            //fixtureTemplate: "#fixtureTemplate",
            //fixtureDateTemplate: "#fixtureDateTemplate",
            //fixtureLeagueTemplate:"#fixtureLeagueTemplate",
            //liveTemplate: "#liveTemplate",
            //dateTemplate: "#dateTemplate",
            //countryTemplate: "#countryTemplate"
        }
    });

var isLive = false;
var fixtureOfDate = null;
var countryList = null;
var date = $('#fixtureDate').val();
var isDateSelected = 0;
var clientDate = new Date();
var userLang;
var userDateFormat;
var updateInterval;
var openedLeagueList = Array(0);
var favoriteFixtureList = Array(0);
var selectedCountryList = Array(0);

$(document).ready(function () {
    PreOdds.Utils.SetTimezone();
    //GetDate();
    userLang = navigator.language || navigator.userLanguage;

    if (userLang == null || userLang == 'undefined') {
        userLang = 'en';
        //userDateFormat = "yyyy-mm-dd";
    }
    else {
        userLang = userLang.substring(0, 2);
        if (userLang != 'tr') {
            userLang = 'en';
            //userDateFormat = "yyyy-mm-dd";
        }
        else {
            //userDateFormat = "yyyy-mm-dd";
        }
    }

    $("#fixtureDate").datepicker({
        orientation: "bottom",
        weekStart: 1,
        daysOfWeekHighlighted: "6,0",
        autoclose: true,
        todayHighlight: true,
        format: 'dd/mm/yyyy',
        language: userLang
    }).datepicker('setDate', 'today');

    var swiper = new Swiper('.swiper-container', {
        slidesPerView: 3,
        spaceBetween: 30,
        speed: 400,
        reverseDirection: true,
        pagination: {
            el: '.swiper-pagination',
            clickable: true,
        },
        autoplay: {
            delay: 100000,
        },
    });

    $("#fixtureDate").on("change", function () {
        isDateSelected = 1;
        renderList();
    });

    renderList();
});

function renderList() {
    window.clearInterval(updateInterval);
    if (date != $('#fixtureDate').val()) {
        date = $('#fixtureDate').val();
        $('#divFixtureOfLeague').empty();
        $('#divFavoriFixture').empty();
        //$('#divFixtureOfLive').empty();
        //$('#divFixtureOfDate').empty();
    }
    //alert('client Date:' + clientDate.toDateString() + ' - Date: ' + date);
    $.ajax({
        type: "GET",
        url: "/Fixture/FixtureOfDateJson",
        data: { 'date': date, 'isDateSelected': isDateSelected, 'clientDate': clientDate.toDateString() },
        cache: false,
        beforeSend: function () {
            //$('#divFixtureOfDate').empty();
            $('#divFixtureOfLeague').empty();
            $('#divFavoriFixture').empty();
            $("#divLoading").show();
        },
        success: function (data) {
            if (data == null) {
                renderList();
            }

            //fixtureOfDate = data;
            checkLeagueIsOpen(data);
            checkFixtureIsFavori(fixtureOfDate);

            //$('#divCountryOfDay').empty();
            //$("#divCountryOfDay").append($.render.countryTemplate(data));

            $("#divLoading").hide();
            $("#divFixtureOfLeague").css("display", "flex");
            //$("#divFixtureOfLive").css("display", "none");
            //$("#divFixtureOfDate").css("display", "none");
            $("#divFixtureOfLeague").removeClass();
            $('#divFixtureOfLeague').addClass("row isotope row-15");
            //$("#divFixtureOfLive").removeClass();
            //$("#divFixtureOfDate").removeClass();
            //$("#divLoading").show();
            isLive = false;
            //$("#live").removeClass();
            //$("#live").addClass("button button-xs button-red-outline");
            //$("#date").removeClass();
            //$("#date").addClass("button button-xs button-red-outline");
            //$("#league").removeClass();
            //$("#league").addClass("button button-xs button-red-outline active");

            data.isLive = isLive;
            $('#divFixtureOfLeague').empty();
            $("#divFixtureOfLeague").append($.render.allTemplate(fixtureOfDate));

            countryList = fixtureOfDate.countries;
            $('#selectCountry').empty();
            $("#selectCountry").append($.render.selectCountryTemplate(fixtureOfDate.countries));
            //$("#divFixtureOfLive").append($.render.liveTemplate(data));
            //$('#divFixtureOfDate').empty();
            //$("#divFixtureOfDate").append($.render.dateTemplate(data));



            if (data.runTimer) {
                updateInterval = window.setInterval(update, 10 * 1000);
            }
            else {
                window.clearInterval(updateInterval);
                //updateInterval = window.setInterval(update, 60 * 1000);
            }

        },
        error: function () {
            $("#divLoading").hide();
            //$("#divLoadMore").show();
            //PreOdds.Utils.HideLoadingModal();
            //PreOdds.Utils.ShowErrorNotification("İşleminiz sırasında bir hata oluştu, lütfen daha sonra tekrar deneyin.");
        }
    });
}

function update() {
    $.ajax({
        type: "GET",
        url: "/Fixture/FixtureOfDateJson",
        data: { 'date': date, 'isDateSelected': isDateSelected, 'clientDate': clientDate.toDateString() },
        cache: false,
        //beforeSend: function () {
        //    if (isButtonCalling) {
        //        DijitalSaha.Utils.ShowLoadingModal();
        //    } else {
        //        $("#divLoading").show();
        //    }

        //    $("#divLoadMore").hide();
        //},

        success: function (data) {
            if (data == null) {
                update();
            }
            //fixtureOfDate = data;
            checkLeagueIsOpen(data);
            checkFixtureIsFavori(fixtureOfDate);
            checkCountrySelected(fixtureOfDate);
            data.isLive = isLive;

            $('#divFixtureOfLeague').empty();
            $("#divFixtureOfLeague").append($.render.allTemplate(fixtureOfDate));

            //$('#divFixtureOfLive').empty();
            //$("#divFixtureOfLive").append($.render.liveTemplate(data));
            //$('#divFixtureOfDate').empty();
            //$("#divFixtureOfDate").append($.render.dateTemplate(data));
            //if (isLive == false) {
            //    renderAll(data);
            //}
            //else if (isLive == true) {
            //    renderLive(data);
            //}
        },
        error: function () {
            $("#divLoading").hide();
            //$("#divLoadMore").show();
            //PreOdds.Utils.HideLoadingModal();
            //PreOdds.Utils.ShowErrorNotification("İşleminiz sırasında bir hata oluştu, lütfen daha sonra tekrar deneyin.");
        }
    });
}

function addRemoveLeagueList(leagueId, groupId) {

    for (var i = 0; i < fixtureOfDate.fixtureForLeague.length; i++) {
        if (fixtureOfDate.fixtureForLeague[i].group != null && groupId != null) {
            if (leagueId === fixtureOfDate.fixtureForLeague[i].league.id && groupId === fixtureOfDate.fixtureForLeague[i].group.id) {
                if (fixtureOfDate.fixtureForLeague[i].isOpen) {
                    fixtureOfDate.fixtureForLeague[i].isOpen = false;
                    openedLeagueList.splice(openedLeagueList.indexOf(leagueId + "" + groupId), 1);
                }
                else {
                    fixtureOfDate.fixtureForLeague[i].isOpen = true;
                    openedLeagueList.push(leagueId + "" + groupId);
                }
                break;
            }
        }
        else {
            if (leagueId === fixtureOfDate.fixtureForLeague[i].league.id && (groupId == null || groupId == 0)) {
                if (fixtureOfDate.fixtureForLeague[i].isOpen) {
                    fixtureOfDate.fixtureForLeague[i].isOpen = false;
                    openedLeagueList.splice(openedLeagueList.indexOf(leagueId), 1);
                }
                else {
                    fixtureOfDate.fixtureForLeague[i].isOpen = true;
                    openedLeagueList.push(leagueId);
                }
                break;
            }
        }
    }

    $('#divFixtureOfLeague').empty();
    $("#divFixtureOfLeague").append($.render.allTemplate(fixtureOfDate));

    //for (var i = 0; i < openedLeagueList.length; i++) {
    //    if (openedLeagueList[i] === id) {
    //        openedLeagueList.splice(id, 1);
    //        break;
    //    }
    //}

    //$('#fixtureDate').datepicker("setDate", new Date());
};

function checkLeagueIsOpen(data) {
    if (data.fixtureForLeague != null) {
        for (var i = 0; i < data.fixtureForLeague.length; i++) {
            for (var x = 0; x < openedLeagueList.length; x++) {
                if (data.fixtureForLeague[i].group != null) {
                    if (openedLeagueList[x] === (data.fixtureForLeague[i].league.id + "" + data.fixtureForLeague[i].group.id)) {
                        data.fixtureForLeague[i].isOpen = true;
                    }
                }
                else {
                    if (openedLeagueList[x] === data.fixtureForLeague[i].league.id) {
                        data.fixtureForLeague[i].isOpen = true;
                    }
                }
            }
        }
        fixtureOfDate = data;
    }
}

function addRemoveFavoriteFixtureList(id) {

    var fixtureList = [];
    var hasFixture = false;

    if (favoriteFixtureList != null) {
        var setFavori = false;
        for (var x = 0; x < favoriteFixtureList.length; x++) {
            if (favoriteFixtureList[x] === id) {
                favoriteFixtureList.splice(favoriteFixtureList.indexOf(id), 1);
                hasFixture = true;
                for (var i = 0; i < fixtureOfDate.fixtureForLeague.length; i++) {
                    for (var a = 0; a < fixtureOfDate.fixtureForLeague[i].fixture.length; a++) {
                        if (favoriteFixtureList[x] === fixtureOfDate.fixtureForLeague[i].fixture[a].id) {
                            fixtureOfDate.fixtureForLeague[i].fixture[a].isFavori = false;
                            setFavori = true;
                            break;
                        }
                    }
                    if (setFavori) {
                        break;
                    }
                }
                break;
            }
        }

        if (!hasFixture) {
            favoriteFixtureList.push(id);
        }
    }
    else {
        favoriteFixtureList.push(id);
        hasFixture = false;
    }

    for (var i = 0; i < fixtureOfDate.fixtureForLeague.length; i++) {
        for (var a = 0; a < fixtureOfDate.fixtureForLeague[i].fixture.length; a++) {
            for (var x = 0; x < favoriteFixtureList.length; x++) {
                if (favoriteFixtureList[x] === fixtureOfDate.fixtureForLeague[i].fixture[a].id) {
                    fixtureOfDate.fixtureForLeague[i].fixture[a].isFavori = true;
                    fixtureList.push(fixtureOfDate.fixtureForLeague[i].fixture[a]);
                }
            }
        }
    }

    if (fixtureList.length > 0) {
        $('#divFavoriFixture').empty();
        $("#divFavoriFixture").append($.render.favoriFixtureTemplate(fixtureList));
        $("#divFavoriFixtureHeader").css("display", "flex");
        $("#divFavoriFixtureHeader").removeClass();
        $('#divFavoriFixtureHeader').addClass("row isotope row-15");
    }
    else {
        $('#divFavoriFixture').empty();
        $("#divFavoriFixtureHeader").css("display", "none");
        $("#divFavoriFixtureHeader").removeClass();
    }
}

function checkFixtureIsFavori(data) {
    var fixtureList = [];
    if (favoriteFixtureList != null) {
        for (var x = 0; x < favoriteFixtureList.length; x++) {
            for (var i = 0; i < data.fixtureForLeague.length; i++) {
                for (var a = 0; a < data.fixtureForLeague[i].fixture.length; a++) {
                    if (favoriteFixtureList[x] === data.fixtureForLeague[i].fixture[a].id) {
                        data.fixtureForLeague[i].fixture[a].isFavori = true;
                        fixtureList.push(data.fixtureForLeague[i].fixture[a]);
                    }
                    else {
                        data.fixtureForLeague[i].fixture[a].isFavori = false;
                    }
                }
            }
        }
    }

    fixtureOfDate = data;

    if (fixtureList.length > 0) {
        $('#divFavoriFixture').empty();
        $("#divFavoriFixture").append($.render.favoriFixtureTemplate(fixtureList));
        $("#divFavoriFixtureHeader").css("display", "flex");
        $("#divFavoriFixtureHeader").removeClass();
        $('#divFavoriFixtureHeader').addClass("row isotope row-15");
    }
    else {
        $('#divFavoriFixture').empty();
        $("#divFavoriFixtureHeader").css("display", "none");
        $("#divFavoriFixtureHeader").removeClass();
    }
}

function renderLive() {
    if (isLive == true) {
        isLive = false;
        $("#live").removeClass();
        $("#live").addClass("button button-xs button-primary-outline");
    }
    else {
        isLive = true;
        $("#live").removeClass();
        $("#live").addClass("button button-xs button-primary-outline active");
    }

    fixtureOfDate.isLive = isLive;
    $('#divFixtureOfLeague').empty();
    $("#divFixtureOfLeague").append($.render.allTemplate(fixtureOfDate));
    //$('#divFixtureOfDate').empty();
    //$("#divFixtureOfDate").append($.render.dateTemplate(fixtureOfDate));
}

function renderLeagueList() {
    //if (data == null) {
    //    data = fixtureOfDate;
    //}
    $("#divFixtureOfLeague").css("display", "flex");
    //$("#divFixtureOfDate").css("display", "none");
    $("#divFixtureOfLeague").removeClass();
    $('#divFixtureOfLeague').addClass("row isotope row-15");
    //$("#divFixtureOfDate").removeClass();
    //$("#divLoading").show();

    //$("#date").removeClass();
    //$("#date").addClass("button button-xs button-red-outline");
    $("#league").removeClass();
    $("#league").addClass("button button-xs button-red-outline active");

    //$("#divFixtureOfDate").append($.render.allTemplate(data));
    //$("#divLoading").hide();
}

function renderDateList() {
    //$("#divFixtureOfDate").css("display", "flex");
    $("#divFixtureOfLeague").css("display", "none");
    //$("#divFixtureOfDate").removeClass();
    //$('#divFixtureOfDate').addClass("row isotope row-15");
    $("#divFixtureOfLeague").removeClass();
    //$("#divLoading").show();
    $("#league").removeClass();
    $("#league").addClass("button button-xs button-red-outline");
    //$("#date").removeClass();
    //$("#date").addClass("button button-xs button-red-outline active");
}

function isGoal(data) {
    var liveData = data.fixtureForLeagueLive;
    for (var i = 0; i < liveData.length; i++) {
        for (var x = 0; x < liveData[i].fixture.length; x++) {
            for (var a = 0; a < fixtureOfDate.fixtureForLeague.length; a++) {
                for (var b = 0; b < fixtureOfDate.fixtureForLeague[a].fixture.length; b++) {
                    if (data.fixtureForLeagueLive[i].Fixture[x].id == fixtureOfDate.fixtureForLeague[a].fixture[b].id) {
                        alert(fixtureOfDate.fixtureForLeague[a].fixture[b].TimeStatus);
                    }
                }
            }
        }
    }
}

function GetDate() {
    var d = new Date();
    $.ajax({
        type: "GET",
        url: "/Home/SetTimeZone",
        data: { 'timeZone': d.getTimezoneOffset() },
    });
}

function addRemoveCountry(id) {
    var setcountry = false;

    if (selectedCountryList.length > 0) {
        for (var i = 0; i < selectedCountryList.length; i++) {
            if (selectedCountryList[i] === id) {
                selectedCountryList.splice(selectedCountryList.indexOf(id), 1);
                setcountry = true;
                $('#input-checkbox-' + id + '').attr('checked', false);
                break;
            }
        }
    }

    if (!setcountry) {
        selectedCountryList.push(id);
        $('#input-checkbox-' + id + '').attr('checked', true);
    }
    update();
}

function checkCountrySelected(data) {
    for (var x = 0; x < selectedCountryList.length; x++) {
        if (selectedCountryList[x] !== "undefined") {
            for (var i = 0; i < data.fixtureForLeague.length; i++) {
                if (selectedCountryList[x] === data.fixtureForLeague[i].country.id) {
                    data.fixtureForLeague[i].countrySelected = true;
                    data.countrySelected = true;
                }
            }
        }
    }

    fixtureOfDate = data;
}

function CollapseAllLeagues()
{
    for (var i = 0; i < fixtureOfDate.fixtureForLeague.length; i++) {
        fixtureOfDate.fixtureForLeague[i].isOpen = false;
        openedLeagueList = Array(0);
    }
    $('#divFixtureOfLeague').empty();
    $("#divFixtureOfLeague").append($.render.allTemplate(fixtureOfDate));
}
function ClearSelectedCountries() {
    selectedCountryList = Array(0);
    $('#selectCountry').empty();
    $("#selectCountry").append($.render.selectCountryTemplate(countryList));
    update();
}