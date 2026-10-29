package dev.skycraft.client.mixin;

import dev.skycraft.client.render.WorldExporter;
import net.minecraft.client.multiplayer.ClientLevel;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.gen.Accessor;

/** Force the unchanged bridge to send a fresh atlas and clear after a scene-ready marker. */
@Mixin(value = WorldExporter.class, remap = false)
public interface SceneExporterAccess {
    @Accessor("sentLevel") static void peakcraft$setSentLevel(ClientLevel value) { throw new AssertionError(); }
}
