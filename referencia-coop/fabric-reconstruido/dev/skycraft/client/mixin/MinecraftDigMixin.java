package dev.skycraft.client.mixin;

import dev.skycraft.client.SkyDigClient;
import net.minecraft.client.Minecraft;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.item.ItemStack;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(Minecraft.class)
public abstract class MinecraftDigMixin {
   @Inject(method = "startAttack", at = @At("HEAD"), cancellable = true)
   private void skycraft$digStart(CallbackInfoReturnable<Boolean> var1) {
      Minecraft var2 = (Minecraft)this;
      if (SkyDigClient.attack(var2)) {
         ItemStack var3 = var2.player.getItemInHand(InteractionHand.MAIN_HAND);
         var2.player.swing(InteractionHand.MAIN_HAND, var3.getAttackAnimation(), false);
         var1.setReturnValue(true);
      }
   }

   @Inject(method = "continueAttack", at = @At("HEAD"))
   private void skycraft$digHold(boolean var1, CallbackInfo var2) {
      if (var1) {
         SkyDigClient.attack((Minecraft)this);
      }
   }
}
