package dev.skycraft.combat;

import dev.skycraft.SkyCraft;
import dev.skycraft.link.SkyLink;
import dev.skycraft.net.SkyNet;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Map;
import java.util.Map.Entry;
import net.fabricmc.fabric.api.event.lifecycle.v1.ServerTickEvents;
import net.fabricmc.fabric.api.object.builder.v1.entity.FabricDefaultAttributeRegistry;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Registry;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.core.registries.Registries;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.Identifier;
import net.minecraft.resources.ResourceKey;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.damagesource.DamageSource;
import net.minecraft.world.damagesource.DamageSources;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.EntityType;
import net.minecraft.world.entity.EquipmentSlot;
import net.minecraft.world.entity.InsideBlockEffectApplier;
import net.minecraft.world.entity.LivingEntity;
import net.minecraft.world.entity.MobCategory;
import net.minecraft.world.entity.EntityType.Builder;
import net.minecraft.world.entity.item.ItemEntity;
import net.minecraft.world.entity.projectile.arrow.AbstractArrow;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.level.block.BasePressurePlateBlock;
import net.minecraft.world.level.block.TripWireBlock;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.phys.AABB;
import org.jspecify.annotations.Nullable;

public final class SkyCombat {
   public static final ResourceKey<EntityType<?>> SKYRIM_ACTOR_KEY = ResourceKey.create(
      Registries.ENTITY_TYPE, Identifier.fromNamespaceAndPath("skycraft", "skyrim_actor")
   );
   public static final EntityType<SkyrimActorEntity> SKYRIM_ACTOR = Registry.register(
      BuiltInRegistries.ENTITY_TYPE,
      SKYRIM_ACTOR_KEY,
      Builder.of(SkyrimActorEntity::new, MobCategory.MISC)
         .sized(0.6F, 1.8F)
         .noSave()
         .noSummon()
         .noLootTable()
         .clientTrackingRange(10)
         .updateInterval(1)
         .build(SKYRIM_ACTOR_KEY)
   );
   public static final float SKYRIM_TO_MC_DAMAGE = 5.0F;
   private static final Map<Integer, SkyrimActorEntity> PROXIES = new HashMap<>();
   private static final List<SkyLink.Actor> ACTORS = new ArrayList<>();

   private SkyCombat() {
   }

   public static void init() {
      FabricDefaultAttributeRegistry.register(SKYRIM_ACTOR, LivingEntity.createLivingAttributes());
      ServerTickEvents.END_SERVER_TICK.register(SkyCombat::serverTick);
   }

   public static @Nullable SkyrimActorEntity proxy(int var0) {
      return PROXIES.get(var0);
   }

   private static void serverTick(MinecraftServer var0) {
      List var1 = var0.getPlayerList().getPlayers();
      if (SkyLink.active() && !var1.isEmpty()) {
         ServerLevel var2 = ((ServerPlayer)var1.getFirst()).level();

         for (ServerPlayer var4 : var1) {
            pickUpNearby(var4);
         }

         if (SkyLink.readActors(ACTORS)) {
            sync(var2);
         }

         for (SkyrimActorEntity var7 : PROXIES.values()) {
            float[] var5 = var7.takeHit();
            if (var5 != null && (var5[0] > 0.0F || var5[3] > 0.0F)) {
               SkyLink.pushEvent(1, var7.formId(), var5[0], var5[1], var5[2], var5[3], Float.floatToRawIntBits(var5[4]), Float.floatToRawIntBits(var5[5]));
               SkyCraft.LOG.info("SkyCraft: hit {} for {} (knockback {})", new Object[]{var7.getName().getString(), var5[0], var5[3]});
            }
         }
      } else {
         removeAll();
      }
   }

   private static void sync(ServerLevel var0) {
      HashMap var1 = new HashMap();

      for (SkyLink.Actor var3 : ACTORS) {
         if (!var3.dead()) {
            var1.put(var3.formId(), var3);
         }
      }

      Iterator var6 = PROXIES.entrySet().iterator();

      while (var6.hasNext()) {
         Entry var8 = (Entry)var6.next();
         SkyrimActorEntity var4 = (SkyrimActorEntity)var8.getValue();
         if (!var1.containsKey(var8.getKey()) || var4.isRemoved() || var4.level() != var0) {
            var4.discard();
            var6.remove();
         }
      }

      int var7 = PROXIES.size();

      for (SkyLink.Actor var10 : var1.values()) {
         SkyrimActorEntity var5 = PROXIES.get(var10.formId());
         if (var5 == null) {
            var5 = new SkyrimActorEntity(SKYRIM_ACTOR, var0);
            var5.setFormId(var10.formId());
            var5.setSize(var10.width(), var10.height());
            var5.snapTo(var10.x(), var10.y(), var10.z(), var10.yaw(), 0.0F);
            if (!var10.name().isEmpty()) {
               var5.setCustomName(Component.literal(var10.name()));
            }

            if (var0.addFreshEntity(var5)) {
               PROXIES.put(var10.formId(), var5);
            }
         } else {
            var5.setSize(var10.width(), var10.height());
            var5.setPos(var10.x(), var10.y(), var10.z());
            var5.setYRot(var10.yaw());
            var5.setYHeadRot(var10.yaw());
            stepOnTriggers(var0, var5);
         }
      }

      if (PROXIES.size() != var7 && (PROXIES.size() % 5 == 0 || PROXIES.size() < 5)) {
         SkyCraft.LOG.info("SkyCraft: {} Skyrim actors mirrored as hittable stand-ins", PROXIES.size());
      }
   }

   private static void stepOnTriggers(ServerLevel var0, SkyrimActorEntity var1) {
      AABB var2 = var1.getBoundingBox().deflate(1.0E-5);
      BlockPos var3 = BlockPos.containing(var2.minX, var2.minY, var2.minZ);
      BlockPos var4 = BlockPos.containing(var2.maxX, var2.maxY, var2.maxZ);

      for (BlockPos var6 : BlockPos.betweenClosed(var3, var4)) {
         BlockState var7 = var0.getBlockState(var6);
         if (var7.getBlock() instanceof BasePressurePlateBlock || var7.getBlock() instanceof TripWireBlock) {
            var7.entityInside(var0, var6, var1, InsideBlockEffectApplier.NOOP, true);
         }
      }
   }

   private static void pickUpNearby(ServerPlayer var0) {
      if (var0.isAlive() && !var0.isSpectator()) {
         for (Entity var2 : var0.level().getEntities(var0, var0.getBoundingBox().inflate(1.25, 1.0, 1.25))) {
            if (!var2.isRemoved() && (var2 instanceof ItemEntity || var2 instanceof AbstractArrow)) {
               var2.playerTouch(var0);
            }
         }
      }
   }

   private static void removeAll() {
      if (!PROXIES.isEmpty()) {
         PROXIES.values().forEach(Entity::discard);
         PROXIES.clear();
      }
   }

   public static void hurtPlayer(ServerPlayer var0, int var1, float var2, int var3, int var4) {
      if (var0.isAlive() && !(var2 <= 0.0F)) {
         ServerLevel var5 = var0.level();
         SkyrimActorEntity var6 = PROXIES.get(var3);
         if (var6 != null && var6.distanceToSqr(var0) > 576.0) {
            var6 = null;
         }

         DamageSources var7 = var5.damageSources();

         DamageSource var8 = switch (var1) {
            case 0 -> var6 != null ? var7.mobAttack(var6) : var7.generic();
            case 1 -> var6 != null ? var7.mobProjectile(var6, var6) : var7.generic();
            case 2 -> var6 != null ? var7.indirectMagic(var6, var6) : var7.magic();
            default -> var7.generic();
         };
         float var9 = var2 / 5.0F;
         float var10 = var0.getHealth();
         boolean var11 = var0.isBlocking();
         boolean var12 = var0.hurtServer(var5, var8, var9);
         trainDefence(var0, var9, var11 && var0.getHealth() >= var10 - 0.001F);
         SkyCraft.LOG
            .info(
               "SkyCraft: Skyrim hit the player for {} ({} Minecraft): health {} -> {}{}",
               new Object[]{var2, var9, var10, var0.getHealth(), var12 ? "" : " (blocked/immune)"}
            );
         if (var12 && var6 != null && (var4 & 2) != 0 && !var0.isBlocking()) {
            var0.knockback(0.5, var6.getX() - var0.getX(), var6.getZ() - var0.getZ(), var8, var9);
         }
      }
   }

   private static void trainDefence(ServerPlayer var0, float var1, boolean var2) {
      if (SkyNet.isHost(var0) && !(var1 <= 0.0F)) {
         if (var2) {
            SkyLink.pushEvent(5, 9, var1, 0.0F, 0.0F, 0.0F, 0);
         } else {
            int var3 = 0;
            int var4 = 0;

            for (EquipmentSlot var8 : new EquipmentSlot[]{EquipmentSlot.HEAD, EquipmentSlot.CHEST, EquipmentSlot.LEGS, EquipmentSlot.FEET}) {
               ItemStack var9 = var0.getItemBySlot(var8);
               if (!var9.isEmpty()) {
                  String var10 = BuiltInRegistries.ITEM.getKey(var9.getItem()).getPath();
                  if (!var10.startsWith("iron_") && !var10.startsWith("diamond_") && !var10.startsWith("netherite_")) {
                     var3++;
                  } else {
                     var4++;
                  }
               }
            }

            if (var3 + var4 > 0) {
               SkyLink.pushEvent(5, var4 > var3 ? 11 : 12, var1 * (var3 + var4) / 4.0F, 0.0F, 0.0F, 0.0F, 0);
            }
         }
      }
   }

   public static int attackerFormId(DamageSource var0) {
      return var0.getEntity() instanceof SkyrimActorEntity var1 ? var1.formId() : 0;
   }
}
