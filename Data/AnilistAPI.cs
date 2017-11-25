// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

using System.Collections.Generic;

namespace ConsoleApplication
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
        public int? startDate { get; set; }
        public int? endDate { get; set; }
        public int? episodes { get; set; }
        public int? duration { get; set; }
        public bool isAdult { get; set; }
        public int? averageScore { get; set; }
        public string genre { get; set; }
        public MediaTitle title { get; set; }
        
    }

    public class MediaTitle
    {
        public string romaji { get; set; }
        public string english { get; set; }
        public string native { get; set; }
    }
}