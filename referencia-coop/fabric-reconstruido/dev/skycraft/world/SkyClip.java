package dev.skycraft.world;

import java.util.ArrayList;
import java.util.List;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.world.phys.AABB;
import net.minecraft.world.phys.BlockHitResult;
import net.minecraft.world.phys.Vec3;
import net.minecraft.world.phys.HitResult.Type;

public final class SkyClip {
   private static final ThreadLocal<List<SkyTri>> SCRATCH = ThreadLocal.withInitial(ArrayList::new);
   private static final SkyTri STONE_WALL = new SkyTri(new float[9], 0, 770);

   private SkyClip() {
   }

   public static BlockHitResult refine(Vec3 var0, Vec3 var1, BlockHitResult var2, SkyClip.Use var3) {
      SkyRay.Hit var4 = cast(var0, var1);
      if (var3 == SkyClip.Use.PICK) {
         double var5 = var4 != null ? var4.t() : 1.0;
         if (var2.getType() != Type.MISS) {
            var5 = Math.min(var5, Math.sqrt(var0.distanceToSqr(var2.getLocation()) / Math.max(var0.distanceToSqr(var1), 1.0E-9)));
         }

         SkyRay.Hit var7 = digWall(var0, var1, var5);
         if (var7 != null) {
            var4 = var7;
            var2 = BlockHitResult.miss(var1, Direction.UP, BlockPos.containing(var1));
         }
      }

      if (var4 == null) {
         return var2;
      }

      Vec3 var8 = new Vec3(var4.x(), var4.y(), var4.z());
      if (var2.getType() != Type.MISS && var0.distanceToSqr(var2.getLocation()) <= var0.distanceToSqr(var8)) {
         return var2;
      }

      Direction var6 = Direction.values()[SkyRay.dominantFace(var4.nx(), var4.ny(), var4.nz())];
      int[] var9 = var3 == SkyClip.Use.PICK ? SkyRay.placementCell(var4) : SkyRay.surfaceCell(var4);
      return new SkyClip.SkyrimHitResult(var8, var6, new BlockPos(var9[0], var9[1], var9[2]), var4);
   }

   static SkyRay.Hit digWall(Vec3 var0, Vec3 var1, double var2) {
      SkyDig.DugLookup var4 = SkyDig.clientDug;
      if (var4 == null) {
         return null;
      }

      double var5 = var1.x - var0.x;
      double var7 = var1.y - var0.y;
      double var9 = var1.z - var0.z;
      int var11 = (int)Math.floor(var0.x);
      int var12 = (int)Math.floor(var0.y);
      int var13 = (int)Math.floor(var0.z);
      int var14 = var5 > 0.0 ? 1 : -1;
      int var15 = var7 > 0.0 ? 1 : -1;
      int var16 = var9 > 0.0 ? 1 : -1;
      double var17 = var5 == 0.0 ? Double.POSITIVE_INFINITY : Math.abs(1.0 / var5);
      double var19 = var7 == 0.0 ? Double.POSITIVE_INFINITY : Math.abs(1.0 / var7);
      double var21 = var9 == 0.0 ? Double.POSITIVE_INFINITY : Math.abs(1.0 / var9);
      double var23 = var5 == 0.0 ? Double.POSITIVE_INFINITY : (var5 > 0.0 ? var11 + 1 - var0.x : var0.x - var11) * var17;
      double var25 = var7 == 0.0 ? Double.POSITIVE_INFINITY : (var7 > 0.0 ? var12 + 1 - var0.y : var0.y - var12) * var19;
      double var27 = var9 == 0.0 ? Double.POSITIVE_INFINITY : (var9 > 0.0 ? var13 + 1 - var0.z : var0.z - var13) * var21;
      boolean var29 = var4.isDug(var11, var12, var13);
      SkyDig.Probe var30 = null;

      for (int var31 = 0; var31 < 64; var31++) {
         double var34 = 0.0;
         double var36 = 0.0;
         double var38 = 0.0;
         double var32;
         if (var23 <= var25 && var23 <= var27) {
            var32 = var23;
            var23 += var17;
            var11 += var14;
            var34 = -var14;
         } else if (var25 <= var27) {
            var32 = var25;
            var25 += var19;
            var12 += var15;
            var36 = -var15;
         } else {
            var32 = var27;
            var27 += var21;
            var13 += var16;
            var38 = -var16;
         }

         if (var32 > var2) {
            return null;
         }

         boolean var40 = var4.isDug(var11, var12, var13);
         if (var29 && !var40) {
            double var41 = var0.x + var5 * var32;
            double var43 = var0.y + var7 * var32;
            double var45 = var0.z + var9 * var32;
            if (var30 == null) {
               var30 = new SkyDig.Probe()
                  .around(
                     Math.min(var0.x, var1.x),
                     Math.min(var0.y, var1.y),
                     Math.min(var0.z, var1.z),
                     Math.max(var0.x, var1.x),
                     Math.max(var0.y, var1.y),
                     Math.max(var0.z, var1.z)
                  );
            }

            if (var30.test(var41 - var34 * 0.02, var43 - var36 * 0.02, var45 - var38 * 0.02) > 0) {
               return new SkyRay.Hit(var32, var41, var43, var45, var34, var36, var38, var30.surface != null ? var30.surface : STONE_WALL);
            }
         }

         var29 = var40;
      }

      return null;
   }

   public static SkyRay.Hit cast(Vec3 var0, Vec3 var1) {
      List var2 = SCRATCH.get();
      var2.clear();
      SkyCollision.trianglesNear(new AABB(var0, var1).inflate(0.01), var2);
      if (var2.isEmpty()) {
         return null;
      }

      SkyRay.Hit var3 = SkyRay.cast(var2, var0.x, var0.y, var0.z, var1.x, var1.y, var1.z);
      var2.clear();
      return var3;
   }

   public static final class SkyrimHitResult extends BlockHitResult {
      public final double nx;
      public final double ny;
      public final double nz;
      public final SkyRay.Hit hit;

      public SkyrimHitResult(Vec3 var1, Direction var2, BlockPos var3, SkyRay.Hit var4) {
         super(var1, var2, var3, false);
         this.nx = var4.nx();
         this.ny = var4.ny();
         this.nz = var4.nz();
         this.hit = var4;
      }
   }

   public enum Use {
      PROJECTILE,
      PICK;
   }
}
