using PreOddsApi.WebUI.InfraStructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebMarkupMin.AspNetCore7;
using Microsoft.AspNetCore.Localization;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using System;

namespace PreOddsApi.WebUI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddDistributedMemoryCache();
            services.AddRazorPages();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Oturum zaman aşımı süresi
                options.Cookie.HttpOnly = true; // Çerezleri yalnızca HTTP üzerinden kullanılabilir yapma
                options.Cookie.IsEssential = true; // Çerezlerin vazgeçilmez olduğunu belirtme
            });

            //var serviceProvider = services.BuildServiceProvider();

            services.AddLocalization(o =>
            {
                o.ResourcesPath = "Resources";
            });
            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]{
                new CultureInfo("en-US"),
                new CultureInfo("tr-TR"),
                };

                options.DefaultRequestCulture = new RequestCulture("en-US", "en-US");

                // You must explicitly state which cultures your application supports.
                // These are the cultures the app supports for formatting 
                // numbers, dates, etc.

                options.SupportedCultures = supportedCultures;

                // These are the cultures the app supports for UI strings, 
                // i.e. we have localized resources for.

                options.SupportedUICultures = supportedCultures;
                //options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(context =>
                //{
                //    var userLangs = context.Request.Headers["Accept-Language"].ToString();
                //    var firstLang = userLangs.Split(',').FirstOrDefault() == "tr" ? "tr-TR" : (userLangs.Split(',').FirstOrDefault() != "tr-TR") ? "en-US" : "tr-TR";
                //    var defaultLang = string.IsNullOrEmpty(firstLang) ? "en" : firstLang;
                //    return Task.FromResult(new ProviderCultureResult(defaultLang, defaultLang));
                //}));
            });


            services.AddSession();

            services.AddWebMarkupMin(
                   options =>
                   {
                       options.AllowMinificationInDevelopmentEnvironment = true;
                       options.AllowCompressionInDevelopmentEnvironment = true;

                   })
               .AddHtmlMinification(
                   options =>
                   {
                       options.MinificationSettings.RemoveRedundantAttributes = true;
                       options.MinificationSettings.RemoveHttpProtocolFromAttributes = true;
                       options.MinificationSettings.RemoveHttpsProtocolFromAttributes = true;
                   })
               .AddHttpCompression();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            IList<CultureInfo> supportedCulture = new List<CultureInfo>
            {
                new CultureInfo("en-US"),
                new CultureInfo("tr-TR")
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(culture: "en", uiCulture: "en"),
                SupportedCultures = supportedCulture,
                SupportedUICultures = supportedCulture
            });

            Bundler.HostingEnvironment = env;
            app.UseWebMarkupMin();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseBrowserLink();
            }
            else
            {
                //app.UseMiddleware<SrcExceptionMiddleware>();
                //app.UseWebMarkupMin();
                //app.UseResponseCompression();                
            }

            app.UseRouting();

            app.UseSession();
            app.UseAuthentication();
            app.UseStaticFiles();
            app.UseSession();


            ConfigureRouting(app);
        }

        private static void ConfigureRouting(IApplicationBuilder app)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                  name: "HotRates",
                  pattern: "hot-rate-analysis",
                  defaults: new { controller = "Analysis", action = "Hotrates" });

                endpoints.MapControllerRoute(
                    name: "WinningRates",
                    pattern: "winning-analysis",
                    defaults: new { controller = "Analysis", action = "WinningRates" });

                endpoints.MapControllerRoute(
                    name: "EarningRates",
                    pattern: "earning-analysis",
                    defaults: new { controller = "Analysis", action = "EarningRates" });

                endpoints.MapControllerRoute(
                   name: "LeagueDetail",
                   pattern: "leagueDetail/{leagueId:long}",
                   defaults: new { controller = "League", action = "LeagueDetail" });

                endpoints.MapControllerRoute(
                     name: "Tips",
                    pattern: "tips",
                    defaults: new { controller = "Tips", action = "Tips" });

                endpoints.MapControllerRoute(
                 name: "FixtureDetail",
                  pattern: "fixtureDetail/{fixtureId:long}",
                  defaults: new { controller = "Fixture", action = "FixtureDetail" });


                endpoints.MapControllerRoute(
                name: "PrivacyTerm",
                pattern: "privacy-term",
                defaults: new { controller = "Home", action = "PrivacyTerm" }
            );

                endpoints.MapControllerRoute(
              name: "Help",
              pattern: "help",
              defaults: new { controller = "Home", action = "Help" }
          );


                endpoints.MapControllerRoute(
              name: "AboutUs",
              pattern: "about-us",
              defaults: new { controller = "Home", action = "AboutUs" }
          );


                endpoints.MapControllerRoute(
              name: "Leagues",
              pattern: "leagues",
              defaults: new { controller = "League", action = "Leagues" }
          );

                endpoints.MapControllerRoute(
                    name: "dailyCoupons",
                    pattern: "gunluk-kuponlar",
                    defaults: new { controller = "Coupon", action = "DailyCoupons" }
                );


                endpoints.MapControllerRoute(
                   name: "feed",
                   pattern: "kullanici/anasayfa",
                   defaults: new { controller = "Feed", action = "Index" }
               );

                endpoints.MapControllerRoute(
                   name: "userAccountDetaıls",
                   pattern: "kullanici/hesap-detaylari",
                   defaults: new { controller = "User", action = "AccountDetails" }
               );

                endpoints.MapControllerRoute(
                   name: "profile",
                   pattern: "profil/{guid}/{display-name}",
                   defaults: new { controller = "Profile", action = "Index" }
               );


                endpoints.MapControllerRoute(
                   name: "sharedCoupon",
                   pattern: "kupon/{guid}",
                   defaults: new { controller = "Coupon", action = "SharedCoupon" }
               );

                endpoints.MapControllerRoute(
                   name: "userCoupon",
                   pattern: "kullanici/kupon",
                   defaults: new { controller = "Coupon", action = "Index" }
               );

                endpoints.MapControllerRoute(
                   name: "EPostaOnaylama",
                   pattern: "e-posta-onayla/{guid}",
                   defaults: new { controller = "User", action = "ApproveEMail" }
               );

                endpoints.MapControllerRoute(
                   name: "AktivasyonEPostaGonder",
                   pattern: "aktivasyon-e-postasi-gonder/{guid}",
                   defaults: new { controller = "User", action = "SendConfirmationEMail" }
               );

                endpoints.MapControllerRoute(
                 name: "hesapBilgileri",
                 pattern: "kullanici/hesap-bilgileri",
                 defaults: new { controller = "User", action = "Account" }
             );


                endpoints.MapControllerRoute(
                 name: "sifreBilgileri",
                 pattern: "kullanici/sifre-guncelle",
                 defaults: new { controller = "User", action = "UpdatePassword" }
             );

                endpoints.MapControllerRoute(
               name: "sifremiUnuttum",
               pattern: "sifremi-unuttum",
               defaults: new { controller = "User", action = "ForgotPassword" }
           );

                endpoints.MapControllerRoute(
              name: "sifreSifirlama",
              pattern: "sifre-sifirla/{guid}",
              defaults: new { controller = "User", action = "ResetPassword" }
          );
                endpoints.MapControllerRoute(
           name: "kazananlarListesi",
           pattern: "en-cok-kazanan-kullanicilar",
           defaults: new { controller = "User", action = "MostEarningUsers" }
       );

                endpoints.MapControllerRoute(
          name: "populerListesi",
          pattern: "en-populer-kullanicilar",
          defaults: new { controller = "User", action = "MostPopularUsers" }
      );

                endpoints.MapControllerRoute(
                  name: "iletisim",
                  pattern: "iletisim",
                  defaults: new { controller = "Contact", action = "Index" }
              );

                endpoints.MapControllerRoute(
                 name: "paraKazanmaBilgi",
                 pattern: "sss/nasil-dsc-kazanirim",
                 defaults: new { controller = "Faq", action = "EarnDsc" }
             );

                endpoints.MapControllerRoute(
                   name: "hata",
                   pattern: "hata",
                   defaults: new { controller = "Home", action = "Error" }
               );

                endpoints.MapControllerRoute(
                   name: "dsoAnalizi",
                   pattern: "dso-analizi",
                   defaults: new { controller = "Analysis", action = "DsoMatches" }
               );

                endpoints.MapControllerRoute(
                   name: "vbetAnalizi",
                   pattern: "valuebet-analizi",
                   defaults: new { controller = "Analysis", action = "ValuebetMatches" }
               );

                endpoints.MapControllerRoute(
                   name: "rateAnalizi",
                   pattern: "oran-analizi",
                   defaults: new { controller = "Analysis", action = "RateAnalysisMatches" }
               );

                endpoints.MapControllerRoute(
                   name: "sicakOran",
                   pattern: "sicak-oran-analizi",
                   defaults: new { controller = "Analysis", action = "HotRateMatches" }
               );

                // endpoints.MapControllerRoute(
                //    name: "hotRates",
                //    pattern: "hot-rate-analysis",
                //    defaults: new { controller = "Analysis", action = "HotRateMatches" }
                //);

                endpoints.MapControllerRoute(
                   name: "gunlukprogram",
                   pattern: "gunluk-program/{date?}",
                   defaults: new { controller = "DailyProgram", action = "Index" }
               );
                endpoints.MapControllerRoute(
                   name: "canliBulten",
                   pattern: "canli-bulten/{date?}",
                   defaults: new { controller = "DailyProgram", action = "Index" }
               );

                endpoints.MapControllerRoute(
                   name: "macDetay",
                   pattern: "mac/{Id:int}/{url}",
                   defaults: new { controller = "MatchInfo", action = "Detail" }
               );

                // endpoints.MapControllerRoute(
                //    name: "valuebetOranAnalizi",
                //    pattern: "valuebet-oran-analizi",
                //    defaults: new { controller = "Analysis", action = "ValuebetRates" }
                //);

                endpoints.MapControllerRoute(
                   name: "blogListesi",
                   pattern: "blog-listesi",
                   defaults: new { controller = "Blog", action = "List" }
               );

                endpoints.MapControllerRoute(
                   name: "blog",
                   pattern: "blog/{id:int}/{url}",
                   defaults: new { controller = "Blog", action = "Detail" }
               );

                //ingilizceler
                endpoints.MapControllerRoute(
                   name: "dailyprogram",
                   pattern: "daily-program/{date?}",
                   defaults: new { controller = "DailyProgram", action = "Index" }
               );
                endpoints.MapControllerRoute(
                   name: "dsoAnalysis",
                   pattern: "dso-analysis",
                   defaults: new { controller = "Analysis", action = "DsoMatches" }
               );

                endpoints.MapControllerRoute(
                   name: "valuebetAnalysis",
                   pattern: "valuebet-analysis",
                   defaults: new { controller = "Analysis", action = "ValuebetMatches" }
               );



                endpoints.MapControllerRoute(
                   name: "valuebetRateAnalysis",
                   pattern: "valuebet-rate-analysis",
                   defaults: new { controller = "Analysis", action = "ValuebetRates" }
               );

                endpoints.MapControllerRoute(
                   name: "error",
                   pattern: "error",
                   defaults: new { controller = "Home", action = "Error" }
               );


                ////eski url ler
                endpoints.MapControllerRoute(
                   name: "oldBlogDso",
                   pattern: "News/DsoMatches",
                   defaults: new { controller = "AllBlogs", action = "Read", id = 1 }
               );

                endpoints.MapControllerRoute(
                   name: "oldBlogIko",
                   pattern: "News/IKO",
                   defaults: new { controller = "AllBlogs", action = "Read", id = 2 }
               );

                endpoints.MapControllerRoute(
                   name: "oldBlogVbet",
                   pattern: "Analysis/MatchesForDSO",
                   defaults: new { controller = "Analysis", action = "DsoMatches", id = 3 }
               );

                endpoints.MapControllerRoute(
                   name: "oldDso",
                   pattern: "News/VBET",
                   defaults: new { controller = "AllBlogs", action = "Read", id = 3 }
               );

                endpoints.MapControllerRoute(
                   name: "oldMatchesForValueBet",
                   pattern: "MatchesForValueBet",
                   defaults: new { controller = "Analysis", action = "Detail" }
               );

                endpoints.MapControllerRoute(
                   name: "oldValuebet",
                   pattern: "GetValueBets",
                   defaults: new { controller = "Analysis", action = "Read", id = 3 }
               );

                endpoints.MapControllerRoute(
                   name: "videoGaleri",
                   pattern: "sss/video-galeri",
                   defaults: new { controller = "Faq", action = "VideoGalery" }
               );

                endpoints.MapRazorPages();
            });
        }
    }
}
