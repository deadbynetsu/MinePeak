package dev.skycraft.client.render;

import com.mojang.blaze3d.vertex.QuadInstance;
import com.mojang.blaze3d.vertex.VertexConsumer;
import dev.skycraft.SkyCraft;
import dev.skycraft.client.SkyDigClient;
import dev.skycraft.client.mixin.ClientLevelAccessor;
import dev.skycraft.combat.SkyrimActorEntity;
import dev.skycraft.link.SkyLink;
import dev.skycraft.world.SkyClip;
import dev.skycraft.world.SkyCollision;
import dev.skycraft.world.SkyDig;
import it.unimi.dsi.fastutil.longs.LongLinkedOpenHashSet;
import it.unimi.dsi.fastutil.longs.LongOpenHashSet;
import it.unimi.dsi.fastutil.objects.ObjectIterator;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import net.minecraft.client.Minecraft;
import net.minecraft.client.color.block.BlockTintSource;
import net.minecraft.client.model.geom.builders.UVPair;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.client.renderer.block.BlockQuadOutput;
import net.minecraft.client.renderer.block.FluidRenderer;
import net.minecraft.client.renderer.block.ModelBlockRenderer;
import net.minecraft.client.renderer.block.FluidRenderer.Output;
import net.minecraft.client.renderer.block.dispatch.BlockStateModel;
import net.minecraft.client.renderer.block.dispatch.BlockStateModelPart;
import net.minecraft.client.renderer.chunk.ChunkSectionLayer;
import net.minecraft.client.renderer.item.ItemStackRenderState;
import net.minecraft.client.renderer.texture.TextureAtlasSprite;
import net.minecraft.client.resources.model.geometry.BakedQuad;
import net.minecraft.client.resources.model.sprite.Material.Baked;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.core.SectionPos;
import net.minecraft.core.BlockPos.MutableBlockPos;
import net.minecraft.core.Direction.Axis;
import net.minecraft.server.level.BlockDestructionProgress;
import net.minecraft.util.Mth;
import net.minecraft.util.RandomSource;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.LivingEntity;
import net.minecraft.world.entity.item.ItemEntity;
import net.minecraft.world.entity.projectile.ItemSupplier;
import net.minecraft.world.entity.projectile.arrow.AbstractArrow;
import net.minecraft.world.entity.projectile.arrow.Arrow;
import net.minecraft.world.entity.projectile.arrow.SpectralArrow;
import net.minecraft.world.entity.projectile.arrow.ThrownTrident;
import net.minecraft.world.item.BlockItem;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemDisplayContext;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.CardinalLighting;
import net.minecraft.world.level.EmptyBlockGetter;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.RenderShape;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.level.chunk.LevelChunk;
import net.minecraft.world.level.chunk.LevelChunkSection;
import net.minecraft.world.level.chunk.status.ChunkStatus;
import net.minecraft.world.level.material.FluidState;
import net.minecraft.world.phys.AABB;
import net.minecraft.world.phys.BlockHitResult;
import net.minecraft.world.phys.HitResult;
import net.minecraft.world.phys.Vec3;
import net.minecraft.world.phys.HitResult.Type;
import net.minecraft.world.phys.shapes.VoxelShape;
import org.joml.Vector3fc;

public final class WorldExporter {
   private static final int SECTIONS_PER_FRAME = 12;
   private static final double ENTITY_RANGE = 96.0;
   private static final LongLinkedOpenHashSet DIRTY = new LongLinkedOpenHashSet();
   private static final LongOpenHashSet SENT = new LongOpenHashSet();
   private static final LongOpenHashSet LIT = new LongOpenHashSet();
   private static final LongOpenHashSet SOLID = new LongOpenHashSet();
   private static final long[] SOLID_BITS = new long[64];
   private static final LongOpenHashSet DUG = new LongOpenHashSet();
   private static final ByteBuffer LIGHTS = ByteBuffer.allocate(32768).order(ByteOrder.LITTLE_ENDIAN);
   private static int sentGeneration = Integer.MIN_VALUE;
   private static int meshesSent;
   private static ClientLevel sentLevel;
   private static SkyAtlas atlas;
   private static ModelBlockRenderer blockRenderer;
   private static FluidRenderer fluidRenderer;
   private static final WorldExporter.MeshBuilder MESH = new WorldExporter.MeshBuilder();
   private static final List<SkyLink.WorldEntity> ENTITIES = new ArrayList<>();
   private static final ItemStackRenderState ITEM_STATE = new ItemStackRenderState();
   private static final Map<Item, float[]> ICONS = new HashMap<>();
   private static final Map<BlockState, float[]> CUBE_FACES = new HashMap<>();
   private static final RandomSource RANDOM = RandomSource.create();

   private WorldExporter() {
   }

   public static void markDirty(int var0, int var1, int var2) {
      synchronized (DIRTY) {
         DIRTY.add(SectionPos.asLong(var0, var1, var2));
      }
   }

   public static void markDirtyNow(int var0, int var1, int var2) {
      synchronized (DIRTY) {
         DIRTY.addAndMoveToFirst(SectionPos.asLong(var0, var1, var2));
      }
   }

   public static void frame(Minecraft var0, float var1) {
      ClientLevel var2 = var0.level;
      if (var2 != null && var0.player != null && SkyLink.active()) {
         if (sentGeneration != SkyLink.generation() || sentLevel != var2 || atlas == null || atlas.stale(var0)) {
            resendEverything(var0, var2);
         }

         meshDirtySections(var2);
         atlas.animate(
            var2.getGameTime(),
            var0x -> {
               ByteBuffer var1x = ByteBuffer.allocate(16)
                  .order(ByteOrder.LITTLE_ENDIAN)
                  .putInt(var0x.x())
                  .putInt(var0x.y())
                  .putInt(var0x.w())
                  .putInt(var0x.h())
                  .flip();
               return SkyLink.tryWriteRender(7, var1x, var0x.pixels());
            }
         );
         exportEntities(var0, var2, var1);
         AvatarExporter.frame(var0, atlas, var1);
      }
   }

   private static void resendEverything(Minecraft var0, ClientLevel var1) {
      sentGeneration = SkyLink.generation();
      sentLevel = var1;
      atlas = SkyAtlas.build(var0);
      SkyCraft.LOG.info("SkyCraft: {} animated textures (water, lava, fire, ...) will play in Skyrim", atlas.animatedSprites());
      AvatarExporter.reset();
      ICONS.clear();
      CUBE_FACES.clear();
      boolean var2 = var0.options.ambientOcclusion().get();
      blockRenderer = new ModelBlockRenderer(var2, true, var0.getBlockColors());
      fluidRenderer = new FluidRenderer(var0.getModelManager().getFluidStateModelSet());
      SkyLink.writeRender(3, ByteBuffer.allocate(0), null);
      ByteBuffer var3 = ByteBuffer.allocate(8).order(ByteOrder.LITTLE_ENDIAN).putInt(atlas.width).putInt(atlas.height).flip();
      boolean var4 = SkyLink.writeRender(1, var3, atlas.pixels.duplicate().clear());
      SkyCraft.LOG.info("SkyCraft: sent {}x{} texture atlas to Skyrim ({})", new Object[]{atlas.width, atlas.height, var4 ? "ok" : "FAILED"});
      SENT.clear();
      LIT.clear();
      SOLID.clear();
      DUG.clear();
      SkyDigClient.resendAll();
      int var5 = var0.options.getEffectiveRenderDistance() + 1;
      int var6 = SectionPos.blockToSectionCoord(var0.player.getBlockX());
      int var7 = SectionPos.blockToSectionCoord(var0.player.getBlockZ());

      for (int var8 = var6 - var5; var8 <= var6 + var5; var8++) {
         for (int var9 = var7 - var5; var9 <= var7 + var5; var9++) {
            LevelChunk var10 = var1.getChunkSource().getChunk(var8, var9, ChunkStatus.FULL, false);
            if (var10 != null) {
               LevelChunkSection[] var11 = var10.getSections();

               for (int var12 = 0; var12 < var11.length; var12++) {
                  if (!var11[var12].hasOnlyAir()) {
                     markDirty(var8, var10.getSectionYFromSectionIndex(var12), var9);
                  }
               }
            }
         }
      }
   }

   private static void meshDirtySections(ClientLevel var0) {
      long var1 = System.nanoTime() + 3000000L;
      int var3 = 0;

      while (var3 < 12 && System.nanoTime() < var1) {
         long var4;
         synchronized (DIRTY) {
            if (DIRTY.isEmpty()) {
               return;
            }

            var4 = DIRTY.removeFirstLong();
         }

         if (meshSection(var0, var4)) {
            var3++;
         }
      }
   }

   private static LevelChunkSection sectionAt(ClientLevel var0, LevelChunk var1, int var2) {
      int var3 = var0.getSectionIndexFromSectionY(var2);
      return var3 >= 0 && var3 < var1.getSections().length ? var1.getSections()[var3] : null;
   }

   private static boolean meshSection(ClientLevel var0, long var1) {
      int var3 = SectionPos.x(var1);
      int var4 = SectionPos.y(var1);
      int var5 = SectionPos.z(var1);
      LevelChunk var6 = var0.getChunkSource().getChunk(var3, var5, ChunkStatus.FULL, false);
      if (var6 == null) {
         return false;
      }

      LevelChunkSection var7 = sectionAt(var0, var6, var4);
      boolean var8 = var7 == null || var7.hasOnlyAir();
      long[] var9 = SkyDigClient.dugBits(var6, var4);
      int var10 = 0;
      if (var9 != null) {
         for (long var14 : var9) {
            var10 += Long.bitCount(var14);
         }
      }

      if (var8 && !SENT.contains(var1) && !LIT.contains(var1) && !SOLID.contains(var1) && var10 == 0 && !DUG.contains(var1)) {
         return false;
      }

      MESH.reset();
      LIGHTS.clear();
      int var22 = 0;
      Arrays.fill(SOLID_BITS, 0L);
      int var23 = 0;
      MESH.cardinal = var0.cardinalLighting();
      if (!var8) {
         BlockPos var24 = SectionPos.of(var3, var4, var5).origin();
         MutableBlockPos var26 = new MutableBlockPos();

         for (int var15 = 0; var15 < 16; var15++) {
            for (int var16 = 0; var16 < 16; var16++) {
               for (int var17 = 0; var17 < 16; var17++) {
                  var26.set(var24.getX() + var17, var24.getY() + var15, var24.getZ() + var16);
                  BlockState var18 = var6.getBlockState(var26);
                  if (!var18.isAir()) {
                     if (!var18.getCollisionShape(var0, var26).isEmpty()) {
                        int var19 = var17 + 16 * var16 + 256 * var15;
                        SOLID_BITS[var19 >> 6] = SOLID_BITS[var19 >> 6] | 1L << (var19 & 63);
                        var23++;
                     }

                     int var38 = var18.getLightEmission();
                     if (var38 > 0) {
                        LIGHTS.put((byte)var17).put((byte)var15).put((byte)var16).put((byte)var38).putInt(BlockLightColors.of(var18));
                        var22++;
                     }

                     FluidState var20 = var18.getFluidState();
                     if (!var20.isEmpty()) {
                        MESH.fluidGround = SkyCollision.groundTop(var26);
                        MESH.fluidBaseY = var15;
                        fluidRenderer.tesselate(var0, var26, MESH, var18, var20);
                        MESH.fluidGround = 0.0F;
                     }

                     if (var18.getRenderShape() == RenderShape.MODEL) {
                        BlockStateModel var21 = Minecraft.getInstance().getModelManager().getBlockStateModelSet().get(var18);
                        blockRenderer.tesselateBlock(MESH, var17, var15, var16, var0, var26.immutable(), var18, var21, var18.getSeed(var26));
                     }
                  }
               }
            }
         }
      }

      SkyDig.DugLookup var25 = SkyDig.clientDug;
      if (var10 > 0 && var25 != null) {
         Minecraft var27 = Minecraft.getInstance();
         DigWalls.add(var0, var3, var4, var5, var9, var25, var1x -> cubeFaces(var27, var1x), MESH::wall);
      }

      if (MESH.vertexCount() == 0
         && !SENT.contains(var1)
         && var22 == 0
         && !LIT.contains(var1)
         && var23 == 0
         && !SOLID.contains(var1)
         && var10 == 0
         && !DUG.contains(var1)) {
         return true;
      }

      ByteBuffer var28 = ByteBuffer.allocate(16).order(ByteOrder.LITTLE_ENDIAN).putInt(var3).putInt(var4).putInt(var5).putInt(MESH.vertexCount()).flip();
      if (SkyLink.writeRender(2, var28, MESH.bytes())) {
         if (MESH.vertexCount() > 0) {
            SENT.add(var1);
         } else {
            SENT.remove(var1);
         }

         if (var22 > 0 || LIT.contains(var1)) {
            ByteBuffer var29 = ByteBuffer.allocate(16).order(ByteOrder.LITTLE_ENDIAN).putInt(var3).putInt(var4).putInt(var5).putInt(var22).flip();
            if (!SkyLink.writeRender(8, var29, LIGHTS.flip())) {
               markDirty(var3, var4, var5);
            } else if (var22 > 0) {
               LIT.add(var1);
            } else {
               LIT.remove(var1);
            }
         }

         if (var23 > 0 || SOLID.contains(var1)) {
            ByteBuffer var30 = ByteBuffer.allocate(16).order(ByteOrder.LITTLE_ENDIAN).putInt(var3).putInt(var4).putInt(var5).putInt(var23).flip();
            ByteBuffer var32 = ByteBuffer.allocate(var23 > 0 ? 512 : 0).order(ByteOrder.LITTLE_ENDIAN);
            if (var23 > 0) {
               for (long var41 : SOLID_BITS) {
                  var32.putLong(var41);
               }
            }

            if (!SkyLink.writeRender(10, var30, var32.flip())) {
               markDirty(var3, var4, var5);
            } else if (var23 > 0) {
               SOLID.add(var1);
            } else {
               SOLID.remove(var1);
            }
         }

         if (var10 > 0 || DUG.contains(var1)) {
            ByteBuffer var31 = ByteBuffer.allocate(24)
               .order(ByteOrder.LITTLE_ENDIAN)
               .putInt(var3)
               .putInt(var4)
               .putInt(var5)
               .putInt(var10)
               .putInt(SkyDigClient.world())
               .putInt(0)
               .flip();
            ByteBuffer var33 = ByteBuffer.allocate(var10 > 0 ? 512 : 0).order(ByteOrder.LITTLE_ENDIAN);
            if (var10 > 0) {
               for (long var42 : var9) {
                  var33.putLong(var42);
               }
            }

            if (!SkyLink.writeRender(11, var31, var33.flip())) {
               markDirty(var3, var4, var5);
            } else if (var10 > 0) {
               DUG.add(var1);
            } else {
               DUG.remove(var1);
            }
         }

         if (++meshesSent <= 10 || meshesSent % 200 == 0) {
            SkyCraft.LOG
               .info(
                  "SkyCraft: block mesh for section {} {} {}: {} vertices ({} sections in Skyrim)",
                  new Object[]{var3, var4, var5, MESH.vertexCount(), SENT.size()}
               );
         }
      } else {
         markDirty(var3, var4, var5);
      }

      return true;
   }

   private static void exportEntities(Minecraft var0, ClientLevel var1, float var2) {
      ENTITIES.clear();
      Vec3 var3 = var0.player.getEyePosition(var2);

      for (Entity var5 : var1.entitiesForRendering()) {
         if (ENTITIES.size() < 160 && !(var5.distanceToSqr(var3) > 9216.0)) {
            Vec3 var6 = var5.getPosition(var2);
            if (var5 instanceof AbstractArrow var7) {
               boolean var16 = var7 instanceof ThrownTrident;
               int var17 = var16 ? 3 : 1;
               float var12 = Mth.rotLerp(var2, var7.yRotO, var7.getYRot());
               float var13 = Mth.lerp(var2, var7.xRotO, var7.getXRot());
               float[] var14 = var16
                  ? ICONS.computeIfAbsent(Items.TRIDENT, var2x -> iconUv(var0, var1, new ItemStack(var2x)))
                  : atlas.arrowUv(var7 instanceof SpectralArrow ? 2 : (var7 instanceof Arrow var15 && var15.getColor() > 0 ? 1 : 0));
               if (var14 != null) {
                  ENTITIES.add(new SkyLink.WorldEntity(var17, var5.getId(), (float)var6.x, (float)var6.y, (float)var6.z, var12, var13, 1.0F, null, var14, 0));
               }
            } else if (var5 instanceof ItemEntity var8) {
               float var10 = Mth.sin((var8.getAge() + var2) / 10.0F + var8.bobOffs) * 0.1F + 0.1F;
               float var11 = ItemEntity.getSpin(var8.getAge() + var2, var8.bobOffs) * (180.0F / (float)Math.PI);
               addItem(var0, var1, var5, var8.getItem(), var6.add(0.0, var10, 0.0), var11);
            } else if (var5 instanceof ItemSupplier var9) {
               addItem(var0, var1, var5, var9.getItem(), var6.add(0.0, var5.getBbHeight() * 0.5 - 0.25, 0.0), 0.0F);
            } else if (var5 instanceof LivingEntity
               && !(var5 instanceof SkyrimActorEntity)
               && !var5.isInvisible()
               && (var5 != var0.player || var0.gameRenderer.mainCamera().isDetached())) {
               ENTITIES.add(new SkyLink.WorldEntity(6, var5.getId(), (float)var6.x, (float)var6.y, (float)var6.z, 0.0F, 0.0F, var5.getBbWidth(), null, null, 0));
            }
         }
      }

      addCracks(var1);
      SkyLink.writeWorldEntities(ENTITIES, selection(var0, var1));
   }

   private static void addItem(Minecraft var0, ClientLevel var1, Entity var2, ItemStack var3, Vec3 var4, float var5) {
      if (var3.getItem() instanceof BlockItem var6) {
         BlockState var12 = var6.getBlock().defaultBlockState();
         if (var12.getRenderShape() == RenderShape.MODEL && Block.isShapeFullBlock(var12.getShape(EmptyBlockGetter.INSTANCE, BlockPos.ZERO))) {
            float[] var8 = cubeFaces(var0, var12);
            if (var8 != null) {
               float var9 = 0.25F;
               int var10 = cubeTint(var0, var12);
               ENTITIES.add(
                  new SkyLink.WorldEntity(
                     4, var2.getId(), (float)var4.x, (float)(var4.y + var9 * 0.5 + 0.02), (float)var4.z, var5, 0.0F, var9, null, var8, var10
                  )
               );
               return;
            }
         }
      }

      float[] var11 = iconUv(var0, var1, var3);
      if (var11 != null) {
         ENTITIES.add(new SkyLink.WorldEntity(2, var2.getId(), (float)var4.x, (float)var4.y + 0.25F, (float)var4.z, var5, 0.0F, 0.5F, null, var11, 0));
      }
   }

   private static float[] cubeFaces(Minecraft var0, BlockState var1) {
      return CUBE_FACES.computeIfAbsent(var1, var1x -> {
         BlockStateModel var2 = var0.getModelManager().getBlockStateModelSet().get(var1x);
         ArrayList var3 = new ArrayList();
         RANDOM.setSeed(42L);
         var2.collectParts(RANDOM, var3);
         float[] var4 = new float[12];
         Direction[] var5 = new Direction[]{Direction.NORTH, Direction.UP, Direction.DOWN};

         for (int var6 = 0; var6 < 3; var6++) {
            TextureAtlasSprite var7 = null;

            for (BlockStateModelPart var9 : var3) {
               List var10 = var9.getQuads(var5[var6]);
               if (!var10.isEmpty()) {
                  var7 = ((BakedQuad)var10.getFirst()).materialInfo().sprite();
                  break;
               }
            }

            if (var7 == null) {
               return null;
            }

            System.arraycopy(atlas.rect(var7), 0, var4, var6 * 4, 4);
         }

         return var4;
      });
   }

   private static int cubeTint(Minecraft var0, BlockState var1) {
      List var2 = var0.getBlockColors().getTintSources(var1);
      if (!var2.isEmpty() && var2.getFirst() != null) {
         int var3 = ((BlockTintSource)var2.getFirst()).color(var1);
         return 0xFF000000 | (var3 & 0xFF) << 16 | var3 & 0xFF00 | var3 >> 16 & 0xFF;
      } else {
         return 0;
      }
   }

   private static void addCracks(ClientLevel var0) {
      ObjectIterator var1 = ((ClientLevelAccessor)var0).skycraft$destroyingBlocks().values().iterator();

      while (var1.hasNext()) {
         BlockDestructionProgress var2 = (BlockDestructionProgress)var1.next();
         int var3 = var2.getProgress();
         if (var3 >= 0 && var3 <= 9 && ENTITIES.size() < 160) {
            BlockPos var4 = var2.getPos();
            VoxelShape var5 = var0.getBlockState(var4).getShape(var0, var4);
            if (!var5.isEmpty()) {
               AABB var6 = var5.bounds().move(var4).inflate(0.004);
               ENTITIES.add(
                  new SkyLink.WorldEntity(
                     5,
                     var4.hashCode(),
                     (float)var6.minX,
                     (float)var6.minY,
                     (float)var6.minZ,
                     0.0F,
                     0.0F,
                     1.0F,
                     new float[]{(float)var6.getXsize(), (float)var6.getYsize(), (float)var6.getZsize()},
                     atlas.crackUv(var3),
                     0
                  )
               );
            }
         }
      }
   }

   private static float[] iconUv(Minecraft var0, ClientLevel var1, ItemStack var2) {
      var0.getItemModelResolver().updateForTopItem(ITEM_STATE, var2, ItemDisplayContext.GROUND, var1, null, 0);
      RANDOM.setSeed(0L);
      Baked var3 = ITEM_STATE.pickParticleMaterial(RANDOM);
      return var3 == null ? null : atlas.rect(var3.sprite());
   }

   private static float[] selection(Minecraft var0, ClientLevel var1) {
      HitResult var2 = var0.hitResult;
      if (var2 instanceof BlockHitResult var3 && var2.getType() == Type.BLOCK && var0.gui.screen() == null) {
         BlockPos var4 = var3.getBlockPos();
         if (!(var3 instanceof SkyClip.SkyrimHitResult)) {
            VoxelShape var5 = var1.getBlockState(var4).getShape(var1, var4);
            if (var5.isEmpty()) {
               return null;
            }

            AABB var6 = var5.bounds().move(var4);
            return new float[]{(float)var6.minX, (float)var6.minY, (float)var6.minZ, (float)var6.maxX, (float)var6.maxY, (float)var6.maxZ};
         } else {
            return !(var0.player.getMainHandItem().getItem() instanceof BlockItem)
               ? null
               : new float[]{var4.getX(), var4.getY(), var4.getZ(), var4.getX() + 1, var4.getY() + 1, var4.getZ() + 1};
         }
      } else {
         return null;
      }
   }

   private static final class MeshBuilder implements BlockQuadOutput, Output, VertexConsumer {
      private ByteBuffer buf = ByteBuffer.allocateDirect(1048576).order(ByteOrder.LITTLE_ENDIAN);
      private int vertices;
      private final float[] fq = new float[32];
      private int fqCount;
      private boolean fluidTranslucent;
      CardinalLighting cardinal = CardinalLighting.DEFAULT;
      float fluidGround;
      int fluidBaseY;

      void reset() {
         this.buf.clear();
         this.vertices = 0;
         this.fqCount = 0;
      }

      int vertexCount() {
         return this.vertices;
      }

      ByteBuffer bytes() {
         return this.buf.duplicate().flip();
      }

      private void ensure(int var1) {
         if (this.buf.remaining() < var1) {
            ByteBuffer var2 = ByteBuffer.allocateDirect(Math.max(this.buf.capacity() * 2, this.buf.position() + var1)).order(ByteOrder.LITTLE_ENDIAN);
            this.buf.flip();
            var2.put(this.buf);
            this.buf = var2;
         }
      }

      private void vertex(float var1, float var2, float var3, float var4, float var5, int var6, int var7, int var8) {
         this.buf.putFloat(var1).putFloat(var2).putFloat(var3).putFloat(var4).putFloat(var5);
         this.buf.put((byte)(var6 >> 16)).put((byte)(var6 >> 8)).put((byte)var6).put((byte)(var6 >>> 24));
         int var9 = var7 >> 4 & 15;
         int var10 = var7 >> 20 & 15;
         this.buf.putInt(var9 | var10 << 8);
         this.buf.putInt(var8);
         this.vertices++;
      }

      private static int flags(boolean var0, Direction var1) {
         return (var0 ? 2 : 1) | (var1 == null ? 0 : var1.ordinal() + 1 << 4);
      }

      private static int unshade(int var0, float var1) {
         if (!(var1 >= 0.999F) && !(var1 <= 0.0F)) {
            int var2 = Math.min(255, Math.round((var0 >> 16 & 0xFF) / var1));
            int var3 = Math.min(255, Math.round((var0 >> 8 & 0xFF) / var1));
            int var4 = Math.min(255, Math.round((var0 & 0xFF) / var1));
            return var0 & 0xFF000000 | var2 << 16 | var3 << 8 | var4;
         } else {
            return var0;
         }
      }

      void wall(float[] var1, float[] var2, int var3, Direction var4) {
         this.ensure(192);
         int var5 = flags(false, var4);

         for (int var9 : new int[]{0, 1, 2, 0, 2, 3}) {
            this.vertex(var1[var9 * 3], var1[var9 * 3 + 1], var1[var9 * 3 + 2], var2[var9 * 2], var2[var9 * 2 + 1], -1, var3, var5);
         }
      }

      @Override
      public void put(float var1, float var2, float var3, BakedQuad var4, QuadInstance var5) {
         this.ensure(192);
         TextureAtlasSprite var6 = var4.materialInfo().sprite();
         boolean var7 = var4.materialInfo().layer().translucent();
         int var8 = var4.materialInfo().lightEmission();
         Direction var9 = var4.materialInfo().shadeDirectionOverride();
         Direction var10 = var9 == Direction.UP && var4.direction() != Direction.UP ? null : (var9 != null ? var9 : var4.direction());
         float var11 = this.cardinal.byFace(var9 != null ? var9 : var4.direction());
         int var12 = flags(var7, var10);

         for (int var16 : new int[]{0, 1, 2, 0, 2, 3}) {
            Vector3fc var17 = var4.position(var16);
            long var18 = var4.packedUV(var16);
            float var20 = WorldExporter.atlas.u(var6, UVPair.unpackU(var18));
            float var21 = WorldExporter.atlas.v(var6, UVPair.unpackV(var18));
            this.vertex(
               var17.x() + var1,
               var17.y() + var2,
               var17.z() + var3,
               var20,
               var21,
               unshade(var5.getColor(var16), var11),
               var5.getLightCoordsWithEmission(var16, var8),
               var12
            );
         }
      }

      @Override
      public VertexConsumer getBuilder(ChunkSectionLayer var1) {
         this.fluidTranslucent = var1.translucent();
         return this;
      }

      @Override
      public void addVertex(float var1, float var2, float var3, int var4, float var5, float var6, int var7, int var8, float var9, float var10, float var11) {
         int var12 = this.fqCount * 8;
         if (this.fluidGround > 0.0F) {
            float var13 = Math.max(0.0F, Math.min(1.0F, var2 - this.fluidBaseY));
            var2 = this.fluidBaseY + this.fluidGround + var13 * (1.0F - this.fluidGround);
         }

         this.fq[var12] = var1;
         this.fq[var12 + 1] = var2;
         this.fq[var12 + 2] = var3;
         this.fq[var12 + 3] = WorldExporter.atlas.blockU(var5);
         this.fq[var12 + 4] = WorldExporter.atlas.blockV(var6);
         this.fq[var12 + 5] = Float.intBitsToFloat(var4);
         this.fq[var12 + 6] = Float.intBitsToFloat(var8);
         if (++this.fqCount == 4) {
            this.fqCount = 0;
            this.ensure(192);
            Direction var21 = var9 == 0.0F && var10 == 0.0F && var11 == 0.0F ? null : Direction.getApproximateNearest(var9, var10, var11);
            float var14 = var21 == null ? 1.0F : (var21.getAxis() == Axis.Y ? this.cardinal.byFace(var21) : this.cardinal.up() * this.cardinal.byFace(var21));
            int var15 = flags(this.fluidTranslucent, var21);

            for (int var19 : new int[]{0, 1, 2, 0, 2, 3}) {
               int var20 = var19 * 8;
               this.vertex(
                  this.fq[var20],
                  this.fq[var20 + 1],
                  this.fq[var20 + 2],
                  this.fq[var20 + 3],
                  this.fq[var20 + 4],
                  unshade(Float.floatToRawIntBits(this.fq[var20 + 5]), var14),
                  Float.floatToRawIntBits(this.fq[var20 + 6]),
                  var15
               );
            }
         }
      }

      @Override
      public VertexConsumer addVertex(float var1, float var2, float var3) {
         throw new UnsupportedOperationException();
      }

      @Override
      public VertexConsumer setColor(int var1, int var2, int var3, int var4) {
         return this;
      }

      @Override
      public VertexConsumer setColor(int var1) {
         return this;
      }

      @Override
      public VertexConsumer setUv(float var1, float var2) {
         return this;
      }

      @Override
      public VertexConsumer setUv1(int var1, int var2) {
         return this;
      }

      @Override
      public VertexConsumer setUv2(int var1, int var2) {
         return this;
      }

      @Override
      public VertexConsumer setUv3(float var1, float var2) {
         return this;
      }

      @Override
      public VertexConsumer setNormal(float var1, float var2, float var3) {
         return this;
      }

      @Override
      public VertexConsumer setLineWidth(float var1) {
         return this;
      }
   }
}
