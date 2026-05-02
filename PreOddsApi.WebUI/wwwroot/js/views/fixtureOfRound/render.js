//var myHelpers = { formatDecimal: formatDecimal };
$.templates("roundTemplate",
    {
        markup: "#roundTemplate",
        //helpers: myHelpers,
        templates: {
            roundFixturesTemplate: "#roundFixturesTemplate",
            standingBaseTemplate: "#standingBaseTemplate",
            standingTemplate:"#standingTemplate",
            //seasonTemplate: "#seasonTemplate",
            selectStageTemplate: "#selectStageTemplate",
            selectRoundTemplate: "#selectRoundTemplate",
            goalScorerTemplate: "#goalScorerTemplate",
            assistsScorerTemplate: "#assistsScorerTemplate",
            cardScorerTemplate: "#cardScorerTemplate",
            lastMatchesTemplate: "#lastMatchesTemplate",
            lastMatchesBaseTemplate:"#lastMatchesBaseTemplate"
        }
    });

var leagueId = $('#leagueId').val();
var seasonId = $('#selectSeasonId').val();
var stageId = $('#selectStageId').val();
var roundId = $('#selectRoundId').val();

$(document).ready(function () {
    renderRoundList();

    $("#selectSeasonId").on("change", function () {
        renderSelectStage();
    });

    $("#selectStageId").on("change", function () {
        renderSelectRound();
    });

    $("#selectRoundId").on("change", function () {
        renderRoundList();
    });
})


function renderSelectStage() {
    if (leagueId !== $('#leagueId').val() || seasonId !== $('#selectSeasonId').val() || stageId !== $('#selectStageId').val()) {
        leagueId = $('#leagueId').val();
        seasonId = $('#selectSeasonId').val();
        stageId = $('#selectStageId').val();

        //$('#divFixtureOfRound').empty();
        $('#divHomeTeamForms').empty();
    }
    $.ajax({
        type: "GET",
        url: "/League/LeagueDetailJson",
        data: { 'leagueId': leagueId, 'seasonId': seasonId, 'stageId': stageId },
        cache: false,
        beforeSend: function () {
            $("#divLoading").show();
            $("#divHomeTeamForms").hide();
        },
        success: function (data) {
            for (var i = 0; i < data.seasons.length; i++) {
                if (data.seasons[i].id == seasonId) {
                    $('#selectStageId').empty();
                    $("#selectStageId").append($.render.selectStageTemplate(data.seasons[i].stages));
                    for (var x = 0; x < data.seasons[i].stages.length; x++) {
                        if (data.seasons[i].stages[x].id == stageId)
                        {
                            $('#selectRoundId').empty();
                            $("#selectRoundId").append($.render.selectRoundTemplate(data.seasons[i].stages[x].rounds));
                            break;
                        }
                    }
                    break;
                }
            }

            $("#divLoading").hide();
            $("#divHomeTeamForms").show();
            //$('#divFixtureOfRound').empty();
            //$("#divFixtureOfRound").append($.render.roundTemplate(data.fixtureOfRounds));
            $('#divStanding').empty();
            $("#divStanding").append($.render.standingBaseTemplate(data));
            $('#divHomeTeamForms').empty();
            $("#divHomeTeamForms").append($.render.lastMatchesBaseTemplate(data.fixtureOfRounds));
        },
        error: function () {
            //$("#divLoading").hide();
            //$("#divLoadMore").show();
            //PreOdds.Utils.HideLoadingModal();
            //PreOdds.Utils.ShowErrorNotification("İşleminiz sırasında bir hata oluştu, lütfen daha sonra tekrar deneyin.");
        }
    });
}

function renderSelectRound() {
    if (leagueId !== $('#leagueId').val() || seasonId !== $('#selectSeasonId').val() || stageId !== $('#selectStageId').val()) {
        leagueId = $('#leagueId').val();
        seasonId = $('#selectSeasonId').val();
        stageId = $('#selectStageId').val();

        $('#divFixtureOfRound').empty();
    }
    $.ajax({
        type: "GET",
        url: "/League/LeagueDetailJson",
        data: { 'leagueId': leagueId, 'seasonId': seasonId, 'stageId': stageId },
        cache: false,
        beforeSend: function () {
            $("#divLoading").show();
            $("#divHomeTeamForms").hide();
        },
        success: function (data) {
            for (var i = 0; i < data.seasons.length; i++) {
                if (data.seasons[i].id == seasonId) {
                    for (var x = 0; x < data.seasons[i].stages.length; x++) {
                        if ((data.seasons[i].stages[x].id + "_" + data.seasons[i].stages[x].groupId) == stageId) {
                            $('#selectRoundId').empty();
                            $("#selectRoundId").append($.render.selectRoundTemplate(data.seasons[i].stages[x].rounds));
                            for (var a = 0; a < data.seasons[i].stages[x].rounds.length; a++) {
                                if (data.seasons[i].stages[x].rounds[a].currentRoundId == data.seasons[i].stages[x].rounds[a].id)
                                {
                                    $('#selectRoundId').val(data.seasons[i].stages[x].rounds[a].id).trigger('change');
                                }
                            }
                            break;
                        }
                    }
                    break;
                }
            }

            $("#divLoading").hide();
            $("#divHomeTeamForms").show();
            //$('#divFixtureOfRound').empty();
            //$("#divFixtureOfRound").append($.render.roundTemplate(data.fixtureOfRounds));
            $('#divStanding').empty();
            $("#divStanding").append($.render.standingBaseTemplate(data));

            $("#tbodyGoalScorer").empty();
            $("#tbodyGoalScorer").append($.render.goalScorerTemplate(data.topScorers.goalScorer));
            $("#tbodyAssistsScorer").empty();
            $("#tbodyAssistsScorer").append($.render.assistsScorerTemplate(data.topScorers.assistsScorer));
            $('#tbodyCardScorer').empty();
            $("#tbodyCardScorer").append($.render.cardScorerTemplate(data.topScorers.cardScorer));
            $('#divHomeTeamForms').empty();
            $("#divHomeTeamForms").append($.render.lastMatchesBaseTemplate(data.fixtureOfRounds));

        },
        error: function () {
            //$("#divLoading").hide();
            //$("#divLoadMore").show();
            //PreOdds.Utils.HideLoadingModal();
            //PreOdds.Utils.ShowErrorNotification("İşleminiz sırasında bir hata oluştu, lütfen daha sonra tekrar deneyin.");
        }
    });
};

function getRoundList() {
    $('#divFixtureOfRound').empty();
    renderRoundList();
}

function renderRoundList() {
    if (leagueId !== $('#leagueId').val() || seasonId !== $('#selectSeasonId').val() || stageId !== $('#selectStageId').val() || roundId !== $('#selectRoundId').val()) {
        leagueId = $('#leagueId').val();
        seasonId = $('#selectSeasonId').val();
        stageId = $('#selectStageId').val();
        roundId = $('#selectRoundId').val();

        $('#divFixtureOfRound').empty();
    }
    $.ajax({
        type: "GET",
        url: "/League/FixtureOfRound",
        data: { 'leagueId': leagueId, 'seasonId': seasonId, 'stageId': stageId, 'roundId': roundId },
        cache: false,
        beforeSend: function () {
            $("#divLoading").show();
            $("#divHomeTeamForms").hide();
        },
        success: function (data) {
            //$('#divFixtureOfRound').empty();
            //$("#divFixtureOfRound").append($.render.roundTemplate(data));
            $("#divLoading").hide();
            $("#divHomeTeamForms").show();

            $('#divHomeTeamForms').empty();
            $("#divHomeTeamForms").append($.render.lastMatchesBaseTemplate(data));
        },
        error: function () {
            //$("#divLoading").hide();
            //$("#divLoadMore").show();
            //PreOdds.Utils.HideLoadingModal();
            //PreOdds.Utils.ShowErrorNotification("İşleminiz sırasında bir hata oluştu, lütfen daha sonra tekrar deneyin.");
        }
    });
}