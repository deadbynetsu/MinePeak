package dev.skycraft.client.mixin;

import dev.skycraft.client.SkyClient;
import dev.skycraft.client.SkyCollider;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.phys.Vec3;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(Entity.class)
public abstract class EntityCollideMixin {
   @Inject(method = "collide", at = @At("RETURN"), cancellable = true)
   private void skycraft$smoothSkyrimCollision(Vec3 var1, CallbackInfoReturnable<Vec3> var2) {
      if (this instanceof LocalPlayer var3 && SkyClient.linked() && !var3.noPhysics) {
         var2.setReturnValue(SkyCollider.collide(var3, (Vec3)var2.getReturnValue()));
      }
   }
}
