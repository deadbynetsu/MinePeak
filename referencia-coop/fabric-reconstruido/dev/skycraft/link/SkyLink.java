package dev.skycraft.link;

import dev.skycraft.SkyCraft;
import java.lang.foreign.Arena;
import java.lang.foreign.FunctionDescriptor;
import java.lang.foreign.Linker;
import java.lang.foreign.MemorySegment;
import java.lang.foreign.StructLayout;
import java.lang.foreign.SymbolLookup;
import java.lang.foreign.ValueLayout;
import java.lang.foreign.Linker.Option;
import java.lang.foreign.MemoryLayout.PathElement;
import java.lang.invoke.MethodHandle;
import java.lang.invoke.VarHandle;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.util.List;

public final class SkyLink {
   private static final int FILE_MAP_ALL_ACCESS = 983071;
   private static final long HEARTBEAT_TIMEOUT_MS = 8000L;
   private static final VarHandle INT = ValueLayout.JAVA_INT.varHandle();
   private static final VarHandle LONG = ValueLayout.JAVA_LONG.varHandle();
   private static final MethodHandle OPEN_FILE_MAPPING;
   private static final StructLayout CALL_STATE = Option.captureStateLayout();
   private static final VarHandle LAST_ERROR = CALL_STATE.varHandle(PathElement.groupElement("GetLastError"));
   private static final MemorySegment OPEN_STATE = Arena.global().allocate(CALL_STATE);
   private static int lastOpenError = -1;
   private static final MethodHandle MAP_VIEW_OF_FILE;
   private static final MethodHandle GET_TICK_COUNT64;
   private static final MethodHandle GET_CURRENT_PROCESS_ID;
   private static final MethodHandle QUERY_PERFORMANCE_COUNTER;
   private static final MethodHandle QUERY_PERFORMANCE_FREQUENCY;
   private static final MethodHandle CREATE_MUTEX;
   private static MemorySegment runningMutex;
   private static final MemorySegment QPC_OUT = Arena.global().allocate(ValueLayout.JAVA_LONG);
   private static volatile MemorySegment shm;
   private static long lastOpenAttempt;
   private static int skyrimPid;
   private static volatile int generation;
   private static int overlayBack = 1;

   public static synchronized void announceRunning() {
      if (runningMutex == null) {
         try (Arena var0 = Arena.ofConfined()) {
            MemorySegment var1 = var0.allocateFrom(Proto.MAPPING_NAME + "_minecraft", StandardCharsets.UTF_16LE);
            runningMutex = (MemorySegment)CREATE_MUTEX.invokeExact((MemorySegment)MemorySegment.NULL, (int)0, (MemorySegment)var1);
         } catch (Throwable var5) {
            SkyCraft.LOG.warn("SkyCraft: couldn't create the running-Minecraft mutex", var5);
         }
      }
   }

   private SkyLink() {
   }

   public static boolean active() {
      MemorySegment var0 = shm;
      if (var0 == null) {
         return false;
      }

      long var1 = (long)LONG.getAcquire((MemorySegment)var0, (long)16L);
      return tickCount() - var1 < 8000L;
   }

   public static int generation() {
      return generation;
   }

   public static int skyrimPid() {
      return skyrimPid;
   }

   public static MemorySegment segment() {
      return shm;
   }

   public static void poll() {
      if (shm != null) {
         LONG.setRelease((MemorySegment)shm, (long)24L, (long)tickCount());
         int var12 = shm.get(ValueLayout.JAVA_INT, 8L);
         if (var12 != skyrimPid) {
            skyrimPid = var12;
            overlayBack = 1;
            generation++;
            SkyCraft.LOG.info("SkyCraft: Skyrim instance changed (pid {})", var12);
         }
      } else {
         long var0 = System.currentTimeMillis();
         if (var0 - lastOpenAttempt >= 1000L) {
            lastOpenAttempt = var0;

            try (Arena var2 = Arena.ofConfined()) {
               MemorySegment var3 = var2.allocateFrom(Proto.MAPPING_NAME, StandardCharsets.UTF_16LE);
               MemorySegment var4 = (MemorySegment)OPEN_FILE_MAPPING.invokeExact((MemorySegment)OPEN_STATE, (int)983071, (int)0, (MemorySegment)var3);
               if (var4.address() == 0L) {
                  int var13 = (int)LAST_ERROR.get((MemorySegment)OPEN_STATE, (long)0L);
                  if (var13 != lastOpenError) {
                     lastOpenError = var13;
                     SkyCraft.LOG
                        .info(
                           "SkyCraft: can't open Skyrim's shared memory yet (Windows error {}{})",
                           var13,
                           var13 == 2 ? ": Skyrim hasn't created it yet" : (var13 == 5 ? ": access denied; is Skyrim running as administrator?" : "")
                        );
                  }
               } else {
                  MemorySegment var5 = (MemorySegment)MAP_VIEW_OF_FILE.invokeExact((MemorySegment)var4, (int)983071, (int)0, (int)0, (long)0L);
                  if (var5.address() == 0L) {
                     SkyCraft.LOG.error("SkyCraft: MapViewOfFile failed");
                  } else {
                     MemorySegment var6 = var5.reinterpret(200327168L);
                     int var7 = var6.get(ValueLayout.JAVA_INT, 0L);
                     int var8 = var6.get(ValueLayout.JAVA_INT, 4L);
                     if (var7 == 1129925459 && var8 == 11) {
                        var6.set(ValueLayout.JAVA_INT, 12L, (int)GET_CURRENT_PROCESS_ID.invokeExact());
                        LONG.setRelease((MemorySegment)var6, (long)24L, (long)tickCount());
                        skyrimPid = var6.get(ValueLayout.JAVA_INT, 8L);
                        generation++;
                        shm = var6;
                        SkyCraft.LOG.info("SkyCraft: linked to Skyrim (pid {})", var6.get(ValueLayout.JAVA_INT, 8L));
                     } else {
                        SkyCraft.LOG
                           .error("SkyCraft: protocol mismatch (magic {} version {}); expected version {}", new Object[]{Integer.toHexString(var7), var8, 11});
                     }
                  }
               }
            } catch (Throwable var11) {
               SkyCraft.LOG.error("SkyCraft: failed to open shared memory", var11);
            }
         }
      }
   }

   public static synchronized long qpc() {
      try {
         int var0 = (int)QUERY_PERFORMANCE_COUNTER.invokeExact((MemorySegment)QPC_OUT);
         return QPC_OUT.get(ValueLayout.JAVA_LONG, 0L);
      } catch (Throwable var1) {
         throw new RuntimeException(var1);
      }
   }

   public static synchronized long qpcFrequency() {
      try {
         int var0 = (int)QUERY_PERFORMANCE_FREQUENCY.invokeExact((MemorySegment)QPC_OUT);
         return QPC_OUT.get(ValueLayout.JAVA_LONG, 0L);
      } catch (Throwable var1) {
         throw new RuntimeException(var1);
      }
   }

   public static long tickCount() {
      try {
         return (long)GET_TICK_COUNT64.invokeExact();
      } catch (Throwable var1) {
         throw new RuntimeException(var1);
      }
   }

   public static SkyLink.WaterGrid readWaterGrid() {
      MemorySegment var0 = shm;
      if (var0 == null) {
         return null;
      }

      long var1 = 1024L;
      SkyLink.WaterGrid var3 = new SkyLink.WaterGrid();

      for (int var4 = 0; var4 < 100; var4++) {
         int var5 = (int)INT.getAcquire((MemorySegment)var0, (long)(var1 + 0L));
         if ((var5 & 1) == 0 && var5 != 0) {
            var3.originX = var0.get(ValueLayout.JAVA_INT, var1 + 4L);
            var3.originZ = var0.get(ValueLayout.JAVA_INT, var1 + 8L);
            var3.worldId = var0.get(ValueLayout.JAVA_INT, var1 + 12L);

            for (int var6 = 0; var6 < var3.surface.length; var6++) {
               var3.surface[var6] = var0.get(ValueLayout.JAVA_FLOAT, var1 + 16L + var6 * 4L);
            }

            VarHandle.loadLoadFence();
            if ((int)INT.getAcquire((MemorySegment)var0, (long)(var1 + 0L)) == var5) {
               return var3;
            }
         } else {
            Thread.onSpinWait();
            if (var5 == 0) {
               return null;
            }
         }
      }

      return null;
   }

   public static boolean readSkyState(SkyLink.SkyState var0) {
      MemorySegment var1 = shm;
      if (var1 == null) {
         return false;
      }

      long var2 = 256L;

      for (int var4 = 0; var4 < 1000; var4++) {
         int var5 = (int)INT.getAcquire((MemorySegment)var1, (long)(var2 + 0L));
         if ((var5 & 1) != 0) {
            if (var4 > 100) {
               Thread.yield();
            } else {
               Thread.onSpinWait();
            }
         } else {
            var0.flags = var1.get(ValueLayout.JAVA_INT, var2 + 4L);
            var0.worldId = var1.get(ValueLayout.JAVA_INT, var2 + 8L);
            var0.collisionEpoch = var1.get(ValueLayout.JAVA_INT, var2 + 12L);
            var0.x = var1.get(ValueLayout.JAVA_DOUBLE, var2 + 16L);
            var0.y = var1.get(ValueLayout.JAVA_DOUBLE, var2 + 24L);
            var0.z = var1.get(ValueLayout.JAVA_DOUBLE, var2 + 32L);
            var0.yaw = var1.get(ValueLayout.JAVA_FLOAT, var2 + 40L);
            var0.pitch = var1.get(ValueLayout.JAVA_FLOAT, var2 + 44L);
            var0.teleportSeq = var1.get(ValueLayout.JAVA_INT, var2 + 48L);
            var0.viewportW = var1.get(ValueLayout.JAVA_INT, var2 + 52L);
            var0.viewportH = var1.get(ValueLayout.JAVA_INT, var2 + 56L);
            var0.gameHour = var1.get(ValueLayout.JAVA_FLOAT, var2 + 60L);
            VarHandle.loadLoadFence();
            int var6 = (int)INT.getAcquire((MemorySegment)var1, (long)(var2 + 0L));
            if (var5 == var6) {
               var0.seq = var5;
               return true;
            }
         }
      }

      return false;
   }

   public static int skyStateSeq() {
      MemorySegment var0 = shm;
      return var0 == null ? 0 : (int)INT.getAcquire((MemorySegment)var0, (long)256L);
   }

   public static void writeMcState(SkyLink.McState var0) {
      MemorySegment var1 = shm;
      if (var1 != null) {
         long var2 = 512L;
         int var4 = var1.get(ValueLayout.JAVA_INT, var2 + 0L);
         INT.setRelease((MemorySegment)var1, (long)(var2 + 0L), (int)(var4 + 1));
         VarHandle.storeStoreFence();
         var1.set(ValueLayout.JAVA_INT, var2 + 4L, var0.flags);
         var1.set(ValueLayout.JAVA_DOUBLE, var2 + 8L, var0.x);
         var1.set(ValueLayout.JAVA_DOUBLE, var2 + 16L, var0.y);
         var1.set(ValueLayout.JAVA_DOUBLE, var2 + 24L, var0.z);
         var1.set(ValueLayout.JAVA_FLOAT, var2 + 32L, var0.yaw);
         var1.set(ValueLayout.JAVA_FLOAT, var2 + 36L, var0.pitch);
         var1.set(ValueLayout.JAVA_FLOAT, var2 + 40L, var0.eyeHeight);
         var1.set(ValueLayout.JAVA_FLOAT, var2 + 44L, var0.sensitivity);
         var1.set(ValueLayout.JAVA_INT, var2 + 48L, var0.teleportAck);
         var1.set(ValueLayout.JAVA_INT, var2 + 52L, var0.guiScale);
         var1.set(ValueLayout.JAVA_LONG, var2 + 56L, var0.frameCounter);
         var1.set(ValueLayout.JAVA_FLOAT, var2 + 64L, var0.fov);
         var1.set(ValueLayout.JAVA_FLOAT, var2 + 68L, var0.bobPhase);
         var1.set(ValueLayout.JAVA_FLOAT, var2 + 72L, var0.bobAmount);
         var1.set(ValueLayout.JAVA_DOUBLE, var2 + 80L, var0.eyeX);
         var1.set(ValueLayout.JAVA_DOUBLE, var2 + 88L, var0.eyeY);
         var1.set(ValueLayout.JAVA_DOUBLE, var2 + 96L, var0.eyeZ);
         var1.set(ValueLayout.JAVA_LONG, var2 + 104L, var0.tickQpc);
         var1.set(ValueLayout.JAVA_DOUBLE, var2 + 112L, var0.prevX);
         var1.set(ValueLayout.JAVA_DOUBLE, var2 + 112L + 8L, var0.prevY);
         var1.set(ValueLayout.JAVA_DOUBLE, var2 + 112L + 16L, var0.prevZ);
         var1.set(ValueLayout.JAVA_DOUBLE, var2 + 136L, var0.curX);
         var1.set(ValueLayout.JAVA_DOUBLE, var2 + 136L + 8L, var0.curY);
         var1.set(ValueLayout.JAVA_DOUBLE, var2 + 136L + 16L, var0.curZ);
         var1.set(ValueLayout.JAVA_FLOAT, var2 + 160L, var0.eyeHeightO);
         var1.set(ValueLayout.JAVA_FLOAT, var2 + 164L, var0.eyeHeightT);
         var1.set(ValueLayout.JAVA_FLOAT, var2 + 168L, var0.walkDistO);
         var1.set(ValueLayout.JAVA_FLOAT, var2 + 172L, var0.walkDist);
         var1.set(ValueLayout.JAVA_FLOAT, var2 + 176L, var0.bobO);
         var1.set(ValueLayout.JAVA_FLOAT, var2 + 180L, var0.bob);
         var1.set(ValueLayout.JAVA_FLOAT, var2 + 184L, var0.tickMs);
         var1.set(ValueLayout.JAVA_INT, var2 + 192L, var0.cameraMode);
         var1.set(ValueLayout.JAVA_FLOAT, var2 + 196L, var0.cameraDistance);
         INT.setRelease((MemorySegment)var1, (long)(var2 + 0L), (int)(var4 + 2));
      }
   }

   public static void drainInput(SkyLink.InputSink var0) {
      MemorySegment var1 = shm;
      if (var1 != null) {
         long var2 = 4096L;
         long var4 = (long)LONG.getAcquire((MemorySegment)var1, (long)(var2 + 0L));
         long var6 = var1.get(ValueLayout.JAVA_LONG, var2 + 64L);
         if (var4 - var6 > 4096L) {
            var6 = var4 - 4096L;
         }

         while (var6 < var4) {
            long var8 = var2 + 128L + (var6 & 4095L) * 16L;
            int var10 = Short.toUnsignedInt(var1.get(ValueLayout.JAVA_SHORT, var8));
            int var11 = Short.toUnsignedInt(var1.get(ValueLayout.JAVA_SHORT, var8 + 2L));
            int var12 = var1.get(ValueLayout.JAVA_INT, var8 + 4L);
            int var13 = var1.get(ValueLayout.JAVA_INT, var8 + 8L);
            int var14 = var1.get(ValueLayout.JAVA_INT, var8 + 12L);
            var6++;
            var0.accept(var10, var11, var12, var13, var14);
         }

         LONG.setRelease((MemorySegment)var1, (long)(var2 + 64L), (long)var6);
      }
   }

   public static boolean readActors(List<SkyLink.Actor> var0) {
      var0.clear();
      MemorySegment var1 = shm;
      if (var1 == null) {
         return false;
      }

      long var2 = 73728L;

      for (int var4 = 0; var4 < 16; var4++) {
         int var5 = (int)INT.getAcquire((MemorySegment)var1, (long)(var2 + 0L));
         if ((var5 & 1) != 0) {
            Thread.onSpinWait();
         } else {
            int var6 = Math.min(var1.get(ValueLayout.JAVA_INT, var2 + 4L), 256);

            for (int var7 = 0; var7 < var6; var7++) {
               long var8 = var2 + 64L + var7 * 64L;
               var0.add(
                  new SkyLink.Actor(
                     var1.get(ValueLayout.JAVA_INT, var8),
                     var1.get(ValueLayout.JAVA_INT, var8 + 4L),
                     var1.get(ValueLayout.JAVA_FLOAT, var8 + 8L),
                     var1.get(ValueLayout.JAVA_FLOAT, var8 + 12L),
                     var1.get(ValueLayout.JAVA_FLOAT, var8 + 16L),
                     var1.get(ValueLayout.JAVA_FLOAT, var8 + 20L),
                     var1.get(ValueLayout.JAVA_FLOAT, var8 + 24L),
                     var1.get(ValueLayout.JAVA_FLOAT, var8 + 28L),
                     var1.get(ValueLayout.JAVA_FLOAT, var8 + 32L),
                     Short.toUnsignedInt(var1.get(ValueLayout.JAVA_SHORT, var8 + 36L)),
                     readName(var1, var8 + 40L, 24)
                  )
               );
            }

            VarHandle.loadLoadFence();
            if ((int)INT.getAcquire((MemorySegment)var1, (long)(var2 + 0L)) == var5) {
               return true;
            }

            var0.clear();
         }
      }

      return false;
   }

   private static String readName(MemorySegment var0, long var1, int var3) {
      byte[] var4 = new byte[var3];
      int var5 = 0;

      while (var5 < var3) {
         byte var6 = var0.get(ValueLayout.JAVA_BYTE, var1 + var5);
         if (var6 == 0) {
            break;
         }

         var4[var5++] = var6;
      }

      return new String(var4, 0, var5, StandardCharsets.UTF_8);
   }

   public static void pushEvent(int var0, int var1, float var2, float var3, float var4, float var5, int var6) {
      pushEvent(var0, var1, var2, var3, var4, var5, var6, 0);
   }

   public static synchronized void pushEvent(int var0, int var1, float var2, float var3, float var4, float var5, int var6, int var7) {
      MemorySegment var8 = shm;
      if (var8 != null) {
         long var9 = 94208L;
         long var11 = var8.get(ValueLayout.JAVA_LONG, var9 + 0L);
         long var13 = (long)LONG.getAcquire((MemorySegment)var8, (long)(var9 + 64L));
         if (var11 - var13 < 512L) {
            long var15 = var9 + 128L + (var11 & 511L) * 32L;
            var8.set(ValueLayout.JAVA_INT, var15, var0);
            var8.set(ValueLayout.JAVA_INT, var15 + 4L, var1);
            var8.set(ValueLayout.JAVA_FLOAT, var15 + 8L, var2);
            var8.set(ValueLayout.JAVA_FLOAT, var15 + 12L, var3);
            var8.set(ValueLayout.JAVA_FLOAT, var15 + 16L, var4);
            var8.set(ValueLayout.JAVA_FLOAT, var15 + 20L, var5);
            var8.set(ValueLayout.JAVA_INT, var15 + 24L, var6);
            var8.set(ValueLayout.JAVA_INT, var15 + 28L, var7);
            LONG.setRelease((MemorySegment)var8, (long)(var9 + 0L), (long)(var11 + 1L));
         }
      }
   }

   public static synchronized void pushEventRaw(int var0, int var1, int var2, int var3, int var4, int var5, int var6, int var7) {
      MemorySegment var8 = shm;
      if (var8 != null) {
         long var9 = 94208L;
         long var11 = var8.get(ValueLayout.JAVA_LONG, var9 + 0L);
         long var13 = (long)LONG.getAcquire((MemorySegment)var8, (long)(var9 + 64L));
         if (var11 - var13 < 512L) {
            long var15 = var9 + 128L + (var11 & 511L) * 32L;
            var8.set(ValueLayout.JAVA_INT, var15, var0);
            var8.set(ValueLayout.JAVA_INT, var15 + 4L, var1);
            var8.set(ValueLayout.JAVA_INT, var15 + 8L, var2);
            var8.set(ValueLayout.JAVA_INT, var15 + 12L, var3);
            var8.set(ValueLayout.JAVA_INT, var15 + 16L, var4);
            var8.set(ValueLayout.JAVA_INT, var15 + 20L, var5);
            var8.set(ValueLayout.JAVA_INT, var15 + 24L, var6);
            var8.set(ValueLayout.JAVA_INT, var15 + 28L, var7);
            LONG.setRelease((MemorySegment)var8, (long)(var9 + 0L), (long)(var11 + 1L));
         }
      }
   }

   public static void writeWorldEntities(List<SkyLink.WorldEntity> var0, float[] var1) {
      MemorySegment var2 = shm;
      if (var2 != null) {
         long var3 = 114688L;
         int var5 = var2.get(ValueLayout.JAVA_INT, var3 + 0L);
         INT.setRelease((MemorySegment)var2, (long)(var3 + 0L), (int)(var5 + 1));
         VarHandle.storeStoreFence();
         int var6 = Math.min(var0.size(), 160);
         var2.set(ValueLayout.JAVA_INT, var3 + 4L, var6);
         var2.set(ValueLayout.JAVA_INT, var3 + 8L, var1 != null ? 1 : 0);
         if (var1 != null) {
            for (int var7 = 0; var7 < 6; var7++) {
               var2.set(ValueLayout.JAVA_FLOAT, var3 + 12L + var7 * 4L, var1[var7]);
            }
         }

         for (int var12 = 0; var12 < var6; var12++) {
            SkyLink.WorldEntity var8 = (SkyLink.WorldEntity)var0.get(var12);
            long var9 = var3 + 64L + var12 * 96L;
            var2.set(ValueLayout.JAVA_INT, var9, var8.kind());
            var2.set(ValueLayout.JAVA_INT, var9 + 4L, var8.id());
            var2.set(ValueLayout.JAVA_FLOAT, var9 + 8L, var8.x());
            var2.set(ValueLayout.JAVA_FLOAT, var9 + 12L, var8.y());
            var2.set(ValueLayout.JAVA_FLOAT, var9 + 16L, var8.z());
            var2.set(ValueLayout.JAVA_FLOAT, var9 + 20L, var8.yaw());
            var2.set(ValueLayout.JAVA_FLOAT, var9 + 24L, var8.pitch());
            var2.set(ValueLayout.JAVA_FLOAT, var9 + 28L, var8.scale());

            for (int var11 = 0; var11 < 3; var11++) {
               var2.set(ValueLayout.JAVA_FLOAT, var9 + 32L + var11 * 4L, var8.ext() != null ? var8.ext()[var11] : 0.0F);
            }

            for (int var13 = 0; var13 < 12; var13++) {
               var2.set(ValueLayout.JAVA_FLOAT, var9 + 44L + var13 * 4L, var8.uv() != null && var13 < var8.uv().length ? var8.uv()[var13] : 0.0F);
            }

            var2.set(ValueLayout.JAVA_INT, var9 + 92L, var8.tint());
         }

         INT.setRelease((MemorySegment)var2, (long)(var3 + 0L), (int)(var5 + 2));
      }
   }

   public static boolean writeRender(int var0, ByteBuffer var1, ByteBuffer var2) {
      return writeRender(var0, var1, var2, 500);
   }

   public static boolean tryWriteRender(int var0, ByteBuffer var1, ByteBuffer var2) {
      return writeRender(var0, var1, var2, 1);
   }

   private static synchronized boolean writeRender(int var0, ByteBuffer var1, ByteBuffer var2, int var3) {
      MemorySegment var4 = shm;
      if (var4 == null) {
         return false;
      }

      int var5 = var1.remaining() + (var2 != null ? var2.remaining() : 0);
      long var6 = 8 + var5 + 7 & -8L;
      if (var6 > 33554368L) {
         SkyCraft.LOG.warn("SkyCraft: render message too large ({} bytes)", var6);
         return false;
      }

      long var8 = 133218304L;

      for (int var10 = 0; var10 < var3; var10++) {
         long var11 = var4.get(ValueLayout.JAVA_LONG, var8 + 0L);
         long var13 = (long)LONG.getAcquire((MemorySegment)var4, (long)(var8 + 64L));
         long var15 = var11 % 67108736L;
         long var17 = var15 + var6 > 67108736L ? 67108736L - var15 : 0L;
         if (67108736L - (var11 - var13) >= var6 + var17) {
            if (var17 > 0L) {
               var4.set(ValueLayout.JAVA_INT, var8 + 128L + var15, 0);
               var4.set(ValueLayout.JAVA_INT, var8 + 128L + var15 + 4L, 0);
               var11 += var17;
               var15 = 0L;
            }

            long var19 = var8 + 128L + var15;
            var4.set(ValueLayout.JAVA_INT, var19, var0);
            var4.set(ValueLayout.JAVA_INT, var19 + 4L, var5);
            MemorySegment.copy(MemorySegment.ofBuffer(var1), 0L, var4, var19 + 8L, var1.remaining());
            if (var2 != null && var2.remaining() > 0) {
               MemorySegment.copy(MemorySegment.ofBuffer(var2), 0L, var4, var19 + 8L + var1.remaining(), var2.remaining());
            }

            LONG.setRelease((MemorySegment)var4, (long)(var8 + 0L), (long)(var11 + var6));
            return true;
         }

         if (var10 + 1 >= var3) {
            break;
         }

         try {
            Thread.sleep(2L);
         } catch (InterruptedException var21) {
            return false;
         }
      }

      return false;
   }

   public static long overlayBackSlotOffset() {
      return 33685504L + overlayBack * 33177600L;
   }

   public static void publishOverlay(int var0, int var1, boolean var2, long var3) {
      MemorySegment var5 = shm;
      if (var5 != null) {
         long var6 = 832L + overlayBack * 64L;
         var5.set(ValueLayout.JAVA_INT, var6 + 0L, var0);
         var5.set(ValueLayout.JAVA_INT, var6 + 4L, var1);
         var5.set(ValueLayout.JAVA_INT, var6 + 8L, var2 ? 1 : 0);
         var5.set(ValueLayout.JAVA_LONG, var6 + 16L, var3);
         int var8 = (int)INT.getAndSet((MemorySegment)var5, (long)768L, (int)(overlayBack | 4));
         overlayBack = var8 & 3;
         LONG.getAndAdd((MemorySegment)var5, (long)776L, (long)1L);
      }
   }

   public static long collisionHead() {
      MemorySegment var0 = shm;
      return var0 == null ? 0L : (long)LONG.getAcquire((MemorySegment)var0, (long)131072L);
   }

   public static long collisionTail() {
      MemorySegment var0 = shm;
      return var0 == null ? 0L : var0.get(ValueLayout.JAVA_LONG, 131136L);
   }

   public static void setCollisionTail(long var0) {
      MemorySegment var2 = shm;
      if (var2 != null) {
         LONG.setRelease((MemorySegment)var2, (long)131136L, (long)var0);
      }
   }

   static {
      Linker var0 = Linker.nativeLinker();
      SymbolLookup var1 = SymbolLookup.libraryLookup("kernel32", Arena.global());
      OPEN_FILE_MAPPING = var0.downcallHandle(
         var1.find("OpenFileMappingW").orElseThrow(),
         FunctionDescriptor.of(ValueLayout.ADDRESS, ValueLayout.JAVA_INT, ValueLayout.JAVA_INT, ValueLayout.ADDRESS),
         Option.captureCallState("GetLastError")
      );
      MAP_VIEW_OF_FILE = var0.downcallHandle(
         var1.find("MapViewOfFile").orElseThrow(),
         FunctionDescriptor.of(
            ValueLayout.ADDRESS, ValueLayout.ADDRESS, ValueLayout.JAVA_INT, ValueLayout.JAVA_INT, ValueLayout.JAVA_INT, ValueLayout.JAVA_LONG
         )
      );
      GET_TICK_COUNT64 = var0.downcallHandle(var1.find("GetTickCount64").orElseThrow(), FunctionDescriptor.of(ValueLayout.JAVA_LONG));
      GET_CURRENT_PROCESS_ID = var0.downcallHandle(var1.find("GetCurrentProcessId").orElseThrow(), FunctionDescriptor.of(ValueLayout.JAVA_INT));
      QUERY_PERFORMANCE_COUNTER = var0.downcallHandle(
         var1.find("QueryPerformanceCounter").orElseThrow(), FunctionDescriptor.of(ValueLayout.JAVA_INT, ValueLayout.ADDRESS)
      );
      QUERY_PERFORMANCE_FREQUENCY = var0.downcallHandle(
         var1.find("QueryPerformanceFrequency").orElseThrow(), FunctionDescriptor.of(ValueLayout.JAVA_INT, ValueLayout.ADDRESS)
      );
      CREATE_MUTEX = var0.downcallHandle(
         var1.find("CreateMutexW").orElseThrow(), FunctionDescriptor.of(ValueLayout.ADDRESS, ValueLayout.ADDRESS, ValueLayout.JAVA_INT, ValueLayout.ADDRESS)
      );
   }

   public record Actor(int formId, int flags, float x, float y, float z, float yaw, float width, float height, float healthFrac, int level, String name) {
      public boolean dead() {
         return (this.flags & 2) != 0;
      }

      public boolean hostile() {
         return (this.flags & 1) != 0;
      }
   }

   public interface InputSink {
      void accept(int var1, int var2, int var3, int var4, int var5);
   }

   public static final class McState {
      public int flags;
      public double x;
      public double y;
      public double z;
      public float yaw;
      public float pitch;
      public float eyeHeight;
      public float sensitivity;
      public int teleportAck;
      public int guiScale;
      public long frameCounter;
      public float fov;
      public float bobPhase;
      public float bobAmount;
      public double eyeX;
      public double eyeY;
      public double eyeZ;
      public long tickQpc;
      public double prevX;
      public double prevY;
      public double prevZ;
      public double curX;
      public double curY;
      public double curZ;
      public float eyeHeightO;
      public float eyeHeightT;
      public float walkDistO;
      public float walkDist;
      public float bobO;
      public float bob;
      public float tickMs = 50.0F;
      public int cameraMode;
      public float cameraDistance;
   }

   public static final class SkyState {
      public int seq;
      public int flags;
      public int worldId;
      public int collisionEpoch;
      public double x;
      public double y;
      public double z;
      public float yaw;
      public float pitch;
      public int teleportSeq;
      public int viewportW;
      public int viewportH;
      public float gameHour;

      public boolean inGame() {
         return (this.flags & 1) != 0;
      }

      public boolean menuOpen() {
         return (this.flags & 2) != 0;
      }

      public boolean loading() {
         return (this.flags & 4) != 0;
      }
   }

   public static final class WaterGrid {
      public int originX;
      public int originZ;
      public int worldId;
      public final int size = 16;
      public final float[] surface = new float[256];
   }

   public record WorldEntity(int kind, int id, float x, float y, float z, float yaw, float pitch, float scale, float[] ext, float[] uv, int tint) {
   }
}
