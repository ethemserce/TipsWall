$.views.settings.debugMode(true);

$.templates("fixtureTemplate",
    {
        markup: "#fixtureTemplate",
        //helpers: myHelpers,
        templates: {
            standingBaseTemplate: "#standingBaseTemplate",
            standingTemplate: "#standingTemplate",
            homeLastMatchesBaseTemplate: "#homeLastMatchesBaseTemplate",
            awayLastMatchesBaseTemplate: "#awayLastMatchesBaseTemplate",
            hthLastMatchesBaseTemplate: "#hthLastMatchesBaseTemplate",
            lastMatchesTemplate: "#lastMatchesTemplate",
            eventsBaseTemplate: "#eventsBaseTemplate",
            eventsTemplate: "#eventsTemplate",
            statisticsTemplate: "#statisticsTemplate",
            statisticsAnalysisTemplate: "#statisticsAnalysisTemplate",
            formationTemplate: "#formationTemplate",
            formationDetailTemplate: "#formationDetailTemplate",
            fixtureOddsTemplate: "#fixtureOddsTemplate",
            fixtureOddsAnalysisTemplate: "#fixtureOddsAnalysisTemplate",
            cornersBaseTemplate: "#cornersBaseTemplate",
            cornerTemplate: "#cornerTemplate",
            commentTemplate: "#commentTemplate"
        }
    });

var fixtureId = $('#fixtureId').val();
var marketId = $('#markets').val();
var analysisPeriod = $('#analysisPeriod').val();

var updateDetailInterval;
var isMarketIdChange = 1;

$(document).ready(function () {
    renderList(fixtureId);
/*    renderOdds();*/
})

function renderList(fixtureId) {
    window.clearInterval(updateDetailInterval);
    $.ajax({
        type: "GET",
        url: "/Fixture/FixtureDetailJson",
        data: { 'fixtureId': fixtureId },
        cache: false,
        beforeSend: function () {
            $("#divLoading").show();
            $("#divFixtureDetailLoading").hide();
        },
        success: function (data) {

            $('#divFixtureDetail').empty();
            $("#divFixtureDetail").append(
                $.render.fixtureTemplate(data)
            );

            $('#divStatistics').empty();
            $("#divStatistics").append(
                $.render.statisticsTemplate(data)
            );

            $('#divHomeTeamForms').empty();
            $("#divHomeTeamForms").append(
                $.render.homeLastMatchesBaseTemplate(data)
            );
            $('#divHTHForms').empty();
            $("#divHTHForms").append(
                $.render.hthLastMatchesBaseTemplate(data)
            );
            $('#divAwayTeamForms').empty();
            $("#divAwayTeamForms").append(
                $.render.awayLastMatchesBaseTemplate(data)
            );

            $('#divEventList').empty();
            $("#divEventList").append(
                $.render.eventsBaseTemplate(data)
            );

            $('#divCornerList').empty();
            $("#divCornerList").append(
                $.render.cornersBaseTemplate(data)
            );

            $('#divStandings').empty();
            $("#divStandings").append(
                $.render.standingBaseTemplate(data)
            );

            $('#divStatisticsAnalysis').empty();
            $("#divStatisticsAnalysis").append(
                $.render.statisticsAnalysisTemplate(data)
            );

            $('#divformation').empty();
            $("#divformation").append(
                $.render.formationTemplate(data)
            );

            $('#divComment').empty();
            $("#divComment").append(
                $.render.commentTemplate(data)
            );

            $("#divLoading").hide();
            $("#divFixtureDetailLoading").show();


            if (data.runTimer) {
                updateDetailInterval = window.setInterval(update, 10 * 1000);
            }
            else {
                window.clearInterval(updateDetailInterval);
            }
        },
        error: function () {
            $("#divLoading").hide();
            PreOdds.Utils.ShowErrorNotification("İşleminiz sırasında bir hata oluştu, lütfen daha sonra tekrar deneyin.");
        }
    });
}

function update() {
    $.ajax({
        type: "GET",
        url: "/Fixture/FixtureDetailJson",
        data: { 'fixtureId': fixtureId },
        cache: false,
        //beforeSend: function () {
        //        $("#divLoading").show();
        //},
        success: function (data) {

            if (data != null) {
                $('#divFixtureDetail').empty();
                $("#divFixtureDetail").append(
                    $.render.fixtureTemplate(data)
                );

                $('#divStatistics').empty();
                $("#divStatistics").append(
                    $.render.statisticsTemplate(data)
                );

                //$('#divLastMatches').empty();
                //$("#divLastMatches").append(
                //    $.render.lastMatchesBaseTemplate(data)
                //);

                $('#divEventList').empty();
                $("#divEventList").append(
                    $.render.eventsBaseTemplate(data)
                );

                $('#divCornerList').empty();
                $("#divCornerList").append(
                    $.render.cornersBaseTemplate(data)
                );

                $('#divformation').empty();
                $("#divformation").append(
                    $.render.formationTemplate(data)
                );

                $('#divComment').empty();
                $("#divComment").append(
                    $.render.commentTemplate(data)
                );
            }
        },
        error: function () {
            $("#divLoading").hide();
            $("#divLoadMore").show();
            PreOdds.Utils.HideLoadingModal();
            PreOdds.Utils.ShowErrorNotification("İşleminiz sırasında bir hata oluştu, lütfen daha sonra tekrar deneyin.");
        }
    });
}

function renderOdds() {
    if (marketId != $('#markets').val() || analysisPeriod != $('#analysisPeriod').val()) {
        marketId = $('#markets').val();
        analysisPeriod = $('#analysisPeriod').val();
    }

    $.ajax({
        type: "GET",
        url: "/Fixture/FixtureOddsJson",
        data: { 'fixtureId': fixtureId, 'marketId': marketId, 'analysisPeriod': analysisPeriod },
        cache: false,
        beforeSend: function () {
            $("#divLoadingOdds").show();
            $("#divFixtureOdds").hide();
        },
        success: function (data) {
            $("#divLoadingOdds").hide();
            $("#divFixtureOdds").show();

            $('#divFixtureOdds').empty();
            $("#divFixtureOdds").append(
                $.render.fixtureOddsTemplate(data)
            );



        },
        error: function () {
            //$("#divLoading").hide();
            //$("#divLoadMore").show();
            //PreOdds.Utils.HideLoadingModal();
            //PreOdds.Utils.ShowErrorNotification("İşleminiz sırasında bir hata oluştu, lütfen daha sonra tekrar deneyin.");
        }
    });
}