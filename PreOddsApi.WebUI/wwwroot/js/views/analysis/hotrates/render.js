var isButtonCalling = false;
$.templates("movieTemplate",
    {
        markup: "#movieTemplate",
        //helpers: myHelpers,
        templates: {
            columnTemplate: "#columnTemplate"
            //infoTemplate: "#infoTemplate"
        }
    });

var marketId = $('#marketId').val();
var analysisPeriodId = $('#analysisPeriodId').val();
var winningId = $('#winningId').val();
var earningId = $('#earningId').val();
var minRateId = $('#minRateId').val();
var matchStateId = $('#matchStateId').val();
var date = $("#hotratesDatetime").val();
var page = 0;
var userLang;

$(document).ready(function () {

    userLang = navigator.language || navigator.userLanguage;

    if (userLang == null || userLang == 'undefined') {
        userLang = 'en'
    }
    else {
        userLang = userLang.substring(0, 2);
        if (userLang != 'tr') {
            userLang = 'en'
        }
    }

    $("#hotratesDatetime").datepicker({
        orientation: "bottom",
        weekStart: 1,
        daysOfWeekHighlighted: "6,0",
        autoclose: true,
        todayHighlight: true,
        format: 'dd/mm/yyyy',
        language: userLang
    });
    $('#hotratesDatetime').datepicker("setDate", new Date());

    renderList();
});

function getList() {
    isButtonCalling = true;
    page = 0;
    $('#divHotrates').empty();
    renderList();
}

function renderMore() {
    isButtonCalling = false;
    renderList();
}


function renderList() {
   
    if (marketId != $('#marketId').val() || analysisPeriodId != $('#analysisPeriodId').val() || winningId != $('#winningId').val() ||
        earningId != $('#earningId').val() || minRateId != $('#minRateId').val() || matchStateId != $('#matchStateId').val() || date != $('#hotratesDatetime').val()) {
        marketId = $('#marketId').val();
        analysisPeriodId = $('#analysisPeriodId').val();
        winningId = $('#winningId').val();
        earningId = $('#earningId').val();
        minRateId = $('#minRateId').val();
        matchStateId = $('#matchStateId').val();
        date = $('#hotratesDatetime').val();
        pageNo = 0;
        $('#divHotRates').empty();
    }
    //date = date.getFullYear() + "-" + date.getMonth() + "-" + date.getDay();
    $.ajax({
        type: "GET",
        url: "/Analysis/HotratesJson",
        data: { 'marketId': marketId, 'analysisPeriodId': analysisPeriodId, 'winningId': winningId, 'earningId': earningId, 'minRateId': minRateId, 'matchStateId': matchStateId, 'date': date, 'page': page },
        cache: false,
        beforeSend: function () {
            if (isButtonCalling) {
                PreOdds.Utils.ShowLoadingModal();
            } else {
                $("#divLoading").show();
            }

            $("#divLoadMore").hide();
        },
        success: function (data) {
            if (!data.success || data.success == null) {
                $("#divLoading").hide();
                PreOdds.Utils.HideLoadingModal();
                return;
            }

            page = data.page + 1;
            if (data.isLastPage) {
                $("#divLoadMore").hide();
            }
            else {
                $("#divLoadMore").show();
            }

            $("#divHotrates").append(
                $.render.movieTemplate(data.fixture)
            );

            //var myTmpl = $.templates("#infoTemplate");

            //var html = myTmpl.render(data.summaryResult);

            //$("#divInfo").html(html);

            //var $color_primary = '#ffdc11';
            //var $track_color = '#ecf0f6';
            //var $circular_bar = $('.circular__bar');
            //$circular_bar.easyPieChart({
            //    barColor: $color_primary,
            //    trackColor: $track_color,
            //    lineCap: 'square',
            //    lineWidth: 8,
            //    size: 90,
            //    scaleLength: 0
            //});

            $("#divLoading").hide();
            PreOdds.Utils.HideLoadingModal();


            //addTour(window.localStorage);
        },
        error: function () {
            $("#divLoading").hide();
            $("#divLoadMore").show();
            PreOdds.Utils.HideLoadingModal();
            PreOdds.Utils.ShowErrorNotification("İşleminiz sırasında bir hata oluştu, lütfen daha sonra tekrar deneyin.");
        }
    });
}