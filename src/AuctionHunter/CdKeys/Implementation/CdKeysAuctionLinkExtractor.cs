﻿using AuctionHunter.Infrastructure;
using Newtonsoft.Json.Linq;

namespace AuctionHunter.G2A.Implementation
{
	public class G2AAuctionLinkExtractor : IAuctionLinkExtractor
	{
		public string Extract(string item)
		{
			var token = JObject.Parse(item);
			var slug = token.SelectToken("$.slug").ToString();
			return $"https://www.g2a.com/pl-pl/{slug}";
		}
	}
}
