using BepInEx;
using BepInEx.Configuration;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GuaranteedScraper
{
    [BepInPlugin("cavemoss.GuaranteedScraper", "GuaranteedScraper", "1.0.0")]

    public class GuaranteedScrapper : BaseUnityPlugin
    {
        public static ConfigEntry<int> ScrapperAmount { get; set; }

        public void Awake()
        {
            GuaranteedScrapper.ScrapperAmount = base.Config.Bind<int>("Config", "Scrapper Amount", 3, new ConfigDescription("Amount of scrappers to be spawned on each stage. (0 for vanilla spawn rate)."));

            On.RoR2.PurchaseInteraction.CanBeAffordedByInteractor += new On.RoR2.PurchaseInteraction.hook_CanBeAffordedByInteractor(this.PurchaseInteraction_CanBeAffordedByInteractor);
            On.RoR2.PurchaseInteraction.OnInteractionBegin += new On.RoR2.PurchaseInteraction.hook_OnInteractionBegin(this.PurchaseInteraction_OnInteractionBegin);
            On.RoR2.ShopTerminalBehavior.DropPickup += new On.RoR2.ShopTerminalBehavior.hook_DropPickup(this.ShopTerminalBehavior_DropPickup);
            On.RoR2.PurchaseInteraction.GetContextString += new On.RoR2.PurchaseInteraction.hook_GetContextString(this.PurchaseInteraction_GetContextString);
            On.RoR2.CostHologramContent.FixedUpdate += new On.RoR2.CostHologramContent.hook_FixedUpdate(this.CostHologramContent_FixedUpdate);
            On.RoR2.BazaarController.Awake += delegate (On.RoR2.BazaarController.orig_Awake orig, BazaarController self)
            {
                this.SpawnCauldron();
                orig.Invoke(self);
            };

            On.RoR2.SceneDirector.GenerateInteractableCardSelection += new On.RoR2.SceneDirector.hook_GenerateInteractableCardSelection(this.SceneDirector_GenerateInteractableCardSelection);
            On.RoR2.SceneDirector.PlaceTeleporter += delegate (On.RoR2.SceneDirector.orig_PlaceTeleporter orig, SceneDirector self)
            {
                orig.Invoke(self);
                if ((GuaranteedScrapper.ScrapperAmount.Value > 0 ? GuaranteedScrapper.ScrapperAmount.Value : 0) > 0)
                {
                    if (canSpawn)
                    {
                        for (int i = 0; i < GuaranteedScrapper.ScrapperAmount.Value; i++)
                        {
                            this.SpawnScrapper(self);
                        }
                    }
                }
            };

            SceneCatalog.onMostRecentSceneDefChanged += this.OnMostRecentSceneDefChanged;
        }

        private bool PurchaseInteraction_CanBeAffordedByInteractor(On.RoR2.PurchaseInteraction.orig_CanBeAffordedByInteractor orig, PurchaseInteraction self, Interactor activator)
        {
            return self.costType == CostTypeIndex.RedItem && self.cost == 1 && activator.GetComponent<CharacterBody>().inventory.GetTotalItemCountOfTier(ItemTier.Boss) > 0 || orig.Invoke(self, activator);
        }

        private async void PurchaseInteraction_OnInteractionBegin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            if (self.name.StartsWith("LunarCauldron, RedToWhite Variant"))
            {
                var rng = new System.Random();
                var types = new List<CostTypeIndex> { CostTypeIndex.RedItem, CostTypeIndex.BossItem };

                if (activator.GetComponent<CharacterBody>().inventory.GetTotalItemCountOfTier(ItemTier.Boss) > 0 &&
                    activator.GetComponent<CharacterBody>().inventory.GetItemCount(RoR2Content.Items.ScrapRed) > 0)
                {
                    if (activator.GetComponent<CharacterBody>().inventory.GetItemCount(RoR2Content.Items.ScrapYellow) > 0)
                    {
                        self.costType = types[rng.Next(types.Count)];
                        self.cost = 1;
                    }
                    else
                    {
                        self.costType = CostTypeIndex.RedItem;
                        self.cost = 1;
                    }
                }
                else if (activator.GetComponent<CharacterBody>().inventory.GetTotalItemCountOfTier(ItemTier.Boss) > 0 &&
                    !(activator.GetComponent<CharacterBody>().inventory.GetItemCount(RoR2Content.Items.ScrapYellow) > 0) &&
                    activator.GetComponent<CharacterBody>().inventory.GetTotalItemCountOfTier(ItemTier.Tier3) > 0)
                {
                    self.costType = types[rng.Next(types.Count)];
                    self.cost = 1;
                }
                else if (activator.GetComponent<CharacterBody>().inventory.GetTotalItemCountOfTier(ItemTier.Tier3) > 0 &&
                    activator.GetComponent<CharacterBody>().inventory.GetItemCount(RoR2Content.Items.ScrapYellow) > 0)
                {
                    if (activator.GetComponent<CharacterBody>().inventory.GetItemCount(RoR2Content.Items.ScrapRed) > 0)
                    {
                        self.costType = types[rng.Next(types.Count)];
                        self.cost = 1;
                    }
                    else
                    {
                        self.costType = CostTypeIndex.BossItem;
                        self.cost = 1;
                    }
                }
                else if (activator.GetComponent<CharacterBody>().inventory.GetTotalItemCountOfTier(ItemTier.Boss) > 0)
                {
                    self.costType = CostTypeIndex.BossItem;
                    self.cost = 1;
                }

                orig.Invoke(self, activator);
                ShopTerminalBehavior component = self.GetComponent<ShopTerminalBehavior>();

                await Task.Delay(1550);
                for (int i = 0; i < 3; i++)
                {
                    PickupDropletController.CreatePickupDroplet(component.pickupIndex, (component.dropTransform ? component.dropTransform : component.transform).position, component.transform.TransformVector(component.dropVelocity));
                    await Task.Delay(200);
                }

                self.costType = CostTypeIndex.RedItem;
                self.cost = 1;
            }
            else
            {
                orig.Invoke(self, activator);
            }
        }

        private void ShopTerminalBehavior_DropPickup(On.RoR2.ShopTerminalBehavior.orig_DropPickup orig, ShopTerminalBehavior self)
        {
            if (self.name.StartsWith("LunarCauldron, RedToWhite Variant"))
            {
                return;
            }
            else
            {
                orig.Invoke(self);
            }
        }

        private string PurchaseInteraction_GetContextString(On.RoR2.PurchaseInteraction.orig_GetContextString orig, PurchaseInteraction self, Interactor activator)
        {
            string result;
            if (self.name.StartsWith("LunarCauldron, RedToWhite Variant"))
            {
                PurchaseInteraction.sharedStringBuilder.Clear();
                PurchaseInteraction.sharedStringBuilder.Append(Language.GetString(self.contextToken));
                if (self.costType > CostTypeIndex.None)
                {
                    PurchaseInteraction.sharedStringBuilder.Append(" <nobr>(<color=#e7543a>1 Item(s)</color></nobr> / <nobr><color=yellow>1 Item(s)</color>)</nobr>");
                }
                result = PurchaseInteraction.sharedStringBuilder.ToString();
            }
            else
            {
                result = orig.Invoke(self, activator);
            }
            return result;
        }

        private void CostHologramContent_FixedUpdate(On.RoR2.CostHologramContent.orig_FixedUpdate orig, CostHologramContent self)
        {
            if (!canSpawn)
            {
                if ((self.displayValue == 1 && self.costType == CostTypeIndex.RedItem) || (self.displayValue == 1 && self.costType == CostTypeIndex.BossItem))
                {
                    CostHologramContent.sharedStringBuilder.Clear();
                    self.targetTextMesh.color = Color.white;
                    self.targetTextMesh.SetText("<nobr><color=#d91111>1 Item(s)</color></nobr><br>OR<br><nobr><color=#736100>1 Item(s)</color></nobr>", true);
                }
                else
                {
                    orig.Invoke(self);
                }
            }
            else
            {
                orig.Invoke(self);
            }
        }

        private WeightedSelection<DirectorCard> SceneDirector_GenerateInteractableCardSelection(On.RoR2.SceneDirector.orig_GenerateInteractableCardSelection orig, SceneDirector self)
        {
            WeightedSelection<DirectorCard> weightedSelection = orig.Invoke(self);
            if((GuaranteedScrapper.ScrapperAmount.Value > 0 ? GuaranteedScrapper.ScrapperAmount.Value : 0) > 0)
            {
                for (int i = 0; i < weightedSelection.Count; i++)
                {
                    WeightedSelection<DirectorCard>.ChoiceInfo[] choices = weightedSelection.choices;
                    string name = weightedSelection.choices[i].value.spawnCard.name;
                    if (name.Replace("Sandy", "").Replace("Snowy", "") == "iscScrapper")
                    {
                        WeightedSelection<DirectorCard>.ChoiceInfo[] choices2 = weightedSelection.choices;
                        choices2[i].weight = choices2[i].weight * 0;
                    }
                }
            }
            return weightedSelection;
        }

        public void SpawnScrapper(SceneDirector self)
        {
            Xoroshiro128Plus rng = new Xoroshiro128Plus(Reflection.GetFieldValue<Xoroshiro128Plus>(self, "rng").nextUlong);
            if (SceneInfo.instance.countsAsStage)
            {
                SpawnCard spawnCard = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscScrapper");
                DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Random
                }, rng));
            }
        }

        public void SpawnCauldron()
        {
            GameObject Cauldron = Instantiate(
                LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/LunarCauldron, RedToWhite Variant"),
                new Vector3(-122.6f, -23.65f, -5.7f),
                Quaternion.identity
            );
            Cauldron.transform.eulerAngles = new Vector3(0f, 86f, 0f);
            NetworkServer.Spawn(Cauldron);
        }

        public void OnMostRecentSceneDefChanged(SceneDef scene)
        {
            if (scene.cachedName == "moon2" || scene.cachedName == "bazaar")
            {
                canSpawn = false;
            }
            else
            {
                canSpawn = true;
            }
        }

        public bool canSpawn;
    }
}