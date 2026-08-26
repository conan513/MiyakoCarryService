
using System;
using System.Collections.Generic;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators.Bot;
using SPTarkov.Server.Core.Generators.Loot;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Helpers.Bot;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Services.Bot;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace MiyakoCarryService.Server.Generators.CustomGeneration
{
    [Injectable]
    public class McsBotLootGenerator(
        ISptLogger<BotLootGenerator> logger,
        RandomUtil randomUtil,
        ItemHelper itemHelper,
        InventoryHelper inventoryHelper,
        HandbookHelper handbookHelper,
        BotGeneratorHelper botGeneratorHelper,
        BotWeaponGenerator botWeaponGenerator,
        WeightedRandomHelper weightedRandomHelper,
        BotHelper botHelper,
        BotLootCacheService botLootCacheService,
        ServerLocalisationService serverLocalisationService,
        BotConfig botConfig, 
        PmcConfig pmcConfig,
        ICloner cloner
    ) : BotLootGenerator(
        logger, randomUtil, itemHelper, inventoryHelper, handbookHelper,
        botGeneratorHelper, botWeaponGenerator, weightedRandomHelper,
        botHelper, botLootCacheService, serverLocalisationService, botConfig, pmcConfig, cloner
    )
    {
        public void CustomGenerateLoot(
            MongoId botId,
            BotType botJsonTemplate,
            BotGenerationDetails botGenerationDetails,
            BotBaseInventory botInventory
        )
        {
            var itemCounts = botJsonTemplate.BotGeneration?.Items;

            if (
                itemCounts?.BackpackLoot.Weights is null
                || itemCounts.PocketLoot.Weights is null
                || itemCounts.VestLoot.Weights is null
                || itemCounts.SpecialItems.Weights is null
                || itemCounts.Healing.Weights is null
                || itemCounts.Drugs.Weights is null
                || itemCounts.Food.Weights is null
                || itemCounts.Drink.Weights is null
                || itemCounts.Currency.Weights is null
                || itemCounts.Stims.Weights is null
                || itemCounts.Grenades.Weights is null
            )
            {
                logger.Warning(serverLocalisationService.GetText("bot-unable_to_generate_bot_loot", botGenerationDetails.RoleLowercase));
                return;
            }

            // Companion botoknak legalább 5 healing item garantálva (vest/backpack-be kerülnek)
            var healingItemCount = Math.Max(weightedRandomHelper.GetWeightedValue(itemCounts.Healing.Weights), 5);
            var drugItemCount = weightedRandomHelper.GetWeightedValue(itemCounts.Drugs.Weights);
            var foodItemCount = weightedRandomHelper.GetWeightedValue(itemCounts.Food.Weights);
            var drinkItemCount = weightedRandomHelper.GetWeightedValue(itemCounts.Drink.Weights);
            // Companion botoknak legalább 3 stim garantálva
            var stimItemCount = Math.Max(weightedRandomHelper.GetWeightedValue(itemCounts.Stims.Weights), 3);
            var grenadeCount = weightedRandomHelper.GetWeightedValue(itemCounts.Grenades.Weights);

            // Mindig adjuk hozzá a PMC forced medical item-eket (config-tól függetlenül)
            if (botGenerationDetails.IsPmc)
            {
                AddForcedMedicalItemsToPmcSecure(botInventory, botGenerationDetails.RoleLowercase, botId);
            }

            var botItemLimits = GetItemSpawnLimitsForBot(botGenerationDetails.RoleLowercase);
            var containersBotHasAvailable = GetAvailableContainersBotCanStoreItemsIn(botInventory);

            // Healing items / Meds
            AddLootFromPool(
                botId,
                botLootCacheService.GetLootFromCache(
                    botGenerationDetails.RoleLowercase,
                    botGenerationDetails.IsPmc,
                    LootCacheType.HealingItems,
                    botJsonTemplate
                ),
                containersBotHasAvailable,
                healingItemCount,
                botInventory,
                botGenerationDetails.RoleLowercase,
                null,
                0,
                botGenerationDetails.IsPmc
            );

            // Drugs
            AddLootFromPool(
                botId,
                botLootCacheService.GetLootFromCache(
                    botGenerationDetails.RoleLowercase,
                    botGenerationDetails.IsPmc,
                    LootCacheType.DrugItems,
                    botJsonTemplate
                ),
                containersBotHasAvailable,
                drugItemCount,
                botInventory,
                botGenerationDetails.RoleLowercase,
                null,
                0,
                botGenerationDetails.IsPmc
            );

            // Food
            AddLootFromPool(
                botId,
                botLootCacheService.GetLootFromCache(
                    botGenerationDetails.RoleLowercase,
                    botGenerationDetails.IsPmc,
                    LootCacheType.FoodItems,
                    botJsonTemplate
                ),
                containersBotHasAvailable,
                foodItemCount,
                botInventory,
                botGenerationDetails.RoleLowercase,
                null,
                0,
                botGenerationDetails.IsPmc
            );

            // Drink
            AddLootFromPool(
                botId,
                botLootCacheService.GetLootFromCache(
                    botGenerationDetails.RoleLowercase,
                    botGenerationDetails.IsPmc,
                    LootCacheType.DrinkItems,
                    botJsonTemplate
                ),
                containersBotHasAvailable,
                drinkItemCount,
                botInventory,
                botGenerationDetails.RoleLowercase,
                null,
                0,
                botGenerationDetails.IsPmc
            );

            // Stims
            AddLootFromPool(
                botId,
                botLootCacheService.GetLootFromCache(
                    botGenerationDetails.RoleLowercase,
                    botGenerationDetails.IsPmc,
                    LootCacheType.StimItems,
                    botJsonTemplate
                ),
                containersBotHasAvailable,
                stimItemCount,
                botInventory,
                botGenerationDetails.RoleLowercase,
                botItemLimits,
                0,
                botGenerationDetails.IsPmc
            );

            // Grenades (random, a bot template Grenades.Weights alapján)
            AddLootFromPool(
                botId,
                botLootCacheService.GetLootFromCache(
                    botGenerationDetails.RoleLowercase,
                    botGenerationDetails.IsPmc,
                    LootCacheType.GrenadeItems,
                    botJsonTemplate
                ),
                new HashSet<EquipmentSlots> { EquipmentSlots.Pockets, EquipmentSlots.TacticalVest, EquipmentSlots.Backpack },
                grenadeCount,
                botInventory,
                botGenerationDetails.RoleLowercase,
                botItemLimits,
                0,
                botGenerationDetails.IsPmc
            );

            // Secure
            if (!botGenerationDetails.IsPmc || (botGenerationDetails.IsPmc && pmcConfig.AddSecureContainerLootFromBotConfig))
            {
                AddLootFromPool(
                    botId,
                    botLootCacheService.GetLootFromCache(
                        botGenerationDetails.RoleLowercase,
                        botGenerationDetails.IsPmc,
                        LootCacheType.Secure,
                        botJsonTemplate
                    ),
                    [EquipmentSlots.SecuredContainer],
                    50,
                    botInventory,
                    botGenerationDetails.RoleLowercase,
                    null,
                    -1,
                    botGenerationDetails.IsPmc
                );
            }
            
            AddGuaranteedMedicalItemsToCarryContainers(botId, botInventory);
        }

        /// <summary>
        /// 向护航的背包/战术背心/口袋添加固定医疗品（这些槽位是BotFirstAid能访问的）
        /// </summary>
        public void AddGuaranteedMedicalItemsToCarryContainers(MongoId botId, BotBaseInventory botInventory)
        {
            // Célzott tárolók: hátizsák, vest, zsebek (BotFirstAid ezeket keresi)
            HashSet<EquipmentSlots> carrySlots = [EquipmentSlots.Backpack, EquipmentSlots.TacticalVest, EquipmentSlots.Pockets];

            // Grizzly medkit (nagy HP visszatöltés, vérzés, törés kezelés)
            AddItemsToCarrySlots(botId, botInventory, carrySlots, ItemTpl.MEDKIT_GRIZZLY_MEDICAL_KIT, 1);

            // AFAK kompakt kötszerkészlet (vérzés, seb)
            AddItemsToCarrySlots(botId, botInventory, carrySlots, ItemTpl.MEDKIT_AFAK_TACTICAL_INDIVIDUAL_FIRST_AID_KIT, 2);

            // IFAK (általános elsősegély - 590c657e86f77412b013051d)
            AddItemsToCarrySlots(botId, botInventory, carrySlots, new MongoId("590c657e86f77412b013051d"), 1);

            // CAT hemostatikus érszorító (könnyű és nehéz vérzés megállítása)
            AddItemsToCarrySlots(botId, botInventory, carrySlots, ItemTpl.MEDICAL_CAT_HEMOSTATIC_TOURNIQUET, 2);

            // CMS sebészeti készlet (fekete testrészek, törések)
            AddItemsToCarrySlots(botId, botInventory, carrySlots, ItemTpl.MEDICAL_CMS_SURGICAL_KIT, 1);

            // Zagustin hemostatikus stim (vérzés gyors megállítása)
            AddItemsToCarrySlots(botId, botInventory, carrySlots, ItemTpl.STIM_ZAGUSTIN_HEMOSTATIC_DRUG_INJECTOR, 2);

            // Alumínium sín (törések)
            AddItemsToCarrySlots(botId, botInventory, carrySlots, ItemTpl.MEDICAL_ALUMINUM_SPLINT, 2);

            // Vaseline (seb fertőtlenítő, HP regeneráció - 5751a25924597722c463c472)
            AddItemsToCarrySlots(botId, botInventory, carrySlots, new MongoId("5751a25924597722c463c472"), 1);

        }

        private void AddItemsToCarrySlots(MongoId botId, BotBaseInventory botInventory, HashSet<EquipmentSlots> slots, MongoId itemTpl, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var itemId = new MongoId();
                var itemsToAdd = new List<Item>
                {
                    new() { Id = itemId, Template = itemTpl }
                };

                botGeneratorHelper.AddItemWithChildrenToEquipmentSlot(
                    botId,
                    slots,
                    itemId,
                    itemTpl,
                    itemsToAdd,
                    botInventory
                );
            }
        }
    }
}