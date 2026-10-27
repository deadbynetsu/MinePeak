package dev.skycraft.client;

import com.mojang.blaze3d.pipeline.RenderTarget;
import com.mojang.blaze3d.systems.RenderSystem;
import com.mojang.renderpearl.api.buffers.GpuBuffer;
import com.mojang.renderpearl.api.buffers.GpuBufferSlice.MappedView;
import com.mojang.renderpearl.api.textures.GpuTexture;
import dev.skycraft.SkyCraft;
import dev.skycraft.link.SkyLink;
import java.lang.foreign.MemorySegment;
import net.minecraft.client.Minecraft;

public final class FrameExporter {
   private static final int STAGING = 3;
   private static final int FREE = 0;
   private static final int PENDING = 1;
   private static final int READY = 2;
   private static final FrameExporter.Staging[] staging = new FrameExporter.Staging[3];
   private static long nextFrameId = 1L;
   private static boolean loggedFormat;

   private FrameExporter() {
   }

   public static void capture(Minecraft var0) {
      shipReadyFrames();
      RenderTarget var1 = var0.gameRenderer.mainRenderTarget();
      GpuTexture var2 = var1.getColorTexture();
      if (var2 != null) {
         int var3 = var1.width;
         int var4 = var1.height;
         if (var3 <= 3840 && var4 <= 2160) {
            if (!loggedFormat) {
               loggedFormat = true;
               SkyCraft.LOG.info("SkyCraft: overlay capture {}x{} format {}", new Object[]{var3, var4, var2.getFormat()});
            }

            FrameExporter.Staging var5 = null;

            for (FrameExporter.Staging var9 : staging) {
               if (var9 != null && var9.state == 0) {
                  var5 = var9;
                  break;
               }
            }

            if (var5 == null) {
               for (int var10 = 0; var10 < 3; var10++) {
                  if (staging[var10] == null) {
                     staging[var10] = var5 = new FrameExporter.Staging();
                     break;
                  }
               }
            }

            if (var5 != null) {
               long var11 = (long)var3 * var4 * 4L;
               if (var5.buffer == null || var5.width != var3 || var5.height != var4) {
                  if (var5.buffer != null) {
                     var5.buffer.close();
                  }

                  var5.buffer = RenderSystem.getDevice().createBuffer(() -> "SkyCraft overlay readback", 9, var11);
                  var5.width = var3;
                  var5.height = var4;
               }

               FrameExporter.Staging var12 = var5;
               var12.state = 1;
               var12.frameId = nextFrameId++;
               RenderSystem.getDevice().createCommandEncoder().copyTextureToBuffer(var2, var12.buffer, 0L, () -> var12.state = 2, 0);
            }
         }
      }
   }

   private static void shipReadyFrames() {
      FrameExporter.Staging var0 = null;

      for (FrameExporter.Staging var4 : staging) {
         if (var4 != null && var4.state == 2 && (var0 == null || var4.frameId > var0.frameId)) {
            var0 = var4;
         }
      }

      if (var0 != null) {
         MemorySegment var9 = SkyLink.segment();
         if (var9 != null) {
            long var10 = (long)var0.width * var0.height * 4L;

            try (MappedView var13 = var0.buffer.map(true, false)) {
               MemorySegment var5 = MemorySegment.ofBuffer(var13.data());
               MemorySegment.copy(var5, 0L, var9, SkyLink.overlayBackSlotOffset(), Math.min(var10, var5.byteSize()));
            }

            SkyLink.publishOverlay(var0.width, var0.height, true, var0.frameId);
         }

         for (FrameExporter.Staging var15 : staging) {
            if (var15 != null && var15.state == 2 && var15.frameId <= var0.frameId) {
               var15.state = 0;
            }
         }
      }
   }

   private static final class Staging {
      GpuBuffer buffer;
      int width;
      int height;
      volatile int state = 0;
      long frameId;
   }
}
