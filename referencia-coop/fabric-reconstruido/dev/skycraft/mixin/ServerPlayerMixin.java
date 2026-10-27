package dev.skycraft.mixin;

import dev.skycraft.SkyCraft;
import dev.skycraft.combat.SkyCombat;
import dev.skycraft.combat.SkyrimActorEntity;
import dev.skycraft.link.SkyLink;
import dev.skycraft.net.SkyNet;
import net.fabricmc.fabric.api.networking.v1.ServerPlayNetworking;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.damagesource.DamageSource;
import net.minecraft.world.entity.Entity;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(ServerPlayer.class)
public abstract class ServerPlayerMixin {
   @Inject(method = "crit", at = @At("HEAD"))
   private void skycraft$critSkyrim(Entity var1, CallbackInfo var2) {
      if (var1 instanceof SkyrimActorEntity var3) {
         var3.markCritical();
      }
   }

   @Inject(method = "die", at = @At("HEAD"))
   private void skycraft$diesInSkyrim(DamageSource var1, CallbackInfo var2) {
      ServerPlayer var3 = (ServerPlayer)this;
      int var4 = SkyCombat.attackerFormId(var1);
      if (!SkyNet.isHost(var3)) {
         if (ServerPlayNetworking.canSend(var3, SkyNet.Died.TYPE)) {
            ServerPlayNetworking.send(var3, new SkyNet.Died(var4));
         }

         SkyCraft.LOG.info("SkyCraft: guest {} died ({}); telling their Skyrim", var3.getPlainTextName(), var1.getMsgId());
      } else {
         if (SkyLink.active()) {
            SkyLink.pushEvent(2, var4, 0.0F, 0.0F, 0.0F, 0.0F, 0);
            SkyCraft.LOG.info("SkyCraft: player died ({}); telling Skyrim", var1.getMsgId());
         }
      }
   }
}
