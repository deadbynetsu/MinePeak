package dev.skycraft.client.mixin;

import com.mojang.blaze3d.platform.InputConstants;
import com.mojang.blaze3d.platform.Window;
import dev.skycraft.client.InputBridge;
import dev.skycraft.client.SkyClient;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(InputConstants.class)
public abstract class InputConstantsMixin {
   @Inject(method = "isKeyDown", at = @At("HEAD"), cancellable = true)
   private static void skycraft$isKeyDown(int var0, CallbackInfoReturnable<Boolean> var1) {
      if (SkyClient.tookOver()) {
         var1.setReturnValue(InputBridge.isKeyDown(var0));
      }
   }

   @Inject(method = "grabMouse", at = @At("HEAD"), cancellable = true)
   private static void skycraft$grabMouse(Window var0, double var1, double var3, CallbackInfo var5) {
      if (SkyClient.tookOver()) {
         var5.cancel();
      }
   }

   @Inject(method = "releaseMouse", at = @At("HEAD"), cancellable = true)
   private static void skycraft$releaseMouse(Window var0, double var1, double var3, CallbackInfo var5) {
      if (SkyClient.tookOver()) {
         var5.cancel();
      }
   }
}
