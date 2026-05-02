var isButtonCalling = false;
$.templates("tipsTemplate",
    {
        markup: "#tipsTemplate",
        //helpers: myHelpers,
        templates: {
            //infoTemplate: "#infoTemplate"
        }
    });

var page = 0;

$(document).ready(function () {
    renderTips();
});

function renderMore() {
    renderTips();
}

function renderTips() {

    //if (marketId != $('#marketId').val() || analysisPeriodId != $('#analysisPeriodId').val() || minRateId != $('#minRateId').val() ||
    //    matchStateId != $('#matchStateId').val() || date != $('#earningDatetime').val()) {
    //    marketId = $('#marketId').val();
    //    analysisPeriodId = $('#analysisPeriodId').val();
    //    minRateId = $('#minRateId').val();
    //    matchStateId = $('#matchStateId').val();
    //    date = $('#earningDatetime').val();
    //    pageNo = 0;
    //    $('#divEarning').empty();
    //}
    //date = date.getFullYear() + "-" + date.getMonth() + "-" + date.getDay();
    $.ajax({
        type: "GET",
        url: "/Tips/TipsJson",
        data: { 'page': page },
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

            $("#divTips").append(
                $.render.tipsTemplate(data.tips)
            );

            $("#divLoading").hide();
            PreOdds.Utils.HideLoadingModal();
        },
        error: function () {
            $("#divLoading").hide();
            $("#divLoadMore").show();
            PreOdds.Utils.HideLoadingModal();
            PreOdds.Utils.ShowErrorNotification("İşleminiz sırasında bir hata oluştu, lütfen daha sonra tekrar deneyin.");
        }
    });
}