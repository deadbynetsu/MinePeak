package dev.skycraft.net;

import dev.skycraft.combat.SkyCombat;
import dev.skycraft.world.SkyDig;
import java.util.List;
import net.fabricmc.fabric.api.networking.v1.PayloadTypeRegistry;
import net.fabricmc.fabric.api.networking.v1.ServerPlayNetworking;
import net.minecraft.core.BlockPos;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.network.codec.ByteBufCodecs;
import net.minecraft.network.codec.StreamCodec;
import net.minecraft.network.protocol.common.custom.CustomPacketPayload;
import net.minecraft.network.protocol.common.custom.CustomPacketPayload.Type;
import net.minecraft.resources.Identifier;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerPlayer;

public final class SkyNet {
   private SkyNet() {
   }

   public static void init() {
      PayloadTypeRegistry.serverboundPlay().register(SkyNet.Hurt.TYPE, SkyNet.Hurt.CODEC);
      PayloadTypeRegistry.serverboundPlay().register(SkyNet.DigOpen.TYPE, SkyNet.DigOpen.CODEC);
      PayloadTypeRegistry.serverboundPlay().register(SkyNet.DigReveal.TYPE, SkyNet.DigReveal.CODEC);
      ServerPlayNetworking.registerGlobalReceiver(SkyNet.DigOpen.TYPE, (var0, var1) -> {
         ServerPlayer var2 = var1.player();
         var1.server().execute(() -> SkyDig.open(var2, var0.world(), var0.pos(), var0.material()));
      });
      ServerPlayNetworking.registerGlobalReceiver(SkyNet.DigReveal.TYPE, (var0, var1) -> {
         ServerPlayer var2 = var1.player();
         int[] var3 = var0.materials().stream().mapToInt(Integer::intValue).toArray();
         var1.server().execute(() -> SkyDig.reveal(var2, var0.world(), var0.cells(), var3));
      });
      PayloadTypeRegistry.clientboundPlay().register(SkyNet.Died.TYPE, SkyNet.Died.CODEC);
      ServerPlayNetworking.registerGlobalReceiver(SkyNet.Hurt.TYPE, (var0, var1) -> {
         ServerPlayer var2 = var1.player();
         float var3 = Math.max(0.0F, Math.min(var0.skyrimDamage(), 10000.0F));
         var1.server().execute(() -> SkyCombat.hurtPlayer(var2, var0.kind(), var3, var0.attackerFormId(), var0.flags()));
      });
   }

   public static boolean isHost(ServerPlayer var0) {
      MinecraftServer var1 = var0.level().getServer();
      return var1 != null && var1.isSingleplayerOwner(var0.nameAndId());
   }

   public record Died(int attackerFormId) implements CustomPacketPayload {
      public static final Type<SkyNet.Died> TYPE = new Type<>(Identifier.fromNamespaceAndPath("skycraft", "died"));
      public static final StreamCodec<RegistryFriendlyByteBuf, SkyNet.Died> CODEC = StreamCodec.composite(
         ByteBufCodecs.INT, SkyNet.Died::attackerFormId, SkyNet.Died::new
      );

      @Override
      public Type<? extends CustomPacketPayload> type() {
         return TYPE;
      }
   }

   public record DigOpen(int world, BlockPos pos, int material) implements CustomPacketPayload {
      public static final Type<SkyNet.DigOpen> TYPE = new Type<>(Identifier.fromNamespaceAndPath("skycraft", "dig_open"));
      public static final StreamCodec<RegistryFriendlyByteBuf, SkyNet.DigOpen> CODEC = StreamCodec.composite(
         ByteBufCodecs.INT,
         SkyNet.DigOpen::world,
         BlockPos.STREAM_CODEC,
         SkyNet.DigOpen::pos,
         ByteBufCodecs.VAR_INT,
         SkyNet.DigOpen::material,
         SkyNet.DigOpen::new
      );

      @Override
      public Type<? extends CustomPacketPayload> type() {
         return TYPE;
      }
   }

   public record DigReveal(int world, List<BlockPos> cells, List<Integer> materials) implements CustomPacketPayload {
      public static final Type<SkyNet.DigReveal> TYPE = new Type<>(Identifier.fromNamespaceAndPath("skycraft", "dig_reveal"));
      public static final StreamCodec<RegistryFriendlyByteBuf, SkyNet.DigReveal> CODEC = StreamCodec.composite(
         ByteBufCodecs.INT,
         SkyNet.DigReveal::world,
         BlockPos.STREAM_CODEC.apply(ByteBufCodecs.list(64)),
         SkyNet.DigReveal::cells,
         ByteBufCodecs.VAR_INT.apply(ByteBufCodecs.list(64)),
         SkyNet.DigReveal::materials,
         SkyNet.DigReveal::new
      );

      @Override
      public Type<? extends CustomPacketPayload> type() {
         return TYPE;
      }
   }

   public record Hurt(int kind, float skyrimDamage, int attackerFormId, int flags) implements CustomPacketPayload {
      public static final Type<SkyNet.Hurt> TYPE = new Type<>(Identifier.fromNamespaceAndPath("skycraft", "hurt"));
      public static final StreamCodec<RegistryFriendlyByteBuf, SkyNet.Hurt> CODEC = StreamCodec.composite(
         ByteBufCodecs.VAR_INT,
         SkyNet.Hurt::kind,
         ByteBufCodecs.FLOAT,
         SkyNet.Hurt::skyrimDamage,
         ByteBufCodecs.INT,
         SkyNet.Hurt::attackerFormId,
         ByteBufCodecs.VAR_INT,
         SkyNet.Hurt::flags,
         SkyNet.Hurt::new
      );

      @Override
      public Type<? extends CustomPacketPayload> type() {
         return TYPE;
      }
   }
}
