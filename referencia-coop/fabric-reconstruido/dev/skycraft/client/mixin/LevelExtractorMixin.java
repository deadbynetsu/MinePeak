package dev.skycraft.client.mixin;

import dev.skycraft.client.render.WorldExporter;
import net.minecraft.client.renderer.extract.LevelExtractor;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(LevelExtractor.class)
public abstract class LevelExtractorMixin {
   @Inject(method = "setSectionDirty(IIIZ)V", at = @At("HEAD"))
   private void skycraft$sectionDirty(int var1, int var2, int var3, boolean var4, CallbackInfo var5) {
      if (var4) {
         WorldExporter.markDirtyNow(var1, var2, var3);
      } else {
         WorldExporter.markDirty(var1, var2, var3);
      }
   }
}
