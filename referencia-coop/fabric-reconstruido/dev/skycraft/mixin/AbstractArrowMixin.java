package dev.skycraft.mixin;

import com.llamalad7.mixinextras.injector.wrapoperation.Operation;
import com.llamalad7.mixinextras.injector.wrapoperation.WrapOperation;
import dev.skycraft.combat.SkyrimActorEntity;
import dev.skycraft.link.SkyLink;
import dev.skycraft.world.SkyClip;
import net.minecraft.util.Mth;
import net.minecraft.world.entity.LivingEntity;
import net.minecraft.world.entity.projectile.arrow.AbstractArrow;
import net.minecraft.world.entity.projectile.arrow.Arrow;
import net.minecraft.world.entity.projectile.arrow.SpectralArrow;
import net.minecraft.world.level.ClipContext;
import net.minecraft.world.level.Level;
import net.minecraft.world.phys.BlockHitResult;
import net.minecraft.world.phys.EntityHitResult;
import net.minecraft.world.phys.Vec3;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Unique;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(AbstractArrow.class)
public abstract class AbstractArrowMixin {
   @Unique
   private Vec3 skycraft$hitAt;

   @WrapOperation(
      method = "tick",
      at = @At(
         value = "INVOKE",
         target = "Lnet/minecraft/world/level/Level;clipIncludingBorder(Lnet/minecraft/world/level/ClipContext;)Lnet/minecraft/world/phys/BlockHitResult;"
      )
   )
   private BlockHitResult skycraft$hitSkyrim(Level var1, ClipContext var2, Operation<BlockHitResult> var3) {
      return SkyClip.refine(var2.getFrom(), var2.getTo(), (BlockHitResult)var3.call(new Object[]{var1, var2}), SkyClip.Use.PROJECTILE);
   }

   @Inject(method = "onHitEntity", at = @At("HEAD"))
   private void skycraft$rememberHit(EntityHitResult var1, CallbackInfo var2) {
      this.skycraft$hitAt = var1.getLocation();
   }

   @WrapOperation(method = "onHitEntity", at = @At(value = "INVOKE", target = "Lnet/minecraft/world/entity/LivingEntity;setArrowCount(I)V"))
   private void skycraft$stickInSkyrimActor(LivingEntity var1, int var2, Operation<Void> var3) {
      var3.call(new Object[]{var1, var2});
      if (var1 instanceof SkyrimActorEntity var4 && this.skycraft$hitAt != null && SkyLink.active()) {
         AbstractArrow var5 = (AbstractArrow)this;
         Vec3 var6 = var5.getDeltaMovement();
         float var7 = (float)(Mth.atan2(var6.x, var6.z) * 180.0F / (float)Math.PI);
         float var8 = (float)(Mth.atan2(var6.y, var6.horizontalDistance()) * 180.0F / (float)Math.PI);
         int var9 = var5 instanceof SpectralArrow ? 2 : (var5 instanceof Arrow var10 && var10.getColor() > 0 ? 1 : 0);
         Vec3 var11 = this.skycraft$hitAt;
         SkyLink.pushEvent(4, var4.formId(), (float)var11.x, (float)var11.y, (float)var11.z, var7, Float.floatToRawIntBits(var8), var9);
      }
   }
}
