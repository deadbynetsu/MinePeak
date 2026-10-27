package dev.skycraft.client.render;

import dev.skycraft.world.SkyDig;
import dev.skycraft.world.SkyTri;
import java.util.ArrayList;
import java.util.List;
import java.util.function.ToDoubleFunction;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.core.BlockPos.MutableBlockPos;
import net.minecraft.core.Direction.Axis;
import net.minecraft.util.LightCoordsUtil;
import net.minecraft.world.level.LightLayer;
import net.minecraft.world.level.block.state.BlockState;

final class DigWalls {
   private static final Direction[] WALLS = Direction.values();
   private static final int STEPS = 4;
   private static final double INTO = 0.02;

   private DigWalls() {
   }

   static void add(ClientLevel var0, int var1, int var2, int var3, long[] var4, SkyDig.DugLookup var5, DigWalls.Faces var6, DigWalls.Out var7) {
      SkyDig.Probe var8 = new SkyDig.Probe();
      DigWalls.Emitter var9 = new DigWalls.Emitter(var1, var2, var3, var7);
      MutableBlockPos var10 = new MutableBlockPos();

      for (int var11 = 0; var11 < 64; var11++) {
         long var12 = var4[var11];

         while (var12 != 0L) {
            int var14 = var11 * 64 + Long.numberOfTrailingZeros(var12);
            var12 &= var12 - 1L;
            int var15 = var1 * 16 + (var14 & 15);
            int var16 = var2 * 16 + (var14 >> 8);
            int var17 = var3 * 16 + (var14 >> 4 & 15);
            var10.set(var15, var16, var17);
            if (!var0.getBlockState(var10).canOcclude()) {
               var9.light = LightCoordsUtil.pack(var0.getBrightness(LightLayer.BLOCK, var10), var0.getBrightness(LightLayer.SKY, var10));

               for (Direction var21 : WALLS) {
                  int var22 = var15 + var21.getStepX();
                  int var23 = var16 + var21.getStepY();
                  int var24 = var17 + var21.getStepZ();
                  if (!var5.isDug(var22, var23, var24) && !var0.getBlockState(new BlockPos(var22, var23, var24)).canOcclude()) {
                     var8.around(var22, var23, var24, var22 + 1, var23 + 1, var24 + 1);
                     var9.rects = null;
                     var9.material = -1;
                     face(var8, var21, var15, var16, var17, var6, var9);
                  }
               }
            }
         }
      }
   }

   private static void face(SkyDig.Probe var0, Direction var1, int var2, int var3, int var4, DigWalls.Faces var5, DigWalls.Emitter var6) {
      boolean var7 = var1.getAxis() != Axis.Y;
      if (!var0.land.isEmpty()) {
         if (var7) {
            landSide(var0, var1, var2, var3, var4, var5, var6);
         } else {
            landFlat(var0, var1 == Direction.UP, var2, var3, var4, var5, var6);
         }
      }

      if (var0.land.isEmpty() || !var0.tris.isEmpty()) {
         sampled(var0, var1, var2, var3, var4, var5, var6);
      }
   }

   private static double[] onFace(Direction var0, int var1, int var2, int var3, double var4, double var6) {
      return switch (var0) {
         case NORTH -> new double[]{var1 + var4, var2 + var6, var3};
         case SOUTH -> new double[]{var1 + 1 - var4, var2 + var6, var3 + 1};
         case WEST -> new double[]{var1, var2 + var6, var3 + 1 - var4};
         case EAST -> new double[]{var1 + 1, var2 + var6, var3 + var4};
         case UP -> new double[]{var1 + var4, var2 + 1, var3 + var6};
         case DOWN -> new double[]{var1 + var4, var2, var3 + var6};
      };
   }

   private static int rectFor(Direction var0) {
      return var0 == Direction.UP ? 8 : (var0 == Direction.DOWN ? 4 : 0);
   }

   private static void landSide(SkyDig.Probe var0, Direction var1, int var2, int var3, int var4, DigWalls.Faces var5, DigWalls.Emitter var6) {
      ArrayList var7 = new ArrayList();

      for (SkyTri var9 : var0.land) {
         double[] var10 = cross(var9, var1, var2, var4);
         if (var10 != null) {
            var7.add(var10);
         }
      }

      if (!var7.isEmpty()) {
         ArrayList var32 = new ArrayList();
         var32.add(0.0);
         var32.add(1.0);

         for (double[] var35 : var7) {
            if (var35[0] > 0.0 && var35[0] < 1.0) {
               var32.add(var35[0]);
            }

            if (var35[2] > 0.0 && var35[2] < 1.0) {
               var32.add(var35[2]);
            }
         }

         var32.sort(Double::compare);
         Direction var34 = var1.getOpposite();

         for (int var36 = 0; var36 + 1 < var32.size(); var36++) {
            double var11 = (Double)var32.get(var36);
            double var13 = (Double)var32.get(var36 + 1);
            if (!(var13 - var11 < 1.0E-6)) {
               double[] var15 = covering(var7, (var11 + var13) * 0.5);
               if (var15 != null) {
                  double var16 = at(var15, var11) - var3;
                  double var18 = at(var15, var13) - var3;
                  ArrayList var20 = new ArrayList<>(List.of(var11, var13));

                  for (double var24 : new double[]{0.0, 1.0}) {
                     if ((var16 - var24) * (var18 - var24) < 0.0) {
                        var20.add(var11 + (var13 - var11) * (var24 - var16) / (var18 - var16));
                     }
                  }

                  var20.sort(Double::compare);

                  for (int var37 = 0; var37 + 1 < var20.size(); var37++) {
                     double var38 = (Double)var20.get(var37);
                     double var39 = (Double)var20.get(var37 + 1);
                     double var26 = Math.min(1.0, var16 + (var18 - var16) * (var38 - var11) / (var13 - var11));
                     double var28 = Math.min(1.0, var16 + (var18 - var16) * (var39 - var11) / (var13 - var11));
                     if (!(var26 <= 1.0E-4) || !(var28 <= 1.0E-4)) {
                        var26 = Math.max(var26, 0.0);
                        var28 = Math.max(var28, 0.0);
                        if (!var6.texture(var5, surfaceMaterial(var15), 0.5)) {
                           return;
                        }

                        List var30 = List.of(
                           onFace(var1, var2, var3, var4, var38, 0.0),
                           onFace(var1, var2, var3, var4, var39, 0.0),
                           onFace(var1, var2, var3, var4, var39, var28),
                           onFace(var1, var2, var3, var4, var38, var26)
                        );
                        List var31 = List.of(
                           new double[]{var38, 1.0}, new double[]{var39, 1.0}, new double[]{var39, 1.0 - var28}, new double[]{var38, 1.0 - var26}
                        );
                        var6.polygon(var30, var31, rectFor(var1), var34);
                     }
                  }
               }
            }
         }
      }
   }

   private static int surfaceMaterial(double[] var0) {
      return (int)var0[4];
   }

   private static double[] cross(SkyTri var0, Direction var1, int var2, int var3) {
      boolean var4 = var1 == Direction.NORTH || var1 == Direction.SOUTH;

      double var5 = switch (var1) {
         case NORTH -> var3;
         case SOUTH -> var3 + 1;
         case WEST -> var2;
         default -> var2 + 1;
      };
      double[][] var7 = new double[][]{{var0.ax, var0.ay, var0.az}, {var0.bx, var0.by, var0.bz}, {var0.cx, var0.cy, var0.cz}};
      ArrayList var8 = new ArrayList(2);

      for (int var9 = 0; var9 < 3; var9++) {
         double[] var10 = var7[var9];
         double[] var11 = var7[(var9 + 1) % 3];
         double var12 = (var4 ? var10[2] : var10[0]) - var5;
         double var14 = (var4 ? var11[2] : var11[0]) - var5;
         if (var12 < 0.0 && var14 >= 0.0 || var12 >= 0.0 && var14 < 0.0) {
            double var16 = var12 / (var12 - var14);
            double var18 = var4 ? var10[0] + (var11[0] - var10[0]) * var16 : var10[2] + (var11[2] - var10[2]) * var16;
            double var20 = var10[1] + (var11[1] - var10[1]) * var16;
            var8.add(new double[]{var18, var20});
         }
      }

      if (var8.size() < 2) {
         return null;
      }

      double[] var22 = (double[])var8.get(0);
      double[] var23 = (double[])var8.get(1);
      double var24 = toU(var1, var2, var3, var22[0]);
      double var13 = toU(var1, var2, var3, var23[0]);
      return var24 > var13 ? new double[]{var13, var23[1], var24, var22[1], var0.material} : new double[]{var24, var22[1], var13, var23[1], var0.material};
   }

   private static double toU(Direction var0, int var1, int var2, double var3) {
      return switch (var0) {
         case NORTH -> var3 - var1;
         case SOUTH -> var1 + 1 - var3;
         case WEST -> var2 + 1 - var3;
         default -> var3 - var2;
      };
   }

   private static double[] covering(List<double[]> var0, double var1) {
      double[] var3 = null;
      double var4 = Double.NEGATIVE_INFINITY;

      for (double[] var7 : var0) {
         if (var1 >= var7[0] - 1.0E-9 && var1 <= var7[2] + 1.0E-9 && var7[2] - var7[0] > 1.0E-9) {
            double var8 = at(var7, var1);
            if (var8 > var4) {
               var4 = var8;
               var3 = var7;
            }
         }
      }

      return var3;
   }

   private static double at(double[] var0, double var1) {
      return var0[1] + (var0[3] - var0[1]) * (var1 - var0[0]) / (var0[2] - var0[0]);
   }

   private static void landFlat(SkyDig.Probe var0, boolean var1, int var2, int var3, int var4, DigWalls.Faces var5, DigWalls.Emitter var6) {
      double var7 = var1 ? var3 + 1 : var3;
      Direction var9 = var1 ? Direction.UP : Direction.DOWN;
      Direction var10 = var1 ? Direction.DOWN : Direction.UP;

      for (SkyTri var12 : var0.land) {
         if (!(var12.maxX < var2)
            && !(var12.minX > var2 + 1)
            && !(var12.maxZ < var4)
            && !(var12.minZ > var4 + 1)
            && !(var12.maxY <= var7)
            && !(Math.abs(var12.ny) < 1.0E-6)) {
            List var13 = new ArrayList<>(
               List.of(new double[]{var12.ax, var12.az, var12.ay}, new double[]{var12.bx, var12.bz, var12.by}, new double[]{var12.cx, var12.cz, var12.cy})
            );
            var13 = clip(var13, var1x -> var1x[0] - var2);
            var13 = clip(var13, var1x -> var2 + 1 - var1x[0]);
            var13 = clip(var13, var1x -> var1x[1] - var4);
            var13 = clip(var13, var1x -> var4 + 1 - var1x[1]);
            var13 = clip(var13, var2x -> var2x[2] - var7);
            if (var13.size() >= 3) {
               if (!var6.texture(var5, var12.material, 0.5)) {
                  return;
               }

               ArrayList var14 = new ArrayList(var13.size());
               ArrayList var15 = new ArrayList(var13.size());

               for (double[] var17 : var13) {
                  var14.add(new double[]{var17[0], var7, var17[1]});
                  var15.add(new double[]{var17[0] - var2, var17[1] - var4});
               }

               var6.polygon(var14, var15, rectFor(var9), var10);
            }
         }
      }
   }

   private static List<double[]> clip(List<double[]> var0, ToDoubleFunction<double[]> var1) {
      ArrayList var2 = new ArrayList(var0.size() + 2);

      for (int var3 = 0; var3 < var0.size(); var3++) {
         double[] var4 = (double[])var0.get(var3);
         double[] var5 = (double[])var0.get((var3 + 1) % var0.size());
         double var6 = var1.applyAsDouble(var4);
         double var8 = var1.applyAsDouble(var5);
         if (var6 >= 0.0) {
            var2.add(var4);
         }

         if (var6 >= 0.0 != var8 >= 0.0) {
            double var10 = var6 / (var6 - var8);
            var2.add(new double[]{var4[0] + (var5[0] - var4[0]) * var10, var4[1] + (var5[1] - var4[1]) * var10, var4[2] + (var5[2] - var4[2]) * var10});
         }
      }

      return var2;
   }

   private static void sampled(SkyDig.Probe var0, Direction var1, int var2, int var3, int var4, DigWalls.Faces var5, DigWalls.Emitter var6) {
      double var7 = var1.getStepX() * 0.02;
      double var9 = var1.getStepY() * 0.02;
      double var11 = var1.getStepZ() * 0.02;
      double var13 = 0.25;
      Direction var15 = var1.getOpposite();

      for (int var16 = 0; var16 < 4; var16++) {
         for (int var17 = 0; var17 < 4; var17++) {
            double var18 = (var16 + 0.5) * var13;
            double var20 = (var17 + 0.5) * var13;
            double[] var22 = onFace(var1, var2, var3, var4, var18, var20);
            double var23 = var22[0] + var7;
            double var25 = var22[1] + var9;
            double var27 = var22[2] + var11;
            double var29 = var0.landHeight(var23, var27);
            if (Double.isNaN(var29) || !(var25 < var29)) {
               int var31 = var0.test(var23, var25, var27);
               if (var31 > 0 && var6.texture(var5, var31, var0.depth)) {
                  double var32 = var16 * var13;
                  double var34 = var32 + var13;
                  double var36 = var17 * var13;
                  double var38 = var36 + var13;
                  boolean var40 = var1.getAxis() == Axis.Y;
                  List var41 = List.of(
                     onFace(var1, var2, var3, var4, var32, var36),
                     onFace(var1, var2, var3, var4, var34, var36),
                     onFace(var1, var2, var3, var4, var34, var38),
                     onFace(var1, var2, var3, var4, var32, var38)
                  );
                  List var42 = var40
                     ? List.of(new double[]{var32, var36}, new double[]{var34, var36}, new double[]{var34, var38}, new double[]{var32, var38})
                     : List.of(
                        new double[]{var32, 1.0 - var36}, new double[]{var34, 1.0 - var36}, new double[]{var34, 1.0 - var38}, new double[]{var32, 1.0 - var38}
                     );
                  var6.polygon(var41, var42, rectFor(var1), var15);
               }
            }
         }
      }
   }

   private static final class Emitter {
      final int sx;
      final int sy;
      final int sz;
      final DigWalls.Out out;
      int light;
      float[] rects;
      int material;
      final float[] xyz = new float[12];
      final float[] uv = new float[8];

      Emitter(int var1, int var2, int var3, DigWalls.Out var4) {
         this.sx = var1;
         this.sy = var2;
         this.sz = var3;
         this.out = var4;
      }

      boolean texture(DigWalls.Faces var1, int var2, double var3) {
         if (this.rects == null) {
            this.material = var2;
            this.rects = var1.of(SkyDig.materialState(SkyDig.underground(var2, Math.max(var3, 0.5))));
         }

         return this.rects != null;
      }

      void polygon(List<double[]> var1, List<double[]> var2, int var3, Direction var4) {
         for (byte var5 = 1; var5 + 1 < var1.size(); var5 += 2) {
            byte var6 = 0;
            byte var7 = var5;
            int var8 = var5 + 1;
            int var9 = Math.min(var5 + 2, var1.size() - 1);
            int[] var10 = new int[]{var6, var7, var8, var9};

            for (int var11 = 0; var11 < 4; var11++) {
               double[] var12 = (double[])var1.get(var10[var11]);
               double[] var13 = (double[])var2.get(var10[var11]);
               this.xyz[var11 * 3] = (float)(var12[0] - this.sx * 16);
               this.xyz[var11 * 3 + 1] = (float)(var12[1] - this.sy * 16);
               this.xyz[var11 * 3 + 2] = (float)(var12[2] - this.sz * 16);
               this.uv[var11 * 2] = this.rects[var3] + (float)var13[0] * (this.rects[var3 + 2] - this.rects[var3]);
               this.uv[var11 * 2 + 1] = this.rects[var3 + 1] + (float)var13[1] * (this.rects[var3 + 3] - this.rects[var3 + 1]);
            }

            this.out.quad(this.xyz, this.uv, this.light, var4);
         }
      }
   }

   interface Faces {
      float[] of(BlockState var1);
   }

   interface Out {
      void quad(float[] var1, float[] var2, int var3, Direction var4);
   }
}
