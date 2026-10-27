package dev.skycraft.coop;

import dev.skycraft.SkyCraft;
import dev.skycraft.link.SkyLink;
import java.nio.charset.StandardCharsets;
import net.minecraft.core.BlockPos;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.state.BlockState;

public final class CoopBlocks {
   private static final ThreadLocal<Boolean> APPLYING = ThreadLocal.withInitial(() -> false);

   private CoopBlocks() {
   }

   public static void report(Level var0, BlockPos var1, BlockState var2) {
      if (!APPLYING.get() && !var0.isClientSide() && SkyLink.active()) {
         String var3 = BuiltInRegistries.BLOCK.getKey(var2.getBlock()).toString();
         pushString(2, var3);
         SkyLink.pushEventRaw(7, Block.getId(var2), var1.getX(), var1.getY(), var1.getZ(), 0, 0, 0);
      }
   }

   public static void applyRemote(MinecraftServer var0, int var1, int var2, int var3, int var4) {
      if (var0 != null) {
         var0.execute(
            () -> {
               ServerLevel var5 = var0.overworld();
               BlockState var6 = Block.stateById(var1);
               BlockPos var7 = new BlockPos(var2, var3, var4);
               if (var5.getBlockState(var7) != var6) {
                  APPLYING.set(true);

                  try {
                     var5.setBlock(var7, var6, 3);
                  } finally {
                     APPLYING.set(false);
                  }

                  SkyCraft.LOG
                     .info(
                        "SkyCraft: coop: applied remote block {} at {} {} {}", new Object[]{BuiltInRegistries.BLOCK.getKey(var6.getBlock()), var2, var3, var4}
                     );
               }
            }
         );
      }
   }

   public static void pushString(int var0, String var1) {
      byte[] var2 = var1.getBytes(StandardCharsets.UTF_8);
      int var3 = Math.max(1, Math.min(255, (var2.length + 23) / 24));

      for (int var4 = 0; var4 < var3; var4++) {
         int[] var5 = new int[6];

         for (int var6 = 0; var6 < 24; var6++) {
            int var7 = var4 * 24 + var6;
            int var8 = var7 < var2.length ? var2[var7] & 0xFF : 0;
            var5[var6 / 4] = var5[var6 / 4] | var8 << 8 * (var6 % 4);
         }

         SkyLink.pushEventRaw(6, var0 << 16 | var4 << 8 | var3, var5[0], var5[1], var5[2], var5[3], var5[4], var5[5]);
      }
   }
}
