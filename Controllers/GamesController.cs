using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using KSULax.Models;
using System.Web.Routing;
using Elmah;

namespace KSULax.Controllers
{
    public class GamesController : Controller
    {
        KSULaxEntities entities;

        public GamesController() { entities = new KSULaxEntities(); }

        [HandleError]
        public ActionResult Index(int? id)
        {
            if (!id.HasValue)
            {
                return RedirectToAction("Index", "games", new { id = KSU.maxGameSeason});
            }

            if (id >= 2006 && id <= KSU.maxGameSeason)
            {
                ViewData.Model = GamesPhotosList(id.GetValueOrDefault());
                if (ViewData.Model != null)
                { return View(); }
                else { throw new Exception("KSULAX||we can't find the season you requested"); }
            }
            else
            {
                ViewData.Model = GameDetail(id.GetValueOrDefault());
                if (ViewData.Model != null)
                { return View("GameMeta"); }
                else { throw new Exception("KSULAX||we can't find the game you requested"); }
            }
        }

        public News GameDetail(int id)
        {
            List<Game> result = (entities.GameSet
                      .Include("AwayTeam")
                      .Include("HomeTeam")
                      .Where("it.id = " + id)
                      .Take(1)
                      .ToList());

            if (result.Count > 0)
            { return GameResult(result.ElementAt(0)); }
            else
            { return null; }
        }

        public List<Game> GamesList(int id)
        {
            List<Game> result = (entities.GameSet
                      .Include("AwayTeam")
                      .Include("HomeTeam")
                      .Where("it.game_season_id = " + id)
                      .OrderBy("it.game_date")
                      .ToList());
            if (result.Count > 0)
            { return result; }
            else
            { return null; }
        }

        public List<Game> GamesPhotosList(int id)
        {
            List<Game> result = (entities.GameSet
                      .Include("AwayTeam")
                      .Include("HomeTeam")
                      .Include("PhotoGalleries")
                      .Include("PhotoGalleries.Photographer")
                      .Where("it.game_season_id = " + id)
                      .OrderBy("it.game_date")
                      .ToList());
            if (result.Count > 0)
            { return result; }
            else
            { return null; }
        }

        public List<Game> GameSummary(int numGames)
        {
            return (entities.GameSet
                      .Include("AwayTeam")
                      .Include("HomeTeam")
                      .Where("it.detail is not null")
                      .Where("it.game_date <= CAST('" + DateTime.Now + "' as System.DateTime)")
                      .OrderBy("it.game_date desc")
                      .Take(numGames)
                      .ToList());
        }

        public List<Game> GameSummaryYear(DateTime date)
        {
            return (entities.GameSet
                      .Include("AwayTeam")
                      .Include("HomeTeam")
                      .Where("it.detail is not null")
                      .Where("it.game_date BETWEEN CAST('" + date + "' as System.DateTime)" +
                        "AND CAST('" + date.AddYears(1) + "' as System.DateTime)")
                      .Where("it.game_date <= CAST('" + DateTime.Now + "' as System.DateTime)")
                      .OrderBy("it.game_date desc")
                      .ToList());
        }

        public List<Game> GameSummaryYearMonth(DateTime date)
        {
            return (entities.GameSet
                      .Include("AwayTeam")
                      .Include("HomeTeam")
                      .Where("it.detail is not null")
                      .Where("it.game_date BETWEEN CAST('" + date + "' as System.DateTime)" +
                        "AND CAST('" + date.AddMonths(1) + "' as System.DateTime)")
                      .Where("it.game_date <= CAST('" + DateTime.Now + "' as System.DateTime)")
                      .OrderBy("it.game_date desc")
                      .ToList());
        }

        public List<Game> GameSummaryYearMonthDay(DateTime date)
        {
            return (entities.GameSet
                      .Include("AwayTeam")
                      .Include("HomeTeam")
                      .Where("it.detail is not null")
                      .Where("it.game_date BETWEEN CAST('" + date + "' as System.DateTime)" +
                        "AND CAST('" + date.AddDays(1) + "' as System.DateTime)")
                      .Where("it.game_date <= CAST('" + DateTime.Now + "' as System.DateTime)")
                      .OrderBy("it.game_date desc")
                      .ToList());
        }

        public List<News> GameListNewsList(List<Game> gameslist)
        {
            List<News> newslist = new List<News>();
            foreach (Game game in gameslist)
            {
                newslist.Add(GameResult(game));
            }
            return newslist;
        }

        private News GameResult(Game game)
        {
            HttpContextWrapper httpContextWrapper = new HttpContextWrapper(System.Web.HttpContext.Current);
            UrlHelper urlHelper = new UrlHelper(new RequestContext(httpContextWrapper, RouteTable.Routes.GetRouteData(httpContextWrapper)));
            News summary = new News();
            Team ksu = new Team();
            Team opp = new Team();
            bool home = true;

            if (game.HomeTeam.slug.Equals("kennesaw_state"))
            {
                ksu = game.HomeTeam;
                opp = game.AwayTeam;
                home = true;
            }
            else
            {
                ksu = game.AwayTeam;
                opp = game.HomeTeam;
                home = false;
            }

            summary.date = game.game_date.GetValueOrDefault();
            summary.story = (game.detail == null) ? "" : game.detail;
            summary.url_title = urlHelper.Action("Index", "games", new { id = game.game_season_id.Value }) + "#" + game.id;
            summary.title = ksu.abr + " "
                + gameResult(game.home_team_score, game.away_team_score, home) + " "
                + opp.abr + " "
                + (home ? game.home_team_score : game.away_team_score) + " - "
                + (home ? game.away_team_score : game.home_team_score) + " "
                + (home ? "at home" : "on the road");

            return summary;
        }

        private string gameResult(int? home_team_score, int? away_team_score, bool home)
        {
            if (!home_team_score.HasValue || !away_team_score.HasValue)
            { return "-"; }
            else if (home && (home_team_score > away_team_score))
            { return "beats"; }
            else if (!home && (away_team_score > home_team_score))
            { return "beats"; }
            else if (!home)
            { return "loses to"; }
            else if (home)
            { return "loses to"; }
            else { return string.Empty; }
        }
    }
}