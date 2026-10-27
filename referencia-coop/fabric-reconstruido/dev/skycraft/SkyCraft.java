package dev.skycraft;

import dev.skycraft.combat.SkyCombat;
import dev.skycraft.net.SkyNet;
import dev.skycraft.world.SkyDig;
import net.fabricmc.api.ModInitializer;
import net.fabricmc.fabric.api.event.lifecycle.v1.ServerLifecycleEvents;
import net.fabricmc.fabric.api.networking.v1.ServerPlayConnectionEvents;
import net.fabricmc.fabric.api.networking.v1.ServerPlayConnectionEvents.Join;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.util.RandomSource;
import net.minecraft.world.entity.EquipmentSlot;
import net.minecraft.world.entity.player.Inventory;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.gamerules.GameRules;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public final class SkyCraft implements ModInitializer {
   public static final String MOD_ID = "skycraft";
   public static final String WORLD_NAME = "SkyCraft";
   public static final Logger LOG = LoggerFactory.getLogger("skycraft");
   private static final String KIT2_TAG = "skycraft_builder_kit";

   public void onInitialize() {
      SkyCombat.init();
      SkyNet.init();
      SkyDig.init();
      ServerLifecycleEvents.SERVER_STARTED.register(SkyCraft::configureServer);
      ServerPlayConnectionEvents.JOIN.register((Join)(var0, var1, var2) -> {
         giveStarterKit(var0.getPlayer());
         giveBuilderKit(var0.getPlayer());
         dressTestGuest(var0.getPlayer());
      });
   }

   private static void configureServer(MinecraftServer var0) {
      GameRules var1 = var0.getGameRules();
      var1.set(GameRules.ADVANCE_TIME, false, var0);
      var1.set(GameRules.ADVANCE_WEATHER, false, var0);
      var1.set(GameRules.SPAWN_MOBS, false, var0);
      var1.set(GameRules.SPAWN_MONSTERS, false, var0);
      var1.set(GameRules.SPAWN_PHANTOMS, false, var0);
      var1.set(GameRules.SPAWN_PATROLS, false, var0);
      var1.set(GameRules.SPAWN_WANDERING_TRADERS, false, var0);
      var1.set(GameRules.PLAYER_MOVEMENT_CHECK, false, var0);
      var1.set(GameRules.KEEP_INVENTORY, true, var0);
      var1.set(GameRules.IMMEDIATE_RESPAWN, true, var0);
      var1.set(GameRules.SHOW_ADVANCEMENT_MESSAGES, false, var0);
      var0.getCommands().performPrefixedCommand(var0.createCommandSourceStack().withSuppressedOutput(), "time set noon");
      LOG.info("SkyCraft: mirror world configured");
   }

   private static void dressTestGuest(ServerPlayer var0) {
      if (var0.getName().getString().startsWith("Guest")) {
         RandomSource var1 = var0.getRandom();
         EquipmentSlot[] var2 = new EquipmentSlot[]{EquipmentSlot.HEAD, EquipmentSlot.CHEST, EquipmentSlot.LEGS, EquipmentSlot.FEET};
         Item[][] var3 = new Item[][]{
            {Items.IRON_HELMET, Items.DIAMOND_HELMET},
            {Items.IRON_CHESTPLATE, Items.DIAMOND_CHESTPLATE},
            {Items.IRON_LEGGINGS, Items.DIAMOND_LEGGINGS},
            {Items.IRON_BOOTS, Items.DIAMOND_BOOTS}
         };

         for (int var4 = 0; var4 < var2.length; var4++) {
            var0.setItemSlot(var2[var4], new ItemStack(var3[var4][var1.nextBoolean() ? 1 : 0]));
         }

         LOG.info("SkyCraft: dressed test guest {} in iron and diamond", var0.getName().getString());
      }
   }

   private static void giveStarterKit(ServerPlayer var0) {
      if (var0.getInventory().isEmpty()) {
         var0.getInventory().add(new ItemStack(Items.DIAMOND_SWORD));
         var0.getInventory().add(new ItemStack(Items.DIAMOND_PICKAXE));
         var0.getInventory().add(new ItemStack(Items.BOW));
         var0.getInventory().add(new ItemStack(Items.COOKED_BEEF, 32));
         var0.getInventory().add(new ItemStack(Items.OAK_PLANKS, 64));
         var0.getInventory().add(new ItemStack(Items.TORCH, 32));
         var0.getInventory().add(new ItemStack(Items.ARROW, 64));
         var0.setItemSlot(EquipmentSlot.OFFHAND, new ItemStack(Items.SHIELD));
         LOG.info("SkyCraft: gave starter kit to {}", var0.getName().getString());
      }
   }

   private static void giveBuilderKit(ServerPlayer var0) {
      if (!var0.entityTags().contains("skycraft_builder_kit")) {
         equipIfEmpty(var0, EquipmentSlot.HEAD, Items.IRON_HELMET);
         equipIfEmpty(var0, EquipmentSlot.CHEST, Items.IRON_CHESTPLATE);
         equipIfEmpty(var0, EquipmentSlot.LEGS, Items.IRON_LEGGINGS);
         equipIfEmpty(var0, EquipmentSlot.FEET, Items.IRON_BOOTS);
         Inventory var1 = var0.getInventory();
         var1.add(new ItemStack(Items.COBBLESTONE, 64));
         var1.add(new ItemStack(Items.STONE_BRICKS, 64));
         var1.add(new ItemStack(Items.OAK_LOG, 64));
         var1.add(new ItemStack(Items.GLASS, 64));
         var1.add(new ItemStack(Items.OAK_STAIRS, 64));
         var1.add(new ItemStack(Items.OAK_SLAB, 64));
         var1.add(new ItemStack(Items.OAK_DOOR, 8));
         var1.add(new ItemStack(Items.LADDER, 32));
         var1.add(new ItemStack(Items.LANTERN, 16));
         var1.add(new ItemStack(Items.CRAFTING_TABLE));
         var1.add(new ItemStack(Items.WATER_BUCKET));
         var1.add(new ItemStack(Items.ARROW, 64));
         var1.add(new ItemStack(Items.GOLDEN_APPLE, 4));
         var0.addTag("skycraft_builder_kit");
         LOG.info("SkyCraft: gave builder kit to {}", var0.getName().getString());
      }
   }

   private static void equipIfEmpty(ServerPlayer var0, EquipmentSlot var1, Item var2) {
      if (var0.getItemBySlot(var1).isEmpty()) {
         var0.setItemSlot(var1, new ItemStack(var2));
      } else {
         var0.getInventory().add(new ItemStack(var2));
      }
   }
}
