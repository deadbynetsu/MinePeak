package dev.skycraft.mixin;

import dev.skycraft.link.SkyLink;
import net.minecraft.server.MinecraftServer;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(MinecraftServer.class)
public abstract class MinecraftServerFlightMixin {
   @Inject(method = "allowFlight", at = @At("HEAD"), cancellable = true)
   private void skycraft$guestsStandOnTheirSkyrim(CallbackInfoReturnable<Boolean> var1) {
      if (SkyLink.active()) {
         var1.setReturnValue(true);
      }
   }
}
