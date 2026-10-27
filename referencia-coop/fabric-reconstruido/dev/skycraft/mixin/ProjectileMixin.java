package dev.skycraft.mixin;

import dev.skycraft.world.SkyClip;
import net.minecraft.world.entity.projectile.Projectile;
import net.minecraft.world.entity.projectile.ProjectileDeflection;
import net.minecraft.world.phys.HitResult;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(Projectile.class)
public abstract class ProjectileMixin {
   @Shadow
   protected abstract void onHit(HitResult var1);

   @Inject(method = "hitTargetOrDeflectSelf", at = @At("HEAD"), cancellable = true)
   private void skycraft$hitSkyrim(HitResult var1, CallbackInfoReturnable<ProjectileDeflection> var2) {
      if (var1 instanceof SkyClip.SkyrimHitResult) {
         this.onHit(var1);
         var2.setReturnValue(ProjectileDeflection.NONE);
      }
   }
}
