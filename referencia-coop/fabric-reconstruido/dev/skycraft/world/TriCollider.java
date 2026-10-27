package dev.skycraft.world;

import java.util.ArrayList;
import java.util.List;

public final class TriCollider {
   private static final double FLOOR_RADIUS = 0.15;
   private static final double SUBSTEP = 0.1;
   private static final double AIR_STEP = 0.3;
   private static final double EPS = 1.0E-4;
   private static final double[][] FLOOR_SAMPLES = buildSamples();

   private TriCollider() {
   }

   private static double[][] buildSamples() {
      ArrayList var0 = new ArrayList();
      var0.add(new double[]{0.0, 0.0});

      for (int var1 = 0; var1 < 8; var1++) {
         double var2 = var1 * Math.PI / 4.0;
         var0.add(new double[]{Math.cos(var2) * 0.15, Math.sin(var2) * 0.15});
      }

      for (int var4 = 0; var4 < 4; var4++) {
         double var5 = (Math.PI / 4) + var4 * Math.PI / 2.0;
         var0.add(new double[]{Math.cos(var5) * 0.15 * 0.5, Math.sin(var5) * 0.15 * 0.5});
      }

      return var0.toArray(new double[0][]);
   }

   public static double[] resolve(
      List<SkyTri> var0, double var1, double var3, double var5, double var7, double var9, double var11, boolean var13, double var14, double var16, double var18
   ) {
      if (var0.isEmpty()) {
         return new double[]{var14, var16, var18};
      }

      double var20 = var1;
      double var22 = var3;
      double var24 = var5;
      double var26 = Math.hypot(var14, var18);
      int var28 = Math.max(1, (int)Math.ceil(var26 / 0.1));
      double var29 = var13 ? var11 : 0.02;
      boolean var31 = false;

      for (int var32 = 0; var32 < var28; var32++) {
         double var33 = var20;
         double var35 = var24;
         var20 += var14 / var28;
         var24 += var18 / var28;
         double[] var37 = pushOutOfWalls(var0, var20, var22, var24, var7, var9, var29, var11, var33, var35);
         var31 |= var37[0] != var20 || var37[1] != var24;
         var20 = var37[0];
         var24 = var37[1];
      }

      double var48 = var16;
      if (var48 > 0.0) {
         double var34 = ceilingAbove(var0, var20, var22 + var9, var24, var7 * 0.8);
         if (!Double.isNaN(var34)) {
            var48 = Math.max(0.0, Math.min(var48, var34 - (var22 + var9)));
         }
      }

      double var49 = var13 ? var11 : 0.3;
      double var36 = floor(var0, var20, var22, var24, true, var49);
      double var38 = floor(var0, var20, var22, var24, false, 1.0E-4);
      double var40 = Math.max(var36, var38);
      double var42 = var22 + var48;
      double var44;
      if (var42 <= var40) {
         var44 = var40 - var3;
      } else if (var13 && var48 <= 0.0 && var36 > Double.NEGATIVE_INFINITY && var22 - var36 <= Math.max(var11, var26 * 1.5)) {
         var44 = var36 - var3;
      } else {
         var44 = var48;
      }

      return new double[]{var31 ? var20 - var1 : var14, var44, var31 ? var24 - var5 : var18};
   }

   private static double floor(List<SkyTri> var0, double var1, double var3, double var5, boolean var7, double var8) {
      double var10 = Double.NEGATIVE_INFINITY;
      double var12 = var3 + var8;

      for (SkyTri var15 : var0) {
         if ((!var7 || var15.walkable) && !(var15.minY > var12)) {
            for (double[] var19 : FLOOR_SAMPLES) {
               double var20 = var15.heightAt(var1 + var19[0], var5 + var19[1]);
               if (!Double.isNaN(var20) && var20 <= var12 && var20 > var10) {
                  var10 = var20;
               }
            }
         }
      }

      return var10;
   }

   private static double ceilingAbove(List<SkyTri> var0, double var1, double var3, double var5, double var7) {
      double var9 = Double.NaN;

      for (SkyTri var12 : var0) {
         if (!var12.stairHelper && !(var12.maxY < var3 - 0.05)) {
            for (double[] var16 : FLOOR_SAMPLES) {
               double var17 = var12.heightAt(var1 + var16[0] * var7 / 0.15, var5 + var16[1] * var7 / 0.15);
               if (!Double.isNaN(var17) && var17 >= var3 - 0.05 && (Double.isNaN(var9) || var17 < var9)) {
                  var9 = var17;
               }
            }
         }
      }

      return var9;
   }

   private static double[] pushOutOfWalls(
      List<SkyTri> var0, double var1, double var3, double var5, double var7, double var9, double var11, double var13, double var15, double var17
   ) {
      double[] var19 = new double[18];

      for (int var20 = 0; var20 < 4; var20++) {
         double var21 = 0.0;
         double var23 = 0.0;
         double var25 = 0.0;

         for (SkyTri var28 : var0) {
            if (!var28.stairHelper) {
               double var29 = var3 + (var28.walkable ? var13 : var11);
               double var31 = var3 + var9 - 0.02;
               if (!(var28.maxY < var29)
                  && !(var28.minY > var31)
                  && !(var28.maxX < var1 - var7)
                  && !(var28.minX > var1 + var7)
                  && !(var28.maxZ < var5 - var7)
                  && !(var28.minZ > var5 + var7)) {
                  int var33 = clipToSlab(var28, var29, var31, var19);
                  if (var33 != 0) {
                     double[] var34 = closestXZ(var19, var33, var1, var5);
                     double var35 = var34[0];
                     double var37 = var34[1];
                     double var39 = var1 - var35;
                     double var41 = var5 - var37;
                     double var43 = Math.sqrt(var39 * var39 + var41 * var41);
                     double var45;
                     double var47;
                     double var49;
                     if (var43 > 1.0E-6) {
                        var45 = var7 - var43;
                        var47 = var39 / var43;
                        var49 = var41 / var43;
                     } else {
                        double var51 = Math.hypot(var28.nx, var28.nz);
                        if (var51 < 1.0E-6) {
                           continue;
                        }

                        var47 = var28.nx / var51;
                        var49 = var28.nz / var51;
                        if ((var15 - var28.ax) * var47 + (var17 - var28.az) * var49 < 0.0) {
                           var47 = -var47;
                           var49 = -var49;
                        }

                        var45 = var7;
                     }

                     if (var45 > var21) {
                        var21 = var45;
                        var23 = var47;
                        var25 = var49;
                     }
                  }
               }
            }
         }

         if (var21 <= 1.0E-4) {
            break;
         }

         var1 += var23 * (var21 + 1.0E-4);
         var5 += var25 * (var21 + 1.0E-4);
      }

      return new double[]{var1, var5};
   }

   private static int clipToSlab(SkyTri var0, double var1, double var3, double[] var5) {
      double[] var6 = new double[]{var0.ax, var0.ay, var0.az, var0.bx, var0.by, var0.bz, var0.cx, var0.cy, var0.cz};
      double[] var7 = new double[18];
      int var8 = clipPlane(var6, 3, var7, var1, true);
      return var8 == 0 ? 0 : clipPlane(var7, var8, var5, var3, false);
   }

   private static int clipPlane(double[] var0, int var1, double[] var2, double var3, boolean var5) {
      int var6 = 0;

      for (int var7 = 0; var7 < var1; var7++) {
         int var8 = (var7 + 1) % var1;
         double var9 = var0[var7 * 3 + 1];
         double var11 = var0[var8 * 3 + 1];
         boolean var13 = var5 ? var9 >= var3 : var9 <= var3;
         boolean var14 = var5 ? var11 >= var3 : var11 <= var3;
         if (var13) {
            var2[var6 * 3] = var0[var7 * 3];
            var2[var6 * 3 + 1] = var9;
            var2[var6 * 3 + 2] = var0[var7 * 3 + 2];
            var6++;
         }

         if (var13 != var14) {
            double var15 = (var3 - var9) / (var11 - var9);
            var2[var6 * 3] = var0[var7 * 3] + (var0[var8 * 3] - var0[var7 * 3]) * var15;
            var2[var6 * 3 + 1] = var3;
            var2[var6 * 3 + 2] = var0[var7 * 3 + 2] + (var0[var8 * 3 + 2] - var0[var7 * 3 + 2]) * var15;
            var6++;
         }
      }

      return var6;
   }

   private static double[] closestXZ(double[] var0, int var1, double var2, double var4) {
      double var6 = 0.0;

      for (int var8 = 0; var8 < var1; var8++) {
         int var9 = (var8 + 1) % var1;
         var6 += var0[var8 * 3] * var0[var9 * 3 + 2] - var0[var9 * 3] * var0[var8 * 3 + 2];
      }

      if (Math.abs(var6) > 1.0E-9) {
         boolean var38 = true;

         for (int var40 = 0; var40 < var1 && var38; var40++) {
            int var10 = (var40 + 1) % var1;
            double var11 = (var0[var10 * 3] - var0[var40 * 3]) * (var4 - var0[var40 * 3 + 2])
               - (var0[var10 * 3 + 2] - var0[var40 * 3 + 2]) * (var2 - var0[var40 * 3]);
            var38 = var6 > 0.0 ? var11 >= -1.0E-12 : var11 <= 1.0E-12;
         }

         if (var38) {
            return new double[]{var2, var4};
         }
      }

      double var39 = Double.MAX_VALUE;
      double var41 = var0[0];
      double var12 = var0[2];

      for (int var14 = 0; var14 < var1; var14++) {
         int var15 = (var14 + 1) % var1;
         double var16 = var0[var14 * 3];
         double var18 = var0[var14 * 3 + 2];
         double var20 = var0[var15 * 3];
         double var22 = var0[var15 * 3 + 2];
         double var24 = var20 - var16;
         double var26 = var22 - var18;
         double var28 = var24 * var24 + var26 * var26;
         double var30 = var28 > 1.0E-12 ? Math.max(0.0, Math.min(1.0, ((var2 - var16) * var24 + (var4 - var18) * var26) / var28)) : 0.0;
         double var32 = var16 + var24 * var30;
         double var34 = var18 + var26 * var30;
         double var36 = (var32 - var2) * (var32 - var2) + (var34 - var4) * (var34 - var4);
         if (var36 < var39) {
            var39 = var36;
            var41 = var32;
            var12 = var34;
         }
      }

      return new double[]{var41, var12};
   }

   public static double groundAt(List<SkyTri> var0, double var1, double var3, double var5, double var7) {
      double var9 = floor(var0, var1, var3, var5, false, var7);
      return var9 == Double.NEGATIVE_INFINITY ? Double.NaN : var9;
   }
}
