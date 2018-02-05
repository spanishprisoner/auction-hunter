﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AuctionHunter.Infrastructure.Implementation
{
	public class AuctionHunterCore : IAuctionHunterCore
	{
		public string Name { get; set; }
		public int NumberOfPages { get; set; }
		public int NumberOfDays { get; set; }
		public IUrlProvider UrlProvider { get; set; }
		public IWebClient WebClient { get; set; }
		public IItemsExtractor ItemsExtractor { get; set; }
		public IAuctionLinkExtractor AuctionLinkExtractor { get; set; }
		public IContentExtractor ContentExtractor { get; set; }
		public IList<string> SkipPatterns { get; set; }

		private bool _initialRun;

		public async Task Run()
		{
			var savedAuctionItems = Load($"{Name}.cache").ToList();
			if (savedAuctionItems.Count == 0)
				_initialRun = true;

			var allAuctionItems = new List<AuctionItem>();
			for (var i = 1; i <= NumberOfPages; i++)
			{
				Console.WriteLine($"Doing page number: {i}");
				var url = UrlProvider.GetNextUrl();
				var page = await WebClient.Get(url);
				var items = ItemsExtractor.GetItems(page);
				allAuctionItems.AddRange(ConvertItems(i, items.ToList()));
			}
			UpdateLists(allAuctionItems, savedAuctionItems, out var resultAuctionItems);

			Save($"{Name}.cache", savedAuctionItems);
			Save($"{Name}_Results.txt", resultAuctionItems);
		}

		private IEnumerable<AuctionItem> ConvertItems(int pageNumber, IEnumerable<string> items)
		{
			var convertedItems = new List<AuctionItem>();
			foreach (var item in items)
			{
				var auctionLink = AuctionLinkExtractor.Extract(item);
				var content = ContentExtractor.Extract(item);
				if (SkipPatterns.Any(e => content.ToString().Contains(e)))
					continue;
				convertedItems.Add(new AuctionItem
				{
					AuctionLink = auctionLink,
					OnPage = pageNumber,
					Content = content,
					Timestamp = _initialRun ? DateTime.MinValue : DateTime.Now,
				});
			}

			return convertedItems;
		}

		private void UpdateLists(IEnumerable<AuctionItem> allAuctionItems, ICollection<AuctionItem> savedAuctionItems, out List<AuctionItem> resultAuctionItems)
		{
			resultAuctionItems = new List<AuctionItem>();
			foreach (var item in allAuctionItems)
			{
				var oldItem = savedAuctionItems.FirstOrDefault(e => e.AuctionLink == item.AuctionLink);
				if (oldItem == null)
				{
					resultAuctionItems.Add(item);
					savedAuctionItems.Add(item);
				}
				else if (DateTime.Compare(item.Timestamp, oldItem.Timestamp + TimeSpan.FromDays(NumberOfDays)) < 0)
				{
					resultAuctionItems.Add(item);
				}
			}
		}

		private static IEnumerable<AuctionItem> Load(string name)
		{
			if (File.Exists(name) == false)
				return new List<AuctionItem>();

			using (var r = new StreamReader(name))
			{
				var json = r.ReadToEnd();
				return JsonConvert.DeserializeObject<List<AuctionItem>>(json);
			}
		}

		private static void Save(string name, List<AuctionItem> items, Formatting formatting = Formatting.Indented)
		{
			using (var w = File.CreateText(name))
			{
				w.Write(JsonConvert.SerializeObject(items, formatting));
			}
		}
	}
}
