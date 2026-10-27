package dev.skycraft.world;

import dev.skycraft.SkyCraft;
import dev.skycraft.link.SkyLink;
import java.lang.foreign.MemorySegment;
import java.lang.foreign.ValueLayout;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Set;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentLinkedQueue;
import java.util.function.LongConsumer;
import java.util.function.Predicate;
import net.minecraft.core.BlockPos;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.phys.AABB;
import net.minecraft.world.phys.shapes.BitSetDiscreteVoxelShape;
import net.minecraft.world.phys.shapes.CubeVoxelShape;
import net.minecraft.world.phys.shapes.Shapes;
import net.minecraft.world.phys.shapes.VoxelShape;
import org.jspecify.annotations.Nullable;

public final class SkyCollision {
   public static final int REGION_SIZE = 8;
   private static final ConcurrentHashMap<Long, VoxelShape> SHAPES = new ConcurrentHashMap<>();
   private static final ConcurrentHashMap<Long, Integer> FILL = new ConcurrentHashMap<>();
   private static final int FILL_LOWER = 1024;
   private static final int FILL_UPPER = 2048;
   private static final int FILL_TOP_SHIFT = 12;
   private static final ConcurrentHashMap<Long, SkyTri[]> TRIS = new ConcurrentHashMap<>();
   private static final ConcurrentHashMap<Long, SkyTri[]> GHOSTS = new ConcurrentHashMap<>();
   private static final ConcurrentHashMap<Long, Long> TRI_HASH = new ConcurrentHashMap<>();
   private static final ConcurrentLinkedQueue<Long> CHANGED = new ConcurrentLinkedQueue<>();
   private static volatile Predicate<Entity> smoothCollider = var0 -> false;
   private static final Set<Long> KNOWN_REGIONS = ConcurrentHashMap.newKeySet();
   private static volatile int epoch = -1;
   private static Thread consumer;

   public static void takeChangedRegions(LongConsumer var0) {
      Long var1;
      while ((var1 = CHANGED.poll()) != null) {
         var0.accept(var1);
      }
   }

   private SkyCollision() {
   }

   public static @Nullable VoxelShape shapeAt(BlockPos var0) {
      return SHAPES.isEmpty() ? null : SHAPES.get(var0.asLong());
   }

   public static void setSmoothCollider(Predicate<Entity> var0) {
      smoothCollider = var0;
   }

   public static boolean usesSmoothCollider(@Nullable Entity var0) {
      return var0 != null && smoothCollider.test(var0);
   }

   public static void trianglesNear(AABB var0, List<SkyTri> var1) {
      if (!TRIS.isEmpty()) {
         int var2 = Math.floorDiv((int)Math.floor(var0.minX), 8);
         int var3 = Math.floorDiv((int)Math.floor(var0.maxX), 8);
         int var4 = Math.floorDiv((int)Math.floor(var0.minY), 8);
         int var5 = Math.floorDiv((int)Math.floor(var0.maxY), 8);
         int var6 = Math.floorDiv((int)Math.floor(var0.minZ), 8);
         int var7 = Math.floorDiv((int)Math.floor(var0.maxZ), 8);

         for (int var8 = var2; var8 <= var3; var8++) {
            for (int var9 = var4; var9 <= var5; var9++) {
               for (int var10 = var6; var10 <= var7; var10++) {
                  SkyTri[] var11 = TRIS.get(regionKey(var8, var9, var10));
                  if (var11 != null) {
                     for (SkyTri var15 : var11) {
                        if (var15.maxX >= var0.minX
                           && var15.minX <= var0.maxX
                           && var15.maxY >= var0.minY
                           && var15.minY <= var0.maxY
                           && var15.maxZ >= var0.minZ
                           && var15.minZ <= var0.maxZ) {
                           var1.add(var15);
                        }
                     }
                  }
               }
            }
         }
      }
   }

   public static void originalSurfacesNear(AABB var0, List<SkyTri> var1) {
      trianglesNear(var0, var1);
      near(GHOSTS, var0, var1);
   }

   private static void near(ConcurrentHashMap<Long, SkyTri[]> var0, AABB var1, List<SkyTri> var2) {
      if (!var0.isEmpty()) {
         int var3 = Math.floorDiv((int)Math.floor(var1.minX), 8);
         int var4 = Math.floorDiv((int)Math.floor(var1.maxX), 8);
         int var5 = Math.floorDiv((int)Math.floor(var1.minY), 8);
         int var6 = Math.floorDiv((int)Math.floor(var1.maxY), 8);
         int var7 = Math.floorDiv((int)Math.floor(var1.minZ), 8);
         int var8 = Math.floorDiv((int)Math.floor(var1.maxZ), 8);

         for (int var9 = var3; var9 <= var4; var9++) {
            for (int var10 = var5; var10 <= var6; var10++) {
               for (int var11 = var7; var11 <= var8; var11++) {
                  SkyTri[] var12 = (SkyTri[])var0.get(regionKey(var9, var10, var11));
                  if (var12 != null) {
                     for (SkyTri var16 : var12) {
                        if (var16.maxX >= var1.minX
                           && var16.minX <= var1.maxX
                           && var16.maxY >= var1.minY
                           && var16.minY <= var1.maxY
                           && var16.maxZ >= var1.minZ
                           && var16.minZ <= var1.maxZ) {
                           var2.add(var16);
                        }
                     }
                  }
               }
            }
         }
      }
   }

   public static boolean isKnown(int var0, int var1, int var2) {
      return KNOWN_REGIONS.contains(regionKey(Math.floorDiv(var0, 8), Math.floorDiv(var1, 8), Math.floorDiv(var2, 8)));
   }

   public static boolean hasSolidBelow(int var0, int var1, int var2, int var3) {
      for (int var4 = 0; var4 <= var3; var4++) {
         for (int var5 = -1; var5 <= 1; var5++) {
            for (int var6 = -1; var6 <= 1; var6++) {
               if (SHAPES.containsKey(BlockPos.asLong(var0 + var5, var1 - var4, var2 + var6))) {
                  return true;
               }
            }
         }
      }

      return false;
   }

   public static float solidFraction(BlockPos var0) {
      Integer var1 = FILL.isEmpty() ? null : FILL.get(var0.asLong());
      return var1 == null ? 0.0F : (var1 & 1023) / 512.0F;
   }

   public static boolean hasGeometry(BlockPos var0) {
      return !FILL.isEmpty() && FILL.containsKey(var0.asLong());
   }

   public static float groundTop(BlockPos var0) {
      Integer var1 = FILL.isEmpty() ? null : FILL.get(var0.asLong());
      return var1 == null ? 0.0F : ((var1 >> 12 & 7) + 1) / 8.0F;
   }

   public static boolean supportsFromBelow(BlockPos var0) {
      if (FILL.isEmpty()) {
         return false;
      }

      Integer var1 = FILL.get(var0.asLong());
      if (var1 != null && (var1 & 1024) != 0) {
         return true;
      }

      Integer var2 = FILL.get(BlockPos.asLong(var0.getX(), var0.getY() - 1, var0.getZ()));
      return var2 != null && (var2 & 2048) != 0;
   }

   public static int blockCount() {
      return SHAPES.size();
   }

   public static int regionCount() {
      return KNOWN_REGIONS.size();
   }

   public static boolean active() {
      return !KNOWN_REGIONS.isEmpty();
   }

   private static long regionKey(int var0, int var1, int var2) {
      return BlockPos.asLong(var0, var1, var2);
   }

   public static synchronized void startConsumer() {
      if (consumer == null) {
         consumer = new Thread(SkyCollision::consumeLoop, "SkyCraft collision");
         consumer.setDaemon(true);
         consumer.start();
      }
   }

   private static void consumeLoop() {
      while (true) {
         try {
            if (!drainOnce()) {
               Thread.sleep(2L);
            }
         } catch (InterruptedException var3) {
            return;
         } catch (Throwable var4) {
            SkyCraft.LOG.error("SkyCraft: collision consumer error", var4);

            try {
               Thread.sleep(500L);
            } catch (InterruptedException var2) {
               return;
            }
         }
      }
   }

   private static boolean drainOnce() {
      MemorySegment var0 = SkyLink.segment();
      if (var0 == null) {
         return false;
      }

      long var1 = SkyLink.collisionHead();
      long var3 = SkyLink.collisionTail();
      if (var3 >= var1) {
         return false;
      }

      long var5 = 131200L;

      while (var3 < var1) {
         long var7 = var3 % 33554304L;
         int var9 = var0.get(ValueLayout.JAVA_INT, var5 + var7);
         int var10 = var0.get(ValueLayout.JAVA_INT, var5 + var7 + 4L);
         if (var9 == 0) {
            var3 += 33554304L - var7;
         } else {
            long var11 = var5 + var7 + 8L;
            switch (var9) {
               case 1:
                  clear(var0.get(ValueLayout.JAVA_INT, var11));
                  break;
               case 2:
                  readRegion(var0, var11);
                  break;
               case 3:
                  readTris(var0, var11);
                  break;
               default:
                  SkyCraft.LOG.warn("SkyCraft: unknown collision message {}", var9);
            }

            var3 += align8(8 + var10);
         }
      }

      SkyLink.setCollisionTail(var3);
      return true;
   }

   private static long align8(long var0) {
      return var0 + 7L & -8L;
   }

   private static void adoptEpochIfFresh(int var0) {
      if (epoch == -1) {
         epoch = var0;
         SkyCraft.LOG.info("SkyCraft: joined collision epoch {} already in progress", var0);
      }
   }

   private static void clear(int var0) {
      SHAPES.clear();
      FILL.clear();
      TRIS.clear();
      GHOSTS.clear();
      TRI_HASH.clear();
      KNOWN_REGIONS.clear();
      epoch = var0;
      SkyCraft.LOG.info("SkyCraft: collision cleared (epoch {})", var0);
   }

   private static void readRegion(MemorySegment var0, long var1) {
      int var3 = var0.get(ValueLayout.JAVA_INT, var1);
      int var4 = var0.get(ValueLayout.JAVA_INT, var1 + 4L);
      int var5 = var0.get(ValueLayout.JAVA_INT, var1 + 8L);
      int var6 = var0.get(ValueLayout.JAVA_INT, var1 + 12L);
      int var7 = var0.get(ValueLayout.JAVA_INT, var1 + 16L);
      int var8 = var0.get(ValueLayout.JAVA_INT, var1 + 20L);
      int var9 = var0.get(ValueLayout.JAVA_INT, var1 + 24L);
      int var10 = var0.get(ValueLayout.JAVA_INT, var1 + 28L);
      adoptEpochIfFresh(var9);
      if (var9 == epoch) {
         HashMap var11 = new HashMap(var10 * 2);
         HashMap var12 = new HashMap(var10 * 2);
         long var13 = var1 + 32L;

         for (int var15 = 0; var15 < var10; var13 += 80L) {
            int var16 = var0.get(ValueLayout.JAVA_INT, var13);
            int var17 = var0.get(ValueLayout.JAVA_INT, var13 + 4L);
            int var18 = var0.get(ValueLayout.JAVA_INT, var13 + 8L);
            VoxelShape var19 = buildShape(var0, var13 + 16L);
            if (var19 != null) {
               long var20 = BlockPos.asLong(var16, var17, var18);
               var11.put(var20, var19);
               var12.put(var20, fillInfo(var0, var13 + 16L));
            }

            var15++;
         }

         for (int var22 = var3; var22 <= var6; var22++) {
            for (int var24 = var4; var24 <= var7; var24++) {
               for (int var26 = var5; var26 <= var8; var26++) {
                  long var28 = BlockPos.asLong(var22, var24, var26);
                  VoxelShape var29 = (VoxelShape)var11.get(var28);
                  if (var29 != null) {
                     SHAPES.put(var28, var29);
                     FILL.put(var28, (Integer)var12.get(var28));
                  } else {
                     SHAPES.remove(var28);
                     FILL.remove(var28);
                  }
               }
            }
         }

         for (int var23 = Math.floorDiv(var3, 8); var23 <= Math.floorDiv(var6, 8); var23++) {
            for (int var25 = Math.floorDiv(var4, 8); var25 <= Math.floorDiv(var7, 8); var25++) {
               for (int var27 = Math.floorDiv(var5, 8); var27 <= Math.floorDiv(var8, 8); var27++) {
                  KNOWN_REGIONS.add(regionKey(var23, var25, var27));
               }
            }
         }
      }
   }

   private static void readTris(MemorySegment var0, long var1) {
      int var3 = var0.get(ValueLayout.JAVA_INT, var1);
      int var4 = var0.get(ValueLayout.JAVA_INT, var1 + 4L);
      int var5 = var0.get(ValueLayout.JAVA_INT, var1 + 8L);
      int var6 = var0.get(ValueLayout.JAVA_INT, var1 + 24L);
      int var7 = var0.get(ValueLayout.JAVA_INT, var1 + 28L);
      adoptEpochIfFresh(var6);
      if (var6 == epoch) {
         SkyTri[] var8 = new SkyTri[var7];
         ArrayList var9 = new ArrayList();
         float[] var10 = new float[9];
         int var11 = 0;
         long var12 = var7;
         long var14 = var1 + 32L;

         for (int var16 = 0; var16 < var7; var14 += 40L) {
            for (int var17 = 0; var17 < 9; var17++) {
               var10[var17] = var0.get(ValueLayout.JAVA_FLOAT, var14 + var17 * 4L);
               var12 = var12 * 31L + Float.floatToRawIntBits(var10[var17]);
            }

            int var20 = var0.get(ValueLayout.JAVA_INT, var14 + 36L);
            var12 = var12 * 31L + var20;
            SkyTri var18 = new SkyTri(var10, 0, var20);
            if (!var18.degenerate()) {
               if ((var20 & 4) != 0) {
                  var9.add(var18);
               } else {
                  var8[var11++] = var18;
               }
            }

            var16++;
         }

         long var19 = regionKey(Math.floorDiv(var3, 8), Math.floorDiv(var4, 8), Math.floorDiv(var5, 8));
         if (var9.isEmpty()) {
            GHOSTS.remove(var19);
         } else {
            GHOSTS.put(var19, var9.toArray(new SkyTri[0]));
         }

         TRIS.put(var19, Arrays.copyOf(var8, var11));
         Long var21 = TRI_HASH.put(var19, var12);
         if (var21 == null || var21 != var12) {
            CHANGED.add(BlockPos.asLong(var3, var4, var5));
         }
      }
   }

   public static int triangleCount() {
      int var0 = 0;

      for (SkyTri[] var2 : TRIS.values()) {
         var0 += var2.length;
      }

      return var0;
   }

   private static int fillInfo(MemorySegment var0, long var1) {
      int var3 = 0;
      byte var4 = 0;
      int var5 = 0;

      for (int var6 = 0; var6 < 8; var6++) {
         long var7 = var0.get(ValueLayout.JAVA_LONG, var1 + var6 * 8L);
         var3 += Long.bitCount(var7);
         if (var7 != 0L) {
            var4 |= var6 < 4 ? 1024 : 2048;
            var5 = var6;
         }
      }

      return var4 | var3 | var5 << 12;
   }

   private static @Nullable VoxelShape buildShape(MemorySegment var0, long var1) {
      boolean var3 = false;
      boolean var4 = true;
      long[] var5 = new long[8];

      for (int var6 = 0; var6 < 8; var6++) {
         var5[var6] = var0.get(ValueLayout.JAVA_LONG, var1 + var6 * 8L);
         var3 |= var5[var6] != 0L;
         var4 &= var5[var6] == -1L;
      }

      if (!var3) {
         return null;
      }

      if (var4) {
         return Shapes.block();
      }

      BitSetDiscreteVoxelShape var11 = new BitSetDiscreteVoxelShape(8, 8, 8);

      for (int var7 = 0; var7 < 8; var7++) {
         long var8 = var5[var7];

         while (var8 != 0L) {
            int var10 = Long.numberOfTrailingZeros(var8);
            var8 &= var8 - 1L;
            var11.fill(var10 & 7, var7, var10 >>> 3);
         }
      }

      return new CubeVoxelShape(var11);
   }
}
