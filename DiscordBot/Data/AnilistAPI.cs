// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

using System.Collections.Generic;

namespace DiscordBot.Data
{
    public class AnilistResponse
    {
        public Data data { get; set; }
    }

    public class Data
    {
        public Page Page { get; set; }
    }
    
    public class Page
    {
        public List<AiringSchedules> airingSchedules { get; set; }
        public List<Media> media { get; set; }
    }
    
    public class AiringSchedules
    {
        public int? id { get; set; }
        public int? timeUntilAiring { get; set; }
        public int? episode { get; set; }
        public Media media { get; set; }
    }
    
    public class Media
    {
        public int? id { get; set; }
        public int? idMal { get; set; }
        public Date startDate { get; set; }
        public Date endDate { get; set; }
        public int? episodes { get; set; }
        public int? duration { get; set; }
        public bool isAdult { get; set; }
        public int? averageScore { get; set; }
        public List<string> genres { get; set; }
        public MediaTitle title { get; set; }
        public string description { get; set; }
        public MediaCoverImage coverImage { get; set; }
    }

    public class Date
    {
        public int? year { get; set; }
        public int? month { get; set; }
        public int? day { get; set; }
    }

    public class MediaCoverImage
    {
        public string large { get; set; }
        public string medium { get; set; }
    }
    
    public class MediaTitle
    {
        public string romaji { get; set; }
        public string english { get; set; }
        public string native { get; set; }
    }
}