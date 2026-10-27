package dev.skycraft.client.render;

import com.mojang.blaze3d.platform.NativeImage;
import dev.skycraft.SkyCraft;
import dev.skycraft.client.mixin.SpriteContentsAccessor;
import dev.skycraft.client.mixin.TextureAtlasAccessor;
import java.io.IOException;
import java.io.InputStream;
import java.lang.reflect.Field;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.function.Predicate;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.texture.SpriteContents;
import net.minecraft.client.renderer.texture.TextureAtlas;
import net.minecraft.client.renderer.texture.TextureAtlasSprite;
import net.minecraft.client.resources.model.sprite.AtlasManager;
import net.minecraft.data.AtlasIds;
import net.minecraft.resources.Identifier;
import net.minecraft.server.packs.resources.Resource;
import net.minecraft.server.packs.resources.ResourceManager;

final class SkyAtlas {
   final int width;
   final int height;
   final ByteBuffer pixels;
   private final int blockW;
   private final int blockH;
   private final int itemW;
   private final int itemH;
   final Object blockSprites;
   final Object itemSprites;
   private static final int CRACK_STAGES = 10;
   private static final int CRACK_SIZE = 16;
   private static final String[] ARROW_TEXTURES = new String[]{"arrow", "arrow_tipped", "arrow_spectral"};
   private static final int ARROW_X = 160;
   private static final int ARROW_W = 32;
   private final List<SkyAtlas.Animation> animations = new ArrayList<>();

   private SkyAtlas(TextureAtlas var1, TextureAtlas var2, ResourceManager var3) {
      TextureAtlasAccessor var4 = (TextureAtlasAccessor)var1;
      TextureAtlasAccessor var5 = (TextureAtlasAccessor)var2;
      this.blockW = var4.skycraft$width();
      this.blockH = var4.skycraft$height();
      this.itemW = var5.skycraft$width();
      this.itemH = var5.skycraft$height();
      this.width = Math.max(Math.max(this.blockW, this.itemW), 160 + ARROW_TEXTURES.length * 32);
      this.height = this.blockH + this.itemH + 16;
      this.pixels = ByteBuffer.allocateDirect(this.width * this.height * 4).order(ByteOrder.LITTLE_ENDIAN);

      for (TextureAtlasSprite var7 : var4.skycraft$sprites().values()) {
         this.copy(var7, this.blockW, this.blockH, 0);
      }

      for (TextureAtlasSprite var9 : var5.skycraft$sprites().values()) {
         this.copy(var9, this.itemW, this.itemH, this.blockH);
      }

      this.copyCracks(var3);
      this.copyArrows(var3);
      this.blockSprites = var4.skycraft$sprites();
      this.itemSprites = var5.skycraft$sprites();
   }

   static SkyAtlas build(Minecraft var0) {
      AtlasManager var1 = var0.getAtlasManager();
      return new SkyAtlas(var1.getAtlasOrThrow(AtlasIds.BLOCKS), var1.getAtlasOrThrow(AtlasIds.ITEMS), var0.getResourceManager());
   }

   private void copyCracks(ResourceManager var1) {
      int var2 = this.blockH + this.itemH;

      for (int var3 = 0; var3 < 10; var3++) {
         Identifier var4 = Identifier.withDefaultNamespace("textures/block/destroy_stage_" + var3 + ".png");
         Optional var5 = var1.getResource(var4);
         if (!var5.isEmpty()) {
            try (
               InputStream var6 = ((Resource)var5.get()).open();
               NativeImage var7 = NativeImage.read(var6);
            ) {
               for (int var8 = 0; var8 < 16; var8++) {
                  for (int var9 = 0; var9 < 16; var9++) {
                     int var10 = var7.getPixel(var9 * var7.getWidth() / 16, var8 * var7.getHeight() / 16);
                     int var11 = ((var2 + var8) * this.width + var3 * 16 + var9) * 4;
                     this.pixels.put(var11, (byte)(var10 >> 16));
                     this.pixels.put(var11 + 1, (byte)(var10 >> 8));
                     this.pixels.put(var11 + 2, (byte)var10);
                     this.pixels.put(var11 + 3, (byte)(var10 >>> 24));
                  }
               }
            } catch (IOException var16) {
               SkyCraft.LOG.warn("SkyCraft: couldn't read {}", var4, var16);
            }
         }
      }
   }

   private void copyArrows(ResourceManager var1) {
      int var2 = this.blockH + this.itemH;

      for (int var3 = 0; var3 < ARROW_TEXTURES.length; var3++) {
         Identifier var4 = Identifier.withDefaultNamespace("textures/entity/projectiles/" + ARROW_TEXTURES[var3] + ".png");
         Optional var5 = var1.getResource(var4);
         if (!var5.isEmpty()) {
            try (
               InputStream var6 = ((Resource)var5.get()).open();
               NativeImage var7 = NativeImage.read(var6);
            ) {
               for (int var8 = 0; var8 < 16; var8++) {
                  for (int var9 = 0; var9 < 32; var9++) {
                     int var10 = var7.getPixel(var9 * var7.getWidth() / 32, var8 * var7.getHeight() / 32);
                     int var11 = ((var2 + var8) * this.width + 160 + var3 * 32 + var9) * 4;
                     this.pixels.put(var11, (byte)(var10 >> 16));
                     this.pixels.put(var11 + 1, (byte)(var10 >> 8));
                     this.pixels.put(var11 + 2, (byte)var10);
                     this.pixels.put(var11 + 3, (byte)(var10 >>> 24));
                  }
               }
            } catch (IOException var16) {
               SkyCraft.LOG.warn("SkyCraft: couldn't read {}", var4, var16);
            }
         }
      }
   }

   float[] arrowUv(int var1) {
      float var2 = 160 + Math.clamp(var1, 0, ARROW_TEXTURES.length - 1) * 32;
      float var3 = this.blockH + this.itemH;
      return new float[]{
         var2 / this.width,
         var3 / this.height,
         (var2 + 16.0F) / this.width,
         (var3 + 5.0F) / this.height,
         var2 / this.width,
         (var3 + 5.0F) / this.height,
         (var2 + 5.0F) / this.width,
         (var3 + 10.0F) / this.height
      };
   }

   float[] crackUv(int var1) {
      int var2 = Math.clamp(var1, 0, 9);
      float var3 = (float)(this.blockH + this.itemH) / this.height;
      float var4 = (float)(this.blockH + this.itemH + 16) / this.height;
      return new float[]{(float)(var2 * 16) / this.width, var3, (float)((var2 + 1) * 16) / this.width, var4};
   }

   float[] rect(TextureAtlasSprite var1) {
      return new float[]{this.u(var1, var1.getU0()), this.v(var1, var1.getV0()), this.u(var1, var1.getU1()), this.v(var1, var1.getV1())};
   }

   boolean stale(Minecraft var1) {
      AtlasManager var2 = var1.getAtlasManager();
      return ((TextureAtlasAccessor)var2.getAtlasOrThrow(AtlasIds.BLOCKS)).skycraft$sprites() != this.blockSprites
         || ((TextureAtlasAccessor)var2.getAtlasOrThrow(AtlasIds.ITEMS)).skycraft$sprites() != this.itemSprites;
   }

   private void copy(TextureAtlasSprite var1, int var2, int var3, int var4) {
      NativeImage var5 = ((SpriteContentsAccessor)var1.contents()).skycraft$originalImage();
      int var6 = Math.min(var1.contents().width(), var5.getWidth());
      int var7 = Math.min(var1.contents().height(), var5.getHeight());
      int var8 = Math.round(var1.getU0() * var2);
      int var9 = Math.round(var1.getV0() * var3);
      int var10 = Math.max(0, Math.min(var8 - var1.getX(), var9 - var1.getY()));

      for (int var11 = -var10; var11 < var7 + var10; var11++) {
         int var12 = var9 + var11 + var4;
         if (var12 >= 0 && var12 < this.height) {
            int var13 = Math.clamp(var11, 0, var7 - 1);

            for (int var14 = -var10; var14 < var6 + var10; var14++) {
               int var15 = var8 + var14;
               if (var15 >= 0 && var15 < this.width) {
                  int var16 = var5.getPixel(Math.clamp(var14, 0, var6 - 1), var13);
                  int var17 = (var12 * this.width + var15) * 4;
                  this.pixels.put(var17, (byte)(var16 >> 16));
                  this.pixels.put(var17 + 1, (byte)(var16 >> 8));
                  this.pixels.put(var17 + 2, (byte)var16);
                  this.pixels.put(var17 + 3, (byte)(var16 >>> 24));
               }
            }
         }
      }

      SkyAtlas.Animation var18 = animationOf(var1.contents());
      if (var18 != null) {
         var18.image = var5;
         var18.x = var8;
         var18.y = var9 + var4;
         var18.w = var6;
         var18.h = var7;
         var18.pad = var10;
         this.animations.add(var18);
      }
   }

   private static SkyAtlas.Animation animationOf(SpriteContents var0) {
      try {
         Field var1 = SpriteContents.class.getDeclaredField("animatedTexture");
         var1.setAccessible(true);
         Object var2 = var1.get(var0);
         if (var2 == null) {
            return null;
         }

         Class var3 = var2.getClass();
         Field var4 = var3.getDeclaredField("frames");
         Field var5 = var3.getDeclaredField("frameRowSize");
         Field var6 = var3.getDeclaredField("interpolateFrames");
         var4.setAccessible(true);
         var5.setAccessible(true);
         var6.setAccessible(true);
         List var7 = (List)var4.get(var2);
         if (var7.size() < 2) {
            return null;
         }

         SkyAtlas.Animation var8 = new SkyAtlas.Animation();
         var8.index = new int[var7.size()];
         var8.time = new int[var7.size()];

         for (int var9 = 0; var9 < var7.size(); var9++) {
            Object var10 = var7.get(var9);
            Field var11 = var10.getClass().getDeclaredField("index");
            Field var12 = var10.getClass().getDeclaredField("time");
            var11.setAccessible(true);
            var12.setAccessible(true);
            var8.index[var9] = var11.getInt(var10);
            var8.time[var9] = Math.max(1, var12.getInt(var10));
            var8.cycle = var8.cycle + var8.time[var9];
         }

         var8.rowSize = Math.max(1, var5.getInt(var2));
         var8.interpolate = var6.getBoolean(var2);
         return var8;
      } catch (ReflectiveOperationException | RuntimeException var13) {
         return null;
      }
   }

   int animatedSprites() {
      return this.animations.size();
   }

   void animate(long var1, Predicate<SkyAtlas.Region> var3) {
      for (SkyAtlas.Animation var5 : this.animations) {
         long var6 = Math.floorMod((long)var1, (long)var5.cycle);

         int var8;
         for (var8 = 0; var6 >= var5.time[var8]; var8++) {
            var6 -= var5.time[var8];
         }

         long var9 = var5.interpolate ? (long)var8 << 20 | var6 : var8;
         if (var9 != var5.shown) {
            int var11 = (var8 + 1) % var5.index.length;
            float var12 = var5.interpolate ? (float)var6 / var5.time[var8] : 0.0F;
            int var13 = var5.w + 2 * var5.pad;
            int var14 = var5.h + 2 * var5.pad;
            ByteBuffer var15 = ByteBuffer.allocateDirect(var13 * var14 * 4).order(ByteOrder.LITTLE_ENDIAN);
            int var16 = var5.index[var8] % var5.rowSize * var5.w;
            int var17 = var5.index[var8] / var5.rowSize * var5.h;
            int var18 = var5.index[var11] % var5.rowSize * var5.w;
            int var19 = var5.index[var11] / var5.rowSize * var5.h;

            for (int var20 = -var5.pad; var20 < var5.h + var5.pad; var20++) {
               int var21 = Math.clamp(var20, 0, var5.h - 1);

               for (int var22 = -var5.pad; var22 < var5.w + var5.pad; var22++) {
                  int var23 = Math.clamp(var22, 0, var5.w - 1);
                  int var24 = var5.image.getPixel(var16 + var23, var17 + var21);
                  if (var12 > 0.0F) {
                     var24 = mix(var24, var5.image.getPixel(var18 + var23, var19 + var21), var12);
                  }

                  var15.put((byte)(var24 >> 16)).put((byte)(var24 >> 8)).put((byte)var24).put((byte)(var24 >>> 24));
               }
            }

            var15.flip();
            if (var3.test(new SkyAtlas.Region(var5.x - var5.pad, var5.y - var5.pad, var13, var14, var15))) {
               var5.shown = var9;
            }
         }
      }
   }

   private static int mix(int var0, int var1, float var2) {
      int var3 = 0;

      for (byte var4 = 0; var4 < 32; var4 += 8) {
         int var5 = var0 >>> var4 & 0xFF;
         int var6 = var1 >>> var4 & 0xFF;
         var3 |= (Math.round(var5 + (var6 - var5) * var2) & 0xFF) << var4;
      }

      return var3;
   }

   static NativeImage image(TextureAtlas var0) {
      TextureAtlasAccessor var1 = (TextureAtlasAccessor)var0;
      int var2 = var1.skycraft$width();
      int var3 = var1.skycraft$height();
      NativeImage var4 = new NativeImage(var2, var3, true);

      for (TextureAtlasSprite var6 : var1.skycraft$sprites().values()) {
         NativeImage var7 = ((SpriteContentsAccessor)var6.contents()).skycraft$originalImage();
         int var8 = Math.min(var6.contents().width(), var7.getWidth());
         int var9 = Math.min(var6.contents().height(), var7.getHeight());
         int var10 = Math.round(var6.getU0() * var2);
         int var11 = Math.round(var6.getV0() * var3);
         int var12 = Math.max(0, Math.min(var10 - var6.getX(), var11 - var6.getY()));

         for (int var13 = -var12; var13 < var9 + var12; var13++) {
            for (int var14 = -var12; var14 < var8 + var12; var14++) {
               int var15 = var10 + var14;
               int var16 = var11 + var13;
               if (var15 >= 0 && var15 < var2 && var16 >= 0 && var16 < var3) {
                  var4.setPixel(var15, var16, var7.getPixel(Math.clamp(var14, 0, var8 - 1), Math.clamp(var13, 0, var9 - 1)));
               }
            }
         }
      }

      return var4;
   }

   float u(TextureAtlasSprite var1, float var2) {
      return var2 * (isItem(var1) ? this.itemW : this.blockW) / this.width;
   }

   float v(TextureAtlasSprite var1, float var2) {
      return isItem(var1) ? (this.blockH + var2 * this.itemH) / this.height : var2 * this.blockH / this.height;
   }

   float blockU(float var1) {
      return var1 * this.blockW / this.width;
   }

   float blockV(float var1) {
      return var1 * this.blockH / this.height;
   }

   float itemU(float var1) {
      return var1 * this.itemW / this.width;
   }

   float itemV(float var1) {
      return (this.blockH + var1 * this.itemH) / this.height;
   }

   private static boolean isItem(TextureAtlasSprite var0) {
      return var0.atlasLocation().equals(TextureAtlas.LOCATION_ITEMS);
   }

   private static final class Animation {
      NativeImage image;
      int x;
      int y;
      int w;
      int h;
      int pad;
      int[] index;
      int[] time;
      int cycle;
      int rowSize;
      boolean interpolate;
      long shown = Long.MIN_VALUE;
   }

   record Region(int x, int y, int w, int h, ByteBuffer pixels) {
   }
}
