package dev.skycraft.world;

import dev.skycraft.link.SkyLink;
import net.minecraft.core.BlockPos;
import net.minecraft.world.level.BlockGetter;
import net.minecraft.world.level.material.FluidState;
import net.minecraft.world.level.material.Fluids;
import org.jspecify.annotations.Nullable;

public final class SkyWater {
   private static volatile SkyWater.@Nullable Grid grid;

   private SkyWater() {
   }

   public static void refresh() {
      SkyLink.WaterGrid var0 = SkyLink.readWaterGrid();
      if (var0 != null) {
         grid = new SkyWater.Grid(var0.originX, var0.originZ, 16, var0.surface);
      }
   }

   public static void clear() {
      grid = null;
   }

   public static boolean active() {
      return grid != null;
   }

   public static double surfaceAt(int var0, int var1) {
      SkyWater.Grid var2 = grid;
      if (var2 == null) {
         return Double.NaN;
      } else {
         int var3 = var0 - var2.originX();
         int var4 = var1 - var2.originZ();
         if (var3 >= 0 && var4 >= 0 && var3 < var2.size() && var4 < var2.size()) {
            float var5 = var2.surface()[var4 * var2.size() + var3];
            return var5 < -1.0E20F ? Double.NaN : var5;
         } else {
            return Double.NaN;
         }
      }
   }

   public static float depthIn(BlockPos var0) {
      double var1 = surfaceAt(var0.getX(), var0.getZ());
      if (Double.isNaN(var1)) {
         return 0.0F;
      }

      double var3 = var1 - var0.getY();
      return var3 < 0.02 ? 0.0F : (float)Math.min(1.0, var3);
   }

   public static boolean anyIn(int var0, int var1, int var2, int var3, int var4, int var5) {
      if (grid == null) {
         return false;
      }

      for (int var6 = var0; var6 <= var3; var6++) {
         for (int var7 = var2; var7 <= var5; var7++) {
            double var8 = surfaceAt(var6, var7);
            if (!Double.isNaN(var8) && var8 > var1) {
               return true;
            }
         }
      }

      return false;
   }

   public static @Nullable FluidState fluidAt(BlockGetter var0, BlockPos var1) {
      return !(depthIn(var1) <= 0.0F) && var0.getBlockState(var1).isAir() ? Fluids.WATER.getSource(false) : null;
   }

   public static float substitutedHeight(BlockGetter var0, BlockPos var1) {
      float var2 = depthIn(var1);
      return !(var2 <= 0.0F) && var0.getFluidState(var1).isEmpty() && var0.getBlockState(var1).isAir() ? var2 : -1.0F;
   }

   private record Grid(int originX, int originZ, int size, float[] surface) {
   }
}
