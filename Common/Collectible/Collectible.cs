﻿using Cairo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common
{

    public delegate void CollectibleBehaviorDelegate(CollectibleBehavior behavior, ref EnumHandling handling);

    /// <summary>
    /// Contains all properties shared by Blocks and Items
    /// </summary>
    public abstract class CollectibleObject : RegistryObject
    {
        /// <summary>
        /// Liquids are handled and rendered differently than solid blocks.
        /// </summary>
        public EnumMatterState MatterState = EnumMatterState.Solid;

        /// <summary>
        /// This value is set the the BlockId or ItemId-Remapper if it encounters a block/item in the savegame, 
        /// but no longer exists as a loaded block/item
        /// </summary>
        public bool IsMissing { get; set; }

        /// <summary>
        /// The block or item id
        /// </summary>
        public abstract int Id { get; }

        /// <summary>
        /// Block or Item?
        /// </summary>
        public abstract EnumItemClass ItemClass { get; }


        /// <summary>
        /// Max amount of collectible that one default inventory slot can hold
        /// </summary>
        public int MaxStackSize = 64;

        /// <summary>
        /// How many uses does this collectible has when being used. Item disappears at durability 0
        /// </summary>
        public int Durability = 1;

        /// <summary>
        /// When true, liquids become selectable to the player when being held in hands
        /// </summary>
        public bool LiquidSelectable;

        /// <summary>
        /// How much damage this collectible deals when used as a weapon
        /// </summary>
        public float AttackPower = 0.5f;

        /// <summary>
        /// Until how for away can you attack entities using this collectibe
        /// </summary>
        public float AttackRange = GlobalConstants.DefaultAttackRange;

        /// <summary>
        /// From which damage sources does the item takes durability damage
        /// </summary>
        public EnumItemDamageSource[] DamagedBy;

        /// <summary>
        /// Modifies how fast the player can break a block when holding this item
        /// </summary>
        public Dictionary<EnumBlockMaterial, float> MiningSpeed;

        /// <summary>
        /// What tier this block can mine when held in hands
        /// </summary>
        public int ToolTier;

        [Obsolete("Use tool tier")]
        public int MiningTier { get { return ToolTier; } set { ToolTier = value; } }
        
        public HeldSounds HeldSounds;

        /// <summary>
        /// List of creative tabs in which this collectible should appear in
        /// </summary>
        public string[] CreativeInventoryTabs;

        /// <summary>
        /// If you want to add itemstacks with custom attributes to the creative inventory, add them to this list
        /// </summary>
        public CreativeTabAndStackList[] CreativeInventoryStacks;

        /// <summary>
        /// Alpha test value for rendering in gui, fp hand, tp hand or on the ground
        /// </summary>
        public float RenderAlphaTest = 0.05f;

        /// <summary>
        /// Used for scaling, rotation or offseting the block when rendered in guis
        /// </summary>
        public ModelTransform GuiTransform;

        /// <summary>
        /// Used for scaling, rotation or offseting the block when rendered in the first person mode hand
        /// </summary>
        public ModelTransform FpHandTransform;

        /// <summary>
        /// Used for scaling, rotation or offseting the block when rendered in the third person mode hand
        /// </summary>
        public ModelTransform TpHandTransform;

        /// <summary>
        /// Used for scaling, rotation or offseting the rendered as a dropped item on the ground
        /// </summary>
        public ModelTransform GroundTransform;

        /// <summary>
        /// Custom Attributes that's always assiociated with this item
        /// </summary>
        public JsonObject Attributes;

        /// <summary>
        /// Information about the burnable states
        /// </summary>
        public CombustibleProperties CombustibleProps = null;

        /// <summary>
        /// Information about the nutrition states
        /// </summary>
        public FoodNutritionProperties NutritionProps = null;

        /// <summary>
        /// Information about the transitionable states
        /// </summary>
        public TransitionableProperties[] TransitionableProps = null;

        /// <summary>
        /// If set, the collectible can be ground into something else
        /// </summary>
        public GrindingProperties GrindingProps = null;

        /// <summary>
        /// If set, the collectible can be crushed into something else
        /// </summary>
        public CrushingProperties CrushingProps = null;

        /// <summary>
        /// Particles that should spawn in regular intervals from this block or item when held in hands
        /// </summary>
        public AdvancedParticleProperties[] ParticleProperties = null;

        /// <summary>
        /// The origin point from which particles are being spawned
        /// </summary>
        public Vec3f TopMiddlePos = new Vec3f(0.5f, 1, 0.5f);


        /// <summary>
        /// If set, this item will be classified as given tool
        /// </summary>
        public EnumTool? Tool;

        /// <summary>
        /// Determines in which kind of bags the item can be stored in
        /// </summary>
        public EnumItemStorageFlags StorageFlags = EnumItemStorageFlags.General;

        /// <summary>
        /// Determines on whether an object floats on liquids or not. Water has a density of 1000
        /// </summary>
        public int MaterialDensity = 2000;

        /// <summary>
        /// The animation to play in 3rd person mod when hitting with this collectible
        /// </summary>
        public string HeldTpHitAnimation = "breakhand";

        /// <summary>
        /// The animation to play in 3rd person mod when holding this collectible in the right hand
        /// </summary>
        public string HeldRightTpIdleAnimation;

        /// <summary>
        /// The animation to play in 3rd person mod when holding this collectible in the left hand
        /// </summary>
        public string HeldLeftTpIdleAnimation;


        /// <summary>
        /// The animation to play in 3rd person mod when using this collectible
        /// </summary>
        public string HeldTpUseAnimation = "placeblock";


        
        /// <summary>
        /// The api object, assigned during OnLoaded
        /// </summary>
        protected ICoreAPI api;


        /// <summary>
        /// Modifiers that can alter the behavior of the item or block, mostly for held interaction
        /// </summary>
        public CollectibleBehavior[] CollectibleBehaviors = new CollectibleBehavior[0];


        // Non overridable so people don't accidently forget to call the base method for assigning the api in OnLoaded
        public void OnLoadedNative(ICoreAPI api)
        {
            this.api = api;
            OnLoaded(api);
        }

        /// <summary>
        /// Server Side: Called one the collectible has been registered
        /// Client Side: Called once the collectible has been loaded from server packet
        /// </summary>
        public virtual void OnLoaded(ICoreAPI api)
        {
        }

        /// <summary>
        /// Called when the client/server is shutting down
        /// </summary>
        /// <param name="api"></param>
        public virtual void OnUnloaded(ICoreAPI api)
        {

        }

        /// <summary>
        /// Should return the nutrition properties of the item/block
        /// </summary>
        /// <param name="world"></param>
        /// <param name="itemstack"></param>
        /// <param name="forEntity"></param>
        /// <returns></returns>
        public virtual FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
        {
            return NutritionProps;
        }

        /// <summary>
        /// Should return the transition properties of the item/block when in itemstack form
        /// </summary>
        /// <param name="world"></param>
        /// <param name="itemstack"></param>
        /// <param name="forEntity"></param>
        /// <returns></returns>
        public virtual TransitionableProperties[] GetTransitionableProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
        {
            return TransitionableProps;
        }


        /// <summary>
        /// Should return in which storage containers this item can be placed in
        /// </summary>
        /// <param name="itemstack"></param>
        /// <returns></returns>
        public virtual EnumItemStorageFlags GetStorageFlags(ItemStack itemstack)
        {
            // We clear the backpack flag if the backpack is empty
            if ((StorageFlags & EnumItemStorageFlags.Backpack) > 0 && IsEmptyBackPack(itemstack)) return EnumItemStorageFlags.General | EnumItemStorageFlags.Backpack;

            return StorageFlags;
        }

        /// <summary>
        /// Returns a hardcoded rgb color (green->yellow->red) that is representative for its remaining durability vs total durability
        /// </summary>
        /// <param name="itemstack"></param>
        /// <returns></returns>
        public virtual int GetItemDamageColor(ItemStack itemstack)
        {
            int maxdura = GetDurability(itemstack);
            if (maxdura == 0) return 0;

            int p = GameMath.Clamp(100 * itemstack.Attributes.GetInt("durability") / maxdura, 0, 99); ;

            return GuiStyle.DamageColorGradient[p];
        }

        /// <summary>
        /// Return true if remaining durability != total durability
        /// </summary>
        /// <param name="itemstack"></param>
        /// <returns></returns>
        public virtual bool ShouldDisplayItemDamage(IItemStack itemstack)
        {
            return Durability != itemstack.Attributes.GetInt("durability", GetDurability(itemstack));
        }



        /// <summary>
        /// This method is called before rendering the item stack into GUI, first person hand, third person hand and/or on the ground
        /// The renderinfo object is pre-filled with default values. 
        /// </summary>
        /// <param name="capi"></param>
        /// <param name="itemstack"></param>
        /// <param name="target"></param>
        /// <param name="renderinfo"></param>
        public virtual void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            for (int i = 0; i < CollectibleBehaviors.Length; i++)
            {
                CollectibleBehaviors[i].OnBeforeRender(capi, itemstack, target, ref renderinfo);
            }
        }


        /// <summary>
        /// Returns the items total durability
        /// </summary>
        /// <param name="itemstack"></param>
        /// <returns></returns>
        public virtual int GetDurability(IItemStack itemstack)
        {
            return Durability;
        }

        /// <summary>
        /// The amount of damage dealt when used as a weapon
        /// </summary>
        /// <param name="withItemStack"></param>
        /// <returns></returns>
        public virtual float GetAttackPower(IItemStack withItemStack)
        {
            return AttackPower;
        }

        /// <summary>
        /// The the attack range when used as a weapon
        /// </summary>
        /// <param name="withItemStack"></param>
        /// <returns></returns>
        public virtual float GetAttackRange(IItemStack withItemStack)
        {
            return AttackRange;
        }



        /// <summary>
        /// Player is holding this collectible and breaks the targeted block
        /// </summary>
        /// <param name="player"></param>
        /// <param name="blockSel"></param>
        /// <param name="itemslot"></param>
        /// <param name="remainingResistance"></param>
        /// <param name="dt"></param>
        /// <param name="counter"></param>
        /// <returns></returns>
        public virtual float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
        {
            Block block = player.Entity.World.BlockAccessor.GetBlock(blockSel.Position);
            var mat = block.GetBlockMaterial(api.World.BlockAccessor, blockSel.Position);

            Vec3f faceVec = blockSel.Face.Normalf;
            Random rnd = player.Entity.World.Rand;

            bool cantMine = block.RequiredMiningTier > 0 && itemslot.Itemstack?.Collectible != null && (itemslot.Itemstack.Collectible.ToolTier < block.RequiredMiningTier || (MiningSpeed == null || !MiningSpeed.ContainsKey(mat)));

            double chance = mat == EnumBlockMaterial.Ore ? 0.72 : 0.12;

            if ((counter % 5 == 0) && (rnd.NextDouble() < chance || cantMine) && (mat == EnumBlockMaterial.Stone || mat == EnumBlockMaterial.Ore) && (Tool == EnumTool.Pickaxe || Tool == EnumTool.Hammer))
            {
                double posx = blockSel.Position.X + blockSel.HitPosition.X;
                double posy = blockSel.Position.Y + blockSel.HitPosition.Y;
                double posz = blockSel.Position.Z + blockSel.HitPosition.Z;

                player.Entity.World.SpawnParticles(new SimpleParticleProperties()
                {
                    MinQuantity = 0,
                    AddQuantity = 8,
                    Color = ColorUtil.ToRgba(255, 255, 255, 128),
                    MinPos = new Vec3d(posx + faceVec.X * 0.01f, posy + faceVec.Y * 0.01f, posz + faceVec.Z * 0.01f),
                    AddPos = new Vec3d(0, 0, 0),
                    MinVelocity = new Vec3f(
                        4 * faceVec.X,
                        4 * faceVec.Y,
                        4 * faceVec.Z
                    ),
                    AddVelocity = new Vec3f(
                        8 * ((float)rnd.NextDouble() - 0.5f),
                        8 * ((float)rnd.NextDouble() - 0.5f),
                        8 * ((float)rnd.NextDouble() - 0.5f)
                    ),
                    LifeLength = 0.025f,
                    GravityEffect = 0f,
                    MinSize = 0.03f,
                    MaxSize = 0.4f,
                    ParticleModel = EnumParticleModel.Cube,
                    VertexFlags = 200,
                    SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.15f)
                }, player);
            }


            if (cantMine)
            {
                return remainingResistance;
            }

            return remainingResistance - GetMiningSpeed(itemslot.Itemstack, blockSel, block, player) * dt;
        }


        /// <summary>
        /// Whenever the collectible was modified while inside a slot, usually when it was moved, split or merged.  
        /// </summary>
        /// <param name="world"></param>
        /// <param name="slot">The slot the item is or was in</param>
        /// <param name="extractedStack">Non null if the itemstack was removed from this slot</param>
        public virtual void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
        {
        }


        /// <summary>
        /// Player has broken a block while holding this collectible. Return false if you want to cancel the block break event.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="byEntity"></param>
        /// <param name="itemslot"></param>
        /// <param name="blockSel"></param>
        /// <returns></returns>
        public virtual bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

            Block block = world.BlockAccessor.GetBlock(blockSel.Position);
            block.OnBlockBroken(world, blockSel.Position, byPlayer, dropQuantityMultiplier);

            if (DamagedBy != null && DamagedBy.Contains(EnumItemDamageSource.BlockBreaking))
            {
                DamageItem(world, byEntity, itemslot);
            }

            return true;
        }



        /// <summary>
        /// Called every game tick when the player breaks a block with this item in his hands. Returns the mining speed for given block.
        /// </summary>
        /// <param name="itemstack"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        public virtual float GetMiningSpeed(IItemStack itemstack, BlockSelection blockSel, Block block, IPlayer forPlayer)
        {
            float traitRate = 1f;

            var mat = block.GetBlockMaterial(api.World.BlockAccessor, blockSel.Position);

            if (mat == EnumBlockMaterial.Ore || mat == EnumBlockMaterial.Stone) {
                traitRate = forPlayer.Entity.Stats.GetBlended("miningSpeedMul");
            }

            if (MiningSpeed == null || !MiningSpeed.ContainsKey(mat)) return traitRate;

            return MiningSpeed[mat] * GlobalConstants.ToolMiningSpeedModifier * traitRate;
        }


        /// <summary>
        /// Not implemented yet
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <returns></returns>
        public virtual ModelTransformKeyFrame[] GeldHeldFpHitAnimation(ItemSlot slot, Entity byEntity)
        {
            return null;
        }

        /// <summary>
        /// Called when an entity uses this item to hit something in 3rd person mode
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <returns></returns>
        public virtual string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
        {
            return HeldTpHitAnimation;
        }

        /// <summary>
        /// Called when an entity holds this item in hands in 3rd person mode
        /// </summary>
        /// <param name="activeHotbarSlot"></param>
        /// <param name="forEntity"></param>
        /// <returns></returns>
        public virtual string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
        {
            return hand == EnumHand.Left ? HeldLeftTpIdleAnimation : HeldRightTpIdleAnimation;
        }

        /// <summary>
        /// Called when an entity holds this item in hands in 3rd person mode
        /// </summary>
        /// <param name="activeHotbarSlot"></param>
        /// <param name="forEntity"></param>
        /// <returns></returns>
        public virtual string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
        {
            if (GetNutritionProperties(forEntity.World, activeHotbarSlot.Itemstack, forEntity) != null) return null;

            return HeldTpUseAnimation;
        }

        /// <summary>
        /// An entity used this collectibe to attack something
        /// </summary>
        /// <param name="world"></param>
        /// <param name="byEntity"></param>
        /// <param name="attackedEntity"></param>
        /// <param name="itemslot"></param>
        public virtual void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
        {
            if (DamagedBy != null && DamagedBy.Contains(EnumItemDamageSource.Attacking) && attackedEntity?.Alive == true)
            {
                DamageItem(world, byEntity, itemslot);
            }
        }


        /// <summary>
        /// Called when this collectible is attempted to being used as part of a crafting recipe and should get consumed now. Return false if it doesn't match the ingredient
        /// </summary>
        /// <param name="inputStack"></param>
        /// <param name="gridRecipe"></param>
        /// <param name="ingredient"></param>
        /// <returns></returns>
        public virtual bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient)
        {
            if (ingredient.IsTool && ingredient.ToolDurabilityCost > inputStack.Attributes.GetInt("durability", GetDurability(inputStack))) return false;

            return true;
        }



        /// <summary>
        /// Called when this collectible is being used as part of a crafting recipe and should get consumed now
        /// </summary>
        /// <param name="allInputSlots"></param>
        /// <param name="stackInSlot"></param>
        /// <param name="gridRecipe"></param>
        /// <param name="fromIngredient"></param>
        /// <param name="byPlayer"></param>
        /// <param name="quantity"></param>
        public virtual void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
        {
            if (fromIngredient.IsTool)
            {
                stackInSlot.Itemstack.Collectible.DamageItem(byPlayer.Entity.World, byPlayer.Entity, stackInSlot, fromIngredient.ToolDurabilityCost);
            }
            else
            {
                stackInSlot.Itemstack.StackSize -= quantity;

                if (stackInSlot.Itemstack.StackSize <= 0)
                {
                    stackInSlot.Itemstack = null;
                    stackInSlot.MarkDirty();
                }

                if (fromIngredient.ReturnedStack != null)
                {
                    byPlayer.InventoryManager.TryGiveItemstack(fromIngredient.ReturnedStack.ResolvedItemstack.Clone(), true);
                }
            }
        }


        /// <summary>
        /// Called when a matching grid recipe has been found and an item is placed into the crafting output slot (which is still before the player clicks on the output slot to actually craft the item and consume the ingredients)
        /// </summary>
        /// <param name="allInputslots"></param>
        /// <param name="outputSlot"></param>
        /// <param name="byRecipe"></param>
        public virtual void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
        {

        }



        /// <summary>
        /// Causes the item to be damaged. Will play a breaking sound and removes the itemstack if no more durability is left
        /// </summary>
        /// <param name="world"></param>
        /// <param name="byEntity"></param>
        /// <param name="itemslot"></param>
        /// <param name="amount">Amount of damage</param>
        public virtual void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
        {
            IItemStack itemstack = itemslot.Itemstack;

            int leftDurability = itemstack.Attributes.GetInt("durability", GetDurability(itemstack));
            leftDurability -= amount;
            itemstack.Attributes.SetInt("durability", leftDurability);

            if (leftDurability <= 0)
            {
                itemslot.Itemstack = null;

                if (byEntity is EntityAgent && Tool != null)
                {
                    RefillSlotIfEmpty(itemslot, byEntity as EntityAgent, (stack) => stack.Collectible.Tool == Tool);
                }

                if (byEntity is EntityPlayer)
                {
                    IPlayer player = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
                    world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), player, player);
                } else
                {
                    world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z);
                }
            }

            itemslot.MarkDirty();
        }



        protected virtual void RefillSlotIfEmpty(ItemSlot slot, EntityAgent byEntity, ActionConsumable<ItemStack> matcher)
        {
            if (!slot.Empty) return;

            byEntity.WalkInventory((invslot) =>
            {
                if (invslot is ItemSlotCreative) return true;

                InventoryBase inv = invslot.Inventory;
                if (!(inv is InventoryBasePlayer) && !inv.HasOpened((byEntity as EntityPlayer).Player)) return true;

                if (invslot.Itemstack != null && matcher(invslot.Itemstack))
                {
                    invslot.TryPutInto(byEntity.World, slot);
                    invslot.Inventory.PerformNotifySlot(invslot.Inventory.GetSlotId(invslot));
                    slot.Inventory.PerformNotifySlot(slot.Inventory.GetSlotId(slot));

                    return false;
                }

                return true;
            });
        }


        public virtual SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            return null;
        }

        /// <summary>
        /// Should return the current items tool mode.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byPlayer"></param>
        /// <param name="blockSelection"></param>
        /// <returns></returns>
        public virtual int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
        {
            return 0;
        }

        /// <summary>
        /// Should set given toolmode
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byPlayer"></param>
        /// <param name="blockSelection"></param>
        /// <param name="toolMode"></param>
        public virtual void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {

        }

        /// <summary>
        /// This method is called during the opaque render pass when this item or block is being held in hands
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="byPlayer"></param>
        public virtual void OnHeldRenderOpaque(ItemSlot inSlot, IClientPlayer byPlayer)
        {

        }

        /// <summary>
        /// This method is called during the order independent transparency render pass when this item or block is being held in hands
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="byPlayer"></param>
        public virtual void OnHeldRenderOit(ItemSlot inSlot, IClientPlayer byPlayer)
        {

        }

        /// <summary>
        /// This method is called during the ortho (for 2D GUIs) render pass when this item or block is being held in hands
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="byPlayer"></param>
        public virtual void OnHeldRenderOrtho(ItemSlot inSlot, IClientPlayer byPlayer)
        {

        }



        /// <summary>
        /// Called every frame when the player is holding this collectible in his hands. Is not called during OnUsing() or OnAttacking()
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        public virtual void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
        {

        }

        /// <summary>
        /// Called every game tick when this collectible is in dropped form in the world (i.e. as EntityItem)
        /// </summary>
        /// <param name="entityItem"></param>
        public virtual void OnGroundIdle(EntityItem entityItem)
        {
            if (entityItem.Swimming && api.Side == EnumAppSide.Server && Attributes?.IsTrue("dissolveInWater") == true)
            {
                if (api.World.Rand.NextDouble() < 0.01)
                {
                    api.World.SpawnCubeParticles(entityItem.ServerPos.XYZ, entityItem.Itemstack.Clone(), 0.1f, 80, 0.3f);
                    entityItem.Die();
                } else
                {
                    if (api.World.Rand.NextDouble() < 0.2)
                    {
                        api.World.SpawnCubeParticles(entityItem.ServerPos.XYZ, entityItem.Itemstack.Clone(), 0.1f, 2, 0.2f + (float)api.World.Rand.NextDouble() / 5f);
                    }
                }
            }
        }

        /// <summary>
        /// Called every frame when this item is being displayed in the gui
        /// </summary>
        /// <param name="world"></param>
        /// <param name="stack"></param>
        public virtual void InGuiIdle(IWorldAccessor world, ItemStack stack)
        {

        }



        /// <summary>
        /// General begin use access. Override OnHeldAttackStart or OnHeldInteractStart to alter the behavior.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <param name="useType"></param>
        /// <param name="firstEvent">True on first mouse down</param>
        /// <param name="handling">Whether or not to do any subsequent actions. If not set or set to NotHandled, the action will not called on the server.</param>
        /// <returns></returns>
        public void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
        {
            if (useType == EnumHandInteract.HeldItemAttack)
            {
                OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
                return;
            }

            if (useType == EnumHandInteract.HeldItemInteract)
            {
                OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }

        }

        /// <summary>
        /// General cancel use access. Override OnHeldAttackCancel or OnHeldInteractCancel to alter the behavior.
        /// </summary>
        /// <param name="secondsPassed"></param>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <param name="cancelReason"></param>
        /// <returns></returns>
        public EnumHandInteract OnHeldUseCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            EnumHandInteract useType = byEntity.Controls.HandUse;

            bool allowCancel = useType == EnumHandInteract.HeldItemAttack ? OnHeldAttackCancel(secondsPassed, slot, byEntity, blockSel, entitySel, cancelReason) : OnHeldInteractCancel(secondsPassed, slot, byEntity, blockSel, entitySel, cancelReason);
            return allowCancel ? EnumHandInteract.None : useType;
        }

        /// <summary>
        /// General using access. Override OnHeldAttackStep or OnHeldInteractStep to alter the behavior.
        /// </summary>
        /// <param name="secondsPassed"></param>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <returns></returns>
        public EnumHandInteract OnHeldUseStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            EnumHandInteract useType = byEntity.Controls.HandUse;

            bool shouldContinueUse = useType == EnumHandInteract.HeldItemAttack ? OnHeldAttackStep(secondsPassed, slot, byEntity, blockSel, entitySel) : OnHeldInteractStep(secondsPassed, slot, byEntity, blockSel, entitySel);

            return shouldContinueUse ? useType : EnumHandInteract.None;
        }

        /// <summary>
        /// General use over access. Override OnHeldAttackStop or OnHeldInteractStop to alter the behavior.
        /// </summary>
        /// <param name="secondsPassed"></param>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <param name="useType"></param>
        public void OnHeldUseStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType)
        {
            if (useType == EnumHandInteract.HeldItemAttack)
            {
                OnHeldAttackStop(secondsPassed, slot, byEntity, blockSel, entitySel);
            } else
            {
                OnHeldInteractStop(secondsPassed, slot, byEntity, blockSel, entitySel);
            }
        }


        /// <summary>
        /// When the player has begun using this item for attacking (left mouse click). Return true to play a custom action.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <param name="handling">Whether or not to do any subsequent actions. If not set or set to NotHandled, the action will not called on the server.</param>
        /// <returns></returns>
        public virtual void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            if (HeldSounds?.Attack != null && api.World.Side == EnumAppSide.Client)
            {
                api.World.PlaySoundAt(HeldSounds.Attack, 0, 0, 0, null, 0.9f + (float)api.World.Rand.NextDouble() * 0.2f);
            }
        }

        /// <summary>
        /// When the player has canceled a custom attack action. Return false to deny action cancellation.
        /// </summary>
        /// <param name="secondsPassed"></param>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSelection"></param>
        /// <param name="entitySel"></param>
        /// <param name="cancelReason"></param>
        /// <returns></returns>
        public virtual bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            return false;
        }

        /// <summary>
        /// Called continously when a custom attack action is playing. Return false to stop the action.
        /// </summary>
        /// <param name="secondsPassed"></param>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSelection"></param>
        /// <param name="entitySel"></param>
        /// <returns></returns>
        public virtual bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
            return false;
        }

        /// <summary>
        /// Called when a custom attack action is finished
        /// </summary>
        /// <param name="secondsPassed"></param>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSelection"></param>
        /// <param name="entitySel"></param>
        public virtual void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {

        }


        /// <summary>
        /// Called when the player right clicks while holding this block/item in his hands
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <param name="firstEvent">True when the player pressed the right mouse button on this block. Every subsequent call, while the player holds right mouse down will be false, it gets called every second while right mouse is down</param>
        /// <param name="handling">Whether or not to do any subsequent actions. If not set or set to NotHandled, the action will not called on the server.</param>
        /// <returns>True if an interaction should happen (makes it sync to the server), false if no sync to server is required</returns>
        public virtual void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            EnumHandHandling bhHandHandling = EnumHandHandling.NotHandled;
            WalkBehaviors(
                (CollectibleBehavior bh, ref EnumHandling hd) => bh.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref bhHandHandling, ref hd),
                () => tryEatBegin(slot, byEntity, ref bhHandHandling)
            );
            handling = bhHandHandling;
        }


        /// <summary>
        /// Called every frame while the player is using this collectible. Return false to stop the interaction.
        /// </summary>
        /// <param name="secondsUsed"></param>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <returns>False if the interaction should be stopped. True if the interaction should continue</returns>
        public virtual bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            bool result = true;
            bool preventDefault = false;

            foreach (CollectibleBehavior behavior in CollectibleBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                bool behaviorResult = behavior.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handled);
                if (handled != EnumHandling.PassThrough)
                {
                    result &= behaviorResult;
                    preventDefault = true;
                }

                if (handled == EnumHandling.PreventSubsequent) return result;
            }

            if (preventDefault) return result;

            return tryEatStep(secondsUsed, slot, byEntity);
        }


        /// <summary>
        /// Called when the player successfully completed the using action, always called once an interaction is over
        /// </summary>
        /// <param name="secondsUsed"></param>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        public virtual void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            bool preventDefault = false;

            foreach (CollectibleBehavior behavior in CollectibleBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handled);
                if (handled != EnumHandling.PassThrough) preventDefault = true;

                if (handled == EnumHandling.PreventSubsequent) return;
            }

            if (preventDefault) return;

            tryEatStop(secondsUsed, slot, byEntity);
        }



        /// <summary>
        /// When the player released the right mouse button. Return false to deny the cancellation (= will keep using the item until OnHeldInteractStep returns false).
        /// </summary>
        /// <param name="secondsUsed"></param>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <param name="cancelReason"></param>
        /// <returns></returns>
        public virtual bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            return true;
        }



        /// <summary>
        /// Tries to eat the contents in the slot, first call
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="handling"></param>
        protected virtual void tryEatBegin(ItemSlot slot, EntityAgent byEntity, ref EnumHandHandling handling)
        {
            if (GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity as Entity) != null)
            {
                byEntity.World.RegisterCallback((dt) =>
                {
                    if (byEntity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
                    {
                        IPlayer player = null;
                        if (byEntity is EntityPlayer) player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

                        byEntity.PlayEntitySound("eat", player);
                    }
                }, 500);

                byEntity.AnimManager?.StartAnimation("eat");

                handling = EnumHandHandling.PreventDefault;
            }
        }

        /// <summary>
        /// Tries to eat the contents in the slot, eat step call
        /// </summary>
        /// <param name="secondsUsed"></param>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        protected virtual bool tryEatStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
        {
            if (GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity as Entity) == null) return false;


            Vec3d pos = byEntity.Pos.AheadCopy(0.4f).XYZ;
            pos.X += byEntity.LocalEyePos.X;
            pos.Y += byEntity.LocalEyePos.Y - 0.4f;
            pos.Z += byEntity.LocalEyePos.Z;
            //pos.Y += byEntity.EyeHeight - 0.4f;

            if (secondsUsed > 0.5f && (int)(30 * secondsUsed) % 7 == 1)
            {
                byEntity.World.SpawnCubeParticles(pos, slot.Itemstack, 0.3f, 4, 0.5f, (byEntity as EntityPlayer)?.Player);
            }


            if (byEntity.World is IClientWorldAccessor)
            {
                ModelTransform tf = new ModelTransform();

                tf.EnsureDefaultValues();

                tf.Origin.Set(0f, 0, 0f);

                if (secondsUsed > 0.5f)
                {
                    tf.Translation.Y = Math.Min(0.02f, GameMath.Sin(20 * secondsUsed) / 10);
                }

                tf.Translation.X -= Math.Min(1f, secondsUsed * 4 * 1.57f);
                tf.Translation.Y -= Math.Min(0.05f, secondsUsed * 2);

                tf.Rotation.X += Math.Min(30f, secondsUsed * 350);
                tf.Rotation.Y += Math.Min(80f, secondsUsed * 350);

                byEntity.Controls.UsingHeldItemTransformAfter = tf;

                return secondsUsed <= 1f;
            }

            // Let the client decide when he is done eating
            return true;
        }

        /// <summary>
        /// Finished eating the contents in the slot, final call
        /// </summary>
        /// <param name="secondsUsed"></param>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        protected virtual void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
        {
            FoodNutritionProperties nutriProps = GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity as Entity);

            if (byEntity.World is IServerWorldAccessor && nutriProps != null && secondsUsed >= 0.95f)
            {
                TransitionState state = UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish);
                float spoilState = state != null ? state.TransitionLevel : 0;

                float satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, slot.Itemstack, byEntity);
                float healthLossMul = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, slot.Itemstack, byEntity);

                byEntity.ReceiveSaturation(nutriProps.Satiety * satLossMul, nutriProps.FoodCategory);

                IPlayer player = null;
                if (byEntity is EntityPlayer) player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

                if (nutriProps.EatenStack != null)
                {

                    if (player == null || !player.InventoryManager.TryGiveItemstack(nutriProps.EatenStack.ResolvedItemstack.Clone(), true))
                    {
                        byEntity.World.SpawnItemEntity(nutriProps.EatenStack.ResolvedItemstack.Clone(), byEntity.SidedPos.XYZ);
                    }
                }

                slot.Itemstack.StackSize--;

                float healthChange = nutriProps.Health * healthLossMul;

                if (healthChange != 0)
                {
                    byEntity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Internal, Type = healthChange > 0 ? EnumDamageType.Heal : EnumDamageType.Poison }, Math.Abs(healthChange));
                }

                slot.MarkDirty();
                player.InventoryManager.BroadcastHotbarSlot();
            }
        }



        /// <summary>
        /// Callback when the player dropped this item from his inventory. You can set handling to PreventDefault to prevent dropping this item.
        /// You can also check if the entityplayer of this player is dead to check if dropping of this item was due the players death
        /// </summary>
        /// <param name="world"></param>
        /// <param name="byPlayer"></param>
        /// <param name="slot"></param>
        /// <param name="quantity">Amount of items the player wants to drop</param>
        /// <param name="handling"></param>
        public virtual void OnHeldDropped(IWorldAccessor world, IPlayer byPlayer, ItemSlot slot, int quantity, ref EnumHandling handling)
        {

        }


        /// <summary>
        /// Called by the inventory system when you hover over an item stack. This is the item stack name that is getting displayed.
        /// </summary>
        /// <param name="itemStack"></param>
        /// <returns></returns>
        public virtual string GetHeldItemName(ItemStack itemStack)
        {
            string type = ItemClass.Name();

            return Lang.GetMatching(Code?.Domain + AssetLocation.LocationSeparator + type + "-" + Code?.Path);
        }


        /// <summary>
        /// Called by the inventory system when you hover over an item stack. This is the text that is getting displayed.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="dsc"></param>
        /// <param name="world"></param>
        /// <param name="withDebugInfo"></param>
        public virtual void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            ItemStack stack = inSlot.Itemstack;

            string descLangCode = Code?.Domain + AssetLocation.LocationSeparator + ItemClass.ToString().ToLowerInvariant() + "desc-" + Code?.Path;
            string descText = Lang.GetMatching(descLangCode);
            if (descText == descLangCode) descText = "";
            else descText = descText + "\n";

            dsc.Append((withDebugInfo ? "Id: " + Id + "\n" : ""));
            dsc.Append((withDebugInfo ? "Code: " + Code + "\n" : ""));

            int durability = GetDurability(stack);

            if (durability > 1)
            {
                dsc.AppendLine(Lang.Get("Durability: {0} / {1}", stack.Attributes.GetInt("durability", durability), durability));
            }


            if (MiningSpeed != null && MiningSpeed.Count > 0)
            {
                dsc.AppendLine(Lang.Get("Tool Tier: {0}", ToolTier));

                dsc.Append(Lang.Get("item-tooltip-miningspeed"));
                int i = 0;
                foreach (var val in MiningSpeed)
                {
                    if (val.Value < 1.1) continue;

                    if (i > 0) dsc.Append(", ");
                    dsc.Append(Lang.Get(val.Key.ToString()) + " " + val.Value.ToString("#.#") + "x");
                    i++;
                }

                dsc.Append("\n");

            }

            if (IsBackPack(stack))
            {
                dsc.AppendLine(Lang.Get("Quantity Slots: {0}", QuantityBackPackSlots(stack)));
                ITreeAttribute backPackTree = stack.Attributes.GetTreeAttribute("backpack");
                if (backPackTree != null)
                {
                    bool didPrint = false;

                    ITreeAttribute slotsTree = backPackTree.GetTreeAttribute("slots");

                    foreach (var val in slotsTree)
                    {
                        ItemStack cstack = (ItemStack)val.Value?.GetValue();
                        
                        if (cstack != null && cstack.StackSize > 0)
                        {
                            if (!didPrint)
                            {
                                dsc.AppendLine(Lang.Get("Contents: "));
                                didPrint = true;
                            }
                            cstack.ResolveBlockOrItem(world);
                            dsc.AppendLine("- " + cstack.StackSize + "x " + cstack.GetName());
                        }
                    }

                    if (!didPrint)
                    {
                        dsc.AppendLine(Lang.Get("Empty"));
                    }

                }
            }

            EntityPlayer entity = world.Side == EnumAppSide.Client ? (world as IClientWorldAccessor).Player.Entity : null;

            float spoilState = AppendPerishableInfoText(inSlot, dsc, world);

            FoodNutritionProperties nutriProps = GetNutritionProperties(world, stack, entity);
            if (nutriProps != null)
            {
                float satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, stack, entity);
                float healthLossMul = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, stack, entity);

                if (Math.Abs(nutriProps.Health * healthLossMul) > 0.001f)
                {
                    dsc.AppendLine(Lang.Get("When eaten: {0} sat, {1} hp", Math.Round(nutriProps.Satiety * satLossMul), nutriProps.Health * healthLossMul));
                }
                else
                {
                    dsc.AppendLine(Lang.Get("When eaten: {0} sat", Math.Round(nutriProps.Satiety * satLossMul)));
                }

                dsc.AppendLine(Lang.Get("Food Category: {0}", Lang.Get("foodcategory-" + nutriProps.FoodCategory.ToString().ToLowerInvariant())));
            }
            


            if (GrindingProps != null)
            {
                dsc.AppendLine(Lang.Get("When ground: Turns into {0}x {1}", GrindingProps.GroundStack.ResolvedItemstack.StackSize, GrindingProps.GroundStack.ResolvedItemstack.GetName()));
            }

            if (CrushingProps != null)
            {
                dsc.AppendLine(Lang.Get("When pulverized: Turns into {0}x {1}", CrushingProps.CrushedStack.ResolvedItemstack.StackSize, CrushingProps.CrushedStack.ResolvedItemstack.GetName()));
                dsc.AppendLine(Lang.Get("Requires Pulverizer tier: {0}", CrushingProps.HardnessTier));
            }

            if (GetAttackPower(stack) > 0.5f)
            {
                dsc.AppendLine(Lang.Get("Attack power: -{0} hp", GetAttackPower(stack).ToString("0.#")));
                dsc.AppendLine(Lang.Get("Attack tier: {0}", ToolTier));
            }

            if (GetAttackRange(stack) > GlobalConstants.DefaultAttackRange)
            {
                dsc.AppendLine(Lang.Get("Attack range: {0} m", GetAttackRange(stack).ToString("0.#")));
            }

            if (CombustibleProps != null)
            {
                if (CombustibleProps.BurnTemperature > 0)
                {
                    dsc.AppendLine(Lang.Get("Burn temperature: {0}°C", CombustibleProps.BurnTemperature));
                    dsc.AppendLine(Lang.Get("Burn duration: {0}s", CombustibleProps.BurnDuration));
                }


                string smelttype = CombustibleProps.SmeltingType.ToString().ToLowerInvariant();
                if (CombustibleProps.MeltingPoint > 0)
                {
                    dsc.AppendLine(Lang.Get("game:smeltpoint-" + smelttype, CombustibleProps.MeltingPoint));
                }

                if (CombustibleProps.SmeltedStack?.ResolvedItemstack != null)
                {
                    int instacksize = CombustibleProps.SmeltedRatio;
                    int outstacksize = CombustibleProps.SmeltedStack.ResolvedItemstack.StackSize;


                    string str = instacksize == 1 ?
                        Lang.Get("game:smeltdesc-" + smelttype + "-singular", outstacksize, CombustibleProps.SmeltedStack.ResolvedItemstack.GetName()) :
                        Lang.Get("game:smeltdesc-" + smelttype + "-plural", instacksize, outstacksize, CombustibleProps.SmeltedStack.ResolvedItemstack.GetName())
                    ;

                    dsc.AppendLine(str);
                }
            }

            if (descText.Length > 0 && dsc.Length > 0) dsc.Append("\n");
            dsc.Append(descText);

            if (Attributes?["pigment"]?["color"].Exists == true)
            {
                dsc.AppendLine(Lang.Get("Pigment: {0}", Lang.Get(Attributes["pigment"]["name"].AsString())));
            }


            JsonObject obj = Attributes?["fertilizerProps"];
            if (obj != null && obj.Exists)
            {
                FertilizerProps props = obj.AsObject<FertilizerProps>();
                if (props != null)
                {
                    dsc.AppendLine(Lang.Get("Fertilizer: {0}% N, {1}% P, {2}% K", props.N, props.P, props.K));
                }
            }

            


            float temp = GetTemperature(world, stack);
            if (temp > 20)
            {
                dsc.AppendLine(Lang.Get("Temperature: {0}°C", (int)temp));
            }
        }


        /// <summary>
        /// Interaction help thats displayed above the hotbar, when the player puts this item/block in his active hand slot
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        public virtual WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            WorldInteraction[] interactions;

            if (GetNutritionProperties(api.World, inSlot.Itemstack, null) != null)
            {
                interactions = new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "heldhelp-eat",
                        MouseButton = EnumMouseButton.Right
                    }
                };
            } else
            {
                interactions = new WorldInteraction[0];
            }

            EnumHandling handled = EnumHandling.PassThrough;

            foreach (CollectibleBehavior behavior in CollectibleBehaviors)
            {
                WorldInteraction[] bhi = behavior.GetHeldInteractionHelp(inSlot, ref handled);

                interactions = interactions.Append(bhi);

                if (handled == EnumHandling.PreventSubsequent) break;
            }

            return interactions;
        }



        public virtual float AppendPerishableInfoText(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world)
        {
            float spoilState = 0;
            TransitionState[] transitionStates = UpdateAndGetTransitionStates(api.World, inSlot);

            bool nowSpoiling = false;

            if (transitionStates != null)
            {
                for (int i = 0; i < transitionStates.Length; i++)
                {
                    TransitionState state = transitionStates[i];

                    TransitionableProperties prop = state.Props;
                    float perishRate = GetTransitionRateMul(world, inSlot, prop.Type);
                    if (inSlot.Inventory is CreativeInventoryTab) perishRate = 1f;
                    float transitionLevel = state.TransitionLevel;
                    float freshHoursLeft = state.FreshHoursLeft / perishRate;

                    switch (prop.Type)
                    {
                        case EnumTransitionType.Perish:
                            spoilState = transitionLevel;

                            if (transitionLevel > 0)
                            {
                                nowSpoiling = true;
                                dsc.AppendLine(Lang.Get("itemstack-perishable-spoiling", (int)Math.Round(transitionLevel * 100)));
                            }
                            else
                            {
                                if (perishRate <= 0)
                                {
                                    dsc.AppendLine(Lang.Get("itemstack-perishable"));
                                }
                                else
                                {

                                    float hoursPerday = api.World.Calendar.HoursPerDay;
                                    float years = freshHoursLeft / hoursPerday / api.World.Calendar.DaysPerYear;

                                    if (years >= 1.0f)
                                    {
                                        if (years <= 1.05f)
                                        {
                                            dsc.AppendLine(Lang.Get("itemstack-perishable-fresh-one-year"));
                                        }
                                        else
                                        {
                                            dsc.AppendLine(Lang.Get("itemstack-perishable-fresh-years", Math.Round(years, 1)));
                                        }
                                    }
                                    /*else if (freshHoursLeft / hoursPerday >= api.World.Calendar.DaysPerMonth)  - confusing. 12 days per months and stuff..
                                    {
                                        dsc.AppendLine(Lang.Get("<font color=\"orange\">Perishable.</font> Fresh for {0} months", Math.Round(freshHoursLeft / hoursPerday / api.World.Calendar.DaysPerMonth, 1)));
                                    }*/
                                    else if (freshHoursLeft > hoursPerday)
                                    {
                                        dsc.AppendLine(Lang.Get("itemstack-perishable-fresh-days", Math.Round(freshHoursLeft / hoursPerday, 1)));
                                    }
                                    else
                                    {
                                        dsc.AppendLine(Lang.Get("itemstack-perishable-fresh-hours", Math.Round(freshHoursLeft, 1)));
                                    }
                                }
                            }
                            break;

                        case EnumTransitionType.Cure:
                            if (nowSpoiling) break;

                            if (transitionLevel > 0 || (freshHoursLeft <= 0 && perishRate > 0))
                            {
                                dsc.AppendLine(Lang.Get("itemstack-curable-curing", (int)Math.Round(transitionLevel * 100)));
                            }
                            else
                            {
                                double hoursPerday = api.World.Calendar.HoursPerDay;

                                if (freshHoursLeft > hoursPerday)
                                {
                                    dsc.AppendLine(Lang.Get("itemstack-curable-duration-days", Math.Round(freshHoursLeft / hoursPerday, 1)));
                                }
                                else
                                {
                                    if (perishRate == 0)
                                    {
                                        dsc.AppendLine(Lang.Get("itemstack-curable"));
                                    } else
                                    {
                                        dsc.AppendLine(Lang.Get("itemstack-curable-duration-hours", Math.Round(freshHoursLeft, 1)));
                                    }
                                    
                                }
                            }
                            break;
                        /*

                    case EnumTransitionType.Ferment:
                        if (transitionLevel > 0)
                        {
                            dsc.AppendLine(Lang.Get("<font color=\"olive\">Fermentable.</font> {0}% fermented", (int)Math.Round(transitionLevel * 100)));
                        }
                        else
                        {
                            double hoursPerday = api.World.Calendar.HoursPerDay;
                            if (freshHoursLeft > hoursPerday)
                            {
                                dsc.AppendLine(Lang.Get("<font color=\"olive\">Fermentable.</font> Duration: {0} days", Math.Round(freshHoursLeft / hoursPerday, 1)));
                            }
                            else
                            {
                                dsc.AppendLine(Lang.Get("<font color=\"olive\">Fermentable.</font> Duration: {0} hours", Math.Round(freshHoursLeft, 1)));
                            }
                        }
                        break;
                        */

                        case EnumTransitionType.Ripen:
                            if (nowSpoiling) break;

                            if (transitionLevel > 0 || (freshHoursLeft <= 0 && perishRate > 0))
                            {
                                dsc.AppendLine(Lang.Get("itemstack-ripenable-ripening", (int)Math.Round(transitionLevel * 100)));
                            }
                            else
                            {
                                double hoursPerday = api.World.Calendar.HoursPerDay;

                                if (freshHoursLeft > hoursPerday)
                                {
                                    dsc.AppendLine(Lang.Get("itemstack-ripenable-duration-days", Math.Round(freshHoursLeft / hoursPerday, 1)));
                                }
                                else
                                {
                                    if (perishRate == 0)
                                    {
                                        dsc.AppendLine(Lang.Get("itemstack-ripenable"));
                                    }
                                    else
                                    {
                                        dsc.AppendLine(Lang.Get("itemstack-ripenable-duration-hours", Math.Round(freshHoursLeft, 1)));
                                    }

                                }
                            }
                            break;

                        case EnumTransitionType.Dry:
                            if (nowSpoiling) break;

                            if (transitionLevel > 0)
                            {
                                dsc.AppendLine(Lang.Get("<font color=\"burlywood\">Dryable.</font> {0}% dried", (int)Math.Round(transitionLevel * 100)));
                            }
                            else
                            {
                                double hoursPerday = api.World.Calendar.HoursPerDay;
                                if (freshHoursLeft > hoursPerday)
                                {
                                    dsc.AppendLine(Lang.Get("<font color=\"burlywood\">Dryable.</font> Duration: {0} days", Math.Round(freshHoursLeft / hoursPerday, 1)));
                                }
                                else
                                {
                                    dsc.AppendLine(Lang.Get("<font color=\"burlywood\">Dryable.</font> Duration: {0} hours", Math.Round(freshHoursLeft, 1)));
                                }
                            }
                            break;




                    }
                }
            }

            return spoilState;
        }

        public virtual List<ItemStack> GetHandBookStacks(ICoreClientAPI capi)
        {
            if (Code == null) return null;

            bool inCreativeTab = CreativeInventoryTabs != null && CreativeInventoryTabs.Length > 0;
            bool inCreativeTabStack = CreativeInventoryStacks != null && CreativeInventoryStacks.Length > 0;
            bool explicitlyIncluded = Attributes?["handbook"]?["include"].AsBool() == true;
            bool explicitlyExcluded = Attributes?["handbook"]?["exclude"].AsBool() == true;

            if (explicitlyExcluded) return null;
            if (!explicitlyIncluded && !inCreativeTab && !inCreativeTabStack) return null;

            List<ItemStack> stacks = new List<ItemStack>();

            if (inCreativeTabStack)
            {
                for (int i = 0; i < CreativeInventoryStacks.Length; i++)
                {
                    for (int j = 0; j < CreativeInventoryStacks[i].Stacks.Length; j++)
                    {
                        ItemStack stack = CreativeInventoryStacks[i].Stacks[j].ResolvedItemstack;
                        stack.ResolveBlockOrItem(capi.World);

                        stack = stack.Clone();
                        stack.StackSize = stack.Collectible.MaxStackSize;

                        if (!stacks.Any((stack1) => stack1.Equals(stack)))
                        {
                            stacks.Add(stack);
                        }
                    }
                }
            }
            else
            {
                ItemStack stack = new ItemStack(this);
                //stack.StackSize = stack.Collectible.MaxStackSize; -wtf? what is this for?

                stacks.Add(stack);
            }

            return stacks;
        }

        

        /// <summary>
        /// Detailed information on this block/item to be displayed in the handbook
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="capi"></param>
        /// <param name="allStacks">An itemstack for every block and item that should be considered during information display</param>
        /// <param name="openDetailPageFor">Callback when someone clicks a displayed itemstack</param>
        /// <returns></returns>
        public virtual RichTextComponentBase[] GetHandbookInfo(ItemSlot inSlot, ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor)
        {
            ItemStack stack = inSlot.Itemstack;

            List<RichTextComponentBase> components = new List<RichTextComponentBase>();

            components.Add(new ItemstackTextComponent(capi, stack, 100, 10, EnumFloat.Left));
            components.Add(new RichTextComponent(capi, stack.GetName() + "\n", CairoFont.WhiteSmallishText()));

            components.AddRange(VtmlUtil.Richtextify(capi, stack.GetDescription(capi.World, inSlot), CairoFont.WhiteSmallText()));



            components.Add(new ClearFloatTextComponent(capi, 10));


            List<ItemStack> breakBlocks = new List<ItemStack>();

            //Dictionary<AssetLocation, ItemStack> breakBlocks = new Dictionary<AssetLocation, ItemStack>();

            foreach (var blockStack in allStacks)
            {
                if (blockStack.Block == null) continue;

                BlockDropItemStack[] droppedStacks = blockStack.Block.GetDropsForHandbook(blockStack, capi.World.Player);
                if (droppedStacks == null) continue;

                for (int i = 0; i < droppedStacks.Length; i++)
                {
                    ItemStack droppedStack = droppedStacks[i].ResolvedItemstack;

                    if (droppedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
                    {
                        //breakBlocks[val.Block.Code] = new ItemStack(val.Block); - why val.Block? It breaks lantern textures
                        breakBlocks.Add(blockStack);
                        //breakBlocks[blockStack.Block.Code] = droppedStack;
                    }
                }
            }




            if (stack.Class == EnumItemClass.Block)
            {
                BlockDropItemStack[] blockdropStacks = stack.Block.GetDropsForHandbook(stack, capi.World.Player);
                List<ItemStack> dropsStacks = new List<ItemStack>();
                foreach (var val in blockdropStacks)
                {
                    dropsStacks.Add(val.ResolvedItemstack);
                }

                if (dropsStacks != null && dropsStacks.Count > 0)
                {
                    if (dropsStacks.Count == 1 && breakBlocks.Count == 1 && breakBlocks[0].Equals(capi.World, dropsStacks[0], GlobalConstants.IgnoredStackAttributes))
                    {
                        // No need to display the same info twice
                    }
                    else
                    {
                        components.Add(new RichTextComponent(capi, Lang.Get("Drops when broken") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                        while (dropsStacks.Count > 0)
                        {
                            ItemStack dstack = dropsStacks[0];
                            dropsStacks.RemoveAt(0);
                            if (dstack == null) continue;

                            SlideshowItemstackTextComponent comp = new SlideshowItemstackTextComponent(capi, dstack, dropsStacks, 40, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                            components.Add(comp);
                        }

                        components.Add(new ClearFloatTextComponent(capi, 10));
                    }
                }
            }



            // Obtained through...
            // * Killing drifters
            // * From flax crops
            List<string> killCreatures = new List<string>();

            foreach (var val in capi.World.EntityTypes)
            {
                if (val.Drops == null) continue;

                for (int i = 0; i < val.Drops.Length; i++)
                {
                    if (val.Drops[i].ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
                    {
                        killCreatures.Add(Lang.Get(val.Code.Domain + ":item-creature-" + val.Code.Path));
                    }
                }
            }


            bool haveText = false;

            if (killCreatures.Count > 0)
            {
                components.Add(new RichTextComponent(capi, Lang.Get("Obtained by killing") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                components.Add(new RichTextComponent(capi, string.Join(", ", killCreatures) + "\n", CairoFont.WhiteSmallText()));
                haveText = true;
            }



            if (breakBlocks.Count > 0)
            {
                components.Add(new RichTextComponent(capi, (haveText ? "\n" : "") + Lang.Get("Obtained by breaking") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));

                while (breakBlocks.Count > 0)
                {
                    ItemStack dstack = breakBlocks[0];
                    breakBlocks.RemoveAt(0);
                    if (dstack == null) continue;

                    SlideshowItemstackTextComponent comp = new SlideshowItemstackTextComponent(capi, dstack, breakBlocks, 40, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                    components.Add(comp);
                }

                haveText = true;
            }


            // Found in....
            string customFoundIn = stack.Collectible.Attributes?["handbook"]?["foundIn"]?.AsString(null);
            if (customFoundIn != null)
            {
                components.Add(new RichTextComponent(capi, (haveText ? "\n" : "") + Lang.Get("Found in") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                components.Add(new RichTextComponent(capi, Lang.Get(customFoundIn), CairoFont.WhiteSmallText()));
                haveText = true;
            }


            if (Attributes?["hostRockFor"].Exists == true)
            {
                ushort[] blockids = Attributes?["hostRockFor"].AsArray<ushort>();

                OrderedDictionary<string, List<ItemStack>> blocks = new OrderedDictionary<string, List<ItemStack>>();

                for (int i = 0; i < blockids.Length; i++)
                {
                    Block block = api.World.Blocks[blockids[i]];

                    string key = block.Code.ToString();
                    if (block.Attributes?["handbook"]["groupBy"].Exists == true)
                    {
                        key = block.Attributes["handbook"]["groupBy"].AsArray<string>()[0];
                    }

                    if (!blocks.ContainsKey(key))
                    {
                        blocks[key] = new List<ItemStack>();
                    }

                    blocks[key].Add(new ItemStack(block));
                }

                components.Add(new RichTextComponent(capi, (haveText ? "\n" : "") + Lang.Get("Host rock for") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));

                foreach (var val in blocks)
                {
                    components.Add(new SlideshowItemstackTextComponent(capi, val.Value.ToArray(), 40, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
                }

                haveText = true;
            }


            if (Attributes?["hostRock"].Exists == true)
            {
                ushort[] blockids = Attributes?["hostRock"].AsArray<ushort>();

                OrderedDictionary<string, List<ItemStack>> blocks = new OrderedDictionary<string, List<ItemStack>>();

                for (int i = 0; i < blockids.Length; i++)
                {
                    Block block = api.World.Blocks[blockids[i]];

                    string key = block.Code.ToString();
                    if (block.Attributes?["handbook"]["groupBy"].Exists == true)
                    {
                        key = block.Attributes["handbook"]["groupBy"].AsArray<string>()[0];
                    }

                    if (!blocks.ContainsKey(key))
                    {
                        blocks[key] = new List<ItemStack>();
                    }

                    blocks[key].Add(new ItemStack(block));
                }

                components.Add(new RichTextComponent(capi, (haveText ? "\n" : "") + Lang.Get("Occurs in host rock") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));

                foreach (var val in blocks)
                {
                    components.Add(new SlideshowItemstackTextComponent(capi, val.Value.ToArray(), 40, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
                }

                haveText = true;
            }



            // Alloy for...


            Dictionary<AssetLocation, ItemStack> alloyables = new Dictionary<AssetLocation, ItemStack>();
            foreach (var val in capi.World.Alloys)
            {
                foreach (var ing in val.Ingredients)
                {
                    if (ing.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
                    {
                        alloyables[val.Output.ResolvedItemstack.Collectible.Code] = val.Output.ResolvedItemstack;
                    }
                }
            }

            if (alloyables.Count > 0)
            {
                components.Add(new RichTextComponent(capi, (haveText ? "\n" : "") + Lang.Get("Alloy for") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                foreach (var val in alloyables)
                {
                    components.Add(new ItemstackTextComponent(capi, val.Value, 40, 10, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
                }

                haveText = true;
            }

            // Bakes into
            if (Attributes?["bakingResultCode"].AsString() is string bakingResult)
            {
                string title = Lang.Get("smeltdesc-bake-title");
                components.Add(new RichTextComponent(capi, (haveText ? "\n" : "") + title + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                components.Add(new ItemstackTextComponent(capi, new ItemStack(capi.World.GetItem(new AssetLocation(bakingResult))), 40, 10, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
            }
            else
            // Smelts into
            if (CombustibleProps?.SmeltedStack?.ResolvedItemstack != null && !CombustibleProps.SmeltedStack.ResolvedItemstack.Equals(api.World, stack, GlobalConstants.IgnoredStackAttributes))
            {
                string smelttype = CombustibleProps.SmeltingType.ToString().ToLowerInvariant();
                string title = Lang.Get("game:smeltdesc-" + smelttype + "-title");
                    

                components.Add(new RichTextComponent(capi, (haveText ? "\n" : "") + title + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                components.Add(new ItemstackTextComponent(capi, CombustibleProps.SmeltedStack.ResolvedItemstack, 40, 10, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
                haveText = true;
            }

            // Pulverizes into
            if (CrushingProps?.CrushedStack?.ResolvedItemstack != null && !CrushingProps.CrushedStack.ResolvedItemstack.Equals(api.World, stack, GlobalConstants.IgnoredStackAttributes))
            {
                string title = Lang.Get("game:pulverizesdesc-title");

                components.Add(new RichTextComponent(capi, (haveText ? "\n" : "") + title + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                components.Add(new ItemstackTextComponent(capi, CrushingProps.CrushedStack.ResolvedItemstack, 40, 10, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
                haveText = true;
            }


            // Grinds into
            if (GrindingProps?.GroundStack?.ResolvedItemstack != null && !GrindingProps.GroundStack.ResolvedItemstack.Equals(api.World, stack, GlobalConstants.IgnoredStackAttributes))
            {
                components.Add(new RichTextComponent(capi, (haveText ? "\n" : "") + Lang.Get("Grinds into") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                components.Add(new ItemstackTextComponent(capi, GrindingProps.GroundStack.ResolvedItemstack, 40, 10, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
                haveText = true;
            }

            TransitionableProperties[] props = GetTransitionableProperties(api.World, stack, null);

            if (props != null)
            {
                foreach (var prop in props)
                {
                    switch (prop.Type)
                    {
                        case EnumTransitionType.Cure:
                            components.Add(new RichTextComponent(capi, (haveText ? "\n" : "") + Lang.Get("After {0} hours, cures into", prop.TransitionHours.avg) + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                            components.Add(new ItemstackTextComponent(capi, prop.TransitionedStack.ResolvedItemstack, 40, 10, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
                            break;

                        case EnumTransitionType.Ripen:
                            components.Add(new RichTextComponent(capi, (haveText ? "\n" : "") + Lang.Get("After {0} hours of open storage, ripens into", prop.TransitionHours.avg) + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                            components.Add(new ItemstackTextComponent(capi, prop.TransitionedStack.ResolvedItemstack, 40, 10, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
                            break;

                        case EnumTransitionType.Dry:
                            components.Add(new RichTextComponent(capi, (haveText ? "\n" : "") + Lang.Get("After {0} hours of open storage, dries into", prop.TransitionHours.avg) + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                            components.Add(new ItemstackTextComponent(capi, prop.TransitionedStack.ResolvedItemstack, 40, 10, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
                            break;

                        case EnumTransitionType.Convert:
                            break;

                        case EnumTransitionType.Perish:
                            break;

                    }
                }
            }


            // Alloyable from

            Dictionary<AssetLocation, MetalAlloyIngredient[]> alloyableFrom = new Dictionary<AssetLocation, MetalAlloyIngredient[]>();
            foreach (var val in capi.World.Alloys)
            {
                if (val.Output.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
                {
                    List<MetalAlloyIngredient> ingreds = new List<MetalAlloyIngredient>();
                    foreach (var ing in val.Ingredients) ingreds.Add(ing);
                    alloyableFrom[val.Output.ResolvedItemstack.Collectible.Code] = ingreds.ToArray();
                }
            }

            if (alloyableFrom.Count > 0)
            {
                components.Add(new RichTextComponent(capi, (haveText ? "\n" : "") + Lang.Get("Alloyed from") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                foreach (var val in alloyableFrom)
                {
                    foreach (var ingred in val.Value) {
                        string ratio = " " + Lang.Get("alloy-ratio-from-to", (int)(ingred.MinRatio * 100), (int)(ingred.MaxRatio * 100));
                        components.Add(new RichTextComponent(capi, ratio, CairoFont.WhiteSmallText()));
                        ItemstackComponentBase comp = new ItemstackTextComponent(capi, ingred.ResolvedItemstack, 30, 5, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                        comp.offY = 8;
                        components.Add(comp);
                    }

                    components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
                }

                components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));

                haveText = true;
            }

            // Ingredient for...
            // Pickaxe
            // Axe
            ItemStack maxstack = stack.Clone();
            maxstack.StackSize = maxstack.Collectible.MaxStackSize; // because SatisfiesAsIngredient() tests for stacksize

            List<ItemStack> recipestacks = new List<ItemStack>();

            foreach (var recval in capi.World.GridRecipes)
            {
                foreach (var val in recval.resolvedIngredients)
                {
                    CraftingRecipeIngredient ingred = val;

                    if (ingred != null && ingred.SatisfiesAsIngredient(maxstack) && !recipestacks.Any(s => s.Equals(capi.World, recval.Output.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
                    {
                        ItemStack outstack = recval.Output.ResolvedItemstack;
                        DummySlot outSlot = new DummySlot(outstack);

                        DummySlot[] inSlots = new DummySlot[recval.Width * recval.Height];
                        for (int x = 0; x < recval.Width; x++) {
                            for (int y = 0; y < recval.Height; y++)
                            {
                                CraftingRecipeIngredient inIngred = recval.GetElementInGrid(y, x, recval.resolvedIngredients, recval.Width);
                                ItemStack ingredStack = inIngred?.ResolvedItemstack?.Clone();
                                if (inIngred == val) ingredStack = maxstack;

                                inSlots[y * recval.Width + x] = new DummySlot(ingredStack);
                            }
                        }
                             

                        outstack.Collectible.OnCreatedByCrafting(inSlots, outSlot, recval);
                        recipestacks.Add(outSlot.Itemstack);
                    }
                }
                
            }


            foreach (var val in capi.World.SmithingRecipes)
            {
                if (val.Ingredient.SatisfiesAsIngredient(maxstack) && !recipestacks.Any(s => s.Equals(capi.World, val.Output.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
                {
                    recipestacks.Add(val.Output.ResolvedItemstack);
                }
            }


            foreach (var val in capi.World.ClayFormingRecipes)
            {
                if (val.Ingredient.SatisfiesAsIngredient(maxstack) && !recipestacks.Any(s => s.Equals(capi.World, val.Output.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
                {
                    recipestacks.Add(val.Output.ResolvedItemstack);
                }
            }


            foreach (var val in capi.World.KnappingRecipes)
            {
                if (val.Ingredient.SatisfiesAsIngredient(maxstack) && !recipestacks.Any(s => s.Equals(capi.World, val.Output.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
                {
                    recipestacks.Add(val.Output.ResolvedItemstack);
                }
            }


            foreach (var recipe in capi.World.BarrelRecipes)
            {
                foreach (var ingred in recipe.Ingredients)
                {
                    if (ingred.SatisfiesAsIngredient(maxstack) && !recipestacks.Any(s => s.Equals(capi.World, recipe.Output.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
                    {
                        recipestacks.Add(recipe.Output.ResolvedItemstack);
                    }
                }
            }



            if (recipestacks.Count > 0)
            {
                components.Add(new ClearFloatTextComponent(capi, 16));
                components.Add(new RichTextComponent(capi, Lang.Get("Ingredient for") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));

                while (recipestacks.Count > 0)
                {
                    ItemStack dstack = recipestacks[0];
                    recipestacks.RemoveAt(0);
                    if (dstack == null) continue;

                    SlideshowItemstackTextComponent comp = new SlideshowItemstackTextComponent(capi, dstack, recipestacks, 40, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                    components.Add(comp);
                }
            }




            // Created by....
            // * Smithing
            // * Grid crafting:
            //   x x x
            //   x x x
            //   x x x

            bool smithable = false;
            bool knappable = false;
            bool clayformable = false;
            
            foreach (var val in capi.World.SmithingRecipes)
            {
                if (val.Output.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
                {
                    smithable = true;
                    break;
                }
            }

            foreach (var val in capi.World.KnappingRecipes)
            {
                if (val.Output.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
                {
                    knappable = true;
                    break;
                }
            }


            foreach (var val in capi.World.ClayFormingRecipes)
            {
                if (val.Output.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
                {
                    clayformable = true;
                    break;
                }
            }


            List<GridRecipe> grecipes = new List<GridRecipe>();

            foreach (var val in capi.World.GridRecipes)
            {
                if (val.Output.ResolvedItemstack.Satisfies(stack))
                {
                    grecipes.Add(val);
                }
            }


            List<ItemStack> bakables = new List<ItemStack>();
            List<ItemStack> grindables = new List<ItemStack>();
            List<ItemStack> crushables = new List<ItemStack>();
            List<ItemStack> curables = new List<ItemStack>();
            List<ItemStack> ripenables = new List<ItemStack>();
            List<ItemStack> dryables = new List<ItemStack>();

            foreach (var val in allStacks)
            {
                ItemStack smeltedStack = val.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack;
                if (smeltedStack != null && smeltedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !bakables.Any(s => s.Equals(capi.World, smeltedStack, GlobalConstants.IgnoredStackAttributes)))
                {
                    bakables.Add(val);
                }

                ItemStack groundStack = val.Collectible.GrindingProps?.GroundStack.ResolvedItemstack;
                if (groundStack != null && groundStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !grindables.Any(s => s.Equals(capi.World, groundStack, GlobalConstants.IgnoredStackAttributes)))
                {
                    grindables.Add(val);
                }

                ItemStack crushedStack = val.Collectible.CrushingProps?.CrushedStack.ResolvedItemstack;
                if (crushedStack != null && crushedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !crushables.Any(s => s.Equals(capi.World, crushedStack, GlobalConstants.IgnoredStackAttributes)))
                {
                    crushables.Add(val);
                }

                TransitionableProperties[] oprops = val.Collectible.GetTransitionableProperties(api.World, val, null);
                if (oprops != null)
                {
                    foreach (var prop in oprops)
                    {
                        ItemStack transitionedStack = prop.TransitionedStack?.ResolvedItemstack;

                        switch (prop.Type)
                        {
                            case EnumTransitionType.Cure:
                                if (transitionedStack != null && transitionedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !curables.Any(s => s.Equals(capi.World, transitionedStack, GlobalConstants.IgnoredStackAttributes)))
                                {
                                    curables.Add(val);
                                }
                                break;

                            case EnumTransitionType.Ripen:
                                if (transitionedStack != null && transitionedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !curables.Any(s => s.Equals(capi.World, transitionedStack, GlobalConstants.IgnoredStackAttributes)))
                                {
                                    ripenables.Add(val);
                                }
                                break;


                            case EnumTransitionType.Dry:
                                if (transitionedStack != null && transitionedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !curables.Any(s => s.Equals(capi.World, transitionedStack, GlobalConstants.IgnoredStackAttributes)))
                                {
                                    dryables.Add(val);
                                }
                                break;

                            case EnumTransitionType.Convert:
                                break;

                            case EnumTransitionType.Perish:
                                break;

                        }
                    }
                }

            }


            List<RichTextComponentBase> barrelRecipestext = new List<RichTextComponentBase>();
            Dictionary<string, List<BarrelRecipe>> brecipesbyName = new Dictionary<string, List<BarrelRecipe>>();
            foreach (var recipe in capi.World.BarrelRecipes)
            {
                ItemStack mixdStack = recipe.Output.ResolvedItemstack;

                if (mixdStack != null && mixdStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
                {
                    List<BarrelRecipe> tmp;

                    if (!brecipesbyName.TryGetValue(recipe.Code, out tmp))
                    {
                        brecipesbyName[recipe.Code] = tmp = new List<BarrelRecipe>();
                    }

                    tmp.Add(recipe);
                }
            }

            

            foreach (var recipes in brecipesbyName.Values)
            {
                int ingredientsLen = recipes[0].Ingredients.Length;
                ItemStack[][] ingstacks = new ItemStack[ingredientsLen][];

                for (int i = 0; i < recipes.Count; i++)
                {
                    if (recipes[i].Ingredients.Length != ingredientsLen)
                    {
                        throw new Exception("Barrel recipe with same name but different ingredient count! Sorry, this is not supported right now. Please make sure you choose different barrel recipe names if you have different ingredient counts.");
                    }

                    

                    for (int j = 0; j < ingredientsLen; j++)
                    {
                        if (i == 0)
                        {
                            ingstacks[j] = new ItemStack[recipes.Count];
                        }

                        ingstacks[j][i] = recipes[i].Ingredients[j].ResolvedItemstack;
                    }
                }

                for (int i = 0; i < ingredientsLen; i++)
                {
                    if (i > 0)
                    {
                        RichTextComponent cmp = new RichTextComponent(capi, "+", CairoFont.WhiteMediumText());
                        cmp.VerticalAlign = EnumVerticalAlign.Middle;
                        barrelRecipestext.Add(cmp);
                    }

                    SlideshowItemstackTextComponent scmp = new SlideshowItemstackTextComponent(capi, ingstacks[i], 40, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                    scmp.ShowStackSize = true;
                    barrelRecipestext.Add(scmp);
                }


                barrelRecipestext.Add(new ClearFloatTextComponent(capi, 10));
            }





            string customCreatedBy = stack.Collectible.Attributes?["handbook"]?["createdBy"]?.AsString(null);
            string bakingInitialIngredient = Attributes?["bakingInitialCode"].AsString();

            if (grecipes.Count > 0 || smithable || knappable || clayformable || customCreatedBy != null || bakables.Count > 0 || barrelRecipestext.Count > 0 || grindables.Count > 0 || curables.Count > 0 || ripenables.Count > 0 || dryables.Count > 0 || crushables.Count > 0 || bakingInitialIngredient != null)
            {
                components.Add(new ClearFloatTextComponent(capi, 16));
                components.Add(new RichTextComponent(capi, Lang.Get("Created by") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));


                if (smithable)
                {
                    components.Add(new ClearFloatTextComponent(capi, 4));
                    components.Add(new RichTextComponent(capi, "• ", CairoFont.WhiteSmallText()));
                    components.Add(new LinkTextComponent(capi, Lang.Get("Smithing") + "\n", CairoFont.WhiteSmallText(), (cs) => { openDetailPageFor("craftinginfo-smithing"); }));
                }
                if (knappable)
                {
                    components.Add(new ClearFloatTextComponent(capi, 4));
                    components.Add(new RichTextComponent(capi, "• ", CairoFont.WhiteSmallText()));
                    components.Add(new LinkTextComponent(capi, Lang.Get("Knapping") + "\n", CairoFont.WhiteSmallText(), (cs) => { openDetailPageFor("craftinginfo-knapping"); }));
                }
                if (clayformable)
                {
                    components.Add(new ClearFloatTextComponent(capi, 4));
                    components.Add(new RichTextComponent(capi, "• ", CairoFont.WhiteSmallText()));
                    components.Add(new LinkTextComponent(capi, Lang.Get("Clay forming") + "\n", CairoFont.WhiteSmallText(), (cs) => { openDetailPageFor("craftinginfo-clayforming"); }));
                }
                if (customCreatedBy != null)
                {
                    components.Add(new ClearFloatTextComponent(capi, 4));
                    components.Add(new RichTextComponent(capi, "• ", CairoFont.WhiteSmallText()));
                    components.AddRange(VtmlUtil.Richtextify(capi, Lang.Get(customCreatedBy) + "\n", CairoFont.WhiteSmallText()));
                }

                if (grindables.Count > 0)
                {
                    components.Add(new ClearFloatTextComponent(capi, 4));
                    components.Add(new RichTextComponent(capi, "• " + Lang.Get("Grinding") + "\n", CairoFont.WhiteSmallText()));

                    while (grindables.Count > 0)
                    {
                        ItemStack dstack = grindables[0];
                        grindables.RemoveAt(0);
                        if (dstack == null) continue;

                        SlideshowItemstackTextComponent comp = new SlideshowItemstackTextComponent(capi, dstack, grindables, 40, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                        components.Add(comp);
                    }

                    components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                }

                if (crushables.Count > 0)
                {
                    components.Add(new ClearFloatTextComponent(capi, 4));
                    components.Add(new RichTextComponent(capi, "• " + Lang.Get("Crushing") + "\n", CairoFont.WhiteSmallText()));

                    while (crushables.Count > 0)
                    {
                        ItemStack dstack = crushables[0];
                        crushables.RemoveAt(0);
                        if (dstack == null) continue;

                        SlideshowItemstackTextComponent comp = new SlideshowItemstackTextComponent(capi, dstack, crushables, 40, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                        components.Add(comp);
                    }

                    components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                }


                if (curables.Count > 0)
                {
                    components.Add(new ClearFloatTextComponent(capi, 4));
                    components.Add(new RichTextComponent(capi, "• " + Lang.Get("Curing") + "\n", CairoFont.WhiteSmallText()));

                    while (curables.Count > 0)
                    {
                        ItemStack dstack = curables[0];
                        curables.RemoveAt(0);
                        if (dstack == null) continue;

                        SlideshowItemstackTextComponent comp = new SlideshowItemstackTextComponent(capi, dstack, curables, 40, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                        components.Add(comp);
                    }
                }



                if (ripenables.Count > 0)
                {
                    components.Add(new ClearFloatTextComponent(capi, 4));
                    components.Add(new RichTextComponent(capi, "• " + Lang.Get("Ripening") + "\n", CairoFont.WhiteSmallText()));

                    while (ripenables.Count > 0)
                    {
                        ItemStack dstack = ripenables[0];
                        ripenables.RemoveAt(0);
                        if (dstack == null) continue;

                        SlideshowItemstackTextComponent comp = new SlideshowItemstackTextComponent(capi, dstack, ripenables, 40, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                        components.Add(comp);
                    }

                    components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
                }

                if (dryables.Count > 0)
                {
                    components.Add(new ClearFloatTextComponent(capi, 4));
                    components.Add(new RichTextComponent(capi, "• " + Lang.Get("Drying") + "\n", CairoFont.WhiteSmallText()));

                    while (dryables.Count > 0)
                    {
                        ItemStack dstack = dryables[0];
                        dryables.RemoveAt(0);
                        if (dstack == null) continue;

                        SlideshowItemstackTextComponent comp = new SlideshowItemstackTextComponent(capi, dstack, dryables, 40, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                        components.Add(comp);
                    }

                    components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
                }


                if (bakables.Count > 0)
                {
                    components.Add(new ClearFloatTextComponent(capi, 4));
                    components.Add(new RichTextComponent(capi, "• " + Lang.Get("Cooking/Smelting/Baking") + "\n", CairoFont.WhiteSmallText()));

                    while (bakables.Count > 0)
                    {
                        ItemStack dstack = bakables[0];
                        bakables.RemoveAt(0);
                        if (dstack == null) continue;

                        SlideshowItemstackTextComponent comp = new SlideshowItemstackTextComponent(capi, dstack, bakables, 40, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                        components.Add(comp);
                    }

                    components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
                }


                if (bakingInitialIngredient != null)
                {
                    components.Add(new ClearFloatTextComponent(capi, 4));
                    components.Add(new RichTextComponent(capi, "• " + Lang.Get("Baking (in oven)") + "\n", CairoFont.WhiteSmallText()));
                    components.Add(new ItemstackTextComponent(capi, new ItemStack(capi.World.GetItem(new AssetLocation(bakingInitialIngredient))), 40, 10, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
                }


                if (grecipes.Count > 0)
                {
                    components.Add(new ClearFloatTextComponent(capi, 4));
                    /*if (knappable) - whats this for? o.O */
                    components.Add(new RichTextComponent(capi, "• " + Lang.Get("Crafting") + "\n", CairoFont.WhiteSmallText()));

                    Dictionary<int, List<GridRecipe>> grouped = new Dictionary<int, List<GridRecipe>>();
                    foreach (var recipe in grecipes)
                    {
                        List<GridRecipe> list;
                        if (!grouped.TryGetValue(recipe.RecipeGroup, out list))
                        {
                            grouped[recipe.RecipeGroup] = list = new List<GridRecipe>();
                        }
                        list.Add(recipe);
                    }

                    foreach (var val in grouped)
                    {
                        var comp = new SlideshowGridRecipeTextComponent(capi, val.Value.ToArray(), 40, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)), allStacks);
                        comp.PaddingLeft = 10;
                        components.Add(comp);
                    }
                    
                }

                if (barrelRecipestext.Count > 0)
                {
                    components.Add(new ClearFloatTextComponent(capi, 4));
                    components.Add(new RichTextComponent(capi, "• " + Lang.Get("In Barrel") + "\n", CairoFont.WhiteSmallText()));
                    components.AddRange(barrelRecipestext);
                }
            }

            JsonObject obj = stack.Collectible.Attributes?["handbook"]?["extraSections"];
            if (obj != null && obj.Exists)
            {
                ExtraSection[] sections = obj?.AsObject<ExtraSection[]>();
                for (int i = 0; i < sections.Length; i++)
                {
                    components.Add(new ClearFloatTextComponent(capi, 16));
                    components.Add(new RichTextComponent(capi, Lang.Get(sections[i].Title) + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));

                    components.AddRange(VtmlUtil.Richtextify(capi, Lang.Get(sections[i].Text) + "\n", CairoFont.WhiteSmallText()));
                }
            }

            string type = stack.Class.Name();
            string code = Code.ToShortString();
            string langExtraSectionTitle = Lang.GetMatchingIfExists(Code.Domain + ":" + type + "-handbooktitle-" + code);
            string langExtraSectionText = Lang.GetMatchingIfExists(Code.Domain + ":" + type + "-handbooktext-" + code);

            if (langExtraSectionTitle != null || langExtraSectionText != null)
            {
                components.Add(new ClearFloatTextComponent(capi, 16));
                if (langExtraSectionTitle != null)
                {
                    components.Add(new RichTextComponent(capi, langExtraSectionTitle + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                    components.Add(new ClearFloatTextComponent(capi, 4));
                }
                if (langExtraSectionText != null)
                {
                    components.AddRange(VtmlUtil.Richtextify(capi, langExtraSectionText + "\n", CairoFont.WhiteSmallText()));
                }
            }

            return components.ToArray();
        }


        class ExtraSection { public string Title=null; public string Text=null; }

        /// <summary>
        /// Should return true if the stack can be placed into given slot
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        public virtual bool CanBePlacedInto(ItemStack stack, ItemSlot slot)
        {
            return slot.StorageType == 0 || (slot.StorageType & GetStorageFlags(stack)) > 0;
        }

        /// <summary>
        /// Should return the max. number of items that can be placed from sourceStack into the sinkStack
        /// </summary>
        /// <param name="sinkStack"></param>
        /// <param name="sourceStack"></param>
        /// <returns></returns>
        public virtual int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority)
        {
            if (Equals(sinkStack, sourceStack, GlobalConstants.IgnoredStackAttributes) && sinkStack.StackSize < MaxStackSize)
            {
                return Math.Min(MaxStackSize - sinkStack.StackSize, sourceStack.StackSize);
            }

            return 0;
        }

        /// <summary>
        /// Is always called on the sink slots item
        /// </summary>
        /// <param name="op"></param>
        public virtual void TryMergeStacks(ItemStackMergeOperation op)
        {
            op.MovableQuantity = GetMergableQuantity(op.SinkSlot.Itemstack, op.SourceSlot.Itemstack, op.CurrentPriority);
            if (op.MovableQuantity == 0) return;
            if (!op.SinkSlot.CanTakeFrom(op.SourceSlot, op.CurrentPriority)) return;

            bool doTemperatureAveraging = false;
            bool doTransitionAveraging = false;

            op.MovedQuantity = GameMath.Min(op.SinkSlot.RemainingSlotSpace, op.MovableQuantity, op.RequestedQuantity);

            if (HasTemperature(op.SinkSlot.Itemstack) || HasTemperature(op.SourceSlot.Itemstack))
            {
                if (op.CurrentPriority < EnumMergePriority.DirectMerge)
                {
                    float tempDiff = Math.Abs(GetTemperature(op.World, op.SinkSlot.Itemstack) - GetTemperature(op.World, op.SourceSlot.Itemstack));
                    if (tempDiff > 30)
                    {
                        op.MovedQuantity = 0;
                        op.MovableQuantity = 0;
                        op.RequiredPriority = EnumMergePriority.DirectMerge;
                        return;
                    }
                }

                doTemperatureAveraging = true;
            }


            TransitionState[] sourceTransitionStates = UpdateAndGetTransitionStates(op.World, op.SourceSlot);
            TransitionState[] targetTransitionStates = UpdateAndGetTransitionStates(op.World, op.SinkSlot);
            Dictionary<EnumTransitionType, TransitionState> targetStatesByType=null;

            if (sourceTransitionStates != null)
            {
                bool canDirectStack = true;
                bool canAutoStack = true;

                
                if (targetTransitionStates == null)
                {
                    op.MovedQuantity = 0;
                    op.MovableQuantity = 0;
                    return;
                }

                targetStatesByType = new Dictionary<EnumTransitionType, TransitionState>();
                foreach (var state in targetTransitionStates) targetStatesByType[state.Props.Type] = state;


                foreach (var sourceState in sourceTransitionStates)
                {
                    TransitionState targetState = null;
                    if (!targetStatesByType.TryGetValue(sourceState.Props.Type, out targetState))
                    {
                        canAutoStack = false;
                        canDirectStack = false;
                        break;
                    }

                    if (Math.Abs(targetState.TransitionedHours - sourceState.TransitionedHours) > 4 && Math.Abs(targetState.TransitionedHours - sourceState.TransitionedHours) / sourceState.FreshHours > 0.03f)
                    {
                        canAutoStack = false;
                    }
                }

                if ((!canAutoStack && op.CurrentPriority < EnumMergePriority.DirectMerge))
                {
                    op.MovedQuantity = 0;
                    op.MovableQuantity = 0;
                    op.RequiredPriority = EnumMergePriority.DirectMerge;
                    return;
                }

                if (!canDirectStack)
                {
                    op.MovedQuantity = 0;
                    op.MovableQuantity = 0;
                    return;
                }

                doTransitionAveraging = true;
            }



            if (doTemperatureAveraging)
            {
                SetTemperature(
                    op.World,
                    op.SinkSlot.Itemstack,
                    (op.SinkSlot.StackSize * GetTemperature(op.World, op.SinkSlot.Itemstack) + op.MovedQuantity * GetTemperature(op.World, op.SourceSlot.Itemstack)) / (op.SinkSlot.StackSize + op.MovedQuantity)
                );
            }

            if (doTransitionAveraging && op.MovedQuantity > 0)
            {
                float t = (float)op.MovedQuantity / (op.MovedQuantity + op.SinkSlot.StackSize);

                foreach (var sourceState in sourceTransitionStates)
                {
                    TransitionState targetState = targetStatesByType[sourceState.Props.Type];
                    SetTransitionState(op.SinkSlot.Itemstack, sourceState.Props.Type, sourceState.TransitionedHours * t + targetState.TransitionedHours * (1-t));
                }
            }

            op.SinkSlot.Itemstack.StackSize += op.MovedQuantity;
            op.SourceSlot.Itemstack.StackSize -= op.MovedQuantity;

            if (op.SourceSlot.Itemstack.StackSize <= 0)
            {
                op.SourceSlot.Itemstack = null;
            }
        }

        


        /// <summary>
        /// If the item is smeltable, this is the time it takes to smelt at smelting point
        /// </summary>
        /// <param name="world"></param>
        /// <param name="cookingSlotsProvider"></param>
        /// <param name="inputSlot"></param>
        /// <returns></returns>
        public virtual float GetMeltingDuration(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
        {
            return CombustibleProps == null ? 0 : CombustibleProps.MeltingDuration;
        }

        /// <summary>
        /// If the item is smeltable, this is its melting point
        /// </summary>
        /// <param name="world"></param>
        /// <param name="cookingSlotsProvider"></param>
        /// <param name="inputSlot"></param>
        /// <returns></returns>
        public virtual float GetMeltingPoint(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
        {
            return CombustibleProps == null ? 0 : CombustibleProps.MeltingPoint;
        }


        /// <summary>
        /// Should return true if this collectible is smeltable
        /// </summary>
        /// <param name="world"></param>
        /// <param name="cookingSlotsProvider"></param>
        /// <param name="inputStack"></param>
        /// <param name="outputStack"></param>
        /// <returns></returns>
        public virtual bool CanSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack, ItemStack outputStack)
        {
            ItemStack smeltedStack = CombustibleProps?.SmeltedStack?.ResolvedItemstack;

            return
                smeltedStack != null
                && inputStack.StackSize >= CombustibleProps.SmeltedRatio
                && CombustibleProps.MeltingPoint > 0
                && (outputStack == null || outputStack.Collectible.GetMergableQuantity(outputStack, smeltedStack, EnumMergePriority.AutoMerge) >= smeltedStack.StackSize)
            ;
        }

        /// <summary>
        /// Transform the item to it's smelted variant
        /// </summary>
        /// <param name="world"></param>
        /// <param name="cookingSlotsProvider"></param>
        /// <param name="inputSlot"></param>
        /// <param name="outputSlot"></param>
        public virtual void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
        {
            if (!CanSmelt(world, cookingSlotsProvider, inputSlot.Itemstack, outputSlot.Itemstack)) return;

            ItemStack smeltedStack = CombustibleProps.SmeltedStack.ResolvedItemstack.Clone();

            // Copy over spoilage values but reduce them by a bit
            TransitionState state = UpdateAndGetTransitionState(world, new DummySlot(inputSlot.Itemstack), EnumTransitionType.Perish);

            if (state != null)
            {
                TransitionState smeltedState = smeltedStack.Collectible.UpdateAndGetTransitionState(world, new DummySlot(smeltedStack), EnumTransitionType.Perish);

                float nowTransitionedHours = (state.TransitionedHours / (state.TransitionHours + state.FreshHours)) * 0.8f * (smeltedState.TransitionHours + smeltedState.FreshHours) - 1;

                smeltedStack.Collectible.SetTransitionState(smeltedStack, EnumTransitionType.Perish, Math.Max(0, nowTransitionedHours));
            }

            int batchSize = 1;

            if (outputSlot.Itemstack == null)
            {
                outputSlot.Itemstack = smeltedStack;
                outputSlot.Itemstack.StackSize = batchSize * smeltedStack.StackSize;
            }
            else
            {
                smeltedStack.StackSize = batchSize * smeltedStack.StackSize;

                // use TryMergeStacks to average spoilage rate and temperature
                ItemStackMergeOperation op = new ItemStackMergeOperation(world, EnumMouseButton.Left, 0, EnumMergePriority.ConfirmedMerge, batchSize * smeltedStack.StackSize);
                op.SourceSlot = new DummySlot(smeltedStack);
                op.SinkSlot = new DummySlot(outputSlot.Itemstack);
                outputSlot.Itemstack.Collectible.TryMergeStacks(op);
                outputSlot.Itemstack = op.SinkSlot.Itemstack;
            }

            inputSlot.Itemstack.StackSize -= batchSize * CombustibleProps.SmeltedRatio;

            if (inputSlot.Itemstack.StackSize <= 0)
            {
                inputSlot.Itemstack = null;
            }

            outputSlot.MarkDirty();
        }

        /// <summary>
        /// Returns true if the stack can spoil
        /// </summary>
        /// <param name="itemstack"></param>
        /// <returns></returns>
        public virtual bool CanSpoil(ItemStack itemstack)
        {
            if (itemstack == null || itemstack.Attributes == null) return false;
            return itemstack.Collectible.NutritionProps != null && itemstack.Attributes.HasAttribute("spoilstate");
        }


        /// <summary>
        /// Returns the transition state of given transition type
        /// </summary>
        /// <param name="world"></param>
        /// <param name="inslot"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual TransitionState UpdateAndGetTransitionState(IWorldAccessor world, ItemSlot inslot, EnumTransitionType type)
        {
            TransitionState[] states = UpdateAndGetTransitionStates(world, inslot);
            TransitionableProperties[] propsm = GetTransitionableProperties(world, inslot.Itemstack, null);
            if (propsm == null) return null;

            for (int i = 0; i < propsm.Length; i++)
            {
                if (propsm[i].Type == type) return states[i];
            }

            return null;
        }


        public virtual void SetTransitionState(ItemStack stack, EnumTransitionType type, float transitionedHours)
        {
            ITreeAttribute attr = (ITreeAttribute)stack.Attributes["transitionstate"];

            if (attr == null)
            {
                UpdateAndGetTransitionState(api.World, new DummySlot(stack), type);
                attr = (ITreeAttribute)stack.Attributes["transitionstate"];
            }

            TransitionableProperties[] propsm = GetTransitionableProperties(api.World, stack, null);
            for (int i = 0; i < propsm.Length; i++)
            {
                if (propsm[i].Type == type)
                {        
                    (attr["transitionedHours"] as FloatArrayAttribute).value[i] = transitionedHours;
                    return;
                }
            }
        }
        

        public virtual float GetTransitionRateMul(IWorldAccessor world, ItemSlot inSlot, EnumTransitionType transType)
        {
            float rate = inSlot.Inventory == null ? 1 : inSlot.Inventory.GetTransitionSpeedMul(transType, inSlot.Itemstack);

            if (transType == EnumTransitionType.Perish)
            {
                float temp = inSlot.Itemstack.Collectible.GetTemperature(world, inSlot.Itemstack);
                if (temp > 75)
                {
                    rate = 0;
                }

                rate *= GlobalConstants.PerishSpeedModifier;
            }

            return rate;
        }

        /// <summary>
        /// Returns a list of the current transition states of this item
        /// </summary>
        /// <param name="world"></param>
        /// <param name="inslot"></param>
        /// <returns></returns>
        public virtual TransitionState[] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
        {
            ItemStack itemstack = inslot.Itemstack;

            TransitionableProperties[] propsm = GetTransitionableProperties(world, inslot.Itemstack, null);

            if (itemstack == null || propsm == null || propsm.Length == 0)
            {
                return null;
            }

            if (itemstack.Attributes == null)
            {
                itemstack.Attributes = new TreeAttribute();
            }

            if (!(itemstack.Attributes["transitionstate"] is ITreeAttribute))
            {
                itemstack.Attributes["transitionstate"] = new TreeAttribute();
            }

            ITreeAttribute attr = (ITreeAttribute)itemstack.Attributes["transitionstate"];


            float[] transitionedHours;
            float[] freshHours;
            float[] transitionHours;
            TransitionState[] states = new TransitionState[propsm.Length];

            if (!attr.HasAttribute("createdTotalHours"))
            {
                attr.SetDouble("createdTotalHours", world.Calendar.TotalHours);
                attr.SetDouble("lastUpdatedTotalHours", world.Calendar.TotalHours);

                freshHours = new float[propsm.Length];
                transitionHours = new float[propsm.Length];
                transitionedHours = new float[propsm.Length];

                for (int i = 0; i < propsm.Length; i++)
                {
                    transitionedHours[i] = 0;
                    freshHours[i] = propsm[i].FreshHours.nextFloat(1, world.Rand);
                    transitionHours[i] = propsm[i].TransitionHours.nextFloat(1, world.Rand);
                }

                attr["freshHours"] = new FloatArrayAttribute(freshHours);
                attr["transitionHours"] = new FloatArrayAttribute(transitionHours);
                attr["transitionedHours"] = new FloatArrayAttribute(transitionedHours);
            } else
            {
                freshHours = (attr["freshHours"] as FloatArrayAttribute).value;
                transitionHours = (attr["transitionHours"] as FloatArrayAttribute).value;
                transitionedHours = (attr["transitionedHours"] as FloatArrayAttribute).value;
            }

            double lastUpdatedTotalHours = attr.GetDouble("lastUpdatedTotalHours");
            double nowTotalHours = world.Calendar.TotalHours;


            bool nowSpoiling = false;
         
            float hoursPassed = (float)(nowTotalHours - lastUpdatedTotalHours);

            for (int i = 0; i < propsm.Length; i++)
            {
                TransitionableProperties prop = propsm[i];
                if (prop == null) continue;

                float transitionRateMul = GetTransitionRateMul(world, inslot, prop.Type);

                if (hoursPassed > 0.05f) // Maybe prevents us from running into accumulating rounding errors?
                {
                    float hoursPassedAdjusted = hoursPassed * transitionRateMul;
                    transitionedHours[i] += hoursPassedAdjusted;

                    /*if (api.World.Side == EnumAppSide.Server && inslot.Inventory.ClassName == "chest")
                    {
                        Console.WriteLine(hoursPassed + " hours passed. " + inslot.Itemstack.Collectible.Code + " spoil by " + transitionRateMul + "x. Is inside " + inslot.Inventory.ClassName + " {0}/{1}", transitionedHours[i], freshHours[i]);
                    }*/
                }

                float freshHoursLeft = Math.Max(0, freshHours[i] - transitionedHours[i]);
                float transitionLevel = Math.Max(0, transitionedHours[i] - freshHours[i]) / transitionHours[i];
                
                // Don't continue transitioning spoiled foods
                if (transitionLevel > 0)
                {
                    if (prop.Type == EnumTransitionType.Perish)
                    {
                        nowSpoiling = true;
                    } else
                    {
                        if (nowSpoiling) continue;
                    }
                }

                if (transitionLevel >= 1 && world.Side == EnumAppSide.Server)
                {
                    ItemStack newstack = OnTransitionNow(inslot, itemstack.Collectible.TransitionableProps[i]);

                    if (newstack.StackSize <= 0)
                    {
                        inslot.Itemstack = null;
                    } else {
                        itemstack.SetFrom(newstack);
                    }

                    inslot.MarkDirty();

                    // Only do one transformation, then do the next one next update
                    // This does fully not respect time-fast-forward, so that should be fixed some day
                    break;
                }

                states[i] = new TransitionState()
                {
                    FreshHoursLeft = freshHoursLeft,
                    TransitionLevel = Math.Min(1, transitionLevel),
                    TransitionedHours = transitionedHours[i],
                    TransitionHours = transitionHours[i],
                    FreshHours = freshHours[i],
                    Props = prop
                };

                //if (transitionRateMul > 0) break; // Only do one transformation at the time (i.e. food can not cure and perish at the same time) - Tyron 9/oct 2020, but why not at the same time? We need it for cheese ripening
            }

            if (hoursPassed > 0.05f)
            {
                attr.SetDouble("lastUpdatedTotalHours", nowTotalHours);
            }

            return states.Where(s => s != null).OrderBy(s => (int)s.Props.Type).ToArray();
        }


        /// <summary>
        /// Called when any of its TransitionableProperties causes the stack to transition to another stack. Default behavior is to return props.TransitionedStack.ResolvedItemstack and set the stack size according to the transition rtio
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="props"></param>
        /// <returns>The stack it should transition into</returns>
        public virtual ItemStack OnTransitionNow(ItemSlot slot, TransitionableProperties props)
        {
            ItemStack newStack = props.TransitionedStack.ResolvedItemstack.Clone();
            newStack.StackSize = GameMath.RoundRandom(api.World.Rand, slot.Itemstack.StackSize * props.TransitionRatio);
            return newStack;
        }

        public static void CarryOverFreshness(ICoreAPI api, ItemSlot[] inputSlots, ItemStack[] outStacks, TransitionableProperties perishProps)
        {
            float transitionedHoursRelative = 0;

            float spoilageRelMax = 0;
            float spoilageRel = 0;
            int quantity = 0;

            for (int i = 0; i < inputSlots.Length; i++)
            {
                ItemSlot slot = inputSlots[i];
                if (slot.Empty) continue;
                TransitionState state = slot.Itemstack.Collectible.UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish);
                if (state == null) continue;

                quantity++;
                float val = state.TransitionedHours / (state.TransitionHours + state.FreshHours);

                float spoilageRelOne = Math.Max(0, (state.TransitionedHours - state.FreshHours) / state.TransitionHours);
                spoilageRelMax = Math.Max(spoilageRelOne, spoilageRelMax);

                transitionedHoursRelative += val;
                spoilageRel += spoilageRelOne;
            }

            transitionedHoursRelative /= Math.Max(1, quantity);
            spoilageRel /= Math.Max(1, quantity);

            for (int i = 0; i < outStacks.Length; i++)
            {
                if (!(outStacks[i].Attributes["transitionstate"] is ITreeAttribute))
                {
                    outStacks[i].Attributes["transitionstate"] = new TreeAttribute();
                }

                float transitionHours = perishProps.TransitionHours.nextFloat(1, api.World.Rand);
                float freshHours = perishProps.FreshHours.nextFloat(1, api.World.Rand);

                ITreeAttribute attr = (ITreeAttribute)outStacks[i].Attributes["transitionstate"];
                attr.SetDouble("createdTotalHours", api.World.Calendar.TotalHours);
                attr.SetDouble("lastUpdatedTotalHours", api.World.Calendar.TotalHours);

                attr["freshHours"] = new FloatArrayAttribute(new float[] { freshHours });
                attr["transitionHours"] = new FloatArrayAttribute(new float[] { transitionHours });

                if (spoilageRel > 0)
                {
                    // If already spoiled: Take away 40% spoilage and 2 hours
                    spoilageRel *= 0.6f;
                    attr["transitionedHours"] = new FloatArrayAttribute(new float[] { freshHours + Math.Max(0, transitionHours * spoilageRel - 2)  });

                } else
                {
                    // If not yet spoiled: Weird formula :D
                    attr["transitionedHours"] = new FloatArrayAttribute(new float[] { Math.Max(0, transitionedHoursRelative * (0.2f + (2 + quantity) * spoilageRelMax) * (transitionHours + freshHours)) });
                }

                
            }
        }

        /// <summary>
        /// Test is failed for Perish-able items which have less than 50% of their fresh state remaining (or are already starting to spoil)
        /// </summary>
        /// <param name="world"></param>
        /// <param name="itemstack"></param>
        /// <returns></returns>
        public virtual bool IsReasonablyFresh(IWorldAccessor world, ItemStack itemstack)
        {
            if (itemstack == null) return true;
            TransitionableProperties[] propsm = GetTransitionableProperties(world, itemstack, null);
            if (propsm == null) return true;
            ITreeAttribute attr = (ITreeAttribute)itemstack.Attributes["transitionstate"];
            if (attr == null) return true;
            float[] freshHours = new float[propsm.Length];
            float[] transitionedHours = new float[propsm.Length];
            freshHours = (attr["freshHours"] as FloatArrayAttribute).value;
            transitionedHours = (attr["transitionedHours"] as FloatArrayAttribute).value;
            for (int i = 0; i < propsm.Length; i++)
            {
                TransitionableProperties prop = propsm[i];
                if (prop?.Type == EnumTransitionType.Perish)
                {
                    if (transitionedHours[i] > freshHours[i] / 2f) return false;
                }
            }
            return true;
        }




        /// <summary>
        /// Returns true if the stack has a temperature attribute
        /// </summary>
        /// <param name="itemstack"></param>
        /// <returns></returns>
        public virtual bool HasTemperature(IItemStack itemstack)
        {
            if (itemstack == null || itemstack.Attributes == null) return false;
            return itemstack.Attributes.HasAttribute("temperature");
        }

        /// <summary>
        /// Returns the stacks item temperature in degree celsius
        /// </summary>
        /// <param name="world"></param>
        /// <param name="itemstack"></param>
        /// <returns></returns>
        public virtual float GetTemperature(IWorldAccessor world, ItemStack itemstack)
        {
            if (
                itemstack == null
                || itemstack.Attributes == null
                || itemstack.Attributes["temperature"] == null
                || !(itemstack.Attributes["temperature"] is ITreeAttribute)
            )
            {
                return 20;
            }

            ITreeAttribute attr = ((ITreeAttribute)itemstack.Attributes["temperature"]);

            double nowHours = world.Calendar.TotalHours;
            double lastUpdateHours = attr.GetDouble("temperatureLastUpdate");

            double hourDiff = nowHours - lastUpdateHours;

            // 1.5 deg per irl second
            // 1 game hour = irl 60 seconds
            if (hourDiff > 1/85f)
            {
                float temp = Math.Max(0, attr.GetFloat("temperature", 20) - Math.Max(0, (float)(nowHours - lastUpdateHours) * attr.GetFloat("cooldownSpeed", 90)));
                SetTemperature(world, itemstack, temp);
                return temp;
            }

            return attr.GetFloat("temperature", 20);
        }

        /// <summary>
        /// Sets the stacks item temperature in degree celsius
        /// </summary>
        /// <param name="world"></param>
        /// <param name="itemstack"></param>
        /// <param name="temperature"></param>
        /// <param name="delayCooldown"></param>
        public virtual void SetTemperature(IWorldAccessor world, ItemStack itemstack, float temperature, bool delayCooldown = true)
        {
            if (itemstack == null) return;

            ITreeAttribute attr = ((ITreeAttribute)itemstack.Attributes["temperature"]);

            if (attr == null)
            {
                itemstack.Attributes["temperature"] = attr = new TreeAttribute();
            }

            double nowHours = world.Calendar.TotalHours;
            // If the colletible gets heated, retain the heat for 1 ingame hour
            if (delayCooldown && attr.GetFloat("temperature") < temperature) nowHours += 0.5f;

            attr.SetDouble("temperatureLastUpdate", nowHours);
            attr.SetFloat("temperature", temperature);
        }


        


        /// <summary>
        /// Returns true if this stack is an empty backpack
        /// </summary>
        /// <param name="itemstack"></param>
        /// <returns></returns>
        public static bool IsEmptyBackPack(IItemStack itemstack)
        {
            if (!IsBackPack(itemstack)) return false;

            ITreeAttribute backPackTree = itemstack.Attributes.GetTreeAttribute("backpack");
            if (backPackTree == null) return true;
            ITreeAttribute slotsTree = backPackTree.GetTreeAttribute("slots");

            foreach (var val in slotsTree)
            {
                IItemStack stack = (IItemStack)val.Value?.GetValue();
                if (stack != null && stack.StackSize > 0) return false;
            }
            return true;
        }


        /// <summary>
        /// Returns true if this stack is a backpack that can hold other items/blocks
        /// </summary>
        /// <param name="itemstack"></param>
        /// <returns></returns>
        public static bool IsBackPack(IItemStack itemstack)
        {
            if (itemstack == null || itemstack.Collectible.Attributes == null) return false;
            return itemstack.Collectible.Attributes["backpack"]["quantitySlots"].AsInt() > 0;
        }

        /// <summary>
        /// If the stack is a backpack, this returns the amount of slots it has
        /// </summary>
        /// <param name="itemstack"></param>
        /// <returns></returns>
        public static int QuantityBackPackSlots(IItemStack itemstack)
        {
            if (itemstack == null || itemstack.Collectible.Attributes == null) return 0;
            return itemstack.Collectible.Attributes["backpack"]["quantitySlots"].AsInt();
        }

        /// <summary>
        /// Should return true if given stacks are equal, ignoring their stack size.
        /// </summary>
        /// <param name="thisStack"></param>
        /// <param name="otherStack"></param>
        /// <param name="ignoreAttributeSubTrees"></param>
        /// <returns></returns>
        public virtual bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
        {
            return 
                thisStack.Class == otherStack.Class &&
                thisStack.Id == otherStack.Id &&
                thisStack.Attributes.Equals(api.World, otherStack.Attributes, ignoreAttributeSubTrees)
            ;
        }

        /// <summary>
        /// Should return true if thisStack is a satisfactory replacement of otherStack. It's bascially an Equals() test, but it ignores any additional attributes that exist in otherStack
        /// </summary>
        /// <param name="thisStack"></param>
        /// <param name="otherStack"></param>
        /// <returns></returns>
        public virtual bool Satisfies(ItemStack thisStack, ItemStack otherStack)
        {
            return
                thisStack.Class == otherStack.Class &&
                thisStack.Id == otherStack.Id &&
                thisStack.Attributes.IsSubSetOf(api.World, otherStack.Attributes)
            ;
        }

        /// <summary>
        /// This method is for example called by chests when they are being exported as part of a block schematic. Has to store all the currents block/item id mappings so it can be correctly imported again. By default it puts itself into the mapping and searches the itemstack attributes for attributes of type ItemStackAttribute and adds those to the mapping as well.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="inSlot"></param>
        /// <param name="blockIdMapping"></param>
        /// <param name="itemIdMapping"></param>
        public virtual void OnStoreCollectibleMappings(IWorldAccessor world, ItemSlot inSlot, Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
        {
            if (this is Item)
            {
                itemIdMapping[Id] = Code;
            }
            else
            {
                blockIdMapping[Id] = Code;
            }

            OnStoreCollectibleMappings(world, inSlot.Itemstack.Attributes, blockIdMapping, itemIdMapping);
        }

        /// <summary>
        /// This method is called after a block/item like this has been imported as part of a block schematic. Has to restore fix the block/item id mappings as they are probably different compared to the world from where they were exported. By default iterates over all the itemstacks attributes and searches for attribute sof type ItenStackAttribute and calls .FixMapping() on them.
        /// </summary>
        /// <param name="worldForResolve"></param>
        /// <param name="inSlot"></param>
        /// <param name="oldBlockIdMapping"></param>
        /// <param name="oldItemIdMapping"></param>
        public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, ItemSlot inSlot, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping)
        {
            OnLoadCollectibleMappings(worldForResolve, inSlot.Itemstack.Attributes, oldBlockIdMapping, oldItemIdMapping);
        }

        private void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, ITreeAttribute tree, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping)
        {
            foreach (var val in tree)
            {
                if (val.Value is ITreeAttribute)
                {
                    OnLoadCollectibleMappings(worldForResolve, val.Value as ITreeAttribute, oldBlockIdMapping, oldItemIdMapping);
                    continue;
                }

                if (val.Value is ItemstackAttribute)
                {
                    ItemStack stack = (val.Value as ItemstackAttribute).value;
                    stack?.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve);
                }
            }
        }

        void OnStoreCollectibleMappings(IWorldAccessor world, ITreeAttribute tree, Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
        {
            foreach (var val in tree)
            {
                if (val.Value is ITreeAttribute)
                {
                    OnStoreCollectibleMappings(world, val.Value as ITreeAttribute, blockIdMapping, itemIdMapping);
                    continue;
                }

                if (val.Value is ItemstackAttribute)
                {
                    ItemStack stack = (val.Value as ItemstackAttribute).value;
                    if (stack == null) continue;

                    if (stack.Collectible == null) stack.ResolveBlockOrItem(world);

                    if (stack.Class == EnumItemClass.Item)
                    {
                        itemIdMapping[stack.Id] = stack.Collectible.Code;
                    } else
                    {
                        blockIdMapping[stack.Id] = stack.Collectible.Code;
                    }
                }
            }

        }


        /// <summary>
        /// Should return a random pixel within the items/blocks texture
        /// </summary>
        /// <param name="capi"></param>
        /// <param name="stack"></param>
        /// <returns></returns>
        public virtual int GetRandomColor(ICoreClientAPI capi, ItemStack stack)
        {
            return 0;
        }



        /// <summary>
        /// Returns true if this blocks matterstate is liquid.
        /// NOTE: Liquid blocks should also implement IBlockFlowing
        /// </summary>
        /// <returns></returns>
        public virtual bool IsLiquid()
        {
            return MatterState == EnumMatterState.Liquid;
        }


        void WalkBehaviors(CollectibleBehaviorDelegate onBehavior, Action defaultAction)
        {
            bool executeDefault = true;
            foreach (CollectibleBehavior behavior in CollectibleBehaviors)
            {
                EnumHandling handling = EnumHandling.PassThrough;
                onBehavior(behavior, ref handling);

                if (handling == EnumHandling.PreventSubsequent) return;
                if (handling == EnumHandling.PreventDefault) executeDefault = false;
            }

            if (executeDefault) defaultAction();
        }







        /// <summary>
        /// Returns the blocks behavior of given type, if it has such behavior
        /// </summary>
        /// <param name="type"></param>
        /// <param name="withInheritance"></param>
        /// <returns></returns>
        public CollectibleBehavior GetCollectibleBehavior(Type type, bool withInheritance)
        {
            return GetBehavior(CollectibleBehaviors, type, withInheritance);
        }

        protected virtual CollectibleBehavior GetBehavior(CollectibleBehavior[] fromList, Type type, bool withInheritance)
        {
            if (withInheritance)
            {
                for (int i = 0; i < fromList.Length; i++)
                {
                    Type testType = fromList[i].GetType();
                    if (testType == type || type.IsAssignableFrom(testType))
                    {
                        return fromList[i];
                    }
                }
                return null;
            }

            // simpler loop if withInheritance is false
            for (int i = 0; i < fromList.Length; i++)
            {
                if (fromList[i].GetType() == type)
                {
                    return fromList[i];
                }
            }
            return null;
        }


        /// <summary>
        /// Returns true if the block has given behavior
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="withInheritance"></param>
        /// <returns></returns>
        public virtual bool HasBehavior<T>(bool withInheritance = false) where T : CollectibleBehavior
        {
            return (T)GetCollectibleBehavior(typeof(T), withInheritance) != null;
        }


        /// <summary>
        /// Returns true if the block has given behavior
        /// </summary>
        /// <param name="type"></param>
        /// <param name="withInheritance"></param>
        /// <returns></returns>
        public virtual bool HasBehavior(Type type, bool withInheritance = false)
        {
            return GetCollectibleBehavior(type, withInheritance) != null;
        }



        /// <summary>
        /// Returns true if the block has given behavior
        /// </summary>
        /// <param name="type"></param>
        /// <param name="classRegistry"></param>
        /// <returns></returns>
        public virtual bool HasBehavior(string type, IClassRegistryAPI classRegistry)
        {
            return GetBehavior(classRegistry.GetBlockBehaviorClass(type)) != null;
        }


        /// <summary>
        /// Returns the blocks behavior of given type, if it has such behavior
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public CollectibleBehavior GetBehavior(Type type)
        {
            return GetCollectibleBehavior(type, false);
        }

        /// <summary>
        /// Returns the blocks behavior of given type, if it has such behavior
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetBehavior<T>() where T : CollectibleBehavior
        {
            return (T)GetCollectibleBehavior(typeof(T), false);
        }


    }
}
