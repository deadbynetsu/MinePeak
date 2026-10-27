package dev.skycraft.client.mixin;

import com.mojang.blaze3d.platform.Window;
import dev.skycraft.client.SkyClient;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(Window.class)
public abstract class WindowMixin {
   @Inject(method = "isFocused", at = @At("HEAD"), cancellable = true)
   private void skycraft$focused(CallbackInfoReturnable<Boolean> var1) {
      if (SkyClient.tookOver()) {
         var1.setReturnValue(SkyClient.linked());
      }
   }

   @Inject(method = "isIconified", at = @At("HEAD"), cancellable = true)
   private void skycraft$notIconified(CallbackInfoReturnable<Boolean> var1) {
      if (SkyClient.linked()) {
         var1.setReturnValue(false);
      }
   }
}
