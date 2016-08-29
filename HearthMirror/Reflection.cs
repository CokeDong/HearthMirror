﻿using System;
using System.Collections.Generic;
using System.Linq;
using HearthMirror.Enums;
using HearthMirror.Objects;

namespace HearthMirror
{
	public class Reflection
	{
		private static readonly Lazy<Mirror> LazyMirror = new Lazy<Mirror>(() => new Mirror {ImageName = "Hearthstone"});
		private static Mirror Mirror => LazyMirror.Value;

		private static T TryGetInternal<T>(Func<T> action, bool clearCache = true)
		{
			try
			{
				if(clearCache)
					Mirror.View?.ClearCache();
				return action.Invoke();
			}
			catch
			{
				Mirror.Clean();
				try
				{
					return action.Invoke();
				}
				catch
				{
					return default(T);
				}
			}
		}

		public static List<Card> GetCollection() => TryGetInternal(() => GetCollectionInternal().ToList());

		private static IEnumerable<Card> GetCollectionInternal()
		{
			var values = Mirror.Root["NetCache"]["s_instance"]["m_netCache"]["valueSlots"];
			foreach(var val in values)
			{
				if(val == null || val.Class.Name != "NetCacheCollection") continue;
				var stacks = val["<Stacks>k__BackingField"];
				var items = stacks["_items"];
				int size = stacks["_size"];
				for(var i = 0; i < size; i++)
				{
					var stack = items[i];
					int count = stack["<Count>k__BackingField"];
					var def = stack["<Def>k__BackingField"];
					string name = def["<Name>k__BackingField"];
					int premium = def["<Premium>k__BackingField"];
					yield return new Card(name, count, premium > 0);
				}
			}
		}

		public static List<Deck> GetDecks() => TryGetInternal(() => InternalGetDecks().ToList());

		private static IEnumerable<Deck> InternalGetDecks()
		{
			var values = Mirror.Root["CollectionManager"]["s_instance"]["m_decks"]["valueSlots"];
			foreach(var val in values)
			{
				if(val == null || val.Class.Name != "CollectionDeck")
					continue;
				yield return GetDeck(val);
			}
		}

		public static GameServerInfo GetServerInfo() => TryGetInternal(InternalGetServerInfo);
		private static GameServerInfo InternalGetServerInfo()
		{
			var serverInfo = Mirror.Root["Network"]["s_instance"]["m_lastGameServerInfo"];
			return new GameServerInfo
			{
				Address = serverInfo["<Address>k__BackingField"],
				AuroraPassword = serverInfo["<AuroraPassword>k__BackingField"],
				ClientHandle = serverInfo["<ClientHandle>k__BackingField"],
				GameHandle = serverInfo["<GameHandle>k__BackingField"],
				Mission = serverInfo["<Mission>k__BackingField"],
				Port = serverInfo["<Port>k__BackingField"],
				Resumable = serverInfo["<Resumable>k__BackingField"],
				SpectatorMode = serverInfo["<SpectatorMode>k__BackingField"],
				SpectatorPassword = serverInfo["<SpectatorPassword>k__BackingField"],
				Version = serverInfo["<Version>k__BackingField"],
			};
		}

		public static int GetGameType() => TryGetInternal(InternalGetGameType);
		private static int InternalGetGameType() => (int) Mirror.Root["GameMgr"]["s_instance"]["m_gameType"];

		public static bool IsSpectating() => TryGetInternal(() => (bool)Mirror.Root["GameMgr"]["s_instance"]["m_spectator"]);

		public static long GetSelectedDeckInMenu() => TryGetInternal(() => (long)Mirror.Root["DeckPickerTrayDisplay"]["s_instance"]["m_selectedCustomDeckBox"]["m_deckID"]);

		public static MatchInfo GetMatchInfo() => TryGetInternal(GetMatchInfoInternal);
		private static MatchInfo GetMatchInfoInternal()
		{
			var matchInfo = new MatchInfo();
			var gameState = Mirror.Root["GameState"]["s_instance"];
			var playerIds = gameState["m_playerMap"]["keySlots"];
			var players = gameState["m_playerMap"]["valueSlots"];
			var netCacheValues = Mirror.Root["NetCache"]["s_instance"]["m_netCache"]["valueSlots"];
			for(var i = 0; i < playerIds.Length; i++)
			{
				if(players[i]?.Class.Name != "Player")
					continue;
				var medalInfo = players[i]["m_medalInfo"];
				var sMedalInfo = medalInfo?["m_currMedalInfo"];
				var wMedalInfo = medalInfo?["m_currWildMedalInfo"];
				var name = players[i]["m_name"];
				var sRank = sMedalInfo?["rank"] ?? 0;
				var sLegendRank = sMedalInfo?["legendIndex"] ?? 0;
				var wRank = wMedalInfo?["rank"] ?? 0;
				var wLegendRank = wMedalInfo?["legendIndex"] ?? 0;
				var cardBack = players[i]["m_cardBackId"];
				var id = playerIds[i];
				if((Side)players[i]["m_side"] == Side.FRIENDLY)
				{
					dynamic netCacheMedalInfo = null;
					foreach(var netCache in netCacheValues)
					{
						if(netCache?.Class.Name != "NetCacheMedalInfo")
							continue;
						netCacheMedalInfo = netCache;
						break;
					}
					var sStars = netCacheMedalInfo?["<Standard>k__BackingField"]["<Stars>k__BackingField"];
					var wStars = netCacheMedalInfo?["<Wild>k__BackingField"]["<Stars>k__BackingField"];
					matchInfo.LocalPlayer = new MatchInfo.Player(id, name, sRank, sLegendRank, sStars, wRank, wLegendRank, wStars, cardBack);
				}
				else
					matchInfo.OpposingPlayer = new MatchInfo.Player(id, name, sRank, sLegendRank, 0, wRank, wLegendRank, 0, cardBack);
			}
			matchInfo.BrawlSeasonId = Mirror.Root["TavernBrawlManager"]["s_instance"]?["m_currentMission"]?["seasonId"] ?? 0;
			matchInfo.MissionId = Mirror.Root["GameMgr"]["s_instance"]["m_missionId"];
			foreach(var netCache in netCacheValues)
			{
				if(netCache?.Class.Name != "NetCacheRewardProgress")
					continue;
				matchInfo.RankedSeasonId = netCache["<Season>k__BackingField"];
				break;
			}
			return matchInfo;
		}

		public static ArenaInfo GetArenaDeck() => TryGetInternal(GetArenaDeckInternal);

		private static ArenaInfo GetArenaDeckInternal()
		{
			var draftManager = Mirror.Root["DraftManager"]["s_instance"];
			return new ArenaInfo {
				Wins = draftManager["m_wins"],
				Losses = draftManager["m_losses"],
				Deck = GetDeck(draftManager["m_draftDeck"])
			};
		}

		public static List<Card> GetArenaDraftChoices() => TryGetInternal(() => GetArenaDraftChoicesInternal().ToList());

		private static IEnumerable<Card> GetArenaDraftChoicesInternal()
		{
			var choicesList =  Mirror.Root["DraftDisplay"]["s_instance"]["m_choices"];
			var choices = choicesList["_items"];
			int size = choicesList["_size"];
			for(var i = 0; i < size; i++)
			{
				if(choices[i] != null)
					yield return new Card(choices[i]["m_actor"]["m_entityDef"]["m_cardId"], 1, false);
			}
		}

		private static Deck GetDeck(dynamic deckObj)
		{
			var deck = new Deck
			{
				Id = deckObj["ID"],
				Name = deckObj["m_name"],
				Hero = deckObj["HeroCardID"],
				IsWild = deckObj["m_isWild"],
				Type = deckObj["Type"],
				SeasonId = deckObj["SeasonId"],
				CardBackId = deckObj["CardBackID"],
				HeroPremium = deckObj["HeroPremium"],
			};
			var cardList = deckObj["m_slots"];
			var cards = cardList["_items"];
			int size = cardList["_size"];
			for(var i = 0; i < size; i++)
			{
				var card = cards[i];
				string cardId = card["m_cardId"];
				int count = card["m_count"];
				var existingCard = deck.Cards.FirstOrDefault(x => x.Id == cardId);
				if(existingCard != null)
					existingCard.Count++;
				else
					deck.Cards.Add(new Card(cardId, count, false));
			}
			return deck;
		}

		public static int GetFormat() => TryGetInternal(() => (int)Mirror.Root["GameMgr"]["s_instance"]["m_formatType"]);

		public static Deck GetEditedDeck() => TryGetInternal(GetEditedDeckInternal);
		private static Deck GetEditedDeckInternal()
		{
			var taggedDecks = Mirror.Root["CollectionManager"]["s_instance"]["m_taggedDecks"];
			var tags = taggedDecks["keySlots"];
			var decks = taggedDecks["valueSlots"];
			for (var i = 0; i < tags.Length; i++)
			{
				if(tags[i] == null || decks[i] == null)
					continue;
				if(tags[i]["value__"] == 0)
					return GetDeck(decks[i]);
			}
			return null;
		}

		public static bool IsFriendsListVisible() => TryGetInternal(() => Mirror.Root["ChatMgr"]["s_instance"]["m_friendListFrame"] != null);

		public static int GetCurrentManaFilter() => TryGetInternal(() => (int)Mirror.Root["CollectionManagerDisplay"]["s_instance"]["m_manaTabManager"]["m_currentFilterValue"]);

		public static SetFilterItem GetCurrentSetFilter() => TryGetInternal(GetCurrentSetFilterInternal);

		private static SetFilterItem GetCurrentSetFilterInternal()
		{
			var item = Mirror.Root["CollectionManagerDisplay"]["s_instance"]["m_setFilterTray"]["m_selected"];
			return new SetFilterItem()
			{
				IsAllStandard =  (bool)item["m_isAllStandard"],
				IsWild = (bool)item["m_isWild"]
			};
		}

		public static BattleTag GetBattleTag() => TryGetInternal(GetBattleTagInternal);

		private static BattleTag GetBattleTagInternal()
		{
			var bTag = Mirror.Root["BnetPresenceMgr"]["s_instance"]["m_myPlayer"]["m_account"]["m_battleTag"];
			return new BattleTag
			{
				Name = bTag["m_name"],
				Number = bTag["m_number"]
			};
		}

		public static List<Card> GetPackCards() => TryGetInternal(() => GetPackCardsInternal().ToList());

		private static IEnumerable<Card> GetPackCardsInternal()
		{
			var cards = Mirror.Root["PackOpening"]["s_instance"]["m_director"]?["m_hiddenCards"]?["_items"];
			if(cards == null)
				yield break;
			foreach(var card in cards)
			{
				if(card?.Class.Name != "PackOpeningCard")
					continue;
				var def = card["m_boosterCard"]?["<Def>k__BackingField"];
				if(def == null)
					continue;
				yield return new Card((string)def["<Name>k__BackingField"], 1, (int)def["<Premium>k__BackingField"] > 0);
			}
		}

		public static List<RewardData> GetArenaRewards() => TryGetInternal(() => GetArenaRewardsInternal().ToList());

		private static IEnumerable<RewardData> GetArenaRewardsInternal()
		{
			var rewards = Mirror.Root["DraftManager"]["s_instance"]["m_chest"]?["<Rewards>k__BackingField"]?["_items"];
			if(rewards == null)
				yield break;
			foreach(var reward in rewards)
			{
				switch((string)reward?.Class.Name)
				{
					case "ArcaneDustRewardData":
						yield return new ArcaneDustRewardData((int)reward["<Amount>k__BackingField"]);
						break;
					case "BoosterPackRewardData":
						yield return new BoosterPackRewardData((int)reward["<Id>k__BackingField"], (int)reward["<Count>k__BackingField"]);
						break;
					case "CardRewardData":
						yield return new CardRewardData((string)reward["<CardID>k__BackingField"], (int)reward["<Count>k__BackingField"], (int)reward["<Premium>k__BackingField"] > 0);
						break;
					case "GoldRewardData":
						yield return new GoldRewardData((int)reward["<Amount>k__BackingField"]);
						break;
				}
			}
		}
	}
}