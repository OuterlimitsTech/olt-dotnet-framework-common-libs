﻿using Newtonsoft.Json;

namespace OLT.Core
{
    public class OltPagingWithSortParams : OltPagingParams, IOltPagingWithSortParams
    {
        [JsonProperty("sort")]
        public virtual string PropertyName { get; set; }
        [JsonProperty("asc")]
        public virtual bool IsAscending { get; set; } = true;
    }
}