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
}
