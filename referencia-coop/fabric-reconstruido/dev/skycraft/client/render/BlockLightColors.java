package dev.skycraft.client.render;

import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.state.BlockState;

final class BlockLightColors {
   private static final int HAZARD_FIRE = 268435456;
   private static final int HAZARD_LAVA = 536870912;
   private static final int HAZARD_MAGMA = 805306368;

   private BlockLightColors() {
   }

   static int of(BlockState var0) {
      Block var1 = var0.getBlock();
      String var2 = BuiltInRegistries.BLOCK.getKey(var1).getPath();
      if (var2.contains("soul")) {
         int var3 = var2.contains("lantern") ? 0 : 1;
         return pack(90, 210, 255, var3) | (var2.endsWith("fire") ? 268435456 : 0);
      } else if (var2.contains("lava")) {
         return pack(255, 105, 25, 2) | 536870912;
      } else if (var2.equals("magma_block")) {
         return pack(255, 105, 25, 2) | 805306368;
      } else if (var2.contains("redstone_lamp")) {
         return pack(255, 215, 150, 0);
      } else if (var2.contains("redstone")) {
         return pack(255, 55, 30, 0);
      } else if (var2.contains("jack_o_lantern")) {
         return pack(255, 165, 60, 1);
      } else if (var2.contains("torch")) {
         return pack(255, 185, 105, 1);
      } else if (var2.contains("lantern") && !var2.contains("sea")) {
         return pack(255, 195, 120, 1);
      } else if (var2.contains("campfire")) {
         return pack(255, 165, 85, 1) | 268435456;
      } else if (var2.equals("fire")) {
         return pack(255, 145, 55, 1) | 268435456;
      } else if (var2.contains("candle")) {
         return pack(255, 180, 105, 1);
      } else if (var2.contains("furnace") || var2.equals("smoker")) {
         return pack(255, 150, 60, 1);
      } else if (var2.equals("glowstone")) {
         return pack(255, 215, 140, 0);
      } else if (var2.equals("shroomlight")) {
         return pack(255, 175, 105, 0);
      } else if (var2.equals("sea_lantern") || var2.equals("beacon") || var2.equals("end_rod")) {
         return pack(205, 235, 255, 0);
      } else if (var2.equals("conduit")) {
         return pack(160, 225, 255, 0);
      } else if (var2.contains("ochre_froglight")) {
         return pack(255, 220, 140, 0);
      } else if (var2.contains("verdant_froglight")) {
         return pack(170, 255, 150, 0);
      } else if (var2.contains("pearlescent_froglight")) {
         return pack(255, 190, 240, 0);
      } else if (var2.contains("amethyst")) {
         return pack(190, 130, 255, 0);
      } else if (var2.contains("crying_obsidian") || var2.equals("nether_portal") || var2.equals("respawn_anchor") || var2.equals("dragon_egg")) {
         return pack(150, 65, 255, 0);
      } else if (var2.startsWith("end_portal") || var2.equals("end_gateway")) {
         return pack(120, 230, 200, 0);
      } else if (var2.contains("sculk")) {
         return pack(60, 200, 220, 0);
      } else if (var2.contains("glow") || var2.contains("cave_vines")) {
         return pack(255, 210, 130, 0);
      } else if (var2.contains("copper_bulb")) {
         return pack(255, 190, 130, 0);
      } else {
         return !var2.contains("spawner") && !var2.equals("vault") && !var2.equals("creaking_heart") ? pack(255, 225, 190, 0) : pack(255, 165, 90, 1);
      }
   }

   private static int pack(int var0, int var1, int var2, int var3) {
      return var0 | var1 << 8 | var2 << 16 | var3 << 24;
   }
}
