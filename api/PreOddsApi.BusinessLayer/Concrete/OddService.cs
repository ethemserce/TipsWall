using AutoMapper;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Odd;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.DataLayer;
using PreOddsApi.Entities.PreOddsEntities;
using PreOddsApi.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.BusinessLayer.Concrete
{
    public class OddService : IOddService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IAnalysisUnitOfWork<PreOddsApiDbContext> _analysisUnitOfWork;
        private readonly ITeamService _teamService;
        private readonly ICacheHelper _cacheHelper;
        private readonly IMapper _mapper;

        public OddService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IAnalysisUnitOfWork<PreOddsApiDbContext> analysisUnitOfWork, ITeamService teamService, ICacheHelper cacheHelper, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _analysisUnitOfWork = analysisUnitOfWork;
            _teamService = teamService;
            _cacheHelper = cacheHelper;
            _mapper = mapper;
        }

        public OddBusinessModel GetOdd(long id)
        {
            return _mapper.Map<OddBusinessModel>(_unitOfWork.Repository<odd>().Get(p => p.id == id && p.status == 1));
        }

        public List<OddBusinessModel> GetOdds(long fixtureId)
        {
            return _mapper.Map<List<OddBusinessModel>>(_unitOfWork.Repository<odd>().GetList(p => p.fixtureId == fixtureId && p.status == 1)).OrderBy(p => p.Id).ToList();
        }

        public List<OddBusinessModel> GetOdds(long fixtureId, long bookmarkerId)
        {
            return _mapper.Map<List<OddBusinessModel>>(_unitOfWork.Repository<odd>().GetList(p => p.fixtureId == fixtureId && p.bookmakerId == bookmarkerId && p.status == 1)).OrderBy(p => p.Id).ToList();
        }

        public List<OddBusinessModel> GetOdds(long fixtureId, long bookmarkerId, long marketId)
        {
            return _mapper.Map<List<OddBusinessModel>>(_unitOfWork.Repository<odd>().GetList(p => p.fixtureId == fixtureId && p.bookmakerId == bookmarkerId && p.marketId == marketId && p.status == 1)).OrderBy(p => p.Id).ToList();
        }

        public List<OddBusinessModel> GetOddsWithAnalysis(long fixtureId)
        {
            var fixture = _unitOfWork.Repository<fixture>().Get(p => p.id == fixtureId);
            //if (fixture.time_status == "AU" || fixture.time_status == "Deleted")
            //{
            //    return new List<OddBusinessModel>();
            //}

            var odds = _mapper.Map<List<OddBusinessModel>>(_unitOfWork.Repository<odd>().GetList(p => p.fixtureId == fixtureId && p.status == 1)).OrderBy(p => p.Id);

            foreach (var odd in odds)
            {
                // GetOddAnalysis(DateTime.Parse(fixture.time_starting_at_date), odd);
            }

            return odds.ToList();
        }

        public List<OddBusinessModel> GetOddsWithAnalysis(long fixtureId, long bookmakerId)
        {
            var odds = _mapper.Map<List<OddBusinessModel>>(_unitOfWork.Repository<odd>().GetList(p => p.fixtureId == fixtureId && p.bookmakerId == bookmakerId && p.status == 1));

            var fixture = _unitOfWork.Repository<fixture>().Get(p => p.id == fixtureId);
            //if (fixture.time_status == "AU" || fixture.time_status == "Deleted")
            //{
            //    return new List<OddBusinessModel>();
            //}
            //GetOddAnalysis(DateTime.Parse(fixture.time_starting_at_date).AddDays(-1).ToString("yyyy-MM-dd"), bookmarkerId, odds);
            GetOddAnalysis(fixture.startingAt.Value.AddDays(-1).ToString("yyyy-MM-dd"), bookmakerId, odds);

            return odds.OrderBy(p => p.Id).ToList();
        }

        public List<OddBusinessModel> GetOddsWithAnalysis(long fixtureId, long bookmarkerId, long marketId)
        {
            var fixture = _unitOfWork.Repository<fixture>().Get(p => p.id == fixtureId);
            //if (fixture.sta == "AU" || fixture.time_status == "Deleted")
            //{
            //    return new List<OddBusinessModel>();
            //}

            var odds = _mapper.Map<List<OddBusinessModel>>(_unitOfWork.Repository<odd>().GetList(p => p.fixtureId == fixtureId && p.bookmakerId == bookmarkerId && p.marketId == marketId && p.status == 1));

            //GetOddWinLostSeries(odd.Id);
            var maxDate = _analysisUnitOfWork.Repository<odd_analysis>().GetList().OrderBy(x=>x.create_date_time).Max(o => o.create_date_time);
            GetOddAnalysis(maxDate.ToString("yyyy-MM-dd"), bookmarkerId, marketId, odds);

            if (marketId == 12 || marketId == 38 || marketId == 47 || marketId == 975903)
            {
                return odds.OrderBy(p => p.OddTotal).ToList();
            }

            return odds.OrderBy(p => p.Id).ToList();
        }

        public List<HotRateBusinessModel> GetHotRateOdds(string date, long bookmarkerId, long marketId, int winningPercent, int earningPercente, int count, int odd_value)
        {
            if (count == 1)
            {
                //return GetHotRatesOneMonth(date, bookmarkerId, marketId, winningPercent, earningPercente, odd_value);
                return GetHotRatesOneMonth(date, bookmarkerId, marketId);
            }
            else if (count == 3)
            {
                //return GetHotRatesThreeMonth(date, bookmarkerId, marketId, winningPercent, earningPercente, odd_value);
                return GetHotRatesThreeMonth(date, bookmarkerId, marketId);
            }
            else if (count == 6)
            {
                //return GetHotRatesSixMonth(date, bookmarkerId, marketId, winningPercent, earningPercente, odd_value);
                return GetHotRatesSixMonth(date, bookmarkerId, marketId);
            }
            else if (count == 12)
            {
                //return GetHotRatesOneYear(date, bookmarkerId, marketId, winningPercent, earningPercente, odd_value);
                return GetHotRatesOneYear(date, bookmarkerId, marketId);
            }
            else if (count == 0)
            {
                //return GetHotRatesAll(date, bookmarkerId, marketId, winningPercent, earningPercente, odd_value);
                return GetHotRatesAll(date, bookmarkerId, marketId);
            }
            else
            {
                return new List<HotRateBusinessModel>();
            }
        }

        public List<HotRateBusinessModel> GetWinningPercenteOdds(string date, long bookmarkerId, long marketId, int winningPercent, int count, int odd_value)
        {
            if (count == 1)
            {
                //return GetWinningPercenteOneMonth(date, bookmarkerId, marketId, winningPercent, odd_value);
                return GetWinningPercenteOneMonth(date, bookmarkerId, marketId);
            }
            else if (count == 3)
            {
                //return GetWinningPercenteThreeMonth(date, bookmarkerId, marketId, winningPercent, odd_value);
                return GetWinningPercenteThreeMonth(date, bookmarkerId, marketId);
            }
            else if (count == 6)
            {
                //return GetWinningPercenteSixMonth(date, bookmarkerId, marketId, winningPercent, odd_value);
                return GetWinningPercenteSixMonth(date, bookmarkerId, marketId);
            }
            else if (count == 12)
            {
                //return GetWinningPercenteOneYear(date, bookmarkerId, marketId, winningPercent, odd_value);
                return GetWinningPercenteOneYear(date, bookmarkerId, marketId);
            }
            else if (count == 0)
            {
                //return GetWinningPercenteAll(date, bookmarkerId, marketId, winningPercent, odd_value);
                return GetWinningPercenteAll(date, bookmarkerId, marketId);
            }
            else
            {
                return new List<HotRateBusinessModel>();
            }
        }

        public List<HotRateBusinessModel> GetEarningPercenteOdds(string date, long bookmarkerId, long marketId, int count, int odd_value)
        {
            if (count == 1)
            {
                //return GetEarningPercenteOneMonth(date, bookmarkerId, marketId, odd_value);
                return GetEarningPercenteOneMonth(date, bookmarkerId, marketId);
            }
            else if (count == 3)
            {
                //return GetEarningPercenteThreeMonth(date, bookmarkerId, marketId, odd_value);
                return GetEarningPercenteThreeMonth(date, bookmarkerId, marketId);
            }
            else if (count == 6)
            {
                //return GetEarningPercenteSixMonth(date, bookmarkerId, marketId, odd_value);
                return GetEarningPercenteSixMonth(date, bookmarkerId, marketId);
            }
            else if (count == 12)
            {
                //return GetEarningPercenteOneYear(date, bookmarkerId, marketId, odd_value);
                return GetEarningPercenteOneYear(date, bookmarkerId, marketId);
            }
            else if (count == 0)
            {
                //return GetEarningPercenteAll(date, bookmarkerId, marketId, odd_value);
                return GetEarningPercenteAll(date, bookmarkerId, marketId);
            }
            else
            {
                return new List<HotRateBusinessModel>();
            }
        }

        private void GetOddAnalysis(string date, long bookmarkerId, long marketId, List<OddBusinessModel> odds)
        {
            var currentDate = DateTime.UtcNow;
            if (DateTime.TryParse(date, out currentDate))
            {
                // p.created_date <= date.Date &&
                List<odd_analysis> oddAnalysisList;
                var oddAnalysisCacheModel = (List<odd_analysis>)_cacheHelper.Get(string.Format(CacheKeys.HotRates, date, bookmarkerId, marketId));
                if (oddAnalysisCacheModel != null)
                {
                    oddAnalysisList = oddAnalysisCacheModel;
                }
                else
                {
                    oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().
                       GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId && p.create_date_time.Date == currentDate.Date).ToList();

                    _cacheHelper.Set(string.Format(CacheKeys.HotRates, date, bookmarkerId, marketId), oddAnalysisList, 45 * 60 * 60);
                }

                //List<odd_analysis> oddAnalysisList = _unitOfWork.Repository<odd_analysis>().GetList(p => p.created_date.ToString("yyyy-MM-dd") == date && p.bookmakerId == bookmarkerId &&
                //p.marketId == marketId ).ToList();

                int analysisCount = 0;
                if (oddAnalysisList != null)
                {
                    analysisCount = oddAnalysisList.Count();
                }

                Parallel.ForEach(odds, odd =>
                {
                    decimal oddGroupPercent = CalculateOddGroupPercent(odd, odds);
                    if (oddAnalysisList != null && analysisCount > 0)
                    {
                        var oddAnalysis = oddAnalysisList.Where(p => p.odd_label == odd.OddLabel && p.odd_total == (string.IsNullOrEmpty(odd.OddTotal) ? null : odd.OddTotal) && p.odd_value == odd.OddValue && p.odd_handicap == (string.IsNullOrEmpty(odd.OddHandicap) ? null : odd.OddHandicap)).OrderBy(p => p.id).FirstOrDefault();
                        if (oddAnalysis != null)
                        {
                            var OddAnalysisOneMonth = new OddAnalysisBusinessModel()
                            {
                                EarningPercent = oddAnalysis.earning_percent_1m,
                                LostCount = oddAnalysis.lost_count_1m.ToString(),
                                WinCount = oddAnalysis.win_count_1m.ToString(),
                                WinningPercent = oddAnalysis.winning_percent_1m,
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "OneMonth"
                            };
                            odd.OddAnalysis.Add(OddAnalysisOneMonth);

                            var OddAnalysisThreeMonth = new OddAnalysisBusinessModel()
                            {
                                EarningPercent = oddAnalysis.earning_percent_3m,
                                LostCount = oddAnalysis.lost_count_3m.ToString(),
                                WinCount = oddAnalysis.win_count_3m.ToString(),
                                WinningPercent = oddAnalysis.winning_percent_3m,
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "ThreeMonth"
                            };
                            odd.OddAnalysis.Add(OddAnalysisThreeMonth);

                            var OddAnalysisSixMonth = new OddAnalysisBusinessModel()
                            {
                                EarningPercent = oddAnalysis.earning_percent_6m,
                                LostCount = oddAnalysis.lost_count_6m.ToString(),
                                WinCount = oddAnalysis.win_count_6m.ToString(),
                                WinningPercent = oddAnalysis.winning_percent_6m,
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "SixMonth"
                            };
                            odd.OddAnalysis.Add(OddAnalysisSixMonth);

                            var OddAnalysisOneYear = new OddAnalysisBusinessModel()
                            {
                                EarningPercent = oddAnalysis.earning_percent_1y,
                                LostCount = oddAnalysis.lost_count_1y.ToString(),
                                WinCount = oddAnalysis.win_count_1y.ToString(),
                                WinningPercent = oddAnalysis.winning_percent_1y,
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "OneYear"
                            };
                            odd.OddAnalysis.Add(OddAnalysisOneYear);

                            var OddAnalysisAll = new OddAnalysisBusinessModel()
                            {
                                EarningPercent = oddAnalysis.earning_percent_all,
                                LostCount = oddAnalysis.lost_count_all.ToString(),
                                WinCount = oddAnalysis.win_count_all.ToString(),
                                WinningPercent = oddAnalysis.winning_percent_all,
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "All"
                            };
                            odd.OddAnalysis.Add(OddAnalysisAll);
                        }
                        else
                        {
                            var OddAnalysisOneMonth = new OddAnalysisBusinessModel()
                            {
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "OneMonth"
                            };
                            odd.OddAnalysis.Add(OddAnalysisOneMonth);

                            var OddAnalysisThreeMonth = new OddAnalysisBusinessModel()
                            {
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "ThreeMonth"
                            };
                            odd.OddAnalysis.Add(OddAnalysisThreeMonth);

                            var OddAnalysisSixMonth = new OddAnalysisBusinessModel()
                            {
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "SixMonth"
                            };
                            odd.OddAnalysis.Add(OddAnalysisSixMonth);

                            var OddAnalysisOneYear = new OddAnalysisBusinessModel()
                            {
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "OneYear"
                            };
                            odd.OddAnalysis.Add(OddAnalysisOneYear);

                            var OddAnalysisAll = new OddAnalysisBusinessModel()
                            {
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "All"
                            };
                            odd.OddAnalysis.Add(OddAnalysisAll);
                        }
                    }
                    else
                    {
                        var OddAnalysisOneMonth = new OddAnalysisBusinessModel()
                        {
                            OddGroupPercent = oddGroupPercent,
                            AnalysisType = "OneMonth"
                        };
                        odd.OddAnalysis.Add(OddAnalysisOneMonth);

                        var OddAnalysisThreeMonth = new OddAnalysisBusinessModel()
                        {
                            OddGroupPercent = oddGroupPercent,
                            AnalysisType = "ThreeMonth"
                        };
                        odd.OddAnalysis.Add(OddAnalysisThreeMonth);

                        var OddAnalysisSixMonth = new OddAnalysisBusinessModel()
                        {
                            OddGroupPercent = oddGroupPercent,
                            AnalysisType = "SixMonth"
                        };
                        odd.OddAnalysis.Add(OddAnalysisSixMonth);

                        var OddAnalysisOneYear = new OddAnalysisBusinessModel()
                        {
                            OddGroupPercent = oddGroupPercent,
                            AnalysisType = "OneYear"
                        };
                        odd.OddAnalysis.Add(OddAnalysisOneYear);

                        var OddAnalysisAll = new OddAnalysisBusinessModel()
                        {
                            OddGroupPercent = oddGroupPercent,
                            AnalysisType = "All"
                        };
                        odd.OddAnalysis.Add(OddAnalysisAll);
                    }
                });
            }
        }

        private void GetOddAnalysis(string date, long bookmakerId, List<OddBusinessModel> odds)
        {
            var currentDate = DateTime.UtcNow;
            if (DateTime.TryParse(date, out currentDate))
            {
                // p.created_date <= date.Date &&
                List<odd_analysis> oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().GetList(p => p.create_date_time.Date == currentDate.Date && p.bookmakerId == bookmakerId).ToList();

                Parallel.ForEach(odds, odd =>
                {
                    decimal oddGroupPercent = CalculateOddGroupPercent(odd, odds, odd.MarketId);
                    if (oddAnalysisList != null && oddAnalysisList.Count() > 0)
                    {
                        var oddAnalysis = oddAnalysisList.Where(p => p.odd_label == odd.OddLabel && p.odd_total == (string.IsNullOrEmpty(odd.OddTotal) ? null : odd.OddTotal) && p.odd_value == odd.OddValue && p.odd_handicap == (string.IsNullOrEmpty(odd.OddHandicap) ? null : odd.OddHandicap)).OrderBy(p => p.id).FirstOrDefault();
                        if (oddAnalysis != null)
                        {
                            var OddAnalysisOneMonth = new OddAnalysisBusinessModel()
                            {
                                EarningPercent = oddAnalysis.earning_percent_1m,
                                LostCount = oddAnalysis.lost_count_1m.ToString(),
                                WinCount = oddAnalysis.win_count_1m.ToString(),
                                WinningPercent = oddAnalysis.winning_percent_1m,
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "OneMonth"
                            };
                            odd.OddAnalysis.Add(OddAnalysisOneMonth);

                            var OddAnalysisThreeMonth = new OddAnalysisBusinessModel()
                            {
                                EarningPercent = oddAnalysis.earning_percent_3m,
                                LostCount = oddAnalysis.lost_count_3m.ToString(),
                                WinCount = oddAnalysis.win_count_3m.ToString(),
                                WinningPercent = oddAnalysis.winning_percent_3m,
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "ThreeMonth"
                            };
                            odd.OddAnalysis.Add(OddAnalysisThreeMonth);

                            var OddAnalysisSixMonth = new OddAnalysisBusinessModel()
                            {
                                EarningPercent = oddAnalysis.earning_percent_6m,
                                LostCount = oddAnalysis.lost_count_6m.ToString(),
                                WinCount = oddAnalysis.win_count_6m.ToString(),
                                WinningPercent = oddAnalysis.winning_percent_6m,
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "SixMonth"
                            };
                            odd.OddAnalysis.Add(OddAnalysisSixMonth);

                            var OddAnalysisOneYear = new OddAnalysisBusinessModel()
                            {
                                EarningPercent = oddAnalysis.earning_percent_1y,
                                LostCount = oddAnalysis.lost_count_1y.ToString(),
                                WinCount = oddAnalysis.win_count_1y.ToString(),
                                WinningPercent = oddAnalysis.winning_percent_1y,
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "OneYear"
                            };
                            odd.OddAnalysis.Add(OddAnalysisOneYear);

                            var OddAnalysisAll = new OddAnalysisBusinessModel()
                            {
                                EarningPercent = oddAnalysis.earning_percent_all,
                                LostCount = oddAnalysis.lost_count_all.ToString(),
                                WinCount = oddAnalysis.win_count_all.ToString(),
                                WinningPercent = oddAnalysis.winning_percent_all,
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "All"
                            };
                            odd.OddAnalysis.Add(OddAnalysisAll);
                        }
                        else
                        {
                            var OddAnalysisOneMonth = new OddAnalysisBusinessModel()
                            {
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "OneMonth"
                            };
                            odd.OddAnalysis.Add(OddAnalysisOneMonth);

                            var OddAnalysisThreeMonth = new OddAnalysisBusinessModel()
                            {
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "ThreeMonth"
                            };
                            odd.OddAnalysis.Add(OddAnalysisThreeMonth);

                            var OddAnalysisSixMonth = new OddAnalysisBusinessModel()
                            {
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "SixMonth"
                            };
                            odd.OddAnalysis.Add(OddAnalysisSixMonth);

                            var OddAnalysisOneYear = new OddAnalysisBusinessModel()
                            {
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "OneYear"
                            };
                            odd.OddAnalysis.Add(OddAnalysisOneYear);

                            var OddAnalysisAll = new OddAnalysisBusinessModel()
                            {
                                OddGroupPercent = oddGroupPercent,
                                AnalysisType = "All"
                            };
                            odd.OddAnalysis.Add(OddAnalysisAll);
                        }
                    }
                    else
                    {
                        var OddAnalysisOneMonth = new OddAnalysisBusinessModel()
                        {
                            OddGroupPercent = oddGroupPercent,
                            AnalysisType = "OneMonth"
                        };
                        odd.OddAnalysis.Add(OddAnalysisOneMonth);

                        var OddAnalysisThreeMonth = new OddAnalysisBusinessModel()
                        {
                            OddGroupPercent = oddGroupPercent,
                            AnalysisType = "ThreeMonth"
                        };
                        odd.OddAnalysis.Add(OddAnalysisThreeMonth);

                        var OddAnalysisSixMonth = new OddAnalysisBusinessModel()
                        {
                            OddGroupPercent = oddGroupPercent,
                            AnalysisType = "SixMonth"
                        };
                        odd.OddAnalysis.Add(OddAnalysisSixMonth);

                        var OddAnalysisOneYear = new OddAnalysisBusinessModel()
                        {
                            OddGroupPercent = oddGroupPercent,
                            AnalysisType = "OneYear"
                        };
                        odd.OddAnalysis.Add(OddAnalysisOneYear);

                        var OddAnalysisAll = new OddAnalysisBusinessModel()
                        {
                            OddGroupPercent = oddGroupPercent,
                            AnalysisType = "All"
                        };
                        odd.OddAnalysis.Add(OddAnalysisAll);
                    }
                });
            }
        }

        private decimal CalculateOddGroupPercent(OddBusinessModel odd)
        {
            if (odd.OddValue == "0")
            {
                return 0;
            }

            var odds = _unitOfWork.Repository<odd>().GetList(p => p.fixtureId == odd.FixtureId && p.bookmakerId == odd.BookmakerId && p.marketId == odd.MarketId && p.total == odd.OddTotal && p.handicap == odd.OddHandicap);
            decimal karPayi = 0;
            foreach (var item in odds)
            {
                if (item.value == "0")
                {
                    continue;
                }
                else
                {
                    karPayi += Convert.ToDecimal(1 / decimal.Parse(item.value));
                }
            }

            decimal dagitilacakTutar = 1 / karPayi;

            decimal dagitilacakTutarOrani = 100 * dagitilacakTutar;

            return Math.Round(Convert.ToDecimal(Convert.ToDecimal(1 / decimal.Parse(odd.OddValue)) * dagitilacakTutarOrani), 2);
        }

        private decimal CalculateOddGroupPercent(OddBusinessModel odd, List<OddBusinessModel> oddList)
        {
            if (odd.OddValue == "0")
            {
                return 0;
            }

            var odds = oddList.Where(p => p.OddTotal == odd.OddTotal && p.OddHandicap == odd.OddHandicap); //_unitOfWork.Repository<odd>().GetList(p => p.fixtureId == odd.FixtureId && p.bookmakerId == odd.BookmarkerId && p.marketId == odd.MarketId && p.odd_total == odd.OddTotal && p.odd_handicap == odd.OddHandicap);

            decimal karPayi = 0;
            foreach (var item in odds)
            {
                if (item.OddValue == "0")
                {
                    continue;
                }
                else
                {
                    karPayi += Convert.ToDecimal(1 / decimal.Parse(item.OddValue));
                }
            }

            decimal dagitilacakTutar = 1 / karPayi;

            decimal dagitilacakTutarOrani = 100 * dagitilacakTutar;

            return Math.Round(Convert.ToDecimal(Convert.ToDecimal(1 / decimal.Parse(odd.OddValue)) * dagitilacakTutarOrani), 2);
        }

        private decimal CalculateOddGroupPercent(OddBusinessModel odd, List<OddBusinessModel> oddList, long marketId)
        {
            if (odd.OddValue == "0")
            {
                return 0;
            }

            var odds = oddList.Where(p => p.MarketId == marketId && p.OddTotal == odd.OddTotal && p.OddHandicap == odd.OddHandicap); //_unitOfWork.Repository<odd>().GetList(p => p.fixtureId == odd.FixtureId && p.bookmakerId == odd.BookmarkerId && p.marketId == odd.MarketId && p.odd_total == odd.OddTotal && p.odd_handicap == odd.OddHandicap);
            decimal karPayi = 0;
            foreach (var item in odds)
            {
                if (item.OddValue == "0")
                {
                    continue;
                }
                else
                {
                    karPayi += Convert.ToDecimal(1 / decimal.Parse(item.OddValue));
                }
            }

            decimal dagitilacakTutar = 1 / karPayi;

            decimal dagitilacakTutarOrani = 100 * dagitilacakTutar;

            return Math.Round(Convert.ToDecimal(Convert.ToDecimal(1 / decimal.Parse(odd.OddValue)) * dagitilacakTutarOrani), 2);
        }

        private List<HotRateBusinessModel> GetHotRatesOneMonth(string date, long bookmarkerId, long marketId, int winningPercent, int earningPercente, int odd_value)
        {
            var oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().
                GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId && p.create_date_time.ToString("yyyy-MM-dd") == date && int.Parse(p.odd_value.Replace(".", "")) >= odd_value &&
                p.winning_percent_1m >= winningPercent && p.earning_percent_1m >= earningPercente && (p.win_count_1m + p.lost_count_1m) > 0);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            foreach (var oddAnalysis in oddAnalysisList)
            {
                HotRateBusinessModel hotrate = new HotRateBusinessModel()
                {
                    Id = oddAnalysis.id,
                    OddHandicap = oddAnalysis.odd_handicap,
                    OddLabel = oddAnalysis.odd_label,
                    OddTotal = oddAnalysis.odd_total,
                    OddValue = oddAnalysis.odd_value,
                    EarningPercent = oddAnalysis.earning_percent_1m,
                    LostCount = oddAnalysis.lost_count_1m.ToString(),
                    WinCount = oddAnalysis.win_count_1m.ToString(),
                    WinningPercent = oddAnalysis.winning_percent_1m,
                };
                hotRateList.Add(hotrate);
            }

            return hotRateList;
        }
        private List<HotRateBusinessModel> GetHotRatesOneMonth(string date, long bookmarkerId, long marketId)
        {
            List<odd_analysis> oddAnalysisList = GetOddAnalysisList(date, bookmarkerId, marketId);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            Parallel.ForEach(oddAnalysisList, oddAnalysis =>
            {
                if ((oddAnalysis.win_count_1m + oddAnalysis.lost_count_1m) > 0)
                {
                    HotRateBusinessModel hotrate = new HotRateBusinessModel()
                    {
                        Id = oddAnalysis.id,
                        OddHandicap = oddAnalysis.odd_handicap,
                        OddLabel = oddAnalysis.odd_label,
                        OddTotal = oddAnalysis.odd_total,
                        OddValue = oddAnalysis.odd_value,
                        EarningPercent = oddAnalysis.earning_percent_1m,
                        LostCount = oddAnalysis.lost_count_1m.ToString(),
                        WinCount = oddAnalysis.win_count_1m.ToString(),
                        WinningPercent = oddAnalysis.winning_percent_1m,
                    };
                    hotRateList.Add(hotrate);
                }
            });

            //foreach (var oddAnalysis in oddAnalysisList)
            //{
            //    if ((oddAnalysis.win_count_1m + oddAnalysis.lost_count_1m) > 0)
            //    {
            //        HotRateBusinessModel hotrate = new HotRateBusinessModel()
            //        {
            //            Id = oddAnalysis.id,
            //            OddHandicap = oddAnalysis.odd_handicap,
            //            OddLabel = oddAnalysis.odd_label,
            //            OddTotal = oddAnalysis.odd_total,
            //            OddValue = oddAnalysis.odd_value,
            //            EarningPercent = oddAnalysis.earning_percent_1m,
            //            LostCount = oddAnalysis.lost_count_1m.ToString(),
            //            WinCount = oddAnalysis.win_count_1m.ToString(),
            //            WinningPercent = oddAnalysis.winning_percent_1m,
            //        };
            //        hotRateList.Add(hotrate);
            //    }
            //}

            return hotRateList;
        }

        private List<HotRateBusinessModel> GetHotRatesThreeMonth(string date, long bookmarkerId, long marketId, int winningPercent, int earningPercente, int odd_value)
        {
            var oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().
                GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId && p.create_date_time.ToString("yyyy-MM-dd") == date && int.Parse(p.odd_value.Replace(".", "")) >= odd_value &&
                p.winning_percent_3m >= winningPercent && p.earning_percent_3m >= earningPercente && (p.win_count_3m + p.lost_count_3m) > 0);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            foreach (var oddAnalysis in oddAnalysisList)
            {
                HotRateBusinessModel hotrate = new HotRateBusinessModel()
                {
                    Id = oddAnalysis.id,
                    OddHandicap = oddAnalysis.odd_handicap,
                    OddLabel = oddAnalysis.odd_label,
                    OddTotal = oddAnalysis.odd_total,
                    OddValue = oddAnalysis.odd_value,
                    EarningPercent = oddAnalysis.earning_percent_3m,
                    LostCount = oddAnalysis.lost_count_3m.ToString(),
                    WinCount = oddAnalysis.win_count_3m.ToString(),
                    WinningPercent = oddAnalysis.winning_percent_3m,
                };

                hotRateList.Add(hotrate);
            }

            return hotRateList;
        }
        private List<HotRateBusinessModel> GetHotRatesThreeMonth(string date, long bookmarkerId, long marketId)
        {
            List<odd_analysis> oddAnalysisList = GetOddAnalysisList(date, bookmarkerId, marketId);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            Parallel.ForEach(oddAnalysisList, oddAnalysis =>
            {
                if ((oddAnalysis.win_count_3m + oddAnalysis.lost_count_3m) > 0)
                {
                    HotRateBusinessModel hotrate = new HotRateBusinessModel()
                    {
                        Id = oddAnalysis.id,
                        OddHandicap = oddAnalysis.odd_handicap,
                        OddLabel = oddAnalysis.odd_label,
                        OddTotal = oddAnalysis.odd_total,
                        OddValue = oddAnalysis.odd_value,
                        EarningPercent = oddAnalysis.earning_percent_3m,
                        LostCount = oddAnalysis.lost_count_3m.ToString(),
                        WinCount = oddAnalysis.win_count_3m.ToString(),
                        WinningPercent = oddAnalysis.winning_percent_3m,
                    };

                    hotRateList.Add(hotrate);
                }
            });
            //foreach (var oddAnalysis in oddAnalysisList)
            //{
            //    if ((oddAnalysis.win_count_3m + oddAnalysis.lost_count_3m) > 0)
            //    {
            //        HotRateBusinessModel hotrate = new HotRateBusinessModel()
            //        {
            //            Id = oddAnalysis.id,
            //            OddHandicap = oddAnalysis.odd_handicap,
            //            OddLabel = oddAnalysis.odd_label,
            //            OddTotal = oddAnalysis.odd_total,
            //            OddValue = oddAnalysis.odd_value,
            //            EarningPercent = oddAnalysis.earning_percent_3m,
            //            LostCount = oddAnalysis.lost_count_3m.ToString(),
            //            WinCount = oddAnalysis.win_count_3m.ToString(),
            //            WinningPercent = oddAnalysis.winning_percent_3m,
            //        };

            //        hotRateList.Add(hotrate);
            //    }
            //}

            return hotRateList;
        }

        private List<HotRateBusinessModel> GetHotRatesSixMonth(string date, long bookmarkerId, long marketId, int winningPercent, int earningPercente, int odd_value)
        {
            var oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().
                GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId && p.create_date_time.ToString("yyyy-MM-dd") == date && int.Parse(p.odd_value.Replace(".", "")) >= odd_value &&
                p.winning_percent_6m >= winningPercent && p.earning_percent_6m >= earningPercente && (p.win_count_6m + p.lost_count_6m) > 0);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            foreach (var oddAnalysis in oddAnalysisList)
            {
                HotRateBusinessModel hotrate = new HotRateBusinessModel()
                {
                    Id = oddAnalysis.id,
                    OddHandicap = oddAnalysis.odd_handicap,
                    OddLabel = oddAnalysis.odd_label,
                    OddTotal = oddAnalysis.odd_total,
                    OddValue = oddAnalysis.odd_value,
                    EarningPercent = oddAnalysis.earning_percent_6m,
                    LostCount = oddAnalysis.lost_count_6m.ToString(),
                    WinCount = oddAnalysis.win_count_6m.ToString(),
                    WinningPercent = oddAnalysis.winning_percent_6m,
                };

                hotRateList.Add(hotrate);
            }

            return hotRateList;
        }
        private List<HotRateBusinessModel> GetHotRatesSixMonth(string date, long bookmarkerId, long marketId)
        {
            List<odd_analysis> oddAnalysisList = GetOddAnalysisList(date, bookmarkerId, marketId);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            Parallel.ForEach(oddAnalysisList, oddAnalysis =>
            {
                if ((oddAnalysis.win_count_6m + oddAnalysis.lost_count_6m) > 0)
                {
                    HotRateBusinessModel hotrate = new HotRateBusinessModel()
                    {
                        Id = oddAnalysis.id,
                        OddHandicap = oddAnalysis.odd_handicap,
                        OddLabel = oddAnalysis.odd_label,
                        OddTotal = oddAnalysis.odd_total,
                        OddValue = oddAnalysis.odd_value,
                        EarningPercent = oddAnalysis.earning_percent_6m,
                        LostCount = oddAnalysis.lost_count_6m.ToString(),
                        WinCount = oddAnalysis.win_count_6m.ToString(),
                        WinningPercent = oddAnalysis.winning_percent_6m,
                    };

                    hotRateList.Add(hotrate);
                }
            });
            //foreach (var oddAnalysis in oddAnalysisList)
            //{
            //    if ((oddAnalysis.win_count_6m + oddAnalysis.lost_count_6m) > 0)
            //    {
            //        HotRateBusinessModel hotrate = new HotRateBusinessModel()
            //        {
            //            Id = oddAnalysis.id,
            //            OddHandicap = oddAnalysis.odd_handicap,
            //            OddLabel = oddAnalysis.odd_label,
            //            OddTotal = oddAnalysis.odd_total,
            //            OddValue = oddAnalysis.odd_value,
            //            EarningPercent = oddAnalysis.earning_percent_6m,
            //            LostCount = oddAnalysis.lost_count_6m.ToString(),
            //            WinCount = oddAnalysis.win_count_6m.ToString(),
            //            WinningPercent = oddAnalysis.winning_percent_6m,
            //        };

            //        hotRateList.Add(hotrate);
            //    }
            //}

            return hotRateList;
        }

        private List<HotRateBusinessModel> GetHotRatesOneYear(string date, long bookmarkerId, long marketId, int winningPercent, int earningPercente, int odd_value)
        {
            var oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().
                GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId && p.create_date_time.ToString("yyyy-MM-dd") == date && int.Parse(p.odd_value.Replace(".", "")) >= odd_value &&
                p.winning_percent_1y >= winningPercent && p.earning_percent_1y >= earningPercente && (p.win_count_1y + p.lost_count_1y) > 0);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            foreach (var oddAnalysis in oddAnalysisList)
            {
                HotRateBusinessModel hotrate = new HotRateBusinessModel()
                {
                    Id = oddAnalysis.id,
                    OddHandicap = oddAnalysis.odd_handicap,
                    OddLabel = oddAnalysis.odd_label,
                    OddTotal = oddAnalysis.odd_total,
                    OddValue = oddAnalysis.odd_value,
                    EarningPercent = oddAnalysis.earning_percent_1y,
                    LostCount = oddAnalysis.lost_count_1y.ToString(),
                    WinCount = oddAnalysis.win_count_1y.ToString(),
                    WinningPercent = oddAnalysis.winning_percent_1y,
                };

                hotRateList.Add(hotrate);
            }

            return hotRateList;
        }
        private List<HotRateBusinessModel> GetHotRatesOneYear(string date, long bookmarkerId, long marketId)
        {
            List<odd_analysis> oddAnalysisList = GetOddAnalysisList(date, bookmarkerId, marketId);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            Parallel.ForEach(oddAnalysisList, oddAnalysis =>
            {
                if ((oddAnalysis.win_count_1y + oddAnalysis.lost_count_1y) > 0)
                {
                    HotRateBusinessModel hotrate = new HotRateBusinessModel()
                    {
                        Id = oddAnalysis.id,
                        OddHandicap = oddAnalysis.odd_handicap,
                        OddLabel = oddAnalysis.odd_label,
                        OddTotal = oddAnalysis.odd_total,
                        OddValue = oddAnalysis.odd_value,
                        EarningPercent = oddAnalysis.earning_percent_1y,
                        LostCount = oddAnalysis.lost_count_1y.ToString(),
                        WinCount = oddAnalysis.win_count_1y.ToString(),
                        WinningPercent = oddAnalysis.winning_percent_1y,
                    };

                    hotRateList.Add(hotrate);
                }
            });
            //foreach (var oddAnalysis in oddAnalysisList)
            //{
            //    if ((oddAnalysis.win_count_1y + oddAnalysis.lost_count_1y) > 0)
            //    {
            //        HotRateBusinessModel hotrate = new HotRateBusinessModel()
            //        {
            //            Id = oddAnalysis.id,
            //            OddHandicap = oddAnalysis.odd_handicap,
            //            OddLabel = oddAnalysis.odd_label,
            //            OddTotal = oddAnalysis.odd_total,
            //            OddValue = oddAnalysis.odd_value,
            //            EarningPercent = oddAnalysis.earning_percent_1y,
            //            LostCount = oddAnalysis.lost_count_1y.ToString(),
            //            WinCount = oddAnalysis.win_count_1y.ToString(),
            //            WinningPercent = oddAnalysis.winning_percent_1y,
            //        };

            //        hotRateList.Add(hotrate);
            //    }
            //}

            return hotRateList;
        }

        private List<HotRateBusinessModel> GetHotRatesAll(string date, long bookmarkerId, long marketId, int winningPercent, int earningPercente, int odd_value)
        {
            //
            var oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().
                GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId && p.create_date_time.ToString("yyyy-MM-dd") == date && int.Parse(p.odd_value.Replace(".", "")) >= odd_value &&
                p.winning_percent_all >= winningPercent && p.earning_percent_all >= earningPercente && (p.win_count_all + p.lost_count_all) > 0);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            foreach (var oddAnalysis in oddAnalysisList)
            {
                HotRateBusinessModel hotrate = new HotRateBusinessModel()
                {
                    Id = oddAnalysis.id,
                    OddHandicap = oddAnalysis.odd_handicap,
                    OddLabel = oddAnalysis.odd_label,
                    OddTotal = oddAnalysis.odd_total,
                    OddValue = oddAnalysis.odd_value,
                    EarningPercent = oddAnalysis.earning_percent_all,
                    LostCount = oddAnalysis.lost_count_all.ToString(),
                    WinCount = oddAnalysis.win_count_all.ToString(),
                    WinningPercent = oddAnalysis.winning_percent_all,
                };

                hotRateList.Add(hotrate);
            }

            return hotRateList;
        }
        private List<HotRateBusinessModel> GetHotRatesAll(string date, long bookmarkerId, long marketId)
        {
            List<odd_analysis> oddAnalysisList = GetOddAnalysisList(date, bookmarkerId, marketId);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            Parallel.ForEach(oddAnalysisList, oddAnalysis =>
            {
                if ((oddAnalysis.win_count_all + oddAnalysis.lost_count_all) > 0)
                {
                    HotRateBusinessModel hotrate = new HotRateBusinessModel()
                    {
                        Id = oddAnalysis.id,
                        OddHandicap = oddAnalysis.odd_handicap,
                        OddLabel = oddAnalysis.odd_label,
                        OddTotal = oddAnalysis.odd_total,
                        OddValue = oddAnalysis.odd_value,
                        EarningPercent = oddAnalysis.earning_percent_all,
                        LostCount = oddAnalysis.lost_count_all.ToString(),
                        WinCount = oddAnalysis.win_count_all.ToString(),
                        WinningPercent = oddAnalysis.winning_percent_all,
                    };

                    hotRateList.Add(hotrate);
                }
            });
            //foreach (var oddAnalysis in oddAnalysisList)
            //{
            //    if ((oddAnalysis.win_count_all + oddAnalysis.lost_count_all) > 0)
            //    {
            //        HotRateBusinessModel hotrate = new HotRateBusinessModel()
            //        {
            //            Id = oddAnalysis.id,
            //            OddHandicap = oddAnalysis.odd_handicap,
            //            OddLabel = oddAnalysis.odd_label,
            //            OddTotal = oddAnalysis.odd_total,
            //            OddValue = oddAnalysis.odd_value,
            //            EarningPercent = oddAnalysis.earning_percent_all,
            //            LostCount = oddAnalysis.lost_count_all.ToString(),
            //            WinCount = oddAnalysis.win_count_all.ToString(),
            //            WinningPercent = oddAnalysis.winning_percent_all,
            //        };

            //        hotRateList.Add(hotrate);
            //    }
            //}

            return hotRateList;
        }

        private List<HotRateBusinessModel> GetWinningPercenteOneMonth(string date, long bookmarkerId, long marketId, int winningPercent, int odd_value)
        {
            var oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().
                GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId && p.create_date_time.ToString("yyyy-MM-dd") == date && int.Parse(p.odd_value.Replace(".", "")) >= odd_value &&
                p.winning_percent_1m >= winningPercent && (p.win_count_1m + p.lost_count_1m) > 0);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            foreach (var oddAnalysis in oddAnalysisList)
            {
                HotRateBusinessModel hotrate = new HotRateBusinessModel()
                {
                    Id = oddAnalysis.id,
                    OddHandicap = oddAnalysis.odd_handicap,
                    OddLabel = oddAnalysis.odd_label,
                    OddTotal = oddAnalysis.odd_total,
                    OddValue = oddAnalysis.odd_value,
                    EarningPercent = oddAnalysis.earning_percent_1m,
                    LostCount = oddAnalysis.lost_count_1m.ToString(),
                    WinCount = oddAnalysis.win_count_1m.ToString(),
                    WinningPercent = oddAnalysis.winning_percent_1m,
                };

                hotRateList.Add(hotrate);
            }

            return hotRateList;
        }
        private List<HotRateBusinessModel> GetWinningPercenteOneMonth(string date, long bookmarkerId, long marketId)
        {
            List<odd_analysis> oddAnalysisList = GetOddAnalysisList(date, bookmarkerId, marketId);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            Parallel.ForEach(oddAnalysisList, oddAnalysis =>
            {
                if ((oddAnalysis.win_count_1m + oddAnalysis.lost_count_1m) > 0)
                {
                    HotRateBusinessModel hotrate = new HotRateBusinessModel()
                    {
                        Id = oddAnalysis.id,
                        OddHandicap = oddAnalysis.odd_handicap,
                        OddLabel = oddAnalysis.odd_label,
                        OddTotal = oddAnalysis.odd_total,
                        OddValue = oddAnalysis.odd_value,
                        EarningPercent = oddAnalysis.earning_percent_1m,
                        LostCount = oddAnalysis.lost_count_1m.ToString(),
                        WinCount = oddAnalysis.win_count_1m.ToString(),
                        WinningPercent = oddAnalysis.winning_percent_1m,
                    };

                    hotRateList.Add(hotrate);
                }
            });
            //foreach (var oddAnalysis in oddAnalysisList)
            //{
            //    if ((oddAnalysis.win_count_1m + oddAnalysis.lost_count_1m) > 0)
            //    {
            //        HotRateBusinessModel hotrate = new HotRateBusinessModel()
            //        {
            //            Id = oddAnalysis.id,
            //            OddHandicap = oddAnalysis.odd_handicap,
            //            OddLabel = oddAnalysis.odd_label,
            //            OddTotal = oddAnalysis.odd_total,
            //            OddValue = oddAnalysis.odd_value,
            //            EarningPercent = oddAnalysis.earning_percent_1m,
            //            LostCount = oddAnalysis.lost_count_1m.ToString(),
            //            WinCount = oddAnalysis.win_count_1m.ToString(),
            //            WinningPercent = oddAnalysis.winning_percent_1m,
            //        };

            //        hotRateList.Add(hotrate);
            //    }
            //}

            return hotRateList;
        }

        private List<HotRateBusinessModel> GetWinningPercenteThreeMonth(string date, long bookmarkerId, long marketId, int winningPercent, int odd_value)
        {
            var oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().
                GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId && p.create_date_time.ToString("yyyy-MM-dd") == date && int.Parse(p.odd_value.Replace(".", "")) >= odd_value &&
                p.winning_percent_3m >= winningPercent && (p.win_count_3m + p.lost_count_3m) > 0);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            foreach (var oddAnalysis in oddAnalysisList)
            {
                HotRateBusinessModel hotrate = new HotRateBusinessModel()
                {
                    Id = oddAnalysis.id,
                    OddHandicap = oddAnalysis.odd_handicap,
                    OddLabel = oddAnalysis.odd_label,
                    OddTotal = oddAnalysis.odd_total,
                    OddValue = oddAnalysis.odd_value,
                    EarningPercent = oddAnalysis.earning_percent_3m,
                    LostCount = oddAnalysis.lost_count_3m.ToString(),
                    WinCount = oddAnalysis.win_count_3m.ToString(),
                    WinningPercent = oddAnalysis.winning_percent_3m,
                };

                hotRateList.Add(hotrate);
            }

            return hotRateList;
        }
        private List<HotRateBusinessModel> GetWinningPercenteThreeMonth(string date, long bookmarkerId, long marketId)
        {
            List<odd_analysis> oddAnalysisList = GetOddAnalysisList(date, bookmarkerId, marketId);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            Parallel.ForEach(oddAnalysisList, oddAnalysis =>
            {
                if ((oddAnalysis.win_count_3m + oddAnalysis.lost_count_3m) > 0)
                {
                    HotRateBusinessModel hotrate = new HotRateBusinessModel()
                    {
                        Id = oddAnalysis.id,
                        OddHandicap = oddAnalysis.odd_handicap,
                        OddLabel = oddAnalysis.odd_label,
                        OddTotal = oddAnalysis.odd_total,
                        OddValue = oddAnalysis.odd_value,
                        EarningPercent = oddAnalysis.earning_percent_3m,
                        LostCount = oddAnalysis.lost_count_3m.ToString(),
                        WinCount = oddAnalysis.win_count_3m.ToString(),
                        WinningPercent = oddAnalysis.winning_percent_3m,
                    };

                    hotRateList.Add(hotrate);
                }
            });
            //foreach (var oddAnalysis in oddAnalysisList)
            //{
            //    if ((oddAnalysis.win_count_3m + oddAnalysis.lost_count_3m) > 0)
            //    {
            //        HotRateBusinessModel hotrate = new HotRateBusinessModel()
            //        {
            //            Id = oddAnalysis.id,
            //            OddHandicap = oddAnalysis.odd_handicap,
            //            OddLabel = oddAnalysis.odd_label,
            //            OddTotal = oddAnalysis.odd_total,
            //            OddValue = oddAnalysis.odd_value,
            //            EarningPercent = oddAnalysis.earning_percent_3m,
            //            LostCount = oddAnalysis.lost_count_3m.ToString(),
            //            WinCount = oddAnalysis.win_count_3m.ToString(),
            //            WinningPercent = oddAnalysis.winning_percent_3m,
            //        };

            //        hotRateList.Add(hotrate);
            //    }
            //}

            return hotRateList;
        }

        private List<HotRateBusinessModel> GetWinningPercenteSixMonth(string date, long bookmarkerId, long marketId, int winningPercent, int odd_value)
        {
            var oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().
                GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId && p.create_date_time.ToString("yyyy-MM-dd") == date && int.Parse(p.odd_value.Replace(".", "")) >= odd_value &&
                p.winning_percent_6m >= winningPercent && (p.win_count_6m + p.lost_count_6m) > 0);
            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            foreach (var oddAnalysis in oddAnalysisList)
            {
                HotRateBusinessModel hotrate = new HotRateBusinessModel()
                {
                    Id = oddAnalysis.id,
                    OddHandicap = oddAnalysis.odd_handicap,
                    OddLabel = oddAnalysis.odd_label,
                    OddTotal = oddAnalysis.odd_total,
                    OddValue = oddAnalysis.odd_value,
                    EarningPercent = oddAnalysis.earning_percent_6m,
                    LostCount = oddAnalysis.lost_count_6m.ToString(),
                    WinCount = oddAnalysis.win_count_6m.ToString(),
                    WinningPercent = oddAnalysis.winning_percent_6m,
                };

                hotRateList.Add(hotrate);
            }

            return hotRateList;
        }
        private List<HotRateBusinessModel> GetWinningPercenteSixMonth(string date, long bookmarkerId, long marketId)
        {
            List<odd_analysis> oddAnalysisList = GetOddAnalysisList(date, bookmarkerId, marketId);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            Parallel.ForEach(oddAnalysisList, oddAnalysis =>
            {

                if ((oddAnalysis.win_count_6m + oddAnalysis.lost_count_6m) > 0)
                {
                    HotRateBusinessModel hotrate = new HotRateBusinessModel()
                    {
                        Id = oddAnalysis.id,
                        OddHandicap = oddAnalysis.odd_handicap,
                        OddLabel = oddAnalysis.odd_label,
                        OddTotal = oddAnalysis.odd_total,
                        OddValue = oddAnalysis.odd_value,
                        EarningPercent = oddAnalysis.earning_percent_6m,
                        LostCount = oddAnalysis.lost_count_6m.ToString(),
                        WinCount = oddAnalysis.win_count_6m.ToString(),
                        WinningPercent = oddAnalysis.winning_percent_6m,
                    };

                    hotRateList.Add(hotrate);
                }
            });
            //foreach (var oddAnalysis in oddAnalysisList)
            //{
            //    if ((oddAnalysis.win_count_6m + oddAnalysis.lost_count_6m) > 0)
            //    {
            //        HotRateBusinessModel hotrate = new HotRateBusinessModel()
            //        {
            //            Id = oddAnalysis.id,
            //            OddHandicap = oddAnalysis.odd_handicap,
            //            OddLabel = oddAnalysis.odd_label,
            //            OddTotal = oddAnalysis.odd_total,
            //            OddValue = oddAnalysis.odd_value,
            //            EarningPercent = oddAnalysis.earning_percent_6m,
            //            LostCount = oddAnalysis.lost_count_6m.ToString(),
            //            WinCount = oddAnalysis.win_count_6m.ToString(),
            //            WinningPercent = oddAnalysis.winning_percent_6m,
            //        };

            //        hotRateList.Add(hotrate);
            //    }
            //}

            return hotRateList;
        }

        private List<HotRateBusinessModel> GetWinningPercenteOneYear(string date, long bookmarkerId, long marketId, int winningPercent, int odd_value)
        {
            var oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().
                GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId && p.create_date_time.ToString("yyyy-MM-dd") == date && int.Parse(p.odd_value.Replace(".", "")) >= odd_value &&
                p.winning_percent_1y >= winningPercent && (p.win_count_1y + p.lost_count_1y) > 0);
            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            foreach (var oddAnalysis in oddAnalysisList)
            {
                HotRateBusinessModel hotrate = new HotRateBusinessModel()
                {
                    Id = oddAnalysis.id,
                    OddHandicap = oddAnalysis.odd_handicap,
                    OddLabel = oddAnalysis.odd_label,
                    OddTotal = oddAnalysis.odd_total,
                    OddValue = oddAnalysis.odd_value,
                    EarningPercent = oddAnalysis.earning_percent_1y,
                    LostCount = oddAnalysis.lost_count_1y.ToString(),
                    WinCount = oddAnalysis.win_count_1y.ToString(),
                    WinningPercent = oddAnalysis.winning_percent_1y,
                };

                hotRateList.Add(hotrate);
            }

            return hotRateList;
        }
        private List<HotRateBusinessModel> GetWinningPercenteOneYear(string date, long bookmarkerId, long marketId)
        {
            List<odd_analysis> oddAnalysisList = GetOddAnalysisList(date, bookmarkerId, marketId);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            Parallel.ForEach(oddAnalysisList, oddAnalysis =>
            {
                if ((oddAnalysis.win_count_1y + oddAnalysis.lost_count_1y) > 0)
                {
                    HotRateBusinessModel hotrate = new HotRateBusinessModel()
                    {
                        Id = oddAnalysis.id,
                        OddHandicap = oddAnalysis.odd_handicap,
                        OddLabel = oddAnalysis.odd_label,
                        OddTotal = oddAnalysis.odd_total,
                        OddValue = oddAnalysis.odd_value,
                        EarningPercent = oddAnalysis.earning_percent_1y,
                        LostCount = oddAnalysis.lost_count_1y.ToString(),
                        WinCount = oddAnalysis.win_count_1y.ToString(),
                        WinningPercent = oddAnalysis.winning_percent_1y,
                    };

                    hotRateList.Add(hotrate);
                }
            });
            //foreach (var oddAnalysis in oddAnalysisList)
            //{
            //    if ((oddAnalysis.win_count_1y + oddAnalysis.lost_count_1y) > 0)
            //    {
            //        HotRateBusinessModel hotrate = new HotRateBusinessModel()
            //        {
            //            Id = oddAnalysis.id,
            //            OddHandicap = oddAnalysis.odd_handicap,
            //            OddLabel = oddAnalysis.odd_label,
            //            OddTotal = oddAnalysis.odd_total,
            //            OddValue = oddAnalysis.odd_value,
            //            EarningPercent = oddAnalysis.earning_percent_1y,
            //            LostCount = oddAnalysis.lost_count_1y.ToString(),
            //            WinCount = oddAnalysis.win_count_1y.ToString(),
            //            WinningPercent = oddAnalysis.winning_percent_1y,
            //        };

            //        hotRateList.Add(hotrate);
            //    }
            //}

            return hotRateList;
        }

        private List<HotRateBusinessModel> GetWinningPercenteAll(string date, long bookmarkerId, long marketId, int winningPercent, int odd_value)
        {
            // 
            var oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().
                GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId && p.create_date_time.ToString("yyyy-MM-dd") == date && int.Parse(p.odd_value.Replace(".", "")) >= odd_value &&
                p.winning_percent_all >= winningPercent && (p.win_count_all + p.lost_count_all) > 0);
            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            foreach (var oddAnalysis in oddAnalysisList)
            {
                HotRateBusinessModel hotrate = new HotRateBusinessModel()
                {
                    Id = oddAnalysis.id,
                    OddHandicap = oddAnalysis.odd_handicap,
                    OddLabel = oddAnalysis.odd_label,
                    OddTotal = oddAnalysis.odd_total,
                    OddValue = oddAnalysis.odd_value,
                    EarningPercent = oddAnalysis.earning_percent_all,
                    LostCount = oddAnalysis.lost_count_all.ToString(),
                    WinCount = oddAnalysis.win_count_all.ToString(),
                    WinningPercent = oddAnalysis.winning_percent_all,
                };

                hotRateList.Add(hotrate);
            }

            return hotRateList;
        }
        private List<HotRateBusinessModel> GetWinningPercenteAll(string date, long bookmarkerId, long marketId)
        {
            List<odd_analysis> oddAnalysisList = GetOddAnalysisList(date, bookmarkerId, marketId);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            Parallel.ForEach(oddAnalysisList, oddAnalysis =>
            {
                if ((oddAnalysis.win_count_all + oddAnalysis.lost_count_all) > 0)
                {
                    HotRateBusinessModel hotrate = new HotRateBusinessModel()
                    {
                        Id = oddAnalysis.id,
                        OddHandicap = oddAnalysis.odd_handicap,
                        OddLabel = oddAnalysis.odd_label,
                        OddTotal = oddAnalysis.odd_total,
                        OddValue = oddAnalysis.odd_value,
                        EarningPercent = oddAnalysis.earning_percent_all,
                        LostCount = oddAnalysis.lost_count_all.ToString(),
                        WinCount = oddAnalysis.win_count_all.ToString(),
                        WinningPercent = oddAnalysis.winning_percent_all,
                    };

                    hotRateList.Add(hotrate);
                }
            });
            //foreach (var oddAnalysis in oddAnalysisList)
            //{
            //    if ((oddAnalysis.win_count_all + oddAnalysis.lost_count_all) > 0)
            //    {
            //        HotRateBusinessModel hotrate = new HotRateBusinessModel()
            //        {
            //            Id = oddAnalysis.id,
            //            OddHandicap = oddAnalysis.odd_handicap,
            //            OddLabel = oddAnalysis.odd_label,
            //            OddTotal = oddAnalysis.odd_total,
            //            OddValue = oddAnalysis.odd_value,
            //            EarningPercent = oddAnalysis.earning_percent_all,
            //            LostCount = oddAnalysis.lost_count_all.ToString(),
            //            WinCount = oddAnalysis.win_count_all.ToString(),
            //            WinningPercent = oddAnalysis.winning_percent_all,
            //        };

            //        hotRateList.Add(hotrate);
            //    }
            //}

            return hotRateList;
        }

        private List<HotRateBusinessModel> GetEarningPercenteOneMonth(string date, long bookmarkerId, long marketId, int odd_value)
        {
            var oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().
                GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId && p.create_date_time.ToString("yyyy-MM-dd") == date &&
                int.Parse(p.odd_value.Replace(".", "")) >= odd_value && p.earning_percent_1m >= 0 && (p.win_count_1m + p.lost_count_1m) > 0);
            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            foreach (var oddAnalysis in oddAnalysisList)
            {
                HotRateBusinessModel hotrate = new HotRateBusinessModel()
                {
                    Id = oddAnalysis.id,
                    OddHandicap = oddAnalysis.odd_handicap,
                    OddLabel = oddAnalysis.odd_label,
                    OddTotal = oddAnalysis.odd_total,
                    OddValue = oddAnalysis.odd_value,
                    EarningPercent = oddAnalysis.earning_percent_1m,
                    LostCount = oddAnalysis.lost_count_1m.ToString(),
                    WinCount = oddAnalysis.win_count_1m.ToString(),
                    WinningPercent = oddAnalysis.winning_percent_1m,
                };

                hotRateList.Add(hotrate);
            }

            return hotRateList;
        }
        private List<HotRateBusinessModel> GetEarningPercenteOneMonth(string date, long bookmarkerId, long marketId)
        {
            List<odd_analysis> oddAnalysisList = GetOddAnalysisList(date, bookmarkerId, marketId);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            Parallel.ForEach(oddAnalysisList, oddAnalysis =>
            {
                if ((oddAnalysis.win_count_1m + oddAnalysis.lost_count_1m) > 0)
                {
                    HotRateBusinessModel hotrate = new HotRateBusinessModel()
                    {
                        Id = oddAnalysis.id,
                        OddHandicap = oddAnalysis.odd_handicap,
                        OddLabel = oddAnalysis.odd_label,
                        OddTotal = oddAnalysis.odd_total,
                        OddValue = oddAnalysis.odd_value,
                        EarningPercent = oddAnalysis.earning_percent_1m,
                        LostCount = oddAnalysis.lost_count_1m.ToString(),
                        WinCount = oddAnalysis.win_count_1m.ToString(),
                        WinningPercent = oddAnalysis.winning_percent_1m,
                    };

                    hotRateList.Add(hotrate);
                }
            });
            //foreach (var oddAnalysis in oddAnalysisList)
            //{
            //    if ((oddAnalysis.win_count_1m + oddAnalysis.lost_count_1m) > 0)
            //    {
            //        HotRateBusinessModel hotrate = new HotRateBusinessModel()
            //        {
            //            Id = oddAnalysis.id,
            //            OddHandicap = oddAnalysis.odd_handicap,
            //            OddLabel = oddAnalysis.odd_label,
            //            OddTotal = oddAnalysis.odd_total,
            //            OddValue = oddAnalysis.odd_value,
            //            EarningPercent = oddAnalysis.earning_percent_1m,
            //            LostCount = oddAnalysis.lost_count_1m.ToString(),
            //            WinCount = oddAnalysis.win_count_1m.ToString(),
            //            WinningPercent = oddAnalysis.winning_percent_1m,
            //        };

            //        hotRateList.Add(hotrate);
            //    }
            //}

            return hotRateList;
        }

        private List<HotRateBusinessModel> GetEarningPercenteThreeMonth(string date, long bookmarkerId, long marketId, int odd_value)
        {
            var oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().
                GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId && p.create_date_time.ToString("yyyy-MM-dd") == date &&
                int.Parse(p.odd_value.Replace(".", "")) >= odd_value && p.earning_percent_3m >= 0 && (p.win_count_3m + p.lost_count_3m) > 0);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            foreach (var oddAnalysis in oddAnalysisList)
            {
                HotRateBusinessModel hotrate = new HotRateBusinessModel()
                {
                    Id = oddAnalysis.id,
                    OddHandicap = oddAnalysis.odd_handicap,
                    OddLabel = oddAnalysis.odd_label,
                    OddTotal = oddAnalysis.odd_total,
                    OddValue = oddAnalysis.odd_value,
                    EarningPercent = oddAnalysis.earning_percent_3m,
                    LostCount = oddAnalysis.lost_count_3m.ToString(),
                    WinCount = oddAnalysis.win_count_3m.ToString(),
                    WinningPercent = oddAnalysis.winning_percent_3m,
                };

                hotRateList.Add(hotrate);
            }

            return hotRateList;
        }
        private List<HotRateBusinessModel> GetEarningPercenteThreeMonth(string date, long bookmarkerId, long marketId)
        {
            List<odd_analysis> oddAnalysisList = GetOddAnalysisList(date, bookmarkerId, marketId);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            Parallel.ForEach(oddAnalysisList, oddAnalysis =>
            {
                if ((oddAnalysis.win_count_3m + oddAnalysis.lost_count_3m) > 0)
                {
                    HotRateBusinessModel hotrate = new HotRateBusinessModel()
                    {
                        Id = oddAnalysis.id,
                        OddHandicap = oddAnalysis.odd_handicap,
                        OddLabel = oddAnalysis.odd_label,
                        OddTotal = oddAnalysis.odd_total,
                        OddValue = oddAnalysis.odd_value,
                        EarningPercent = oddAnalysis.earning_percent_3m,
                        LostCount = oddAnalysis.lost_count_3m.ToString(),
                        WinCount = oddAnalysis.win_count_3m.ToString(),
                        WinningPercent = oddAnalysis.winning_percent_3m,
                    };

                    hotRateList.Add(hotrate);
                }
            });
            //foreach (var oddAnalysis in oddAnalysisList)
            //{
            //    if ((oddAnalysis.win_count_3m + oddAnalysis.lost_count_3m) > 0)
            //    {
            //        HotRateBusinessModel hotrate = new HotRateBusinessModel()
            //        {
            //            Id = oddAnalysis.id,
            //            OddHandicap = oddAnalysis.odd_handicap,
            //            OddLabel = oddAnalysis.odd_label,
            //            OddTotal = oddAnalysis.odd_total,
            //            OddValue = oddAnalysis.odd_value,
            //            EarningPercent = oddAnalysis.earning_percent_3m,
            //            LostCount = oddAnalysis.lost_count_3m.ToString(),
            //            WinCount = oddAnalysis.win_count_3m.ToString(),
            //            WinningPercent = oddAnalysis.winning_percent_3m,
            //        };

            //        hotRateList.Add(hotrate);
            //    }
            //}

            return hotRateList;
        }

        private List<HotRateBusinessModel> GetEarningPercenteSixMonth(string date, long bookmarkerId, long marketId, int odd_value)
        {
            var oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().
                GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId && p.create_date_time.ToString("yyyy-MM-dd") == date &&
                int.Parse(p.odd_value.Replace(".", "")) >= odd_value && p.earning_percent_6m >= 0 && (p.win_count_6m + p.lost_count_6m) > 0);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            foreach (var oddAnalysis in oddAnalysisList)
            {
                HotRateBusinessModel hotrate = new HotRateBusinessModel()
                {
                    Id = oddAnalysis.id,
                    OddHandicap = oddAnalysis.odd_handicap,
                    OddLabel = oddAnalysis.odd_label,
                    OddTotal = oddAnalysis.odd_total,
                    OddValue = oddAnalysis.odd_value,
                    EarningPercent = oddAnalysis.earning_percent_6m,
                    LostCount = oddAnalysis.lost_count_6m.ToString(),
                    WinCount = oddAnalysis.win_count_6m.ToString(),
                    WinningPercent = oddAnalysis.winning_percent_6m,
                };

                hotRateList.Add(hotrate);
            }

            return hotRateList;
        }
        private List<HotRateBusinessModel> GetEarningPercenteSixMonth(string date, long bookmarkerId, long marketId)
        {
            List<odd_analysis> oddAnalysisList = GetOddAnalysisList(date, bookmarkerId, marketId);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            Parallel.ForEach(oddAnalysisList, oddAnalysis =>
            {
                if ((oddAnalysis.win_count_6m + oddAnalysis.lost_count_6m) > 0)
                {
                    HotRateBusinessModel hotrate = new HotRateBusinessModel()
                    {
                        Id = oddAnalysis.id,
                        OddHandicap = oddAnalysis.odd_handicap,
                        OddLabel = oddAnalysis.odd_label,
                        OddTotal = oddAnalysis.odd_total,
                        OddValue = oddAnalysis.odd_value,
                        EarningPercent = oddAnalysis.earning_percent_6m,
                        LostCount = oddAnalysis.lost_count_6m.ToString(),
                        WinCount = oddAnalysis.win_count_6m.ToString(),
                        WinningPercent = oddAnalysis.winning_percent_6m,
                    };

                    hotRateList.Add(hotrate);
                }
            });
            //foreach (var oddAnalysis in oddAnalysisList)
            //{
            //    if ((oddAnalysis.win_count_6m + oddAnalysis.lost_count_6m) > 0)
            //    {
            //        HotRateBusinessModel hotrate = new HotRateBusinessModel()
            //        {
            //            Id = oddAnalysis.id,
            //            OddHandicap = oddAnalysis.odd_handicap,
            //            OddLabel = oddAnalysis.odd_label,
            //            OddTotal = oddAnalysis.odd_total,
            //            OddValue = oddAnalysis.odd_value,
            //            EarningPercent = oddAnalysis.earning_percent_6m,
            //            LostCount = oddAnalysis.lost_count_6m.ToString(),
            //            WinCount = oddAnalysis.win_count_6m.ToString(),
            //            WinningPercent = oddAnalysis.winning_percent_6m,
            //        };

            //        hotRateList.Add(hotrate);
            //    }
            //}

            return hotRateList;
        }

        private List<HotRateBusinessModel> GetEarningPercenteOneYear(string date, long bookmarkerId, long marketId, int odd_value)
        {
            var oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().
                GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId && p.create_date_time.ToString("yyyy-MM-dd") == date &&
               int.Parse(p.odd_value.Replace(".", "")) >= odd_value && p.earning_percent_1y >= 0 && (p.win_count_1y + p.lost_count_1y) > 0);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            foreach (var oddAnalysis in oddAnalysisList)
            {
                HotRateBusinessModel hotrate = new HotRateBusinessModel()
                {
                    Id = oddAnalysis.id,
                    OddHandicap = oddAnalysis.odd_handicap,
                    OddLabel = oddAnalysis.odd_label,
                    OddTotal = oddAnalysis.odd_total,
                    OddValue = oddAnalysis.odd_value,
                    EarningPercent = oddAnalysis.earning_percent_1y,
                    LostCount = oddAnalysis.lost_count_1y.ToString(),
                    WinCount = oddAnalysis.win_count_1y.ToString(),
                    WinningPercent = oddAnalysis.winning_percent_1y,
                };

                hotRateList.Add(hotrate);
            }

            return hotRateList;
        }
        private List<HotRateBusinessModel> GetEarningPercenteOneYear(string date, long bookmarkerId, long marketId)
        {
            List<odd_analysis> oddAnalysisList = GetOddAnalysisList(date, bookmarkerId, marketId);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            Parallel.ForEach(oddAnalysisList, oddAnalysis =>
            {
                if ((oddAnalysis.win_count_1y + oddAnalysis.lost_count_1y) > 0)
                {
                    HotRateBusinessModel hotrate = new HotRateBusinessModel()
                    {
                        Id = oddAnalysis.id,
                        OddHandicap = oddAnalysis.odd_handicap,
                        OddLabel = oddAnalysis.odd_label,
                        OddTotal = oddAnalysis.odd_total,
                        OddValue = oddAnalysis.odd_value,
                        EarningPercent = oddAnalysis.earning_percent_1y,
                        LostCount = oddAnalysis.lost_count_1y.ToString(),
                        WinCount = oddAnalysis.win_count_1y.ToString(),
                        WinningPercent = oddAnalysis.winning_percent_1y,
                    };

                    hotRateList.Add(hotrate);
                }
            });
            //foreach (var oddAnalysis in oddAnalysisList)
            //{
            //    if ((oddAnalysis.win_count_1y + oddAnalysis.lost_count_1y) > 0)
            //    {
            //        HotRateBusinessModel hotrate = new HotRateBusinessModel()
            //        {
            //            Id = oddAnalysis.id,
            //            OddHandicap = oddAnalysis.odd_handicap,
            //            OddLabel = oddAnalysis.odd_label,
            //            OddTotal = oddAnalysis.odd_total,
            //            OddValue = oddAnalysis.odd_value,
            //            EarningPercent = oddAnalysis.earning_percent_1y,
            //            LostCount = oddAnalysis.lost_count_1y.ToString(),
            //            WinCount = oddAnalysis.win_count_1y.ToString(),
            //            WinningPercent = oddAnalysis.winning_percent_1y,
            //        };

            //        hotRateList.Add(hotrate);
            //    }
            //}

            return hotRateList;
        }

        private List<HotRateBusinessModel> GetEarningPercenteAll(string date, long bookmarkerId, long marketId, int odd_value)
        {
            var oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId && p.create_date_time.ToString("yyyy-MM-dd") == date &&
            int.Parse(p.odd_value.Replace(".", "")) >= odd_value && p.earning_percent_all >= 0 && (p.win_count_all + p.lost_count_all) > 0);

            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            foreach (var oddAnalysis in oddAnalysisList)
            {
                HotRateBusinessModel hotrate = new HotRateBusinessModel()
                {
                    Id = oddAnalysis.id,
                    OddHandicap = oddAnalysis.odd_handicap,
                    OddLabel = oddAnalysis.odd_label,
                    OddTotal = oddAnalysis.odd_total,
                    OddValue = oddAnalysis.odd_value,
                    EarningPercent = oddAnalysis.earning_percent_all,
                    LostCount = oddAnalysis.lost_count_all.ToString(),
                    WinCount = oddAnalysis.win_count_all.ToString(),
                    WinningPercent = oddAnalysis.winning_percent_all,
                };

                hotRateList.Add(hotrate);
            }

            return hotRateList;
        }
        private List<HotRateBusinessModel> GetEarningPercenteAll(string date, long bookmarkerId, long marketId)
        {
            List<odd_analysis> oddAnalysisList = GetOddAnalysisList(date, bookmarkerId, marketId);
            List<HotRateBusinessModel> hotRateList = new List<HotRateBusinessModel>();
            Parallel.ForEach(oddAnalysisList, oddAnalysis =>
            {
                if ((oddAnalysis.win_count_all + oddAnalysis.lost_count_all) > 0)
                {
                    HotRateBusinessModel hotrate = new HotRateBusinessModel()
                    {
                        Id = oddAnalysis.id,
                        OddHandicap = oddAnalysis.odd_handicap,
                        OddLabel = oddAnalysis.odd_label,
                        OddTotal = oddAnalysis.odd_total,
                        OddValue = oddAnalysis.odd_value,
                        EarningPercent = oddAnalysis.earning_percent_all,
                        LostCount = oddAnalysis.lost_count_all.ToString(),
                        WinCount = oddAnalysis.win_count_all.ToString(),
                        WinningPercent = oddAnalysis.winning_percent_all,
                    };

                    hotRateList.Add(hotrate);
                }
            });
            //foreach (var oddAnalysis in oddAnalysisList)
            //{
            //    if ((oddAnalysis.win_count_all + oddAnalysis.lost_count_all) > 0)
            //    {
            //        HotRateBusinessModel hotrate = new HotRateBusinessModel()
            //        {
            //            Id = oddAnalysis.id,
            //            OddHandicap = oddAnalysis.odd_handicap,
            //            OddLabel = oddAnalysis.odd_label,
            //            OddTotal = oddAnalysis.odd_total,
            //            OddValue = oddAnalysis.odd_value,
            //            EarningPercent = oddAnalysis.earning_percent_all,
            //            LostCount = oddAnalysis.lost_count_all.ToString(),
            //            WinCount = oddAnalysis.win_count_all.ToString(),
            //            WinningPercent = oddAnalysis.winning_percent_all,
            //        };

            //        hotRateList.Add(hotrate);
            //    }
            //}

            return hotRateList;
        }

        private OddWinLostSeriesBusinessModel GetOddWinLostSeries(long oddId)
        {
            OddWinLostSeriesBusinessModel series = new OddWinLostSeriesBusinessModel();
            var odd = _unitOfWork.Repository<odd>().Get(p => p.id == oddId);

            if (odd == null)
            {
                return new OddWinLostSeriesBusinessModel();
            }

            var fixture = _unitOfWork.Repository<fixture>().Get(p => p.id == odd.fixtureId);
            var fixtureList = (from f in _unitOfWork.Repository<fixture>().GetList(p => p.startingAtTimestamp < fixture.startingAtTimestamp)
                               join s in _unitOfWork.Repository<score>().GetList(p=>p.fixtureId == odd.fixtureId) on f.id equals s.fixtureId
                               join o in _unitOfWork.Repository<odd>().GetList(p => p.bookmakerId == odd.bookmakerId && p.marketId == odd.marketId &&
                                    p.handicap == odd.handicap && p.label == odd.label && p.total == odd.total && p.value == odd.value && p.status == 1) on f.id equals o.fixtureId
                               select new
                               {
                                   win = o.winning,
                                   localTeamId = f.localTeamId,
                                   //localTeamName = 
                                   visitorTeamId = f.visitorTeamId,
                                   //visitorTeamName = 
                                   leagueId = f.leagueId,
                                   //LeagueName = 
                                   //CountryName = 
                                   ftScore = s.goals,
                                   htScore = s.goals,
                                   matchDate = f.startingAt,
                                   matchDateTime = f.startingAt,
                                   seasonId = f.seasonId,
                                   fixtureId = f.id,
                                   oddHandicap = o.handicap,
                                   oddTotal = o.total,
                                   bookmarkerId = o.bookmakerId,
                                   marketId = o.marketId,
                                   timestamp = f.startingAtTimestamp,
                                   timeStatus = f.status
                               }).OrderByDescending(p => p.timestamp);


            //var oddList = _unitOfWork.Repository<odd>().GetList(p => p.bookmakerId == odd.bookmaker_id && p.marketId == odd.market_id &&
            //                p.odd_handicap == odd.odd_handicap && p.odd_label == odd.odd_label && p.odd_total == odd.odd_total && p.odd_value == odd.odd_value).OrderByDescending(p => p.create_date_time);

            foreach (var item in fixtureList.Take(10))
            {
                //var itemOdds = _unitOfWork.Repository<odd>().GetList(p => p.fixtureId == item.fixtureId && p.bookmakerId == item.bookmarkerId && p.marketId == item.marketId &&
                // p.odd_handicap == item.oddHandicap && p.odd_total == item.oddTotal);

                series.OddSeries += item.win.ToString();
            }


            //var localTeam = (from f in _unitOfWork.Repository<fixture>().GetList(p => p.localTeamId == fixture.local_team_id && p.time_starting_at_timestamp < fixture.time_starting_at_timestamp && p.time_status != "AU" && p.time_status != "Deleted")
            //                 join o in _unitOfWork.Repository<odd>().GetList(p => p.bookmakerId == odd.bookmaker_id && p.marketId == odd.market_id &&
            //                      p.odd_handicap == odd.odd_handicap && p.odd_label == odd.odd_label && p.odd_total == odd.odd_total && p.odd_value == odd.odd_value) on f.id equals o.fixture_id
            //                 orderby f.time_starting_at_date
            //                 select new
            //                 {
            //                     win = o.odd_winning
            //                 }).Take(10).ToList();

            foreach (var item in fixtureList.Where(p => p.localTeamId == fixture.localTeamId).Take(10))
            {
 //               var itemOdds = _unitOfWork.Repository<odd>().GetList(p => p.fixtureId == item.fixtureId && p.bookmakerId == item.bookmarkerId && p.marketId == item.marketId &&
 //p.odd_handicap == item.oddHandicap && p.odd_total == item.oddTotal);

                series.LocalTeamSeries += item.win.ToString();
            }

            //var visitorTeam = (from f in _unitOfWork.Repository<fixture>().GetList(p => p.visitorTeamId == fixture.visitor_team_id && p.time_starting_at_timestamp < fixture.time_starting_at_timestamp && p.time_status != "AU" && p.time_status != "Deleted")
            //                   join o in _unitOfWork.Repository<odd>().GetList(p => p.bookmakerId == odd.bookmaker_id && p.marketId == odd.market_id &&
            //                        p.odd_handicap == odd.odd_handicap && p.odd_label == odd.odd_label && p.odd_total == odd.odd_total && p.odd_value == odd.odd_value) on f.id equals o.fixture_id
            //                   orderby f.time_starting_at_date
            //                   select new
            //                   {
            //                       win = o.odd_winning
            //                   }).Take(10).ToList();

            //var visitorTeam = oddList.Where(p => p.fixtureId == odd.fixtureId).Take(10);

            //foreach (var item in visitorTeam)
            //{
            //    series.VisitorTeamSeries += item.win.ToString();
            //}

            foreach (var item in fixtureList.Where(p => p.visitorTeamId == fixture.visitorTeamId).Take(10))
            {
 //               var itemOdds = _unitOfWork.Repository<odd>().GetList(p => p.fixtureId == item.fixtureId && p.bookmakerId == item.bookmarkerId && p.marketId == item.marketId &&
 //p.odd_handicap == item.oddHandicap && p.odd_total == item.oddTotal);

                series.VisitorTeamSeries += item.win.ToString();
            }

            //var league = (from f in _unitOfWork.Repository<fixture>().GetList(p => p.leagueId == fixture.league_id && p.seasonId== fixture.season_id && p.time_starting_at_timestamp < fixture.time_starting_at_timestamp && p.time_status != "AU" && p.time_status != "Deleted")
            //              join o in _unitOfWork.Repository<odd>().GetList(p => p.bookmakerId == odd.bookmaker_id && p.marketId == odd.market_id &&
            //                   p.odd_handicap == odd.odd_handicap && p.odd_label == odd.odd_label && p.odd_total == odd.odd_total && p.odd_value == odd.odd_value) on f.id equals o.fixture_id
            //              orderby f.time_starting_at_date
            //              select new
            //              {
            //                  win = o.odd_winning
            //              }).Take(10).ToList();

            foreach (var item in fixtureList.Where(p => p.leagueId == fixture.leagueId && p.seasonId == fixture.seasonId).Take(10))
            {
 //               var itemOdds = _unitOfWork.Repository<odd>().GetList(p => p.fixtureId == item.fixtureId && p.bookmakerId == item.bookmarkerId && p.marketId == item.marketId &&
 //p.odd_handicap == item.oddHandicap && p.odd_total == item.oddTotal);

                series.LeagueSeries += item.win.ToString();
            }

            return series;
        }

        public OddSeriesBusinessModel GetOddSeries(long oddId, string timeZone)
        {
            //double zone;
            //if (!double.TryParse(timeZone, out zone))
            //{
            //    return new OddSeriesBusinessModel();
            //}

            OddSeriesBusinessModel oddSeriesModel = new OddSeriesBusinessModel();

            //var odd = _unitOfWork.Repository<odd>().Get(p => p.id == oddId);
            //var fixture = _unitOfWork.Repository<fixture>().Get(p => p.id == odd.fixtureId);

            //var localTeamSeries = (from f in _unitOfWork.Repository<fixture>().GetList(p => p.localTeamId == fixture.local_team_id && p.time_starting_at_timestamp < fixture.time_starting_at_timestamp && p.time_status != "AU" && p.time_status != "Deleted")
            //                       join o in _unitOfWork.Repository<odd>().GetList(p => p.bookmakerId == odd.bookmaker_id && p.marketId == odd.market_id && p.odd_label == odd.odd_label && p.odd_total == odd.odd_total && p.odd_value == odd.odd_value && p.odd_handicap == odd.odd_handicap)
            //                       on f.id equals o.fixture_id
            //                       select new FixtureForOddAnalysisBusinessModel
            //                       {
            //                           Id = f.id,
            //                           FtScore = f.ft_score,
            //                           HtScore = f.ht_score,
            //                           LocalTeamId = f.local_team_id,
            //                           VisitorTeamId = f.visitor_team_id,
            //                           TimeStartingAtDateTime = f.time_starting_at_date_time,
            //                       }).OrderByDescending(p => p.TimeStartingAtDate).Take(10).ToList();

            //foreach (var item in localTeamSeries)
            //{
            //    item.Odds = _mapper.Map<List<OddBusinessModel>>(_unitOfWork.Repository<odd>().GetList(p => p.fixtureId == item.Id && p.bookmakerId == odd.bookmaker_id && p.marketId == odd.market_id && p.odd_total == odd.odd_total && p.odd_handicap == odd.odd_handicap)).OrderBy(p => p.Id).ToList();
            //    item.LocalTeam = _teamService.GetTeam(item.LocalTeamId);
            //    item.VisitorTeam = _teamService.GetTeam(item.VisitorTeamId);
            //    AddTimeZone(item, zone);
            //}

            //oddSeriesModel.LocalTeamSeries = localTeamSeries;

            //var visitorTeamSeries = (from f in _unitOfWork.Repository<fixture>().GetList(p => p.visitorTeamId == fixture.visitor_team_id && p.time_starting_at_timestamp < fixture.time_starting_at_timestamp && p.time_status != "AU" && p.time_status != "Deleted")
            //                         join o in _unitOfWork.Repository<odd>().GetList(p => p.bookmakerId == odd.bookmaker_id && p.marketId == odd.market_id && p.odd_label == odd.odd_label && p.odd_total == odd.odd_total && p.odd_value == odd.odd_value && p.odd_handicap == odd.odd_handicap)
            //                         on f.id equals o.fixture_id
            //                         select new FixtureForOddAnalysisBusinessModel
            //                         {
            //                             Id = f.id,
            //                             FtScore = f.ft_score,
            //                             HtScore = f.ht_score,
            //                             LocalTeamId = f.local_team_id,
            //                             VisitorTeamId = f.visitor_team_id,
            //                             TimeStartingAtDateTime = f.time_starting_at_date_time,
            //                         }).OrderByDescending(p => p.TimeStartingAtDate).Take(10).ToList();

            //foreach (var item in visitorTeamSeries)
            //{
            //    item.Odds = _mapper.Map<List<OddBusinessModel>>(_unitOfWork.Repository<odd>().GetList(p => p.fixtureId == item.Id && p.bookmakerId == odd.bookmaker_id && p.marketId == odd.market_id && p.odd_total == odd.odd_total && p.odd_handicap == odd.odd_handicap)).OrderBy(p => p.Id).ToList();
            //    item.LocalTeam = _teamService.GetTeam(item.LocalTeamId);
            //    item.VisitorTeam = _teamService.GetTeam(item.VisitorTeamId);
            //    AddTimeZone(item, zone);
            //}

            //oddSeriesModel.VisitorTeamSeries = visitorTeamSeries;
            oddSeriesModel.WinLostSeries = GetOddWinLostSeries(oddId);

            return oddSeriesModel;
        }

        private void AddTimeZone(FixtureForOddAnalysisBusinessModel fixture, double timeZone)
        {
            DateTime addedFixtureDate = DateTime.Parse(fixture.TimeStartingAtDateTime);
            addedFixtureDate = addedFixtureDate.AddMinutes(timeZone);

            fixture.TimeStartingAtDate = addedFixtureDate.ToString("yyyy-MM-dd");
            fixture.TimeStartingAtDateTime = addedFixtureDate.ToString("yyyy-MM-dd HH:mm:ss");
            fixture.TimeStartingAtTime = addedFixtureDate.ToString("HH:mm:ss");
        }

        private List<odd_analysis> GetOddAnalysisList(string date, long bookmarkerId, long marketId)
        {
            List<odd_analysis> oddAnalysisList = new List<odd_analysis>();
            var currentDate = DateTime.UtcNow;
            if (DateTime.TryParse(date, out currentDate))
            {
                var oddAnalysisCacheModel = (List<odd_analysis>)_cacheHelper.Get(string.Format(CacheKeys.HotRates, date, bookmarkerId, marketId));
                if (oddAnalysisCacheModel != null && oddAnalysisCacheModel.Count > 0)
                {
                    oddAnalysisList = oddAnalysisCacheModel;
                }
                else
                {
                    oddAnalysisList = _analysisUnitOfWork.Repository<odd_analysis>().GetList(p => p.bookmakerId == bookmarkerId && p.marketId == marketId).ToList();
                    var oddAnalysisListGroup = oddAnalysisList.GroupBy(p => p.create_date_time).OrderByDescending(x=>x.Key).Last();
                    var oddAnalysisListLast = oddAnalysisList.Where(x=>x.create_date_time == oddAnalysisListGroup.Key).ToList();

                    if (oddAnalysisCacheModel != null && oddAnalysisList.Count > 0)
                    {
                        _cacheHelper.Set(string.Format(CacheKeys.HotRates, oddAnalysisListGroup.Key, bookmarkerId, marketId), oddAnalysisList, 45 * 60 * 60);
                    }
                }
            }

            return oddAnalysisList;
        }
    }
}
