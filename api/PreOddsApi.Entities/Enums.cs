using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.Entities
{
    //public enum MarketType
    //{
    //    ThreeWayResult = 1,
    //    HomeAway = 10,
    //    OverUnder = 12,
    //    ThreeWayResult1stHalf = 37,
    //    OverUnder1stHalf = 38,
    //    OverUnder2ndHalf = 47,
    //    BothTeamstoScore = 59,
    //    DoubleChance = 63,
    //    TeamToScoreFirst = 69,
    //    TeamToScoreLast = 75,
    //    ThreeWayResult2ndHalf = 80,
    //    ThreeWayHandicap = 83,
    //    Handicap = 28,
    //    HTFTDouble = 53,
    //    GoalsOverUnder1stHalf = 975903,
    //    HTFTDouble = 975905,
    //    CleanSheetHome = 976096,
    //    BothTeamsToScore = 976105,
    //    CorrectScore = 975909,
    //    HighestScoringHalf = 976144,
    //    CorrectScore1stHalf = 975916,
    //    WinBothHalves = 976193,
    //    TotalHome = 976198,
    //    TotalAway = 976204,
    //    WinToNil = 976236,
    //    ExactGoalsNumber = 976241,
    //    ResultsBothTeamsToScore = 976316,
    //    ResultTotalGoals = 976334,
    //    HomeTeamScoreaGoal = 976348,
    //    AwayTeamScoreaGoal = 976360,
    //    CornersOverUnder = 976384,
    //    HandicapResult1stHalf = 976209,
    //    DoubleChance1stHalf = 975926,
    //    BothTeamsToScore1stHalf = 976226,
    //    BothTeamsToScore2ndHalf = 976230,
    //    OddEven = 975930,
    //    OddEven1stHalf = 975932,
    //    ToWinEitherHalf = 976265,
    //    HomeTeamExactGoalsNumber = 976270,
    //    AwayTeamExactGoalsNumber = 976286,
    //    SecondHalfExactGoalsNumber = 976298,
    //    First10minWinner = 976373,
    //    FirstHalfExactGoalsNumber = 976389,
    //    AsianHandicapFirstHalf = 975925,
    //    CleanSheetAway = 8594683,
    //    TeamToScoreFirst = 975923,
    //    TeamToScoreLast = 976187,
    //    FirstHalfWinner = 975929,
    //    Corners1x2 = 977179,
    //}

    public enum AnalysisPart
    {
        OneMonth = 0,
        ThreeMonth = 1,
        SixMonth = 2,
        OneYear = 3,
        All = 4
    }

    public enum AnalysisRatePart
    {
        Rate_1 = 0,
        Rate_110 = 1,
        Rate_125 = 2,
        Rate_150 = 3,
        Rate_200 = 4,
        Rate_500 = 5,
        Rate_1000 = 6,
    }
}
