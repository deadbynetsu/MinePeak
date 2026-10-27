package dev.skycraft.world;

import dev.skycraft.link.SkyLink;
import it.unimi.dsi.fastutil.longs.Long2IntOpenHashMap;
import it.unimi.dsi.fastutil.longs.LongOpenHashSet;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.world.level.ServerExplosion;
import net.minecraft.world.phys.Vec3;
import org.jspecify.annotations.Nullable;

public final class SkyDigBlast {
   private static final float BUILDING_RESISTANCE = 1200.0F;
   private static final double[] SAMPLE = new double[]{0.2, 0.5, 0.8};
   private static final SkyLink.SkyState SKY = new SkyLink.SkyState();
   private final ServerLevel level;
   private final int world;
   private final SkyDig.Probe probe;
   private final Long2IntOpenHashMap cells = new Long2IntOpenHashMap();
   private final List<BlockPos> opened = new ArrayList<>();

   private SkyDigBlast(ServerLevel var1, int var2, Vec3 var3, float var4) {
      this.level = var1;
      this.world = var2;
      double var5 = var4 * 1.4 + 2.0;
      this.probe = new SkyDig.Probe().around(var3.x - var5, var3.y - var5, var3.z - var5, var3.x + var5, var3.y + var5, var3.z + var5);
      this.cells.defaultReturnValue(Integer.MIN_VALUE);
   }

   public static @Nullable SkyDigBlast begin(ServerExplosion var0) {
      Vec3 var1 = var0.center();
      return SkyDig.destruction
            && SkyCollision.active()
            && SkyCollision.isKnown((int)Math.floor(var1.x), (int)Math.floor(var1.y), (int)Math.floor(var1.z))
            && SkyLink.readSkyState(SKY)
         ? new SkyDigBlast(var0.level(), SKY.worldId, var1, var0.radius())
         : null;
   }

   private int material(BlockPos var1) {
      long var2 = var1.asLong();
      int var4 = this.cells.get(var2);
      if (var4 != Integer.MIN_VALUE) {
         return var4;
      }

      int var5 = 0;
      if (this.level.getBlockState(var1).isAir() && !SkyDig.isDug(this.level, this.world, var1)) {
         for (double var9 : SAMPLE) {
            for (double var14 : SAMPLE) {
               for (double var19 : SAMPLE) {
                  int var21 = this.probe.test(var1.getX() + var19, var1.getY() + var9, var1.getZ() + var14);
                  if (var21 == -1) {
                     var5 = -1;
                     break;
                  }

                  if (var21 > 0 && var5 == 0) {
                     var5 = SkyDig.underground(var21, Math.max(this.probe.depth, 0.5));
                  }
               }
            }
         }
      }

      this.cells.put(var2, var5);
      return var5;
   }

   public Optional<Float> resistance(BlockPos var1, Optional<Float> var2) {
      if (var2.isPresent()) {
         return var2;
      } else {
         int var3 = this.material(var1);
         if (var3 == -1) {
            return Optional.of(1200.0F);
         } else {
            return var3 > 0 ? Optional.of(SkyDig.materialState(var3).getBlock().getExplosionResistance()) : var2;
         }
      }
   }

   public void materialize(List<BlockPos> var1, boolean var2) {
      if (var2) {
         for (BlockPos var4 : var1) {
            int var5 = this.material(var4);
            if (var5 > 0) {
               SkyDig.markDug(this.level, this.world, var4);
               this.level.setBlock(var4, SkyDig.blockFor(this.level, var4, var5), 18);
               this.opened.add(var4.immutable());
            }
         }
      }
   }

   public void finish() {
      if (!this.opened.isEmpty()) {
         LongOpenHashSet var1 = new LongOpenHashSet();

         for (BlockPos var3 : this.opened) {
            var1.add(var3.asLong());
         }

         for (BlockPos var11 : this.opened) {
            for (Direction var7 : Direction.values()) {
               BlockPos var8 = var11.relative(var7);
               if (var1.add(var8.asLong()) && !SkyDig.isDug(this.level, this.world, var8)) {
                  int var9 = SkyDig.classify(var8.getX(), var8.getY(), var8.getZ());
                  if (var9 > 0) {
                     SkyDig.digCell(this.level, this.world, var8, var9);
                  }
               }
            }
         }
      }
   }
}
