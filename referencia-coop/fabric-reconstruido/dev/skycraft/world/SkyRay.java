package dev.skycraft.world;

import java.util.List;

public final class SkyRay {
   private SkyRay() {
   }

   public static SkyRay.Hit cast(List<SkyTri> var0, double var1, double var3, double var5, double var7, double var9, double var11) {
      double var13 = var7 - var1;
      double var15 = var9 - var3;
      double var17 = var11 - var5;
      double var19 = Double.POSITIVE_INFINITY;
      SkyTri var21 = null;

      for (SkyTri var23 : var0) {
         if (!var23.stairHelper) {
            double var24 = intersect(var23, var1, var3, var5, var13, var15, var17);
            if (var24 >= 0.0 && var24 <= 1.0 && var24 < var19) {
               var19 = var24;
               var21 = var23;
            }
         }
      }

      if (var21 == null) {
         return null;
      }

      double var28 = var21.nx;
      double var29 = var21.ny;
      double var26 = var21.nz;
      if (var28 * var13 + var29 * var15 + var26 * var17 > 0.0) {
         var28 = -var28;
         var29 = -var29;
         var26 = -var26;
      }

      return new SkyRay.Hit(var19, var1 + var13 * var19, var3 + var15 * var19, var5 + var17 * var19, var28, var29, var26, var21);
   }

   static double intersect(SkyTri var0, double var1, double var3, double var5, double var7, double var9, double var11) {
      double var13 = var0.bx - var0.ax;
      double var15 = var0.by - var0.ay;
      double var17 = var0.bz - var0.az;
      double var19 = var0.cx - var0.ax;
      double var21 = var0.cy - var0.ay;
      double var23 = var0.cz - var0.az;
      double var25 = var9 * var23 - var11 * var21;
      double var27 = var11 * var19 - var7 * var23;
      double var29 = var7 * var21 - var9 * var19;
      double var31 = var13 * var25 + var15 * var27 + var17 * var29;
      if (Math.abs(var31) < 1.0E-12) {
         return -1.0;
      } else {
         double var33 = 1.0 / var31;
         double var35 = var1 - var0.ax;
         double var37 = var3 - var0.ay;
         double var39 = var5 - var0.az;
         double var41 = (var35 * var25 + var37 * var27 + var39 * var29) * var33;
         if (!(var41 < -1.0E-9) && !(var41 > 1.000000001)) {
            double var43 = var37 * var17 - var39 * var15;
            double var45 = var39 * var13 - var35 * var17;
            double var47 = var35 * var15 - var37 * var13;
            double var49 = (var7 * var43 + var9 * var45 + var11 * var47) * var33;
            return !(var49 < -1.0E-9) && !(var41 + var49 > 1.000000001) ? (var19 * var43 + var21 * var45 + var23 * var47) * var33 : -1.0;
         } else {
            return -1.0;
         }
      }
   }

   public static int[] placementCell(SkyRay.Hit var0) {
      double var1 = 0.4;
      return new int[]{(int)Math.floor(var0.x + var0.nx * var1), (int)Math.floor(var0.y + var0.ny * var1), (int)Math.floor(var0.z + var0.nz * var1)};
   }

   public static int[] surfaceCell(SkyRay.Hit var0) {
      double var1 = 0.01;
      return new int[]{(int)Math.floor(var0.x - var0.nx * var1), (int)Math.floor(var0.y - var0.ny * var1), (int)Math.floor(var0.z - var0.nz * var1)};
   }

   public static int dominantFace(double var0, double var2, double var4) {
      double var6 = Math.abs(var0);
      double var8 = Math.abs(var2);
      double var10 = Math.abs(var4);
      if (var8 >= var6 && var8 >= var10) {
         return var2 >= 0.0 ? 1 : 0;
      } else if (var6 >= var10) {
         return var0 >= 0.0 ? 5 : 4;
      } else {
         return var4 >= 0.0 ? 3 : 2;
      }
   }

   public record Hit(double t, double x, double y, double z, double nx, double ny, double nz, SkyTri tri) {
   }
}
