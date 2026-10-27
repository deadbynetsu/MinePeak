package dev.skycraft.coop;

import net.minecraft.client.Minecraft;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.world.item.ItemStack;

public final class CoopHeld {
   private static String last = null;
   private static long lastSent;

   private CoopHeld() {
   }

   public static void tick(Minecraft var0) {
      if (var0.player != null) {
         ItemStack var1 = var0.player.getMainHandItem();
         String var2 = var1.isEmpty() ? "" : BuiltInRegistries.ITEM.getKey(var1.getItem()).toString();
         long var3 = System.currentTimeMillis();
         if (!var2.equals(last) || var3 - lastSent > 5000L) {
            last = var2;
            lastSent = var3;
            CoopBlocks.pushString(1, var2);
         }
      }
   }
}
