using System.Collections.Generic;

namespace DiscordBot.Data
{
    public class Rating
    {
        public object count { get; set; }
        public int average { get; set; }
    }

    public class Kategory
    {
        public string slug { get; set; }
        public string name { get; set; }
        public string id { get; set; }
    }

    public class Category
    {
        public string tree_id { get; set; }
        public string label_english { get; set; }
        public string label { get; set; }
        public string id { get; set; }
        public string multiple { get; set; }
    }

    public class Publisher
    {
        public string label_english { get; set; }
        public string url { get; set; }
        public string slug { get; set; }
        public string label { get; set; }
        public string id { get; set; }
        public string support_email { get; set; }
        public object support_url { get; set; }
    }

    public class Link
    {
        public string type { get; set; }
        public string id { get; set; }
    }

    public class List
    {
        public string slug { get; set; }
        public string slug_v2 { get; set; }
        public string name { get; set; }
        public object overlay { get; set; }
    }

    public class Flags
    {
    }

    public class Image
    {
        public string link { get; set; }
        public string width { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string height { get; set; }
        public string thumb { get; set; }
    }


    public class Keyimage
    {
        public string small { get; set; }
        public string big { get; set; }
        public object small_legacy { get; set; }
        public object facebook { get; set; }
        public object big_legacy { get; set; }
        public string icon { get; set; }
        public string icon75 { get; set; }
        public string icon25 { get; set; }
    }

    public class Daily
    {
        public string icon { get; set; }
        public Rating rating { get; set; }
        public int remaining { get; set; }
        public Kategory kategory { get; set; }
        public string package_version_id { get; set; }
        public string slug { get; set; }
        public Category category { get; set; }
        public string hotness { get; set; }
        public string id { get; set; }
        public Publisher publisher { get; set; }
        public List<object> list { get; set; }
        public Link link { get; set; }
        public Flags flags { get; set; }
        public Keyimage keyimage { get; set; }
        public string description { get; set; }
        public string title_english { get; set; }
        public string title { get; set; }
    }

    public class Content
    {
        public string pubdate { get; set; }
        public string min_unity_version { get; set; }
        public Rating rating { get; set; }
        public Kategory kategory { get; set; }
        public List<string> unity_versions { get; set; }
        public string url { get; set; }
        public string package_version_id { get; set; }
        public string slug { get; set; }
        public Category category { get; set; }
        public string id { get; set; }
        public Publisher publisher { get; set; }
        public string sizetext { get; set; }
        public List<object> list { get; set; }
        public Link link { get; set; }
        public List<Image> images { get; set; }
        public Flags flags { get; set; }
        public string version { get; set; }
        public string first_published_at { get; set; }
        public Keyimage keyimage { get; set; }
        public int license { get; set; }
        public string description { get; set; }
        public List<object> upgrades { get; set; }
        public string publishnotes { get; set; }
        public string title { get; set; }
        public string short_url { get; set; }
        public List<object> upgradables { get; set; }
    }


    public class DailyObject
    {
        public string banner { get; set; }
        public string feed { get; set; }
        public string status { get; set; }
        public int days_left { get; set; }
        public int total { get; set; }
        public Daily daily { get; set; }
        public int remaining { get; set; }
        public string badge { get; set; }
        public string title { get; set; }
        public bool countdown { get; set; }
        public List<object> results { get; set; }
    }

    public class PackageObject
    {
        public Content content { get; set; }
    }

    public class PriceObject
    {
        public string vat { get; set; }
        public string price_exvat { get; set; }
        public string price { get; set; }
        public bool is_free { get; set; }
    }

    public class Result
    {
        public string category { get; set; }
        public string title { get; set; }
        public string publisher { get; set; }
    }

    public class PackageHeadObject
    {
        public Result result { get; set; }
    }
}