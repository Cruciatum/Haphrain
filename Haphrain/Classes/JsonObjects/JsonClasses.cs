using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Haphrain.Classes.JsonObjects
{
    #region Urban Dictionary Types
    internal class UrbDicJson
    {
        public string definition { get; set; }
        public string permalink { get; set; }
        public int thumbs_up { get; set; }
        public object[] sound_urls { get; set; }
        public string author { get; set; }
        public string word { get; set; }
        public int defid { get; set; }
        public string current_vote { get; set; }
        public string written_on { get; set; }
        public string example { get; set; }
        public int thumbs_down { get; set; }
    }

    internal class UrbDicJsonObject
    {
        public UrbDicJson[] list { get; set; }
    }
    #endregion

    #region Oxford Definition Types
    internal class OxfordEntry
    {
        public string Id { get; set; }
        public Metadata Metadata { get; set; }
        public Result[] Results { get; set; }
        public string Word { get; set; }
    }

    internal class Metadata
    {
        public string Operation { get; set; }
        public string Provider { get; set; }
        public string Schema { get; set; }
    }

    internal class Result
    {
        public string Id { get; set; }
        public string Language { get; set; }
        public LexicalEntry[] LexicalEntries { get; set; }
        public string Type { get; set; }
        public string Word { get; set; }
    }

    internal class LexicalEntry
    {
        public Entry[] Entries { get; set; }
        public string Language { get; set; }
        public LexicalCategory LexicalCategory { get; set; }
        public string Text { get; set; }
    }

    internal class Entry
    {
        public long HomographNumber { get; set; }
        public Sense[] Senses { get; set; }
    }

    internal class Sense
    {
        public string[] Definitions { get; set; }
        public string Id { get; set; }
        public Subsense[] Subsenses { get; set; }
    }

    internal class Subsense
    {
        public string[] Definitions { get; set; }
        public string Id { get; set; }
    }

    internal class LexicalCategory
    {
        public string Id { get; set; }
        public string Text { get; set; }
    }
    #endregion

    #region Oxford Lemma Types
    internal class OxfordLemma
    {
        public MetadataLemma Metadata { get; set; }
        public ResultLemma[] Results { get; set; }
    }

    internal class MetadataLemma
    {
        public string Provider { get; set; }
    }

    internal class ResultLemma
    {
        public string Id { get; set; }
        public string Language { get; set; }
        public LexicalEntryLemma[] LexicalEntries { get; set; }
        public string Word { get; set; }
    }

    internal class LexicalEntryLemma
    {
        public LexicalCategoryLemma[] InflectionOf { get; set; }
        public string Language { get; set; }
        public LexicalCategoryLemma LexicalCategory { get; set; }
        public string Text { get; set; }
    }

    internal class LexicalCategoryLemma
    {
        public string Id { get; set; }
        public string Text { get; set; }
    }
    #endregion

    #region Tenor GifSearch Types

    internal class TenorResult
    {
        public GIF_OBJECT[] results { get; set; }
        public string next { get; set; }
    }

    internal class GIF_OBJECT
    {
        public float created { get; set; }
        public bool hasaudio { get; set; }
        public string id { get; set; }
        public GIF_LIST[] media { get; set; }
        public string[] tags { get; set; }
        public int shares { get; set; }
        public string title { get; set; }
        public string itemurl { get; set; }
        public bool hascaption { get; set; }
        public string url { get; set; }
        public object composite { get; set; }
    }

    internal class GIF_LIST
    {
        public MEDIA_OBJECT tinygif { get; set; }
        public MEDIA_OBJECT gif { get; set; }
        public MEDIA_OBJECT mp4 { get; set; }
    }

    internal class MEDIA_OBJECT
    {
        public string preview { get; set; }
        public string url { get; set; }
        public int[] dims { get; set; }
        public float? duration { get; set; }
        public int size { get; set; }
    }
    #endregion

    #region GfyCat Types
    internal class GfyCatAuthPost
    {
        public string grant_type { get; set; }
        public string client_id { get; set; }
        public string client_secret { get; set; }
    }
    internal class GfyCatAuthResult
    {
        public string token_type { get; set; }
        public string scope { get; set; }
        public int expires_in { get; set; }
        public string access_token { get; set; }
    }

    internal class GfyCatResult
    {
        public GfyItem GfyItem { get; set; }
    }

    internal class GfyItem
    {
        public string GfyId { get; set; }
        public string GfyName { get; set; }
        public string GfyNumber { get; set; }
        public string WebmUrl { get; set; }
        public string GifUrl { get; set; }
        public string MobileUrl { get; set; }
        public string MobilePosterUrl { get; set; }
        public string MiniUrl { get; set; }
        public string MiniPosterUrl { get; set; }
        public string PosterUrl { get; set; }
        public string Thumb100PosterUrl { get; set; }
        public string Max5MbGif { get; set; }
        public string Max2MbGif { get; set; }
        public string Max1MbGif { get; set; }
        public string Gif100Px { get; set; }
        public long Width { get; set; }
        public long Height { get; set; }
        public string AvgColor { get; set; }
        public long FrameRate { get; set; }
        public long NumFrames { get; set; }
        public long Mp4Size { get; set; }
        public long WebmSize { get; set; }
        public long GifSize { get; set; }
        public long Source { get; set; }
        public long CreateDate { get; set; }
        public long Nsfw { get; set; }
        public string Mp4Url { get; set; }
        public long Likes { get; set; }
        public long Published { get; set; }
        public long Dislikes { get; set; }
        public string ExtraLemmas { get; set; }
        public string Md5 { get; set; }
        public long Views { get; set; }
        public string[] Tags { get; set; }
        public string UserName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string LanguageText { get; set; }
        public object LanguageCategories { get; set; }
        public string Subreddit { get; set; }
        public string RedditId { get; set; }
        public string RedditIdText { get; set; }
        public object[] DomainWhitelist { get; set; }
    }
    #endregion
}