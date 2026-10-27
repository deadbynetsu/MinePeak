package dev.skycraft.client.render;

import com.mojang.blaze3d.platform.NativeImage;
import com.mojang.blaze3d.vertex.PoseStack;
import com.mojang.blaze3d.vertex.VertexConsumer;
import dev.skycraft.SkyCraft;
import dev.skycraft.client.mixin.RenderSetupAccessor;
import dev.skycraft.client.mixin.RenderTypeAccessor;
import dev.skycraft.client.mixin.TextureBindingAccessor;
import dev.skycraft.client.mixin.TextureManagerAccessor;
import dev.skycraft.combat.SkyrimActorEntity;
import dev.skycraft.link.SkyLink;
import java.io.IOException;
import java.io.InputStream;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.function.Function;
import net.fabricmc.fabric.api.client.renderer.v1.mesh.Mesh;
import net.fabricmc.fabric.api.client.renderer.v1.mesh.MeshView;
import net.fabricmc.fabric.api.client.renderer.v1.mesh.QuadAtlas;
import net.minecraft.client.Camera;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.Font.DisplayMode;
import net.minecraft.client.model.Model;
import net.minecraft.client.model.geom.builders.UVPair;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.client.particle.SingleQuadParticle.Layer;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.client.renderer.OrderedSubmitNodeCollector;
import net.minecraft.client.renderer.SubmitNodeCollector;
import net.minecraft.client.renderer.SubmitNodeCollector.CustomGeometryRenderer;
import net.minecraft.client.renderer.block.ModelBlockRenderer;
import net.minecraft.client.renderer.block.MovingBlockRenderState;
import net.minecraft.client.renderer.block.dispatch.BlockStateModel;
import net.minecraft.client.renderer.block.dispatch.BlockStateModelPart;
import net.minecraft.client.renderer.blockentity.BlockEntityRenderDispatcher;
import net.minecraft.client.renderer.blockentity.state.BlockEntityRenderState;
import net.minecraft.client.renderer.chunk.ChunkSectionLayer;
import net.minecraft.client.renderer.entity.EntityRenderDispatcher;
import net.minecraft.client.renderer.entity.state.ArmedEntityRenderState;
import net.minecraft.client.renderer.entity.state.AvatarRenderState;
import net.minecraft.client.renderer.entity.state.EntityRenderState;
import net.minecraft.client.renderer.entity.state.HumanoidRenderState;
import net.minecraft.client.renderer.entity.state.LivingEntityRenderState;
import net.minecraft.client.renderer.entity.state.EntityRenderState.LeashState;
import net.minecraft.client.renderer.entity.state.EntityRenderState.ShadowPiece;
import net.minecraft.client.renderer.feature.ModelFeatureRenderer.CrumblingOverlay;
import net.minecraft.client.renderer.item.ItemStackRenderState.FoilType;
import net.minecraft.client.renderer.rendertype.RenderType;
import net.minecraft.client.renderer.state.level.CameraRenderState;
import net.minecraft.client.renderer.state.level.ParticleGroupRenderState;
import net.minecraft.client.renderer.state.level.QuadParticleRenderState;
import net.minecraft.client.renderer.texture.AbstractTexture;
import net.minecraft.client.renderer.texture.DynamicTexture;
import net.minecraft.client.renderer.texture.OverlayTexture;
import net.minecraft.client.renderer.texture.TextureAtlas;
import net.minecraft.client.renderer.texture.TextureAtlasSprite;
import net.minecraft.client.renderer.texture.UvMapping;
import net.minecraft.client.resources.model.geometry.BakedQuad;
import net.minecraft.client.resources.model.geometry.ItemQuads;
import net.minecraft.client.resources.model.geometry.BakedQuad.MaterialInfo;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.Identifier;
import net.minecraft.server.packs.resources.Resource;
import net.minecraft.util.FormattedCharSequence;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.Pose;
import net.minecraft.world.entity.item.ItemEntity;
import net.minecraft.world.entity.player.PlayerModelType;
import net.minecraft.world.entity.player.PlayerSkin;
import net.minecraft.world.entity.projectile.ItemSupplier;
import net.minecraft.world.entity.projectile.arrow.AbstractArrow;
import net.minecraft.world.item.ItemDisplayContext;
import net.minecraft.world.level.block.entity.BlockEntity;
import net.minecraft.world.level.chunk.LevelChunk;
import net.minecraft.world.phys.Vec3;
import net.minecraft.world.phys.shapes.VoxelShape;
import org.joml.Matrix4f;
import org.joml.Quaternionf;
import org.joml.Vector3f;
import org.joml.Vector3fc;
import org.jspecify.annotations.Nullable;

final class AvatarExporter implements SubmitNodeCollector {
   private static final int SOLID = 121;
   private static final int PARTICLE = 9;
   private static final int PARTICLE_BLENDED = 10;
   private static final int UV_RAW = 0;
   private static final int UV_BLOCK_ATLAS = 1;
   private static final int UV_ITEM_ATLAS = 2;
   private static final Direction[] FACES_AND_NONE = new Direction[]{
      null, Direction.DOWN, Direction.UP, Direction.NORTH, Direction.SOUTH, Direction.WEST, Direction.EAST
   };
   private static final double SCENE_RANGE = 64.0;
   private static final int SCENE_MAX_ENTITIES = 48;
   private static final double BLOCK_ENTITY_RANGE = 48.0;
   private static final int SCENE_MAX_BLOCK_ENTITIES = 256;
   private static boolean warnedBlockEntity;
   private static final Map<Identifier, Integer> TEXTURE_IDS = new HashMap<>();
   private static final Set<Identifier> UNUSABLE = new HashSet<>();
   private static int nextTextureId = 1;
   private static @Nullable ModelBlockRenderer movingBlocks;
   private static boolean warnedEntity;
   private static final AvatarExporter AVATAR = new AvatarExporter();
   private static final AvatarExporter SCENE = new AvatarExporter();
   private static final AvatarExporter RAGDOLL = new AvatarExporter();
   private static long nextRagdollNanos;
   private static final boolean FIRST_PERSON_AVATAR = Boolean.parseBoolean(System.getProperty("skycraft.firstPersonAvatar", "true"));
   private final Map<Long, AvatarExporter.Batch> batches = new HashMap<>();
   private final AvatarExporter.Capture capture = new AvatarExporter.Capture();
   private boolean shown;
   private SkyAtlas atlas;
   private float offX;
   private float offY;
   private float offZ;

   private AvatarExporter() {
   }

   static void reset() {
      TEXTURE_IDS.clear();
      UNUSABLE.clear();
      nextTextureId = 1;
      AVATAR.shown = false;
      SCENE.shown = false;
      RAGDOLL.shown = false;
      nextRagdollNanos = 0L;
      movingBlocks = null;
   }

   static void frame(Minecraft var0, SkyAtlas var1, float var2) {
      AVATAR.exportAvatar(var0, var1, var2);
      SCENE.exportScene(var0, var1, var2);
      long var3 = System.nanoTime();
      if (var3 >= nextRagdollNanos) {
         nextRagdollNanos = var3 + 1000000000L;
         RAGDOLL.exportRagdoll(var0, var1, var2);
      }
   }

   private void exportAvatar(Minecraft var1, SkyAtlas var2, float var3) {
      this.atlas = var2;
      LocalPlayer var4 = var1.player;
      Camera var5 = var1.gameRenderer.mainCamera();
      if (var4 != null && (var5.isDetached() || FIRST_PERSON_AVATAR)) {
         this.begin();

         try {
            EntityRenderDispatcher var6 = var1.getEntityRenderDispatcher();
            var6.prepare(var5, var1.crosshairPickEntity);
            EntityRenderState var7 = var6.extractEntity(var4, var3);
            CameraRenderState var8 = var1.gameRenderer.gameRenderState().levelRenderState.cameraRenderState;
            var6.submit(var7, var8, 0.0, 0.0, 0.0, new PoseStack(), this);
         } catch (RuntimeException var9) {
            SkyCraft.LOG.warn("SkyCraft: couldn't capture the player model", var9);
            return;
         }

         this.send(5, null);
      } else {
         this.sendEmpty(5, false);
      }
   }

   private void exportRagdoll(Minecraft var1, SkyAtlas var2, float var3) {
      this.atlas = var2;
      LocalPlayer var4 = var1.player;
      if (var4 != null && !var4.isDeadOrDying() && !var4.isSpectator()) {
         this.begin();

         try {
            EntityRenderDispatcher var5 = var1.getEntityRenderDispatcher();
            Camera var6 = var1.gameRenderer.mainCamera();
            var5.prepare(var6, var1.crosshairPickEntity);
            EntityRenderState var7 = var5.extractEntity(var4, var3);
            var7.ageInTicks = 0.0F;
            var7.nameTag = null;
            if (var7 instanceof LivingEntityRenderState var8) {
               var8.bodyRot = var8.yRot = var8.xRot = 0.0F;
               var8.deathTime = 0.0F;
               var8.walkAnimationPos = var8.walkAnimationSpeed = 0.0F;
               var8.hasRedOverlay = false;
               var8.isAutoSpinAttack = false;
               var8.pose = Pose.STANDING;
            }

            if (var7 instanceof ArmedEntityRenderState var10) {
               var10.rightHandItemState.clear();
               var10.leftHandItemState.clear();
               var10.currentSwing = null;
               var10.swingAnimation = 0.0F;
            }

            if (var7 instanceof HumanoidRenderState var11) {
               var11.swimAmount = 0.0F;
               var11.isCrouching = var11.isFallFlying = var11.isVisuallySwimming = var11.isPassenger = var11.isUsingItem = false;
            }

            if (var7 instanceof AvatarRenderState var12) {
               var12.arrowCount = var12.stingerCount = 0;
               var12.parrotOnLeftShoulder = var12.parrotOnRightShoulder = null;
            }

            CameraRenderState var13 = var1.gameRenderer.gameRenderState().levelRenderState.cameraRenderState;
            var5.submit(var7, var13, 0.0, 0.0, 0.0, new PoseStack(), this);
         } catch (RuntimeException var9) {
            SkyCraft.LOG.warn("SkyCraft: couldn't capture the player's body for the ragdoll", var9);
            return;
         }

         this.sendParts(9);
      }
   }

   private static int partAt(float var0, float var1) {
      if (var1 >= 1.5F) {
         return 1;
      } else if (var1 >= 0.75F) {
         return var0 < -0.255F ? 3 : (var0 > 0.255F ? 4 : 2);
      } else {
         return var0 < 0.0F ? 5 : 6;
      }
   }

   private void sendParts(int var1) {
      this.capture.flush();
      ArrayList var2 = new ArrayList();
      int var3 = 0;

      record Group(AvatarExporter.Batch batch, int part, int[] quads, int count) {
      }

      for (AvatarExporter.Batch var5 : this.batches.values()) {
         int var6 = var5.count / 4;
         if (var6 != 0) {
            int[][] var7 = new int[7][var6];
            int[] var8 = new int[7];
            int var9 = 0;

            while (var9 < var6) {
               float var10 = 0.0F;
               float var11 = 0.0F;

               for (int var12 = 0; var12 < 4; var12++) {
                  int var13 = (var9 * 4 + var12) * 8;
                  var10 += Float.intBitsToFloat(var5.data[var13]);
                  var11 += Float.intBitsToFloat(var5.data[var13 + 1]);
               }

               int var22 = partAt(var10 * 0.25F, var11 * 0.25F);
               var7[var22][var8[var22]++] = var9++;
            }

            for (int var19 = 1; var19 < 7; var19++) {
               if (var8[var19] > 0) {
                  var2.add(new Group(var5, var19, var7[var19], var8[var19]));
                  var3 += var8[var19] * 6;
               }
            }
         }
      }

      if (!var2.isEmpty()) {
         ByteBuffer var14 = ByteBuffer.allocate(8 + var2.size() * 16).order(ByteOrder.LITTLE_ENDIAN);
         var14.putInt(var2.size()).putInt(var3);
         ByteBuffer var15 = ByteBuffer.allocateDirect(var3 * 32).order(ByteOrder.LITTLE_ENDIAN);
         int var16 = 0;

         for (Group var18 : var2) {
            int var20 = var18.count() * 6;
            var14.putInt(var18.batch().texture).putInt(var16).putInt(var20).putInt((var18.batch().translucent ? 1 : 0) | var18.part() << 8);

            for (int var21 = 0; var21 < var18.count(); var21++) {
               var18.batch().writeQuad(var15, var18.quads()[var21]);
            }

            var16 += var20;
         }

         var14.flip();
         var15.flip();
         if (SkyLink.tryWriteRender(var1, var14, var15)) {
            this.shown = true;
         }
      }
   }

   private void exportScene(Minecraft var1, SkyAtlas var2, float var3) {
      this.atlas = var2;
      ClientLevel var4 = var1.level;
      LocalPlayer var5 = var1.player;
      if (var4 != null && var5 != null) {
         Camera var6 = var1.gameRenderer.mainCamera();
         Vec3 var7 = var6.position();
         double[] var8 = new double[]{Math.floor(var7.x), Math.floor(var7.y), Math.floor(var7.z)};
         this.begin();
         EntityRenderDispatcher var9 = var1.getEntityRenderDispatcher();
         var9.prepare(var6, var1.crosshairPickEntity);
         CameraRenderState var10 = var1.gameRenderer.gameRenderState().levelRenderState.cameraRenderState;
         PoseStack var11 = new PoseStack();
         int var12 = 0;

         for (Entity var14 : var4.entitiesForRendering()) {
            if (var14 != var5
               && !(var14 instanceof ItemEntity)
               && !(var14 instanceof AbstractArrow)
               && !(var14 instanceof ItemSupplier)
               && !(var14 instanceof SkyrimActorEntity)
               && !(var14.distanceToSqr(var7) > 4096.0)
               && var12 < 48) {
               var12++;

               try {
                  EntityRenderState var15 = var9.extractEntity(var14, var3);
                  var9.submit(var15, var10, var15.x - var8[0], var15.y - var8[1], var15.z - var8[2], var11, this);
               } catch (RuntimeException var17) {
                  if (!warnedEntity) {
                     warnedEntity = true;
                     SkyCraft.LOG.warn("SkyCraft: couldn't capture {} for Skyrim", var14, var17);
                  }
               }
            }
         }

         this.submitBlockEntities(var1, var4, var7, var8, var3, var10, var11);
         this.capture.flush();
         this.offX = (float)(var7.x - var8[0]);
         this.offY = (float)(var7.y - var8[1]);
         this.offZ = (float)(var7.z - var8[2]);

         try {
            for (ParticleGroupRenderState var19 : var1.gameRenderer.gameRenderState().levelRenderState.particlesRenderState.particles) {
               var19.submit(this, var10);
            }
         } catch (RuntimeException var16) {
            SkyCraft.LOG.warn("SkyCraft: couldn't capture particles for Skyrim", var16);
         }

         this.capture.flush();
         this.offX = this.offY = this.offZ = 0.0F;
         this.send(6, var8);
      }
   }

   private void submitBlockEntities(Minecraft var1, ClientLevel var2, Vec3 var3, double[] var4, float var5, CameraRenderState var6, PoseStack var7) {
      BlockEntityRenderDispatcher var8 = var1.getBlockEntityRenderDispatcher();
      var8.prepare(var3);
      double var9 = 2304.0;
      int var11 = 0;
      int var12 = (int)Math.floor((var3.x - 48.0) / 16.0);
      int var13 = (int)Math.floor((var3.x + 48.0) / 16.0);
      int var14 = (int)Math.floor((var3.z - 48.0) / 16.0);
      int var15 = (int)Math.floor((var3.z + 48.0) / 16.0);

      for (int var16 = var12; var16 <= var13; var16++) {
         for (int var17 = var14; var17 <= var15; var17++) {
            LevelChunk var18 = var2.getChunkSource().getChunk(var16, var17, false);
            if (var18 != null) {
               for (BlockEntity var20 : var18.getBlockEntities().values()) {
                  BlockPos var21 = var20.getBlockPos();
                  if (!var20.isRemoved() && !(var21.distToCenterSqr(var3) > var9) && var11 < 256) {
                     try {
                        BlockEntityRenderState var22 = var8.tryExtractRenderState(var20, var5, null, false);
                        if (var22 == null) {
                           var22 = var8.tryExtractRenderState(var20, var5, null, true);
                        }

                        if (var22 != null) {
                           var11++;
                           var7.pushPose();
                           var7.translate(var21.getX() - var4[0], var21.getY() - var4[1], var21.getZ() - var4[2]);
                           var8.submit(var22, var7, this, var6);
                           var7.popPose();
                        }
                     } catch (RuntimeException var23) {
                        if (!warnedBlockEntity) {
                           warnedBlockEntity = true;
                           SkyCraft.LOG.warn("SkyCraft: couldn't capture {} at {} for Skyrim", new Object[]{var20.getType(), var21, var23});
                        }
                     }
                  }
               }
            }
         }
      }
   }

   private void begin() {
      for (AvatarExporter.Batch var2 : this.batches.values()) {
         var2.clear();
      }
   }

   private void send(int var1, double @Nullable [] var2) {
      this.capture.flush();
      ArrayList var3 = new ArrayList();
      int var4 = 0;

      for (AvatarExporter.Batch var6 : this.batches.values()) {
         if (var6.count >= 4) {
            var3.add(var6);
            var4 += var6.count / 4 * 6;
         }
      }

      if (var3.isEmpty()) {
         this.sendEmpty(var1, var2 != null);
      } else {
         ByteBuffer var11 = ByteBuffer.allocate((var2 != null ? 24 : 0) + 8 + var3.size() * 16).order(ByteOrder.LITTLE_ENDIAN);
         if (var2 != null) {
            var11.putDouble(var2[0]).putDouble(var2[1]).putDouble(var2[2]);
         }

         var11.putInt(var3.size()).putInt(var4);
         ByteBuffer var12 = ByteBuffer.allocateDirect(var4 * 32).order(ByteOrder.LITTLE_ENDIAN);
         int var7 = 0;

         for (AvatarExporter.Batch var9 : var3) {
            int var10 = var9.count / 4 * 6;
            var11.putInt(var9.texture).putInt(var7).putInt(var10).putInt(var9.translucent ? 1 : 0);
            var9.writeTriangles(var12);
            var7 += var10;
         }

         var11.flip();
         var12.flip();
         if (SkyLink.tryWriteRender(var1, var11, var12)) {
            this.shown = true;
         }
      }
   }

   private void sendEmpty(int var1, boolean var2) {
      if (this.shown) {
         ByteBuffer var3 = ByteBuffer.allocate((var2 ? 24 : 0) + 8).order(ByteOrder.LITTLE_ENDIAN);
         if (var2) {
            var3.putDouble(0.0).putDouble(0.0).putDouble(0.0);
         }

         var3.putInt(0).putInt(0).flip();
         this.shown = !SkyLink.writeRender(var1, var3, null);
      }
   }

   private AvatarExporter.Batch batch(int var1, int var2, int var3) {
      long var4 = (long)var1 << 16 | (long)var2 << 8 | var3;
      return this.batches.computeIfAbsent(var4, var4x -> new AvatarExporter.Batch(var1, var2, var3));
   }

   private AvatarExporter.@Nullable Batch batchFor(RenderType var1) {
      RenderTypeAccessor var2 = (RenderTypeAccessor)var1;
      String var3 = var2.skycraft$name();
      if (!var3.contains("glint") && !var3.contains("outline") && !var3.contains("shadow")) {
         Object var4 = ((RenderSetupAccessor)var2.skycraft$state()).skycraft$textures().get("Sampler0");
         if (var4 == null) {
            return null;
         }

         Identifier var5 = ((TextureBindingAccessor)var4).skycraft$location();
         if (var5.equals(TextureAtlas.LOCATION_BLOCKS)) {
            return this.batch(0, 1, 121);
         }

         if (var5.equals(TextureAtlas.LOCATION_ITEMS)) {
            return this.batch(0, 2, 121);
         }

         int var6 = textureId(var5);
         return var6 < 0 ? null : this.batch(var6, 0, 121);
      } else {
         return null;
      }
   }

   private static int textureId(Identifier var0) {
      Integer var1 = TEXTURE_IDS.get(var0);
      if (var1 != null) {
         return var1;
      }

      if (UNUSABLE.contains(var0)) {
         return -1;
      }

      NativeImage var2 = readTexture(var0);
      if (var2 == null) {
         UNUSABLE.add(var0);
         SkyCraft.LOG.info("SkyCraft: texture {} isn't available to Skyrim; what uses it is left out", var0);
         return -1;
      }

      int var3 = nextTextureId++;

      try (var2) {
         int var5 = var2.getWidth();
         int var6 = var2.getHeight();
         ByteBuffer var7 = ByteBuffer.allocateDirect(var5 * var6 * 4).order(ByteOrder.LITTLE_ENDIAN);

         for (int var8 = 0; var8 < var6; var8++) {
            for (int var9 = 0; var9 < var5; var9++) {
               int var10 = var2.getPixel(var9, var8);
               var7.put((byte)(var10 >> 16)).put((byte)(var10 >> 8)).put((byte)var10).put((byte)(var10 >>> 24));
            }
         }

         var7.flip();
         ByteBuffer var13 = ByteBuffer.allocate(16).order(ByteOrder.LITTLE_ENDIAN).putInt(var3).putInt(var5).putInt(var6).putInt(skinFlags(var0)).flip();
         if (!SkyLink.writeRender(4, var13, var7)) {
            nextTextureId--;
            return -1;
         }
      }

      TEXTURE_IDS.put(var0, var3);
      SkyCraft.LOG.info("SkyCraft: sent texture {} to Skyrim (id {})", var0, var3);
      return var3;
   }

   private static int skinFlags(Identifier var0) {
      LocalPlayer var1 = Minecraft.getInstance().player;
      if (var1 == null) {
         return 0;
      }

      PlayerSkin var2 = var1.getSkin();
      return !var0.equals(var2.body().texturePath()) ? 0 : 1 | (var2.model() == PlayerModelType.SLIM ? 2 : 0);
   }

   private static @Nullable NativeImage readTexture(Identifier var0) {
      Minecraft var1 = Minecraft.getInstance();
      Optional var2 = var1.getResourceManager().getResource(var0);
      if (var2.isPresent()) {
         try (InputStream var9 = ((Resource)var2.get()).open()) {
            return NativeImage.read(var9);
         } catch (IOException var8) {
            SkyCraft.LOG.warn("SkyCraft: couldn't read {}", var0, var8);
            return null;
         }
      } else {
         AbstractTexture var3 = ((TextureManagerAccessor)var1.getTextureManager()).skycraft$byPath().get(var0);
         if (var3 instanceof DynamicTexture var4 && var4.getPixels() != null) {
            NativeImage var5 = new NativeImage(var4.getPixels().getWidth(), var4.getPixels().getHeight(), false);
            var5.copyFrom(var4.getPixels());
            return var5;
         } else {
            return var3 instanceof TextureAtlas var10 ? SkyAtlas.image(var10) : null;
         }
      }
   }

   private void addQuad(AvatarExporter.Batch var1, Matrix4f var2, BakedQuad var3, int[] var4, int var5, int var6) {
      MaterialInfo var7 = var3.materialInfo();
      int var8 = var7.isTinted() ? var7.tintIndex() : -1;
      int var9 = var8 >= 0 && var8 < var4.length ? var4[var8] : -1;
      TextureAtlasSprite var10 = var7.sprite();
      Vector3f var11 = new Vector3f();

      for (int var12 = 0; var12 < 4; var12++) {
         var2.transformPosition(var3.position(var12), var11);
         long var13 = var3.packedUV(var12);
         var1.add(var11.x(), var11.y(), var11.z(), this.atlas.u(var10, UVPair.unpackU(var13)), this.atlas.v(var10, UVPair.unpackV(var13)), var9, var5, var6);
      }
   }

   @Override
   public OrderedSubmitNodeCollector order(int var1) {
      return this;
   }

   @Override
   public <S> void submitModel(Model<? super S> var1, S var2, PoseStack var3, RenderType var4, int var5, int var6, int var7, @Nullable UvMapping var8, int var9) {
      AvatarExporter.Batch var10 = this.batchFor(var4);
      if (var10 != null) {
         this.capture.begin(var10);
         VertexConsumer var11 = var8 != null ? var8.wrap(this.capture) : this.capture;
         var1.setupAnim(var2);
         var1.renderToBuffer(var3, var11, var5, var6, var7);
         this.capture.flush();
      }
   }

   @Override
   public void submitItem(PoseStack var1, ItemDisplayContext var2, int var3, int var4, int var5, int[] var6, ItemQuads var7, FoilType var8) {
      this.capture.flush();
      AvatarExporter.Batch var9 = this.batch(0, 0, 121);
      Matrix4f var10 = var1.last().pose();

      for (BakedQuad var12 : var7.all()) {
         this.addQuad(var9, var10, var12, var6, var3, var4);
      }
   }

   @Override
   public void submitBlockModel(PoseStack var1, RenderType var2, List<BlockStateModelPart> var3, int[] var4, int var5, int var6, int var7) {
      this.capture.flush();
      AvatarExporter.Batch var8 = this.batch(0, 0, 121);
      Matrix4f var9 = var1.last().pose();

      for (BlockStateModelPart var11 : var3) {
         for (Direction var15 : FACES_AND_NONE) {
            for (BakedQuad var17 : var11.getQuads(var15)) {
               this.addQuad(var8, var9, var17, var4, var5, var6);
            }
         }
      }
   }

   public void submitBlockModel(
      PoseStack var1,
      Function<ChunkSectionLayer, RenderType> var2,
      boolean var3,
      List<BlockStateModelPart> var4,
      @Nullable Mesh var5,
      int[] var6,
      int var7,
      int var8,
      int var9
   ) {
      this.submitBlockModel(var1, (RenderType)null, var4, var6, var7, var8, var9);
      this.addMesh(var1.last().pose(), var5, var7, var8);
   }

   public void submitItem(
      PoseStack var1, ItemDisplayContext var2, int var3, int var4, int var5, int[] var6, ItemQuads var7, @Nullable MeshView var8, FoilType var9
   ) {
      this.submitItem(var1, var2, var3, var4, var5, var6, var7, var9);
      this.addMesh(var1.last().pose(), var8, var3, var4);
   }

   public void submitBreakingBlockModel(PoseStack var1, List<BlockStateModelPart> var2, @Nullable Mesh var3, int var4, boolean var5) {
   }

   private void addMesh(Matrix4f var1, @Nullable MeshView var2, int var3, int var4) {
      if (var2 != null && var2.size() != 0) {
         Vector3f var5 = new Vector3f();
         var2.forEach(var5x -> {
            AvatarExporter.Batch var6 = this.batch(0, var5x.atlas() == QuadAtlas.ITEM ? 2 : 1, 121);

            for (int var7 = 0; var7 < 4; var7++) {
               var1.transformPosition(var5x.x(var7), var5x.y(var7), var5x.z(var7), var5);
               int var8 = Math.max(var5x.lightmap(var7), var3);
               var6.add(var5.x(), var5.y(), var5.z(), var5x.u(var7), var5x.v(var7), var5x.color(var7), var8, var4);
            }
         });
      }
   }

   @Override
   public void submitMovingBlock(PoseStack var1, MovingBlockRenderState var2, int var3) {
      this.capture.flush();
      Minecraft var4 = Minecraft.getInstance();
      if (movingBlocks == null) {
         movingBlocks = new ModelBlockRenderer(false, true, var4.getBlockColors());
      }

      BlockStateModel var5 = var4.getModelManager().getBlockStateModelSet().get(var2.blockState);
      AvatarExporter.Batch var6 = this.batch(0, 0, 121);
      Matrix4f var7 = new Matrix4f(var1.last().pose());
      Vector3f var8 = new Vector3f();
      movingBlocks.tesselateBlock(
         (var4x, var5x, var6x, var7x, var8x) -> {
            TextureAtlasSprite var9 = var7x.materialInfo().sprite();
            int var10 = var7x.materialInfo().lightEmission();

            for (int var11 = 0; var11 < 4; var11++) {
               Vector3fc var12 = var7x.position(var11);
               var7.transformPosition(var12.x() + var4x, var12.y() + var5x, var12.z() + var6x, var8);
               long var13 = var7x.packedUV(var11);
               var6.add(
                  var8.x(),
                  var8.y(),
                  var8.z(),
                  this.atlas.u(var9, UVPair.unpackU(var13)),
                  this.atlas.v(var9, UVPair.unpackV(var13)),
                  var8x.getColor(var11),
                  var8x.getLightCoordsWithEmission(var11, var10),
                  OverlayTexture.NO_OVERLAY
               );
            }
         },
         0.0F,
         0.0F,
         0.0F,
         var2,
         var2.blockPos,
         var2.blockState,
         var5,
         var2.blockState.getSeed(var2.randomSeedPos)
      );
   }

   @Override
   public void submitQuadParticleGroup(QuadParticleRenderState var1) {
      this.capture.flush();

      for (Layer var3 : var1.layers()) {
         Identifier var4 = var3.textureAtlasLocation();
         int var5 = var3.translucent() ? 10 : 9;
         AvatarExporter.Batch var6;
         if (var4.equals(TextureAtlas.LOCATION_BLOCKS)) {
            var6 = this.batch(0, 1, var5);
         } else if (var4.equals(TextureAtlas.LOCATION_ITEMS)) {
            var6 = this.batch(0, 2, var5);
         } else {
            int var7 = textureId(var4);
            if (var7 < 0) {
               continue;
            }

            var6 = this.batch(var7, 0, var5);
         }

         this.capture.begin(var6);
         var1.buildLayer(var3, this.capture);
         this.capture.flush();
      }
   }

   @Override
   public void submitShadow(PoseStack var1, float var2, List<ShadowPiece> var3) {
   }

   @Override
   public void submitNameTag(PoseStack var1, @Nullable Vec3 var2, int var3, Component var4, boolean var5, int var6, CameraRenderState var7) {
   }

   @Override
   public void submitText(
      PoseStack var1, float var2, float var3, FormattedCharSequence var4, boolean var5, DisplayMode var6, int var7, int var8, int var9, int var10
   ) {
   }

   @Override
   public void submitTextBackground(PoseStack var1, float var2, float var3, float var4, float var5, int var6, DisplayMode var7, int var8) {
   }

   @Override
   public void submitFlame(PoseStack var1, EntityRenderState var2, Quaternionf var3) {
   }

   @Override
   public void submitLeash(PoseStack var1, LeashState var2) {
   }

   @Override
   public <S> void submitCrumblingOverlay(Model<? super S> var1, S var2, PoseStack var3, RenderType var4, int var5, int var6, int var7, CrumblingOverlay var8) {
   }

   @Override
   public void submitBreakingBlockModel(PoseStack var1, List<BlockStateModelPart> var2, int var3, boolean var4) {
   }

   @Override
   public void submitShapeOutline(PoseStack var1, VoxelShape var2, RenderType var3, int var4, float var5, boolean var6) {
   }

   @Override
   public void submitCustomGeometry(PoseStack var1, RenderType var2, CustomGeometryRenderer var3) {
   }

   @Override
   public void submitGizmoPrimitives(net.minecraft.client.renderer.gizmos.DrawableGizmoPrimitives.Group var1, CameraRenderState var2, boolean var3) {
   }

   private final class Batch {
      final int texture;
      final int uvMode;
      final int flags;
      final boolean translucent;
      int[] data = new int[2048];
      int count;

      Batch(int nullx, int nullxx, int nullxxx) {
         this.texture = nullx;
         this.uvMode = nullxx;
         this.flags = nullxxx;
         this.translucent = (nullxxx & 2) != 0;
      }

      void clear() {
         this.count = 0;
      }

      void add(float var1, float var2, float var3, float var4, float var5, int var6, int var7, int var8) {
         if ((this.count + 1) * 8 > this.data.length) {
            this.data = Arrays.copyOf(this.data, this.data.length * 2);
         }

         if (this.uvMode == 1) {
            var4 = AvatarExporter.this.atlas.blockU(var4);
            var5 = AvatarExporter.this.atlas.blockV(var5);
         } else if (this.uvMode == 2) {
            var4 = AvatarExporter.this.atlas.itemU(var4);
            var5 = AvatarExporter.this.atlas.itemV(var5);
         }

         if ((var8 >>> 16 & 65535) < 8) {
            int var9 = var6 >> 16 & 0xFF;
            int var10 = (int)((var6 >> 8 & 0xFF) * 0.55F);
            int var11 = (int)((var6 & 0xFF) * 0.55F);
            var6 = var6 & 0xFF000000 | var9 << 16 | var10 << 8 | var11;
         }

         if ((var8 & 65535) >= 8) {
            var7 = 15728880;
         }

         int var12 = this.count * 8;
         this.data[var12] = Float.floatToRawIntBits(var1 + AvatarExporter.this.offX);
         this.data[var12 + 1] = Float.floatToRawIntBits(var2 + AvatarExporter.this.offY);
         this.data[var12 + 2] = Float.floatToRawIntBits(var3 + AvatarExporter.this.offZ);
         this.data[var12 + 3] = Float.floatToRawIntBits(var4);
         this.data[var12 + 4] = Float.floatToRawIntBits(var5);
         this.data[var12 + 5] = var6;
         this.data[var12 + 6] = var7 >> 4 & 15 | (var7 >> 20 & 15) << 8;
         this.data[var12 + 7] = this.flags;
         this.count++;
      }

      void writeTriangles(ByteBuffer var1) {
         for (byte var2 = 0; var2 + 4 <= this.count; var2 += 4) {
            this.writeQuad(var1, var2 / 4);
         }
      }

      void writeQuad(ByteBuffer var1, int var2) {
         for (int var6 : new int[]{0, 1, 2, 0, 2, 3}) {
            int var7 = (var2 * 4 + var6) * 8;
            var1.putInt(this.data[var7]).putInt(this.data[var7 + 1]).putInt(this.data[var7 + 2]).putInt(this.data[var7 + 3]).putInt(this.data[var7 + 4]);
            int var8 = this.data[var7 + 5];
            var1.put((byte)(var8 >> 16)).put((byte)(var8 >> 8)).put((byte)var8).put((byte)(var8 >>> 24));
            var1.putInt(this.data[var7 + 6]).putInt(this.data[var7 + 7]);
         }
      }
   }

   private final class Capture implements VertexConsumer {
      private AvatarExporter.Batch batch;
      private boolean pending;
      private float x;
      private float y;
      private float z;
      private float u;
      private float v;
      private int color;
      private int light;
      private int overlay;

      void begin(AvatarExporter.Batch var1) {
         this.flush();
         this.batch = var1;
      }

      void flush() {
         if (this.pending && this.batch != null) {
            this.batch.add(this.x, this.y, this.z, this.u, this.v, this.color, this.light, this.overlay);
         }

         this.pending = false;
      }

      @Override
      public VertexConsumer addVertex(float var1, float var2, float var3) {
         this.flush();
         this.x = var1;
         this.y = var2;
         this.z = var3;
         this.color = -1;
         this.light = 15728880;
         this.overlay = OverlayTexture.NO_OVERLAY;
         this.pending = true;
         return this;
      }

      @Override
      public VertexConsumer setColor(int var1, int var2, int var3, int var4) {
         this.color = var4 << 24 | var1 << 16 | var2 << 8 | var3;
         return this;
      }

      @Override
      public VertexConsumer setColor(int var1) {
         this.color = var1;
         return this;
      }

      @Override
      public VertexConsumer setUv(float var1, float var2) {
         this.u = var1;
         this.v = var2;
         return this;
      }

      @Override
      public VertexConsumer setUv1(int var1, int var2) {
         this.overlay = var1 & 65535 | var2 << 16;
         return this;
      }

      @Override
      public VertexConsumer setUv2(int var1, int var2) {
         this.light = var1 & 65535 | var2 << 16;
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
