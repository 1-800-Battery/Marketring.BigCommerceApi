﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Fusionary.BigCommerce.Types.BcCartLineItems;

namespace Fusionary.BigCommerce.Types
{
    public record BcCartPost : IBcExtensionData
    {
        [JsonPropertyName("customer_id")]
        public long? CustomerId { get; set; }

        [JsonPropertyName("line_items")]
        public List<BcCartLineItem>? LineItems { get; set; }

        [JsonPropertyName("channel_id")]
        public long? ChannelId { get; set; }

        [JsonPropertyName("currency")]
        public Currency? Currency { get; set; }

        [JsonPropertyName("locale")]
        public string? Locale { get; set; }
   
   


    public IDictionary<string, JsonElement>? ExtensionData { get; init; }

    }

    public partial class Currency
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }
}
