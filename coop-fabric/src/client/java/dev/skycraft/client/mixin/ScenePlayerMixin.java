package dev.skycraft.client.mixin;

import dev.skycraft.client.scene.SceneWorlds;
import java.util.Optional;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.players.NameAndId;
import net.minecraft.server.players.PlayerList;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.level.storage.PlayerDataStorage;
import org.spongepowered.asm.mixin.Final;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(PlayerList.class)
abstract class ScenePlayerMixin {
    @Shadow @Final private MinecraftServer server;
    @Inject(method="loadPlayerData", at=@At("HEAD"), cancellable=true)
    private void peakcraft$load(NameAndId player, CallbackInfoReturnable<Optional<CompoundTag>> result) {
        Optional<CompoundTag> transfer = SceneWorlds.transferredPlayer(server, player);
        if (transfer != null) result.setReturnValue(transfer);
    }
}

@Mixin(PlayerDataStorage.class)
abstract class ScenePlayerSavedMixin {
    @Inject(method="save", at=@At("TAIL"))
    private void peakcraft$saved(Player player, CallbackInfo ci) { SceneWorlds.playerSaved(player); }
}
