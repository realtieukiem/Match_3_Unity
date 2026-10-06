using System;

namespace Pokiwar.Domain
{
    public sealed class UpgradeResult
    {
        public bool Attempted;
        public bool Success;
        public string Message;
        public float Chance;
    }

    /// <summary>StoneMerge and PetEnhancement are separate systems.</summary>
    public sealed class UpgradeService
    {
        private readonly ContentDatabase db;

        public UpgradeService(ContentDatabase db)
        {
            this.db = db;
        }

        private UpgradeConfig Cfg => db.Upgrades;

        public float MergeChance(int tier, bool lucky)
        {
            var t = Cfg.MergeChanceByTier;
            float c = t[MathUtil.Clamp(tier - 1, 0, t.Length - 1)];
            if (lucky) c += Cfg.LuckyCharmBonus;
            return (float)MathUtil.Clamp(c, 0, 1);
        }

        public int MergeCost(int tier)
        {
            var t = Cfg.MergeGoldByTier;
            return t[MathUtil.Clamp(tier - 1, 0, t.Length - 1)];
        }

        /// <summary>Three stones of one tier try for one stone of the next tier. Failure still consumes all three.</summary>
        public UpgradeResult MergeStones(SaveData d, Element element, int tier, bool useLucky, SeededRng rng)
        {
            var r = new UpgradeResult();
            if (tier >= Cfg.MaxStoneTier) { r.Message = "Max tier"; return r; }
            if (d.StoneCount(element, tier) < 3) { r.Message = "Need 3 stones"; return r; }
            int cost = MergeCost(tier);
            if (d.Gold < cost) { r.Message = "Need " + cost + " gold"; return r; }
            if (useLucky && d.LuckyCharms <= 0) { r.Message = "No Lucky Charm"; return r; }
            r.Attempted = true;
            d.Gold -= cost;
            if (useLucky) d.LuckyCharms--;
            d.AddStones(element, tier, -3);
            r.Chance = MergeChance(tier, useLucky);
            r.Success = rng.Chance(r.Chance);
            if (r.Success)
            {
                d.AddStones(element, tier + 1, 1);
                r.Message = "Success! " + element + " Stone T" + (tier + 1);
            }
            else r.Message = "Merge failed. 3 stones lost.";
            return r;
        }

        public float EnhanceChance(OwnedPet pet, int stoneTier, bool lucky)
        {
            var t = Cfg.EnhanceChanceByLevel;
            float c = t[MathUtil.Clamp(pet.Level - 1, 0, t.Length - 1)];
            int requiredTier = 1 + (pet.Level - 1) / 2;
            c += (stoneTier - requiredTier) * Cfg.EnhanceTierBonusPerTierAbove;
            c += pet.EnhanceBonusChance;
            if (lucky) c += Cfg.LuckyCharmBonus;
            return (float)MathUtil.Clamp(c, Cfg.EnhanceMinChance, 1);
        }

        public int EnhanceCost(OwnedPet pet)
        {
            var t = Cfg.EnhanceGoldByLevel;
            return t[MathUtil.Clamp(pet.Level - 1, 0, t.Length - 1)];
        }

        /// <summary>Feeds one stone of the pet's own element into it to raise its level. Failure adds bonus chance; without protection it can drop a level.</summary>
        public UpgradeResult Enhance(SaveData d, OwnedPet pet, Element element, int tier, bool useLucky, bool useProtection, SeededRng rng)
        {
            var r = new UpgradeResult();
            if (pet.Level >= db.Progression.PetMaxLevel) { r.Message = "Max level"; return r; }
            var own = db.Creature(pet.PetId).Element;
            if (element != own) { r.Message = "Needs a " + own + " stone"; return r; }
            if (d.StoneCount(element, tier) < 1) { r.Message = "No stone"; return r; }
            int cost = EnhanceCost(pet);
            if (d.Gold < cost) { r.Message = "Need " + cost + " gold"; return r; }
            if (useLucky && d.LuckyCharms <= 0) { r.Message = "No Lucky Charm"; return r; }
            if (useProtection && d.ProtectionCharms <= 0) { r.Message = "No Protection Charm"; return r; }
            r.Attempted = true;
            r.Chance = EnhanceChance(pet, tier, useLucky);
            d.Gold -= cost;
            d.AddStones(element, tier, -1);
            if (useLucky) d.LuckyCharms--;
            if (useProtection) d.ProtectionCharms--;
            r.Success = rng.Chance(r.Chance);
            if (r.Success)
            {
                pet.Level++;
                pet.EnhanceBonusChance = 0;
                r.Message = "Upgrade success! Lv " + pet.Level;
            }
            else
            {
                pet.EnhanceBonusChance += Cfg.EnhanceFailAccumulate;
                if (!useProtection && Cfg.FailDropsLevelWithoutProtection && pet.Level > 1)
                {
                    pet.Level--;
                    r.Message = "Upgrade failed. Level dropped to Lv " + pet.Level;
                }
                else r.Message = "Upgrade failed. Bonus chance +" + (int)Math.Round(Cfg.EnhanceFailAccumulate * 100) + "%";
            }
            return r;
        }

        public float CardUpgradeChance(OwnedCard card, int stoneTier, bool lucky)
        {
            var t = Cfg.CardUpgradeChanceByLevel;
            float c = t[MathUtil.Clamp(card.Level - 1, 0, t.Length - 1)];
            int requiredTier = 1 + (card.Level - 1) / 2;
            c += (stoneTier - requiredTier) * Cfg.CardStoneTierBonusPerTierAbove;
            if (lucky) c += Cfg.LuckyCharmBonus;
            return (float)MathUtil.Clamp(c, Cfg.EnhanceMinChance, 1);
        }

        public int CardUpgradeCost(OwnedCard card)
        {
            var t = Cfg.CardUpgradeGoldByLevel;
            return t[MathUtil.Clamp(card.Level - 1, 0, t.Length - 1)];
        }

        /// <summary>Feeds one card stone into a reusable card. Card stones are their own currency, never pet stones.</summary>
        public UpgradeResult UpgradeCard(SaveData d, OwnedCard card, int tier, bool useLucky, bool useProtection, SeededRng rng)
        {
            var r = new UpgradeResult();
            if (card.Level >= Cfg.MaxCardLevel) { r.Message = "Max level"; return r; }
            if (d.CardStoneCount(tier) < 1) { r.Message = "No card stone"; return r; }
            int cost = CardUpgradeCost(card);
            if (d.Gold < cost) { r.Message = "Need " + cost + " gold"; return r; }
            if (useLucky && d.LuckyCharms <= 0) { r.Message = "No Lucky Charm"; return r; }
            if (useProtection && d.ProtectionCharms <= 0) { r.Message = "No Protection Charm"; return r; }
            r.Attempted = true;
            r.Chance = CardUpgradeChance(card, tier, useLucky);
            d.Gold -= cost;
            d.AddCardStones(tier, -1);
            if (useLucky) d.LuckyCharms--;
            if (useProtection) d.ProtectionCharms--;
            r.Success = rng.Chance(r.Chance);
            if (r.Success)
            {
                card.Level++;
                r.Message = "Card upgraded! Lv " + card.Level;
            }
            else if (!useProtection && Cfg.CardFailDropsLevelWithoutProtection && card.Level > 1)
            {
                card.Level--;
                r.Message = "Upgrade failed. Level dropped to " + card.Level;
            }
            else r.Message = "Upgrade failed. The stone is lost.";
            return r;
        }

        /// <summary>Three card stones of one tier try for one of the next tier, on the stone merge table.</summary>
        public UpgradeResult MergeCardStones(SaveData d, int tier, bool useLucky, SeededRng rng)
        {
            var r = new UpgradeResult();
            if (tier >= Cfg.MaxCardStoneTier) { r.Message = "Max tier"; return r; }
            if (d.CardStoneCount(tier) < 3) { r.Message = "Need 3 card stones"; return r; }
            int cost = MergeCost(tier);
            if (d.Gold < cost) { r.Message = "Need " + cost + " gold"; return r; }
            if (useLucky && d.LuckyCharms <= 0) { r.Message = "No Lucky Charm"; return r; }
            r.Attempted = true;
            d.Gold -= cost;
            if (useLucky) d.LuckyCharms--;
            d.AddCardStones(tier, -3);
            r.Chance = MergeChance(tier, useLucky);
            r.Success = rng.Chance(r.Chance);
            if (r.Success)
            {
                d.AddCardStones(tier + 1, 1);
                r.Message = "Success! Card Stone T" + (tier + 1);
            }
            else r.Message = "Merge failed. 3 card stones lost.";
            return r;
        }

        /// <summary>Puts a stone into the first empty socket, or replaces the lowest one. The stone is consumed.</summary>
        public UpgradeResult Socket(SaveData d, OwnedPet pet, Element element, int tier)
        {
            var r = new UpgradeResult();
            if (d.StoneCount(element, tier) < 1) { r.Message = "No stone"; return r; }
            while (pet.SocketTiers.Count < Cfg.SocketCount) pet.SocketTiers.Add(0);
            while (pet.SocketElements.Count < pet.SocketTiers.Count) pet.SocketElements.Add(Element.Neutral);
            int slot = -1;
            for (int i = 0; i < pet.SocketTiers.Count; i++)
                if (slot < 0 || pet.SocketTiers[i] < pet.SocketTiers[slot]) slot = i;
            if (slot < 0 || pet.SocketTiers[slot] >= tier) { r.Message = "Sockets already hold better stones"; return r; }
            d.AddStones(element, tier, -1);
            pet.SocketTiers[slot] = tier;
            pet.SocketElements[slot] = element;
            r.Attempted = true;
            r.Success = true;
            r.Chance = 1;
            r.Message = "Socket " + (slot + 1) + ": " + element + " T" + tier;
            return r;
        }
    }
}
